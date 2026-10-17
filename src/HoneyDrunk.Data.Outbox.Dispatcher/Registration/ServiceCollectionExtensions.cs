// Copyright (c) HoneyDrunk Studios. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace HoneyDrunk.Data.Outbox.Dispatcher.Registration;

/// <summary>
/// Extension methods for registering the outbox dispatcher background service.
/// </summary>
public static class ServiceCollectionExtensions
{
    /// <summary>
    /// Adds the outbox dispatcher as a hosted background service.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <param name="configureOptions">Optional action to configure <see cref="OutboxDispatcherOptions"/>.</param>
    /// <returns>The service collection for chaining.</returns>
    /// <remarks>
    /// <para>
    /// Prerequisites — register before calling this method:
    /// </para>
    /// <list type="bullet">
    ///   <item><description>Kernel context: <c>AddHoneyDrunkGrid()</c> + <c>AddHoneyDrunkNode()</c></description></item>
    ///   <item><description>Data layer: <c>AddHoneyDrunkData()</c></description></item>
    ///   <item><description>Outbox persistence: <c>AddHoneyDrunkDataOutbox&lt;TContext&gt;()</c></description></item>
    ///   <item><description>Transport: <c>AddHoneyDrunkTransportCore()</c> + adapter</description></item>
    /// </list>
    /// </remarks>
    /// <example>
    /// <code>
    /// services
    ///     .AddHoneyDrunkGrid(...)
    ///     .AddHoneyDrunkNode(...)
    ///     .AddHoneyDrunkData()
    ///     .AddHoneyDrunkDataEntityFramework&lt;AppDbContext&gt;(...)
    ///     .AddHoneyDrunkDataOutbox&lt;AppDbContext&gt;()
    ///     .AddHoneyDrunkTransportCore()
    ///     .AddHoneyDrunkServiceBusTransport(...)
    ///     .AddOutboxDispatcher(opts =&gt;
    ///     {
    ///         opts.PollInterval = TimeSpan.FromSeconds(10);
    ///         opts.DefaultDestination = "domain-events";
    ///     });
    /// </code>
    /// </example>
    public static IServiceCollection AddOutboxDispatcher(
        this IServiceCollection services,
        Action<OutboxDispatcherOptions>? configureOptions = null)
    {
        ArgumentNullException.ThrowIfNull(services);

        if (configureOptions is not null)
        {
            services.Configure(configureOptions);
        }
        else
        {
            services.TryAddSingleton(Microsoft.Extensions.Options.Options.Create(new OutboxDispatcherOptions()));
        }

        // Register as singleton so the same instance serves both IHostedService and IOutboxDispatcher
        services.TryAddSingleton<OutboxDispatcherService>();

        services.AddHostedService(sp => sp.GetRequiredService<OutboxDispatcherService>());
        services.TryAddSingleton<IOutboxDispatcher>(sp => sp.GetRequiredService<OutboxDispatcherService>());

        return services;
    }
}
