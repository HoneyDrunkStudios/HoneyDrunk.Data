# 📦 Migrations - Migration Tooling and Orchestration Conventions

[← Back to File Guide](FILE_GUIDE.md)

---

## Table of Contents

- [Overview](#overview)
- [Design Guidance](#design-guidance)
- [Factories](#factories)
  - [MigrationDbContextFactory.cs](#migrationdbcontextfactorycs)
- [Helpers](#helpers)
  - [MigrationRunner.cs](#migrationrunnercs)
- [Workflows](#workflows)
  - [Creating Migrations](#creating-migrations)
  - [Applying Migrations](#applying-migrations)
  - [CI/CD Integration](#cicd-integration)

---

## Overview

Migration tooling for HoneyDrunk.Data. This package provides design-time DbContext factories for EF Core tooling and helper methods for programmatic migration operations.

**Location:** `HoneyDrunk.Data.Migrations/`

This package provides:
- Design-time DbContext creation for `dotnet ef` commands
- Programmatic migration helpers (usable in CI/CD jobs or controlled runtime scenarios)
- Migration status checking and reporting

---

## Design Guidance

### Recommended Usage Patterns

This package is primarily intended for design-time tooling and CI/CD pipelines. However, the APIs are callable from any context.

| Usage Context | Guidance |
|---------------|----------|
| `dotnet ef` commands | ✅ Primary use case |
| CI/CD migration jobs | ✅ Recommended |
| Controlled startup (dev/staging) | ⚠️ Use with caution, environment-gated |
| Production runtime | ⚠️ Strongly discouraged—prefer explicit deployment steps |

> **Note:** The package does not enforce usage boundaries. These are conventions, not constraints. Production inclusion is possible but not recommended.

### Migration Safety

Programmatic migration application carries risk:

| Risk | Description |
|------|-------------|
| Long-running locks | Migrations may hold schema locks during execution |
| Destructive operations | Column drops, table renames may cause data loss |
| Rollback complexity | Failed mid-migration states can be difficult to recover |
| Review bypass | Auto-apply skips human review of generated SQL |

**Recommendation:** For production, generate idempotent SQL scripts and apply via reviewed deployment processes.

### Connection String Management

| Source | Guidance |
|--------|----------|
| Environment variable | ✅ Recommended for CI/CD (`HONEYDRUNK_MIGRATION_CONNECTION`) |
| Configuration system | ✅ Acceptable if credentials are secured |
| Command-line argument | ✅ Acceptable for local development |
| Hard-coded | ❌ Never hard-code connection strings |

[↑ Back to top](#table-of-contents)

---

## Factories

### MigrationDbContextFactory.cs

```csharp
public abstract class MigrationDbContextFactory<TContext> : IDesignTimeDbContextFactory<TContext>
    where TContext : DbContext
{
    protected virtual string? MigrationsAssembly => GetType().Assembly.GetName().Name;

    public TContext CreateDbContext(string[] args)
    {
        var optionsBuilder = new DbContextOptionsBuilder<TContext>();
        ConfigureOptions(optionsBuilder);
        return CreateContext(optionsBuilder.Options);
    }

    protected virtual string GetConnectionString()
    {
        var connectionString = Environment.GetEnvironmentVariable("HONEYDRUNK_MIGRATION_CONNECTION");

        if (string.IsNullOrEmpty(connectionString))
        {
            throw new InvalidOperationException(
                "Migration connection string not found. " +
                "Set the HONEYDRUNK_MIGRATION_CONNECTION environment variable or override GetConnectionString().");
        }

        return connectionString;
    }

    protected virtual void ConfigureOptions(DbContextOptionsBuilder<TContext> optionsBuilder)
    {
        var connectionString = GetConnectionString();

        optionsBuilder.UseSqlServer(connectionString, sqlOptions =>
        {
            if (!string.IsNullOrEmpty(MigrationsAssembly))
            {
                sqlOptions.MigrationsAssembly(MigrationsAssembly);
            }
        });
    }

    protected abstract TContext CreateContext(DbContextOptions<TContext> options);
}
```

#### Purpose

Base class for design-time DbContext factories. Required by EF Core tooling to create DbContext instances without a running application.

#### Key Features

| Feature | Description |
|---------|-------------|
| Environment variable | Default implementation reads from `HONEYDRUNK_MIGRATION_CONNECTION` |
| Migrations assembly | Property for specifying migrations assembly |
| Default SQL Server | Base implementation uses SQL Server; override `ConfigureOptions` for other providers |

#### Design Notes

- **Default provider is SQL Server.** The base `ConfigureOptions` implementation uses `UseSqlServer()`. Override this method for PostgreSQL or other providers.
- **Design-time dependencies.** The `CreateContext` implementation may need to provide stub implementations for constructor dependencies (`ITenantAccessor`, `IDataDiagnosticsContext`). These stubs are design-time only and do not represent valid runtime state.

#### Usage Example

**Step 1: Create a migration factory in your migrations project**

```csharp
// In your migrations project (e.g., MyApp.Migrations)
public class AppDbContextFactory : MigrationDbContextFactory<AppDbContext>
{
    protected override void ConfigureOptions(DbContextOptionsBuilder<AppDbContext> optionsBuilder)
    {
        var connectionString = GetConnectionString();
        
        optionsBuilder.UseSqlServer(connectionString, sqlOptions =>
        {
            if (!string.IsNullOrEmpty(MigrationsAssembly))
            {
                sqlOptions.MigrationsAssembly(MigrationsAssembly);
            }
        });
    }

    protected override AppDbContext CreateContext(DbContextOptions<AppDbContext> options)
    {
        // Design-time stubs—not valid for runtime use
        return new AppDbContext(
            options,
            new DesignTimeTenantAccessor(),
            new DesignTimeDiagnosticsContext());
    }
}

// Design-time stubs (not for runtime use)
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

**Step 2: Set environment variable**

```bash
# Windows PowerShell
$env:HONEYDRUNK_MIGRATION_CONNECTION = "Server=localhost;Database=AppDb;Trusted_Connection=true;TrustServerCertificate=true"

# Linux/macOS
export HONEYDRUNK_MIGRATION_CONNECTION="Server=localhost;Database=AppDb;Trusted_Connection=true;TrustServerCertificate=true"
```

**Step 3: Run EF Core commands**

```bash
# Add a migration
dotnet ef migrations add InitialCreate \
  --project MyApp.Migrations \
  --startup-project MyApp.Migrations

# Apply migrations
dotnet ef database update \
  --project MyApp.Migrations \
  --startup-project MyApp.Migrations
```

[↑ Back to top](#table-of-contents)

---

## Helpers

### MigrationRunner.cs

```csharp
public static class MigrationRunner
{
    public static async Task ApplyMigrationsAsync<TContext>(
        TContext context,
        CancellationToken cancellationToken = default)
        where TContext : DbContext
    {
        ArgumentNullException.ThrowIfNull(context);
        await context.Database.MigrateAsync(cancellationToken).ConfigureAwait(false);
    }

    public static async Task<IEnumerable<string>> GetPendingMigrationsAsync<TContext>(
        TContext context,
        CancellationToken cancellationToken = default)
        where TContext : DbContext
    {
        ArgumentNullException.ThrowIfNull(context);
        return await context.Database.GetPendingMigrationsAsync(cancellationToken).ConfigureAwait(false);
    }

    public static async Task<IEnumerable<string>> GetAppliedMigrationsAsync<TContext>(
        TContext context,
        CancellationToken cancellationToken = default)
        where TContext : DbContext
    {
        ArgumentNullException.ThrowIfNull(context);
        return await context.Database.GetAppliedMigrationsAsync(cancellationToken).ConfigureAwait(false);
    }

    public static async Task<bool> HasPendingMigrationsAsync<TContext>(
        TContext context,
        CancellationToken cancellationToken = default)
        where TContext : DbContext
    {
        ArgumentNullException.ThrowIfNull(context);
        var pending = await context.Database.GetPendingMigrationsAsync(cancellationToken).ConfigureAwait(false);
        return pending.Any();
    }

    public static async Task EnsureDatabaseAsync<TContext>(
        TContext context,
        CancellationToken cancellationToken = default)
        where TContext : DbContext
    {
        ArgumentNullException.ThrowIfNull(context);
        await context.Database.EnsureCreatedAsync(cancellationToken).ConfigureAwait(false);
    }
}
```

#### Purpose

Static helper methods wrapping EF Core migration APIs. These are thin wrappers providing a consistent surface for migration operations.

#### Method Reference

| Method | Description |
|--------|-------------|
| `ApplyMigrationsAsync` | Apply all pending migrations (**use with caution**) |
| `GetPendingMigrationsAsync` | List migrations not yet applied |
| `GetAppliedMigrationsAsync` | List migrations already applied |
| `HasPendingMigrationsAsync` | Check if any migrations are pending |
| `EnsureDatabaseAsync` | Create database without migrations (**dev/test only**) |

#### Design Notes

- **No usage constraints.** These methods are callable from any context—CI/CD, development startup, or production runtime. The API does not enforce usage boundaries.
- **`EnsureDatabaseAsync` caution.** This method uses `EnsureCreatedAsync` which bypasses migrations entirely and can cause schema drift. Use only for development or testing scenarios where migrations are not needed.

#### Usage Examples

**Check pending migrations in CI/CD job:**

```csharp
public async Task<int> RunMigrationCheck()
{
    await using var context = _factory.CreateDbContext([]);
    
    var pending = await MigrationRunner.GetPendingMigrationsAsync(context);
    
    foreach (var migration in pending)
    {
        Console.WriteLine($"Pending: {migration}");
    }
    
    // Exit code convention—verify CI system interprets as expected
    return pending.Count > 0 ? 1 : 0;
}
```

**Apply migrations in CI/CD job (non-production):**

```csharp
public async Task ApplyMigrations()
{
    await using var context = _factory.CreateDbContext([]);
    
    Console.WriteLine("Checking for pending migrations...");
    
    var pending = await MigrationRunner.GetPendingMigrationsAsync(context);
    
    if (pending.Count > 0)
    {
        Console.WriteLine($"Applying {pending.Count} migrations...");
        Console.WriteLine("WARNING: Auto-applying migrations. Review migration scripts for destructive operations.");
        
        await MigrationRunner.ApplyMigrationsAsync(context);
        
        Console.WriteLine("Migrations applied.");
    }
    else
    {
        Console.WriteLine("No pending migrations.");
    }
}
```

**Development startup (environment-gated):**

```csharp
// Environment-gated startup migration—use only in development
var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    using var scope = app.Services.CreateScope();
    var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    
    if (await MigrationRunner.HasPendingMigrationsAsync(context))
    {
        Console.WriteLine("Development: Applying pending migrations...");
        await MigrationRunner.ApplyMigrationsAsync(context);
    }
}

app.Run();
```

> **Caution:** Runtime migration application, even environment-gated, bypasses review processes. Consider whether this is appropriate for your environment.

[↑ Back to top](#table-of-contents)

---

## Workflows

### Creating Migrations

```bash
# 1. Set connection string
$env:HONEYDRUNK_MIGRATION_CONNECTION = "Server=localhost;Database=AppDb;..."

# 2. Navigate to solution root
cd /path/to/solution

# 3. Add migration
dotnet ef migrations add InitialCreate \
  --project src/MyApp.Migrations \
  --startup-project src/MyApp.Migrations \
  --output-dir Migrations

# 4. Review generated migration before committing
code src/MyApp.Migrations/Migrations/*_InitialCreate.cs
```

### Applying Migrations

**Development:**

```bash
# Apply all pending migrations
dotnet ef database update \
  --project src/MyApp.Migrations \
  --startup-project src/MyApp.Migrations
```

**Production (recommended approach):**

```bash
# Generate idempotent SQL script for review
dotnet ef migrations script \
  --project src/MyApp.Migrations \
  --startup-project src/MyApp.Migrations \
  --idempotent \
  --output migrations.sql

# Review script for destructive operations
# Apply via database tooling after review
sqlcmd -S server -d database -i migrations.sql
```

### CI/CD Integration

**GitHub Actions example:**

```yaml
name: Apply Migrations

on:
  workflow_dispatch:
    inputs:
      environment:
        description: 'Target environment'
        required: true
        type: choice
        options:
          - staging
          - production

jobs:
  migrate:
    runs-on: ubuntu-latest
    environment: ${{ inputs.environment }}
    
    steps:
      - uses: actions/checkout@v4
      - uses: actions/setup-dotnet@v4
        with:
          dotnet-version: '10.0.x'
      - name: Install EF Core tools
        run: dotnet tool install --global dotnet-ef
      - name: Generate migration script
        env:
          HONEYDRUNK_MIGRATION_CONNECTION: ${{ secrets.DATABASE_CONNECTION_STRING }}
        run: |
          dotnet ef migrations script \
            --project src/MyApp.Migrations \
            --startup-project src/MyApp.Migrations \
            --idempotent \
            --output migrations.sql
      - name: Upload migration script
        uses: actions/upload-artifact@v4
        with:
          name: migration-script
          path: migrations.sql
      # Apply via separate reviewed step or manual database deployment
```

**Azure DevOps example:**

```yaml
stages:
  - stage: GenerateMigrationScript
    jobs:
      - job: GenerateScript
        pool:
          vmImage: 'ubuntu-latest'
        steps:
          - task: UseDotNet@2
            inputs:
              version: '10.0.x'
          - script: dotnet tool install --global dotnet-ef
            displayName: 'Install EF Core tools'
          - script: |
              dotnet ef migrations script \
                --project src/MyApp.Migrations \
                --startup-project src/MyApp.Migrations \
                --idempotent \
                --output $(Build.ArtifactStagingDirectory)/migrations.sql
            displayName: 'Generate migration script'
            env:
              HONEYDRUNK_MIGRATION_CONNECTION: $(DatabaseConnectionString)
          - publish: $(Build.ArtifactStagingDirectory)/migrations.sql
            artifact: MigrationScript
```

[↑ Back to top](#table-of-contents)

---

## Summary

The Migrations package provides migration tooling conventions:

- **MigrationDbContextFactory<T>** - Abstract base factory for `dotnet ef` commands (provider-neutral)
- **MigrationRunner** - Programmatic migration helpers (thin wrappers over EF Core APIs)
- **Environment-based** - Default connection string from environment variable

Key guidance:
- Prefer idempotent SQL scripts for production deployments
- Review migrations for destructive operations before applying
- Runtime migration application is possible but carries risk
- These are conventions, not enforced constraints

---

[← Back to File Guide](FILE_GUIDE.md) | [↑ Back to top](#table-of-contents)
