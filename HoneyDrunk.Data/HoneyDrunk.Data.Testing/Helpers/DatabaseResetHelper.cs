// Copyright (c) HoneyDrunk Studios. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using Microsoft.EntityFrameworkCore;
using System.Diagnostics.CodeAnalysis;

namespace HoneyDrunk.Data.Testing.Helpers;

/// <summary>
/// Helper methods for resetting and cleaning up test databases.
/// </summary>
public static class DatabaseResetHelper
{
    /// <summary>
    /// Clears all data from the database while preserving the schema.
    /// </summary>
    /// <typeparam name="TContext">The DbContext type.</typeparam>
    /// <param name="context">The database context.</param>
    /// <param name="cancellationToken">A token to observe for cancellation.</param>
    /// <returns>A task representing the async operation.</returns>
    [SuppressMessage(
        "Security",
        "EF1002:Risk of vulnerability to SQL injection",
        Justification = "Table names come from EF model metadata, not user input.")]
    public static async Task ClearDataAsync<TContext>(
        TContext context,
        CancellationToken cancellationToken = default)
        where TContext : DbContext
    {
        ArgumentNullException.ThrowIfNull(context);

        var entityTypes = context.Model.GetEntityTypes()
            .Where(e => !e.IsOwned())
            .ToList();

        foreach (var entityType in entityTypes)
        {
            var tableName = entityType.GetTableName();
            var schema = entityType.GetSchema();

            if (string.IsNullOrEmpty(tableName))
            {
                continue;
            }

            // Table and schema names come from EF model metadata, not user input
            var fullTableName = string.IsNullOrEmpty(schema)
                ? $"\"{tableName}\""
                : $"\"{schema}\".\"{tableName}\"";

            await context.Database
                .ExecuteSqlRawAsync($"DELETE FROM {fullTableName}", cancellationToken)
                .ConfigureAwait(false);
        }
    }

    /// <summary>
    /// Resets the database by dropping and recreating it.
    /// </summary>
    /// <typeparam name="TContext">The DbContext type.</typeparam>
    /// <param name="context">The database context.</param>
    /// <param name="cancellationToken">A token to observe for cancellation.</param>
    /// <returns>A task representing the async operation.</returns>
    public static async Task ResetDatabaseAsync<TContext>(
        TContext context,
        CancellationToken cancellationToken = default)
        where TContext : DbContext
    {
        ArgumentNullException.ThrowIfNull(context);

        await context.Database.EnsureDeletedAsync(cancellationToken).ConfigureAwait(false);
        await context.Database.EnsureCreatedAsync(cancellationToken).ConfigureAwait(false);
    }

    /// <summary>
    /// Detaches all tracked entities from the change tracker.
    /// Useful for ensuring clean state between test operations.
    /// </summary>
    /// <typeparam name="TContext">The DbContext type.</typeparam>
    /// <param name="context">The database context.</param>
    public static void DetachAllEntities<TContext>(TContext context)
        where TContext : DbContext
    {
        ArgumentNullException.ThrowIfNull(context);

        context.ChangeTracker.Clear();
    }
}
