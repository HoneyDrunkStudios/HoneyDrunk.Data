// Copyright (c) HoneyDrunk Studios. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.

namespace HoneyDrunk.Data.Outbox;

/// <summary>
/// Claims pending outbox messages and manages their dispatch lifecycle.
/// </summary>
/// <remarks>
/// Consumed exclusively by the <see cref="IOutboxDispatcher"/> infrastructure.
/// Application code should use <see cref="IOutboxWriter"/> instead.
/// </remarks>
public interface IOutboxReader
{
    /// <summary>
    /// Atomically claims the next batch of eligible messages under a time-bound lease.
    /// Claimed messages transition from <see cref="OutboxMessageStatus.Pending"/> to
    /// <see cref="OutboxMessageStatus.Leased"/>. Messages with expired leases are
    /// also reclaimed.
    /// </summary>
    /// <param name="batchSize">Maximum number of messages to claim.</param>
    /// <param name="leaseDuration">How long the lease is valid before expiration.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The claimed messages, ordered by <see cref="OutboxMessage.OccurredAt"/>.</returns>
    Task<IReadOnlyList<OutboxMessage>> ClaimBatchAsync(
        int batchSize,
        TimeSpan leaseDuration,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Marks a message as successfully dispatched.
    /// </summary>
    /// <param name="messageId">The message identifier.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    Task MarkDispatchedAsync(Guid messageId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Releases a leased message back to <see cref="OutboxMessageStatus.Pending"/>
    /// with updated retry metadata for a future attempt.
    /// </summary>
    /// <param name="messageId">The message identifier.</param>
    /// <param name="retryCount">Updated retry count.</param>
    /// <param name="nextAttemptAt">When the next attempt should be made.</param>
    /// <param name="lastError">Error description from the failed attempt.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    Task ReleaseForRetryAsync(
        Guid messageId,
        int retryCount,
        DateTimeOffset nextAttemptAt,
        string? lastError = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Moves a message to <see cref="OutboxMessageStatus.DeadLetter"/> permanently.
    /// No further dispatch attempts will be made.
    /// </summary>
    /// <param name="messageId">The message identifier.</param>
    /// <param name="lastError">Error description from the final failed attempt.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    Task DeadLetterAsync(
        Guid messageId,
        string? lastError = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Removes dispatched messages older than the specified threshold.
    /// </summary>
    /// <param name="olderThan">Messages dispatched before this time are removed.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    Task CleanupDispatchedAsync(
        DateTimeOffset olderThan,
        CancellationToken cancellationToken = default);
}
