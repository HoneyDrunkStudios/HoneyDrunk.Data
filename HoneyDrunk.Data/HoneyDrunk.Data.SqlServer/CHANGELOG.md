# Changelog

All notable changes to HoneyDrunk.Data.SqlServer will be documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.1.0/),
and this project adheres to [Semantic Versioning](https://semver.org/spec/v2.0.0.html).


## [0.6.0] - 2026-05-18

### Changed

- Consolidated SQL Server and Azure SQL retry/timeout provider configuration while preserving provider-specific registration calls.
- Package version bumped to `0.6.0` and Vault reference aligned to `0.5.0`.

## [0.5.1] - 2026-05-04

### Fixed

- Corrected NuGet package release metadata for the 0.5.x release train.

## [0.4.0] - 2026-04-25

### Changed

- SQL Server and Azure SQL registration now resolve connection strings through `ISecretStore` using unversioned `SecretIdentifier` values.
- `SqlServerDataOptions` now configures `ConnectionSecretName`, with `UseConnectionPurpose()` applying `Sql--{Purpose}Connection`.
- SQL health failures no longer include provider exception messages that could contain sensitive context.
- Package version bumped to `0.4.0`.

## [0.3.0] - 2026-02-15

### Added

- Canary test coverage for CI invariant enforcement

## [0.2.0] - 2026-01-06

### Changed

- **Architecture Overhaul**: Complete restructure of SQL Server specialization
- Enhanced `SqlServerDataOptions` with improved retry configuration
- Improved `SqlServerHealthContributor` with server metadata retrieval
- Refined model conventions for better SQL Server compatibility

### Added

- `UseAzureSql()` integration for Azure SQL Database optimization
- Improved `UseDateTime2ForAllDateTimeProperties()` with nullable type support
- Enhanced `ConfigureDecimalPrecision()` with configurable precision and scale
- Comprehensive XML documentation for all public types

## [0.1.0] - 2026-01-01

### Added

- Initial release of HoneyDrunk.Data.SqlServer specialization
- `SqlServerDataOptions` configuration class for SQL Server-specific settings
- `AddHoneyDrunkDataSqlServer<TContext>()` extension methods for SQL Server registration
- `AddHoneyDrunkDataAzureSql<TContext>()` extension methods for Azure SQL registration
- `SqlServerModelConventions` with `ApplySqlServerIndexConventions()` placeholder
- `SqlServerModelConventions.UseDateTime2ForAllDateTimeProperties()` convention helper
- `SqlServerModelConventions.ConfigureDecimalPrecision()` convention helper
- `SqlServerHealthContributor<TContext>` with enhanced SQL Server diagnostics
- Retry-on-failure configuration support
- Command timeout configuration support
