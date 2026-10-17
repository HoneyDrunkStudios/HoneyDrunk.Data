// Copyright (c) HoneyDrunk Studios. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using HoneyDrunk.Data.Abstractions.Diagnostics;
using HoneyDrunk.Data.Abstractions.Tenancy;
using HoneyDrunk.Data.Abstractions.Transactions;
using HoneyDrunk.Data.EntityFramework.Registration;
using HoneyDrunk.Data.Tests.TestFixtures;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace HoneyDrunk.Data.Tests.EntityFramework.Registration;

/// <summary>
/// Unit tests for EF <see cref="ServiceCollectionExtensions"/>.
/// </summary>
public sealed class EfServiceCollectionExtensionsTests
{
    [Fact]
    public void AddHoneyDrunkDataEntityFramework_WithNullServices_ThrowsArgumentNullException()
    {
        Assert.Throws<ArgumentNullException>(() =>
            HoneyDrunk.Data.EntityFramework.Registration.ServiceCollectionExtensions
                .AddHoneyDrunkDataEntityFramework<TestDbContext>(null!, _ => { }));
    }

    [Fact]
    public void AddHoneyDrunkDataEntityFramework_WithNullConfiguration_ThrowsArgumentNullException()
    {
        var services = new ServiceCollection();

        Assert.Throws<ArgumentNullException>(() =>
            services.AddHoneyDrunkDataEntityFramework<TestDbContext>((Action<DbContextOptionsBuilder>)null!));
    }

    [Fact]
    public void AddHoneyDrunkDataEntityFramework_RegistersUnitOfWork()
    {
        var services = new ServiceCollection();
        services.AddScoped<ITenantAccessor>(_ => Substitute.For<ITenantAccessor>());
        services.AddScoped<IDataDiagnosticsContext>(_ => Substitute.For<IDataDiagnosticsContext>());

        services.AddHoneyDrunkDataEntityFramework<TestDbContext>(
            options => options.UseInMemoryDatabase(Guid.NewGuid().ToString()));

        Assert.Contains(services, d => d.ServiceType == typeof(IUnitOfWork<TestDbContext>));
    }

    [Fact]
    public void AddHoneyDrunkDataEntityFramework_RegistersNonGenericUnitOfWork()
    {
        var services = new ServiceCollection();
        services.AddScoped<ITenantAccessor>(_ => Substitute.For<ITenantAccessor>());
        services.AddScoped<IDataDiagnosticsContext>(_ => Substitute.For<IDataDiagnosticsContext>());

        services.AddHoneyDrunkDataEntityFramework<TestDbContext>(
            options => options.UseInMemoryDatabase(Guid.NewGuid().ToString()));

        Assert.Contains(services, d => d.ServiceType == typeof(IUnitOfWork));
    }

    [Fact]
    public void AddHoneyDrunkDataEntityFramework_RegistersUnitOfWorkFactory()
    {
        var services = new ServiceCollection();

        services.AddHoneyDrunkDataEntityFramework<TestDbContext>(
            options => options.UseInMemoryDatabase(Guid.NewGuid().ToString()));

        Assert.Contains(services, d => d.ServiceType == typeof(IUnitOfWorkFactory));
    }

    [Fact]
    public void AddHoneyDrunkDataEntityFramework_RegistersHealthContributor_WhenEnabled()
    {
        var services = new ServiceCollection();

        services.AddHoneyDrunkDataEntityFramework<TestDbContext>(
            options => options.UseInMemoryDatabase(Guid.NewGuid().ToString()),
            efOptions => efOptions.RegisterHealthContributors = true);

        Assert.Contains(services, d => d.ServiceType == typeof(IDataHealthContributor));
    }

    [Fact]
    public void AddHoneyDrunkDataEntityFramework_DoesNotRegisterHealthContributor_WhenDisabled()
    {
        var services = new ServiceCollection();

        services.AddHoneyDrunkDataEntityFramework<TestDbContext>(
            options => options.UseInMemoryDatabase(Guid.NewGuid().ToString()),
            efOptions => efOptions.RegisterHealthContributors = false);

        Assert.DoesNotContain(services, d => d.ServiceType == typeof(IDataHealthContributor));
    }

    [Fact]
    public void AddHoneyDrunkDataEntityFramework_ReturnsSameServiceCollection()
    {
        var services = new ServiceCollection();

        var result = services.AddHoneyDrunkDataEntityFramework<TestDbContext>(
            options => options.UseInMemoryDatabase(Guid.NewGuid().ToString()));

        Assert.Same(services, result);
    }

    [Fact]
    public void AddHoneyDrunkDataEntityFramework_WithServiceProviderOverload_RegistersDbContext()
    {
        var services = new ServiceCollection();

        services.AddHoneyDrunkDataEntityFramework<TestDbContext>(
            (sp, options) => options.UseInMemoryDatabase(Guid.NewGuid().ToString()));

        Assert.Contains(services, d => d.ServiceType == typeof(TestDbContext));
    }

    [Fact]
    public void AddHoneyDrunkDataEntityFramework_CanResolveDbContext()
    {
        var services = new ServiceCollection();
        services.AddScoped<ITenantAccessor>(_ => Substitute.For<ITenantAccessor>());
        services.AddScoped<IDataDiagnosticsContext>(_ => Substitute.For<IDataDiagnosticsContext>());
        services.AddHoneyDrunkDataEntityFramework<TestDbContext>(
            options => options.UseInMemoryDatabase(Guid.NewGuid().ToString()));

        var provider = services.BuildServiceProvider();

        using var context = provider.GetRequiredService<TestDbContext>();
        Assert.NotNull(context);
    }

    [Fact]
    public async Task AddHoneyDrunkDataEntityFramework_CanResolveUnitOfWork()
    {
        var services = new ServiceCollection();
        services.AddScoped<ITenantAccessor>(_ => Substitute.For<ITenantAccessor>());
        services.AddScoped<IDataDiagnosticsContext>(_ => Substitute.For<IDataDiagnosticsContext>());
        services.AddHoneyDrunkDataEntityFramework<TestDbContext>(
            options => options.UseInMemoryDatabase(Guid.NewGuid().ToString()));

        var provider = services.BuildServiceProvider();
        await using var scope = provider.CreateAsyncScope();

        var unitOfWork = scope.ServiceProvider.GetRequiredService<IUnitOfWork<TestDbContext>>();
        Assert.NotNull(unitOfWork);
    }
}
