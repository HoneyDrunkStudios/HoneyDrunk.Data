// Copyright (c) HoneyDrunk Studios. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using HoneyDrunk.Data.SqlServer.Diagnostics;
using HoneyDrunk.Data.Tests.TestFixtures;
using Microsoft.EntityFrameworkCore;

namespace HoneyDrunk.Data.Tests.SqlServer.Diagnostics;

/// <summary>
/// Unit tests for SqlServerHealthContributor.
/// </summary>
public sealed class SqlServerHealthContributorTests
{
    [Fact]
    public void Constructor_WithNullContext_ThrowsArgumentNullException()
    {
        Assert.Throws<ArgumentNullException>(
            () => new SqlServerHealthContributor<TestDbContext>(null!));
    }

    [Fact]
    public void Name_ContainsContextTypeName()
    {
        var options = new DbContextOptionsBuilder<TestDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        using var context = new TestDbContext(options);

        var contributor = new SqlServerHealthContributor<TestDbContext>(context);

        Assert.Contains("TestDbContext", contributor.Name);
    }

    [Fact]
    public void Name_HasExpectedFormat()
    {
        var options = new DbContextOptionsBuilder<TestDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        using var context = new TestDbContext(options);

        var contributor = new SqlServerHealthContributor<TestDbContext>(context);

        Assert.Equal("SqlServer:TestDbContext", contributor.Name);
    }
}
