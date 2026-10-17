using HoneyDrunk.Data.Canary.Infrastructure;
using HoneyDrunk.Data.Outbox;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using System.Collections.Concurrent;

namespace HoneyDrunk.Data.Canary;

/// <summary>
/// Invariant 2: Outbox lease-based concurrency guarantees — no double-dispatch,
/// deterministic retry metadata, and correct status transitions.
/// </summary>
public sealed class OutboxConcurrencyCanary : IAsyncLifetime, IDisposable
{
    private CanarySqliteFixture _fixture = null!;

    public async Task InitializeAsync()
    {
        _fixture = new CanarySqliteFixture();
        await _fixture.InitializeAsync();
    }

    public async Task DisposeAsync() => await _fixture.DisposeAsync();

    public void Dispose() => _fixture.Dispose();

    /// <summary>
    /// Two concurrent dispatcher instances claiming against the same database
    /// must never publish the same message twice in a single lease cycle.
    /// </summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous unit test.</returns>
    [Fact]
    public async Task ConcurrentDispatchers_NoDoubleDispatch()
    {
        const int messageCount = 20;

        await SeedPendingMessages(messageCount);

        var publishedIds = new ConcurrentBag<Guid>();
        var leaseDuration = TimeSpan.FromMinutes(5);

        var task1 = DispatchOnce(publishedIds, leaseDuration);
        var task2 = DispatchOnce(publishedIds, leaseDuration);

        await Task.WhenAll(task1, task2);

        // Each message ID must appear at most once across both dispatchers
        var duplicates = publishedIds.GroupBy(id => id).Where(g => g.Count() > 1).ToList();

        Assert.Empty(duplicates);

        // All messages should have been dispatched
        Assert.Equal(messageCount, publishedIds.Distinct().Count());
    }

    /// <summary>
    /// After a successful claim and dispatch, message status transitions from Pending → Leased → Dispatched.
    /// </summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous unit test.</returns>
    [Fact]
    public async Task ClaimAndDispatch_TransitionsToDispatched()
    {
        var messageId = await SeedSinglePendingMessage();

        await using var context = _fixture.CreateContext();
        var reader = CreateReader(context);

        var claimed = await reader.ClaimBatchAsync(10, TimeSpan.FromMinutes(5));
        Assert.Single(claimed);
        Assert.Equal(OutboxMessageStatus.Leased, claimed[0].Status);
        Assert.NotNull(claimed[0].LeasedUntil);

        await reader.MarkDispatchedAsync(messageId);

        var dispatched = await context.OutboxMessages
            .AsNoTracking()
            .FirstAsync(m => m.Id == messageId);

        Assert.Equal(OutboxMessageStatus.Dispatched, dispatched.Status);
        Assert.Null(dispatched.LeasedUntil);
    }

    /// <summary>
    /// ReleaseForRetryAsync transitions message back to Pending with incremented retry count
    /// and records the last error message.
    /// </summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous unit test.</returns>
    [Fact]
    public async Task ReleaseForRetry_SetsRetryMetadata()
    {
        var messageId = await SeedSinglePendingMessage();

        await using var context = _fixture.CreateContext();
        var reader = CreateReader(context);

        await reader.ClaimBatchAsync(10, TimeSpan.FromMinutes(5));

        var nextAttempt = DateTimeOffset.UtcNow.AddMinutes(1);
        await reader.ReleaseForRetryAsync(messageId, retryCount: 1, nextAttempt, lastError: "Transient failure");

        var retried = await context.OutboxMessages
            .AsNoTracking()
            .FirstAsync(m => m.Id == messageId);

        Assert.Equal(OutboxMessageStatus.Pending, retried.Status);
        Assert.Equal(1, retried.RetryCount);
        Assert.Equal("Transient failure", retried.LastError);
        Assert.Null(retried.LeasedUntil);
    }

    /// <summary>
    /// DeadLetterAsync transitions message to DeadLetter and records the error.
    /// </summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous unit test.</returns>
    [Fact]
    public async Task DeadLetter_SetsTerminalStatus()
    {
        var messageId = await SeedSinglePendingMessage();

        await using var context = _fixture.CreateContext();
        var reader = CreateReader(context);

        await reader.ClaimBatchAsync(10, TimeSpan.FromMinutes(5));

        await reader.DeadLetterAsync(messageId, lastError: "Max retries exceeded");

        var deadLettered = await context.OutboxMessages
            .AsNoTracking()
            .FirstAsync(m => m.Id == messageId);

        Assert.Equal(OutboxMessageStatus.DeadLetter, deadLettered.Status);
        Assert.Equal("Max retries exceeded", deadLettered.LastError);
        Assert.Null(deadLettered.LeasedUntil);
    }

    /// <summary>
    /// Messages with expired leases are re-claimable by a competing dispatcher.
    /// </summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous unit test.</returns>
    [Fact]
    public async Task ExpiredLease_IsReclaimable()
    {
        var messageId = await SeedSinglePendingMessage();

        // First dispatcher claims with a very short (already-expired) lease
        await using (var context1 = _fixture.CreateContext())
        {
            var reader1 = CreateReader(context1);
            var claimed = await reader1.ClaimBatchAsync(10, TimeSpan.FromMilliseconds(1));
            Assert.Single(claimed);
        }

        // Allow the lease to expire
        await Task.Delay(50);

        // Second dispatcher should be able to reclaim
        await using var context2 = _fixture.CreateContext();
        var reader2 = CreateReader(context2);
        var reclaimed = await reader2.ClaimBatchAsync(10, TimeSpan.FromMinutes(5));

        Assert.Single(reclaimed);
        Assert.Equal(messageId, reclaimed[0].Id);
    }

    /// <summary>
    /// ClaimBatchAsync respects batch size limits.
    /// </summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous unit test.</returns>
    [Fact]
    public async Task ClaimBatch_RespectsBatchSize()
    {
        await SeedPendingMessages(10);

        await using var context = _fixture.CreateContext();
        var reader = CreateReader(context);

        var claimed = await reader.ClaimBatchAsync(3, TimeSpan.FromMinutes(5));

        Assert.Equal(3, claimed.Count);
        Assert.All(claimed, m => Assert.Equal(OutboxMessageStatus.Leased, m.Status));
    }

    private static OutboxMessage CreatePendingMessage(Guid id) => new()
    {
        Id = id,
        Type = "Canary.TestEvent",
        Payload = """{"test": true}""",
        OccurredAt = DateTimeOffset.UtcNow,
        Status = OutboxMessageStatus.Pending,
    };

    private static Outbox.Persistence.EfOutboxReader<CanaryDbContext> CreateReader(CanaryDbContext context) =>
        new(context, NullLogger<Outbox.Persistence.EfOutboxReader<CanaryDbContext>>.Instance);

    private async Task<Guid> SeedSinglePendingMessage()
    {
        var id = Guid.NewGuid();
        await using var context = _fixture.CreateContext();
        context.OutboxMessages.Add(CreatePendingMessage(id));
        await context.SaveChangesAsync();
        return id;
    }

    private async Task SeedPendingMessages(int count)
    {
        await using var context = _fixture.CreateContext();
        for (int i = 0; i < count; i++)
        {
            context.OutboxMessages.Add(CreatePendingMessage(Guid.NewGuid()));
        }

        await context.SaveChangesAsync();
    }

    private async Task DispatchOnce(ConcurrentBag<Guid> publishedIds, TimeSpan leaseDuration)
    {
        await using var context = _fixture.CreateContext();
        var reader = CreateReader(context);

        var claimed = await reader.ClaimBatchAsync(50, leaseDuration);

        foreach (var message in claimed)
        {
            publishedIds.Add(message.Id);
            await reader.MarkDispatchedAsync(message.Id);
        }
    }
}
