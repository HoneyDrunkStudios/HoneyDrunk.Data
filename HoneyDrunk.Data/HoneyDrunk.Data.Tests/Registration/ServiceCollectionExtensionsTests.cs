// Copyright (c) HoneyDrunk Studios. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using HoneyDrunk.Data.Abstractions.Diagnostics;
using HoneyDrunk.Data.Abstractions.Tenancy;
using HoneyDrunk.Data.Configuration;
using HoneyDrunk.Data.Registration;
using HoneyDrunk.Kernel.Abstractions.Context;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace HoneyDrunk.Data.Tests.Registration;

/// <summary>
/// Unit tests for <see cref="ServiceCollectionExtensions"/>.
/// </summary>
public sealed class ServiceCollectionExtensionsTests
{
    [Fact]
    public void AddHoneyDrunkData_WithNullServices_ThrowsArgumentNullException()
    {
        Assert.Throws<ArgumentNullException>(
            () => ServiceCollectionExtensions.AddHoneyDrunkData(null!));
    }

    [Fact]
    public void AddHoneyDrunkData_RegistersITenantAccessor()
    {
        var services = new ServiceCollection();

        services.AddHoneyDrunkData();

        Assert.Contains(services, d => d.ServiceType == typeof(ITenantAccessor));
    }

    [Fact]
    public void AddHoneyDrunkData_RegistersIDataDiagnosticsContext()
    {
        var services = new ServiceCollection();

        services.AddHoneyDrunkData();

        Assert.Contains(services, d => d.ServiceType == typeof(IDataDiagnosticsContext));
    }

    [Fact]
    public void AddHoneyDrunkData_RegistersDataOptions()
    {
        var services = new ServiceCollection();

        services.AddHoneyDrunkData();

        Assert.Contains(services, d => d.ServiceType == typeof(IConfigureOptions<DataOptions>));
    }

    [Fact]
    public void AddHoneyDrunkData_WithConfiguration_AppliesOptions()
    {
        var services = new ServiceCollection();
        services.AddSingleton(Substitute.For<IOperationContextAccessor>());

        services.AddHoneyDrunkData(options =>
        {
            options.DefaultConnectionStringName = "CustomConnection";
            options.EnableQueryTagging = false;
        });

        var provider = services.BuildServiceProvider();
        var optionsAccessor = provider.GetRequiredService<IOptions<DataOptions>>();

        Assert.Equal("CustomConnection", optionsAccessor.Value.DefaultConnectionStringName);
        Assert.False(optionsAccessor.Value.EnableQueryTagging);
    }

    [Fact]
    public void AddHoneyDrunkData_ReturnsSameServiceCollection()
    {
        var services = new ServiceCollection();

        var result = services.AddHoneyDrunkData();

        Assert.Same(services, result);
    }

    [Fact]
    public void AddHoneyDrunkData_DoesNotDuplicateRegistrations()
    {
        var services = new ServiceCollection();

        services.AddHoneyDrunkData();
        services.AddHoneyDrunkData();

        var tenantAccessorCount = services.Count(d => d.ServiceType == typeof(ITenantAccessor));
        Assert.Equal(1, tenantAccessorCount);
    }

    [Fact]
    public void ValidateHoneyDrunkDataRegistration_WithNullServices_ThrowsArgumentNullException()
    {
        Assert.Throws<ArgumentNullException>(
            () => ServiceCollectionExtensions.ValidateHoneyDrunkDataRegistration(null!));
    }

    [Fact]
    public void ValidateHoneyDrunkDataRegistration_WithMissingKernel_ThrowsInvalidOperationException()
    {
        var services = new ServiceCollection();
        services.AddHoneyDrunkData();

        var ex = Assert.Throws<InvalidOperationException>(
            () => services.ValidateHoneyDrunkDataRegistration());

        Assert.Contains("IOperationContextAccessor", ex.Message);
    }

    [Fact]
    public void ValidateHoneyDrunkDataRegistration_WithMissingTenantAccessor_ThrowsInvalidOperationException()
    {
        var services = new ServiceCollection();
        services.AddSingleton(Substitute.For<IOperationContextAccessor>());

        var ex = Assert.Throws<InvalidOperationException>(
            () => services.ValidateHoneyDrunkDataRegistration());

        Assert.Contains("ITenantAccessor", ex.Message);
    }

    [Fact]
    public void ValidateHoneyDrunkDataRegistration_WithAllServicesRegistered_DoesNotThrow()
    {
        var services = new ServiceCollection();
        services.AddSingleton(Substitute.For<IOperationContextAccessor>());
        services.AddHoneyDrunkData();

        services.ValidateHoneyDrunkDataRegistration();
    }

    [Fact]
    public void ValidateHoneyDrunkDataRegistration_ReturnsSameServiceCollection()
    {
        var services = new ServiceCollection();
        services.AddSingleton(Substitute.For<IOperationContextAccessor>());
        services.AddHoneyDrunkData();

        var result = services.ValidateHoneyDrunkDataRegistration();

        Assert.Same(services, result);
    }
}
