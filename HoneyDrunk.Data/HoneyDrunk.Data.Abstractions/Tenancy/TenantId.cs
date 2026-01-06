// Copyright (c) HoneyDrunk Studios. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.

namespace HoneyDrunk.Data.Abstractions.Tenancy;

/// <summary>
/// Represents a strongly-typed tenant identifier within the Grid.
/// </summary>
/// <param name="Value">The underlying tenant identifier value.</param>
public readonly record struct TenantId(string Value)
{
    /// <summary>
    /// Gets a value indicating whether this tenant identifier is empty or unset.
    /// </summary>
    public bool IsEmpty => string.IsNullOrWhiteSpace(Value);

    /// <summary>
    /// Implicitly converts a <see cref="TenantId"/> to a string.
    /// </summary>
    /// <param name="tenantId">The tenant identifier to convert.</param>
    public static implicit operator string(TenantId tenantId) => tenantId.Value;

    /// <summary>
    /// Creates a <see cref="TenantId"/> from a string value.
    /// </summary>
    /// <param name="value">The string value to convert.</param>
    /// <returns>A new <see cref="TenantId"/> instance.</returns>
    public static TenantId FromString(string value) => new(value);

    /// <summary>
    /// Returns the string representation of the tenant identifier.
    /// </summary>
    /// <returns>The tenant identifier value.</returns>
    public override string ToString() => Value ?? string.Empty;
}
