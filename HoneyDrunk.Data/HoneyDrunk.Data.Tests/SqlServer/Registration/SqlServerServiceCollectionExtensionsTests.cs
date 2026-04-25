// Copyright (c) HoneyDrunk Studios. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using HoneyDrunk.Data.SqlServer.Registration;
using HoneyDrunk.Data.Tests.TestFixtures;
using HoneyDrunk.Vault.Abstractions;
using HoneyDrunk.Vault.Models;
using Microsoft.EntityFrameworkCore;
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
    public void AddHoneyDrunkDataSqlServer_WithMissingSecretStore_ThrowsOnResolve()
    {
        var services = new ServiceCollection();
        services.AddHoneyDrunkDataSqlServer<TestDbContext>(options =>
        {
            options.UseConnectionPurpose("Default");
        });

        var provider = services.BuildServiceProvider();

        Assert.Throws<InvalidOperationException>(
            provider.GetRequiredService<TestDbContext>);
    }

    [Fact]
    public void AddHoneyDrunkDataSqlServer_ReturnsSameServiceCollection()
    {
        var services = new ServiceCollection();
        services.AddSingleton<ISecretStore>(new RotatingSecretStore("Server=test;Database=test"));

        var result = services.AddHoneyDrunkDataSqlServer<TestDbContext>(options =>
        {
            options.UseConnectionPurpose("Default");
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
    public void AddHoneyDrunkDataAzureSql_WithMissingSecretStore_ThrowsOnResolve()
    {
        var services = new ServiceCollection();
        services.AddHoneyDrunkDataAzureSql<TestDbContext>(options =>
        {
            options.UseConnectionPurpose("Default");
        });

        var provider = services.BuildServiceProvider();

        Assert.Throws<InvalidOperationException>(
            provider.GetRequiredService<TestDbContext>);
    }

    [Fact]
    public void AddHoneyDrunkDataAzureSql_ReturnsSameServiceCollection()
    {
        var services = new ServiceCollection();
        services.AddSingleton<ISecretStore>(new RotatingSecretStore("Server=test.database.windows.net;Database=test"));

        var result = services.AddHoneyDrunkDataAzureSql<TestDbContext>(options =>
        {
            options.UseConnectionPurpose("Default");
        });

        Assert.Same(services, result);
    }

    [Fact]
    public void AddHoneyDrunkDataSqlServer_WithEfOptions_PassesOptions()
    {
        var services = new ServiceCollection();
        var efOptionsConfigured = false;
        services.AddSingleton<ISecretStore>(new RotatingSecretStore("Server=test;Database=test"));

        services.AddHoneyDrunkDataSqlServer<TestDbContext>(
            options => options.UseConnectionPurpose("Default"),
            efOptions =>
            {
                efOptionsConfigured = true;
                efOptions.EnableCorrelationInterceptor = false;
            });

        Assert.True(efOptionsConfigured);
    }

    [Fact]
    public void AddHoneyDrunkDataSqlServer_ResolvesLatestSecretPerDbContext()
    {
        var secretStore = new RotatingSecretStore(
            "Server=first;Database=test",
            "Server=second;Database=test",
            "Server=third;Database=test",
            "Server=fourth;Database=test");
        var services = new ServiceCollection();
        services.AddSingleton<ISecretStore>(secretStore);
        services.AddHoneyDrunkDataSqlServer<TestDbContext>(options => options.UseConnectionPurpose("Default"));

        var provider = services.BuildServiceProvider();

        var first = provider.GetRequiredService<TestDbContext>();
        var firstResolvedValue = secretStore.LastReturnedValue;
        var second = provider.GetRequiredService<TestDbContext>();
        var secondResolvedValue = secretStore.LastReturnedValue;

        Assert.Contains(GetServerName(firstResolvedValue), first.Database.GetConnectionString(), StringComparison.Ordinal);
        Assert.Contains(GetServerName(secondResolvedValue), second.Database.GetConnectionString(), StringComparison.Ordinal);
        Assert.All(secretStore.RequestedIdentifiers, identifier => Assert.Null(identifier.Version));
        Assert.All(secretStore.RequestedIdentifiers, identifier => Assert.Equal("Sql--DefaultConnection", identifier.Name));
    }

    private static string GetServerName(string connectionString)
    {
        var start = connectionString.IndexOf("Server=", StringComparison.Ordinal);
        if (start < 0)
        {
            return connectionString;
        }

        start += "Server=".Length;
        var end = connectionString.IndexOf(';', start);

        return end < 0 ? connectionString[start..] : connectionString[start..end];
    }

    private sealed class RotatingSecretStore : ISecretStore
    {
        private readonly Queue<string> _values;

        public RotatingSecretStore(params string[] values)
        {
            ArgumentNullException.ThrowIfNull(values);

            if (values.Length == 0)
            {
                throw new ArgumentException("At least one secret value must be provided.", nameof(values));
            }

            _values = new Queue<string>(values);
            LastReturnedValue = values[0];
        }

        public List<SecretIdentifier> RequestedIdentifiers { get; } = [];

        public string LastReturnedValue { get; private set; }

        public Task<SecretValue> GetSecretAsync(SecretIdentifier identifier, CancellationToken cancellationToken = default)
        {
            RequestedIdentifiers.Add(identifier);

            var value = _values.Count > 1 ? _values.Dequeue() : _values.Peek();
            LastReturnedValue = value;

            return Task.FromResult(new SecretValue(identifier, value, version: null));
        }

        public Task<VaultResult<SecretValue>> TryGetSecretAsync(
            SecretIdentifier identifier,
            CancellationToken cancellationToken = default)
        {
            return Task.FromResult(VaultResult.Success(new SecretValue(identifier, _values.Peek(), version: null)));
        }

        public Task<IReadOnlyList<SecretVersion>> ListSecretVersionsAsync(
            string secretName,
            CancellationToken cancellationToken = default)
        {
            return Task.FromResult<IReadOnlyList<SecretVersion>>([]);
        }
    }
}
