// Copyright (c) HoneyDrunk Studios. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using HoneyDrunk.Data.Outbox;
using HoneyDrunk.Data.Outbox.Dispatcher;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;

namespace HoneyDrunk.Data.Tests.Outbox.Dispatcher;

/// <summary>
/// Unit tests for outbox dispatcher service registration.
/// </summary>
public sealed class OutboxDispatcherRegistrationTests
{
    [Fact]
    public void AddOutboxDispatcher_WithNullServices_ThrowsArgumentNullException()
    {
        Assert.Throws<ArgumentNullException>(
            () => HoneyDrunk.Data.Outbox.Dispatcher.Registration.ServiceCollectionExtensions.AddOutboxDispatcher(null!));
    }

    [Fact]
    public void AddOutboxDispatcher_RegistersDispatcherHostedServiceAndDefaultOptions()
    {
        var services = new ServiceCollection();

        var result = HoneyDrunk.Data.Outbox.Dispatcher.Registration.ServiceCollectionExtensions.AddOutboxDispatcher(services);

        Assert.Same(services, result);
        Assert.Contains(services, descriptor => descriptor.ServiceType == typeof(OutboxDispatcherService));
        Assert.Contains(services, descriptor => descriptor.ServiceType == typeof(IHostedService));
        Assert.Contains(services, descriptor => descriptor.ServiceType == typeof(IOutboxDispatcher));

        using var provider = services.BuildServiceProvider();
        var options = provider.GetRequiredService<IOptions<OutboxDispatcherOptions>>().Value;
        Assert.Equal(TimeSpan.FromSeconds(5), options.PollInterval);
    }

    [Fact]
    public void AddOutboxDispatcher_WithConfiguration_AppliesOptions()
    {
        var services = new ServiceCollection();

        HoneyDrunk.Data.Outbox.Dispatcher.Registration.ServiceCollectionExtensions.AddOutboxDispatcher(
            services,
            options =>
        {
            options.PollInterval = TimeSpan.FromSeconds(17);
            options.BatchSize = 7;
        });

        using var provider = services.BuildServiceProvider();
        var options = provider.GetRequiredService<IOptions<OutboxDispatcherOptions>>().Value;
        Assert.Equal(TimeSpan.FromSeconds(17), options.PollInterval);
        Assert.Equal(7, options.BatchSize);
    }
}
