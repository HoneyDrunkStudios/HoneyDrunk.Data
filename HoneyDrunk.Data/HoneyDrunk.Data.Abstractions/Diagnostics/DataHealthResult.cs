// Copyright (c) HoneyDrunk Studios. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.

namespace HoneyDrunk.Data.Abstractions.Diagnostics;

/// <summary>
/// Represents the result of a data health check.
/// </summary>
/// <param name="Status">The health status.</param>
/// <param name="Description">A human-readable description of the health state.</param>
/// <param name="Data">Optional additional diagnostic data.</param>
public sealed record DataHealthResult(
    DataHealthStatus Status,
    string? Description = null,
    IReadOnlyDictionary<string, object>? Data = null)
{
    /// <summary>
    /// Creates a healthy result.
    /// </summary>
    /// <param name="description">Optional description.</param>
    /// <returns>A healthy <see cref="DataHealthResult"/>.</returns>
    public static DataHealthResult Healthy(string? description = null) =>
        new(DataHealthStatus.Healthy, description);

    /// <summary>
    /// Creates a degraded result.
    /// </summary>
    /// <param name="description">Description of the degradation.</param>
    /// <returns>A degraded <see cref="DataHealthResult"/>.</returns>
    public static DataHealthResult Degraded(string description) =>
        new(DataHealthStatus.Degraded, description);

    /// <summary>
    /// Creates an unhealthy result.
    /// </summary>
    /// <param name="description">Description of the failure.</param>
    /// <returns>An unhealthy <see cref="DataHealthResult"/>.</returns>
    public static DataHealthResult Unhealthy(string description) =>
        new(DataHealthStatus.Unhealthy, description);
}
