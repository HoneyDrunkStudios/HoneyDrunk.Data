// Copyright (c) HoneyDrunk Studios. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using HoneyDrunk.Data.Abstractions.Repositories;

namespace HoneyDrunk.Data.Abstractions.Transactions;

/// <summary>
/// Represents a unit of work that coordinates changes across multiple repositories
/// and persists them atomically.
/// </summary>
public interface IUnitOfWork : IAsyncDisposable
{
    /// <summary>
    /// Gets a value indicating whether there are pending changes to be saved.
    /// </summary>
    bool HasPendingChanges { get; }

    /// <summary>
    /// Saves all pending changes made through repositories within this unit of work.
    /// </summary>
    /// <param name="cancellationToken">A token to observe for cancellation.</param>
    /// <returns>The number of entities affected.</returns>
    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Begins an explicit transaction scope for fine-grained control.
    /// </summary>
    /// <param name="cancellationToken">A token to observe for cancellation.</param>
    /// <returns>A transaction scope that must be committed or disposed.</returns>
    Task<ITransactionScope> BeginTransactionAsync(CancellationToken cancellationToken = default);
}

/// <summary>
/// Extended unit of work that provides access to repositories.
/// </summary>
/// <typeparam name="TContext">The context type marker.</typeparam>
#pragma warning disable SA1402 // File may only contain a single type
public interface IUnitOfWork<TContext> : IUnitOfWork
#pragma warning restore SA1402 // File may only contain a single type
    where TContext : class
{
    /// <summary>
    /// Gets a repository for the specified entity type.
    /// </summary>
    /// <typeparam name="TEntity">The entity type.</typeparam>
    /// <returns>A repository for the entity type.</returns>
    IRepository<TEntity> Repository<TEntity>()
        where TEntity : class;
}
