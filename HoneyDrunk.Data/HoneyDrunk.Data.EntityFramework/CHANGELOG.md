# Changelog

All notable changes to HoneyDrunk.Data.EntityFramework will be documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.1.0/),
and this project adheres to [Semantic Versioning](https://semver.org/spec/v2.0.0.html).


## [Unreleased]

### Changed


- Enabled ADR-0044 OpenClaw/Codex Grid Review Runner request generation for repository PRs.
- Refreshed HoneyDrunk.Standards to 0.2.9 for ADR-0047 testing tooling alignment.

## [0.6.0] - 2026-05-18

### Changed


- Enabled ADR-0044 OpenClaw/Codex Grid Review Runner request generation for repository PRs.
- Consolidated EF DbContext option application so factory and scoped registrations share one configuration path.
- Package version bumped to `0.6.0`.

## [0.5.1] - 2026-05-04

### Fixed

- Corrected NuGet package release metadata for the 0.5.x release train.

## [0.4.0] - 2026-04-25

### Changed


- Enabled ADR-0044 OpenClaw/Codex Grid Review Runner request generation for repository PRs.
- Coordinated package train bump for the ADR-0005/0006 Data rollout.

## [0.3.0] - 2026-02-15

### Added

- Canary test coverage validating EF Core provider contracts against Kernel context and outbox concurrency invariants

## [0.2.0] - 2026-01-06

### Changed


- Enabled ADR-0044 OpenClaw/Codex Grid Review Runner request generation for repository PRs.
- **Architecture Overhaul**: Complete restructure of EF Core provider implementation
- Enhanced `HoneyDrunkDbContext` with improved tenant and correlation access
- Improved `EfRepository` with better async patterns
- Refined `EfUnitOfWork` with thread-safe repository caching
- Enhanced `CorrelationCommandInterceptor` with SQL comment sanitization

### Added

- `ModelBuilderConventions.ApplyDefaultStringLength()` for automatic string column sizing
- Better transaction scope management with `EfTransactionScope`
- Comprehensive XML documentation for all public types
- Improved health contributor diagnostics

## [0.1.0] - 2026-01-01

### Added

- Initial release of HoneyDrunk.Data.EntityFramework provider
- `HoneyDrunkDbContext` base class with tenant awareness and correlation tracking
- `EfRepository<TEntity, TContext>` generic repository implementation
- `EfUnitOfWork<TContext>` unit of work implementation with repository caching
- `EfUnitOfWorkFactory<TContext>` factory for creating unit of work instances
- `EfTransactionScope` transaction scope implementation wrapping `IDbContextTransaction`
- `CorrelationCommandInterceptor` for adding correlation IDs to SQL commands
- `DbContextHealthContributor<TContext>` for database connectivity health checks
- `ModelBuilderConventions` with snake_case naming and default string length helpers
- `EfDataOptions` configuration class for EF-specific settings
- `AddHoneyDrunkDataEntityFramework<TContext>()` extension methods for service registration
