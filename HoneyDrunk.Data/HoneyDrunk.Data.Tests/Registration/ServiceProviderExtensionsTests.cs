// Copyright (c) HoneyDrunk Studios. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using HoneyDrunk.Data.Abstractions.Diagnostics;
using HoneyDrunk.Data.Abstractions.Tenancy;
using HoneyDrunk.Data.Registration;
using HoneyDrunk.Kernel.Abstractions.Context;
using Microsoft.Extensions.DependencyInjection;

namespace HoneyDrunk.Data.Tests.Registration;

/// <summary>
/// Unit tests for <see cref="ServiceProviderExtensions"/>.
/// </summary>
public sealed class ServiceProviderExtensionsTests
{
    [Fact]
    public void ValidateHoneyDrunkDataConfiguration_WithNullProvider_ThrowsArgumentNullException()
    {
        Assert.Throws<ArgumentNullException>(
            () => ServiceProviderExtensions.ValidateHoneyDrunkDataConfiguration(null!));
    }

    [Fact]
    public void ValidateHoneyDrunkDataConfiguration_WithMissingKernel_ThrowsInvalidOperationException()
    {
        var services = new ServiceCollection();
        services.AddScoped<ITenantAccessor>(_ => Substitute.For<ITenantAccessor>());
        services.AddScoped<IDataDiagnosticsContext>(_ => Substitute.For<IDataDiagnosticsContext>());
        var provider = services.BuildServiceProvider();

        var ex = Assert.Throws<InvalidOperationException>(
            () => provider.ValidateHoneyDrunkDataConfiguration());

        Assert.Contains("IOperationContextAccessor", ex.Message);
    }

    [Fact]
    public void ValidateHoneyDrunkDataConfiguration_WithMissingTenantAccessor_ThrowsInvalidOperationException()
    {
        var services = new ServiceCollection();
        services.AddSingleton(Substitute.For<IOperationContextAccessor>());
        services.AddScoped<IDataDiagnosticsContext>(_ => Substitute.For<IDataDiagnosticsContext>());
        var provider = services.BuildServiceProvider();

        var ex = Assert.Throws<InvalidOperationException>(
            () => provider.ValidateHoneyDrunkDataConfiguration());

        Assert.Contains("ITenantAccessor", ex.Message);
    }

    [Fact]
    public void ValidateHoneyDrunkDataConfiguration_WithMissingDiagnosticsContext_ThrowsInvalidOperationException()
    {
        var services = new ServiceCollection();
        services.AddSingleton(Substitute.For<IOperationContextAccessor>());
        services.AddScoped<ITenantAccessor>(_ => Substitute.For<ITenantAccessor>());
        var provider = services.BuildServiceProvider();

        var ex = Assert.Throws<InvalidOperationException>(
            () => provider.ValidateHoneyDrunkDataConfiguration());

        Assert.Contains("IDataDiagnosticsContext", ex.Message);
    }

    [Fact]
    public void ValidateHoneyDrunkDataConfiguration_WithAllServicesRegistered_DoesNotThrow()
    {
        var services = new ServiceCollection();
        services.AddSingleton(Substitute.For<IOperationContextAccessor>());
        services.AddHoneyDrunkData();
        var provider = services.BuildServiceProvider();

        provider.ValidateHoneyDrunkDataConfiguration();
    }

    [Fact]
    public void ValidateHoneyDrunkDataConfiguration_ReturnsSameProvider()
    {
        var services = new ServiceCollection();
        services.AddSingleton(Substitute.For<IOperationContextAccessor>());
        services.AddHoneyDrunkData();
        var provider = services.BuildServiceProvider();

        var result = provider.ValidateHoneyDrunkDataConfiguration();

        Assert.Same(provider, result);
    }

    [Fact]
    public void ValidateHoneyDrunkDataConfiguration_CollectsMultipleErrors()
    {
        var services = new ServiceCollection();
        var provider = services.BuildServiceProvider();

        var ex = Assert.Throws<InvalidOperationException>(
            () => provider.ValidateHoneyDrunkDataConfiguration());

        Assert.Contains("IOperationContextAccessor", ex.Message);
        Assert.Contains("ITenantAccessor", ex.Message);
        Assert.Contains("IDataDiagnosticsContext", ex.Message);
    }
}
