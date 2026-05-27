// Copyright (c) HoneyDrunk Studios. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using HoneyDrunk.Data.Abstractions.Diagnostics;
using HoneyDrunk.Data.EntityFramework.Diagnostics;
using Microsoft.EntityFrameworkCore.Diagnostics;
using System.Data.Common;

namespace HoneyDrunk.Data.Tests.EntityFramework.Diagnostics;

/// <summary>
/// Unit tests for <see cref="CorrelationCommandInterceptor"/>.
/// </summary>
public sealed class CorrelationCommandInterceptorTests
{
    [Fact]
    public void Constructor_WithNullContext_ThrowsArgumentNullException()
    {
        Assert.Throws<ArgumentNullException>(
            () => new CorrelationCommandInterceptor(null!));
    }

    [Fact]
    public void NonQueryExecuting_WithCorrelationId_AddsComment()
    {
        var diagnostics = Substitute.For<IDataDiagnosticsContext>();
        diagnostics.CorrelationId.Returns("test-correlation-123");

        var interceptor = new CorrelationCommandInterceptor(diagnostics);
        var command = CreateMockCommand("UPDATE Table SET Col = 1");

        interceptor.NonQueryExecuting(command, CreateEventData(), default);

        Assert.StartsWith("/* correlation:test-correlation-123 */", command.CommandText);
        Assert.Contains("UPDATE Table SET Col = 1", command.CommandText);
    }

    [Fact]
    public void NonQueryExecuting_WithoutCorrelationId_DoesNotModify()
    {
        var diagnostics = Substitute.For<IDataDiagnosticsContext>();
        string? correlationId = null;
        diagnostics.CorrelationId.Returns(correlationId);

        var interceptor = new CorrelationCommandInterceptor(diagnostics);
        var command = CreateMockCommand("UPDATE Table SET Col = 1");

        interceptor.NonQueryExecuting(command, CreateEventData(), default);

        Assert.Equal("UPDATE Table SET Col = 1", command.CommandText);
    }

    [Fact]
    public void NonQueryExecuting_WithEmptyCorrelationId_DoesNotModify()
    {
        var diagnostics = Substitute.For<IDataDiagnosticsContext>();
        diagnostics.CorrelationId.Returns(string.Empty);

        var interceptor = new CorrelationCommandInterceptor(diagnostics);
        var command = CreateMockCommand("UPDATE Table SET Col = 1");

        interceptor.NonQueryExecuting(command, CreateEventData(), default);

        Assert.Equal("UPDATE Table SET Col = 1", command.CommandText);
    }

    [Fact]
    public void ReaderExecuting_WithCorrelationId_AddsComment()
    {
        var diagnostics = Substitute.For<IDataDiagnosticsContext>();
        diagnostics.CorrelationId.Returns("reader-correlation");

        var interceptor = new CorrelationCommandInterceptor(diagnostics);
        var command = CreateMockCommand("SELECT * FROM Table");

        interceptor.ReaderExecuting(command, CreateEventData(), default);

        Assert.StartsWith("/* correlation:reader-correlation */", command.CommandText);
    }

    [Fact]
    public void ScalarExecuting_WithCorrelationId_AddsComment()
    {
        var diagnostics = Substitute.For<IDataDiagnosticsContext>();
        diagnostics.CorrelationId.Returns("scalar-correlation");

        var interceptor = new CorrelationCommandInterceptor(diagnostics);
        var command = CreateMockCommand("SELECT COUNT(*) FROM Table");

        interceptor.ScalarExecuting(command, CreateEventData(), default);

        Assert.StartsWith("/* correlation:scalar-correlation */", command.CommandText);
    }

    // Allow-list sanitizer accepts ASCII alphanumerics plus '-' and '_'
    // (the standard UUID / W3C trace-id / ULID alphabet). Every other byte —
    // including SQL block-comment terminators '*/' and '/*', newlines, quotes,
    // semicolons — is silently dropped, so the assembled `/* ... */` envelope
    // cannot be escaped regardless of upstream input. '--' is not in this list
    // because it is harmless inside a block comment (block comments do not
    // terminate on '--'); the hyphens themselves remain so UUIDs render
    // recognisably.
    [Theory]
    [InlineData("*/")]
    [InlineData("/*")]
    [InlineData("\n")]
    [InlineData("\r")]
    [InlineData(";DROP TABLE Users")]
    [InlineData("'OR'1'='1")]
    public void AddCorrelationComment_SanitizesDangerousCharacters(string dangerousSequence)
    {
        var correlationId = $"before{dangerousSequence}after";
        var diagnostics = Substitute.For<IDataDiagnosticsContext>();
        diagnostics.CorrelationId.Returns(correlationId);

        var interceptor = new CorrelationCommandInterceptor(diagnostics);
        var command = CreateMockCommand("SELECT 1");

        interceptor.ReaderExecuting(command, CreateEventData(), default);

        var commentStart = command.CommandText.IndexOf("correlation:", StringComparison.Ordinal);
        var commentEnd = command.CommandText.IndexOf(" */", StringComparison.Ordinal);
        var correlationPart = command.CommandText.Substring(
            commentStart + "correlation:".Length,
            commentEnd - commentStart - "correlation:".Length);

        Assert.DoesNotContain(dangerousSequence, correlationPart);
    }

    [Fact]
    public void AddCorrelationComment_PreservesUuidHyphens()
    {
        var uuid = "550e8400-e29b-41d4-a716-446655440000";
        var diagnostics = Substitute.For<IDataDiagnosticsContext>();
        diagnostics.CorrelationId.Returns(uuid);

        var interceptor = new CorrelationCommandInterceptor(diagnostics);
        var command = CreateMockCommand("SELECT 1");

        interceptor.ReaderExecuting(command, CreateEventData(), default);

        Assert.StartsWith($"/* correlation:{uuid} */", command.CommandText);
    }

    [Fact]
    public void AddCorrelationComment_CapsExcessivelyLongIdentifier()
    {
        var oversized = new string('a', 500);
        var diagnostics = Substitute.For<IDataDiagnosticsContext>();
        diagnostics.CorrelationId.Returns(oversized);

        var interceptor = new CorrelationCommandInterceptor(diagnostics);
        var command = CreateMockCommand("SELECT 1");

        interceptor.ReaderExecuting(command, CreateEventData(), default);

        var commentStart = command.CommandText.IndexOf("correlation:", StringComparison.Ordinal);
        var commentEnd = command.CommandText.IndexOf(" */", StringComparison.Ordinal);
        var correlationPart = command.CommandText.Substring(
            commentStart + "correlation:".Length,
            commentEnd - commentStart - "correlation:".Length);

        Assert.Equal(128, correlationPart.Length);
    }

    [Fact]
    public void AddCorrelationComment_AllStrippedInputDoesNotModifyCommand()
    {
        // Every character below is outside the allow-list ([A-Za-z0-9_-]), so the
        // sanitizer should return empty and the command must be unchanged.
        var diagnostics = Substitute.For<IDataDiagnosticsContext>();
        diagnostics.CorrelationId.Returns("'\";/*\\");

        var interceptor = new CorrelationCommandInterceptor(diagnostics);
        var command = CreateMockCommand("SELECT 1");

        interceptor.ReaderExecuting(command, CreateEventData(), default);

        Assert.Equal("SELECT 1", command.CommandText);
    }

    [Fact]
    public async Task NonQueryExecutingAsync_WithCorrelationId_AddsComment()
    {
        var diagnostics = Substitute.For<IDataDiagnosticsContext>();
        diagnostics.CorrelationId.Returns("async-correlation");

        var interceptor = new CorrelationCommandInterceptor(diagnostics);
        var command = CreateMockCommand("INSERT INTO Table VALUES (1)");

        await interceptor.NonQueryExecutingAsync(command, CreateEventData(), default);

        Assert.StartsWith("/* correlation:async-correlation */", command.CommandText);
    }

    [Fact]
    public async Task ReaderExecutingAsync_WithCorrelationId_AddsComment()
    {
        var diagnostics = Substitute.For<IDataDiagnosticsContext>();
        diagnostics.CorrelationId.Returns("async-reader-correlation");

        var interceptor = new CorrelationCommandInterceptor(diagnostics);
        var command = CreateMockCommand("SELECT * FROM Table");

        await interceptor.ReaderExecutingAsync(command, CreateEventData(), default);

        Assert.StartsWith("/* correlation:async-reader-correlation */", command.CommandText);
    }

    [Fact]
    public async Task ScalarExecutingAsync_WithCorrelationId_AddsComment()
    {
        var diagnostics = Substitute.For<IDataDiagnosticsContext>();
        diagnostics.CorrelationId.Returns("async-scalar-correlation");

        var interceptor = new CorrelationCommandInterceptor(diagnostics);
        var command = CreateMockCommand("SELECT MAX(Id) FROM Table");

        await interceptor.ScalarExecutingAsync(command, CreateEventData(), default);

        Assert.StartsWith("/* correlation:async-scalar-correlation */", command.CommandText);
    }

    private static DbCommand CreateMockCommand(string commandText)
    {
        var command = Substitute.For<DbCommand>();
        command.CommandText = commandText;
        command.When(c => c.CommandText = Arg.Any<string>())
            .Do(callInfo => command.CommandText.Returns(callInfo.Arg<string>()));
        return command;
    }

    private static CommandEventData CreateEventData()
    {
        return null!;
    }
}
