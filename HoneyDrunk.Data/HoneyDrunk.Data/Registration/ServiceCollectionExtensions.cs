// Copyright (c) HoneyDrunk Studios. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using HoneyDrunk.Data.Abstractions.Diagnostics;
using HoneyDrunk.Data.Abstractions.Tenancy;
using HoneyDrunk.Data.Configuration;
using HoneyDrunk.Data.Diagnostics;
using HoneyDrunk.Data.Tenancy;
using HoneyDrunk.Kernel.Abstractions.Context;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace HoneyDrunk.Data.Registration;

/// <summary>
/// Extension methods for registering HoneyDrunk.Data services.
/// </summary>
public static class ServiceCollectionExtensions
{
    /// <summary>
    /// Adds the core HoneyDrunk.Data services to the service collection.
    /// This registers provider-agnostic data layer components.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <param name="configureOptions">Optional action to configure data options.</param>
    /// <returns>The service collection for chaining.</returns>
    public static IServiceCollection AddHoneyDrunkData(
        this IServiceCollection services,
        Action<DataOptions>? configureOptions = null)
    {
        ArgumentNullException.ThrowIfNull(services);

        var options = new DataOptions();
        configureOptions?.Invoke(options);

        services.Configure<DataOptions>(opts =>
        {
            opts.DefaultConnectionStringName = options.DefaultConnectionStringName;
            opts.EnableQueryTagging = options.EnableQueryTagging;
            opts.RequireKernelContext = options.RequireKernelContext;
            opts.ActivitySourceName = options.ActivitySourceName;
        });

        // Register tenant accessor - uses Kernel context
        services.TryAddScoped<ITenantAccessor, KernelTenantAccessor>();

        // Register diagnostics context
        services.TryAddScoped<IDataDiagnosticsContext, KernelDataDiagnosticsContext>();

        return services;
    }

    /// <summary>
    /// Validates that required Kernel services are registered in the service collection.
    /// Call this after all services are registered to ensure proper configuration.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <returns>The service collection for chaining.</returns>
    /// <exception cref="InvalidOperationException">
    /// Thrown when required services are not registered.
    /// </exception>
    public static IServiceCollection ValidateHoneyDrunkDataRegistration(this IServiceCollection services)
    {
        ArgumentNullException.ThrowIfNull(services);

        var errors = new List<string>();

        if (!services.Any(d => d.ServiceType == typeof(IOperationContextAccessor)))
        {
            errors.Add("IOperationContextAccessor from HoneyDrunk.Kernel is not registered. " +
                       "Call AddHoneyDrunkGrid() before AddHoneyDrunkData().");
        }

        if (!services.Any(d => d.ServiceType == typeof(ITenantAccessor)))
        {
            errors.Add("ITenantAccessor is not registered. Call AddHoneyDrunkData() first.");
        }

        if (!services.Any(d => d.ServiceType == typeof(IDataDiagnosticsContext)))
        {
            errors.Add("IDataDiagnosticsContext is not registered. Call AddHoneyDrunkData() first.");
        }

        if (errors.Count > 0)
        {
            throw new InvalidOperationException(
                "HoneyDrunk.Data configuration validation failed:\n" +
                string.Join("\n", errors.Select(e => $"  - {e}")));
        }

        return services;
    }
}
