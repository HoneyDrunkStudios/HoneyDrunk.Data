// Copyright (c) HoneyDrunk Studios. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using HoneyDrunk.Data.Diagnostics;
using HoneyDrunk.Kernel.Abstractions.Context;

namespace HoneyDrunk.Data.Tests.Diagnostics;

/// <summary>
/// Unit tests for <see cref="KernelDataDiagnosticsContext"/>.
/// </summary>
public sealed class KernelDataDiagnosticsContextTests
{
    [Fact]
    public void Constructor_WithNullAccessor_ThrowsArgumentNullException()
    {
        Assert.Throws<ArgumentNullException>(() => new KernelDataDiagnosticsContext(null!));
    }

    [Fact]
    public void CorrelationId_WhenContextIsNull_ReturnsNull()
    {
        var accessor = Substitute.For<IOperationContextAccessor>();
        accessor.Current.Returns((IOperationContext?)null);

        var diagnostics = new KernelDataDiagnosticsContext(accessor);

        Assert.Null(diagnostics.CorrelationId);
    }

    [Fact]
    public void CorrelationId_WhenContextHasValue_ReturnsValue()
    {
        var context = Substitute.For<IOperationContext>();
        context.CorrelationId.Returns("corr-123");

        var accessor = Substitute.For<IOperationContextAccessor>();
        accessor.Current.Returns(context);

        var diagnostics = new KernelDataDiagnosticsContext(accessor);

        Assert.Equal("corr-123", diagnostics.CorrelationId);
    }

    [Fact]
    public void OperationId_WhenContextIsNull_ReturnsNull()
    {
        var accessor = Substitute.For<IOperationContextAccessor>();
        accessor.Current.Returns((IOperationContext?)null);

        var diagnostics = new KernelDataDiagnosticsContext(accessor);

        Assert.Null(diagnostics.OperationId);
    }

    [Fact]
    public void OperationId_WhenContextHasValue_ReturnsValue()
    {
        var context = Substitute.For<IOperationContext>();
        context.OperationId.Returns("op-456");

        var accessor = Substitute.For<IOperationContextAccessor>();
        accessor.Current.Returns(context);

        var diagnostics = new KernelDataDiagnosticsContext(accessor);

        Assert.Equal("op-456", diagnostics.OperationId);
    }

    [Fact]
    public void NodeId_WhenContextIsNull_ReturnsNull()
    {
        var accessor = Substitute.For<IOperationContextAccessor>();
        accessor.Current.Returns((IOperationContext?)null);

        var diagnostics = new KernelDataDiagnosticsContext(accessor);

        Assert.Null(diagnostics.NodeId);
    }

    [Fact]
    public void NodeId_WhenGridContextIsNull_ReturnsNull()
    {
        var context = Substitute.For<IOperationContext>();
        context.GridContext.Returns((IGridContext?)null);

        var accessor = Substitute.For<IOperationContextAccessor>();
        accessor.Current.Returns(context);

        var diagnostics = new KernelDataDiagnosticsContext(accessor);

        Assert.Null(diagnostics.NodeId);
    }

    [Fact]
    public void NodeId_WhenGridContextHasValue_ReturnsValue()
    {
        var gridContext = Substitute.For<IGridContext>();
        gridContext.NodeId.Returns("node-789");

        var context = Substitute.For<IOperationContext>();
        context.GridContext.Returns(gridContext);

        var accessor = Substitute.For<IOperationContextAccessor>();
        accessor.Current.Returns(context);

        var diagnostics = new KernelDataDiagnosticsContext(accessor);

        Assert.Equal("node-789", diagnostics.NodeId);
    }

    [Fact]
    public void Tags_WhenContextIsNull_ReturnsEmptyDictionary()
    {
        var accessor = Substitute.For<IOperationContextAccessor>();
        accessor.Current.Returns((IOperationContext?)null);

        var diagnostics = new KernelDataDiagnosticsContext(accessor);

        Assert.NotNull(diagnostics.Tags);
        Assert.Empty(diagnostics.Tags);
    }

    [Fact]
    public void Tags_WhenContextHasAllValues_ContainsAllTags()
    {
        var gridContext = Substitute.For<IGridContext>();
        gridContext.NodeId.Returns("node-id");

        var context = Substitute.For<IOperationContext>();
        context.CorrelationId.Returns("corr-id");
        context.OperationId.Returns("op-id");
        context.TenantId.Returns("tenant-id");
        context.GridContext.Returns(gridContext);

        var accessor = Substitute.For<IOperationContextAccessor>();
        accessor.Current.Returns(context);

        var diagnostics = new KernelDataDiagnosticsContext(accessor);

        Assert.Equal(4, diagnostics.Tags.Count);
        Assert.Equal("corr-id", diagnostics.Tags["correlation.id"]);
        Assert.Equal("op-id", diagnostics.Tags["operation.id"]);
        Assert.Equal("node-id", diagnostics.Tags["node.id"]);
        Assert.Equal("tenant-id", diagnostics.Tags["tenant.id"]);
    }

    [Fact]
    public void Tags_WhenSomeValuesAreEmpty_ExcludesEmptyTags()
    {
        var context = Substitute.For<IOperationContext>();
        context.CorrelationId.Returns("corr-id");
        context.OperationId.Returns(string.Empty);
        context.TenantId.Returns((string?)null);
        context.GridContext.Returns((IGridContext?)null);

        var accessor = Substitute.For<IOperationContextAccessor>();
        accessor.Current.Returns(context);

        var diagnostics = new KernelDataDiagnosticsContext(accessor);

        Assert.Single(diagnostics.Tags);
        Assert.Equal("corr-id", diagnostics.Tags["correlation.id"]);
    }
}
