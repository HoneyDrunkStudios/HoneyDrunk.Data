// Copyright (c) HoneyDrunk Studios. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using HoneyDrunk.Data.EntityFramework.Registration;

namespace HoneyDrunk.Data.Tests.EntityFramework.Registration;

/// <summary>
/// Unit tests for <see cref="EfDataOptions"/>.
/// </summary>
public sealed class EfDataOptionsTests
{
    [Fact]
    public void EnableCorrelationInterceptor_DefaultIsTrue()
    {
        var options = new EfDataOptions();

        Assert.True(options.EnableCorrelationInterceptor);
    }

    [Fact]
    public void EnableCorrelationInterceptor_CanBeDisabled()
    {
        var options = new EfDataOptions
        {
            EnableCorrelationInterceptor = false,
        };

        Assert.False(options.EnableCorrelationInterceptor);
    }

    [Fact]
    public void RegisterHealthContributors_DefaultIsTrue()
    {
        var options = new EfDataOptions();

        Assert.True(options.RegisterHealthContributors);
    }

    [Fact]
    public void RegisterHealthContributors_CanBeDisabled()
    {
        var options = new EfDataOptions
        {
            RegisterHealthContributors = false,
        };

        Assert.False(options.RegisterHealthContributors);
    }

    [Fact]
    public void EnableSensitiveDataLogging_DefaultIsFalse()
    {
        var options = new EfDataOptions();

        Assert.False(options.EnableSensitiveDataLogging);
    }

    [Fact]
    public void EnableSensitiveDataLogging_CanBeEnabled()
    {
        var options = new EfDataOptions
        {
            EnableSensitiveDataLogging = true,
        };

        Assert.True(options.EnableSensitiveDataLogging);
    }

    [Fact]
    public void EnableDetailedErrors_DefaultIsFalse()
    {
        var options = new EfDataOptions();

        Assert.False(options.EnableDetailedErrors);
    }

    [Fact]
    public void EnableDetailedErrors_CanBeEnabled()
    {
        var options = new EfDataOptions
        {
            EnableDetailedErrors = true,
        };

        Assert.True(options.EnableDetailedErrors);
    }
}
