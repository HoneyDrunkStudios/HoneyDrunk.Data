// Copyright (c) HoneyDrunk Studios. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using HoneyDrunk.Data.Tenancy;
using HoneyDrunk.Kernel.Abstractions.Context;

namespace HoneyDrunk.Data.Tests.Tenancy;

/// <summary>
/// Unit tests for <see cref="KernelTenantAccessor"/>.
/// </summary>
public sealed class KernelTenantAccessorTests
{
    [Fact]
    public void Constructor_WithNullAccessor_ThrowsArgumentNullException()
    {
        Assert.Throws<ArgumentNullException>(() => new KernelTenantAccessor(null!));
    }

    [Fact]
    public void GetCurrentTenantId_WhenContextIsNull_ReturnsDefaultTenantId()
    {
        var accessor = Substitute.For<IOperationContextAccessor>();
        accessor.Current.Returns((IOperationContext?)null);

        var tenantAccessor = new KernelTenantAccessor(accessor);

        var result = tenantAccessor.GetCurrentTenantId();

        Assert.True(result.IsEmpty);
        Assert.Equal(default, result);
    }

    [Fact]
    public void GetCurrentTenantId_WhenTenantIdIsNull_ReturnsDefaultTenantId()
    {
        var context = Substitute.For<IOperationContext>();
        context.TenantId.Returns((string?)null);

        var accessor = Substitute.For<IOperationContextAccessor>();
        accessor.Current.Returns(context);

        var tenantAccessor = new KernelTenantAccessor(accessor);

        var result = tenantAccessor.GetCurrentTenantId();

        Assert.True(result.IsEmpty);
    }

    [Fact]
    public void GetCurrentTenantId_WhenTenantIdIsEmpty_ReturnsDefaultTenantId()
    {
        var context = Substitute.For<IOperationContext>();
        context.TenantId.Returns(string.Empty);

        var accessor = Substitute.For<IOperationContextAccessor>();
        accessor.Current.Returns(context);

        var tenantAccessor = new KernelTenantAccessor(accessor);

        var result = tenantAccessor.GetCurrentTenantId();

        Assert.True(result.IsEmpty);
    }

    [Fact]
    public void GetCurrentTenantId_WhenTenantIdIsWhitespace_ReturnsDefaultTenantId()
    {
        var context = Substitute.For<IOperationContext>();
        context.TenantId.Returns("   ");

        var accessor = Substitute.For<IOperationContextAccessor>();
        accessor.Current.Returns(context);

        var tenantAccessor = new KernelTenantAccessor(accessor);

        var result = tenantAccessor.GetCurrentTenantId();

        Assert.True(result.IsEmpty);
    }

    [Fact]
    public void GetCurrentTenantId_WhenTenantIdHasValue_ReturnsTenantId()
    {
        var context = Substitute.For<IOperationContext>();
        context.TenantId.Returns("tenant-123");

        var accessor = Substitute.For<IOperationContextAccessor>();
        accessor.Current.Returns(context);

        var tenantAccessor = new KernelTenantAccessor(accessor);

        var result = tenantAccessor.GetCurrentTenantId();

        Assert.False(result.IsEmpty);
        Assert.Equal("tenant-123", result.Value);
    }

    [Fact]
    public void GetCurrentTenantId_CalledMultipleTimes_ReturnsCurrentValue()
    {
        var context = Substitute.For<IOperationContext>();
        context.TenantId.Returns("first-tenant", "second-tenant");

        var accessor = Substitute.For<IOperationContextAccessor>();
        accessor.Current.Returns(context);

        var tenantAccessor = new KernelTenantAccessor(accessor);

        var first = tenantAccessor.GetCurrentTenantId();
        var second = tenantAccessor.GetCurrentTenantId();

        Assert.Equal("first-tenant", first.Value);
        Assert.Equal("second-tenant", second.Value);
    }
}
