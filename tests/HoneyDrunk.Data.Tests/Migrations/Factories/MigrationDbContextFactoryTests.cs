// Copyright (c) HoneyDrunk Studios. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using HoneyDrunk.Data.Migrations.Factories;
using HoneyDrunk.Data.Tests.TestFixtures;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;

namespace HoneyDrunk.Data.Tests.Migrations.Factories;

public sealed class MigrationDbContextFactoryTests
{
    [Fact]
    public void CreateDbContext_WithDefaultOptions_PreservesSqlServer2019Compatibility()
    {
        var factory = new TestMigrationFactory();
        using var context = factory.CreateDbContext([]);

        var sql = context.TestEntities.Select(entity => Math.Min(entity.Name.Length, 10)).ToQueryString();
        var options = context.GetService<IDbContextOptions>().Extensions.OfType<RelationalOptionsExtension>().Single();

        Assert.Contains("LEN(", sql, StringComparison.Ordinal);
        Assert.DoesNotContain("LEAST(", sql, StringComparison.Ordinal);
        Assert.Equal(typeof(TestMigrationFactory).Assembly.GetName().Name, options.MigrationsAssembly);
        var connection = new SqlConnectionStringBuilder(context.Database.GetConnectionString());
        Assert.Equal("test", connection.DataSource);
        Assert.Equal("migrations", connection.InitialCatalog);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("Custom.Migrations")]
    public void CreateDbContext_WithAssemblyOverride_PreservesAssemblyAndCompatibility(string? assembly)
    {
        var factory = new AssemblyOverrideFactory(assembly);
        using var context = factory.CreateDbContext([]);

        var sql = context.TestEntities.Select(entity => Math.Min(entity.Name.Length, 10)).ToQueryString();
        var options = context.GetService<IDbContextOptions>().Extensions.OfType<RelationalOptionsExtension>().Single();

        Assert.Equal(string.IsNullOrEmpty(assembly) ? null : assembly, options.MigrationsAssembly);
        Assert.Contains("LEN(", sql, StringComparison.Ordinal);
        Assert.DoesNotContain("LEAST(", sql, StringComparison.Ordinal);
    }

    private class TestMigrationFactory : MigrationDbContextFactory<TestDbContext>
    {
        protected override string GetConnectionString() => "Server=test;Database=migrations";

        protected override TestDbContext CreateContext(DbContextOptions<TestDbContext> options) => new(options);
    }

    private sealed class AssemblyOverrideFactory(string? assembly) : TestMigrationFactory
    {
        protected override string? MigrationsAssembly => assembly;
    }
}
