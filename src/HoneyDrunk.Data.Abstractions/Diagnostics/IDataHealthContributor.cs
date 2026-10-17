// Copyright (c) HoneyDrunk Studios. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.

namespace HoneyDrunk.Data.Abstractions.Diagnostics;

/// <summary>
/// Contributes to the health status of the data layer.
/// Implementations provide specific health checks for database connectivity,
/// migration status, or other persistence-related concerns.
/// </summary>
public interface IDataHealthContributor
{
    /// <summary>
    /// Gets the name of this health contributor for diagnostic purposes.
    /// </summary>
    string Name { get; }

    /// <summary>
    /// Checks the health of this data component.
    /// </summary>
    /// <param name="cancellationToken">A token to observe for cancellation.</param>
    /// <returns>The health check result.</returns>
    ValueTask<DataHealthResult> CheckHealthAsync(CancellationToken cancellationToken = default);
}
