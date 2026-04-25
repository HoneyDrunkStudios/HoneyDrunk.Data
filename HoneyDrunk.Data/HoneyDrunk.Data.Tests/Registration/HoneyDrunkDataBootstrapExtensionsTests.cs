// Copyright (c) HoneyDrunk Studios. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using HoneyDrunk.Data.AspNetCore.Registration;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Routing;

namespace HoneyDrunk.Data.Tests.Registration;

/// <summary>
/// Unit tests for <see cref="HoneyDrunkDataAspNetCoreExtensions"/>.
/// </summary>
public sealed class HoneyDrunkDataBootstrapExtensionsTests
{
    [Fact]
    public void MapHoneyDrunkDataVaultInvalidationWebhook_MapsInternalRoute()
    {
        var builder = WebApplication.CreateBuilder();
        var app = builder.Build();

        app.MapHoneyDrunkDataVaultInvalidationWebhook();

        var endpoints = ((IEndpointRouteBuilder)app).DataSources.SelectMany(source => source.Endpoints);
        var endpoint = Assert.Single(endpoints);
        var routeEndpoint = Assert.IsType<RouteEndpoint>(endpoint);
        Assert.Equal(HoneyDrunkDataAspNetCoreExtensions.VaultInvalidationRoute, routeEndpoint.RoutePattern.RawText);
    }
}
