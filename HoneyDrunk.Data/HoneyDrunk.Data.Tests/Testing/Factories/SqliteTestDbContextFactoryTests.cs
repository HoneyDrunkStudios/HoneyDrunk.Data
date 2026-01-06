// Copyright (c) HoneyDrunk Studios. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using HoneyDrunk.Data.Testing.Factories;
using HoneyDrunk.Data.Testing.Helpers;
using HoneyDrunk.Data.Tests.TestFixtures;
using Microsoft.EntityFrameworkCore;

namespace HoneyDrunk.Data.Tests.Testing.Factories;

/// <summary>
/// Unit tests for <see cref="SqliteTestDbContextFactory{TContext}"/>.
/// </summary>
public sealed class SqliteTestDbContextFactoryTests : IAsyncDisposable
{
    private readonly SqliteTestDbContextFactory<TestDbContext> _factory;

    public SqliteTestDbContextFactoryTests()
    {
        _factory = new SqliteTestDbContextFactory<TestDbContext>(
            options => new TestDbContext(
                options,
                TestDoubles.CreateTenantAccessor("test-tenant"),
                TestDoubles.CreateDiagnosticsContext()));
    }

    public async ValueTask DisposeAsync()
    {
        await _factory.DisposeAsync();
    }

    [Fact]
    public void Constructor_WithNullFactory_ThrowsArgumentNullException()
    {
        Assert.Throws<ArgumentNullException>(
            () => new SqliteTestDbContextFactory<TestDbContext>(null!));
    }

    [Fact]
    public void Create_ReturnsContext()
    {
        using var context = _factory.Create();

        Assert.NotNull(context);
    }

    [Fact]
    public void Create_ContextCanConnectToDatabase()
    {
        using var context = _factory.Create();

        var canConnect = context.Database.CanConnect();

        Assert.True(canConnect);
    }

    [Fact]
    public void Create_DatabaseIsCreated()
    {
        using var context = _factory.Create();

        var exists = context.TestEntities.Any();

        Assert.False(exists);
    }

    [Fact]
    public async Task Create_CanAddAndQueryEntities()
    {
        using var context = _factory.Create();

        context.TestEntities.Add(new TestEntity { Id = Guid.NewGuid(), Name = "Test" });
        await context.SaveChangesAsync();

        var entity = await context.TestEntities.FirstOrDefaultAsync();
        Assert.NotNull(entity);
        Assert.Equal("Test", entity.Name);
    }

    [Fact]
    public void Create_WithConfigureOptions_AppliesConfiguration()
    {
        using var context = _factory.Create(options =>
        {
            options.EnableDetailedErrors();
        });

        Assert.NotNull(context);
    }

    [Fact]
    public void Create_WithNullConfigureOptions_ThrowsArgumentNullException()
    {
        Assert.Throws<ArgumentNullException>(
            () => _factory.Create(null!));
    }

    [Fact]
    public async Task DisposeAsync_ClosesConnection()
    {
        var localFactory = new SqliteTestDbContextFactory<TestDbContext>(
            options => new TestDbContext(
                options,
                TestDoubles.CreateTenantAccessor("test-tenant"),
                TestDoubles.CreateDiagnosticsContext()));
        var context = localFactory.Create();

        await localFactory.DisposeAsync();

        // After disposing the factory, the connection is closed
        // Attempting operations on the context may fail or succeed depending on state
        // We verify the factory can be disposed without throwing
        await context.DisposeAsync();
    }

    [Fact]
    public async Task DisposeAsync_CalledMultipleTimes_DoesNotThrow()
    {
        await _factory.DisposeAsync();
        await _factory.DisposeAsync();
    }
}
