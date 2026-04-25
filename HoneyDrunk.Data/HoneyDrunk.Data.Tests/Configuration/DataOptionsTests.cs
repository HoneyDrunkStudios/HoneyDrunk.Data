// Copyright (c) HoneyDrunk Studios. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using HoneyDrunk.Data.Configuration;

namespace HoneyDrunk.Data.Tests.Configuration;

/// <summary>
/// Unit tests for <see cref="DataOptions"/>.
/// </summary>
public sealed class DataOptionsTests
{
    [Fact]
    public void DefaultSqlConnectionSecretName_UsesProviderGroupedDefault()
    {
        var options = new DataOptions();

        Assert.Equal("Sql--DefaultConnection", options.DefaultSqlConnectionSecretName);
    }

    [Fact]
    public void DefaultSqlConnectionSecretName_CanBeChanged()
    {
        var options = new DataOptions
        {
            DefaultSqlConnectionSecretName = "Sql--CustomConnection",
        };

        Assert.Equal("Sql--CustomConnection", options.DefaultSqlConnectionSecretName);
    }

    [Fact]
    public void EnableQueryTagging_DefaultIsTrue()
    {
        var options = new DataOptions();

        Assert.True(options.EnableQueryTagging);
    }

    [Fact]
    public void EnableQueryTagging_CanBeDisabled()
    {
        var options = new DataOptions
        {
            EnableQueryTagging = false,
        };

        Assert.False(options.EnableQueryTagging);
    }

    [Fact]
    public void RequireKernelContext_DefaultIsTrue()
    {
        var options = new DataOptions();

        Assert.True(options.RequireKernelContext);
    }

    [Fact]
    public void RequireKernelContext_CanBeDisabled()
    {
        var options = new DataOptions
        {
            RequireKernelContext = false,
        };

        Assert.False(options.RequireKernelContext);
    }

    [Fact]
    public void ActivitySourceName_DefaultIsHoneyDrunkData()
    {
        var options = new DataOptions();

        Assert.Equal("HoneyDrunk.Data", options.ActivitySourceName);
    }

    [Fact]
    public void ActivitySourceName_CanBeChanged()
    {
        var options = new DataOptions
        {
            ActivitySourceName = "CustomActivitySource",
        };

        Assert.Equal("CustomActivitySource", options.ActivitySourceName);
    }
}
