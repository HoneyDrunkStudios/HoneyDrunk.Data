// Copyright (c) HoneyDrunk Studios. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using HoneyDrunk.Data.Abstractions.Diagnostics;
using HoneyDrunk.Data.EntityFramework.Diagnostics;
using HoneyDrunk.Data.Testing.Factories;
using HoneyDrunk.Data.Testing.Helpers;
using HoneyDrunk.Data.Tests.TestFixtures;

namespace HoneyDrunk.Data.Tests.EntityFramework.Diagnostics;

/// <summary>
/// Unit tests for <see cref="DbContextHealthContributor{TContext}"/>.
/// </summary>
public sealed class DbContextHealthContributorTests : IAsyncDisposable
{
    private readonly SqliteTestDbContextFactory<TestDbContext> _factory;
    private readonly TestDbContext _context;

    public DbContextHealthContributorTests()
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
        Assert.Throws<ArgumentNullException>(
            () => new DbContextHealthContributor<TestDbContext>(null!));
    }

    [Fact]
    public void Name_ContainsContextTypeName()
    {
        var contributor = new DbContextHealthContributor<TestDbContext>(_context);

        Assert.Contains("TestDbContext", contributor.Name);
    }

    [Fact]
    public void Name_HasExpectedFormat()
    {
        var contributor = new DbContextHealthContributor<TestDbContext>(_context);

        Assert.Equal("DbContext:TestDbContext", contributor.Name);
    }

    [Fact]
    public async Task CheckHealthAsync_WhenCanConnect_ReturnsHealthy()
    {
        var contributor = new DbContextHealthContributor<TestDbContext>(_context);

        var result = await contributor.CheckHealthAsync();

        Assert.Equal(DataHealthStatus.Healthy, result.Status);
        Assert.Equal("Database connection successful", result.Description);
    }

    [Fact]
    public async Task CheckHealthAsync_SupportsCancellation()
    {
        var contributor = new DbContextHealthContributor<TestDbContext>(_context);
        using var cts = new CancellationTokenSource();

        var result = await contributor.CheckHealthAsync(cts.Token);

        Assert.Equal(DataHealthStatus.Healthy, result.Status);
    }
}
