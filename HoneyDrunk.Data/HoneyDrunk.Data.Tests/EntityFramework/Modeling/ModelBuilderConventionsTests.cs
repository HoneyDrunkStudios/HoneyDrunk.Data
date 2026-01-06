// Copyright (c) HoneyDrunk Studios. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using HoneyDrunk.Data.EntityFramework.Modeling;
using Microsoft.EntityFrameworkCore;

namespace HoneyDrunk.Data.Tests.EntityFramework.Modeling;

/// <summary>
/// Unit tests for <see cref="ModelBuilderConventions"/>.
/// </summary>
public sealed class ModelBuilderConventionsTests
{
    [Fact]
    public void ApplySnakeCaseNamingConvention_WithNullBuilder_ThrowsArgumentNullException()
    {
        Assert.Throws<ArgumentNullException>(
            () => ModelBuilderConventions.ApplySnakeCaseNamingConvention(null!));
    }

    [Fact]
    public void ApplySnakeCaseNamingConvention_ReturnsModelBuilder()
    {
        var modelBuilder = new ModelBuilder();

        var result = modelBuilder.ApplySnakeCaseNamingConvention();

        Assert.Same(modelBuilder, result);
    }

    [Fact]
    public void ApplyDefaultStringLength_WithNullBuilder_ThrowsArgumentNullException()
    {
        Assert.Throws<ArgumentNullException>(
            () => ModelBuilderConventions.ApplyDefaultStringLength(null!));
    }

    [Fact]
    public void ApplyDefaultStringLength_ReturnsModelBuilder()
    {
        var modelBuilder = new ModelBuilder();

        var result = modelBuilder.ApplyDefaultStringLength();

        Assert.Same(modelBuilder, result);
    }

    [Fact]
    public void ApplyDefaultStringLength_WithCustomLength_ReturnsModelBuilder()
    {
        var modelBuilder = new ModelBuilder();

        var result = modelBuilder.ApplyDefaultStringLength(512);

        Assert.Same(modelBuilder, result);
    }
}
