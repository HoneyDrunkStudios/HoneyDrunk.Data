// Copyright (c) HoneyDrunk Studios. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace HoneyDrunk.Data.Outbox.Configuration;

/// <summary>
/// EF Core entity type configuration for <see cref="OutboxMessage"/>.
/// </summary>
/// <remarks>
/// Apply via <see cref="ModelBuilderExtensions.ApplyOutboxConfiguration"/> in your
/// DbContext's <c>OnModelCreating</c>.
/// </remarks>
public sealed class OutboxMessageConfiguration(
    string schema = "outbox",
    string tableName = "OutboxMessages") : IEntityTypeConfiguration<OutboxMessage>
{
    /// <inheritdoc />
    public void Configure(EntityTypeBuilder<OutboxMessage> builder)
    {
        builder.ToTable(tableName, schema);
        builder.HasKey(m => m.Id);

        builder.Property(m => m.Id)
            .ValueGeneratedNever();

        builder.Property(m => m.Type)
            .IsRequired()
            .HasMaxLength(512);

        builder.Property(m => m.Payload)
            .IsRequired();

        builder.Property(m => m.OccurredAt)
            .IsRequired();

        builder.Property(m => m.Headers)
            .HasMaxLength(8192);

        builder.Property(m => m.TenantId)
            .HasMaxLength(128);

        builder.Property(m => m.CorrelationId)
            .HasMaxLength(128);

        // Concurrency token prevents double-claim across instances
        builder.Property(m => m.Status)
            .IsRequired()
            .HasConversion<int>()
            .IsConcurrencyToken();

        builder.Property(m => m.RetryCount)
            .IsRequired()
            .HasDefaultValue(0);

        builder.Property(m => m.NextAttemptAt);

        builder.Property(m => m.LeasedUntil);

        builder.Property(m => m.LastError)
            .HasMaxLength(4096);

        // Composite index drives the polling query
        builder.HasIndex(m => new { m.Status, m.NextAttemptAt, m.OccurredAt })
            .HasDatabaseName("IX_OutboxMessages_Status_NextAttemptAt_OccurredAt");

        builder.HasIndex(m => m.TenantId)
            .HasDatabaseName("IX_OutboxMessages_TenantId");

        builder.HasIndex(m => m.CorrelationId)
            .HasDatabaseName("IX_OutboxMessages_CorrelationId");
    }
}
