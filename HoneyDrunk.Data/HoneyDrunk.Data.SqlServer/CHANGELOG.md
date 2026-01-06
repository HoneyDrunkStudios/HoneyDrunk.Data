# Changelog

All notable changes to HoneyDrunk.Data.SqlServer will be documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.1.0/),
and this project adheres to [Semantic Versioning](https://semver.org/spec/v2.0.0.html).

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
