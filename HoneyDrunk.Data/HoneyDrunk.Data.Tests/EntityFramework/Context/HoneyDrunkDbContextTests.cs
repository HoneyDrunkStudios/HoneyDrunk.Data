// Copyright (c) HoneyDrunk Studios. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using HoneyDrunk.Data.Testing.Factories;
using HoneyDrunk.Data.Testing.Helpers;
using HoneyDrunk.Data.Tests.TestFixtures;
using Microsoft.EntityFrameworkCore;

namespace HoneyDrunk.Data.Tests.EntityFramework.Context;

/// <summary>
/// Unit tests for <see cref="HoneyDrunk.Data.EntityFramework.Context.HoneyDrunkDbContext"/>.
/// </summary>
public sealed class HoneyDrunkDbContextTests : IAsyncDisposable
{
    private readonly SqliteTestDbContextFactory<TestDbContext> _factory;
    private readonly TestDbContext _context;

    public HoneyDrunkDbContextTests()
    {
        _factory = new SqliteTestDbContextFactory<TestDbContext>(
            options => new TestDbContext(
                options,
                TestDoubles.CreateTenantAccessor("test-tenant"),
                TestDoubles.CreateDiagnosticsContext(
                    correlationId: "corr-123",
                    operationId: "op-456")));
        _context = _factory.Create();
    }

    public async ValueTask DisposeAsync()
    {
        await _context.DisposeAsync();
        await _factory.DisposeAsync();
    }

    [Fact]
    public void Context_CanBeCreatedWithSimpleConstructor()
    {
        var options = new DbContextOptionsBuilder<TestDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        using var context = new TestDbContext(options);

        Assert.NotNull(context);
    }

    [Fact]
    public void Context_CanBeCreatedWithFullConstructor()
    {
        var options = new DbContextOptionsBuilder<TestDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        using var context = new TestDbContext(
            options,
            TestDoubles.CreateTenantAccessor("tenant-123"),
            TestDoubles.CreateDiagnosticsContext());

        Assert.NotNull(context);
    }

    [Fact]
    public void Model_IsCreatedSuccessfully()
    {
        var model = _context.Model;

        Assert.NotNull(model);
    }

    [Fact]
    public void Model_ContainsTestEntity()
    {
        var entityType = _context.Model.FindEntityType(typeof(TestEntity));

        Assert.NotNull(entityType);
    }

    [Fact]
    public async Task SaveChangesAsync_PersistsEntities()
    {
        var entity = new TestEntity { Id = Guid.NewGuid(), Name = "Test" };
        _context.TestEntities.Add(entity);

        await _context.SaveChangesAsync();

        var found = await _context.TestEntities.FindAsync(entity.Id);
        Assert.NotNull(found);
    }

    [Fact]
    public async Task ChangeTracker_TracksAddedEntities()
    {
        var entity = new TestEntity { Id = Guid.NewGuid(), Name = "Tracked" };
        _context.TestEntities.Add(entity);

        Assert.True(_context.ChangeTracker.HasChanges());

        await _context.SaveChangesAsync();

        Assert.False(_context.ChangeTracker.HasChanges());
    }
}
