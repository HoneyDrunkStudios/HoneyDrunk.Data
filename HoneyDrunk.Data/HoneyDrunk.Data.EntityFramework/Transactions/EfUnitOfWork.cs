// Copyright (c) HoneyDrunk Studios. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using HoneyDrunk.Data.Abstractions.Repositories;
using HoneyDrunk.Data.Abstractions.Transactions;
using HoneyDrunk.Data.Diagnostics;
using HoneyDrunk.Data.EntityFramework.Repositories;
using Microsoft.EntityFrameworkCore;

namespace HoneyDrunk.Data.EntityFramework.Transactions;

/// <summary>
/// Entity Framework Core implementation of <see cref="IUnitOfWork{TContext}"/>.
/// </summary>
/// <typeparam name="TContext">The DbContext type.</typeparam>
public sealed class EfUnitOfWork<TContext> : IUnitOfWork<TContext>
    where TContext : DbContext
{
    private readonly TContext _context;
    private readonly Dictionary<Type, object> _repositories = [];
    private bool _disposed;

    /// <summary>
    /// Initializes a new instance of the <see cref="EfUnitOfWork{TContext}"/> class.
    /// </summary>
    /// <param name="context">The database context.</param>
    public EfUnitOfWork(TContext context)
    {
        ArgumentNullException.ThrowIfNull(context);
        _context = context;
    }

    /// <inheritdoc />
    public bool HasPendingChanges => _context.ChangeTracker.HasChanges();

    /// <inheritdoc />
    public IRepository<TEntity> Repository<TEntity>()
        where TEntity : class
    {
        var entityType = typeof(TEntity);

        if (_repositories.TryGetValue(entityType, out var repository))
        {
            return (IRepository<TEntity>)repository;
        }

        var newRepository = new EfRepository<TEntity, TContext>(_context);
        _repositories[entityType] = newRepository;
        return newRepository;
    }

    /// <inheritdoc />
    public async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);

        using var activity = DataActivitySource.StartSaveChangesActivity();
        return await _context.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
    }

    /// <inheritdoc />
    public async Task<ITransactionScope> BeginTransactionAsync(CancellationToken cancellationToken = default)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);

        using var activity = DataActivitySource.StartTransactionActivity("Begin");
        var transaction = await _context.Database
            .BeginTransactionAsync(cancellationToken)
            .ConfigureAwait(false);

        activity?.SetTag("db.transaction.id", transaction.TransactionId.ToString());
        return new EfTransactionScope(transaction);
    }

    /// <inheritdoc />
    public async ValueTask DisposeAsync()
    {
        if (_disposed)
        {
            return;
        }

        _repositories.Clear();
        await _context.DisposeAsync().ConfigureAwait(false);
        _disposed = true;
    }
}
