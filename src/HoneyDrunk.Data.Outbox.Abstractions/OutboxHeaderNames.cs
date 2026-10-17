// Copyright (c) HoneyDrunk Studios. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.

namespace HoneyDrunk.Data.Outbox;

/// <summary>
/// Well-known header keys stored in <see cref="OutboxMessage.Headers"/>.
/// </summary>
public static class OutboxHeaderNames
{
    /// <summary>
    /// The logical Transport destination (topic, queue, or endpoint name) for dispatch.
    /// </summary>
    public const string Destination = "x-outbox-destination";

    /// <summary>
    /// Optional causation identifier linking this message to its triggering operation.
    /// </summary>
    public const string CausationId = "x-outbox-causation-id";

    /// <summary>
    /// Optional node identifier indicating which Grid node emitted the message.
    /// </summary>
    public const string NodeId = "x-outbox-node-id";

    /// <summary>
    /// Optional studio identifier for multi-studio routing.
    /// </summary>
    public const string StudioId = "x-outbox-studio-id";

    /// <summary>
    /// Optional environment tag (e.g. <c>production</c>, <c>staging</c>).
    /// </summary>
    public const string Environment = "x-outbox-environment";
}
