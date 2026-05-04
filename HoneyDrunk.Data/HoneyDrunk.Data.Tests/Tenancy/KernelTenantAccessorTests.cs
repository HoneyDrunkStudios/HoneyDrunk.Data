// Copyright (c) HoneyDrunk Studios. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using HoneyDrunk.Data.Tenancy;
using HoneyDrunk.Kernel.Abstractions.Context;
using KernelTenantId = HoneyDrunk.Kernel.Abstractions.Identity.TenantId;

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
        accessor.Current.Returns((IOperationContext)null!);

        var tenantAccessor = new KernelTenantAccessor(accessor);

        var result = tenantAccessor.GetCurrentTenantId();

        Assert.True(result.IsEmpty);
        Assert.Equal(default, result);
    }

    [Fact]
    public void GetCurrentTenantId_WhenTenantIdIsInternal_ReturnsDefaultTenantId()
    {
        var context = Substitute.For<IOperationContext>();
        context.TenantId.Returns(KernelTenantId.Internal);

        var accessor = Substitute.For<IOperationContextAccessor>();
        accessor.Current.Returns(context);

        var tenantAccessor = new KernelTenantAccessor(accessor);

        var result = tenantAccessor.GetCurrentTenantId();

        Assert.True(result.IsEmpty);
        Assert.Equal(default, result);
    }

    [Fact]
    public void GetCurrentTenantId_WhenTenantIdIsNonInternal_ReturnsTenantIdBackedByKernelTenantId()
    {
        var kernelTenantId = KernelTenantId.NewId();
        var context = Substitute.For<IOperationContext>();
        context.TenantId.Returns(kernelTenantId);

        var accessor = Substitute.For<IOperationContextAccessor>();
        accessor.Current.Returns(context);

        var tenantAccessor = new KernelTenantAccessor(accessor);

        var result = tenantAccessor.GetCurrentTenantId();

        Assert.False(result.IsEmpty);
        Assert.Equal(kernelTenantId.ToString(), result.Value);
    }

    [Fact]
    public void GetCurrentTenantId_CalledMultipleTimes_ReturnsCurrentValue()
    {
        var firstTenantId = KernelTenantId.NewId();
        var secondTenantId = KernelTenantId.NewId();
        var context = Substitute.For<IOperationContext>();
        context.TenantId.Returns(firstTenantId, secondTenantId);

        var accessor = Substitute.For<IOperationContextAccessor>();
        accessor.Current.Returns(context);

        var tenantAccessor = new KernelTenantAccessor(accessor);

        var first = tenantAccessor.GetCurrentTenantId();
        var second = tenantAccessor.GetCurrentTenantId();

        Assert.Equal(firstTenantId.ToString(), first.Value);
        Assert.Equal(secondTenantId.ToString(), second.Value);
    }
}
