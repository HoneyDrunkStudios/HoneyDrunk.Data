# Changelog

All notable changes to HoneyDrunk.Data.Abstractions will be documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.1.0/),
and this project adheres to [Semantic Versioning](https://semver.org/spec/v2.0.0.html).

## [0.8.0] - 2026-11-11

### Added

- Optional `IBoundedReadOnlyRepository<TEntity>` with typed ordering keys and a positive root-row limit applied by the persistence provider before materialization. The EF implementation preserves existing tracking and model loading behavior and performs no count-then-fetch step.
- SQLite regressions for source SQL limits, ordering/ties, explicit tenant filters, cancellation, untranslatable queries, concurrent inserts, and AutoInclude/split-query behavior.

### Changed

- Runtime packages require .NET 11; abstraction packages retain .NET 10 and add .NET 11. This release includes the .NET 11/EF Core 11 upgrade already on main, including SQL Server compatibility level 150 and the Azure authentication extension.
- Existing repository methods and interfaces remain unchanged. The new capability bounds root matches, not eager-loaded graphs, SQL command count or database scan/sort work.

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

### Changed (breaking)

- **`IUnitOfWork<TContext>` exposes a `Type ContextType { get; }` default interface member.** Surfaces the `TContext` marker at runtime for diagnostics and assertion in tests (Sonar S2326: "type parameter is not used"). DIM means no implementer change required; source-compatible for callers, binary-compatible for runtimes that dispatch DIMs (which includes .NET 10). Existing composition patterns like `IUnitOfWork<IAuditDataContext>` continue to work unchanged.
- **Package version bumped** to `HoneyDrunk.Data.Abstractions 0.7.0`.

### Internal

- Bumped `HoneyDrunk.Kernel.Abstractions` `0.7.0 → 0.8.0`.
- Refreshed HoneyDrunk.Standards to 0.2.9 for ADR-0047 testing tooling alignment (carried from the prior unreleased window).

## [0.6.0] - 2026-05-18

### Changed

- Package version bumped to `0.6.0` and Kernel abstractions reference aligned to `0.7.0`.

## [0.5.1] - 2026-05-04

### Fixed

- Corrected NuGet package release metadata for the 0.5.x release train.

## [0.4.0] - 2026-04-25

### Changed

- Coordinated package train bump for the ADR-0005/0006 Data rollout.

## [0.2.0] - 2026-01-06

### Changed

- **Architecture Overhaul**: Complete restructure of abstractions for improved clarity and extensibility
- Improved `IReadOnlyRepository<T>` with better expression-based query support
- Enhanced `IRepository<T>` with `RemoveRange` method
- Refined `IUnitOfWork` interface with clearer change tracking semantics
- Updated `ITenantResolutionStrategy` with async connection string resolution

### Added

- Comprehensive XML documentation for all public types
- Better null handling with `TenantId.IsEmpty` property
- Improved factory patterns for unit of work creation
## [0.3.0] - 2026-02-15

### Added

- `HoneyDrunk.Data.Canary` project with 18 CI invariant tests covering Kernel context, outbox concurrency, and transport boundary isolation
## [0.1.0] - 2026-01-01

### Added

- Initial release of HoneyDrunk.Data.Abstractions
- `TenantId` strongly-typed tenant identifier value type
- `ITenantAccessor` interface for accessing current tenant context
- `ITenantResolutionStrategy` interface for tenant-to-database resource mapping
- `IReadOnlyRepository<T>` interface for read-only repository operations
- `IRepository<T>` interface extending read-only with mutation operations
- `ITransactionScope` interface for explicit transaction boundaries
- `IUnitOfWork` interface for coordinating repository changes
- `IUnitOfWork<TContext>` generic interface with repository access
- `IUnitOfWorkFactory` interface for creating unit of work instances
- `DataHealthStatus` enum for health check status values
- `DataHealthResult` record for health check results
- `IDataHealthContributor` interface for health check contributions
- `IDataDiagnosticsContext` interface for correlation and telemetry context
