// Copyright (c) HoneyDrunk Studios. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using HoneyDrunk.Data.Abstractions.Diagnostics;
using Microsoft.EntityFrameworkCore;

namespace HoneyDrunk.Data.EntityFramework.Diagnostics;

/// <summary>
/// Health contributor that checks database connectivity.
/// </summary>
/// <typeparam name="TContext">The DbContext type.</typeparam>
public sealed class DbContextHealthContributor<TContext> : IDataHealthContributor
    where TContext : DbContext
{
    private readonly TContext _context;

    /// <summary>
    /// Initializes a new instance of the <see cref="DbContextHealthContributor{TContext}"/> class.
    /// </summary>
    /// <param name="context">The database context.</param>
    public DbContextHealthContributor(TContext context)
    {
        ArgumentNullException.ThrowIfNull(context);
        _context = context;
    }

    /// <inheritdoc />
    public string Name => $"DbContext:{typeof(TContext).Name}";

    /// <inheritdoc />
    public async ValueTask<DataHealthResult> CheckHealthAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            var canConnect = await _context.Database.CanConnectAsync(cancellationToken).ConfigureAwait(false);

            return canConnect
                ? DataHealthResult.Healthy("Database connection successful")
                : DataHealthResult.Unhealthy("Cannot connect to database");
        }
        catch (Exception ex)
        {
            return DataHealthResult.Unhealthy($"Database health check failed: {ex.Message}");
        }
    }
}
