// Copyright (c) HoneyDrunk Studios. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using HoneyDrunk.Data.Outbox;
using HoneyDrunk.Data.Outbox.Persistence;
using HoneyDrunk.Data.Tests.TestFixtures;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace HoneyDrunk.Data.Tests.Outbox.Registration;

/// <summary>
/// Unit tests for outbox persistence service registration.
/// </summary>
public sealed class OutboxRegistrationTests
{
    [Fact]
    public void AddHoneyDrunkDataOutbox_WithNullServices_ThrowsArgumentNullException()
    {
        Assert.Throws<ArgumentNullException>(
            () => HoneyDrunk.Data.Outbox.Registration.ServiceCollectionExtensions.AddHoneyDrunkDataOutbox<OutboxTestDbContext>(null!));
    }

    [Fact]
    public void AddHoneyDrunkDataOutbox_RegistersReaderWriterAndDefaultOptions()
    {
        var services = new ServiceCollection();

        var result = HoneyDrunk.Data.Outbox.Registration.ServiceCollectionExtensions.AddHoneyDrunkDataOutbox<OutboxTestDbContext>(services);

        Assert.Same(services, result);
        Assert.Contains(services, descriptor => descriptor.ServiceType == typeof(IOutboxWriter) && descriptor.ImplementationType == typeof(EfOutboxWriter<OutboxTestDbContext>));
        Assert.Contains(services, descriptor => descriptor.ServiceType == typeof(IOutboxReader) && descriptor.ImplementationType == typeof(EfOutboxReader<OutboxTestDbContext>));

        using var provider = services.BuildServiceProvider();
        var options = provider.GetRequiredService<IOptions<OutboxOptions>>().Value;
        Assert.Equal("outbox", options.Schema);
    }

    [Fact]
    public void AddHoneyDrunkDataOutbox_WithConfiguration_AppliesOptions()
    {
        var services = new ServiceCollection();

        HoneyDrunk.Data.Outbox.Registration.ServiceCollectionExtensions.AddHoneyDrunkDataOutbox<OutboxTestDbContext>(
            services,
            options => options.Schema = "custom_outbox");

        using var provider = services.BuildServiceProvider();
        var options = provider.GetRequiredService<IOptions<OutboxOptions>>().Value;
        Assert.Equal("custom_outbox", options.Schema);
    }
}
