# Changelog - HoneyDrunk.Data.Outbox.Abstractions

## [0.8.0] - 2026-11-11

### Changed

- Retain .NET 10 support and add .NET 11 in the coordinated Data 0.8.0 release. Existing outbox contracts are unchanged.

## [0.7.2] - 2026-11-05

### Changed

- Refresh NuGet dependencies and adopt upstream package releases in dependency order. Preserve target frameworks and public contracts.

| Dependency | Previous | Updated |
| --- | --- | --- |
| HoneyDrunk.Kernel | 0.8.1 | 0.8.2 |
| HoneyDrunk.Kernel.Abstractions | 0.8.1 | 0.8.2 |
| HoneyDrunk.Standards | 0.4.0 | 0.4.3 |
| HoneyDrunk.Standards.Tests | 0.4.0 | 0.4.3 |
| HoneyDrunk.Transport | 0.7.2 | 0.7.3 |
| HoneyDrunk.Vault | 0.8.1 | 0.8.2 |
| HoneyDrunk.Vault.EventGrid | 0.8.1 | 0.8.2 |
| HoneyDrunk.Vault.Providers.AppConfiguration | 0.8.1 | 0.8.2 |
| HoneyDrunk.Vault.Providers.AzureKeyVault | 0.8.1 | 0.8.2 |
| Microsoft.CodeAnalysis.NetAnalyzers | 10.0.401 | 10.0.403 |
| Microsoft.EntityFrameworkCore | 10.0.12 | 10.0.14 |
| Microsoft.EntityFrameworkCore.Design | 10.0.12 | 10.0.14 |
| Microsoft.EntityFrameworkCore.InMemory | 10.0.12 | 10.0.14 |
| Microsoft.EntityFrameworkCore.Relational | 10.0.12 | 10.0.14 |
| Microsoft.EntityFrameworkCore.SqlServer | 10.0.12 | 10.0.14 |
| Microsoft.EntityFrameworkCore.Sqlite | 10.0.12 | 10.0.14 |
| Microsoft.Extensions.DependencyInjection | 10.0.12 | 10.0.14 |
| Microsoft.Extensions.DependencyInjection.Abstractions | 10.0.12 | 10.0.14 |
| Microsoft.Extensions.Hosting | 10.0.12 | 10.0.14 |
| Microsoft.Extensions.Hosting.Abstractions | 10.0.12 | 10.0.14 |
| Microsoft.Extensions.Logging.Abstractions | 10.0.12 | 10.0.14 |
| Microsoft.Extensions.Options | 10.0.12 | 10.0.14 |
| System.Security.Cryptography.Pkcs | 10.0.13 | 10.0.14 |

## [0.7.1] - 2026-09-26

### Changed

- Refresh dependency and shared build-tooling versions; preserve target frameworks and existing public contracts. See the [repository dependency changes](../../CHANGELOG.md).

## [Unreleased]

## [0.7.0] - 2026-05-27

### Internal

- Package version bumped to `0.7.0` for the coordinated Data release train.
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
