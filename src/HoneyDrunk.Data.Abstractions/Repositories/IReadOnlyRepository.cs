// Copyright (c) HoneyDrunk Studios. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using System.Linq.Expressions;

namespace HoneyDrunk.Data.Abstractions.Repositories;

/// <summary>
/// Defines read-only operations for a repository of entities.
/// </summary>
/// <typeparam name="TEntity">The entity type.</typeparam>
public interface IReadOnlyRepository<TEntity>
    where TEntity : class
{
    /// <summary>
    /// Finds an entity by its primary key.
    /// </summary>
    /// <param name="id">The primary key value.</param>
    /// <param name="cancellationToken">A token to observe for cancellation.</param>
    /// <returns>The entity if found; otherwise, <c>null</c>.</returns>
    ValueTask<TEntity?> FindByIdAsync(object id, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets all entities matching the specified predicate.
    /// </summary>
    /// <param name="predicate">The filter predicate.</param>
    /// <param name="cancellationToken">A token to observe for cancellation.</param>
    /// <returns>A collection of matching entities.</returns>
    Task<IReadOnlyList<TEntity>> FindAsync(
        Expression<Func<TEntity, bool>> predicate,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets a single entity matching the specified predicate.
    /// </summary>
    /// <param name="predicate">The filter predicate.</param>
    /// <param name="cancellationToken">A token to observe for cancellation.</param>
    /// <returns>The matching entity, or <c>null</c> if not found.</returns>
    Task<TEntity?> FindOneAsync(
        Expression<Func<TEntity, bool>> predicate,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Determines whether any entities match the specified predicate.
    /// </summary>
    /// <param name="predicate">The filter predicate.</param>
    /// <param name="cancellationToken">A token to observe for cancellation.</param>
    /// <returns><c>true</c> if any matching entities exist; otherwise, <c>false</c>.</returns>
    Task<bool> ExistsAsync(
        Expression<Func<TEntity, bool>> predicate,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Counts entities matching the specified predicate.
    /// </summary>
    /// <param name="predicate">The filter predicate, or <c>null</c> to count all entities.</param>
    /// <param name="cancellationToken">A token to observe for cancellation.</param>
    /// <returns>The count of matching entities.</returns>
    Task<int> CountAsync(
        Expression<Func<TEntity, bool>>? predicate = null,
        CancellationToken cancellationToken = default);
}
