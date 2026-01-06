// Copyright (c) HoneyDrunk Studios. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.

namespace HoneyDrunk.Data.Abstractions.Tenancy;

/// <summary>
/// Provides access to the current tenant context for persistence operations.
/// This is the primary abstraction for resolving tenant identity within data operations.
/// </summary>
public interface ITenantAccessor
{
    /// <summary>
    /// Gets the current tenant identifier from the operation context.
    /// </summary>
    /// <returns>The current tenant identifier, or an empty <see cref="TenantId"/> if no tenant is set.</returns>
    TenantId GetCurrentTenantId();
}
