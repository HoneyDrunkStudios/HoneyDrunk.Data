# Changelog

All notable changes to HoneyDrunk.Data will be documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.1.0/),
and this project adheres to [Semantic Versioning](https://semver.org/spec/v2.0.0.html).

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
