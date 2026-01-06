// Copyright (c) HoneyDrunk Studios. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using HoneyDrunk.Data.Abstractions.Diagnostics;

namespace HoneyDrunk.Data.Tests.Abstractions.Diagnostics;

/// <summary>
/// Unit tests for <see cref="DataHealthStatus"/>.
/// </summary>
public sealed class DataHealthStatusTests
{
    [Fact]
    public void Healthy_HasValueZero()
    {
        Assert.Equal(0, (int)DataHealthStatus.Healthy);
    }

    [Fact]
    public void Degraded_HasValueOne()
    {
        Assert.Equal(1, (int)DataHealthStatus.Degraded);
    }

    [Fact]
    public void Unhealthy_HasValueTwo()
    {
        Assert.Equal(2, (int)DataHealthStatus.Unhealthy);
    }
}
