// Copyright (c) HoneyDrunk Studios. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using Microsoft.Extensions.Logging;

namespace HoneyDrunk.Data.Outbox.Dispatcher;

/// <summary>
/// High-performance log messages for outbox dispatcher operations.
/// </summary>
internal static partial class Log
{
    [LoggerMessage(Level = LogLevel.Debug, Message = "Processing {Count} outbox message(s).")]
    internal static partial void ProcessingBatch(ILogger logger, int count);

    [LoggerMessage(Level = LogLevel.Debug, Message = "Outbox message {MessageId} dispatched to {Destination}.")]
    internal static partial void MessageDispatched(ILogger logger, Guid messageId, string? destination);

    [LoggerMessage(Level = LogLevel.Information, Message = "Outbox dispatcher waiting {Delay} before first cycle.")]
    internal static partial void WaitingBeforeFirstCycle(ILogger logger, TimeSpan delay);

    [LoggerMessage(Level = LogLevel.Information, Message = "Outbox dispatcher started. PollInterval={PollInterval}, BatchSize={BatchSize}, MaxRetries={MaxRetries}.")]
    internal static partial void DispatcherStarted(ILogger logger, TimeSpan pollInterval, int batchSize, int maxRetries);

    [LoggerMessage(Level = LogLevel.Information, Message = "Outbox dispatcher stopped.")]
    internal static partial void DispatcherStopped(ILogger logger);

    [LoggerMessage(Level = LogLevel.Error, Message = "Outbox dispatch cycle failed. Retrying in {Delay}.")]
    internal static partial void DispatchCycleFailed(ILogger logger, Exception ex, TimeSpan delay);

    [LoggerMessage(Level = LogLevel.Error, Message = "Outbox message {MessageId} dead-lettered after {Attempts} attempts.")]
    internal static partial void MessageDeadLettered(ILogger logger, Exception ex, Guid messageId, int attempts);

    [LoggerMessage(Level = LogLevel.Warning, Message = "Outbox message {MessageId} failed (attempt {Attempt}/{Max}). Next retry at {NextAttempt}.")]
    internal static partial void MessageFailed(ILogger logger, Exception ex, Guid messageId, int attempt, int max, DateTimeOffset nextAttempt);
}
