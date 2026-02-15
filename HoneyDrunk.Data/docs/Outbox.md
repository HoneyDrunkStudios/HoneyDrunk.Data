# 📤 Outbox — Transactional Outbox Pattern

[← Back to File Guide](FILE_GUIDE.md)

---

## Table of Contents

- [Overview](#overview)
- [Package Architecture](#package-architecture)
- [State Machine](#state-machine)
- [Writing Messages](#writing-messages)
- [Claiming and Dispatching](#claiming-and-dispatching)
- [Lease-Based Concurrency](#lease-based-concurrency)
- [Retry and Dead Letter](#retry-and-dead-letter)
- [Registration](#registration)
- [Entity Configuration](#entity-configuration)
- [Header Serialization](#header-serialization)
- [Testing](#testing)
- [Design Decisions](#design-decisions)

---

## Overview

The transactional outbox pattern guarantees that domain events are published reliably by persisting them in the same database transaction as the business state change. A background dispatcher then polls for pending messages and publishes them through Transport.

**Problem:** Publishing an event and saving to a database are two separate operations. If the app crashes between them, the event is lost or published without the corresponding state change.

**Solution:** Write the event to an `OutboxMessages` table in the same `SaveChangesAsync()` as the domain state. A background service polls the table and publishes via Transport.

### Three-Package Architecture

| Package | Responsibility | Dependencies |
|---------|----------------|--------------|
| `HoneyDrunk.Data.Outbox.Abstractions` | Contracts only | None (standalone) |
| `HoneyDrunk.Data.Outbox` | EF Core persistence | Kernel.Abstractions, EF Core |
| `HoneyDrunk.Data.Outbox.Dispatcher` | Background publish loop | Transport, Hosting |

---

## Package Architecture

```
Application Code
      │
      ├── WriteAsync() ──> IOutboxWriter ──> EfOutboxWriter<T>
      │                                         │
      │                                    (same SaveChangesAsync)
      │                                         │
      │                                    OutboxMessages table
      │                                         │
      └── (background) ──> OutboxDispatcherService
                                │
                                ├── ClaimBatchAsync() ──> IOutboxReader ──> EfOutboxReader<T>
                                ├── PublishAsync() ──> ITransportPublisher
                                └── MarkDispatchedAsync() / ReleaseForRetryAsync() / DeadLetterAsync()
```

### Dependency Flow

```
HoneyDrunk.Data.Outbox.Abstractions     (no dependencies)
          │
          ▼
HoneyDrunk.Data.Outbox                  (+ Kernel.Abstractions, EF Core)
          │
          ▼
HoneyDrunk.Data.Outbox.Dispatcher       (+ Transport, Hosting)
```

---

## State Machine

Messages follow a four-state lifecycle with lease-based transitions:

```
                    ┌──────────────────────────────────────────────┐
                    │                                              │
                    ▼                                              │
               ┌─────────┐                                        │
               │ Pending  │◄───── ReleaseForRetryAsync() ────┐    │
               │  (0)     │      (retries remaining)          │    │
               └────┬─────┘                                   │    │
                    │                                         │    │
             ClaimBatchAsync()                                │    │
             (sets LeasedUntil)                               │    │
                    │                                         │    │
                    ▼                                         │    │
               ┌─────────┐                                   │    │
               │ Leased   │───────────────────────────────────┘    │
               │  (1)     │                                        │
               └────┬─────┘                                        │
                    │                                              │
          ┌─────────┼──────────┐                                   │
          │         │          │                                   │
   Publish OK   Publish Fail  Lease Expires                        │
          │    (exhausted)     (LeasedUntil <= now)                 │
          │         │          │                                   │
          ▼         ▼          └───────────────────────────────────┘
   ┌──────────┐  ┌────────────┐
   │Dispatched│  │ DeadLetter │
   │  (2)     │  │   (3)      │
   └──────────┘  └────────────┘
```

### Status Enum

| Value | Name | Description |
|-------|------|-------------|
| 0 | `Pending` | Awaiting claim. Includes messages released for retry with a future `NextAttemptAt`. |
| 1 | `Leased` | Claimed by a dispatcher instance. `LeasedUntil` is set. |
| 2 | `Dispatched` | Published via Transport. Terminal state. |
| 3 | `DeadLetter` | Retries exhausted. Terminal state. `LastError` contains failure details. |

---

## Writing Messages

`IOutboxWriter` adds messages to the EF change tracker. They persist atomically with domain state:

```csharp
public class OrderService(IOutboxWriter outboxWriter, IUnitOfWork<AppDbContext> unitOfWork)
{
    public async Task PlaceOrderAsync(Order order, CancellationToken ct)
    {
        var orderRepo = unitOfWork.Repository<Order>();
        await orderRepo.AddAsync(order, ct);

        await outboxWriter.WriteAsync(new OutboxMessage
        {
            Id = Guid.NewGuid(),
            Type = typeof(OrderPlaced).FullName!,
            Payload = JsonSerializer.Serialize(new OrderPlaced(order.Id)),
            OccurredAt = DateTimeOffset.UtcNow,
        }, ct);

        // Both the order AND the outbox message save atomically
        await unitOfWork.SaveChangesAsync(ct);
    }
}
```

### Context Enrichment

When `OutboxOptions.AutoPopulateContextFields` is enabled (default), `EfOutboxWriter` enriches messages from `IOperationContextAccessor`:

- `CorrelationId` — copied from `IOperationContext.CorrelationId` (only if not already set)
- `TenantId` — copied from `IOperationContext.TenantId` (only if not already set)

### Batch Writing

```csharp
var messages = events.Select(e => new OutboxMessage
{
    Id = Guid.NewGuid(),
    Type = e.GetType().FullName!,
    Payload = JsonSerializer.Serialize(e),
    OccurredAt = DateTimeOffset.UtcNow,
});

await outboxWriter.WriteBatchAsync(messages, ct);
await unitOfWork.SaveChangesAsync(ct);
```

### Payload Size Validation

`OutboxOptions.MaxPayloadBytes` (default: 1 MB) validates payload size before writing. Messages exceeding this limit throw `InvalidOperationException`.

---

## Claiming and Dispatching

`IOutboxReader.ClaimBatchAsync` selects eligible messages and atomically transitions them to `Leased`:

```csharp
var messages = await reader.ClaimBatchAsync(
    batchSize: 100,
    leaseDuration: TimeSpan.FromMinutes(5),
    cancellationToken);
```

**Eligible messages:**
1. `Status = Pending` AND (`NextAttemptAt` is null OR `NextAttemptAt <= UtcNow`)
2. `Status = Leased` AND `LeasedUntil <= UtcNow` (expired lease recovery)

Messages are ordered by `OccurredAt` and claimed with per-message CAS (`ExecuteUpdateAsync` with `WHERE Status = expectedStatus`). The `Status` column is an EF concurrency token — only one instance wins per message.

### Dispatcher Flow

`OutboxDispatcherService` runs as a `BackgroundService`:

1. Create a DI scope
2. `ClaimBatchAsync` — claim a batch with lease
3. For each message:
   - Build a `TransportEnvelope` from the outbox message
   - Resolve destination from headers or `OutboxDispatcherOptions.DefaultDestination`
   - `PublishAsync` via `ITransportPublisher`
   - `MarkDispatchedAsync` on success
   - `ReleaseForRetryAsync` or `DeadLetterAsync` on failure
4. Wait `PollInterval`, repeat

---

## Lease-Based Concurrency

Leases prevent double-dispatch in multi-instance deployments.

### How It Works

1. `ClaimBatchAsync` sets `LeasedUntil = UtcNow + leaseDuration` on each claimed message
2. While leased, no other instance can claim the message
3. On successful publish, `MarkDispatchedAsync` clears the lease and sets `Dispatched`
4. On failure, `ReleaseForRetryAsync` clears the lease and sets `Pending` with `NextAttemptAt`
5. If the dispatcher crashes, the lease expires naturally
6. The next `ClaimBatchAsync` reclaims expired-lease messages automatically

### Configuration

```csharp
// On OutboxOptions (reader-side)
options.LeaseDuration = TimeSpan.FromMinutes(5);

// On OutboxDispatcherOptions (dispatcher-side)
options.LeaseDuration = TimeSpan.FromMinutes(5);
```

---

## Retry and Dead Letter

Failed dispatches follow exponential backoff:

| Attempt | Delay | Formula |
|---------|-------|---------|
| 1 | 1s | `BaseRetryDelay` |
| 2 | 2s | `BaseRetryDelay × 2` |
| 3 | 4s | `BaseRetryDelay × 4` |
| ... | ... | `min(BaseRetryDelay × 2^(n-1), MaxRetryDelay)` |

When `RetryCount >= MaxRetryAttempts`, the message transitions to `DeadLetter` with `LastError` set.

### Configuration

```csharp
services.AddOutboxDispatcher(opts =>
{
    opts.MaxRetryAttempts = 5;
    opts.BaseRetryDelay = TimeSpan.FromSeconds(1);
    opts.MaxRetryDelay = TimeSpan.FromMinutes(5);
});
```

### LastError Column

The `LastError` column (max 4096 characters) stores the exception message from the last failed dispatch attempt. It is set by both `ReleaseForRetryAsync` and `DeadLetterAsync`.

---

## Registration

### Full Stack

```csharp
var builder = WebApplication.CreateBuilder(args);

// 1. Kernel
builder.Services.AddHoneyDrunkGrid(opts => { /* ... */ });

// 2. Data orchestration
builder.Services.AddHoneyDrunkData();

// 3. EF Core provider
builder.Services.AddHoneyDrunkDataSqlServer<AppDbContext>(sqlOpts => { /* ... */ });

// 4. Outbox persistence
builder.Services.AddHoneyDrunkDataOutbox<AppDbContext>();

// 5. Outbox dispatcher (requires Transport registration)
builder.Services.AddOutboxDispatcher(opts =>
{
    opts.DefaultDestination = "domain-events";
    opts.PollInterval = TimeSpan.FromSeconds(10);
    opts.BatchSize = 100;
    opts.LeaseDuration = TimeSpan.FromMinutes(5);
    opts.MaxRetryAttempts = 5;
});
```

### Registration Dependencies

| Method | Requires | Registers |
|--------|----------|-----------|
| `AddHoneyDrunkDataOutbox<T>()` | EF Core DbContext registered | `IOutboxWriter`, `IOutboxReader` |
| `AddOutboxDispatcher()` | `IOutboxReader`, `ITransportPublisher` | `OutboxDispatcherService`, `IOutboxDispatcher` |

---

## Entity Configuration

Apply outbox table configuration in your `DbContext.OnModelCreating`:

```csharp
protected override void OnModelCreating(ModelBuilder modelBuilder)
{
    base.OnModelCreating(modelBuilder);
    modelBuilder.ApplyOutboxConfiguration(); // schema: "outbox", table: "OutboxMessages"
}
```

### Table Schema

| Column | Type | Constraints |
|--------|------|-------------|
| `Id` | `uniqueidentifier` | PK, not generated |
| `Type` | `nvarchar(512)` | Required |
| `Payload` | `nvarchar(max)` | Required |
| `OccurredAt` | `datetimeoffset` | Required |
| `Headers` | `nvarchar(8192)` | Optional |
| `TenantId` | `nvarchar(128)` | Optional |
| `CorrelationId` | `nvarchar(128)` | Optional |
| `Status` | `int` | Required, concurrency token |
| `RetryCount` | `int` | Required, default 0 |
| `NextAttemptAt` | `datetimeoffset` | Optional |
| `LeasedUntil` | `datetimeoffset` | Optional |
| `LastError` | `nvarchar(4096)` | Optional |

### Indexes

| Name | Columns | Purpose |
|------|---------|---------|
| `IX_OutboxMessages_Status_NextAttemptAt_OccurredAt` | Status, NextAttemptAt, OccurredAt | Drives the polling query |
| `IX_OutboxMessages_TenantId` | TenantId | Tenant-scoped queries |
| `IX_OutboxMessages_CorrelationId` | CorrelationId | Correlation lookups |

---

## Header Serialization

`OutboxHeaderSerializer` converts `Dictionary<string, string>` to/from JSON for the `Headers` column:

```csharp
var headers = new Dictionary<string, string>
{
    [OutboxHeaderNames.Destination] = "orders-topic",
    ["custom-key"] = "custom-value",
};

string? json = OutboxHeaderSerializer.Serialize(headers);     // → JSON string
Dictionary<string, string>? dict = OutboxHeaderSerializer.Deserialize(json); // → dictionary
```

### Well-Known Headers

| Constant | Key | Purpose |
|----------|-----|---------|
| `OutboxHeaderNames.Destination` | `x-outbox-destination` | Override dispatch destination |

The dispatcher strips the `Destination` header before forwarding to Transport.

---

## Testing

### SQLite Integration Tests

`EfOutboxReader` uses `ExecuteUpdateAsync` and `ExecuteDeleteAsync` which require a relational provider. Use `SqliteTestDbContextFactory<T>` with `DateTimeOffset` value converters:

```csharp
public sealed class OutboxTestDbContext(
    DbContextOptions<OutboxTestDbContext> options) : DbContext(options)
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
```

### Mocking the Dispatcher

For unit tests, mock `IOutboxReader` and `ITransportPublisher`:

```csharp
var reader = Substitute.For<IOutboxReader>();
reader.ClaimBatchAsync(Arg.Any<int>(), Arg.Any<TimeSpan>(), Arg.Any<CancellationToken>())
    .Returns(Array.Empty<OutboxMessage>());

var publisher = Substitute.For<ITransportPublisher>();
```

---

## Design Decisions

### Why Three Packages?

**Problem:** The outbox has three distinct consumers with different dependency profiles.

**Solution:**
- **Abstractions** — contracts for domain projects (no EF, no Transport)
- **Outbox** — EF Core persistence for projects with a DbContext
- **Dispatcher** — Transport bridge for projects that publish messages

A domain library can reference just `Outbox.Abstractions` to use `IOutboxWriter` without pulling in EF Core or Transport.

### Why Lease-Based Instead of Status-Only?

**Problem:** With status-only concurrency, a crashed dispatcher leaves messages stuck in `Processing` forever.

**Solution:** Leases have a time-bound expiry. If a dispatcher crashes, the lease expires and another instance reclaims the messages automatically. No operator intervention required.

### Why CAS Instead of Table Locks?

**Problem:** Table-level locks create contention in high-throughput scenarios.

**Solution:** Per-message compare-and-swap (`ExecuteUpdateAsync` with `WHERE Status = expectedStatus`) allows multiple instances to claim different messages concurrently. Only the winner updates each message.

### Why DeadLetter Instead of Failed?

**Problem:** A `Failed` status is ambiguous — does it mean "will retry" or "permanently failed"?

**Solution:** Failed messages go back to `Pending` (with `NextAttemptAt` set for backoff). Only when retries are exhausted does the message move to `DeadLetter`, which is a clear terminal state.

---

[← Back to File Guide](FILE_GUIDE.md) | [↑ Back to top](#table-of-contents)
