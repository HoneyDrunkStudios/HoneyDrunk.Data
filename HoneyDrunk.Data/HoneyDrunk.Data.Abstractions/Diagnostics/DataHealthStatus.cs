// Copyright (c) HoneyDrunk Studios. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.

namespace HoneyDrunk.Data.Abstractions.Diagnostics;

/// <summary>
/// Represents the health status of a data component.
/// </summary>
public enum DataHealthStatus
{
    /// <summary>
    /// The component is healthy and operational.
    /// </summary>
    Healthy = 0,

    /// <summary>
    /// The component is operational but experiencing degraded performance or partial failures.
    /// </summary>
    Degraded = 1,

    /// <summary>
    /// The component is not operational.
    /// </summary>
    Unhealthy = 2,
}
