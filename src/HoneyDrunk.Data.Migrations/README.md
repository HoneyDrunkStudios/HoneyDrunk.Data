# HoneyDrunk.Data.Migrations

Migration tooling conventions for HoneyDrunk.Data. This package provides design-time factory base classes and migration helper utilities.

## Purpose

Provides migration infrastructure for:
- Design-time DbContext factory base class for EF Core tooling
- Migration runner helpers for CI/CD scenarios
- Documentation for migration workflows

**Usage guidance:** This package is intended for tooling and CI/CD workflows. It should not be added as a `PackageReference` to runtime services or API projects.

## Allowed Dependencies

- `HoneyDrunk.Data.Abstractions` - For design-time stub dependencies
- `Microsoft.EntityFrameworkCore` - For migration APIs
- `Microsoft.EntityFrameworkCore.Design` (PrivateAssets=all) - For EF Core tooling
- `HoneyDrunk.Standards` - Analyzer package only (PrivateAssets=all)

**Note:** Database provider configuration (SQL Server, PostgreSQL, etc.) is the responsibility of the derived factory class, not this package.

## What Must Never Be Added

- **No database-specific packages** - Provider configuration belongs in derived factories
- **No runtime application references** - This is for tooling only
- **No transport or auth references** - HoneyDrunk.Transport and HoneyDrunk.Auth are not data concerns

## Namespace Layout

```
HoneyDrunk.Data.Migrations
├── Factories/            # Design-time context factory base
│   └── MigrationDbContextFactory.cs
└── Helpers/              # Migration utilities
    └── MigrationRunner.cs
```

## Canonical Workflow

The recommended approach is a **dedicated migrations project** that contains the design-time factory and owns the migration files.

### 1. Set up the connection string

```bash
# Windows PowerShell
$env:HONEYDRUNK_MIGRATION_CONNECTION = "Server=localhost;Database=MyDb;Trusted_Connection=true;TrustServerCertificate=true"

# Bash
export HONEYDRUNK_MIGRATION_CONNECTION="Server=localhost;Database=MyDb;Trusted_Connection=true;TrustServerCertificate=true"
```

### 2. Create a dedicated migrations project

```
MyApp.Migrations/
├── MyApp.Migrations.csproj
├── MyDbContextFactory.cs
└── Migrations/
    └── (generated migrations)
```

### 3. Create a design-time factory

```csharp
public class MyDbContextFactory : MigrationDbContextFactory<MyDbContext>
{
    protected override void ConfigureOptions(DbContextOptionsBuilder<MyDbContext> optionsBuilder)
    {
        var connectionString = GetConnectionString();
        
        // Provider configuration happens here
        optionsBuilder.UseSqlServer(connectionString, sqlOptions =>
        {
            sqlOptions.MigrationsAssembly(MigrationsAssembly);
        });
    }

    protected override MyDbContext CreateContext(DbContextOptions<MyDbContext> options)
    {
        // Design-time stubs for required dependencies
        // These are not valid for runtime use
        return new MyDbContext(
            options,
            new DesignTimeTenantAccessor(),
            new DesignTimeDiagnosticsContext());
    }
}

// Design-time stubs (include in migrations project)
internal class DesignTimeTenantAccessor : ITenantAccessor
{
    public TenantId GetCurrentTenantId() => default;
}

internal class DesignTimeDiagnosticsContext : IDataDiagnosticsContext
{
    public string? CorrelationId => null;
    public string? OperationId => null;
    public string? NodeId => null;
    public IReadOnlyDictionary<string, string> Tags => new Dictionary<string, string>();
}
```

**Note:** Design-time stubs provide minimal implementations for constructor dependencies. They are not valid for runtime use and should only exist in the migrations project.

### 4. Add a migration

```bash
dotnet ef migrations add InitialCreate \
  --project src/MyApp.Migrations \
  --startup-project src/MyApp.Migrations \
  --output-dir Migrations
```

### 5. Apply migrations

**Development:**

```bash
dotnet ef database update \
  --project src/MyApp.Migrations \
  --startup-project src/MyApp.Migrations
```

**Production (recommended):**

```bash
# Generate idempotent script for review
dotnet ef migrations script \
  --project src/MyApp.Migrations \
  --startup-project src/MyApp.Migrations \
  --idempotent \
  --output migrations.sql

# Review and apply via database tooling
```

## Programmatic Migration

For CI/CD scenarios (use with caution—bypasses human review):

```csharp
await MigrationRunner.ApplyMigrationsAsync(dbContext, cancellationToken);
```

Check for pending migrations:

```csharp
if (await MigrationRunner.HasPendingMigrationsAsync(dbContext))
{
    var pending = await MigrationRunner.GetPendingMigrationsAsync(dbContext);
    foreach (var migration in pending)
    {
        Console.WriteLine($"Pending: {migration}");
    }
}
```

## Versioning Guidance

- **Naming:** Consider timestamped names (`YYYYMMDDHHMMSS_MigrationName`) for ordering clarity
- **Reversibility:** Pure schema changes (add column, create table) are typically reversible; data transforms and destructive changes (drop column) require explicit rollback strategies
- **Production migrations:** Never modify a migration that has been applied to production; create a new migration instead
- **Review:** Generate SQL scripts for production deployments to enable human review before execution
