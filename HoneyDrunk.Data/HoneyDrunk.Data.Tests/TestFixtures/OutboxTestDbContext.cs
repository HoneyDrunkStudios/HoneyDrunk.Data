// Copyright (c) HoneyDrunk Studios. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using HoneyDrunk.Data.Outbox;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

namespace HoneyDrunk.Data.Tests.TestFixtures;

/// <summary>
/// Minimal DbContext for outbox persistence tests.
/// </summary>
public sealed class OutboxTestDbContext(DbContextOptions<OutboxTestDbContext> options) : DbContext(options)
{
    public DbSet<OutboxMessage> OutboxMessages => Set<OutboxMessage>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
        modelBuilder.ApplyOutboxConfiguration();

        // SQLite cannot translate DateTimeOffset comparisons in LINQ;
        // store as ticks so numeric comparison works in test queries.
        var dtoffsetConverter = new ValueConverter<DateTimeOffset, long>(
            v => v.ToUnixTimeMilliseconds(),
            v => DateTimeOffset.FromUnixTimeMilliseconds(v));

        var nullableDtoffsetConverter = new ValueConverter<DateTimeOffset?, long?>(
            v => v.HasValue ? v.Value.ToUnixTimeMilliseconds() : null,
            v => v.HasValue ? DateTimeOffset.FromUnixTimeMilliseconds(v.Value) : null);

        var entity = modelBuilder.Entity<OutboxMessage>();
        entity.Property(m => m.OccurredAt).HasConversion(dtoffsetConverter);
        entity.Property(m => m.NextAttemptAt).HasConversion(nullableDtoffsetConverter);
        entity.Property(m => m.LeasedUntil).HasConversion(nullableDtoffsetConverter);
    }
}
