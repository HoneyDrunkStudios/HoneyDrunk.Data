// Copyright (c) HoneyDrunk Studios. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.

namespace HoneyDrunk.Data.Abstractions.Tenancy;

/// <summary>
/// Defines the strategy for resolving tenant-specific database resources.
/// Implementations determine how tenant identity maps to database connections, schemas, or databases.
/// </summary>
public interface ITenantResolutionStrategy
{
    /// <summary>
    /// Resolves the connection string for the specified tenant.
    /// </summary>
    /// <param name="tenantId">The tenant identifier.</param>
    /// <param name="cancellationToken">A token to observe for cancellation.</param>
    /// <returns>The resolved connection string for the tenant.</returns>
    ValueTask<string> ResolveConnectionStringAsync(TenantId tenantId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Resolves the database schema name for the specified tenant, if applicable.
    /// </summary>
    /// <param name="tenantId">The tenant identifier.</param>
    /// <returns>The schema name for the tenant, or <c>null</c> if schema-based isolation is not used.</returns>
    string? ResolveSchema(TenantId tenantId);
}
