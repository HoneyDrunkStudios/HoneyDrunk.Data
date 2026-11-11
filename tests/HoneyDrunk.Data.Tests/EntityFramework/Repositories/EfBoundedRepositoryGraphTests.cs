// Copyright (c) HoneyDrunk Studios. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using HoneyDrunk.Data.EntityFramework.Repositories;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using System.Data.Common;

namespace HoneyDrunk.Data.Tests.EntityFramework.Repositories;

public sealed class EfBoundedRepositoryGraphTests
{
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task FindOrderedAsync_BoundsRootMatchesWhilePreservingModelLoading(bool splitQuery)
    {
        await using var connection = new SqliteConnection("Data Source=:memory:");
        await connection.OpenAsync();
        var observer = new GraphObserver();
        await using var context = new GraphContext(new DbContextOptionsBuilder<GraphContext>()
            .UseSqlite(connection, sqlite => sqlite.UseQuerySplittingBehavior(
                splitQuery ? QuerySplittingBehavior.SplitQuery : QuerySplittingBehavior.SingleQuery))
            .AddInterceptors(observer).Options);
        await context.Database.EnsureCreatedAsync();
        context.AddRange(
            new RootRecord { Id = 1, Children = [new() { Id = 1 }, new() { Id = 2 }] },
            new RootRecord { Id = 2, Children = [new() { Id = 3 }] });
        await context.SaveChangesAsync();
        context.ChangeTracker.Clear();
        observer.Commands = 0;
        observer.Materialized = 0;
        var repository = new EfRepository<RootRecord, GraphContext>(context);

        var roots = await repository.FindOrderedAsync(row => true, row => row.Id, row => row.Id, 1);

        var root = Assert.Single(roots);
        Assert.Equal(1, root.Id);
        Assert.Equal(2, root.Children.Count);
        Assert.Equal(3, observer.Materialized);
        Assert.Equal(splitQuery ? 2 : 1, observer.Commands);
    }

    private sealed class GraphContext(DbContextOptions<GraphContext> options) : DbContext(options)
    {
        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<RootRecord>().HasMany(row => row.Children).WithOne();
            modelBuilder.Entity<RootRecord>().Navigation(row => row.Children).AutoInclude();
        }
    }

    private sealed class RootRecord
    {
        public int Id { get; set; }

        public List<ChildRecord> Children { get; set; } = [];
    }

    private sealed class ChildRecord
    {
        public int Id { get; set; }
    }

    private sealed class GraphObserver : DbCommandInterceptor, IMaterializationInterceptor
    {
        public int Commands { get; set; }

        public int Materialized { get; set; }

        public object InitializedInstance(MaterializationInterceptionData materializationData, object entity)
        {
            Materialized++;
            return entity;
        }

        public override ValueTask<InterceptionResult<DbDataReader>> ReaderExecutingAsync(
            DbCommand command,
            CommandEventData eventData,
            InterceptionResult<DbDataReader> result,
            CancellationToken cancellationToken = default)
        {
            Commands++;
            return ValueTask.FromResult(result);
        }
    }
}
