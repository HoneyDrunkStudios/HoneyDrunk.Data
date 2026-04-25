// Copyright (c) HoneyDrunk Studios. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using HoneyDrunk.Data.Configuration;
using HoneyDrunk.Kernel.Abstractions.Hosting;
using HoneyDrunk.Vault.Abstractions;
using HoneyDrunk.Vault.Models;
using HoneyDrunk.Vault.Providers.AzureKeyVault.Extensions;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using Microsoft.Extensions.DependencyInjection;

namespace HoneyDrunk.Data.Migrations.Factories;

/// <summary>
/// Base class for design-time DbContext factories.
/// Inherit from this class in your migrations project to enable EF Core tooling.
/// </summary>
/// <typeparam name="TContext">The DbContext type.</typeparam>
public abstract class MigrationDbContextFactory<TContext> : IDesignTimeDbContextFactory<TContext>
    where TContext : DbContext
{
    /// <summary>
    /// Gets the migrations assembly name.
    /// Override this if your migrations are in a different assembly.
    /// </summary>
    protected virtual string? MigrationsAssembly => GetType().Assembly.GetName().Name;

    /// <summary>
    /// Gets the Key Vault secret name used for migration connections.
    /// </summary>
    protected virtual string MigrationConnectionSecretName => SecretNameConventions.SqlConnection("Migration");

    /// <summary>
    /// Creates a new DbContext instance for design-time operations.
    /// </summary>
    /// <param name="args">Command-line arguments (unused).</param>
    /// <returns>A configured DbContext instance.</returns>
    public TContext CreateDbContext(string[] args)
    {
        var optionsBuilder = new DbContextOptionsBuilder<TContext>();

        ConfigureOptions(optionsBuilder);

        return CreateContext(optionsBuilder.Options);
    }

    /// <summary>
    /// Gets the connection string for migrations.
    /// Override this only to provide an alternate <see cref="ISecretStore"/>-backed resolution path.
    /// </summary>
    /// <remarks>
    /// By default, bootstraps HoneyDrunk.Vault from <c>AZURE_KEYVAULT_URI</c> and resolves
    /// <see cref="MigrationConnectionSecretName"/> without pinning a secret version.
    /// </remarks>
    /// <returns>The connection string for migrations.</returns>
    protected virtual string GetConnectionString()
    {
        using var provider = CreateMigrationServiceProvider();
        var secretStore = provider.GetRequiredService<ISecretStore>();
        var secret = secretStore
            .GetSecretAsync(new SecretIdentifier(MigrationConnectionSecretName))
            .ConfigureAwait(false)
            .GetAwaiter()
            .GetResult();

        return secret.Value;
    }

    /// <summary>
    /// Creates the service provider used by design-time migration tooling.
    /// </summary>
    /// <returns>A service provider with Vault bootstrap services registered.</returns>
    protected virtual ServiceProvider CreateMigrationServiceProvider()
    {
        var services = new ServiceCollection();
        var builder = new MigrationHoneyDrunkBuilder(services);

        builder.AddVaultWithAzureKeyVaultBootstrap();

        return services.BuildServiceProvider();
    }

    /// <summary>
    /// Configures the DbContext options.
    /// Override this to customize option configuration.
    /// </summary>
    /// <param name="optionsBuilder">The options builder.</param>
    protected virtual void ConfigureOptions(DbContextOptionsBuilder<TContext> optionsBuilder)
    {
        var connectionString = GetConnectionString();

        optionsBuilder.UseSqlServer(connectionString, sqlOptions =>
        {
            if (!string.IsNullOrEmpty(MigrationsAssembly))
            {
                sqlOptions.MigrationsAssembly(MigrationsAssembly);
            }
        });
    }

    /// <summary>
    /// Creates the DbContext instance.
    /// Override this to provide custom instantiation logic.
    /// </summary>
    /// <param name="options">The configured options.</param>
    /// <returns>A new DbContext instance.</returns>
    protected abstract TContext CreateContext(DbContextOptions<TContext> options);

    private sealed class MigrationHoneyDrunkBuilder(IServiceCollection services) : IHoneyDrunkBuilder
    {
        public IServiceCollection Services { get; } = services;
    }
}
