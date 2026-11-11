// Copyright (c) HoneyDrunk Studios. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using System.Linq.Expressions;

namespace HoneyDrunk.Data.Abstractions.Repositories;

/// <summary>
/// Optional repository capability for ordered reads limited by the persistence provider.
/// </summary>
/// <typeparam name="TEntity">The entity type.</typeparam>
/// <remarks>
/// Implementations must filter, order and limit at the source before materializing matching root entities.
/// They must reject unsupported queries instead of falling back to an unbounded read.
/// The limit bounds matching root entities, not related graphs, database work or SQL command count.
/// Model-configured eager loading, owned navigations and split-query behavior are preserved.
/// Existing repository implementations need not implement this capability.
/// </remarks>
public interface IBoundedReadOnlyRepository<TEntity> : IReadOnlyRepository<TEntity>
    where TEntity : class
{
    /// <summary>
    /// Returns at most <paramref name="limit"/> root matches ordered by two keys in one logical source query.
    /// </summary>
    /// <typeparam name="TOrderKey">The primary ordering key type.</typeparam>
    /// <typeparam name="TTieBreaker">The secondary ordering key type.</typeparam>
    /// <param name="predicate">The filter, including any required tenant scope.</param>
    /// <param name="orderBy">The primary ordering key.</param>
    /// <param name="tieBreaker">The secondary key. Together the keys must uniquely order matches for deterministic results.</param>
    /// <param name="limit">The positive maximum number of matching root entities to materialize.</param>
    /// <param name="descending">Whether to order both keys descending rather than ascending.</param>
    /// <param name="cancellationToken">A token to observe for cancellation.</param>
    /// <returns>The bounded, ordered matches.</returns>
    /// <remarks>
    /// No count-then-fetch step is used. A provider may use multiple SQL statements for eager loading.
    /// Key comparison follows the persistence provider's semantics, including collation.
    /// Results follow the current transaction's isolation level; repeated calls are not a shared snapshot.
    /// </remarks>
    Task<IReadOnlyList<TEntity>> FindOrderedAsync<TOrderKey, TTieBreaker>(
        Expression<Func<TEntity, bool>> predicate,
        Expression<Func<TEntity, TOrderKey>> orderBy,
        Expression<Func<TEntity, TTieBreaker>> tieBreaker,
        int limit,
        bool descending = false,
        CancellationToken cancellationToken = default);
}
