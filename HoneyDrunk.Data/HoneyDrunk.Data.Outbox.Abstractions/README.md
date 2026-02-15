# HoneyDrunk.Data.Outbox.Abstractions

Pure contracts for the HoneyDrunk.Data transactional outbox pattern.

## What's Inside

| Type | Purpose |
|------|---------|
| `IOutboxWriter` | Write outbox messages within a database transaction |
| `IOutboxReader` | Claim pending messages with lease-based concurrency and manage dispatch lifecycle |
| `IOutboxDispatcher` | Trigger a dispatch cycle |
| `OutboxMessage` | Message model with Id, Type, Payload, Headers, Status, LeasedUntil, LastError |
| `OutboxMessageStatus` | Lifecycle enum: Pending → Leased → Dispatched / DeadLetter |
| `OutboxOptions` | Schema, table name, lease duration, and enrichment configuration |
## Dependency Graph

```
HoneyDrunk.Data.Outbox.Abstractions
  └── (none — standalone contracts)
```

## Usage

Application code writes messages through `IOutboxWriter` inside the same
`SaveChangesAsync` that persists domain state:

```csharp
await outboxWriter.WriteAsync(new OutboxMessage
{
    Id = Guid.NewGuid(),
    Type = typeof(OrderPlaced).FullName!,
    Payload = JsonSerializer.Serialize(orderPlaced),
    OccurredAt = DateTimeOffset.UtcNow,
});
await unitOfWork.SaveChangesAsync();
```
