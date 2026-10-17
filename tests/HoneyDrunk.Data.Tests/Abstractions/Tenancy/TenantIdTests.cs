// Copyright (c) HoneyDrunk Studios. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using HoneyDrunk.Data.Abstractions.Tenancy;

namespace HoneyDrunk.Data.Tests.Abstractions.Tenancy;

/// <summary>
/// Unit tests for <see cref="TenantId"/>.
/// </summary>
public sealed class TenantIdTests
{
    [Fact]
    public void Constructor_WithValue_SetsValue()
    {
        var tenantId = new TenantId("test-tenant");

        Assert.Equal("test-tenant", tenantId.Value);
    }

    [Fact]
    public void Constructor_WithNull_SetsNullValue()
    {
        var tenantId = new TenantId(null!);

        Assert.Null(tenantId.Value);
    }

    [Fact]
    public void Default_HasNullValue()
    {
        var tenantId = default(TenantId);

        Assert.Null(tenantId.Value);
    }

    [Theory]
    [InlineData(null, true)]
    [InlineData("", true)]
    [InlineData("  ", true)]
    [InlineData("tenant-123", false)]
    public void IsEmpty_ReturnsExpectedResult(string? value, bool expectedIsEmpty)
    {
        var tenantId = new TenantId(value!);

        Assert.Equal(expectedIsEmpty, tenantId.IsEmpty);
    }

    [Fact]
    public void ImplicitStringConversion_ReturnsValue()
    {
        var tenantId = new TenantId("my-tenant");

        string result = tenantId;

        Assert.Equal("my-tenant", result);
    }

    [Fact]
    public void FromString_CreatesInstance()
    {
        var tenantId = TenantId.FromString("from-string-tenant");

        Assert.Equal("from-string-tenant", tenantId.Value);
    }

    [Fact]
    public void ToString_ReturnsValue()
    {
        var tenantId = new TenantId("string-tenant");

        Assert.Equal("string-tenant", tenantId.ToString());
    }

    [Fact]
    public void ToString_WithNull_ReturnsEmptyString()
    {
        var tenantId = new TenantId(null!);

        Assert.Equal(string.Empty, tenantId.ToString());
    }

    [Fact]
    public void Equality_SameValue_AreEqual()
    {
        var tenantId1 = new TenantId("same");
        var tenantId2 = new TenantId("same");

        Assert.Equal(tenantId1, tenantId2);
        Assert.True(tenantId1 == tenantId2);
        Assert.False(tenantId1 != tenantId2);
    }

    [Fact]
    public void Equality_DifferentValues_AreNotEqual()
    {
        var tenantId1 = new TenantId("tenant-a");
        var tenantId2 = new TenantId("tenant-b");

        Assert.NotEqual(tenantId1, tenantId2);
        Assert.False(tenantId1 == tenantId2);
        Assert.True(tenantId1 != tenantId2);
    }

    [Fact]
    public void GetHashCode_SameValue_SameHashCode()
    {
        var tenantId1 = new TenantId("hash-test");
        var tenantId2 = new TenantId("hash-test");

        Assert.Equal(tenantId1.GetHashCode(), tenantId2.GetHashCode());
    }

    [Fact]
    public void RecordStruct_SupportsWithExpression()
    {
        var original = new TenantId("original");
        var modified = original with { Value = "modified" };

        Assert.Equal("original", original.Value);
        Assert.Equal("modified", modified.Value);
    }
}
