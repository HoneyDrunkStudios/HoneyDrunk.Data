# HoneyDrunk.Data.SqlServer

SQL Server family specialization for HoneyDrunk.Data, covering SQL Server, Azure SQL Database, and Azure SQL Managed Instance.

## Purpose

Provides SQL Server-specific configuration, optional conventions, and diagnostics. This package builds on the EF Core provider layer with SQL Server-specific wiring.

## Allowed Dependencies

- `HoneyDrunk.Data.EntityFramework` - The EF Core provider layer
- `Microsoft.EntityFrameworkCore.SqlServer` - SQL Server EF Core provider (includes relational and ADO.NET types transitively)
- `HoneyDrunk.Standards` - Analyzer package only (PrivateAssets=all)

## What Must Never Be Added

- **No other database providers** - PostgreSQL, MySQL, etc. belong in their own projects
- **No transport or auth references** - HoneyDrunk.Transport and HoneyDrunk.Auth are not data concerns
- **No provider-agnostic EF abstractions** - Base implementations belong in HoneyDrunk.Data.EntityFramework; this package contains SQL Server-specific wiring only

## Namespace Layout

```
HoneyDrunk.Data.SqlServer
├── Conventions/          # Optional SQL Server model conventions
│   └── SqlServerModelConventions.cs
├── Diagnostics/          # SQL Server health contributor
│   └── SqlServerHealthContributor.cs
└── Registration/         # DI extensions
    ├── ServiceCollectionExtensions.cs
    └── SqlServerDataOptions.cs
```

## Usage

```csharp
// Orchestration layer required
services.AddHoneyDrunkData();

// Register SQL Server provider
services.AddHoneyDrunkDataSqlServer<MyDbContext>(sqlOptions =>
{
    sqlOptions.ConnectionString = configuration.GetConnectionString("Default")
        ?? throw new InvalidOperationException("Connection string required");
    sqlOptions.EnableRetryOnFailure = true;
    sqlOptions.MaxRetryCount = 3;
});
```

## SQL Server Conventions (Optional)

These conventions are opt-in utilities, not applied by default. They can be called from either `ApplyConfigurations` or `ApplyConventions`:

```csharp
protected override void ApplyConventions(ModelBuilder modelBuilder)
{
    base.ApplyConventions(modelBuilder);
    
    // Optional SQL Server conventions
    modelBuilder
        .UseDateTime2ForAllDateTimeProperties()
        .ConfigureDecimalPrecision(precision: 18, scale: 4);
}
```

**Note:** `HoneyDrunkDbContext` provides both `ApplyConfigurations` (for entity configurations) and `ApplyConventions` (for naming/type conventions). Use whichever is appropriate for your organization.

## Azure SQL Support

Azure SQL Database and Azure SQL Managed Instance can use either `AddHoneyDrunkDataSqlServer` or `AddHoneyDrunkDataAzureSql`. The Azure SQL variant uses EF Core's `UseAzureSql()` method:

```csharp
// Option 1: Standard SQL Server provider (works for Azure SQL)
services.AddHoneyDrunkDataSqlServer<MyDbContext>(sqlOptions =>
{
    sqlOptions.ConnectionString = configuration.GetConnectionString("AzureSql");
    sqlOptions.EnableRetryOnFailure = true;
    sqlOptions.MaxRetryCount = 5;  // Higher for Azure transient faults
    sqlOptions.CommandTimeoutSeconds = 60;
});

// Option 2: Azure SQL-specific provider
services.AddHoneyDrunkDataAzureSql<MyDbContext>(sqlOptions =>
{
    sqlOptions.ConnectionString = configuration.GetConnectionString("AzureSql");
    sqlOptions.EnableRetryOnFailure = true;
    sqlOptions.MaxRetryCount = 5;
});
```

**Note:** `AddHoneyDrunkDataAzureSql` uses EF Core's `UseAzureSql()` method which may have Azure-specific optimizations. Both methods support the same `SqlServerDataOptions` configuration. Choose based on your deployment target and EF Core version capabilities.
