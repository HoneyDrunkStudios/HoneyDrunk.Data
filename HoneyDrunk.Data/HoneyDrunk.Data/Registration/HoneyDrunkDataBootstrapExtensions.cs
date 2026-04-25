// Copyright (c) HoneyDrunk Studios. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using HoneyDrunk.Kernel.Abstractions.Hosting;
using HoneyDrunk.Vault.EventGrid.Extensions;
using HoneyDrunk.Vault.Providers.AppConfiguration.Extensions;
using HoneyDrunk.Vault.Providers.AzureKeyVault.Extensions;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Routing;
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

    /// <summary>
    /// The default Event Grid cache invalidation route.
    /// </summary>
    public const string VaultInvalidationRoute = "/internal/vault/invalidate";

    private const string NodeIdSetting = "HONEYDRUNK_NODE_ID";

    /// <summary>
    /// Adds env-var-driven Key Vault, App Configuration, and Event Grid invalidation services for HoneyDrunk.Data.
    /// </summary>
    /// <param name="builder">The HoneyDrunk builder.</param>
    /// <returns>The builder for chaining.</returns>
    public static IHoneyDrunkBuilder AddHoneyDrunkDataBootstrap(this IHoneyDrunkBuilder builder)
    {
        ArgumentNullException.ThrowIfNull(builder);

        EnsureDataAppConfigurationLabel(builder.Services);

        builder.AddVaultWithAzureKeyVaultBootstrap();
        builder.AddAppConfiguration();
        builder.Services.AddVaultEventGridInvalidation();

        return builder;
    }

    /// <summary>
    /// Maps the Data Vault cache invalidation webhook.
    /// </summary>
    /// <param name="endpoints">The endpoint route builder.</param>
    /// <param name="pattern">The route pattern.</param>
    /// <returns>The endpoint convention builder.</returns>
    public static IEndpointConventionBuilder MapHoneyDrunkDataVaultInvalidationWebhook(
        this IEndpointRouteBuilder endpoints,
        string pattern = VaultInvalidationRoute)
    {
        ArgumentNullException.ThrowIfNull(endpoints);

        return endpoints.MapVaultInvalidationWebhook(pattern);
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
