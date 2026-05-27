// Copyright (c) HoneyDrunk Studios. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using HoneyDrunk.Data.Abstractions.Diagnostics;
using Microsoft.EntityFrameworkCore.Diagnostics;
using System.Data.Common;
using System.Diagnostics.CodeAnalysis;
using System.Text;

namespace HoneyDrunk.Data.EntityFramework.Diagnostics;

/// <summary>
/// EF Core command interceptor that adds correlation tags to database commands.
/// </summary>
public sealed class CorrelationCommandInterceptor : DbCommandInterceptor
{
    private const int MaxSanitizedLength = 128;

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

    // Allow-list sanitizer: copies only characters from a fixed safe alphabet
    // (alphanumeric + '-' + '_'), the canonical correlation-ID alphabet (RFC 4122
    // UUIDs, W3C trace-id hex, ULIDs). Any other byte — including SQL comment
    // terminators, newlines, quotes, semicolons — is silently dropped, so the
    // composed comment cannot escape its `/* ... */` envelope regardless of
    // upstream input. Capped at MaxSanitizedLength to bound command growth.
    private static string SanitizeForSqlComment(string value)
    {
        var builder = new StringBuilder(Math.Min(value.Length, MaxSanitizedLength));
        for (var i = 0; i < value.Length && builder.Length < MaxSanitizedLength; i++)
        {
            var c = value[i];
            if (char.IsAsciiLetterOrDigit(c) || c == '-' || c == '_')
            {
                builder.Append(c);
            }
        }

        return builder.ToString();
    }

    // CA2100 cannot see through SanitizeForSqlComment's allow-list. The sanitizer
    // only copies characters from [A-Za-z0-9_-] and caps length at MaxSanitizedLength,
    // so the assembled `/* correlation:<...> */` cannot escape its block-comment
    // envelope. The original `command.CommandText` is preserved verbatim; we only
    // prefix a constant-shape comment, no parameterization is possible for SQL
    // comments. Sonar Security Hotspot review reaches the same conclusion.
    [SuppressMessage("Security", "CA2100:Review SQL queries for security vulnerabilities", Justification = "Correlation ID is filtered through a strict allow-list ([A-Za-z0-9_-]) and length-capped before being embedded as a SQL block comment; the wrapping `/* */` cannot be escaped.")]
    private void AddCorrelationComment(DbCommand command)
    {
        var correlationId = _diagnosticsContext.CorrelationId;
        if (string.IsNullOrEmpty(correlationId))
        {
            return;
        }

        var sanitizedId = SanitizeForSqlComment(correlationId);
        if (sanitizedId.Length == 0)
        {
            return;
        }

        command.CommandText = $"/* correlation:{sanitizedId} */\n{command.CommandText}";
    }
}
