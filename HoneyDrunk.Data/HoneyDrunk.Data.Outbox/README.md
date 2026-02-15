# HoneyDrunk.Data.Outbox

EF Core persistence implementation for the HoneyDrunk.Data transactional outbox.

## What's Inside

| Type | Purpose |
|------|---------|
| `EfOutboxWriter<TContext>` | Adds outbox messages to the change tracker for atomic commit |
| `EfOutboxReader<TContext>` | Lease-based batch retrieval with compare-and-swap concurrency and expired-lease recovery |
| `OutboxMessageConfiguration` | EF entity type configuration (table, indexes, concurrency token, LeasedUntil, LastError) |
| `ModelBuilderExtensions` | `ApplyOutboxConfiguration()` extension for `ModelBuilder` |
| `OutboxHeaderSerializer` | System.Text.Json serialization for the headers dictionary |

## Design Constraints

- **No bus references** — does not know about Transport, Service Bus, or any broker.
- **Only persist, fetch, update** — never publishes messages.
- **Concurrency-safe** — `Status` is an EF concurrency token; per-message CAS prevents double-dispatch.
- **Lease recovery** — messages with expired leases are automatically reclaimed by the next poll cycle.

## Setup

### 1. Apply Entity Configuration

```csharp
public class AppDbContext : HoneyDrunkDbContext
{
    protected override void ApplyConfigurations(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyOutboxConfiguration(); // schema: "outbox", table: "OutboxMessages"
    }
}
```

### 2. Register Services

```csharp
services
    .AddHoneyDrunkData()
    .AddHoneyDrunkDataEntityFramework<AppDbContext>(...)
    .AddHoneyDrunkDataOutbox<AppDbContext>();
```

### 3. Add Migration

```bash
dotnet ef migrations add AddOutbox --context AppDbContext
```

## Concurrency Strategy

`ClaimBatchAsync` uses an atomic per-message `ExecuteUpdateAsync` with
a `WHERE Status = Pending` (or `Status = Leased AND LeasedUntil <= UtcNow`)
guard. The `Status` column is configured as an EF concurrency token, so only
one instance can claim each message. Instances that lose the race silently
skip the message.

For higher throughput on SQL Server, replace with a provider-specific reader
using `READPAST` + `UPDLOCK` table hints.

## State Machine

```
Pending ── ClaimBatchAsync ──▶ Leased ── MarkDispatchedAsync ──▶ Dispatched
                                 │
                                 ├── ReleaseForRetryAsync ──▶ Pending (retry)
                                 └── DeadLetterAsync ──▶ DeadLetter
                                 │
                            (lease expires) ──▶ reclaimed by next ClaimBatchAsync
```
