// Copyright (c) HoneyDrunk Studios. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.

namespace HoneyDrunk.Data.Outbox;

/// <summary>
/// Records outbox messages within the current database transaction.
/// </summary>
/// <remarks>
/// <para>
/// Messages added through <see cref="WriteAsync"/> are not immediately flushed to
/// the database. They participate in the same <c>SaveChangesAsync</c> call that
/// persists domain state, guaranteeing atomicity.
/// </para>
/// <para>
/// Application code is responsible for calling <c>SaveChangesAsync</c> on the
/// owning unit-of-work after writing outbox messages.
/// </para>
/// </remarks>
/// <example>
/// <code>
/// await outboxWriter.WriteAsync(new OutboxMessage
/// {
///     Id = Guid.NewGuid(),
///     Type = typeof(OrderPlaced).FullName!,
///     Payload = JsonSerializer.Serialize(orderPlaced),
///     OccurredAt = DateTimeOffset.UtcNow,
///     Headers = OutboxHeaderSerializer.Serialize(
///         new Dictionary&lt;string, string&gt;
///         {
///             [OutboxHeaderNames.Destination] = "orders-topic"
///         })
/// });
/// await unitOfWork.SaveChangesAsync();
/// </code>
/// </example>
public interface IOutboxWriter
{
    /// <summary>
    /// Adds an outbox message to the current transaction.
    /// </summary>
    /// <param name="message">The outbox message to persist.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A task representing the operation.</returns>
    Task WriteAsync(OutboxMessage message, CancellationToken cancellationToken = default);

    /// <summary>
    /// Adds multiple outbox messages to the current transaction.
    /// </summary>
    /// <param name="messages">The outbox messages to persist.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A task representing the operation.</returns>
    Task WriteBatchAsync(IEnumerable<OutboxMessage> messages, CancellationToken cancellationToken = default);
}
