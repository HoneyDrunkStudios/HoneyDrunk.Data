# Changelog - HoneyDrunk.Data.Outbox

## [0.8.0] - 2026-11-11

### Changed

- Require .NET 11 and adopt the coordinated Data 0.8.0 dependencies. Existing package responsibilities and public contracts are unchanged.

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

- Bumped `HoneyDrunk.Kernel.Abstractions` `0.7.0 → 0.8.0`.
- Bumped `Microsoft.EntityFrameworkCore` / `Microsoft.EntityFrameworkCore.Relational` `10.0.7 → 10.0.8`.
- Bumped `Microsoft.Extensions.DependencyInjection.Abstractions` / `.Logging.Abstractions` / `.Options` `10.0.7 → 10.0.8`.
- Refreshed HoneyDrunk.Standards to 0.2.9 for ADR-0047 testing tooling alignment.

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
- `EfOutboxWriter<TContext>` - adds outbox messages to EF change tracker.
- `EfOutboxReader<TContext>` - batch retrieval with compare-and-swap concurrency.
- `OutboxMessageConfiguration` - EF entity type configuration with indexes.
- `ModelBuilderExtensions.ApplyOutboxConfiguration()` - model builder integration.
- `OutboxHeaderSerializer` - System.Text.Json header serialization.
- `AddHoneyDrunkDataOutbox<TContext>()` - DI registration extension.
