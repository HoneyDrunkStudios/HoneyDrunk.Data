# Changelog

All notable changes to HoneyDrunk.Data.SqlServer will be documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.1.0/),
and this project adheres to [Semantic Versioning](https://semver.org/spec/v2.0.0.html).

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
