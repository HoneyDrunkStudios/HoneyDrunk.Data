# Changelog

All notable changes to HoneyDrunk.Data.Migrations will be documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.1.0/),
and this project adheres to [Semantic Versioning](https://semver.org/spec/v2.0.0.html).

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
