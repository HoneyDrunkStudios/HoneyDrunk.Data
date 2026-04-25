// Copyright (c) HoneyDrunk Studios. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using HoneyDrunk.Kernel.Abstractions.Hosting;
using HoneyDrunk.Vault.Providers.AppConfiguration.Extensions;
using HoneyDrunk.Vault.Providers.AzureKeyVault.Extensions;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace HoneyDrunk.Data.Registration;

/// <summary>
/// HoneyDrunk.Data bootstrap helpers for ADR-0005/ADR-0006 configuration and secret wiring.
/// </summary>
public static class HoneyDrunkDataBootstrapExtensions
{
    /// <summary>
    /// The Data App Configuration label.
    /// </summary>
    public const string DataAppConfigurationLabel = "honeydrunk-data";

    private const string NodeIdSetting = "HONEYDRUNK_NODE_ID";

    /// <summary>
    /// Adds env-var-driven Key Vault and App Configuration services for HoneyDrunk.Data.
    /// </summary>
    /// <param name="builder">The HoneyDrunk builder.</param>
    /// <returns>The builder for chaining.</returns>
    public static IHoneyDrunkBuilder AddHoneyDrunkDataBootstrap(this IHoneyDrunkBuilder builder)
    {
        ArgumentNullException.ThrowIfNull(builder);

        EnsureDataAppConfigurationLabel(builder.Services);

        builder.AddVaultWithAzureKeyVaultBootstrap();
        builder.AddAppConfiguration();

        return builder;
    }

    private static void EnsureDataAppConfigurationLabel(IServiceCollection services)
    {
        var descriptor = services.LastOrDefault(static d => d.ServiceType == typeof(IConfiguration));
        if (descriptor?.ImplementationInstance is not IConfigurationManager configuration)
        {
            return;
        }

        if (!string.IsNullOrWhiteSpace(configuration[NodeIdSetting]))
        {
            return;
        }

        configuration.AddInMemoryCollection(
            new Dictionary<string, string?>
            {
                [NodeIdSetting] = DataAppConfigurationLabel,
            });
    }
}
