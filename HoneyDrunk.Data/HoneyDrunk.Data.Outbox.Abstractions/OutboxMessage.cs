// Copyright (c) HoneyDrunk Studios. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.

namespace HoneyDrunk.Data.Outbox;

/// <summary>
/// Represents a message stored in the transactional outbox for reliable, at-least-once delivery.
/// </summary>
/// <remarks>
/// <para>
/// Application code creates an <see cref="OutboxMessage"/> and passes it to
/// <see cref="IOutboxWriter.WriteAsync"/> inside the same database transaction
/// that persists domain state. The <see cref="IOutboxDispatcher"/> later picks
/// up pending messages and publishes them through Transport.
/// </para>
/// <para>
/// <see cref="Headers"/> is a JSON-serialized <c>Dictionary&lt;string, string&gt;</c>.
/// Use <see cref="OutboxHeaderNames"/> for well-known keys such as destination routing.
/// </para>
/// </remarks>
public sealed class OutboxMessage
{
    /// <summary>Gets or sets the unique message identifier.</summary>
    public Guid Id { get; set; }

    /// <summary>Gets or sets the fully qualified CLR type name of the message.</summary>
    public string Type { get; set; } = string.Empty;

    /// <summary>Gets or sets the JSON-serialized message payload.</summary>
    public string Payload { get; set; } = string.Empty;

    /// <summary>Gets or sets the UTC timestamp when the domain event occurred.</summary>
    public DateTimeOffset OccurredAt { get; set; }

    /// <summary>
    /// Gets or sets the JSON-serialized header dictionary.
    /// Use <see cref="OutboxHeaderNames"/> for well-known keys.
    /// </summary>
    public string? Headers { get; set; }

    /// <summary>Gets or sets the tenant identifier.</summary>
    public string? TenantId { get; set; }

    /// <summary>Gets or sets the correlation identifier for distributed tracing.</summary>
    public string? CorrelationId { get; set; }

    /// <summary>Gets or sets the current processing status.</summary>
    public OutboxMessageStatus Status { get; set; } = OutboxMessageStatus.Pending;

    /// <summary>Gets or sets the number of dispatch attempts so far.</summary>
    public int RetryCount { get; set; }

    /// <summary>Gets or sets when the next dispatch attempt is eligible to run.</summary>
    public DateTimeOffset? NextAttemptAt { get; set; }

    /// <summary>Gets or sets the UTC time at which the current lease expires.</summary>
    public DateTimeOffset? LeasedUntil { get; set; }

    /// <summary>Gets or sets the error message from the most recent dispatch failure.</summary>
    public string? LastError { get; set; }
}
