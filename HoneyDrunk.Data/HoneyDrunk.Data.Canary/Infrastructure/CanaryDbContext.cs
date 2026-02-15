using HoneyDrunk.Data.Outbox;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

namespace HoneyDrunk.Data.Canary.Infrastructure;

/// <summary>
/// Minimal DbContext for canary outbox tests. SQLite-backed with DateTimeOffset converters.
/// </summary>
public sealed class CanaryDbContext(DbContextOptions<CanaryDbContext> options) : DbContext(options)
{
    public DbSet<OutboxMessage> OutboxMessages => Set<OutboxMessage>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
        modelBuilder.ApplyOutboxConfiguration();

        // SQLite stores DateTimeOffset as ticks for correct comparison semantics
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
