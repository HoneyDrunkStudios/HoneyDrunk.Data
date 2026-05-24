# Changelog - HoneyDrunk.Data.Outbox.Abstractions

## [Unreleased]

### Changed


- Enabled ADR-0044 OpenClaw/Codex Grid Review Runner request generation for repository PRs.
- Refreshed HoneyDrunk.Standards to 0.2.9 for ADR-0047 testing tooling alignment.

## 0.6.0

- Package version bumped to `0.6.0` for the coordinated Data release train.

## 0.5.1

- Corrected NuGet package release metadata for the 0.5.x release train.

## 0.4.0

- Coordinated package train bump for the ADR-0005/0006 Data rollout.

## 0.3.0

- Canary test coverage for outbox concurrency invariants (no double-dispatch, deterministic state transitions) and transport boundary isolation.

## 0.2.0

- **Breaking:** State machine renamed: `Processing` → `Leased`, `Failed` removed, `Poisoned` → `DeadLetter`.
- `OutboxMessageStatus` enum: `Pending(0)`, `Leased(1)`, `Dispatched(2)`, `DeadLetter(3)`.
- Added `LeasedUntil` (`DateTimeOffset?`) property to `OutboxMessage` for lease-based concurrency.
- Added `LastError` (`string?`) property to `OutboxMessage` for failure diagnostics.
- Added `LeaseDuration` (`TimeSpan`) property to `OutboxOptions`.
- `IOutboxReader.ClaimBatchAsync` now accepts `leaseDuration` parameter.
- Added `IOutboxReader.ReleaseForRetryAsync` (replaces `MarkFailedAsync`) with `lastError` parameter.
- Added `IOutboxReader.DeadLetterAsync` (replaces `MarkPoisonedAsync`) with `lastError` parameter.

## 0.1.0

- Initial release.
- `IOutboxWriter` - write outbox messages within a transaction.
- `IOutboxReader` - load pending messages and manage dispatch lifecycle.
- `IOutboxDispatcher` - dispatch trigger interface.
- `OutboxMessage` - message model.
- `OutboxMessageStatus` - lifecycle enum.
- `OutboxOptions` - configuration.
- `OutboxHeaderNames` - well-known header constants.
