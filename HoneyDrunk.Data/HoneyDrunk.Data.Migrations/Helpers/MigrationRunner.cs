// Copyright (c) HoneyDrunk Studios. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using Microsoft.EntityFrameworkCore;

namespace HoneyDrunk.Data.Migrations.Helpers;

/// <summary>
/// Helper methods for applying migrations programmatically.
/// </summary>
public static class MigrationRunner
{
    /// <summary>
    /// Applies all pending migrations to the database.
    /// </summary>
    /// <typeparam name="TContext">The DbContext type.</typeparam>
    /// <param name="context">The database context.</param>
    /// <param name="cancellationToken">A token to observe for cancellation.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    public static async Task ApplyMigrationsAsync<TContext>(
        TContext context,
        CancellationToken cancellationToken = default)
        where TContext : DbContext
    {
        ArgumentNullException.ThrowIfNull(context);

        await context.Database.MigrateAsync(cancellationToken).ConfigureAwait(false);
    }

    /// <summary>
    /// Gets the list of pending migrations.
    /// </summary>
    /// <typeparam name="TContext">The DbContext type.</typeparam>
    /// <param name="context">The database context.</param>
    /// <param name="cancellationToken">A token to observe for cancellation.</param>
    /// <returns>A collection of pending migration names.</returns>
    public static async Task<IEnumerable<string>> GetPendingMigrationsAsync<TContext>(
        TContext context,
        CancellationToken cancellationToken = default)
        where TContext : DbContext
    {
        ArgumentNullException.ThrowIfNull(context);

        return await context.Database.GetPendingMigrationsAsync(cancellationToken).ConfigureAwait(false);
    }

    /// <summary>
    /// Gets the list of applied migrations.
    /// </summary>
    /// <typeparam name="TContext">The DbContext type.</typeparam>
    /// <param name="context">The database context.</param>
    /// <param name="cancellationToken">A token to observe for cancellation.</param>
    /// <returns>A collection of applied migration names.</returns>
    public static async Task<IEnumerable<string>> GetAppliedMigrationsAsync<TContext>(
        TContext context,
        CancellationToken cancellationToken = default)
        where TContext : DbContext
    {
        ArgumentNullException.ThrowIfNull(context);

        return await context.Database.GetAppliedMigrationsAsync(cancellationToken).ConfigureAwait(false);
    }

    /// <summary>
    /// Checks if the database has any pending migrations.
    /// </summary>
    /// <typeparam name="TContext">The DbContext type.</typeparam>
    /// <param name="context">The database context.</param>
    /// <param name="cancellationToken">A token to observe for cancellation.</param>
    /// <returns><c>true</c> if there are pending migrations; otherwise, <c>false</c>.</returns>
    public static async Task<bool> HasPendingMigrationsAsync<TContext>(
        TContext context,
        CancellationToken cancellationToken = default)
        where TContext : DbContext
    {
        ArgumentNullException.ThrowIfNull(context);

        var pending = await context.Database.GetPendingMigrationsAsync(cancellationToken).ConfigureAwait(false);
        return pending.Any();
    }

    /// <summary>
    /// Ensures the database is created and all migrations are applied.
    /// Use this for development/testing scenarios.
    /// </summary>
    /// <typeparam name="TContext">The DbContext type.</typeparam>
    /// <param name="context">The database context.</param>
    /// <param name="cancellationToken">A token to observe for cancellation.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    public static async Task EnsureDatabaseAsync<TContext>(
        TContext context,
        CancellationToken cancellationToken = default)
        where TContext : DbContext
    {
        ArgumentNullException.ThrowIfNull(context);

        await context.Database.EnsureCreatedAsync(cancellationToken).ConfigureAwait(false);
    }
}
