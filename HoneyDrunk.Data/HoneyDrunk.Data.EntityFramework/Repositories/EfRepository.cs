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
    private readonly TContext _context;
    private readonly DbSet<TEntity> _dbSet;

    /// <summary>
    /// Initializes a new instance of the <see cref="EfRepository{TEntity, TContext}"/> class.
    /// </summary>
    /// <param name="context">The database context.</param>
    public EfRepository(TContext context)
    {
        ArgumentNullException.ThrowIfNull(context);
        _context = context;
        _dbSet = context.Set<TEntity>();
    }

    /// <summary>
    /// Gets the underlying DbContext.
    /// </summary>
    protected TContext Context => _context;

    /// <summary>
    /// Gets the underlying DbSet.
    /// </summary>
    protected DbSet<TEntity> DbSet => _dbSet;

    /// <inheritdoc />
    public virtual async ValueTask<TEntity?> FindByIdAsync(object id, CancellationToken cancellationToken = default)
    {
        using var activity = DataActivitySource.StartRepositoryActivity(typeof(TEntity).Name, "FindById");
        return await _dbSet.FindAsync([id], cancellationToken).ConfigureAwait(false);
    }

    /// <inheritdoc />
    public virtual async Task<IReadOnlyList<TEntity>> FindAsync(
        Expression<Func<TEntity, bool>> predicate,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(predicate);

        using var activity = DataActivitySource.StartRepositoryActivity(typeof(TEntity).Name, "Find");
        return await _dbSet
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
        return await _dbSet
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
        return await _dbSet
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
            ? await _dbSet.CountAsync(cancellationToken).ConfigureAwait(false)
            : await _dbSet.CountAsync(predicate, cancellationToken).ConfigureAwait(false);
    }

    /// <inheritdoc />
    public virtual async Task AddAsync(TEntity entity, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(entity);

        using var activity = DataActivitySource.StartRepositoryActivity(typeof(TEntity).Name, "Add");
        await _dbSet.AddAsync(entity, cancellationToken).ConfigureAwait(false);
    }

    /// <inheritdoc />
    public virtual async Task AddRangeAsync(IEnumerable<TEntity> entities, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(entities);

        using var activity = DataActivitySource.StartRepositoryActivity(typeof(TEntity).Name, "AddRange");
        await _dbSet.AddRangeAsync(entities, cancellationToken).ConfigureAwait(false);
    }

    /// <inheritdoc />
    public virtual void Update(TEntity entity)
    {
        ArgumentNullException.ThrowIfNull(entity);

        using var activity = DataActivitySource.StartRepositoryActivity(typeof(TEntity).Name, "Update");
        _dbSet.Update(entity);
    }

    /// <inheritdoc />
    public virtual void UpdateRange(IEnumerable<TEntity> entities)
    {
        ArgumentNullException.ThrowIfNull(entities);

        using var activity = DataActivitySource.StartRepositoryActivity(typeof(TEntity).Name, "UpdateRange");
        _dbSet.UpdateRange(entities);
    }

    /// <inheritdoc />
    public virtual void Remove(TEntity entity)
    {
        ArgumentNullException.ThrowIfNull(entity);

        using var activity = DataActivitySource.StartRepositoryActivity(typeof(TEntity).Name, "Remove");
        _dbSet.Remove(entity);
    }

    /// <inheritdoc />
    public virtual void RemoveRange(IEnumerable<TEntity> entities)
    {
        ArgumentNullException.ThrowIfNull(entities);

        using var activity = DataActivitySource.StartRepositoryActivity(typeof(TEntity).Name, "RemoveRange");
        _dbSet.RemoveRange(entities);
    }
}
