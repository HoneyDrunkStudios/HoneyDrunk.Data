// Copyright (c) HoneyDrunk Studios. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using HoneyDrunk.Data.Outbox;
using HoneyDrunk.Data.Outbox.Persistence;
using HoneyDrunk.Data.Testing.Factories;
using HoneyDrunk.Data.Tests.TestFixtures;
using Microsoft.Extensions.Logging.Abstractions;

namespace HoneyDrunk.Data.Tests.Outbox.Persistence;

public sealed class EfOutboxReaderTests : IAsyncDisposable
{
    private static readonly TimeSpan DefaultLease = TimeSpan.FromMinutes(5);

    private readonly SqliteTestDbContextFactory<OutboxTestDbContext> _factory;
    private readonly OutboxTestDbContext _context;
    private readonly EfOutboxReader<OutboxTestDbContext> _reader;

    public EfOutboxReaderTests()
    {
        _factory = new SqliteTestDbContextFactory<OutboxTestDbContext>(
            options => new OutboxTestDbContext(options));
        _context = _factory.Create();
        _reader = new EfOutboxReader<OutboxTestDbContext>(
            _context,
            NullLogger<EfOutboxReader<OutboxTestDbContext>>.Instance);
    }

    public async ValueTask DisposeAsync()
    {
        await _context.DisposeAsync();
        await _factory.DisposeAsync();
    }

    // --- ClaimBatchAsync ---
    [Fact]
    public async Task ClaimBatchAsync_WithPendingMessages_ReturnsClaimed()
    {
        await SeedMessage();
        await SeedMessage();

        var claimed = await _reader.ClaimBatchAsync(10, DefaultLease);

        Assert.Equal(2, claimed.Count);
        Assert.All(claimed, m => Assert.Equal(OutboxMessageStatus.Leased, m.Status));
    }

    [Fact]
    public async Task ClaimBatchAsync_SetsLeasedUntil()
    {
        await SeedMessage();
        var before = DateTimeOffset.UtcNow;

        var claimed = await _reader.ClaimBatchAsync(10, DefaultLease);

        Assert.Single(claimed);
        Assert.NotNull(claimed[0].LeasedUntil);
        Assert.True(claimed[0].LeasedUntil >= before.Add(DefaultLease).AddSeconds(-1));
    }

    [Fact]
    public async Task ClaimBatchAsync_RespectsMaxBatchSize()
    {
        await SeedMessage();
        await SeedMessage();
        await SeedMessage();

        var claimed = await _reader.ClaimBatchAsync(2, DefaultLease);

        Assert.Equal(2, claimed.Count);
    }

    [Fact]
    public async Task ClaimBatchAsync_IgnoresDispatchedMessages()
    {
        await SeedMessage(OutboxMessageStatus.Dispatched);
        await SeedMessage(OutboxMessageStatus.Pending);

        var claimed = await _reader.ClaimBatchAsync(10, DefaultLease);

        Assert.Single(claimed);
    }

    [Fact]
    public async Task ClaimBatchAsync_IgnoresDeadLetteredMessages()
    {
        await SeedMessage(OutboxMessageStatus.DeadLetter);
        await SeedMessage(OutboxMessageStatus.Pending);

        var claimed = await _reader.ClaimBatchAsync(10, DefaultLease);

        Assert.Single(claimed);
    }

    [Fact]
    public async Task ClaimBatchAsync_IgnoresLeasedMessagesWithActiveLease()
    {
        await SeedMessage(
            OutboxMessageStatus.Leased,
            leasedUntil: DateTimeOffset.UtcNow.AddMinutes(10));

        var claimed = await _reader.ClaimBatchAsync(10, DefaultLease);

        Assert.Empty(claimed);
    }

    [Fact]
    public async Task ClaimBatchAsync_ReclaimsExpiredLeases()
    {
        var expired = await SeedMessage(
            OutboxMessageStatus.Leased,
            leasedUntil: DateTimeOffset.UtcNow.AddMinutes(-1));

        var claimed = await _reader.ClaimBatchAsync(10, DefaultLease);

        Assert.Single(claimed);
        Assert.Equal(expired.Id, claimed[0].Id);
    }

    [Fact]
    public async Task ClaimBatchAsync_SkipsMessagesWithFutureNextAttemptAt()
    {
        await SeedMessage(nextAttemptAt: DateTimeOffset.UtcNow.AddHours(1));

        var claimed = await _reader.ClaimBatchAsync(10, DefaultLease);

        Assert.Empty(claimed);
    }

    [Fact]
    public async Task ClaimBatchAsync_IncludesMessagesWithPastNextAttemptAt()
    {
        await SeedMessage(nextAttemptAt: DateTimeOffset.UtcNow.AddMinutes(-1));

        var claimed = await _reader.ClaimBatchAsync(10, DefaultLease);

        Assert.Single(claimed);
    }

    [Fact]
    public async Task ClaimBatchAsync_OrdersByOccurredAt()
    {
        var older = await SeedMessage(occurredAt: DateTimeOffset.UtcNow.AddHours(-2));
        var newer = await SeedMessage(occurredAt: DateTimeOffset.UtcNow.AddHours(-1));

        var claimed = await _reader.ClaimBatchAsync(10, DefaultLease);

        Assert.Equal(2, claimed.Count);
        Assert.Equal(older.Id, claimed[0].Id);
        Assert.Equal(newer.Id, claimed[1].Id);
    }

    [Fact]
    public async Task ClaimBatchAsync_WithNoEligibleMessages_ReturnsEmpty()
    {
        var claimed = await _reader.ClaimBatchAsync(10, DefaultLease);

        Assert.Empty(claimed);
    }

    // --- MarkDispatchedAsync ---
    [Fact]
    public async Task MarkDispatchedAsync_TransitionsToDispatched()
    {
        var message = await SeedMessage(OutboxMessageStatus.Leased, leasedUntil: DateTimeOffset.UtcNow.AddMinutes(5));

        await _reader.MarkDispatchedAsync(message.Id);
        _context.ChangeTracker.Clear();

        var updated = await _context.OutboxMessages.FindAsync(message.Id);
        Assert.NotNull(updated);
        Assert.Equal(OutboxMessageStatus.Dispatched, updated.Status);
        Assert.Null(updated.LeasedUntil);
    }

    // --- ReleaseForRetryAsync ---
    [Fact]
    public async Task ReleaseForRetryAsync_TransitionsToPendingWithRetryData()
    {
        var message = await SeedMessage(OutboxMessageStatus.Leased, leasedUntil: DateTimeOffset.UtcNow.AddMinutes(5));
        var nextAttempt = DateTimeOffset.UtcNow.AddSeconds(30);

        await _reader.ReleaseForRetryAsync(message.Id, retryCount: 2, nextAttemptAt: nextAttempt, lastError: "Timeout");
        _context.ChangeTracker.Clear();

        var updated = await _context.OutboxMessages.FindAsync(message.Id);
        Assert.NotNull(updated);
        Assert.Equal(OutboxMessageStatus.Pending, updated.Status);
        Assert.Equal(2, updated.RetryCount);
        Assert.NotNull(updated.NextAttemptAt);
        Assert.Null(updated.LeasedUntil);
        Assert.Equal("Timeout", updated.LastError);
    }

    // --- DeadLetterAsync ---
    [Fact]
    public async Task DeadLetterAsync_TransitionsToDeadLetter()
    {
        var message = await SeedMessage(OutboxMessageStatus.Leased, leasedUntil: DateTimeOffset.UtcNow.AddMinutes(5));

        await _reader.DeadLetterAsync(message.Id, lastError: "Max retries exceeded");
        _context.ChangeTracker.Clear();

        var updated = await _context.OutboxMessages.FindAsync(message.Id);
        Assert.NotNull(updated);
        Assert.Equal(OutboxMessageStatus.DeadLetter, updated.Status);
        Assert.Null(updated.LeasedUntil);
        Assert.Equal("Max retries exceeded", updated.LastError);
    }

    // --- CleanupDispatchedAsync ---
    [Fact]
    public async Task CleanupDispatchedAsync_RemovesOldDispatchedMessages()
    {
        await SeedMessage(OutboxMessageStatus.Dispatched, occurredAt: DateTimeOffset.UtcNow.AddDays(-10));
        await SeedMessage(OutboxMessageStatus.Dispatched, occurredAt: DateTimeOffset.UtcNow.AddDays(-1));

        // Recent dispatched should remain
        var recent = await SeedMessage(OutboxMessageStatus.Dispatched, occurredAt: DateTimeOffset.UtcNow);

        // Pending should remain
        var pending = await SeedMessage(OutboxMessageStatus.Pending);

        var threshold = DateTimeOffset.UtcNow.AddDays(-0.5);
        await _reader.CleanupDispatchedAsync(threshold);
        _context.ChangeTracker.Clear();

        var remaining = _context.OutboxMessages.ToList();
        Assert.Equal(2, remaining.Count);
        Assert.Contains(remaining, m => m.Id == recent.Id);
        Assert.Contains(remaining, m => m.Id == pending.Id);
    }

    [Fact]
    public async Task CleanupDispatchedAsync_DoesNotRemovePendingMessages()
    {
        await SeedMessage(OutboxMessageStatus.Pending, occurredAt: DateTimeOffset.UtcNow.AddDays(-30));

        await _reader.CleanupDispatchedAsync(DateTimeOffset.UtcNow);
        _context.ChangeTracker.Clear();

        Assert.Single(_context.OutboxMessages);
    }

    private async Task<OutboxMessage> SeedMessage(
        OutboxMessageStatus status = OutboxMessageStatus.Pending,
        DateTimeOffset? occurredAt = null,
        DateTimeOffset? nextAttemptAt = null,
        DateTimeOffset? leasedUntil = null,
        int retryCount = 0)
    {
        var message = new OutboxMessage
        {
            Id = Guid.NewGuid(),
            Type = "TestEvent",
            Payload = "{}",
            OccurredAt = occurredAt ?? DateTimeOffset.UtcNow.AddMinutes(-1),
            Status = status,
            NextAttemptAt = nextAttemptAt,
            LeasedUntil = leasedUntil,
            RetryCount = retryCount,
        };

        _context.OutboxMessages.Add(message);
        await _context.SaveChangesAsync();
        _context.ChangeTracker.Clear();
        return message;
    }
}
