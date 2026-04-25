// Copyright (c) HoneyDrunk Studios. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using HoneyDrunk.Data.SqlServer.Registration;

namespace HoneyDrunk.Data.Tests.SqlServer.Registration;

/// <summary>
/// Unit tests for <see cref="SqlServerDataOptions"/>.
/// </summary>
public sealed class SqlServerDataOptionsTests
{
    [Fact]
    public void ConnectionSecretName_UsesProviderGroupedDefault()
    {
        var options = new SqlServerDataOptions();

        Assert.Equal("Sql--DefaultConnection", options.ConnectionSecretName);
    }

    [Fact]
    public void ConnectionSecretName_CanBeSet()
    {
        var options = new SqlServerDataOptions
        {
            ConnectionSecretName = "Sql--ReportingConnection",
        };

        Assert.Equal("Sql--ReportingConnection", options.ConnectionSecretName);
    }

    [Fact]
    public void UseConnectionPurpose_AppliesProviderGroupedConvention()
    {
        var options = new SqlServerDataOptions();

        options.UseConnectionPurpose("Outbox");

        Assert.Equal("Sql--OutboxConnection", options.ConnectionSecretName);
    }

    [Fact]
    public void EnableRetryOnFailure_DefaultIsTrue()
    {
        var options = new SqlServerDataOptions();

        Assert.True(options.EnableRetryOnFailure);
    }

    [Fact]
    public void EnableRetryOnFailure_CanBeDisabled()
    {
        var options = new SqlServerDataOptions
        {
            EnableRetryOnFailure = false,
        };

        Assert.False(options.EnableRetryOnFailure);
    }

    [Fact]
    public void MaxRetryCount_DefaultIsThree()
    {
        var options = new SqlServerDataOptions();

        Assert.Equal(3, options.MaxRetryCount);
    }

    [Fact]
    public void MaxRetryCount_CanBeChanged()
    {
        var options = new SqlServerDataOptions
        {
            MaxRetryCount = 5,
        };

        Assert.Equal(5, options.MaxRetryCount);
    }

    [Fact]
    public void MaxRetryDelaySeconds_DefaultIsThirty()
    {
        var options = new SqlServerDataOptions();

        Assert.Equal(30, options.MaxRetryDelaySeconds);
    }

    [Fact]
    public void MaxRetryDelaySeconds_CanBeChanged()
    {
        var options = new SqlServerDataOptions
        {
            MaxRetryDelaySeconds = 60,
        };

        Assert.Equal(60, options.MaxRetryDelaySeconds);
    }

    [Fact]
    public void CommandTimeoutSeconds_DefaultIsNull()
    {
        var options = new SqlServerDataOptions();

        Assert.Null(options.CommandTimeoutSeconds);
    }

    [Fact]
    public void CommandTimeoutSeconds_CanBeSet()
    {
        var options = new SqlServerDataOptions
        {
            CommandTimeoutSeconds = 120,
        };

        Assert.Equal(120, options.CommandTimeoutSeconds);
    }
}
