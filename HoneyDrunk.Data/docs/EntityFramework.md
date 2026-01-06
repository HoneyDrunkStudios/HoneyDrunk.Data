# 🗃️ EntityFramework - EF Core Provider Implementation

[← Back to File Guide](FILE_GUIDE.md)

---

## Table of Contents

- [Overview](#overview)
- [Design Constraints](#design-constraints)
- [Context](#context)
  - [HoneyDrunkDbContext.cs](#honeydrunkdbcontextcs)
- [Repositories](#repositories)
  - [EfRepository.cs](#efrepositorycs)
- [Transactions](#transactions)
  - [EfTransactionScope.cs](#eftransactionscopecs)
  - [EfUnitOfWork.cs](#efunitofworkcs)
  - [EfUnitOfWorkFactory.cs](#efunitofworkfactorycs)
- [Diagnostics](#diagnostics)
  - [CorrelationCommandInterceptor.cs](#correlationcommandinterceptorcs)
  - [DbContextHealthContributor.cs](#dbcontexthealthcontributorcs)
- [Modeling](#modeling)
  - [ModelBuilderConventions.cs](#modelbuilderconventionscs)
- [Registration](#registration)
  - [EfDataOptions.cs](#efdataoptionscs)
  - [ServiceCollectionExtensions.cs](#servicecollectionextensionscs)

---

## Overview

Entity Framework Core implementation of the data abstractions. This package provides concrete implementations for repositories, unit of work, and diagnostic interceptors.

**Location:** `HoneyDrunk.Data.EntityFramework/`

This package provides:
- Base DbContext with tenant identity and correlation context access
- Generic repository pattern implementation wrapping DbSet
- Unit of work with repository caching
- SQL command interceptor for correlation tagging (relational providers)
- Database health contributor

---

## Design Constraints

### Relational EF Core Providers

This package targets **relational** EF Core providers. It depends on `Microsoft.EntityFrameworkCore.Relational` for interceptor support.

| Dependency | Status | Notes |
|------------|--------|-------|
| `Microsoft.EntityFrameworkCore` | ✅ Required | Core EF abstractions |
| `Microsoft.EntityFrameworkCore.Relational` | ✅ Required | Relational extensions, interceptors |
| `Microsoft.EntityFrameworkCore.SqlServer` | ❌ Forbidden | Provider concern |
| Other database providers | ❌ Forbidden | Provider concern |

> **Rule:** If it's specific to SQL Server, PostgreSQL, or another database, it belongs in a provider package.

> **Note:** Non-relational EF Core providers (Cosmos, etc.) may not support all features, particularly command interception.

### DbContext Lifetime

This package is designed for **scoped DbContext lifetime** (typically per-request in web applications). For background jobs or batch processing, use `IDbContextFactory<T>` via `EfUnitOfWorkFactory` to create contexts with controlled lifetime.

[↑ Back to top](#table-of-contents)

---

## Context

### HoneyDrunkDbContext.cs

```csharp
public abstract class HoneyDrunkDbContext : DbContext
{
    private readonly ITenantAccessor _tenantAccessor;
    private readonly IDataDiagnosticsContext _diagnosticsContext;

    protected HoneyDrunkDbContext(
        DbContextOptions options,
        ITenantAccessor tenantAccessor,
        IDataDiagnosticsContext diagnosticsContext)
        : base(options)
    {
        ArgumentNullException.ThrowIfNull(tenantAccessor);
        ArgumentNullException.ThrowIfNull(diagnosticsContext);

        _tenantAccessor = tenantAccessor;
        _diagnosticsContext = diagnosticsContext;
    }

    protected TenantId CurrentTenantId => _tenantAccessor.GetCurrentTenantId();
    
    protected string? CorrelationId => _diagnosticsContext.CorrelationId;

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
        ApplyConfigurations(modelBuilder);
    }

    protected virtual void ApplyConfigurations(ModelBuilder modelBuilder)
    {
        // Override to apply entity configurations
    }
}
```

#### Purpose

Base DbContext with tenant identity and correlation context access. Extend this class for your application's DbContext.

#### Features

| Feature | Description |
|---------|-------------|
| Tenant identity access | `CurrentTenantId` property exposes tenant from Kernel context |
| Correlation access | `CorrelationId` property for telemetry enrichment |
| Configuration hook | `ApplyConfigurations` for entity configuration |

#### Design Notes

- **No automatic tenant filtering.** The base class exposes `CurrentTenantId` but does not apply query filters. Applications must implement tenant filtering explicitly via global query filters or query predicates.
- **No enforcement.** Using this base class does not guarantee tenant isolation—that is an application responsibility.

#### Usage Example

```csharp
public class AppDbContext : HoneyDrunkDbContext
{
    public AppDbContext(
        DbContextOptions<AppDbContext> options,
        ITenantAccessor tenantAccessor,
        IDataDiagnosticsContext diagnosticsContext)
        : base(options, tenantAccessor, diagnosticsContext)
    {
    }

    public DbSet<Order> Orders => Set<Order>();
    public DbSet<Customer> Customers => Set<Customer>();

    protected override void ApplyConfigurations(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(AppDbContext).Assembly);
        
        // Application must explicitly configure tenant filters per entity type
        // Example for a specific entity (not a generic loop):
        modelBuilder.Entity<Order>()
            .HasQueryFilter(o => o.TenantId == CurrentTenantId.Value);
        
        modelBuilder.Entity<Customer>()
            .HasQueryFilter(c => c.TenantId == CurrentTenantId.Value);
    }
}
```

**Note:** EF Core query filters require strongly-typed expression trees per entity type. Generic loops with casts do not translate correctly. Configure filters explicitly per entity or use a source generator / convention approach.

[↑ Back to top](#table-of-contents)

---

## Repositories

### EfRepository.cs

```csharp
public class EfRepository<TEntity, TContext> : IRepository<TEntity>
    where TEntity : class
    where TContext : DbContext
{
    private readonly TContext _context;
    private readonly DbSet<TEntity> _dbSet;

    public EfRepository(TContext context)
    {
        ArgumentNullException.ThrowIfNull(context);
        _context = context;
        _dbSet = context.Set<TEntity>();
    }

    public virtual ValueTask<TEntity?> FindByIdAsync(object id, CancellationToken ct = default)
        => _dbSet.FindAsync([id], ct);

    public virtual async Task<IReadOnlyList<TEntity>> FindAsync(
        Expression<Func<TEntity, bool>> predicate,
        CancellationToken ct = default)
        => await _dbSet.Where(predicate).ToListAsync(ct);

    public virtual Task<TEntity?> FindOneAsync(
        Expression<Func<TEntity, bool>> predicate,
        CancellationToken ct = default)
        => _dbSet.FirstOrDefaultAsync(predicate, ct);

    public virtual Task<bool> ExistsAsync(
        Expression<Func<TEntity, bool>> predicate,
        CancellationToken ct = default)
        => _dbSet.AnyAsync(predicate, ct);

    public virtual Task<int> CountAsync(
        Expression<Func<TEntity, bool>>? predicate = null,
        CancellationToken ct = default)
        => predicate is null ? _dbSet.CountAsync(ct) : _dbSet.CountAsync(predicate, ct);

    public virtual async Task AddAsync(TEntity entity, CancellationToken ct = default)
        => await _dbSet.AddAsync(entity, ct);

    public virtual async Task AddRangeAsync(IEnumerable<TEntity> entities, CancellationToken ct = default)
        => await _dbSet.AddRangeAsync(entities, ct);

    public virtual void Update(TEntity entity)
        => _dbSet.Update(entity);

    public virtual void Remove(TEntity entity)
        => _dbSet.Remove(entity);

    public virtual void RemoveRange(IEnumerable<TEntity> entities)
        => _dbSet.RemoveRange(entities);
}
```

#### Purpose

EF Core implementation of the repository pattern. Thin wrapper over `DbSet<T>` implementing `IRepository<T>`.

#### Design Notes

- **Coordination wrapper, not a safety boundary.** The repository provides a consistent interface but does not enforce tenant filtering, validation, or other policies.
- **Access through IUnitOfWork.** For consistency with the abstraction layer, access repositories via `IUnitOfWork<TContext>.Repository<T>()` rather than injecting `EfRepository<T, TContext>` directly.

#### Method Mapping

| Interface Method | EF Core Implementation |
|------------------|------------------------|
| `FindByIdAsync` | `DbSet.FindAsync` |
| `FindAsync` | `DbSet.Where().ToListAsync` |
| `FindOneAsync` | `DbSet.FirstOrDefaultAsync` |
| `ExistsAsync` | `DbSet.AnyAsync` |
| `CountAsync` | `DbSet.CountAsync` |
| `AddAsync` | `DbSet.AddAsync` |
| `AddRangeAsync` | `DbSet.AddRangeAsync` |
| `Update` | `DbSet.Update` |
| `Remove` | `DbSet.Remove` |
| `RemoveRange` | `DbSet.RemoveRange` |

#### Usage Example

```csharp
// Access through IUnitOfWork (preferred)
var repo = _unitOfWork.Repository<Order>();
var order = await repo.FindByIdAsync(orderId);
```

[↑ Back to top](#table-of-contents)

---

## Transactions

### EfTransactionScope.cs

```csharp
public sealed class EfTransactionScope : ITransactionScope
{
    private readonly IDbContextTransaction _transaction;

    public EfTransactionScope(IDbContextTransaction transaction)
    {
        ArgumentNullException.ThrowIfNull(transaction);
        _transaction = transaction;
    }

    public Guid TransactionId => _transaction.TransactionId;

    public Task CommitAsync(CancellationToken ct = default)
        => _transaction.CommitAsync(ct);

    public Task RollbackAsync(CancellationToken ct = default)
        => _transaction.RollbackAsync(ct);

    public async ValueTask DisposeAsync()
        => await _transaction.DisposeAsync();
}
```

#### Purpose

Wraps EF Core's `IDbContextTransaction` with the `ITransactionScope` interface.

#### Design Notes

Transaction scope is **connection-local and context-local**. It does not provide distributed transaction support.

#### Usage Example

```csharp
await using var scope = await _unitOfWork.BeginTransactionAsync(ct);

try
{
    // Multiple save operations within transaction
    await _unitOfWork.SaveChangesAsync(ct);
    
    // External call that might fail
    await _externalService.NotifyAsync(ct);
    
    await scope.CommitAsync(ct);
}
catch
{
    await scope.RollbackAsync(ct);
    throw;
}
```

[↑ Back to top](#table-of-contents)

---

### EfUnitOfWork.cs

```csharp
public sealed class EfUnitOfWork<TContext> : IUnitOfWork<TContext>
    where TContext : DbContext
{
    private readonly TContext _context;
    private readonly Dictionary<Type, object> _repositories;

    public EfUnitOfWork(TContext context)
    {
        ArgumentNullException.ThrowIfNull(context);
        _context = context;
        _repositories = new Dictionary<Type, object>();
    }

    public bool HasPendingChanges => _context.ChangeTracker.HasChanges();

    public IRepository<TEntity> Repository<TEntity>()
        where TEntity : class
    {
        var type = typeof(TEntity);
        if (!_repositories.TryGetValue(type, out var repo))
        {
            repo = new EfRepository<TEntity, TContext>(_context);
            _repositories[type] = repo;
        }
        return (IRepository<TEntity>)repo;
    }

    public Task<int> SaveChangesAsync(CancellationToken ct = default)
        => _context.SaveChangesAsync(ct);

    public async Task<ITransactionScope> BeginTransactionAsync(CancellationToken ct = default)
    {
        var transaction = await _context.Database.BeginTransactionAsync(ct);
        return new EfTransactionScope(transaction);
    }

    public async ValueTask DisposeAsync()
        => await _context.DisposeAsync();
}
```

#### Purpose

EF Core implementation of the unit of work pattern. Coordinates changes across multiple repositories and provides transaction support.

#### Features

| Feature | Description |
|---------|-------------|
| Repository caching | Repositories cached per entity type for the lifetime of the unit of work |
| Change tracking | `HasPendingChanges` reflects DbContext state |
| Transaction support | `BeginTransactionAsync` creates explicit transactions |
| Context-local save | `SaveChangesAsync` persists all tracked changes |

#### Design Notes

- **Not thread-safe.** `DbContext` is not thread-safe, and neither is this unit of work. Do not share across threads.
- **Context-local atomicity.** `SaveChangesAsync` is atomic within the scope of a single `DbContext` and its underlying connection. Multi-command saves depend on provider transaction behavior.

#### Usage Example

```csharp
public class OrderService
{
    private readonly IUnitOfWork<AppDbContext> _unitOfWork;

    public async Task CreateOrderWithAuditAsync(Order order, CancellationToken ct)
    {
        var orders = _unitOfWork.Repository<Order>();
        var audit = _unitOfWork.Repository<AuditLog>();

        await orders.AddAsync(order, ct);
        await audit.AddAsync(new AuditLog
        {
            Action = "OrderCreated",
            EntityId = order.Id.ToString()
        }, ct);

        // Changes saved within single DbContext transaction
        await _unitOfWork.SaveChangesAsync(ct);
    }
}
```

[↑ Back to top](#table-of-contents)

---

### EfUnitOfWorkFactory.cs

```csharp
public sealed class EfUnitOfWorkFactory<TContext> : IUnitOfWorkFactory
    where TContext : DbContext
{
    private readonly IDbContextFactory<TContext> _contextFactory;

    public EfUnitOfWorkFactory(IDbContextFactory<TContext> contextFactory)
    {
        ArgumentNullException.ThrowIfNull(contextFactory);
        _contextFactory = contextFactory;
    }

    public IUnitOfWork Create()
    {
        var context = _contextFactory.CreateDbContext();
        return new EfUnitOfWork<TContext>(context);
    }
}
```

#### Purpose

Factory for creating unit of work instances with controlled lifetime. Uses `IDbContextFactory<T>` to create fresh DbContext instances, making it safe for background jobs, batch processing, or any scenario requiring non-scoped DbContext lifetime.

#### Design Notes

- **Uses `IDbContextFactory<T>`.** Each call to `Create()` produces a new DbContext via the factory, ensuring proper lifetime management.
- **Singleton registration.** The factory itself is registered as singleton; the DbContexts it creates are short-lived and disposed by the caller.
- **Returns `IUnitOfWork`.** Note: Returns non-generic `IUnitOfWork`, not `IUnitOfWork<TContext>`. Cast if typed repository access is needed.

#### Usage Example

```csharp
public class BatchProcessor : BackgroundService
{
    private readonly IUnitOfWorkFactory _factory;

    public BatchProcessor(IUnitOfWorkFactory factory)
    {
        _factory = factory;
    }

    protected override async Task ExecuteAsync(CancellationToken ct)
    {
        while (!ct.IsCancellationRequested)
        {
            var batch = await GetNextBatchAsync(ct);
            
            // Each batch gets its own unit of work with fresh DbContext
            await using var unitOfWork = _factory.Create();
            
            // If you need typed repository access, cast:
            // var typedUow = (IUnitOfWork<AppDbContext>)unitOfWork;
            
            foreach (var item in batch)
            {
                await ProcessItemAsync(unitOfWork, item, ct);
            }
            
            await unitOfWork.SaveChangesAsync(ct);
        }
    }
}
```

[↑ Back to top](#table-of-contents)

---

## Diagnostics

### CorrelationCommandInterceptor.cs

```csharp
public sealed class CorrelationCommandInterceptor : DbCommandInterceptor
{
    private readonly IDataDiagnosticsContext _diagnosticsContext;

    public CorrelationCommandInterceptor(IDataDiagnosticsContext diagnosticsContext)
    {
        ArgumentNullException.ThrowIfNull(diagnosticsContext);
        _diagnosticsContext = diagnosticsContext;
    }

    public override InterceptionResult<int> NonQueryExecuting(
        DbCommand command,
        CommandEventData eventData,
        InterceptionResult<int> result)
    {
        AddCorrelationComment(command);
        return base.NonQueryExecuting(command, eventData, result);
    }

    // ... similar overrides for other command types

    private void AddCorrelationComment(DbCommand command)
    {
        var correlationId = _diagnosticsContext.CorrelationId;
        if (string.IsNullOrEmpty(correlationId)) return;

        var sanitizedId = SanitizeForSqlComment(correlationId);
        command.CommandText = $"/* correlation:{sanitizedId} */\n{command.CommandText}";
    }

    private static string SanitizeForSqlComment(string value)
    {
        return value
            .Replace("*/", string.Empty, StringComparison.Ordinal)
            .Replace("/*", string.Empty, StringComparison.Ordinal)
            .Replace("--", string.Empty, StringComparison.Ordinal)
            .Replace("\n", string.Empty, StringComparison.Ordinal)
            .Replace("\r", string.Empty, StringComparison.Ordinal);
    }
}
```

#### Purpose

Adds Grid correlation IDs to SQL commands as comments. Enables correlation between application traces and database activity.

#### SQL Output

```sql
/* correlation:01JXYZ... */
SELECT [o].[Id], [o].[CustomerId], [o].[Total]
FROM [Orders] AS [o]
WHERE [o].[Id] = @p0
```

#### Design Notes

- **Query plan caching:** SQL comments generally do not affect query plan caching in most databases, but behavior varies by engine and configuration. Test with your specific database.
- **Visibility:** Correlation IDs may appear in profiler traces, slow query logs, or monitoring tools depending on database configuration. Not all logging modes preserve comments.
- **Registration required:** The interceptor must be attached to DbContext options via `AddInterceptors()`. DI registration alone does not wire it into EF.

#### Security

Input is sanitized to prevent SQL comment injection.

[↑ Back to top](#table-of-contents)

---

### DbContextHealthContributor.cs

```csharp
public class DbContextHealthContributor<TContext> : IDataHealthContributor
    where TContext : DbContext
{
    private readonly TContext _context;

    public DbContextHealthContributor(TContext context)
    {
        ArgumentNullException.ThrowIfNull(context);
        _context = context;
    }

    public string Name => $"Database ({typeof(TContext).Name})";

    public async ValueTask<DataHealthResult> CheckHealthAsync(CancellationToken ct = default)
    {
        try
        {
            await _context.Database.CanConnectAsync(ct).ConfigureAwait(false);
            return DataHealthResult.Healthy("Database connection successful");
        }
        catch (Exception ex)
        {
            return DataHealthResult.Unhealthy($"Database connection failed: {ex.Message}");
        }
    }
}
```

#### Purpose

Health contributor that verifies database connectivity. Invoked by host health system on demand.

#### Usage Example

```csharp
// Automatically registered with AddHoneyDrunkDataEntityFramework<T>()

// Health check endpoint
app.MapGet("/health", async (IEnumerable<IDataHealthContributor> contributors) =>
{
    var results = new List<object>();
    
    foreach (var contributor in contributors)
    {
        var result = await contributor.CheckHealthAsync();
        results.Add(new { contributor.Name, result.Status, result.Description });
    }
    
    return Results.Ok(results);
});
```

[↑ Back to top](#table-of-contents)

---

## Modeling

### ModelBuilderConventions.cs

```csharp
public static class ModelBuilderConventions
{
    public static ModelBuilder ApplySnakeCaseNamingConvention(this ModelBuilder modelBuilder)
    {
        foreach (var entity in modelBuilder.Model.GetEntityTypes())
        {
            entity.SetTableName(ToSnakeCase(entity.GetTableName()));

            foreach (var property in entity.GetProperties())
            {
                property.SetColumnName(ToSnakeCase(property.GetColumnName()));
            }

            foreach (var key in entity.GetKeys())
            {
                key.SetName(ToSnakeCase(key.GetName()));
            }

            foreach (var fk in entity.GetForeignKeys())
            {
                fk.SetConstraintName(ToSnakeCase(fk.GetConstraintName()));
            }

            foreach (var index in entity.GetIndexes())
            {
                index.SetDatabaseName(ToSnakeCase(index.GetDatabaseName()));
            }
        }

        return modelBuilder;
    }

    public static ModelBuilder ApplyDefaultStringLength(
        this ModelBuilder modelBuilder,
        int maxLength = 256)
    {
        foreach (var property in modelBuilder.Model.GetEntityTypes()
            .SelectMany(t => t.GetProperties())
            .Where(p => p.ClrType == typeof(string) && p.GetMaxLength() is null))
        {
            property.SetMaxLength(maxLength);
        }

        return modelBuilder;
    }

    private static string? ToSnakeCase(string? name)
    {
        if (string.IsNullOrEmpty(name)) return name;

        var builder = new StringBuilder();
        for (var i = 0; i < name.Length; i++)
        {
            var c = name[i];
            if (char.IsUpper(c) && i > 0)
            {
                builder.Append('_');
            }
            builder.Append(char.ToLowerInvariant(c));
        }
        return builder.ToString();
    }
}
```

#### Purpose

Provides **optional** model builder extension methods for naming conventions. These are opt-in utilities, not defaults.

#### Available Conventions

| Method | Description |
|--------|-------------|
| `ApplySnakeCaseNamingConvention` | Converts PascalCase to snake_case (optional, opt-in) |
| `ApplyDefaultStringLength` | Sets default max length for string properties without explicit length |

#### Design Notes

- **Not applied by default.** Applications must explicitly call these methods if desired.
- **Schema impact.** These conventions affect table names, column names, and index names. Evaluate compatibility with your database ecosystem and existing schemas.
- **String length.** `ApplyDefaultStringLength` modifies schema characteristics. Use deliberately—it affects indexing and storage.

#### Usage Example

```csharp
protected override void OnModelCreating(ModelBuilder modelBuilder)
{
    base.OnModelCreating(modelBuilder);

    // Optional: apply snake_case naming
    modelBuilder.ApplySnakeCaseNamingConvention();
    
    // Optional: set default string length
    modelBuilder.ApplyDefaultStringLength(256);
}
```

#### Result

```
// Before: OrderDetails
// After: order_details

// Before: CustomerId
// After: customer_id
```

[↑ Back to top](#table-of-contents)

---

## Registration

### EfDataOptions.cs

```csharp
public sealed class EfDataOptions
{
    public bool EnableCorrelationInterceptor { get; set; } = true;
    public bool RegisterHealthContributors { get; set; } = true;
}
```

#### Purpose

Configuration options specific to the EF Core provider.

#### Properties

| Property | Default | Description |
|----------|---------|-------------|
| `EnableCorrelationInterceptor` | `true` | Register correlation interceptor (must also be added to DbContext options) |
| `RegisterHealthContributors` | `true` | Register database health contributor |

[↑ Back to top](#table-of-contents)

---

### ServiceCollectionExtensions.cs

```csharp
public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddHoneyDrunkDataEntityFramework<TContext>(
        this IServiceCollection services,
        Action<DbContextOptionsBuilder> configureOptions,
        Action<EfDataOptions>? configureEfOptions = null)
        where TContext : DbContext
    {
        var efOptions = new EfDataOptions();
        configureEfOptions?.Invoke(efOptions);

        services.AddDbContext<TContext>((sp, options) =>
        {
            configureOptions(options);
            
            // Wire interceptor into EF pipeline if enabled
            if (efOptions.EnableCorrelationInterceptor)
            {
                var interceptor = sp.GetRequiredService<CorrelationCommandInterceptor>();
                options.AddInterceptors(interceptor);
            }
        });
        
        services.AddScoped<IUnitOfWork<TContext>, EfUnitOfWork<TContext>>();

        if (efOptions.EnableCorrelationInterceptor)
        {
            services.AddScoped<CorrelationCommandInterceptor>();
        }

        if (efOptions.RegisterHealthContributors)
        {
            services.AddScoped<IDataHealthContributor, DbContextHealthContributor<TContext>>();
        }

        return services;
    }
}
```

#### Purpose

Dependency injection registration for the EF Core provider.

#### Design Notes

- **Interceptor wiring.** When `EnableCorrelationInterceptor` is true, the interceptor is both registered in DI and attached to DbContext options via `AddInterceptors()`.
- **Provider configuration.** The `configureOptions` callback is where you configure your database provider (e.g., `UseSqlServer`, `UseNpgsql`). Provider configuration belongs in the consuming application, not this package.

#### Usage Example

```csharp
builder.Services.AddHoneyDrunkData();

builder.Services.AddHoneyDrunkDataEntityFramework<AppDbContext>(
    options => 
    {
        // Provider configuration happens here (in consuming application)
        // Example with SQL Server (requires HoneyDrunk.Data.SqlServer or direct provider reference):
        options.UseSqlServer(connectionString);
    },
    efOptions =>
    {
        efOptions.EnableCorrelationInterceptor = true;
        efOptions.RegisterHealthContributors = true;
    });
```

[↑ Back to top](#table-of-contents)

---

## Summary

The EntityFramework package provides EF Core implementation of data abstractions:

- **HoneyDrunkDbContext** - Base context with tenant identity and correlation context access (no automatic filtering)
- **EfRepository<T>** - Thin wrapper over DbSet implementing IRepository<T>
- **EfUnitOfWork<T>** - Unit of work with repository caching (not thread-safe)
- **CorrelationCommandInterceptor** - SQL correlation tagging (relational providers, requires DbContext options wiring)
- **DbContextHealthContributor** - Database connectivity health contributor
- **ModelBuilderConventions** - Optional naming convention utilities

This package targets **relational EF Core providers**. Non-relational providers may not support all features.

---

[← Back to File Guide](FILE_GUIDE.md) | [↑ Back to top](#table-of-contents)
