// Copyright (c) HoneyDrunk Studios. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using HoneyDrunk.Data.Migrations.Helpers;
using HoneyDrunk.Data.Testing.Factories;
using HoneyDrunk.Data.Testing.Helpers;
using HoneyDrunk.Data.Tests.TestFixtures;

namespace HoneyDrunk.Data.Tests.Migrations.Helpers;

/// <summary>
/// Unit tests for <see cref="MigrationRunner"/>.
/// </summary>
public sealed class MigrationRunnerTests : IAsyncDisposable
{
    private readonly SqliteTestDbContextFactory<TestDbContext> _factory;
    private readonly TestDbContext _context;

    public MigrationRunnerTests()
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
    public async Task ApplyMigrationsAsync_WithNullContext_ThrowsArgumentNullException()
    {
        await Assert.ThrowsAsync<ArgumentNullException>(
            () => MigrationRunner.ApplyMigrationsAsync<TestDbContext>(null!));
    }

    [Fact]
    public async Task GetPendingMigrationsAsync_WithNullContext_ThrowsArgumentNullException()
    {
        await Assert.ThrowsAsync<ArgumentNullException>(
            () => MigrationRunner.GetPendingMigrationsAsync<TestDbContext>(null!));
    }

    [Fact]
    public async Task GetAppliedMigrationsAsync_WithNullContext_ThrowsArgumentNullException()
    {
        await Assert.ThrowsAsync<ArgumentNullException>(
            () => MigrationRunner.GetAppliedMigrationsAsync<TestDbContext>(null!));
    }

    [Fact]
    public async Task HasPendingMigrationsAsync_WithNullContext_ThrowsArgumentNullException()
    {
        await Assert.ThrowsAsync<ArgumentNullException>(
            () => MigrationRunner.HasPendingMigrationsAsync<TestDbContext>(null!));
    }

    [Fact]
    public async Task EnsureDatabaseAsync_WithNullContext_ThrowsArgumentNullException()
    {
        await Assert.ThrowsAsync<ArgumentNullException>(
            () => MigrationRunner.EnsureDatabaseAsync<TestDbContext>(null!));
    }

    [Fact]
    public async Task EnsureDatabaseAsync_CreatesDatabase()
    {
        await MigrationRunner.EnsureDatabaseAsync(_context);

        var canConnect = await _context.Database.CanConnectAsync();
        Assert.True(canConnect);
    }

    [Fact]
    public async Task GetPendingMigrationsAsync_WithSqliteDb_ReturnsEmptyCollection()
    {
        // SQLite supports relational extensions
        var migrations = await MigrationRunner.GetPendingMigrationsAsync(_context);

        Assert.Empty(migrations);
    }

    [Fact]
    public async Task GetAppliedMigrationsAsync_WithSqliteDb_ReturnsEmptyCollection()
    {
        // SQLite supports relational extensions
        var migrations = await MigrationRunner.GetAppliedMigrationsAsync(_context);

        Assert.Empty(migrations);
    }

    [Fact]
    public async Task HasPendingMigrationsAsync_WithSqliteDb_ReturnsFalse()
    {
        // SQLite supports relational extensions
        var hasPending = await MigrationRunner.HasPendingMigrationsAsync(_context);

        Assert.False(hasPending);
    }
}
