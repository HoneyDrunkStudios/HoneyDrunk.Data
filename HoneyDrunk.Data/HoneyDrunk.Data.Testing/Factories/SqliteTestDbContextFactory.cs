// Copyright (c) HoneyDrunk Studios. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using Microsoft.Data.Sqlite;
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
    private SqliteConnection? _connection;

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
        _connection = new SqliteConnection("DataSource=:memory:");
        _connection.Open();

        var options = new DbContextOptionsBuilder<TContext>()
            .UseSqlite(_connection)
            .Options;

        var context = _contextFactory(options);
        context.Database.EnsureCreated();

        return context;
    }

    /// <summary>
    /// Creates a new DbContext instance with additional configuration.
    /// </summary>
    /// <param name="configureOptions">Action to configure DbContext options.</param>
    /// <returns>A configured DbContext instance.</returns>
    public TContext Create(Action<DbContextOptionsBuilder<TContext>> configureOptions)
    {
        ArgumentNullException.ThrowIfNull(configureOptions);

        _connection = new SqliteConnection("DataSource=:memory:");
        _connection.Open();

        var optionsBuilder = new DbContextOptionsBuilder<TContext>()
            .UseSqlite(_connection);

        configureOptions(optionsBuilder);

        var context = _contextFactory(optionsBuilder.Options);
        context.Database.EnsureCreated();

        return context;
    }

    /// <inheritdoc />
    public async ValueTask DisposeAsync()
    {
        if (_connection is not null)
        {
            await _connection.DisposeAsync().ConfigureAwait(false);
            _connection = null;
        }
    }
}
