// Copyright (c) HoneyDrunk Studios. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.

namespace HoneyDrunk.Data.Configuration;

/// <summary>
/// Configuration options for the HoneyDrunk.Data layer.
/// </summary>
public sealed class DataOptions
{
    /// <summary>
    /// Gets or sets the default connection string name to use when tenant-specific resolution is not available.
    /// </summary>
    public string DefaultConnectionStringName { get; set; } = "Default";

    /// <summary>
    /// Gets or sets a value indicating whether to enable query tagging with correlation information.
    /// </summary>
    public bool EnableQueryTagging { get; set; } = true;

    /// <summary>
    /// Gets or sets a value indicating whether to validate Kernel context is available during registration.
    /// </summary>
    public bool RequireKernelContext { get; set; } = true;

    /// <summary>
    /// Gets or sets the activity source name for persistence telemetry.
    /// </summary>
    public string ActivitySourceName { get; set; } = "HoneyDrunk.Data";
}
