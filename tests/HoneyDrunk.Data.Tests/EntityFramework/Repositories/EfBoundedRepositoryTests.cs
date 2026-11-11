// Copyright (c) HoneyDrunk Studios. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using HoneyDrunk.Data.Abstractions.Repositories;
using HoneyDrunk.Data.EntityFramework.Repositories;
using HoneyDrunk.Data.Tests.TestFixtures;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using System.Data.Common;

namespace HoneyDrunk.Data.Tests.EntityFramework.Repositories;

public sealed class EfBoundedRepositoryTests : IAsyncDisposable
{
    private static readonly string[] LegacyReadMethodNames = ["CountAsync", "ExistsAsync", "FindAsync", "FindByIdAsync", "FindOneAsync"];

    private readonly SqliteConnection _connection = new("Data Source=:memory:");
    private readonly QueryObserver _observer = new();
    private readonly TestDbContext _context;
    private readonly EfRepository<TestEntity, TestDbContext> _repository;

    public EfBoundedRepositoryTests()
    {
        _connection.Open();
        _context = new TestDbContext(new DbContextOptionsBuilder<TestDbContext>()
            .UseSqlite(_connection)
            .AddInterceptors(_observer)
            .Options);
        _context.Database.EnsureCreated();
        _repository = new EfRepository<TestEntity, TestDbContext>(_context);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task FindOrderedAsync_BoundsDatabaseReadAndOrdersTies(bool descending)
    {
        await SeedAsync();

        var rows = await _repository.FindOrderedAsync(
            row => row.TenantId == "included", row => row.Name, row => row.Id, 5, descending);

        string[] expectedNames = descending
            ? ["099", "099", "098", "098", "097"]
            : ["000", "000", "001", "001", "002"];
        Assert.Equal(expectedNames, rows.Select(row => row.Name));
        var tiedIds = rows.Take(2).Select(row => row.Id).ToArray();
        Assert.Equal(descending ? tiedIds.OrderDescending() : tiedIds.Order(), tiedIds);
        Assert.All(rows, row => Assert.Equal("included", row.TenantId));
        Assert.Equal(5, _observer.Materialized);
        Assert.Equal(5, _context.ChangeTracker.Entries<TestEntity>().Count());
        var sql = Assert.Single(_observer.Commands);
        Assert.Contains("WHERE", sql, StringComparison.Ordinal);
        Assert.Contains(descending ? "ORDER BY \"t\".\"Name\" DESC, \"t\".\"Id\" DESC" : "ORDER BY \"t\".\"Name\", \"t\".\"Id\"", sql, StringComparison.Ordinal);
        Assert.Contains("LIMIT", sql, StringComparison.Ordinal);
        Assert.Contains(5, _observer.ParameterValues);
    }

    [Fact]
    public async Task FindOrderedAsync_InsertBeforeExecutionRemainsBoundedWithoutCountRace()
    {
        await SeedAsync();
        _observer.BeforeRead = async () =>
        {
            await using var writer = new TestDbContext(new DbContextOptionsBuilder<TestDbContext>()
                .UseSqlite(_connection).Options);
            writer.Add(new TestEntity { Id = Guid.NewGuid(), Name = "999", TenantId = "included" });
            await writer.SaveChangesAsync();
        };

        var rows = await _repository.FindOrderedAsync(
            row => row.TenantId == "included", row => row.Name, row => row.Id, 5, descending: true);

        Assert.Equal("999", rows[0].Name);
        Assert.Equal(5, rows.Count);
        Assert.Equal(5, _observer.Materialized);
        Assert.Single(_observer.Commands);
        Assert.DoesNotContain("COUNT", _observer.Commands[0], StringComparison.OrdinalIgnoreCase);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public async Task FindOrderedAsync_InvalidLimitDoesNotRead(int limit)
    {
        await Assert.ThrowsAsync<ArgumentOutOfRangeException>(() => _repository.FindOrderedAsync(
            row => true, row => row.Name, row => row.Id, limit));
        Assert.Empty(_observer.Commands);
    }

    [Fact]
    public async Task FindOrderedAsync_NullArgumentsDoNotRead()
    {
        await Assert.ThrowsAsync<ArgumentNullException>(() => _repository.FindOrderedAsync<string, Guid>(
            null!, row => row.Name, row => row.Id, 1));
        await Assert.ThrowsAsync<ArgumentNullException>(() => _repository.FindOrderedAsync<string, Guid>(
            row => true, null!, row => row.Id, 1));
        await Assert.ThrowsAsync<ArgumentNullException>(() => _repository.FindOrderedAsync<string, Guid>(
            row => true, row => row.Name, null!, 1));
        Assert.Empty(_observer.Commands);
    }

    [Fact]
    public async Task FindOrderedAsync_CancelledTokenDoesNotRead()
    {
        using var cancellation = new CancellationTokenSource();
        await cancellation.CancelAsync();
        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => _repository.FindOrderedAsync(
            row => true, row => row.Name, row => row.Id, 1, cancellationToken: cancellation.Token));
        Assert.Empty(_observer.Commands);
    }

    [Fact]
    public async Task FindOrderedAsync_UntranslatableOrderDoesNotFallBackToClientEvaluation()
    {
        await Assert.ThrowsAsync<InvalidOperationException>(() => _repository.FindOrderedAsync(
            row => true, row => ReverseName(row.Name), row => row.Id, 1));
        Assert.Empty(_observer.Commands);
        Assert.Equal(0, _observer.Materialized);
    }

    [Fact]
    public async Task FindOrderedAsync_NoMatchesReturnsEmptyList()
    {
        var rows = await _repository.FindOrderedAsync(row => false, row => row.Name, row => row.Id, 1);
        Assert.Empty(rows);
        Assert.Single(_observer.Commands);
        Assert.Equal(0, _observer.Materialized);
    }

    [Fact]
    public void OptionalCapability_DoesNotAddMembersToExistingRepositoryInterfaces()
    {
        Assert.Equal(
            LegacyReadMethodNames,
            typeof(IReadOnlyRepository<TestEntity>).GetMethods().Select(method => method.Name).Order());
        Assert.IsAssignableFrom<IBoundedReadOnlyRepository<TestEntity>>(_repository);
        Assert.DoesNotContain(typeof(IBoundedReadOnlyRepository<TestEntity>), typeof(IRepository<TestEntity>).GetInterfaces());
    }

    public async ValueTask DisposeAsync()
    {
        await _context.DisposeAsync();
        await _connection.DisposeAsync();
    }

    private static string ReverseName(string name) => string.Concat(name.Reverse());

    private async Task SeedAsync()
    {
        for (var index = 199; index >= 0; index--)
        {
            // The leading digits make SQLite's textual Guid ordering agree with Guid.CompareTo.
            var id = Guid.Parse($"{index:D8}-0000-0000-0000-000000000000");
            _context.Add(new TestEntity { Id = id, Name = $"{index / 2:D3}", TenantId = "included" });
            _context.Add(new TestEntity { Id = Guid.NewGuid(), Name = $"{index / 2:D3}", TenantId = "excluded" });
        }

        await _context.SaveChangesAsync();
        _context.ChangeTracker.Clear();
        _observer.Commands.Clear();
        _observer.ParameterValues.Clear();
        _observer.Materialized = 0;
    }

    private sealed class QueryObserver : DbCommandInterceptor, IMaterializationInterceptor
    {
        public List<string> Commands { get; } = [];

        public List<object> ParameterValues { get; } = [];

        public int Materialized { get; set; }

        public Func<Task>? BeforeRead { get; set; }

        public object InitializedInstance(MaterializationInterceptionData materializationData, object entity)
        {
            Materialized++;
            return entity;
        }

        public override async ValueTask<InterceptionResult<DbDataReader>> ReaderExecutingAsync(
            DbCommand command,
            CommandEventData eventData,
            InterceptionResult<DbDataReader> result,
            CancellationToken cancellationToken = default)
        {
            Commands.Add(command.CommandText);
            ParameterValues.AddRange(command.Parameters.Cast<DbParameter>().Select(parameter => parameter.Value!));
            if (BeforeRead is { } beforeRead)
            {
                BeforeRead = null;
                await beforeRead();
            }

            return result;
        }
    }
}
