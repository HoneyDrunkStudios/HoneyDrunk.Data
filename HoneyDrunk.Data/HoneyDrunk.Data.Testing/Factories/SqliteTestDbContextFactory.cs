// Copyright (c) HoneyDrunk Studios. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using HoneyDrunk.Data.Testing.Fixtures;
using Microsoft.EntityFrameworkCore;

namespace HoneyDrunk.Data.Testing.Factories;

/// <summary>
/// Factory for creating SQLite in-memory DbContext instances for testing.
/// </summary>
/// <typeparam name="TContext">The DbContext type.</typeparam>
public sealed class SqliteTestDbContextFactory<TContext> : IAsyncDisposable
    where TContext : DbContext
{
    private readonly Func<DbContextOptions<TContext>, TContext> _contextFactory;
    private SqliteTestDatabase<TContext>? _database;

    /// <summary>
    /// Initializes a new instance of the <see cref="SqliteTestDbContextFactory{TContext}"/> class.
    /// </summary>
    /// <param name="contextFactory">Factory function to create the context.</param>
    public SqliteTestDbContextFactory(Func<DbContextOptions<TContext>, TContext> contextFactory)
    {
        ArgumentNullException.ThrowIfNull(contextFactory);
        _contextFactory = contextFactory;
    }

    /// <summary>
    /// Creates a new DbContext instance with an in-memory SQLite database.
    /// The database schema is automatically created.
    /// </summary>
    /// <returns>A configured DbContext instance.</returns>
    public TContext Create()
    {
        var database = SqliteTestDatabaseFactory.CreateInMemory(_contextFactory);
        ReplaceDatabase(database);
        return database.CreateContext();
    }

    /// <summary>
    /// Creates a new DbContext instance with additional configuration.
    /// </summary>
    /// <param name="configureOptions">Action to configure DbContext options.</param>
    /// <returns>A configured DbContext instance.</returns>
    public TContext Create(Action<DbContextOptionsBuilder<TContext>> configureOptions)
    {
        ArgumentNullException.ThrowIfNull(configureOptions);

        var database = SqliteTestDatabaseFactory.CreateInMemory(_contextFactory, configureOptions);
        ReplaceDatabase(database);
        return database.CreateContext();
    }

    /// <inheritdoc />
    public async ValueTask DisposeAsync()
    {
        if (_database is not null)
        {
            await _database.DisposeAsync().ConfigureAwait(false);
            _database = null;
        }
    }

    private void ReplaceDatabase(SqliteTestDatabase<TContext> database)
    {
        _database?.Dispose();
        _database = database;
    }
}
