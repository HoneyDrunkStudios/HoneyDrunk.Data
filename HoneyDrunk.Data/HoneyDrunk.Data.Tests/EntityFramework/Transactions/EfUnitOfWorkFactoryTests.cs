// Copyright (c) HoneyDrunk Studios. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using HoneyDrunk.Data.Abstractions.Transactions;
using HoneyDrunk.Data.EntityFramework.Transactions;
using HoneyDrunk.Data.Tests.TestFixtures;
using Microsoft.EntityFrameworkCore;

namespace HoneyDrunk.Data.Tests.EntityFramework.Transactions;

/// <summary>
/// Unit tests for <see cref="EfUnitOfWorkFactory{TContext}"/>.
/// </summary>
public sealed class EfUnitOfWorkFactoryTests
{
    [Fact]
    public void Constructor_WithNullFactory_ThrowsArgumentNullException()
    {
        Assert.Throws<ArgumentNullException>(
            () => new EfUnitOfWorkFactory<TestDbContext>(null!));
    }

    [Fact]
    public void Create_ReturnsNewUnitOfWork()
    {
        var contextFactory = Substitute.For<IDbContextFactory<TestDbContext>>();
        var options = new DbContextOptionsBuilder<TestDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        contextFactory.CreateDbContext().Returns(new TestDbContext(options));

        var factory = new EfUnitOfWorkFactory<TestDbContext>(contextFactory);

        var unitOfWork = factory.Create();

        Assert.NotNull(unitOfWork);
        Assert.IsType<EfUnitOfWork<TestDbContext>>(unitOfWork);
    }

    [Fact]
    public void Create_CalledMultipleTimes_ReturnsDistinctInstances()
    {
        var contextFactory = Substitute.For<IDbContextFactory<TestDbContext>>();
        contextFactory.CreateDbContext().Returns(_ =>
        {
            var options = new DbContextOptionsBuilder<TestDbContext>()
                .UseInMemoryDatabase(Guid.NewGuid().ToString())
                .Options;
            return new TestDbContext(options);
        });

        var factory = new EfUnitOfWorkFactory<TestDbContext>(contextFactory);

        var uow1 = factory.Create();
        var uow2 = factory.Create();

        Assert.NotSame(uow1, uow2);
    }

    [Fact]
    public void Create_CallsContextFactory()
    {
        var contextFactory = Substitute.For<IDbContextFactory<TestDbContext>>();
        var options = new DbContextOptionsBuilder<TestDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        contextFactory.CreateDbContext().Returns(new TestDbContext(options));

        var factory = new EfUnitOfWorkFactory<TestDbContext>(contextFactory);

        _ = factory.Create();

        contextFactory.Received(1).CreateDbContext();
    }

    [Fact]
    public void Create_ReturnsIUnitOfWork_NotTyped()
    {
        var contextFactory = Substitute.For<IDbContextFactory<TestDbContext>>();
        var options = new DbContextOptionsBuilder<TestDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        contextFactory.CreateDbContext().Returns(new TestDbContext(options));

        var factory = new EfUnitOfWorkFactory<TestDbContext>(contextFactory);

        IUnitOfWork result = factory.Create();

        Assert.NotNull(result);
    }
}
