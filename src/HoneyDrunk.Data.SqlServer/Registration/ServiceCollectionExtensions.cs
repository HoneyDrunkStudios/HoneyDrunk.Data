// Copyright (c) HoneyDrunk Studios. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using HoneyDrunk.Data.EntityFramework.Registration;
using HoneyDrunk.Vault.Abstractions;
using HoneyDrunk.Vault.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.Extensions.DependencyInjection;

namespace HoneyDrunk.Data.SqlServer.Registration;

/// <summary>
/// Extension methods for registering SQL Server data services.
/// </summary>
public static class ServiceCollectionExtensions
{
    /// <summary>
    /// Adds Entity Framework Core data services configured for SQL Server.
    /// </summary>
    /// <typeparam name="TContext">The DbContext type.</typeparam>
    /// <param name="services">The service collection.</param>
    /// <param name="configureSqlServer">Action to configure SQL Server options.</param>
    /// <param name="configureEfOptions">Optional action to configure EF data options.</param>
    /// <returns>The service collection for chaining.</returns>
    public static IServiceCollection AddHoneyDrunkDataSqlServer<TContext>(
        this IServiceCollection services,
        Action<SqlServerDataOptions> configureSqlServer,
        Action<EfDataOptions>? configureEfOptions = null)
        where TContext : DbContext
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(configureSqlServer);

        var sqlOptions = new SqlServerDataOptions();
        configureSqlServer(sqlOptions);

        return services.AddHoneyDrunkDataEntityFramework<TContext>(
            (sp, options) => ConfigureSqlServer(sp, options, sqlOptions),
            configureEfOptions);
    }

    /// <summary>
    /// Adds Entity Framework Core data services configured for SQL Server with service provider access.
    /// </summary>
    /// <typeparam name="TContext">The DbContext type.</typeparam>
    /// <param name="services">The service collection.</param>
    /// <param name="configureSqlServer">Action to configure SQL Server options with service provider.</param>
    /// <param name="configureEfOptions">Optional action to configure EF data options.</param>
    /// <returns>The service collection for chaining.</returns>
    public static IServiceCollection AddHoneyDrunkDataSqlServer<TContext>(
        this IServiceCollection services,
        Action<IServiceProvider, SqlServerDataOptions> configureSqlServer,
        Action<EfDataOptions>? configureEfOptions = null)
        where TContext : DbContext
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(configureSqlServer);

        return services.AddHoneyDrunkDataEntityFramework<TContext>(
            (sp, options) =>
            {
                var sqlOptions = new SqlServerDataOptions();
                configureSqlServer(sp, sqlOptions);
                ConfigureSqlServer(sp, options, sqlOptions);
            },
            configureEfOptions);
    }

    /// <summary>
    /// Adds Entity Framework Core data services configured for Azure SQL.
    /// Use this instead of <see cref="AddHoneyDrunkDataSqlServer{TContext}(IServiceCollection, Action{SqlServerDataOptions}, Action{EfDataOptions}?)"/>
    /// when targeting Azure SQL Database.
    /// </summary>
    /// <typeparam name="TContext">The DbContext type.</typeparam>
    /// <param name="services">The service collection.</param>
    /// <param name="configureSqlServer">Action to configure SQL Server options.</param>
    /// <param name="configureEfOptions">Optional action to configure EF data options.</param>
    /// <returns>The service collection for chaining.</returns>
    public static IServiceCollection AddHoneyDrunkDataAzureSql<TContext>(
        this IServiceCollection services,
        Action<SqlServerDataOptions> configureSqlServer,
        Action<EfDataOptions>? configureEfOptions = null)
        where TContext : DbContext
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(configureSqlServer);

        var sqlOptions = new SqlServerDataOptions();
        configureSqlServer(sqlOptions);

        return services.AddHoneyDrunkDataEntityFramework<TContext>(
            (sp, options) => ConfigureAzureSql(sp, options, sqlOptions),
            configureEfOptions);
    }

    private static void ConfigureSqlServer(
        IServiceProvider serviceProvider,
        DbContextOptionsBuilder options,
        SqlServerDataOptions sqlOptions)
    {
        var connectionString = ResolveConnectionString(serviceProvider, sqlOptions, "SQL Server");
        options.UseSqlServer(connectionString, providerOptions => ConfigureProviderOptions(providerOptions, sqlOptions));
    }

    private static void ConfigureAzureSql(
        IServiceProvider serviceProvider,
        DbContextOptionsBuilder options,
        SqlServerDataOptions sqlOptions)
    {
        var connectionString = ResolveConnectionString(serviceProvider, sqlOptions, "Azure SQL");
        options.UseAzureSql(connectionString, providerOptions => ConfigureProviderOptions(providerOptions, sqlOptions));
    }

    private static void ConfigureProviderOptions(
        SqlServerDbContextOptionsBuilder providerOptions,
        SqlServerDataOptions sqlOptions)
    {
        if (sqlOptions.EnableRetryOnFailure)
        {
            providerOptions.EnableRetryOnFailure(
                maxRetryCount: sqlOptions.MaxRetryCount,
                maxRetryDelay: TimeSpan.FromSeconds(sqlOptions.MaxRetryDelaySeconds),
                errorNumbersToAdd: null);
        }

        if (sqlOptions.CommandTimeoutSeconds.HasValue)
        {
            providerOptions.CommandTimeout(sqlOptions.CommandTimeoutSeconds.Value);
        }
    }

    private static void ConfigureProviderOptions(
        AzureSqlDbContextOptionsBuilder providerOptions,
        SqlServerDataOptions sqlOptions)
    {
        if (sqlOptions.EnableRetryOnFailure)
        {
            providerOptions.EnableRetryOnFailure(
                maxRetryCount: sqlOptions.MaxRetryCount,
                maxRetryDelay: TimeSpan.FromSeconds(sqlOptions.MaxRetryDelaySeconds),
                errorNumbersToAdd: null);
        }

        if (sqlOptions.CommandTimeoutSeconds.HasValue)
        {
            providerOptions.CommandTimeout(sqlOptions.CommandTimeoutSeconds.Value);
        }
    }

    private static string ResolveConnectionString(
        IServiceProvider serviceProvider,
        SqlServerDataOptions sqlOptions,
        string providerName)
    {
        if (string.IsNullOrWhiteSpace(sqlOptions.ConnectionSecretName))
        {
            throw new InvalidOperationException($"{providerName} connection secret name is required.");
        }

        var secretStore = serviceProvider.GetService<ISecretStore>()
            ?? throw new InvalidOperationException(
                "ISecretStore is required for SQL connection resolution. " +
                "Call AddHoneyDrunkDataBootstrap() or register HoneyDrunk.Vault before adding SQL Server data services.");

        var identifier = new SecretIdentifier(sqlOptions.ConnectionSecretName);
        var secret = secretStore.GetSecretAsync(identifier).ConfigureAwait(false).GetAwaiter().GetResult();

        return secret.Value;
    }
}
