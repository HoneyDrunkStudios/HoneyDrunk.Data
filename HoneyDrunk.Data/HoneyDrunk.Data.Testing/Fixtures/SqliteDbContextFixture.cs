// Copyright (c) HoneyDrunk Studios. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;

namespace HoneyDrunk.Data.Testing.Fixtures;

/// <summary>
/// xUnit fixture that provides a shared SQLite in-memory database for tests.
/// Implements IAsyncLifetime for proper async initialization and cleanup.
/// </summary>
/// <typeparam name="TContext">The DbContext type.</typeparam>
public abstract class SqliteDbContextFixture<TContext> : IAsyncLifetime, IDisposable
    where TContext : DbContext
{
    private SqliteConnection? _connection;
    private bool _disposed;

    /// <summary>
    /// Gets the current DbContext instance.
    /// </summary>
    public TContext Context { get; private set; } = null!;

    /// <inheritdoc />
    public async Task InitializeAsync()
    {
        _connection = new SqliteConnection("DataSource=:memory:");
        await _connection.OpenAsync().ConfigureAwait(false);

        var optionsBuilder = new DbContextOptionsBuilder<TContext>()
            .UseSqlite(_connection);

        ConfigureOptions(optionsBuilder);

        Context = CreateContext(optionsBuilder.Options);
        await Context.Database.EnsureCreatedAsync().ConfigureAwait(false);
        await SeedAsync(Context).ConfigureAwait(false);
    }

    /// <inheritdoc />
    public async Task DisposeAsync()
    {
        if (_disposed)
        {
            return;
        }

        if (Context is not null)
        {
            await Context.DisposeAsync().ConfigureAwait(false);
        }

        if (_connection is not null)
        {
            await _connection.DisposeAsync().ConfigureAwait(false);
            _connection = null;
        }

        _disposed = true;
    }

    /// <inheritdoc />
    public void Dispose()
    {
        Dispose(disposing: true);
        GC.SuppressFinalize(this);
    }

    /// <summary>
    /// Creates a new DbContext instance.
    /// Override this to provide custom context creation logic.
    /// </summary>
    /// <param name="options">The DbContext options.</param>
    /// <returns>A new DbContext instance.</returns>
    protected abstract TContext CreateContext(DbContextOptions<TContext> options);

    /// <summary>
    /// Seeds the database with initial data.
    /// Override this to add test data after the schema is created.
    /// </summary>
    /// <param name="context">The database context.</param>
    /// <param name="cancellationToken">A token to observe for cancellation.</param>
    /// <returns>A task representing the async operation.</returns>
    protected virtual Task SeedAsync(TContext context, CancellationToken cancellationToken = default)
    {
        return Task.CompletedTask;
    }

    /// <summary>
    /// Configures additional DbContext options.
    /// Override this to customize the options builder.
    /// </summary>
    /// <param name="optionsBuilder">The options builder.</param>
    protected virtual void ConfigureOptions(DbContextOptionsBuilder<TContext> optionsBuilder)
    {
    }

    /// <summary>
    /// Disposes managed and unmanaged resources.
    /// </summary>
    /// <param name="disposing">Whether to dispose managed resources.</param>
    protected virtual void Dispose(bool disposing)
    {
        if (_disposed)
        {
            return;
        }

        if (disposing)
        {
            Context?.Dispose();
            _connection?.Dispose();
            _connection = null;
        }

        _disposed = true;
    }
}
