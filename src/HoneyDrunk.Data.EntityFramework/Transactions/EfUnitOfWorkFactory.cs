// Copyright (c) HoneyDrunk Studios. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using HoneyDrunk.Data.Abstractions.Transactions;
using Microsoft.EntityFrameworkCore;

namespace HoneyDrunk.Data.EntityFramework.Transactions;

/// <summary>
/// Entity Framework Core implementation of <see cref="IUnitOfWorkFactory"/>.
/// Uses <see cref="IDbContextFactory{TContext}"/> for proper lifetime management
/// in background jobs and other non-scoped scenarios.
/// </summary>
/// <typeparam name="TContext">The DbContext type.</typeparam>
public sealed class EfUnitOfWorkFactory<TContext> : IUnitOfWorkFactory
    where TContext : DbContext
{
    private readonly IDbContextFactory<TContext> _contextFactory;

    /// <summary>
    /// Initializes a new instance of the <see cref="EfUnitOfWorkFactory{TContext}"/> class.
    /// </summary>
    /// <param name="contextFactory">The DbContext factory for creating new context instances.</param>
    public EfUnitOfWorkFactory(IDbContextFactory<TContext> contextFactory)
    {
        ArgumentNullException.ThrowIfNull(contextFactory);
        _contextFactory = contextFactory;
    }

    /// <inheritdoc />
    /// <remarks>
    /// Creates a new <see cref="IUnitOfWork"/> with its own <see cref="DbContext"/> instance.
    /// The caller is responsible for disposing the returned unit of work, which will dispose
    /// the underlying DbContext.
    /// </remarks>
    public IUnitOfWork Create()
    {
        var context = _contextFactory.CreateDbContext();
        return new EfUnitOfWork<TContext>(context);
    }
}
