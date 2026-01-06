# HoneyDrunk.Data.EntityFramework

Entity Framework Core provider implementation for HoneyDrunk.Data. This package is provider-agnostic—database-specific configuration belongs in specialization packages (e.g., `HoneyDrunk.Data.SqlServer`).

## Purpose

Implements the persistence abstractions using Entity Framework Core. Provides:

- Base DbContext with tenant identity and correlation context access (not automatic filtering)
- Repository implementation wrapping DbSet
- EF-backed unit-of-work abstraction
- Command interceptor for correlation tagging (relational providers, opt-in)
- Health contributor for database connectivity

## Allowed Dependencies

- `HoneyDrunk.Data.Abstractions` - The contracts this package implements
- `HoneyDrunk.Data` - The orchestration layer (**required**)
- `Microsoft.EntityFrameworkCore` and `Microsoft.EntityFrameworkCore.Relational`
- `Microsoft.Extensions.DependencyInjection.Abstractions`
- `HoneyDrunk.Standards` - Analyzer package only (PrivateAssets=all)

**Note:** Relational providers are required for diagnostics features (command interception). Non-relational EF Core providers may not support all features.

## What Must Never Be Added

- **No database-specific packages** - SQL Server, PostgreSQL, etc. belong in their respective projects
- **No transport or auth references** - HoneyDrunk.Transport and HoneyDrunk.Auth are not data concerns

## Namespace Layout

```
HoneyDrunk.Data.EntityFramework
├── Context/              # Base DbContext
│   └── HoneyDrunkDbContext.cs
├── Diagnostics/          # Interceptors and health
│   ├── CorrelationCommandInterceptor.cs
│   └── DbContextHealthContributor.cs
├── Modeling/             # Optional model conventions
│   └── ModelBuilderConventions.cs
├── Registration/         # DI extensions
│   ├── EfDataOptions.cs
│   └── ServiceCollectionExtensions.cs
├── Repositories/         # Repository implementation
│   └── EfRepository.cs
└── Transactions/         # UoW and transaction scope
    ├── EfTransactionScope.cs
    ├── EfUnitOfWork.cs
    └── EfUnitOfWorkFactory.cs
```

## Usage

```csharp
// Orchestration layer is required
services.AddHoneyDrunkData();

// Register EF Core provider (database configuration deferred to specialization package)
services.AddHoneyDrunkDataEntityFramework<MyDbContext>(
    options =>
    {
        // Database provider configured here or via specialization package
        // Example: options.UseSqlServer(...) requires HoneyDrunk.Data.SqlServer
    },
    efOptions =>
    {
        efOptions.EnableCorrelationInterceptor = true;  // Requires relational provider
        efOptions.RegisterHealthContributors = true;
    });

// Or use a specialization package (recommended):
// services.AddHoneyDrunkDataSqlServer<MyDbContext>(...);
```

## Creating a DbContext

```csharp
public class MyDbContext : HoneyDrunkDbContext
{
    public MyDbContext(
        DbContextOptions<MyDbContext> options,
        ITenantAccessor tenantAccessor,
        IDataDiagnosticsContext diagnosticsContext)
        : base(options, tenantAccessor, diagnosticsContext)
    {
    }

    public DbSet<MyEntity> MyEntities => Set<MyEntity>();

    protected override void ApplyConfigurations(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(MyDbContext).Assembly);
        
        // Tenant filtering must be configured explicitly per entity
        // Example: modelBuilder.Entity<MyEntity>().HasQueryFilter(e => e.TenantId == CurrentTenantId.Value);
    }
}
```

**Note:** `HoneyDrunkDbContext` exposes `CurrentTenantId` and `CorrelationId` but does not automatically apply tenant filtering. Applications must configure query filters explicitly.

## Using Repositories

```csharp
public class MyService
{
    private readonly IUnitOfWork<MyDbContext> _unitOfWork;

    public MyService(IUnitOfWork<MyDbContext> unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    public async Task CreateAsync(MyEntity entity, CancellationToken ct)
    {
        var repo = _unitOfWork.Repository<MyEntity>();
        await repo.AddAsync(entity, ct);
        await _unitOfWork.SaveChangesAsync(ct);  // Context-local save
    }
}
```

**Note:** `EfRepository<T>` implements `IRepository<T>` (which extends `IReadOnlyRepository<T>`). No separate read-only implementation exists—read-only access is a usage pattern, not a distinct type.

## Lifetime and Threading

- **Scoped lifetime expected.** This implementation assumes scoped DbContext lifetime (per-request in web applications).
- **Not thread-safe.** DbContext and EfUnitOfWork are not thread-safe; do not share across threads.
- **Factory for controlled lifetime.** Use `EfUnitOfWorkFactory` with `IDbContextFactory<T>` for background jobs or batch processing.

## Correlation Tracking

Correlation tagging is **opt-in and conditional**:

- Requires `EnableCorrelationInterceptor = true`
- Requires relational EF Core provider
- Requires diagnostics context to have a correlation ID

When all conditions are met, SQL commands include correlation comments:

```sql
/* correlation:01JXYZ... */
SELECT * FROM MyEntities WHERE Id = @p0
