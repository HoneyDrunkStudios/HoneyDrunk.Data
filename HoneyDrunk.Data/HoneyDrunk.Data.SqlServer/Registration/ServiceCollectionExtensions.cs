// Copyright (c) HoneyDrunk Studios. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using HoneyDrunk.Data.EntityFramework.Registration;
using Microsoft.EntityFrameworkCore;
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
            options => ConfigureSqlServer(options, sqlOptions),
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
                ConfigureSqlServer(options, sqlOptions);
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
            options => ConfigureAzureSql(options, sqlOptions),
            configureEfOptions);
    }

    private static void ConfigureSqlServer(DbContextOptionsBuilder options, SqlServerDataOptions sqlOptions)
    {
        if (string.IsNullOrEmpty(sqlOptions.ConnectionString))
        {
            throw new InvalidOperationException("SQL Server connection string is required.");
        }

        options.UseSqlServer(sqlOptions.ConnectionString, sqlServerOptions =>
        {
            if (sqlOptions.EnableRetryOnFailure)
            {
                sqlServerOptions.EnableRetryOnFailure(
                    maxRetryCount: sqlOptions.MaxRetryCount,
                    maxRetryDelay: TimeSpan.FromSeconds(sqlOptions.MaxRetryDelaySeconds),
                    errorNumbersToAdd: null);
            }

            if (sqlOptions.CommandTimeoutSeconds.HasValue)
            {
                sqlServerOptions.CommandTimeout(sqlOptions.CommandTimeoutSeconds.Value);
            }
        });
    }

    private static void ConfigureAzureSql(DbContextOptionsBuilder options, SqlServerDataOptions sqlOptions)
    {
        if (string.IsNullOrEmpty(sqlOptions.ConnectionString))
        {
            throw new InvalidOperationException("Azure SQL connection string is required.");
        }

        options.UseAzureSql(sqlOptions.ConnectionString, azureSqlOptions =>
        {
            if (sqlOptions.EnableRetryOnFailure)
            {
                azureSqlOptions.EnableRetryOnFailure(
                    maxRetryCount: sqlOptions.MaxRetryCount,
                    maxRetryDelay: TimeSpan.FromSeconds(sqlOptions.MaxRetryDelaySeconds),
                    errorNumbersToAdd: null);
            }

            if (sqlOptions.CommandTimeoutSeconds.HasValue)
            {
                azureSqlOptions.CommandTimeout(sqlOptions.CommandTimeoutSeconds.Value);
            }
        });
    }
}
