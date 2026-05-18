# Changelog — HoneyDrunk.Data.Outbox

## 0.6.0

- `EfOutboxWriter<TContext>` now requires a current Kernel operation context when context autopopulation is enabled, unless callers provide explicit correlation and tenant ids.
- Internal tenant context is now persisted explicitly on outbox messages instead of being omitted.
- Package version bumped to `0.6.0` and Kernel abstractions reference aligned to `0.7.0`.

## 0.5.1

- Corrected NuGet package release metadata for the 0.5.x release train.

## 0.4.0

- Coordinated package train bump for the ADR-0005/0006 Data rollout.

## 0.3.0

- Canary test coverage for outbox concurrency invariants and transport boundary isolation.

## 0.2.0

- **Breaking:** `ClaimBatchAsync` replaces `LoadPendingBatchAsync` with lease semantics.
- `EfOutboxReader.ClaimBatchAsync` sets `LeasedUntil` on claimed messages and auto-reclaims expired leases.
- `ReleaseForRetryAsync` transitions messages back to `Pending` with `NextAttemptAt` and persists `LastError`.
- `DeadLetterAsync` transitions to `DeadLetter` and persists `LastError`.
- `OutboxMessageConfiguration` adds `LeasedUntil` and `LastError` column mappings.
- Source-generated logging via `Log.cs`.
- Comprehensive test coverage (14 reader tests, 12 writer tests, 7 serializer tests).

## 0.1.0

- Initial release.
- `EfOutboxWriter<TContext>` — adds outbox messages to EF change tracker.
- `EfOutboxReader<TContext>` — batch retrieval with compare-and-swap concurrency.
- `OutboxMessageConfiguration` — EF entity type configuration with indexes.
- `ModelBuilderExtensions.ApplyOutboxConfiguration()` — model builder integration.
- `OutboxHeaderSerializer` — System.Text.Json header serialization.
- `AddHoneyDrunkDataOutbox<TContext>()` — DI registration extension.
