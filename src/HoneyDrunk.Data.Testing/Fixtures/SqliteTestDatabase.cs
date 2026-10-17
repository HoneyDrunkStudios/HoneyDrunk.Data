// Copyright (c) HoneyDrunk Studios. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;

namespace HoneyDrunk.Data.Testing.Fixtures;

/// <summary>
/// Owns the lifecycle for a SQLite test database and creates configured <see cref="DbContext" /> instances.
/// </summary>
/// <typeparam name="TContext">The DbContext type.</typeparam>
public sealed class SqliteTestDatabase<TContext> : IAsyncDisposable, IDisposable
    where TContext : DbContext
{
    internal const string InMemoryConnectionString = "Data Source=:memory:";

    private readonly Func<DbContextOptions<TContext>, TContext> _contextFactory;
    private readonly Action<DbContextOptionsBuilder<TContext>>? _configureOptions;
    private readonly bool _useSharedConnection;
    private readonly string? _databasePath;
    private SqliteConnection? _keepAliveConnection;
    private bool _disposed;

    internal SqliteTestDatabase(
        Func<DbContextOptions<TContext>, TContext> contextFactory,
        string connectionString,
        bool useSharedConnection,
        string? databasePath,
        Action<DbContextOptionsBuilder<TContext>>? configureOptions)
    {
        _contextFactory = contextFactory;
        ConnectionString = connectionString;
        _useSharedConnection = useSharedConnection;
        _databasePath = databasePath;
        _configureOptions = configureOptions;
    }

    /// <summary>
    /// Gets the SQLite connection string used by this test database.
    /// </summary>
    public string ConnectionString { get; }

    /// <summary>
    /// Creates a new DbContext instance connected to this SQLite test database.
    /// </summary>
    /// <returns>A configured DbContext instance.</returns>
    public TContext CreateContext()
    {
        ThrowIfDisposed();

        var optionsBuilder = new DbContextOptionsBuilder<TContext>();

        if (_useSharedConnection)
        {
            optionsBuilder.UseSqlite(_keepAliveConnection ?? throw new ObjectDisposedException(nameof(SqliteTestDatabase<TContext>)));
        }
        else
        {
            optionsBuilder.UseSqlite(ConnectionString);
        }

        _configureOptions?.Invoke(optionsBuilder);

        return _contextFactory(optionsBuilder.Options);
    }

    /// <inheritdoc />
    public async ValueTask DisposeAsync()
    {
        if (_disposed)
        {
            return;
        }

        if (_keepAliveConnection is not null)
        {
            await _keepAliveConnection.DisposeAsync().ConfigureAwait(false);
            _keepAliveConnection = null;
        }

        TryDeleteDatabase();
        _disposed = true;
    }

    /// <inheritdoc />
    public void Dispose()
    {
        if (_disposed)
        {
            return;
        }

        _keepAliveConnection?.Dispose();
        _keepAliveConnection = null;

        TryDeleteDatabase();
        _disposed = true;
    }

    internal void OpenKeepAliveConnection()
    {
        _keepAliveConnection = new SqliteConnection(ConnectionString);
        _keepAliveConnection.Open();
    }

    internal void EnsureCreated()
    {
        using var context = CreateContext();
        context.Database.EnsureCreated();
    }

    private void ThrowIfDisposed()
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
    }

    private void TryDeleteDatabase()
    {
        if (string.IsNullOrWhiteSpace(_databasePath))
        {
            return;
        }

        try
        {
            if (File.Exists(_databasePath))
            {
                File.Delete(_databasePath);
            }
        }
        catch (IOException)
        {
            // Best-effort cleanup in CI.
        }
    }
}
