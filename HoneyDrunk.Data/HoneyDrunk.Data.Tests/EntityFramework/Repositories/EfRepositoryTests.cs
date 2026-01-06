// Copyright (c) HoneyDrunk Studios. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using HoneyDrunk.Data.EntityFramework.Repositories;
using HoneyDrunk.Data.Testing.Factories;
using HoneyDrunk.Data.Testing.Helpers;
using HoneyDrunk.Data.Tests.TestFixtures;

namespace HoneyDrunk.Data.Tests.EntityFramework.Repositories;

/// <summary>
/// Unit tests for <see cref="EfRepository{TEntity,TContext}"/>.
/// </summary>
public sealed class EfRepositoryTests : IAsyncDisposable
{
    private readonly SqliteTestDbContextFactory<TestDbContext> _factory;
    private readonly TestDbContext _context;
    private readonly EfRepository<TestEntity, TestDbContext> _repository;

    public EfRepositoryTests()
    {
        _factory = new SqliteTestDbContextFactory<TestDbContext>(
            options => new TestDbContext(
                options,
                TestDoubles.CreateTenantAccessor("test-tenant"),
                TestDoubles.CreateDiagnosticsContext()));
        _context = _factory.Create();
        _repository = new EfRepository<TestEntity, TestDbContext>(_context);
    }

    public async ValueTask DisposeAsync()
    {
        await _context.DisposeAsync();
        await _factory.DisposeAsync();
    }

    [Fact]
    public void Constructor_WithNullContext_ThrowsArgumentNullException()
    {
        Assert.Throws<ArgumentNullException>(() => new EfRepository<TestEntity, TestDbContext>(null!));
    }

    [Fact]
    public async Task AddAsync_AddsEntityToContext()
    {
        var entity = new TestEntity { Id = Guid.NewGuid(), Name = "Test" };

        await _repository.AddAsync(entity);
        await _context.SaveChangesAsync();

        Assert.Single(_context.TestEntities);
    }

    [Fact]
    public async Task AddRangeAsync_AddsMultipleEntities()
    {
        var entities = new[]
        {
            new TestEntity { Id = Guid.NewGuid(), Name = "Test1" },
            new TestEntity { Id = Guid.NewGuid(), Name = "Test2" },
        };

        await _repository.AddRangeAsync(entities);
        await _context.SaveChangesAsync();

        Assert.Equal(2, _context.TestEntities.Count());
    }

    [Fact]
    public async Task FindByIdAsync_WhenEntityExists_ReturnsEntity()
    {
        var id = Guid.NewGuid();
        var entity = new TestEntity { Id = id, Name = "FindMe" };
        _context.TestEntities.Add(entity);
        await _context.SaveChangesAsync();
        _context.ChangeTracker.Clear();

        var result = await _repository.FindByIdAsync(id);

        Assert.NotNull(result);
        Assert.Equal("FindMe", result.Name);
    }

    [Fact]
    public async Task FindByIdAsync_WhenEntityDoesNotExist_ReturnsNull()
    {
        var result = await _repository.FindByIdAsync(Guid.NewGuid());

        Assert.Null(result);
    }

    [Fact]
    public async Task FindAsync_ReturnsMatchingEntities()
    {
        _context.TestEntities.AddRange(
            new TestEntity { Id = Guid.NewGuid(), Name = "Match", TenantId = "t1" },
            new TestEntity { Id = Guid.NewGuid(), Name = "Match", TenantId = "t2" },
            new TestEntity { Id = Guid.NewGuid(), Name = "NoMatch", TenantId = "t1" });
        await _context.SaveChangesAsync();

        var results = await _repository.FindAsync(e => e.Name == "Match");

        Assert.Equal(2, results.Count);
        Assert.All(results, e => Assert.Equal("Match", e.Name));
    }

    [Fact]
    public async Task FindAsync_WhenNoMatch_ReturnsEmptyList()
    {
        var results = await _repository.FindAsync(e => e.Name == "NonExistent");

        Assert.Empty(results);
    }

    [Fact]
    public async Task FindOneAsync_ReturnsFirstMatch()
    {
        _context.TestEntities.AddRange(
            new TestEntity { Id = Guid.NewGuid(), Name = "First" },
            new TestEntity { Id = Guid.NewGuid(), Name = "Second" });
        await _context.SaveChangesAsync();

        var result = await _repository.FindOneAsync(e => e.Name == "First");

        Assert.NotNull(result);
        Assert.Equal("First", result.Name);
    }

    [Fact]
    public async Task FindOneAsync_WhenNoMatch_ReturnsNull()
    {
        var result = await _repository.FindOneAsync(e => e.Name == "NonExistent");

        Assert.Null(result);
    }

    [Fact]
    public async Task ExistsAsync_WhenEntityExists_ReturnsTrue()
    {
        _context.TestEntities.Add(new TestEntity { Id = Guid.NewGuid(), Name = "Exists" });
        await _context.SaveChangesAsync();

        var exists = await _repository.ExistsAsync(e => e.Name == "Exists");

        Assert.True(exists);
    }

    [Fact]
    public async Task ExistsAsync_WhenEntityDoesNotExist_ReturnsFalse()
    {
        var exists = await _repository.ExistsAsync(e => e.Name == "NonExistent");

        Assert.False(exists);
    }

    [Fact]
    public async Task CountAsync_WithNoPredicate_ReturnsTotal()
    {
        _context.TestEntities.AddRange(
            new TestEntity { Id = Guid.NewGuid(), Name = "One" },
            new TestEntity { Id = Guid.NewGuid(), Name = "Two" },
            new TestEntity { Id = Guid.NewGuid(), Name = "Three" });
        await _context.SaveChangesAsync();

        var count = await _repository.CountAsync();

        Assert.Equal(3, count);
    }

    [Fact]
    public async Task CountAsync_WithPredicate_ReturnsMatchingCount()
    {
        _context.TestEntities.AddRange(
            new TestEntity { Id = Guid.NewGuid(), Name = "Match" },
            new TestEntity { Id = Guid.NewGuid(), Name = "Match" },
            new TestEntity { Id = Guid.NewGuid(), Name = "NoMatch" });
        await _context.SaveChangesAsync();

        var count = await _repository.CountAsync(e => e.Name == "Match");

        Assert.Equal(2, count);
    }

    [Fact]
    public async Task Update_MarksEntityAsModified()
    {
        var entity = new TestEntity { Id = Guid.NewGuid(), Name = "Original" };
        _context.TestEntities.Add(entity);
        await _context.SaveChangesAsync();
        _context.ChangeTracker.Clear();

        var tracked = await _context.TestEntities.FindAsync(entity.Id);
        tracked!.Name = "Updated";
        _repository.Update(tracked);
        await _context.SaveChangesAsync();
        _context.ChangeTracker.Clear();

        var result = await _context.TestEntities.FindAsync(entity.Id);
        Assert.Equal("Updated", result!.Name);
    }

    [Fact]
    public async Task Remove_MarksEntityForDeletion()
    {
        var entity = new TestEntity { Id = Guid.NewGuid(), Name = "ToDelete" };
        _context.TestEntities.Add(entity);
        await _context.SaveChangesAsync();
        _context.ChangeTracker.Clear();

        var tracked = await _context.TestEntities.FindAsync(entity.Id);
        _repository.Remove(tracked!);
        await _context.SaveChangesAsync();

        Assert.Empty(_context.TestEntities);
    }

    [Fact]
    public async Task RemoveRange_RemovesMultipleEntities()
    {
        var entities = new[]
        {
            new TestEntity { Id = Guid.NewGuid(), Name = "Delete1" },
            new TestEntity { Id = Guid.NewGuid(), Name = "Delete2" },
            new TestEntity { Id = Guid.NewGuid(), Name = "Keep" },
        };
        _context.TestEntities.AddRange(entities);
        await _context.SaveChangesAsync();
        _context.ChangeTracker.Clear();

        var toRemove = await _repository.FindAsync(e => e.Name.StartsWith("Delete"));
        _repository.RemoveRange(toRemove);
        await _context.SaveChangesAsync();

        Assert.Single(_context.TestEntities);
        Assert.Equal("Keep", _context.TestEntities.Single().Name);
    }
}
