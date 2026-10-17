// Copyright (c) HoneyDrunk Studios. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.

namespace HoneyDrunk.Data.EntityFramework.Registration;

/// <summary>
/// Configuration options for Entity Framework Core provider.
/// </summary>
public sealed class EfDataOptions
{
    /// <summary>
    /// Gets or sets a value indicating whether to enable query tagging with correlation information.
    /// </summary>
    public bool EnableCorrelationInterceptor { get; set; } = true;

    /// <summary>
    /// Gets or sets a value indicating whether to register health contributors automatically.
    /// </summary>
    public bool RegisterHealthContributors { get; set; } = true;

    /// <summary>
    /// Gets or sets a value indicating whether to enable sensitive data logging.
    /// Should be <c>false</c> in production.
    /// </summary>
    public bool EnableSensitiveDataLogging { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether to enable detailed errors.
    /// Should be <c>false</c> in production.
    /// </summary>
    public bool EnableDetailedErrors { get; set; }
}
