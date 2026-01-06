# Changelog

All notable changes to HoneyDrunk.Data.EntityFramework will be documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.1.0/),
and this project adheres to [Semantic Versioning](https://semver.org/spec/v2.0.0.html).

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
