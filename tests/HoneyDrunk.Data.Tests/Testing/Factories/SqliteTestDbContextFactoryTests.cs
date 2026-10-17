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
    public async Task DisposeAsync_ThenContextDispose_DoesNotThrow()
    {
        // The factory's DisposeAsync tears down the SqliteTestDatabase which owns
        // the keep-alive connection. The context it created is independent and
        // should still be disposable without throwing.
        var localFactory = new SqliteTestDbContextFactory<TestDbContext>(
            options => new TestDbContext(
                options,
                TestDoubles.CreateTenantAccessor("test-tenant"),
                TestDoubles.CreateDiagnosticsContext()));
        var context = localFactory.Create();

        await localFactory.DisposeAsync();

        var exception = await Record.ExceptionAsync(() => context.DisposeAsync().AsTask());

        Assert.Null(exception);
    }

    [Fact]
    public async Task DisposeAsync_CalledMultipleTimes_DoesNotThrow()
    {
        var exception = await Record.ExceptionAsync(async () =>
        {
            await _factory.DisposeAsync();
            await _factory.DisposeAsync();
        });

        Assert.Null(exception);
    }
}
