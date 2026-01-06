// Copyright (c) HoneyDrunk Studios. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using HoneyDrunk.Data.Abstractions.Diagnostics;
using HoneyDrunk.Data.Abstractions.Tenancy;
using HoneyDrunk.Kernel.Abstractions.Context;
using Microsoft.Extensions.DependencyInjection;

namespace HoneyDrunk.Data.Registration;

/// <summary>
/// Extension methods for validating HoneyDrunk.Data configuration at runtime.
/// </summary>
public static class ServiceProviderExtensions
{
    /// <summary>
    /// Validates that required HoneyDrunk.Data services are properly configured and resolvable.
    /// Call this at application startup to fail fast on configuration errors.
    /// </summary>
    /// <param name="serviceProvider">The service provider.</param>
    /// <returns>The service provider for chaining.</returns>
    /// <exception cref="InvalidOperationException">
    /// Thrown when required services cannot be resolved or are misconfigured.
    /// </exception>
    public static IServiceProvider ValidateHoneyDrunkDataConfiguration(this IServiceProvider serviceProvider)
    {
        ArgumentNullException.ThrowIfNull(serviceProvider);

        var errors = new List<string>();

        // Create a scope to resolve scoped services
        using var scope = serviceProvider.CreateScope();
        var scopedProvider = scope.ServiceProvider;

        // Validate Kernel context accessor
        try
        {
            _ = scopedProvider.GetRequiredService<IOperationContextAccessor>();
        }
        catch (InvalidOperationException)
        {
            errors.Add("IOperationContextAccessor from HoneyDrunk.Kernel is not registered or resolvable. " +
                       "Ensure AddHoneyDrunkGrid() is called before AddHoneyDrunkData().");
        }

        // Validate tenant accessor
        try
        {
            _ = scopedProvider.GetRequiredService<ITenantAccessor>();
        }
        catch (InvalidOperationException)
        {
            errors.Add("ITenantAccessor is not registered or resolvable. " +
                       "Ensure AddHoneyDrunkData() is called.");
        }

        // Validate diagnostics context
        try
        {
            _ = scopedProvider.GetRequiredService<IDataDiagnosticsContext>();
        }
        catch (InvalidOperationException)
        {
            errors.Add("IDataDiagnosticsContext is not registered or resolvable. " +
                       "Ensure AddHoneyDrunkData() is called.");
        }

        if (errors.Count > 0)
        {
            throw new InvalidOperationException(
                "HoneyDrunk.Data configuration validation failed:\n" +
                string.Join("\n", errors.Select(e => $"  - {e}")));
        }

        return serviceProvider;
    }
}
