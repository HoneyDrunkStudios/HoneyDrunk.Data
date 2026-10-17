// Copyright (c) HoneyDrunk Studios. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using HoneyDrunk.Data.SqlServer.Conventions;
using HoneyDrunk.Data.Tests.TestFixtures;
using Microsoft.EntityFrameworkCore;

namespace HoneyDrunk.Data.Tests.SqlServer.Conventions;

/// <summary>
/// Unit tests for <see cref="SqlServerModelConventions"/>.
/// </summary>
public sealed class SqlServerModelConventionsTests
{
    [Fact]
    public void ApplySqlServerIndexConventions_WithNullBuilder_ThrowsArgumentNullException()
    {
        Assert.Throws<ArgumentNullException>(
            () => SqlServerModelConventions.ApplySqlServerIndexConventions(null!));
    }

    [Fact]
    public void ApplySqlServerIndexConventions_ReturnsModelBuilder()
    {
        var options = new DbContextOptionsBuilder<TestDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        using var context = new TestDbContext(options);
        var modelBuilder = new ModelBuilder();

        var result = modelBuilder.ApplySqlServerIndexConventions();

        Assert.Same(modelBuilder, result);
    }

    [Fact]
    public void UseDateTime2ForAllDateTimeProperties_WithNullBuilder_ThrowsArgumentNullException()
    {
        Assert.Throws<ArgumentNullException>(
            () => SqlServerModelConventions.UseDateTime2ForAllDateTimeProperties(null!));
    }

    [Fact]
    public void UseDateTime2ForAllDateTimeProperties_ReturnsModelBuilder()
    {
        var modelBuilder = new ModelBuilder();

        var result = modelBuilder.UseDateTime2ForAllDateTimeProperties();

        Assert.Same(modelBuilder, result);
    }

    [Fact]
    public void ConfigureDecimalPrecision_WithNullBuilder_ThrowsArgumentNullException()
    {
        Assert.Throws<ArgumentNullException>(
            () => SqlServerModelConventions.ConfigureDecimalPrecision(null!));
    }

    [Fact]
    public void ConfigureDecimalPrecision_ReturnsModelBuilder()
    {
        var modelBuilder = new ModelBuilder();

        var result = modelBuilder.ConfigureDecimalPrecision();

        Assert.Same(modelBuilder, result);
    }

    [Fact]
    public void ConfigureDecimalPrecision_UsesDefaultValues()
    {
        var modelBuilder = new ModelBuilder();

        var result = modelBuilder.ConfigureDecimalPrecision();

        Assert.Same(modelBuilder, result);
    }

    [Fact]
    public void ConfigureDecimalPrecision_AcceptsCustomValues()
    {
        var modelBuilder = new ModelBuilder();

        var result = modelBuilder.ConfigureDecimalPrecision(precision: 10, scale: 4);

        Assert.Same(modelBuilder, result);
    }
}
