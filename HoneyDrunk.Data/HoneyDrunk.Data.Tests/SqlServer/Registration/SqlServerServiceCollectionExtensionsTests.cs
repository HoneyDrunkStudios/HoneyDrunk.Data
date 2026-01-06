// Copyright (c) HoneyDrunk Studios. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using HoneyDrunk.Data.SqlServer.Registration;
using HoneyDrunk.Data.Tests.TestFixtures;
using Microsoft.Extensions.DependencyInjection;

namespace HoneyDrunk.Data.Tests.SqlServer.Registration;

/// <summary>
/// Unit tests for SQL Server registration extensions.
/// </summary>
public sealed class SqlServerServiceCollectionExtensionsTests
{
    [Fact]
    public void AddHoneyDrunkDataSqlServer_WithNullServices_ThrowsArgumentNullException()
    {
        Assert.Throws<ArgumentNullException>(() =>
            HoneyDrunk.Data.SqlServer.Registration.ServiceCollectionExtensions
                .AddHoneyDrunkDataSqlServer<TestDbContext>(null!, _ => { }));
    }

    [Fact]
    public void AddHoneyDrunkDataSqlServer_WithNullConfiguration_ThrowsArgumentNullException()
    {
        var services = new ServiceCollection();

        Assert.Throws<ArgumentNullException>(() =>
            services.AddHoneyDrunkDataSqlServer<TestDbContext>((Action<SqlServerDataOptions>)null!));
    }

    [Fact]
    public void AddHoneyDrunkDataSqlServer_WithMissingConnectionString_ThrowsOnBuild()
    {
        var services = new ServiceCollection();
        services.AddHoneyDrunkDataSqlServer<TestDbContext>(options =>
        {
            // ConnectionString not set
        });

        var provider = services.BuildServiceProvider();

        Assert.Throws<InvalidOperationException>(() =>
            provider.GetRequiredService<TestDbContext>());
    }

    [Fact]
    public void AddHoneyDrunkDataSqlServer_ReturnsSameServiceCollection()
    {
        var services = new ServiceCollection();

        var result = services.AddHoneyDrunkDataSqlServer<TestDbContext>(options =>
        {
            options.ConnectionString = "Server=test;Database=test";
        });

        Assert.Same(services, result);
    }

    [Fact]
    public void AddHoneyDrunkDataSqlServer_WithServiceProviderOverload_WithNullServices_ThrowsArgumentNullException()
    {
        Assert.Throws<ArgumentNullException>(() =>
            HoneyDrunk.Data.SqlServer.Registration.ServiceCollectionExtensions
                .AddHoneyDrunkDataSqlServer<TestDbContext>(null!, (_, _) => { }));
    }

    [Fact]
    public void AddHoneyDrunkDataSqlServer_WithServiceProviderOverload_WithNullConfiguration_ThrowsArgumentNullException()
    {
        var services = new ServiceCollection();

        Assert.Throws<ArgumentNullException>(() =>
            services.AddHoneyDrunkDataSqlServer<TestDbContext>((Action<IServiceProvider, SqlServerDataOptions>)null!));
    }

    [Fact]
    public void AddHoneyDrunkDataAzureSql_WithNullServices_ThrowsArgumentNullException()
    {
        Assert.Throws<ArgumentNullException>(() =>
            HoneyDrunk.Data.SqlServer.Registration.ServiceCollectionExtensions
                .AddHoneyDrunkDataAzureSql<TestDbContext>(null!, _ => { }));
    }

    [Fact]
    public void AddHoneyDrunkDataAzureSql_WithNullConfiguration_ThrowsArgumentNullException()
    {
        var services = new ServiceCollection();

        Assert.Throws<ArgumentNullException>(() =>
            services.AddHoneyDrunkDataAzureSql<TestDbContext>(null!));
    }

    [Fact]
    public void AddHoneyDrunkDataAzureSql_WithMissingConnectionString_ThrowsOnBuild()
    {
        var services = new ServiceCollection();
        services.AddHoneyDrunkDataAzureSql<TestDbContext>(options =>
        {
            // ConnectionString not set
        });

        var provider = services.BuildServiceProvider();

        Assert.Throws<InvalidOperationException>(() =>
            provider.GetRequiredService<TestDbContext>());
    }

    [Fact]
    public void AddHoneyDrunkDataAzureSql_ReturnsSameServiceCollection()
    {
        var services = new ServiceCollection();

        var result = services.AddHoneyDrunkDataAzureSql<TestDbContext>(options =>
        {
            options.ConnectionString = "Server=test.database.windows.net;Database=test";
        });

        Assert.Same(services, result);
    }

    [Fact]
    public void AddHoneyDrunkDataSqlServer_WithEfOptions_PassesOptions()
    {
        var services = new ServiceCollection();
        var efOptionsConfigured = false;

        services.AddHoneyDrunkDataSqlServer<TestDbContext>(
            options => options.ConnectionString = "Server=test;Database=test",
            efOptions =>
            {
                efOptionsConfigured = true;
                efOptions.EnableCorrelationInterceptor = false;
            });

        Assert.True(efOptionsConfigured);
    }
}
