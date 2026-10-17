// Copyright (c) HoneyDrunk Studios. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using HoneyDrunk.Data.Outbox.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace HoneyDrunk.Data.Outbox.Registration;

/// <summary>
/// Extension methods for registering outbox persistence services.
/// </summary>
public static class ServiceCollectionExtensions
{
    /// <summary>
    /// Adds EF Core-backed outbox writer and reader for the specified DbContext.
    /// </summary>
    /// <typeparam name="TContext">The application's DbContext type (must include outbox entity configuration).</typeparam>
    /// <param name="services">The service collection.</param>
    /// <param name="configureOptions">Optional action to configure <see cref="OutboxOptions"/>.</param>
    /// <returns>The service collection for chaining.</returns>
    /// <remarks>
    /// <para>
    /// The application's DbContext must call
    /// <see cref="ModelBuilderExtensions.ApplyOutboxConfiguration"/> in
    /// <c>OnModelCreating</c> to register the <see cref="OutboxMessage"/> entity mapping.
    /// </para>
    /// <para>
    /// Requires <c>AddHoneyDrunkData()</c> and Kernel context registration
    /// (<c>IOperationContextAccessor</c>) to be configured before this call.
    /// </para>
    /// </remarks>
    /// <example>
    /// <code>
    /// services
    ///     .AddHoneyDrunkData()
    ///     .AddHoneyDrunkDataEntityFramework&lt;AppDbContext&gt;(...)
    ///     .AddHoneyDrunkDataOutbox&lt;AppDbContext&gt;();
    /// </code>
    /// </example>
    public static IServiceCollection AddHoneyDrunkDataOutbox<TContext>(
        this IServiceCollection services,
        Action<OutboxOptions>? configureOptions = null)
        where TContext : DbContext
    {
        ArgumentNullException.ThrowIfNull(services);

        if (configureOptions is not null)
        {
            services.Configure(configureOptions);
        }
        else
        {
            services.TryAddSingleton(Microsoft.Extensions.Options.Options.Create(new OutboxOptions()));
        }

        services.TryAddScoped<IOutboxWriter, EfOutboxWriter<TContext>>();
        services.TryAddScoped<IOutboxReader, EfOutboxReader<TContext>>();

        return services;
    }
}
