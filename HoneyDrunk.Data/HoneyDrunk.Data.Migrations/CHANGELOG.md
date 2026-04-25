# Changelog

All notable changes to HoneyDrunk.Data.Migrations will be documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.1.0/),
and this project adheres to [Semantic Versioning](https://semver.org/spec/v2.0.0.html).

## [0.4.0] - 2026-04-25

### Changed

- `MigrationDbContextFactory<TContext>` now bootstraps HoneyDrunk.Vault from `AZURE_KEYVAULT_URI` and resolves `Sql--MigrationConnection` through `ISecretStore`.
- Removed the `HONEYDRUNK_MIGRATION_CONNECTION` secret-bearing environment variable path.
- Package version bumped to `0.4.0`.

## [0.3.0] - 2026-02-15

### Added

- Canary test coverage for CI invariant enforcement

## [0.2.0] - 2026-01-06

### Changed

- **Architecture Overhaul**: Complete restructure of migration tooling
- Enhanced `MigrationDbContextFactory` with improved configuration options
- Improved `MigrationRunner` with better async patterns

### Added

- Comprehensive CI/CD workflow examples in documentation
- GitHub Actions and Azure DevOps pipeline examples
- Improved environment variable handling
- Comprehensive XML documentation for all public types

## [0.1.0] - 2026-01-01

### Added

- Initial release of HoneyDrunk.Data.Migrations tooling
- `MigrationDbContextFactory<TContext>` base class for design-time context factories
- Support for `HONEYDRUNK_MIGRATION_CONNECTION` environment variable
- Configurable migrations assembly support
- `MigrationRunner` static helper class with:
  - `ApplyMigrationsAsync()` for programmatic migration application
  - `GetPendingMigrationsAsync()` for listing pending migrations
  - `GetAppliedMigrationsAsync()` for listing applied migrations
  - `HasPendingMigrationsAsync()` for checking migration status
  - `EnsureDatabaseAsync()` for development/testing scenarios
