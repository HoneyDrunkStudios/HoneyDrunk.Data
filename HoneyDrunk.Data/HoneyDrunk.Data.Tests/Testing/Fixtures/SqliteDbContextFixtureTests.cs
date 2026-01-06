// Copyright (c) HoneyDrunk Studios. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using HoneyDrunk.Data.Testing.Fixtures;
using HoneyDrunk.Data.Testing.Helpers;
using HoneyDrunk.Data.Tests.TestFixtures;
using Microsoft.EntityFrameworkCore;

namespace HoneyDrunk.Data.Tests.Testing.Fixtures;

/// <summary>
/// Test fixture implementation for testing SqliteDbContextFixture.
/// </summary>
public sealed class TestSqliteDbContextFixture : SqliteDbContextFixture<TestDbContext>
{
    public bool SeedWasCalled { get; private set; }

    public bool ConfigureOptionsWasCalled { get; private set; }

    protected override TestDbContext CreateContext(DbContextOptions<TestDbContext> options)
    {
        return new TestDbContext(
            options,
            TestDoubles.CreateTenantAccessor("fixture-tenant"),
            TestDoubles.CreateDiagnosticsContext());
    }

    protected override Task SeedAsync(TestDbContext context, CancellationToken cancellationToken = default)
    {
        SeedWasCalled = true;
        context.TestEntities.Add(new TestEntity { Id = Guid.NewGuid(), Name = "Seeded" });
        return context.SaveChangesAsync(cancellationToken);
    }

    protected override void ConfigureOptions(DbContextOptionsBuilder<TestDbContext> optionsBuilder)
    {
        ConfigureOptionsWasCalled = true;
        base.ConfigureOptions(optionsBuilder);
    }
}

/// <summary>
/// Unit tests for SqliteDbContextFixture.
/// </summary>
public sealed class SqliteDbContextFixtureTests : IAsyncLifetime, IDisposable
{
    private TestSqliteDbContextFixture? _fixture;

    public async Task InitializeAsync()
    {
        _fixture = new TestSqliteDbContextFixture();
        await _fixture.InitializeAsync();
    }

    public async Task DisposeAsync()
    {
        if (_fixture is not null)
        {
            await _fixture.DisposeAsync();
        }
    }

    public void Dispose()
    {
        _fixture?.Dispose();
    }

    [Fact]
    public void Context_IsNotNull()
    {
        Assert.NotNull(_fixture!.Context);
    }

    [Fact]
    public void SeedAsync_WasCalled()
    {
        Assert.True(_fixture!.SeedWasCalled);
    }

    [Fact]
    public void ConfigureOptions_WasCalled()
    {
        Assert.True(_fixture!.ConfigureOptionsWasCalled);
    }

    [Fact]
    public async Task Context_ContainsSeededData()
    {
        var entity = await _fixture!.Context.TestEntities.FirstOrDefaultAsync(e => e.Name == "Seeded");

        Assert.NotNull(entity);
    }

    [Fact]
    public void Context_CanConnect()
    {
        var canConnect = _fixture!.Context.Database.CanConnect();

        Assert.True(canConnect);
    }

    [Fact]
    public async Task Context_CanAddAndQueryEntities()
    {
        var entity = new TestEntity { Id = Guid.NewGuid(), Name = "FixtureTest" };
        _fixture!.Context.TestEntities.Add(entity);
        await _fixture.Context.SaveChangesAsync();

        var found = await _fixture.Context.TestEntities.FindAsync(entity.Id);
        Assert.NotNull(found);
    }

    [Fact]
    public async Task DisposeAsync_CalledMultipleTimes_DoesNotThrow()
    {
        var localFixture = new TestSqliteDbContextFixture();
        await localFixture.InitializeAsync();

        await localFixture.DisposeAsync();
        await localFixture.DisposeAsync();
    }

    [Fact]
    public async Task Dispose_CleansUpResources()
    {
        var localFixture = new TestSqliteDbContextFixture();
        await localFixture.InitializeAsync();

        localFixture.Dispose();

        // Calling dispose again should not throw
        localFixture.Dispose();
    }
}
