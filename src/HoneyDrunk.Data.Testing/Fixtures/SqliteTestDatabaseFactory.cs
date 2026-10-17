// Copyright (c) HoneyDrunk Studios. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using Microsoft.EntityFrameworkCore;

namespace HoneyDrunk.Data.Testing.Fixtures;

/// <summary>
/// Factory methods for SQLite test databases.
/// </summary>
public static class SqliteTestDatabaseFactory
{
    /// <summary>
    /// Creates a SQLite in-memory database with a keepalive connection.
    /// </summary>
    /// <typeparam name="TContext">The DbContext type.</typeparam>
    /// <param name="contextFactory">Factory function to create the context.</param>
    /// <param name="configureOptions">Optional additional DbContext option configuration.</param>
    /// <returns>A ready-to-use SQLite test database.</returns>
    public static SqliteTestDatabase<TContext> CreateInMemory<TContext>(
        Func<DbContextOptions<TContext>, TContext> contextFactory,
        Action<DbContextOptionsBuilder<TContext>>? configureOptions = null)
        where TContext : DbContext
    {
        ArgumentNullException.ThrowIfNull(contextFactory);

        var database = new SqliteTestDatabase<TContext>(
            contextFactory,
            SqliteTestDatabase<TContext>.InMemoryConnectionString,
            useSharedConnection: true,
            databasePath: null,
            configureOptions);

        database.OpenKeepAliveConnection();
        database.EnsureCreated();
        return database;
    }

    /// <summary>
    /// Creates a file-backed SQLite database with a keepalive connection for multi-context tests.
    /// </summary>
    /// <typeparam name="TContext">The DbContext type.</typeparam>
    /// <param name="contextFactory">Factory function to create the context.</param>
    /// <param name="configureOptions">Optional additional DbContext option configuration.</param>
    /// <returns>A ready-to-use SQLite test database.</returns>
    public static SqliteTestDatabase<TContext> CreateFileBacked<TContext>(
        Func<DbContextOptions<TContext>, TContext> contextFactory,
        Action<DbContextOptionsBuilder<TContext>>? configureOptions = null)
        where TContext : DbContext
    {
        ArgumentNullException.ThrowIfNull(contextFactory);

        var tempDirectory = Path.GetTempPath();
        var databaseFileName = $"sqlite_{Guid.NewGuid():N}.db";
        var databasePath = Path.GetFullPath(databaseFileName, tempDirectory);
        var connectionString = $"Data Source={databasePath}";

        var database = new SqliteTestDatabase<TContext>(
            contextFactory,
            connectionString,
            useSharedConnection: false,
            databasePath,
            configureOptions);

        database.OpenKeepAliveConnection();
        database.EnsureCreated();
        return database;
    }
}
