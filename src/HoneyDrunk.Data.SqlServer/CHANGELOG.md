# Changelog

All notable changes to HoneyDrunk.Data.SqlServer will be documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.1.0/),
and this project adheres to [Semantic Versioning](https://semver.org/spec/v2.0.0.html).

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

- Bumped `HoneyDrunk.Vault` `0.5.0 → 0.7.0`.
- Bumped `Microsoft.EntityFrameworkCore.SqlServer` `10.0.7 → 10.0.8`.
- Refreshed HoneyDrunk.Standards to 0.2.9 for ADR-0047 testing tooling alignment.

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
