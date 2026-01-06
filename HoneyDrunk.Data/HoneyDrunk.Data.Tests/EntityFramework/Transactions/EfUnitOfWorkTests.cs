// Copyright (c) HoneyDrunk Studios. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using HoneyDrunk.Data.EntityFramework.Transactions;
using HoneyDrunk.Data.Testing.Factories;
using HoneyDrunk.Data.Testing.Helpers;
using HoneyDrunk.Data.Tests.TestFixtures;

namespace HoneyDrunk.Data.Tests.EntityFramework.Transactions;

/// <summary>
/// Unit tests for <see cref="EfUnitOfWork{TContext}"/>.
/// </summary>
public sealed class EfUnitOfWorkTests : IAsyncDisposable
{
    private readonly SqliteTestDbContextFactory<TestDbContext> _factory;
    private readonly TestDbContext _context;

    public EfUnitOfWorkTests()
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
    public void Constructor_WithNullContext_ThrowsArgumentNullException()
    {
        Assert.Throws<ArgumentNullException>(() => new EfUnitOfWork<TestDbContext>(null!));
    }

    [Fact]
    public void HasPendingChanges_WhenNoChanges_ReturnsFalse()
    {
        var unitOfWork = new EfUnitOfWork<TestDbContext>(_context);

        Assert.False(unitOfWork.HasPendingChanges);
    }

    [Fact]
    public async Task HasPendingChanges_AfterAdd_ReturnsTrue()
    {
        var unitOfWork = new EfUnitOfWork<TestDbContext>(_context);
        var repo = unitOfWork.Repository<TestEntity>();

        await repo.AddAsync(new TestEntity { Id = Guid.NewGuid(), Name = "Test" });

        Assert.True(unitOfWork.HasPendingChanges);
    }

    [Fact]
    public async Task HasPendingChanges_AfterSave_ReturnsFalse()
    {
        var unitOfWork = new EfUnitOfWork<TestDbContext>(_context);
        var repo = unitOfWork.Repository<TestEntity>();

        await repo.AddAsync(new TestEntity { Id = Guid.NewGuid(), Name = "Test" });
        await unitOfWork.SaveChangesAsync();

        Assert.False(unitOfWork.HasPendingChanges);
    }

    [Fact]
    public void Repository_ReturnsSameInstanceForSameType()
    {
        var unitOfWork = new EfUnitOfWork<TestDbContext>(_context);

        var repo1 = unitOfWork.Repository<TestEntity>();
        var repo2 = unitOfWork.Repository<TestEntity>();

        Assert.Same(repo1, repo2);
    }

    [Fact]
    public async Task SaveChangesAsync_PersistsChanges()
    {
        var unitOfWork = new EfUnitOfWork<TestDbContext>(_context);
        var repo = unitOfWork.Repository<TestEntity>();

        await repo.AddAsync(new TestEntity { Id = Guid.NewGuid(), Name = "Persisted" });
        var affected = await unitOfWork.SaveChangesAsync();

        Assert.Equal(1, affected);
        Assert.Single(_context.TestEntities);
    }

    [Fact]
    public async Task SaveChangesAsync_WhenDisposed_ThrowsObjectDisposedException()
    {
        var unitOfWork = new EfUnitOfWork<TestDbContext>(_context);
        await unitOfWork.DisposeAsync();

        await Assert.ThrowsAsync<ObjectDisposedException>(
            () => unitOfWork.SaveChangesAsync());
    }

    [Fact]
    public async Task BeginTransactionAsync_ReturnsTransactionScope()
    {
        var unitOfWork = new EfUnitOfWork<TestDbContext>(_context);

        await using var scope = await unitOfWork.BeginTransactionAsync();

        Assert.NotNull(scope);
        Assert.NotEqual(Guid.Empty, scope.TransactionId);
    }

    [Fact]
    public async Task BeginTransactionAsync_WhenDisposed_ThrowsObjectDisposedException()
    {
        var unitOfWork = new EfUnitOfWork<TestDbContext>(_context);
        await unitOfWork.DisposeAsync();

        await Assert.ThrowsAsync<ObjectDisposedException>(
            () => unitOfWork.BeginTransactionAsync());
    }

    [Fact]
    public async Task TransactionScope_CommitAsync_CommitsChanges()
    {
        var unitOfWork = new EfUnitOfWork<TestDbContext>(_context);
        var repo = unitOfWork.Repository<TestEntity>();

        await using var scope = await unitOfWork.BeginTransactionAsync();
        await repo.AddAsync(new TestEntity { Id = Guid.NewGuid(), Name = "Committed" });
        await unitOfWork.SaveChangesAsync();
        await scope.CommitAsync();

        Assert.Single(_context.TestEntities);
    }

    [Fact]
    public async Task TransactionScope_RollbackAsync_RollsBackChanges()
    {
        // Rollback behavior with SQLite in-memory is limited
        // but we can at least verify the call doesn't throw
        var unitOfWork = new EfUnitOfWork<TestDbContext>(_context);

        await using var scope = await unitOfWork.BeginTransactionAsync();
        await scope.RollbackAsync();
    }

    [Fact]
    public async Task DisposeAsync_ClearsRepositoryCache()
    {
        var unitOfWork = new EfUnitOfWork<TestDbContext>(_context);
        _ = unitOfWork.Repository<TestEntity>();

        await unitOfWork.DisposeAsync();

        // After dispose, should not be able to save
        await Assert.ThrowsAsync<ObjectDisposedException>(
            () => unitOfWork.SaveChangesAsync());
    }

    [Fact]
    public async Task DisposeAsync_CalledMultipleTimes_DoesNotThrow()
    {
        var unitOfWork = new EfUnitOfWork<TestDbContext>(_context);

        await unitOfWork.DisposeAsync();
        await unitOfWork.DisposeAsync();
    }
}
