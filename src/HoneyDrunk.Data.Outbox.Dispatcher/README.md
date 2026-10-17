# HoneyDrunk.Data.Outbox.Dispatcher

Background service that polls the transactional outbox and publishes pending
messages through HoneyDrunk.Transport abstractions.

## Shape

**Shape A: Library with BackgroundService.**

Nodes enable the dispatcher by calling `AddOutboxDispatcher()`. The hosted
service polls the outbox, publishes through Transport, and manages retry state.

## What's Inside

| Type | Purpose |
|------|---------|
| `OutboxDispatcherService` | `BackgroundService` + `IOutboxDispatcher` — poll / claim / publish / retry loop |
| `OutboxDispatcherOptions` | Batch size, poll interval, lease duration, retry policy, default destination |
| `AddOutboxDispatcher()` | DI registration extension |

## Design Constraints

- **Publishes via Transport interfaces only** — `ITransportPublisher`, `IEndpointAddress`, `ITransportEnvelope`.
- **No Azure Service Bus reference** — adapter-agnostic.
- **Lease-based concurrency** — claims messages with a time-bound lease; crashed dispatchers recover automatically.

## Integration Contract

```
Request Handler   ──write──>  IOutboxWriter  ──(same DB txn)──>  OutboxMessages table
                                                                        │
OutboxDispatcherService  ──poll──>  IOutboxReader  ──claim batch──>     │
                         ──publish──>  ITransportPublisher  ──>   Message Bus
                         ──mark──>  IOutboxReader.MarkDispatchedAsync()
```

### State Machine

```
Pending ── ClaimBatch ──▶ Leased ── Publish OK ──▶ Dispatched
                            │
                            ├── Publish Fail (retries left) ──▶ Pending (via ReleaseForRetry)
                            └── Publish Fail (exhausted)   ──▶ DeadLetter
```

### Guarantees

| Concern | Owner |
|---------|-------|
| Transactional persistence | Data (EfOutboxWriter + UnitOfWork) |
| Message delivery | Transport (ITransportPublisher + adapter) |
| Retry & backoff | Dispatcher (OutboxDispatcherService) |
| Concurrency safety | Data (EfOutboxReader CAS + lease expiry) |

## Setup

```csharp
services.AddOutboxDispatcher(opts =>
{
    opts.PollInterval = TimeSpan.FromSeconds(10);
    opts.BatchSize = 100;
    opts.LeaseDuration = TimeSpan.FromMinutes(5);
