// Copyright (c) HoneyDrunk Studios. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace HoneyDrunk.Data.Outbox.Persistence;

/// <summary>
/// EF Core implementation of <see cref="IOutboxReader"/> with lease-based
/// concurrency to prevent double-dispatch across competing instances.
/// </summary>
/// <typeparam name="TContext">The application's DbContext type.</typeparam>
/// <remarks>
/// <para>
/// <see cref="ClaimBatchAsync"/> uses per-message atomic
/// <c>ExecuteUpdateAsync</c> with a <c>WHERE Status = Pending</c> guard.
/// Because <see cref="OutboxMessage.Status"/> is configured as a concurrency
/// token, only one instance can claim each message. Instances that lose the
/// race simply skip that message.
/// </para>
/// <para>
/// Messages with expired leases (Status = Leased AND LeasedUntil &lt; UtcNow)
/// are automatically reclaimed by the next poll cycle.
/// </para>
/// <para>
/// For SQL Server deployments requiring higher throughput, consider a
/// provider-specific reader that uses <c>READPAST</c> / <c>UPDLOCK</c> hints.
/// </para>
/// </remarks>
public sealed class EfOutboxReader<TContext>(
    TContext dbContext,
    ILogger<EfOutboxReader<TContext>> logger) : IOutboxReader
    where TContext : DbContext
{
    private readonly TContext _dbContext = dbContext ?? throw new ArgumentNullException(nameof(dbContext));
    private readonly ILogger<EfOutboxReader<TContext>> _logger = logger ?? throw new ArgumentNullException(nameof(logger));

    /// <inheritdoc />
    public async Task<IReadOnlyList<OutboxMessage>> ClaimBatchAsync(
        int batchSize,
        TimeSpan leaseDuration,
        CancellationToken cancellationToken = default)
    {
        var now = DateTimeOffset.UtcNow;
        var leaseExpiry = now.Add(leaseDuration);
        var pendingAndReady = _dbContext.Set<OutboxMessage>()
            .Where(m => m.Status == OutboxMessageStatus.Pending)
            .Where(m => m.NextAttemptAt == null || m.NextAttemptAt <= now);
        var leasedAndExpired = _dbContext.Set<OutboxMessage>()
            .Where(m => m.Status == OutboxMessageStatus.Leased)
            .Where(m => m.LeasedUntil != null && m.LeasedUntil <= now);

        // Eligible: Pending + ready, OR Leased with expired lease (crashed dispatcher recovery)
        var candidates = await pendingAndReady
            .Union(leasedAndExpired)
            .AsNoTracking()
            .OrderBy(m => m.OccurredAt)
            .Take(batchSize)
            .ToListAsync(cancellationToken);

        if (candidates.Count == 0)
        {
            return [];
        }

        var claimed = new List<OutboxMessage>(candidates.Count);

        foreach (var candidate in candidates)
        {
            // Atomic CAS: Pending/ExpiredLease → Leased (only one instance succeeds per message)
            var updated = await _dbContext.Set<OutboxMessage>()
                .Where(m => m.Id == candidate.Id && m.Status == candidate.Status)
                .ExecuteUpdateAsync(
                    s => s
                        .SetProperty(m => m.Status, OutboxMessageStatus.Leased)
                        .SetProperty(m => m.LeasedUntil, leaseExpiry),
                    cancellationToken);

            if (updated > 0)
            {
                candidate.Status = OutboxMessageStatus.Leased;
                candidate.LeasedUntil = leaseExpiry;
                claimed.Add(candidate);
            }
            else
            {
                Log.OutboxMessageAlreadyClaimed(_logger, candidate.Id);
            }
        }

        return claimed;
    }

    /// <inheritdoc />
    public async Task MarkDispatchedAsync(Guid messageId, CancellationToken cancellationToken = default)
    {
        var affected = await _dbContext.Set<OutboxMessage>()
            .Where(m => m.Id == messageId && m.Status == OutboxMessageStatus.Leased)
            .ExecuteUpdateAsync(
                s => s
                    .SetProperty(m => m.Status, OutboxMessageStatus.Dispatched)
                    .SetProperty(m => m.LeasedUntil, (DateTimeOffset?)null),
                cancellationToken);

        if (affected == 0)
        {
            Log.OutboxStateTransitionSkipped(_logger, messageId, OutboxMessageStatus.Dispatched);
        }
    }

    /// <inheritdoc />
    public async Task ReleaseForRetryAsync(
        Guid messageId,
        int retryCount,
        DateTimeOffset nextAttemptAt,
        string? lastError = null,
        CancellationToken cancellationToken = default)
    {
        var affected = await _dbContext.Set<OutboxMessage>()
            .Where(m => m.Id == messageId && m.Status == OutboxMessageStatus.Leased)
            .ExecuteUpdateAsync(
                s => s
                    .SetProperty(m => m.Status, OutboxMessageStatus.Pending)
                    .SetProperty(m => m.RetryCount, retryCount)
                    .SetProperty(m => m.NextAttemptAt, (DateTimeOffset?)nextAttemptAt)
                    .SetProperty(m => m.LeasedUntil, (DateTimeOffset?)null)
                    .SetProperty(m => m.LastError, lastError),
                cancellationToken);

        if (affected == 0)
        {
            Log.OutboxStateTransitionSkipped(_logger, messageId, OutboxMessageStatus.Pending);
        }
    }

    /// <inheritdoc />
    public async Task DeadLetterAsync(
        Guid messageId,
        string? lastError = null,
        CancellationToken cancellationToken = default)
    {
        var affected = await _dbContext.Set<OutboxMessage>()
            .Where(m => m.Id == messageId && m.Status == OutboxMessageStatus.Leased)
            .ExecuteUpdateAsync(
                s => s
                    .SetProperty(m => m.Status, OutboxMessageStatus.DeadLetter)
                    .SetProperty(m => m.LeasedUntil, (DateTimeOffset?)null)
                    .SetProperty(m => m.LastError, lastError),
                cancellationToken);

        if (affected == 0)
        {
            Log.OutboxStateTransitionSkipped(_logger, messageId, OutboxMessageStatus.DeadLetter);
        }
    }

    /// <inheritdoc />
    public async Task CleanupDispatchedAsync(
        DateTimeOffset olderThan,
        CancellationToken cancellationToken = default)
    {
        var deleted = await _dbContext.Set<OutboxMessage>()
            .Where(m => m.Status == OutboxMessageStatus.Dispatched && m.OccurredAt < olderThan)
            .ExecuteDeleteAsync(cancellationToken);

        if (deleted > 0)
        {
            Log.CleanedUpDispatchedMessages(_logger, deleted, olderThan);
        }
    }
}
