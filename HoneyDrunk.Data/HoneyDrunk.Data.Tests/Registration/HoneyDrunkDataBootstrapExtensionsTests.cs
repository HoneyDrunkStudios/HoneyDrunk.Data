// Copyright (c) HoneyDrunk Studios. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using HoneyDrunk.Data.Registration;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;

namespace HoneyDrunk.Data.Tests.Registration;

/// <summary>
/// Unit tests for <see cref="HoneyDrunkDataBootstrapExtensions"/>.
/// </summary>
public sealed class HoneyDrunkDataBootstrapExtensionsTests
{
    [Fact]
    public async Task MapHoneyDrunkDataVaultInvalidationWebhook_MapsInternalRoute()
    {
        var builder = WebApplication.CreateBuilder();
        var app = builder.Build();

        app.MapHoneyDrunkDataVaultInvalidationWebhook();
        await app.StartAsync();

        var endpoint = Assert.Single(app.Services.GetRequiredService<EndpointDataSource>().Endpoints);
        var routeEndpoint = Assert.IsType<RouteEndpoint>(endpoint);
        Assert.Equal(HoneyDrunkDataBootstrapExtensions.VaultInvalidationRoute, routeEndpoint.RoutePattern.RawText);

        await app.StopAsync();
    }
}
