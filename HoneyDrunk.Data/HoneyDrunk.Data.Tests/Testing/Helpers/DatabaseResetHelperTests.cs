// Copyright (c) HoneyDrunk Studios. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using HoneyDrunk.Data.Testing.Factories;
using HoneyDrunk.Data.Testing.Helpers;
using HoneyDrunk.Data.Tests.TestFixtures;

namespace HoneyDrunk.Data.Tests.Testing.Helpers;

/// <summary>
/// Unit tests for <see cref="DatabaseResetHelper"/>.
/// </summary>
public sealed class DatabaseResetHelperTests : IAsyncDisposable
{
    private readonly SqliteTestDbContextFactory<TestDbContext> _factory;
    private readonly TestDbContext _context;

    public DatabaseResetHelperTests()
    {
        _factory = new SqliteTestDbContextFactory<TestDbContext>(
            options => new TestDbContext(
                options,
                TestDoubles.CreateTenantAccessor("test-tenant"),
                TestDoubles.CreateDiagnosticsContext()));
        _context = _factory.Create();
    }

    public async ValueTask DisposeAsync()
    {
        await _context.DisposeAsync();
        await _factory.DisposeAsync();
    }

    [Fact]
    public async Task ClearDataAsync_WithNullContext_ThrowsArgumentNullException()
    {
        await Assert.ThrowsAsync<ArgumentNullException>(
            () => DatabaseResetHelper.ClearDataAsync<TestDbContext>(null!));
    }

    [Fact]
    public async Task ClearDataAsync_RemovesAllData()
    {
        _context.TestEntities.AddRange(
            new TestEntity { Id = Guid.NewGuid(), Name = "Entity1" },
            new TestEntity { Id = Guid.NewGuid(), Name = "Entity2" });
        await _context.SaveChangesAsync();

        await DatabaseResetHelper.ClearDataAsync(_context);

        Assert.Empty(_context.TestEntities);
    }

    [Fact]
    public async Task ClearDataAsync_WithEmptyDatabase_DoesNotThrow()
    {
        await DatabaseResetHelper.ClearDataAsync(_context);

        Assert.Empty(_context.TestEntities);
    }

    [Fact]
    public async Task ResetDatabaseAsync_WithNullContext_ThrowsArgumentNullException()
    {
        await Assert.ThrowsAsync<ArgumentNullException>(
            () => DatabaseResetHelper.ResetDatabaseAsync<TestDbContext>(null!));
    }

    [Fact]
    public async Task ResetDatabaseAsync_RecreatesDatabase()
    {
        _context.TestEntities.Add(new TestEntity { Id = Guid.NewGuid(), Name = "ToDelete" });
        await _context.SaveChangesAsync();

        await DatabaseResetHelper.ResetDatabaseAsync(_context);

        Assert.Empty(_context.TestEntities);
    }

    [Fact]
    public void DetachAllEntities_WithNullContext_ThrowsArgumentNullException()
    {
        Assert.Throws<ArgumentNullException>(
            () => DatabaseResetHelper.DetachAllEntities<TestDbContext>(null!));
    }

    [Fact]
    public async Task DetachAllEntities_ClearsChangeTracker()
    {
        var entity = new TestEntity { Id = Guid.NewGuid(), Name = "Tracked" };
        _context.TestEntities.Add(entity);
        await _context.SaveChangesAsync();

        Assert.False(_context.ChangeTracker.HasChanges());
        entity.Name = "Modified";
        _context.Update(entity);
        Assert.True(_context.ChangeTracker.HasChanges());

        DatabaseResetHelper.DetachAllEntities(_context);

        Assert.False(_context.ChangeTracker.HasChanges());
    }

    [Fact]
    public void DetachAllEntities_WithNoTrackedEntities_DoesNotThrow()
    {
        DatabaseResetHelper.DetachAllEntities(_context);
    }
}
