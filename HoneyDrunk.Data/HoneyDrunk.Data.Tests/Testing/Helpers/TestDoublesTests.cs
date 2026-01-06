// Copyright (c) HoneyDrunk Studios. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using HoneyDrunk.Data.Testing.Helpers;

namespace HoneyDrunk.Data.Tests.Testing.Helpers;

/// <summary>
/// Unit tests for <see cref="TestDoubles"/>.
/// </summary>
public sealed class TestDoublesTests
{
    [Fact]
    public void CreateTenantAccessor_ReturnsTenantAccessor()
    {
        var accessor = TestDoubles.CreateTenantAccessor("test-tenant");

        Assert.NotNull(accessor);
    }

    [Fact]
    public void CreateTenantAccessor_ReturnsCorrectTenantId()
    {
        var accessor = TestDoubles.CreateTenantAccessor("my-tenant");

        var tenantId = accessor.GetCurrentTenantId();

        Assert.Equal("my-tenant", tenantId.Value);
    }

    [Fact]
    public void CreateTenantAccessor_WithDifferentTenants_ReturnsDifferentValues()
    {
        var accessor1 = TestDoubles.CreateTenantAccessor("tenant-1");
        var accessor2 = TestDoubles.CreateTenantAccessor("tenant-2");

        Assert.Equal("tenant-1", accessor1.GetCurrentTenantId().Value);
        Assert.Equal("tenant-2", accessor2.GetCurrentTenantId().Value);
    }

    [Fact]
    public void CreateEmptyTenantAccessor_ReturnsAccessorWithEmptyTenant()
    {
        var accessor = TestDoubles.CreateEmptyTenantAccessor();

        var tenantId = accessor.GetCurrentTenantId();

        Assert.True(tenantId.IsEmpty);
    }

    [Fact]
    public void CreateDiagnosticsContext_WithNoArgs_ReturnsContext()
    {
        var context = TestDoubles.CreateDiagnosticsContext();

        Assert.NotNull(context);
    }

    [Fact]
    public void CreateDiagnosticsContext_WithNoArgs_HasNullProperties()
    {
        var context = TestDoubles.CreateDiagnosticsContext();

        Assert.Null(context.CorrelationId);
        Assert.Null(context.OperationId);
        Assert.Null(context.NodeId);
    }

    [Fact]
    public void CreateDiagnosticsContext_WithCorrelationId_SetsCorrelationId()
    {
        var context = TestDoubles.CreateDiagnosticsContext(correlationId: "corr-123");

        Assert.Equal("corr-123", context.CorrelationId);
    }

    [Fact]
    public void CreateDiagnosticsContext_WithOperationId_SetsOperationId()
    {
        var context = TestDoubles.CreateDiagnosticsContext(operationId: "op-456");

        Assert.Equal("op-456", context.OperationId);
    }

    [Fact]
    public void CreateDiagnosticsContext_WithNodeId_SetsNodeId()
    {
        var context = TestDoubles.CreateDiagnosticsContext(nodeId: "node-789");

        Assert.Equal("node-789", context.NodeId);
    }

    [Fact]
    public void CreateDiagnosticsContext_WithAllValues_SetsAllProperties()
    {
        var context = TestDoubles.CreateDiagnosticsContext(
            correlationId: "corr",
            operationId: "op",
            nodeId: "node");

        Assert.Equal("corr", context.CorrelationId);
        Assert.Equal("op", context.OperationId);
        Assert.Equal("node", context.NodeId);
    }

    [Fact]
    public void CreateDiagnosticsContext_Tags_ContainsSetValues()
    {
        var context = TestDoubles.CreateDiagnosticsContext(
            correlationId: "corr",
            operationId: "op",
            nodeId: "node");

        Assert.Equal(3, context.Tags.Count);
        Assert.Equal("corr", context.Tags["correlation.id"]);
        Assert.Equal("op", context.Tags["operation.id"]);
        Assert.Equal("node", context.Tags["node.id"]);
    }

    [Fact]
    public void CreateDiagnosticsContext_Tags_ExcludesNullValues()
    {
        var context = TestDoubles.CreateDiagnosticsContext(correlationId: "corr");

        Assert.Single(context.Tags);
        Assert.Equal("corr", context.Tags["correlation.id"]);
    }

    [Fact]
    public void CreateDiagnosticsContext_Tags_ExcludesEmptyValues()
    {
        var context = TestDoubles.CreateDiagnosticsContext(
            correlationId: "corr",
            operationId: string.Empty);

        Assert.Single(context.Tags);
    }
}
