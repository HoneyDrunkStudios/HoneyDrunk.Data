# Changelog

All notable changes to HoneyDrunk.Data.Abstractions will be documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.1.0/),
and this project adheres to [Semantic Versioning](https://semver.org/spec/v2.0.0.html).

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
