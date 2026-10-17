using System.Reflection;

namespace HoneyDrunk.Data.Canary;

/// <summary>
/// Invariant 3: Transport boundary isolation — Outbox packages must not reference
/// concrete transport adapters. Only the Dispatcher may reference Transport abstractions.
/// </summary>
public sealed class TransportBoundaryCanary
{
    private static readonly string[] ForbiddenAssemblyPrefixes =
    [
        "Azure.Messaging.ServiceBus",
        "RabbitMQ.Client",
        "Confluent.Kafka",
        "AWSSDK.SQS",
        "AWSSDK.SNS",
        "MassTransit",
        "NServiceBus",
        "HoneyDrunk.Transport.AzureServiceBus",
        "HoneyDrunk.Transport.StorageQueue",
        "HoneyDrunk.Transport.InMemory",
        "HoneyDrunk.Transport.RabbitMQ",
    ];

    /// <summary>
    /// HoneyDrunk.Data.Outbox.Abstractions must not reference any transport assemblies.
    /// </summary>
    [Fact]
    public void OutboxAbstractions_HasNoTransportReferences()
    {
        var assembly = typeof(Outbox.IOutboxWriter).Assembly;

        AssertNoForbiddenReferences(assembly);
        AssertNoTransportAbstractionsReference(assembly);
    }

    /// <summary>
    /// HoneyDrunk.Data.Outbox (EF persistence) must not reference any transport assemblies.
    /// </summary>
    [Fact]
    public void OutboxPersistence_HasNoTransportReferences()
    {
        var assembly = typeof(Outbox.Persistence.EfOutboxWriter<>).Assembly;

        AssertNoForbiddenReferences(assembly);
        AssertNoTransportAbstractionsReference(assembly);
    }

    /// <summary>
    /// HoneyDrunk.Data.Outbox.Dispatcher MAY reference HoneyDrunk.Transport (abstractions)
    /// but must NOT reference any concrete transport adapters.
    /// </summary>
    [Fact]
    public void OutboxDispatcher_ReferencesOnlyTransportAbstractions()
    {
        var assembly = typeof(Outbox.Dispatcher.OutboxDispatcherService).Assembly;
        var references = assembly.GetReferencedAssemblies();

        // Dispatcher is allowed to reference HoneyDrunk.Transport (core abstractions)
        var hasTransportCore = references.Any(r =>
            r.Name == "HoneyDrunk.Transport");
        Assert.True(
            hasTransportCore,
            "Dispatcher must reference HoneyDrunk.Transport for ITransportPublisher.");

        // But must NOT reference any concrete adapters
        AssertNoForbiddenReferences(assembly);
    }

    /// <summary>
    /// Outbox.Abstractions must be dependency-free from Outbox persistence (no circular ref).
    /// </summary>
    [Fact]
    public void OutboxAbstractions_DoesNotReferenceOutboxPersistence()
    {
        var abstractionsAssembly = typeof(Outbox.IOutboxWriter).Assembly;
        var references = abstractionsAssembly.GetReferencedAssemblies();

        var hasOutboxRef = references.Any(r =>
            r.Name == "HoneyDrunk.Data.Outbox");

        Assert.False(
            hasOutboxRef,
            "Outbox.Abstractions must not reference HoneyDrunk.Data.Outbox (would create circular dependency).");
    }

    /// <summary>
    /// Outbox.Abstractions must not reference Entity Framework Core.
    /// </summary>
    [Fact]
    public void OutboxAbstractions_DoesNotReferenceEntityFramework()
    {
        var assembly = typeof(Outbox.IOutboxWriter).Assembly;
        var references = assembly.GetReferencedAssemblies();

        var hasEfRef = references.Any(r =>
            r.Name is not null && r.Name.StartsWith("Microsoft.EntityFrameworkCore", StringComparison.Ordinal));

        Assert.False(
            hasEfRef,
            "Outbox.Abstractions must be EF-agnostic. It should not reference Microsoft.EntityFrameworkCore.");
    }

    /// <summary>
    /// Outbox.Abstractions must not reference Kernel (it's a standalone contracts package).
    /// </summary>
    [Fact]
    public void OutboxAbstractions_DoesNotReferenceKernel()
    {
        var assembly = typeof(Outbox.IOutboxWriter).Assembly;
        var references = assembly.GetReferencedAssemblies();

        var hasKernelRef = references.Any(r =>
            r.Name is not null && r.Name.StartsWith("HoneyDrunk.Kernel", StringComparison.Ordinal));

        Assert.False(
            hasKernelRef,
            "Outbox.Abstractions must not reference HoneyDrunk.Kernel. It's a zero-dependency contracts package.");
    }

    private static void AssertNoForbiddenReferences(Assembly assembly)
    {
        var references = assembly.GetReferencedAssemblies();

        var violations = references
            .Where(r => r.Name is not null && ForbiddenAssemblyPrefixes.Any(
                prefix => r.Name.StartsWith(prefix, StringComparison.OrdinalIgnoreCase)))
            .Select(r => r.Name!)
            .ToList();

        Assert.Empty(violations);
    }

    private static void AssertNoTransportAbstractionsReference(Assembly assembly)
    {
        var references = assembly.GetReferencedAssemblies();

        var hasTransport = references.Any(r =>
            r.Name is not null && r.Name.StartsWith("HoneyDrunk.Transport", StringComparison.Ordinal));

        Assert.False(
            hasTransport,
            $"{assembly.GetName().Name} must not reference any HoneyDrunk.Transport assembly.");
    }
}
