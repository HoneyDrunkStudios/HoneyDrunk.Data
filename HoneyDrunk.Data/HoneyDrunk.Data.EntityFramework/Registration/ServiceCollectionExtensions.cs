// Copyright (c) HoneyDrunk Studios. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using HoneyDrunk.Data.Abstractions.Diagnostics;
using HoneyDrunk.Data.Abstractions.Transactions;
using HoneyDrunk.Data.EntityFramework.Diagnostics;
using HoneyDrunk.Data.EntityFramework.Transactions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace HoneyDrunk.Data.EntityFramework.Registration;

/// <summary>
/// Extension methods for registering Entity Framework Core data services.
/// </summary>
public static class ServiceCollectionExtensions
{
    /// <summary>
    /// Adds Entity Framework Core data services for the specified DbContext type.
    /// </summary>
    /// <typeparam name="TContext">The DbContext type.</typeparam>
    /// <param name="services">The service collection.</param>
    /// <param name="configureDbContext">Action to configure the DbContext options.</param>
    /// <param name="configureEfOptions">Optional action to configure EF data options.</param>
    /// <returns>The service collection for chaining.</returns>
    public static IServiceCollection AddHoneyDrunkDataEntityFramework<TContext>(
        this IServiceCollection services,
        Action<IServiceProvider, DbContextOptionsBuilder> configureDbContext,
        Action<EfDataOptions>? configureEfOptions = null)
        where TContext : DbContext
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(configureDbContext);

        var efOptions = new EfDataOptions();
        configureEfOptions?.Invoke(efOptions);

        // Register DbContext with pooling for better performance
        services.AddDbContextFactory<TContext>((sp, options) =>
        {
            configureDbContext(sp, options);

            if (efOptions.EnableSensitiveDataLogging)
            {
                options.EnableSensitiveDataLogging();
            }

            if (efOptions.EnableDetailedErrors)
            {
                options.EnableDetailedErrors();
            }

            if (efOptions.EnableCorrelationInterceptor)
            {
                var diagnosticsContext = sp.GetService<IDataDiagnosticsContext>();
                if (diagnosticsContext is not null)
                {
                    options.AddInterceptors(new CorrelationCommandInterceptor(diagnosticsContext));
                }
            }
        });

        // Also register DbContext for scoped injection (uses factory internally)
        services.AddDbContext<TContext>((sp, options) =>
        {
            configureDbContext(sp, options);

            if (efOptions.EnableSensitiveDataLogging)
            {
                options.EnableSensitiveDataLogging();
            }

            if (efOptions.EnableDetailedErrors)
            {
                options.EnableDetailedErrors();
            }

            if (efOptions.EnableCorrelationInterceptor)
            {
                var diagnosticsContext = sp.GetService<IDataDiagnosticsContext>();
                if (diagnosticsContext is not null)
                {
                    options.AddInterceptors(new CorrelationCommandInterceptor(diagnosticsContext));
                }
            }
        });

        // Register unit of work (scoped - uses scoped DbContext)
        services.TryAddScoped<IUnitOfWork<TContext>, EfUnitOfWork<TContext>>();
        services.TryAddScoped<IUnitOfWork>(sp => sp.GetRequiredService<IUnitOfWork<TContext>>());

        // Register unit of work factory (singleton - uses DbContextFactory for each Create())
        services.TryAddSingleton<IUnitOfWorkFactory, EfUnitOfWorkFactory<TContext>>();

        // Register health contributor
        if (efOptions.RegisterHealthContributors)
        {
            services.AddScoped<IDataHealthContributor, DbContextHealthContributor<TContext>>();
        }

        return services;
    }

    /// <summary>
    /// Adds Entity Framework Core data services for the specified DbContext type with a simple configuration.
    /// </summary>
    /// <typeparam name="TContext">The DbContext type.</typeparam>
    /// <param name="services">The service collection.</param>
    /// <param name="configureDbContext">Action to configure the DbContext options.</param>
    /// <param name="configureEfOptions">Optional action to configure EF data options.</param>
    /// <returns>The service collection for chaining.</returns>
    public static IServiceCollection AddHoneyDrunkDataEntityFramework<TContext>(
        this IServiceCollection services,
        Action<DbContextOptionsBuilder> configureDbContext,
        Action<EfDataOptions>? configureEfOptions = null)
        where TContext : DbContext
    {
        ArgumentNullException.ThrowIfNull(configureDbContext);

        return services.AddHoneyDrunkDataEntityFramework<TContext>(
            (_, options) => configureDbContext(options),
            configureEfOptions);
    }
}
