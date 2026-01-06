# Changelog

All notable changes to HoneyDrunk.Data.Testing will be documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.1.0/),
and this project adheres to [Semantic Versioning](https://semver.org/spec/v2.0.0.html).

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
