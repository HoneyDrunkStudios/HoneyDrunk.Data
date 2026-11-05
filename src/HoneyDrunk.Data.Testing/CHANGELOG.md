# Changelog

All notable changes to HoneyDrunk.Data.Testing will be documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.1.0/),
and this project adheres to [Semantic Versioning](https://semver.org/spec/v2.0.0.html).

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

## [Unreleased]

## [0.7.0] - 2026-05-27

### Changed

- `SqliteTestDatabase<TContext>.ThrowIfDisposed` uses `ObjectDisposedException.ThrowIf(_disposed, this)` instead of an if/throw branch (Sonar S6966). Exception type is unchanged (`ObjectDisposedException`); `ObjectName` and the stack frame now come from the BCL helper rather than `nameof(SqliteTestDatabase<TContext>)` and an inline `throw`, so any consumer asserting on the exact `ObjectName` string or stack shape will see different values. No callers in the repo rely on either.

### Internal

- Bumped `Microsoft.EntityFrameworkCore.Sqlite` `10.0.7 → 10.0.8`.
- Refreshed HoneyDrunk.Standards to 0.2.9 for ADR-0047 testing tooling alignment.
- Marked the helper assembly as non-test-discoverable while retaining test helper dependencies, so coverage gates do not count fixture-only code as an empty test assembly (carried from prior unreleased window).

## [0.6.0] - 2026-05-18

### Changed

- Centralized SQLite test database lifecycle management with explicit in-memory keepalive and file-backed modes.
- Package version metadata bumped to `0.6.0` for the coordinated Data solution release train.

## [0.5.1] - 2026-05-04

### Fixed

- Corrected NuGet package release metadata for the 0.5.x release train.

## [0.3.0] - 2026-02-15

### Added

- `HoneyDrunk.Data.Canary` project leveraging Testing infrastructure for SQLite-based invariant validation

## [0.2.0] - 2026-01-06

### Changed

- **Architecture Overhaul**: Complete restructure of testing infrastructure
- Enhanced `SqliteTestDbContextFactory` with improved connection management
- Improved `SqliteDbContextFixture` with better lifecycle handling
- Refined `DatabaseResetHelper` with safer data clearing

### Added

- Improved `TestDoubles` with additional factory methods
- Better async disposal patterns across all fixtures
- Comprehensive usage pattern documentation
- Comprehensive XML documentation for all public types

## [0.1.0] - 2026-01-01

### Added

- Initial release of HoneyDrunk.Data.Testing helpers
- `SqliteTestDbContextFactory<TContext>` for creating SQLite in-memory test contexts
- `SqliteDbContextFixture<TContext>` xUnit fixture base class with:
  - Automatic SQLite in-memory database creation
  - Schema creation via `EnsureCreated()`
  - Overridable `SeedAsync()` for test data setup
  - Overridable `ConfigureOptions()` for custom configuration
  - Proper async disposal of resources
- `DatabaseResetHelper` static class with:
  - `ClearDataAsync()` for clearing all data while preserving schema
  - `ResetDatabaseAsync()` for dropping and recreating database
  - `DetachAllEntities()` for clearing change tracker
- `TestDoubles` static class with:
  - `CreateTenantAccessor()` for creating test tenant accessors
  - `CreateEmptyTenantAccessor()` for creating empty tenant accessors
  - `CreateDiagnosticsContext()` for creating test diagnostics contexts
