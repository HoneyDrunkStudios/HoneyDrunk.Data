// Copyright (c) HoneyDrunk Studios. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using HoneyDrunk.Data.EntityFramework.Modeling;
using HoneyDrunk.Data.Tests.TestFixtures;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata;

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
    public void ApplySnakeCaseNamingConvention_RenamesTableColumnAndKey()
    {
        var entity = BuildModelWithTestEntity();

        Assert.Equal("test_entity", entity.GetTableName());
        Assert.Contains(entity.GetProperties(), p => p.GetColumnName() == "name");
        Assert.Contains(entity.GetProperties(), p => p.GetColumnName() == "tenant_id");
        var key = Assert.Single(entity.GetKeys());
        Assert.NotNull(key.GetName());
        Assert.Equal(key.GetName(), key.GetName()!.ToLowerInvariant());
    }

    [Fact]
    public void ApplySnakeCaseNamingConvention_RenamesForeignKeyAndIndex()
    {
        var modelBuilder = new ModelBuilder();
        modelBuilder.Entity<ParentEntity>(b =>
        {
            b.HasKey(e => e.Id);
            b.HasIndex(e => e.LookupCode).HasDatabaseName("IX_Parent_LookupCode");
            b.HasMany(e => e.Children).WithOne(e => e.Parent).HasForeignKey(e => e.ParentId);
        });
        modelBuilder.Entity<ChildEntity>(b => b.HasKey(e => e.Id));

        modelBuilder.ApplySnakeCaseNamingConvention();

        var child = modelBuilder.Model.FindEntityType(typeof(ChildEntity))!;
        var fk = Assert.Single(child.GetForeignKeys());
        Assert.NotNull(fk.GetConstraintName());
        Assert.Equal(fk.GetConstraintName(), fk.GetConstraintName()!.ToLowerInvariant());

        var parent = modelBuilder.Model.FindEntityType(typeof(ParentEntity))!;
        var index = Assert.Single(parent.GetIndexes());
        var renamed = index.GetDatabaseName();
        Assert.NotNull(renamed);
        Assert.NotEqual("IX_Parent_LookupCode", renamed);
        Assert.Equal(renamed, renamed!.ToLowerInvariant());
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

    [Fact]
    public void ApplyDefaultStringLength_AppliesToStringPropertiesWithoutExplicitLength()
    {
        var modelBuilder = new ModelBuilder();
        modelBuilder.Entity<NamedEntity>(b =>
        {
            b.HasKey(e => e.Id);
            b.Property(e => e.UnboundedName);
            b.Property(e => e.ExplicitLength).HasMaxLength(50);
        });

        modelBuilder.ApplyDefaultStringLength(123);

        var entity = modelBuilder.Model.FindEntityType(typeof(NamedEntity))!;
        Assert.Equal(123, entity.FindProperty(nameof(NamedEntity.UnboundedName))!.GetMaxLength());
        Assert.Equal(50, entity.FindProperty(nameof(NamedEntity.ExplicitLength))!.GetMaxLength());
    }

    private static IMutableEntityType BuildModelWithTestEntity()
    {
        var modelBuilder = new ModelBuilder();
        modelBuilder.Entity<TestEntity>(b =>
        {
            b.ToTable("TestEntity");
            b.HasKey(e => e.Id);
            b.Property(e => e.Name);
            b.Property(e => e.TenantId);
        });
        modelBuilder.ApplySnakeCaseNamingConvention();
        return modelBuilder.Model.FindEntityType(typeof(TestEntity))!;
    }

    public sealed class ParentEntity
    {
        public Guid Id { get; set; }

        public string LookupCode { get; set; } = string.Empty;

        public List<ChildEntity> Children { get; } = [];
    }

    public sealed class ChildEntity
    {
        public Guid Id { get; set; }

        public Guid ParentId { get; set; }

        public ParentEntity? Parent { get; set; }
    }

    public sealed class NamedEntity
    {
        public Guid Id { get; set; }

        public string UnboundedName { get; set; } = string.Empty;

        public string ExplicitLength { get; set; } = string.Empty;
    }
}
