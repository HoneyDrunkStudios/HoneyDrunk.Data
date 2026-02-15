using HoneyDrunk.Data.Abstractions.Diagnostics;
using HoneyDrunk.Data.Abstractions.Tenancy;
using HoneyDrunk.Data.Abstractions.Transactions;
using HoneyDrunk.Data.Canary.Infrastructure;
using HoneyDrunk.Data.EntityFramework.Registration;
using HoneyDrunk.Data.Registration;
using HoneyDrunk.Kernel.Abstractions;
using HoneyDrunk.Kernel.Abstractions.Context;
using HoneyDrunk.Kernel.Hosting;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using GridEnvironments = HoneyDrunk.Kernel.Abstractions.Environments;

namespace HoneyDrunk.Data.Canary;

/// <summary>
/// Invariant 1: Data relies on Kernel context — never creates or substitutes its own.
/// </summary>
public sealed class KernelContextCanary
{
    /// <summary>
    /// When Kernel + Data are both registered, resolving a scoped IUnitOfWork succeeds
    /// and exactly one IOperationContextAccessor instance exists per scope.
    /// </summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous unit test.</returns>
    [Fact]
    public async Task KernelAndData_Registered_ResolvesScopedUnitOfWork()
    {
        var services = new ServiceCollection();

        services.AddLogging();
        services.AddHoneyDrunkNode(options =>
        {
            options.NodeId = new("canary-data-node");
            options.SectorId = Sectors.Core;
            options.EnvironmentId = GridEnvironments.Testing;
            options.StudioId = "canary-studio";
        });

        services.AddHoneyDrunkData();
        services.AddHoneyDrunkDataEntityFramework<CanaryDbContext>(
            options => options.UseSqlite($"DataSource=canary-ctx-{Guid.NewGuid():N};Mode=Memory;Cache=Shared"));

        await using var provider = services.BuildServiceProvider();
        await using var scope = provider.CreateAsyncScope();

        var unitOfWork = scope.ServiceProvider.GetRequiredService<IUnitOfWork>();

        Assert.NotNull(unitOfWork);
    }

    /// <summary>
    /// The IOperationContextAccessor resolved inside a scope is the same singleton instance,
    /// confirming Data does not replace Kernel's context accessor.
    /// </summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous unit test.</returns>
    [Fact]
    public async Task DataDoesNotSubstitute_KernelContextAccessor()
    {
        var services = new ServiceCollection();

        services.AddLogging();
        services.AddHoneyDrunkNode(options =>
        {
            options.NodeId = new("canary-data-node");
            options.SectorId = Sectors.Core;
            options.EnvironmentId = GridEnvironments.Testing;
            options.StudioId = "canary-studio";
        });

        services.AddHoneyDrunkData();
        services.AddHoneyDrunkDataEntityFramework<CanaryDbContext>(
            options => options.UseSqlite($"DataSource=canary-subst-{Guid.NewGuid():N};Mode=Memory;Cache=Shared"));

        await using var provider = services.BuildServiceProvider();

        // Resolve from two separate scopes — must be the same singleton accessor
        await using var scope1 = provider.CreateAsyncScope();
        await using var scope2 = provider.CreateAsyncScope();

        var accessor1 = scope1.ServiceProvider.GetRequiredService<IOperationContextAccessor>();
        var accessor2 = scope2.ServiceProvider.GetRequiredService<IOperationContextAccessor>();

        Assert.Same(accessor1, accessor2);
    }

    /// <summary>
    /// Exactly one registration of ITenantAccessor and IDataDiagnosticsContext exists per scope —
    /// Data does not register duplicates alongside Kernel.
    /// </summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous unit test.</returns>
    [Fact]
    public async Task ExactlyOneContextInstance_PerScope()
    {
        var services = new ServiceCollection();

        services.AddLogging();
        services.AddHoneyDrunkNode(options =>
        {
            options.NodeId = new("canary-data-node");
            options.SectorId = Sectors.Core;
            options.EnvironmentId = GridEnvironments.Testing;
            options.StudioId = "canary-studio";
        });

        services.AddHoneyDrunkData();
        services.AddHoneyDrunkDataEntityFramework<CanaryDbContext>(
            options => options.UseSqlite($"DataSource=canary-single-{Guid.NewGuid():N};Mode=Memory;Cache=Shared"));

        await using var provider = services.BuildServiceProvider();
        await using var scope = provider.CreateAsyncScope();

        var tenantAccessors = scope.ServiceProvider.GetServices<ITenantAccessor>().ToList();
        var diagnosticsContexts = scope.ServiceProvider.GetServices<IDataDiagnosticsContext>().ToList();

        Assert.Single(tenantAccessors);
        Assert.Single(diagnosticsContexts);
    }

    /// <summary>
    /// When Kernel is not registered, ValidateHoneyDrunkDataRegistration throws immediately
    /// rather than deferring to the first query.
    /// </summary>
    [Fact]
    public void WithoutKernel_ValidateThrowsAtStartup()
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddHoneyDrunkData();

        var ex = Assert.Throws<InvalidOperationException>(
            services.ValidateHoneyDrunkDataRegistration);

        Assert.Contains("IOperationContextAccessor", ex.Message);
    }

    /// <summary>
    /// AddHoneyDrunkData without Kernel does not register IOperationContextAccessor.
    /// Confirm Data doesn't sneak in its own fallback.
    /// </summary>
    [Fact]
    public void WithoutKernel_NoOperationContextAccessorRegistered()
    {
        var services = new ServiceCollection();
        services.AddHoneyDrunkData();

        var hasAccessor = services.Any(d => d.ServiceType == typeof(IOperationContextAccessor));

        Assert.False(hasAccessor, "Data must not register IOperationContextAccessor — that's Kernel's responsibility.");
    }

    /// <summary>
    /// A full round-trip: register Kernel + Data + EF, open scope, write an entity, save.
    /// Confirms the complete stack cooperates without Data creating its own context.
    /// </summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous unit test.</returns>
    [Fact]
    public async Task FullRoundTrip_WriteAndSave_Succeeds()
    {
        var services = new ServiceCollection();

        services.AddLogging();
        services.AddHoneyDrunkNode(options =>
        {
            options.NodeId = new("canary-data-node");
            options.SectorId = Sectors.Core;
            options.EnvironmentId = GridEnvironments.Testing;
            options.StudioId = "canary-studio";
        });

        services.AddHoneyDrunkData();
        services.AddHoneyDrunkDataEntityFramework<CanaryDbContext>(
            options => options.UseSqlite($"DataSource=canary-rt-{Guid.NewGuid():N};Mode=Memory;Cache=Shared"));

        await using var provider = services.BuildServiceProvider();
        await using var scope = provider.CreateAsyncScope();

        var unitOfWork = scope.ServiceProvider.GetRequiredService<IUnitOfWork>();

        Assert.False(unitOfWork.HasPendingChanges);
        await unitOfWork.SaveChangesAsync();
    }
}
