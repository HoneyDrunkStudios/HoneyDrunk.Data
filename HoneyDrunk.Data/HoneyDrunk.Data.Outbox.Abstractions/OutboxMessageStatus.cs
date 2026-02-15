// Copyright (c) HoneyDrunk Studios. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.

namespace HoneyDrunk.Data.Outbox;

/// <summary>
/// Processing lifecycle of an <see cref="OutboxMessage"/>.
/// </summary>
/// <remarks>
/// <para>Transitions:</para>
/// <list type="bullet">
///   <item><description><see cref="Pending"/> → <see cref="Leased"/> (claimed by dispatcher)</description></item>
///   <item><description><see cref="Leased"/> → <see cref="Dispatched"/> (publish succeeded)</description></item>
///   <item><description><see cref="Leased"/> → <see cref="Pending"/> (retry scheduled)</description></item>
///   <item><description><see cref="Leased"/> → <see cref="DeadLetter"/> (retries exhausted)</description></item>
/// </list>
/// </remarks>
public enum OutboxMessageStatus
{
    /// <summary>Awaiting dispatch. Eligible once <see cref="OutboxMessage.NextAttemptAt"/> has passed.</summary>
    Pending = 0,

    /// <summary>Claimed by a dispatcher instance under a time-bound lease.</summary>
    Leased = 1,

    /// <summary>Successfully published to Transport.</summary>
    Dispatched = 2,

    /// <summary>Permanently failed after exhausting all retry attempts.</summary>
    DeadLetter = 3,
}
