// Copyright (c) HoneyDrunk Studios. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using Microsoft.Extensions.Logging;

namespace HoneyDrunk.Data.Outbox.Persistence;

/// <summary>
/// High-performance log messages for outbox persistence operations.
/// </summary>
internal static partial class Log
{
    [LoggerMessage(Level = LogLevel.Debug, Message = "Outbox message {MessageId} was already claimed by another instance.")]
    internal static partial void OutboxMessageAlreadyClaimed(ILogger logger, Guid messageId);

    [LoggerMessage(Level = LogLevel.Warning, Message = "Outbox state transition to {TargetStatus} skipped for message {MessageId} — message was not in Leased state.")]
    internal static partial void OutboxStateTransitionSkipped(ILogger logger, Guid messageId, OutboxMessageStatus targetStatus);

    [LoggerMessage(Level = LogLevel.Information, Message = "Cleaned up {Count} dispatched outbox messages older than {Threshold}.")]
    internal static partial void CleanedUpDispatchedMessages(ILogger logger, int count, DateTimeOffset threshold);
}
