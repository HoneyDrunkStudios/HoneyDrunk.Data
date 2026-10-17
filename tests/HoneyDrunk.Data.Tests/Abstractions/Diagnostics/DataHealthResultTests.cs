// Copyright (c) HoneyDrunk Studios. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using HoneyDrunk.Data.Abstractions.Diagnostics;

namespace HoneyDrunk.Data.Tests.Abstractions.Diagnostics;

/// <summary>
/// Unit tests for <see cref="DataHealthResult"/>.
/// </summary>
public sealed class DataHealthResultTests
{
    [Fact]
    public void Constructor_SetsAllProperties()
    {
        var data = new Dictionary<string, object> { ["key"] = "value" };

        var result = new DataHealthResult(DataHealthStatus.Healthy, "description", data);

        Assert.Equal(DataHealthStatus.Healthy, result.Status);
        Assert.Equal("description", result.Description);
        Assert.Same(data, result.Data);
    }

    [Fact]
    public void Constructor_WithDefaultOptionalParameters_SetsNulls()
    {
        var result = new DataHealthResult(DataHealthStatus.Healthy);

        Assert.Equal(DataHealthStatus.Healthy, result.Status);
        Assert.Null(result.Description);
        Assert.Null(result.Data);
    }

    [Fact]
    public void Healthy_WithoutDescription_ReturnsHealthyStatus()
    {
        var result = DataHealthResult.Healthy();

        Assert.Equal(DataHealthStatus.Healthy, result.Status);
        Assert.Null(result.Description);
        Assert.Null(result.Data);
    }

    [Fact]
    public void Healthy_WithDescription_ReturnsHealthyStatusWithDescription()
    {
        var result = DataHealthResult.Healthy("All systems operational");

        Assert.Equal(DataHealthStatus.Healthy, result.Status);
        Assert.Equal("All systems operational", result.Description);
    }

    [Fact]
    public void Degraded_ReturnsdegradedStatus()
    {
        var result = DataHealthResult.Degraded("Running slowly");

        Assert.Equal(DataHealthStatus.Degraded, result.Status);
        Assert.Equal("Running slowly", result.Description);
    }

    [Fact]
    public void Unhealthy_ReturnsUnhealthyStatus()
    {
        var result = DataHealthResult.Unhealthy("Connection failed");

        Assert.Equal(DataHealthStatus.Unhealthy, result.Status);
        Assert.Equal("Connection failed", result.Description);
    }

    [Fact]
    public void Record_SupportsEquality()
    {
        var result1 = DataHealthResult.Healthy("test");
        var result2 = DataHealthResult.Healthy("test");

        Assert.Equal(result1, result2);
    }

    [Fact]
    public void Record_SupportsWithExpression()
    {
        var original = DataHealthResult.Healthy("original");
        var modified = original with { Description = "modified" };

        Assert.Equal("original", original.Description);
        Assert.Equal("modified", modified.Description);
    }
}
