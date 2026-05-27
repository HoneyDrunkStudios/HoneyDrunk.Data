// Copyright (c) HoneyDrunk Studios. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using HoneyDrunk.Data.Testing.Fixtures;
using HoneyDrunk.Data.Testing.Helpers;
using HoneyDrunk.Data.Tests.TestFixtures;

namespace HoneyDrunk.Data.Tests.Testing.Fixtures;

/// <summary>
/// Unit tests for <see cref="SqliteTestDatabase{TContext}"/>.
/// </summary>
public sealed class SqliteTestDatabaseTests
{
    [Fact]
    public void CreateContext_AfterDispose_ThrowsObjectDisposedException()
    {
        var database = SqliteTestDatabaseFactory.CreateInMemory(
            (Microsoft.EntityFrameworkCore.DbContextOptions<TestDbContext> options) => new TestDbContext(
                options,
                TestDoubles.CreateTenantAccessor("test-tenant"),
                TestDoubles.CreateDiagnosticsContext()));

        database.Dispose();

        Assert.Throws<ObjectDisposedException>(() => database.CreateContext());
    }

    [Fact]
    public async Task CreateContext_AfterAsyncDispose_ThrowsObjectDisposedException()
    {
        var database = SqliteTestDatabaseFactory.CreateInMemory(
            (Microsoft.EntityFrameworkCore.DbContextOptions<TestDbContext> options) => new TestDbContext(
                options,
                TestDoubles.CreateTenantAccessor("test-tenant"),
                TestDoubles.CreateDiagnosticsContext()));

        await database.DisposeAsync();

        Assert.Throws<ObjectDisposedException>(() => database.CreateContext());
    }

    [Fact]
    public void CreateContext_BeforeDispose_Succeeds()
    {
        using var database = SqliteTestDatabaseFactory.CreateInMemory(
            (Microsoft.EntityFrameworkCore.DbContextOptions<TestDbContext> options) => new TestDbContext(
                options,
                TestDoubles.CreateTenantAccessor("test-tenant"),
                TestDoubles.CreateDiagnosticsContext()));

        using var context = database.CreateContext();

        Assert.NotNull(context);
    }

    [Fact]
    public void ConnectionString_IsExposed()
    {
        using var database = SqliteTestDatabaseFactory.CreateInMemory(
            (Microsoft.EntityFrameworkCore.DbContextOptions<TestDbContext> options) => new TestDbContext(
                options,
                TestDoubles.CreateTenantAccessor("test-tenant"),
                TestDoubles.CreateDiagnosticsContext()));

        Assert.False(string.IsNullOrWhiteSpace(database.ConnectionString));
    }
}
