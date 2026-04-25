// Copyright (c) HoneyDrunk Studios. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using HoneyDrunk.Data.Configuration;

namespace HoneyDrunk.Data.SqlServer.Registration;

/// <summary>
/// Configuration options specific to SQL Server.
/// </summary>
public sealed class SqlServerDataOptions
{
    /// <summary>
    /// Gets or sets the Key Vault secret name for the SQL Server connection string.
    /// </summary>
    public string ConnectionSecretName { get; set; } = SecretNameConventions.SqlConnection("Default");

    /// <summary>
    /// Gets or sets a value indicating whether to enable retry on failure.
    /// </summary>
    public bool EnableRetryOnFailure { get; set; } = true;

    /// <summary>
    /// Gets or sets the maximum number of retry attempts.
    /// </summary>
    public int MaxRetryCount { get; set; } = 3;

    /// <summary>
    /// Gets or sets the maximum delay between retries in seconds.
    /// </summary>
    public int MaxRetryDelaySeconds { get; set; } = 30;

    /// <summary>
    /// Gets or sets the command timeout in seconds.
    /// </summary>
    public int? CommandTimeoutSeconds { get; set; }

    /// <summary>
    /// Sets the SQL connection secret using the provider-grouped purpose convention.
    /// </summary>
    /// <param name="purpose">The connection purpose.</param>
    public void UseConnectionPurpose(string purpose)
    {
        ConnectionSecretName = SecretNameConventions.SqlConnection(purpose);
    }
}
