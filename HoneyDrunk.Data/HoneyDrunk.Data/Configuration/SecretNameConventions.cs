// Copyright (c) HoneyDrunk Studios. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.

namespace HoneyDrunk.Data.Configuration;

/// <summary>
/// Centralizes HoneyDrunk.Data secret naming conventions.
/// </summary>
public static class SecretNameConventions
{
    /// <summary>
    /// Builds a provider-grouped SQL connection secret name.
    /// </summary>
    /// <param name="purpose">The connection purpose, such as Default, Migration, Tenant, or Outbox.</param>
    /// <returns>A Key Vault secret name using the <c>Sql--{Purpose}Connection</c> convention.</returns>
    public static string SqlConnection(string purpose)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(purpose);

        return $"Sql--{purpose}Connection";
    }
}
