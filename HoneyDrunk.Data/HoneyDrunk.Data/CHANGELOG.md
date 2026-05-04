# Changelog

All notable changes to HoneyDrunk.Data will be documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.1.0/),
and this project adheres to [Semantic Versioning](https://semver.org/spec/v2.0.0.html).

## [0.5.1] - 2026-05-04

### Fixed

- Corrected NuGet package release metadata for the 0.5.x release train.

## [0.5.0] - 2026-05-04

### Changed

- Adapted Kernel tenant context integration to consume Kernel 0.5.0 typed `TenantId` values, mapping `Internal` to Data's default tenant and preserving Data's public tenant abstractions.
- Package version bumped to `0.5.0`.

## [0.4.0] - 2026-04-25

### Added

- `AddHoneyDrunkDataBootstrap()` for env-driven Key Vault and App Configuration label `honeydrunk-data`.
- `SecretNameConventions.SqlConnection()` for provider-grouped SQL secret names.
- Package version bumped to `0.4.0`.

### Changed

- Replaced the default connection-string name option with `DefaultSqlConnectionSecretName`.

## [0.3.0] - 2026-02-15

### Added

- `HoneyDrunk.Data.Canary` project with 18 CI invariant tests covering Kernel context, outbox concurrency, and transport boundary isolation

## [0.2.0] - 2026-01-06

### Changed

- **Architecture Overhaul**: Complete restructure of the orchestration layer for improved Kernel integration
- Enhanced `KernelTenantAccessor` with better null handling
- Improved `KernelDataDiagnosticsContext` with additional telemetry tags
- Refined `DataActivitySource` with more granular activity types

### Added

- Comprehensive documentation suite in `/docs` folder
- `FILE_GUIDE.md` with complete file-by-file documentation
- Architecture documentation with layer diagrams
- Improved configuration validation

## [0.1.0] - 2026-01-01

### Added

- Initial release of HoneyDrunk.Data orchestration layer
- `DataOptions` configuration class for data layer settings
- `KernelTenantAccessor` implementation integrating with `IOperationContextAccessor`
- `KernelDataDiagnosticsContext` implementation for telemetry enrichment from Kernel context
- `DataActivitySource` static helper for creating telemetry activities
- `AddHoneyDrunkData()` extension method for service registration
- `ValidateHoneyDrunkDataConfiguration()` extension method for configuration validation
- Kernel integration for tenant resolution and correlation tracking
