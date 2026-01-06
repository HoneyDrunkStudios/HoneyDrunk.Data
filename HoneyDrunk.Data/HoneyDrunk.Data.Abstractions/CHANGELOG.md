# Changelog

All notable changes to HoneyDrunk.Data.Abstractions will be documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.1.0/),
and this project adheres to [Semantic Versioning](https://semver.org/spec/v2.0.0.html).

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
