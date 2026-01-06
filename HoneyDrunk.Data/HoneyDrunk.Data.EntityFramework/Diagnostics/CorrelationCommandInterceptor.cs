// Copyright (c) HoneyDrunk Studios. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using HoneyDrunk.Data.Abstractions.Diagnostics;
using Microsoft.EntityFrameworkCore.Diagnostics;
using System.Data.Common;
using System.Diagnostics.CodeAnalysis;

namespace HoneyDrunk.Data.EntityFramework.Diagnostics;

/// <summary>
/// EF Core command interceptor that adds correlation tags to database commands.
/// </summary>
public sealed class CorrelationCommandInterceptor : DbCommandInterceptor
{
    private const string SuppressJustification =
        "Correlation ID is an internal system identifier, not user input. " +
        "It's added as a SQL comment and does not affect query execution.";

    private readonly IDataDiagnosticsContext _diagnosticsContext;

    /// <summary>
    /// Initializes a new instance of the <see cref="CorrelationCommandInterceptor"/> class.
    /// </summary>
    /// <param name="diagnosticsContext">The diagnostics context providing correlation information.</param>
    public CorrelationCommandInterceptor(IDataDiagnosticsContext diagnosticsContext)
    {
        ArgumentNullException.ThrowIfNull(diagnosticsContext);
        _diagnosticsContext = diagnosticsContext;
    }

    /// <inheritdoc />
    public override InterceptionResult<int> NonQueryExecuting(
        DbCommand command,
        CommandEventData eventData,
        InterceptionResult<int> result)
    {
        AddCorrelationComment(command);
        return base.NonQueryExecuting(command, eventData, result);
    }

    /// <inheritdoc />
    public override ValueTask<InterceptionResult<int>> NonQueryExecutingAsync(
        DbCommand command,
        CommandEventData eventData,
        InterceptionResult<int> result,
        CancellationToken cancellationToken = default)
    {
        AddCorrelationComment(command);
        return base.NonQueryExecutingAsync(command, eventData, result, cancellationToken);
    }

    /// <inheritdoc />
    public override InterceptionResult<DbDataReader> ReaderExecuting(
        DbCommand command,
        CommandEventData eventData,
        InterceptionResult<DbDataReader> result)
    {
        AddCorrelationComment(command);
        return base.ReaderExecuting(command, eventData, result);
    }

    /// <inheritdoc />
    public override ValueTask<InterceptionResult<DbDataReader>> ReaderExecutingAsync(
        DbCommand command,
        CommandEventData eventData,
        InterceptionResult<DbDataReader> result,
        CancellationToken cancellationToken = default)
    {
        AddCorrelationComment(command);
        return base.ReaderExecutingAsync(command, eventData, result, cancellationToken);
    }

    /// <inheritdoc />
    public override InterceptionResult<object> ScalarExecuting(
        DbCommand command,
        CommandEventData eventData,
        InterceptionResult<object> result)
    {
        AddCorrelationComment(command);
        return base.ScalarExecuting(command, eventData, result);
    }

    /// <inheritdoc />
    public override ValueTask<InterceptionResult<object>> ScalarExecutingAsync(
        DbCommand command,
        CommandEventData eventData,
        InterceptionResult<object> result,
        CancellationToken cancellationToken = default)
    {
        AddCorrelationComment(command);
        return base.ScalarExecutingAsync(command, eventData, result, cancellationToken);
    }

    private static string SanitizeForSqlComment(string value)
    {
        // Remove any characters that could break out of a SQL comment
        return value
            .Replace("*/", string.Empty, StringComparison.Ordinal)
            .Replace("/*", string.Empty, StringComparison.Ordinal)
            .Replace("--", string.Empty, StringComparison.Ordinal)
            .Replace("\n", string.Empty, StringComparison.Ordinal)
            .Replace("\r", string.Empty, StringComparison.Ordinal);
    }

    [SuppressMessage("Security", "CA2100:Review SQL queries for security vulnerabilities", Justification = SuppressJustification)]
    private void AddCorrelationComment(DbCommand command)
    {
        var correlationId = _diagnosticsContext.CorrelationId;
        if (string.IsNullOrEmpty(correlationId))
        {
            return;
        }

        // Sanitize correlation ID to ensure it only contains safe characters for SQL comments
        var sanitizedId = SanitizeForSqlComment(correlationId);
        var comment = $"/* correlation:{sanitizedId} */";
        command.CommandText = $"{comment}\n{command.CommandText}";
    }
}
