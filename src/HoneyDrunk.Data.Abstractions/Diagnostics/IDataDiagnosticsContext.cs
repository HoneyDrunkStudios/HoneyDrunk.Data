// Copyright (c) HoneyDrunk Studios. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.

namespace HoneyDrunk.Data.Abstractions.Diagnostics;

/// <summary>
/// Provides diagnostic context for persistence operations.
/// Used to enrich telemetry with correlation information from the Grid.
/// </summary>
public interface IDataDiagnosticsContext
{
    /// <summary>
    /// Gets the current correlation identifier for tracing purposes.
    /// </summary>
    string? CorrelationId { get; }

    /// <summary>
    /// Gets the current operation identifier.
    /// </summary>
    string? OperationId { get; }

    /// <summary>
    /// Gets the node identifier where this operation originated.
    /// </summary>
    string? NodeId { get; }

    /// <summary>
    /// Gets additional diagnostic tags to attach to database operations.
    /// </summary>
    IReadOnlyDictionary<string, string> Tags { get; }
}
