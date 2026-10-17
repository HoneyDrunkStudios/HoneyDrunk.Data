// Copyright (c) HoneyDrunk Studios. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.

namespace HoneyDrunk.Data.Outbox;

/// <summary>
/// Configuration options for the outbox persistence layer.
/// </summary>
public sealed class OutboxOptions
{
    /// <summary>
    /// Gets or sets the database schema for the outbox table.
    /// Defaults to <c>"outbox"</c>.
    /// </summary>
    public string Schema { get; set; } = "outbox";

    /// <summary>
    /// Gets or sets the table name for outbox messages.
    /// Defaults to <c>"OutboxMessages"</c>.
    /// </summary>
    public string TableName { get; set; } = "OutboxMessages";

    /// <summary>
    /// Gets or sets a value indicating whether to auto-populate <see cref="OutboxMessage.CorrelationId"/>
    /// and <see cref="OutboxMessage.TenantId"/> from the current Kernel operation context
    /// when either value is <see langword="null"/>. Defaults to <see langword="true"/>.
    /// </summary>
    public bool AutoPopulateFromContext { get; set; } = true;

    /// <summary>
    /// Gets or sets the maximum payload size in characters.
    /// Messages exceeding this limit are rejected by <see cref="IOutboxWriter"/>.
    /// Defaults to <c>1_048_576</c> (1 MB of UTF-8 text).
    /// </summary>
    public int MaxPayloadSize { get; set; } = 1_048_576;

    /// <summary>
    /// Gets or sets how long a claimed message lease is valid.
    /// If a dispatcher crashes, messages with expired leases become eligible
    /// for re-claim by another instance. Defaults to <c>5 minutes</c>.
    /// </summary>
    public TimeSpan LeaseDuration { get; set; } = TimeSpan.FromMinutes(5);
}
