// Copyright (c) HoneyDrunk Studios. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.

namespace HoneyDrunk.Data.Outbox;

/// <summary>
/// Triggers a dispatch cycle for pending outbox messages.
/// </summary>
/// <remarks>
/// Typically implemented by a hosted background service that polls the outbox
/// on a configured interval. Can also be invoked manually for on-demand dispatch
/// in testing or CLI scenarios.
/// </remarks>
public interface IOutboxDispatcher
{
    /// <summary>
    /// Dispatches pending outbox messages.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A task representing the dispatch cycle.</returns>
    Task DispatchPendingAsync(CancellationToken cancellationToken = default);
}
