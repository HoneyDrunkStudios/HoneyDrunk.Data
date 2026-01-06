// Copyright (c) HoneyDrunk Studios. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using HoneyDrunk.Data.Abstractions.Diagnostics;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;

namespace HoneyDrunk.Data.SqlServer.Diagnostics;

/// <summary>
/// SQL Server-specific health contributor with enhanced diagnostics.
/// </summary>
/// <typeparam name="TContext">The DbContext type.</typeparam>
public sealed class SqlServerHealthContributor<TContext> : IDataHealthContributor
    where TContext : DbContext
{
    private readonly TContext _context;

    /// <summary>
    /// Initializes a new instance of the <see cref="SqlServerHealthContributor{TContext}"/> class.
    /// </summary>
    /// <param name="context">The database context.</param>
    public SqlServerHealthContributor(TContext context)
    {
        ArgumentNullException.ThrowIfNull(context);
        _context = context;
    }

    /// <inheritdoc />
    public string Name => $"SqlServer:{typeof(TContext).Name}";

    /// <inheritdoc />
    public async ValueTask<DataHealthResult> CheckHealthAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            var canConnect = await _context.Database.CanConnectAsync(cancellationToken).ConfigureAwait(false);

            if (!canConnect)
            {
                return DataHealthResult.Unhealthy("Cannot connect to SQL Server database");
            }

            // Get additional diagnostic information
            var connectionString = _context.Database.GetConnectionString();
            var builder = new SqlConnectionStringBuilder(connectionString);

            var data = new Dictionary<string, object>
            {
                ["server"] = builder.DataSource,
                ["database"] = builder.InitialCatalog,
            };

            return new DataHealthResult(
                DataHealthStatus.Healthy,
                "SQL Server connection successful",
                data.AsReadOnly());
        }
        catch (SqlException ex)
        {
            return DataHealthResult.Unhealthy($"SQL Server health check failed: {ex.Message}");
        }
        catch (Exception ex)
        {
            return DataHealthResult.Unhealthy($"Unexpected error during health check: {ex.Message}");
        }
    }
}
