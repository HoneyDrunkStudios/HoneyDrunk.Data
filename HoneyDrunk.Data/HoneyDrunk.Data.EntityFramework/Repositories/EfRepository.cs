// Copyright (c) HoneyDrunk Studios. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using HoneyDrunk.Data.Abstractions.Repositories;
using HoneyDrunk.Data.Diagnostics;
using Microsoft.EntityFrameworkCore;
using System.Linq.Expressions;

namespace HoneyDrunk.Data.EntityFramework.Repositories;

/// <summary>
/// Entity Framework Core implementation of <see cref="IRepository{TEntity}"/>.
/// </summary>
/// <typeparam name="TEntity">The entity type.</typeparam>
/// <typeparam name="TContext">The DbContext type.</typeparam>
public class EfRepository<TEntity, TContext> : IRepository<TEntity>
    where TEntity : class
    where TContext : DbContext
{
    /// <summary>
    /// Initializes a new instance of the <see cref="EfRepository{TEntity, TContext}"/> class.
    /// </summary>
    /// <param name="context">The database context.</param>
    public EfRepository(TContext context)
    {
        ArgumentNullException.ThrowIfNull(context);
        Context = context;
        DbSet = context.Set<TEntity>();
    }

    /// <summary>
    /// Gets the underlying DbContext.
    /// </summary>
    protected TContext Context { get; }

    /// <summary>
    /// Gets the underlying DbSet.
    /// </summary>
    protected DbSet<TEntity> DbSet { get; }

    /// <inheritdoc />
    public virtual async ValueTask<TEntity?> FindByIdAsync(object id, CancellationToken cancellationToken = default)
    {
        using var activity = DataActivitySource.StartRepositoryActivity(typeof(TEntity).Name, "FindById");
        return await DbSet.FindAsync([id], cancellationToken).ConfigureAwait(false);
    }

    /// <inheritdoc />
    public virtual async Task<IReadOnlyList<TEntity>> FindAsync(
        Expression<Func<TEntity, bool>> predicate,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(predicate);

        using var activity = DataActivitySource.StartRepositoryActivity(typeof(TEntity).Name, "Find");
        return await DbSet
            .Where(predicate)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);
    }

    /// <inheritdoc />
    public virtual async Task<TEntity?> FindOneAsync(
        Expression<Func<TEntity, bool>> predicate,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(predicate);

        using var activity = DataActivitySource.StartRepositoryActivity(typeof(TEntity).Name, "FindOne");
        return await DbSet
            .FirstOrDefaultAsync(predicate, cancellationToken)
            .ConfigureAwait(false);
    }

    /// <inheritdoc />
    public virtual async Task<bool> ExistsAsync(
        Expression<Func<TEntity, bool>> predicate,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(predicate);

        using var activity = DataActivitySource.StartRepositoryActivity(typeof(TEntity).Name, "Exists");
        return await DbSet
            .AnyAsync(predicate, cancellationToken)
            .ConfigureAwait(false);
    }

    /// <inheritdoc />
    public virtual async Task<int> CountAsync(
        Expression<Func<TEntity, bool>>? predicate = null,
        CancellationToken cancellationToken = default)
    {
        using var activity = DataActivitySource.StartRepositoryActivity(typeof(TEntity).Name, "Count");
        return predicate is null
            ? await DbSet.CountAsync(cancellationToken).ConfigureAwait(false)
            : await DbSet.CountAsync(predicate, cancellationToken).ConfigureAwait(false);
    }

    /// <inheritdoc />
    public virtual async Task AddAsync(TEntity entity, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(entity);

        using var activity = DataActivitySource.StartRepositoryActivity(typeof(TEntity).Name, "Add");
        await DbSet.AddAsync(entity, cancellationToken).ConfigureAwait(false);
    }

    /// <inheritdoc />
    public virtual async Task AddRangeAsync(IEnumerable<TEntity> entities, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(entities);

        using var activity = DataActivitySource.StartRepositoryActivity(typeof(TEntity).Name, "AddRange");
        await DbSet.AddRangeAsync(entities, cancellationToken).ConfigureAwait(false);
    }

    /// <inheritdoc />
    public virtual void Update(TEntity entity)
    {
        ArgumentNullException.ThrowIfNull(entity);

        using var activity = DataActivitySource.StartRepositoryActivity(typeof(TEntity).Name, "Update");
        DbSet.Update(entity);
    }

    /// <inheritdoc />
    public virtual void UpdateRange(IEnumerable<TEntity> entities)
    {
        ArgumentNullException.ThrowIfNull(entities);

        using var activity = DataActivitySource.StartRepositoryActivity(typeof(TEntity).Name, "UpdateRange");
        DbSet.UpdateRange(entities);
    }

    /// <inheritdoc />
    public virtual void Remove(TEntity entity)
    {
        ArgumentNullException.ThrowIfNull(entity);

        using var activity = DataActivitySource.StartRepositoryActivity(typeof(TEntity).Name, "Remove");
        DbSet.Remove(entity);
    }

    /// <inheritdoc />
    public virtual void RemoveRange(IEnumerable<TEntity> entities)
    {
        ArgumentNullException.ThrowIfNull(entities);

        using var activity = DataActivitySource.StartRepositoryActivity(typeof(TEntity).Name, "RemoveRange");
        DbSet.RemoveRange(entities);
    }
}
