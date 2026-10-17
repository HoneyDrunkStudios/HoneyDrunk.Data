// Copyright (c) HoneyDrunk Studios. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using HoneyDrunk.Data.Abstractions.Diagnostics;
using HoneyDrunk.Data.Abstractions.Tenancy;
using HoneyDrunk.Data.EntityFramework.Context;
using Microsoft.EntityFrameworkCore;

namespace HoneyDrunk.Data.Tests.TestFixtures;

/// <summary>
/// Test entity for repository tests.
/// </summary>
public sealed class TestEntity
{
    public Guid Id { get; set; }

    public string Name { get; set; } = string.Empty;

    public string TenantId { get; set; } = string.Empty;
}

/// <summary>
/// Test DbContext for unit tests.
/// </summary>
public sealed class TestDbContext : HoneyDrunkDbContext
{
    public TestDbContext(DbContextOptions<TestDbContext> options)
        : base(options)
    {
    }

    public TestDbContext(
        DbContextOptions<TestDbContext> options,
        ITenantAccessor tenantAccessor,
        IDataDiagnosticsContext diagnosticsContext)
        : base(options, tenantAccessor, diagnosticsContext)
    {
    }

    public DbSet<TestEntity> TestEntities => Set<TestEntity>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<TestEntity>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Name).HasMaxLength(256);
            entity.Property(e => e.TenantId).HasMaxLength(128);
        });
    }
}
