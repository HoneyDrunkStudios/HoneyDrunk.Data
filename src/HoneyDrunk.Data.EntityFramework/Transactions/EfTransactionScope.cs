// Copyright (c) HoneyDrunk Studios. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using HoneyDrunk.Data.Abstractions.Transactions;
using HoneyDrunk.Data.Diagnostics;
using Microsoft.EntityFrameworkCore.Storage;

namespace HoneyDrunk.Data.EntityFramework.Transactions;

/// <summary>
/// Entity Framework Core implementation of <see cref="ITransactionScope"/>.
/// </summary>
internal sealed class EfTransactionScope : ITransactionScope
{
    private readonly IDbContextTransaction _transaction;
    private bool _disposed;

    /// <summary>
    /// Initializes a new instance of the <see cref="EfTransactionScope"/> class.
    /// </summary>
    /// <param name="transaction">The underlying EF Core transaction.</param>
    public EfTransactionScope(IDbContextTransaction transaction)
    {
        ArgumentNullException.ThrowIfNull(transaction);
        _transaction = transaction;
    }

    /// <inheritdoc />
    public Guid TransactionId => _transaction.TransactionId;

    /// <inheritdoc />
    public async Task CommitAsync(CancellationToken cancellationToken = default)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);

        using var activity = DataActivitySource.StartTransactionActivity("Commit");
        activity?.SetTag("db.transaction.id", TransactionId.ToString());

        await _transaction.CommitAsync(cancellationToken).ConfigureAwait(false);
    }

    /// <inheritdoc />
    public async Task RollbackAsync(CancellationToken cancellationToken = default)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);

        using var activity = DataActivitySource.StartTransactionActivity("Rollback");
        activity?.SetTag("db.transaction.id", TransactionId.ToString());

        await _transaction.RollbackAsync(cancellationToken).ConfigureAwait(false);
    }

    /// <inheritdoc />
    public async ValueTask DisposeAsync()
    {
        if (_disposed)
        {
            return;
        }

        await _transaction.DisposeAsync().ConfigureAwait(false);
        _disposed = true;
    }
}
