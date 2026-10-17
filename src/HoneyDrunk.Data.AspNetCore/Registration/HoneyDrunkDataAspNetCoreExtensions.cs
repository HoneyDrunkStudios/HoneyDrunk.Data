// Copyright (c) HoneyDrunk Studios. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using HoneyDrunk.Kernel.Abstractions.Hosting;
using HoneyDrunk.Vault.EventGrid.Extensions;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Routing;

namespace HoneyDrunk.Data.AspNetCore.Registration;

/// <summary>
/// ASP.NET Core integration helpers for HoneyDrunk.Data.
/// </summary>
public static class HoneyDrunkDataAspNetCoreExtensions
{
    /// <summary>
    /// The default Event Grid cache invalidation route.
    /// </summary>
    public const string VaultInvalidationRoute = "/internal/vault/invalidate";

    /// <summary>
    /// Adds ASP.NET Core Vault invalidation services for HoneyDrunk.Data.
    /// </summary>
    /// <param name="builder">The HoneyDrunk builder.</param>
    /// <returns>The builder for chaining.</returns>
    public static IHoneyDrunkBuilder AddHoneyDrunkDataAspNetCore(this IHoneyDrunkBuilder builder)
    {
        ArgumentNullException.ThrowIfNull(builder);

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
}
