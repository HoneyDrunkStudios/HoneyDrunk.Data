# HoneyDrunk.Data - Repository Changelog

All notable changes to the HoneyDrunk.Data repository will be documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.1.0/),
and this project adheres to [Semantic Versioning](https://semver.org/spec/v2.0.0.html).

**Note:** See individual package CHANGELOGs for detailed changes:
- [HoneyDrunk.Data.Abstractions CHANGELOG](HoneyDrunk.Data.Abstractions/CHANGELOG.md)
- [HoneyDrunk.Data CHANGELOG](HoneyDrunk.Data/CHANGELOG.md)
- [HoneyDrunk.Data.EntityFramework CHANGELOG](HoneyDrunk.Data.EntityFramework/CHANGELOG.md)
- [HoneyDrunk.Data.SqlServer CHANGELOG](HoneyDrunk.Data.SqlServer/CHANGELOG.md)
- [HoneyDrunk.Data.Migrations CHANGELOG](HoneyDrunk.Data.Migrations/CHANGELOG.md)
- [HoneyDrunk.Data.Outbox CHANGELOG](HoneyDrunk.Data.Outbox/CHANGELOG.md)
- [HoneyDrunk.Data.Outbox.Abstractions CHANGELOG](HoneyDrunk.Data.Outbox.Abstractions/CHANGELOG.md)
- [HoneyDrunk.Data.Outbox.Dispatcher CHANGELOG](HoneyDrunk.Data.Outbox.Dispatcher/CHANGELOG.md)
- [HoneyDrunk.Data.Testing CHANGELOG](HoneyDrunk.Data.Testing/CHANGELOG.md)

---

## [0.4.0] - 2026-04-25

### Added

- ADR-0005/0006 bootstrap wiring for HoneyDrunk.Data via env-driven Key Vault, App Configuration, and Event Grid invalidation helpers.
- Provider-grouped SQL secret naming with the `Sql--{Purpose}Connection` convention.
- Coordinated all package project versions to `0.4.0`.

### Changed

- SQL Server registration now resolves connection strings through `ISecretStore` per DbContext resolution instead of accepting raw connection strings.
- Migration design-time factories now bootstrap Vault from `AZURE_KEYVAULT_URI` and resolve `Sql--MigrationConnection` through `ISecretStore`.

## [0.3.0] - 2026-02-15

### Added

- `HoneyDrunk.Data.Canary` project with 18 CI invariant tests covering Kernel context, outbox concurrency, and transport boundary isolation

## [0.2.0] - 2026-01-06

### Changed

- Complete restructure of the orchestration layer for improved Kernel integration
- Enhanced `KernelTenantAccessor` with better null handling
- Improved `KernelDataDiagnosticsContext` with additional telemetry tags
- Refined `DataActivitySource` with more granular activity types

### Added

- Comprehensive documentation suite in `/docs` folder
- Improved configuration validation

## [0.1.0] - 2026-01-01

### Added

- Initial release of HoneyDrunk.Data
- `DataOptions` configuration class for data layer settings
- `KernelTenantAccessor` integrating with `IOperationContextAccessor`
- `KernelDataDiagnosticsContext` for telemetry enrichment from Kernel context
- `DataActivitySource` for creating telemetry activities
- `AddHoneyDrunkData()` extension method for service registration
- Entity Framework Core integration with `HoneyDrunkDbContext`
- SQL Server provider with connection-string resolution
- Transactional outbox pattern for reliable message publishing
- Migration infrastructure with `MigrationRunner`
- Testing utilities in `HoneyDrunk.Data.Testing`

[0.3.0]: https://github.com/HoneyDrunkStudios/HoneyDrunk.Data/releases/tag/v0.3.0
[0.4.0]: https://github.com/HoneyDrunkStudios/HoneyDrunk.Data/releases/tag/v0.4.0
[0.2.0]: https://github.com/HoneyDrunkStudios/HoneyDrunk.Data/releases/tag/v0.2.0
[0.1.0]: https://github.com/HoneyDrunkStudios/HoneyDrunk.Data/releases/tag/v0.1.0
