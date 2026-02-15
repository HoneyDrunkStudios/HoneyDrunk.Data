// Copyright (c) HoneyDrunk Studios. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.

namespace HoneyDrunk.Data.Outbox.Dispatcher;

/// <summary>
/// Configuration for the outbox dispatcher background service.
/// </summary>
public sealed class OutboxDispatcherOptions
{
    /// <summary>
    /// Gets or sets the interval between polling cycles. Defaults to <c>5 seconds</c>.
    /// </summary>
    public TimeSpan PollInterval { get; set; } = TimeSpan.FromSeconds(5);

    /// <summary>
    /// Gets or sets the maximum number of messages claimed per polling cycle. Defaults to <c>50</c>.
    /// </summary>
    public int BatchSize { get; set; } = 50;

    /// <summary>
    /// Gets or sets the maximum number of retries before a message is moved to <see cref="OutboxMessageStatus.DeadLetter"/>.
    /// Defaults to <c>5</c>.
    /// </summary>
    public int MaxRetryAttempts { get; set; } = 5;

    /// <summary>
    /// Gets or sets the base delay for exponential backoff. Defaults to <c>1 second</c>.
    /// </summary>
    public TimeSpan BaseRetryDelay { get; set; } = TimeSpan.FromSeconds(1);

    /// <summary>
    /// Gets or sets the upper bound for exponential backoff delay. Defaults to <c>5 minutes</c>.
    /// </summary>
    public TimeSpan MaxRetryDelay { get; set; } = TimeSpan.FromMinutes(5);

    /// <summary>
    /// Gets or sets the delay before the first polling cycle after host startup. Defaults to <see cref="TimeSpan.Zero"/>.
    /// </summary>
    public TimeSpan StartupDelay { get; set; } = TimeSpan.Zero;

    /// <summary>
    /// Gets or sets the delay after an unhandled error in a polling cycle before retrying.
    /// Defaults to <c>30 seconds</c>.
    /// </summary>
    public TimeSpan ErrorDelay { get; set; } = TimeSpan.FromSeconds(30);

    /// <summary>
    /// Gets or sets the default Transport destination used when an outbox message does not carry
    /// an <see cref="OutboxHeaderNames.Destination"/> header.
    /// Must be set unless every message specifies its own destination.
    /// </summary>
    public string DefaultDestination { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets how long a claimed lease is valid before it expires
    /// and the message becomes eligible for re-claim. Defaults to <c>5 minutes</c>.
    /// </summary>
    public TimeSpan LeaseDuration { get; set; } = TimeSpan.FromMinutes(5);
}
