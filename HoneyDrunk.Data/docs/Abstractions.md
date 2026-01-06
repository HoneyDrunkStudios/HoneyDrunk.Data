# 📋 Abstractions - Core Contracts and Types

[← Back to File Guide](FILE_GUIDE.md)

---

## Table of Contents

- [Overview](#overview)
- [Design Constraints](#design-constraints)
- [Tenancy](#tenancy)
  - [TenantId.cs](#tenantidcs)
  - [ITenantAccessor.cs](#itenantaccessorcs)
  - [ITenantResolutionStrategy.cs](#itenantresolutionstrategycs)
- [Repositories](#repositories)
  - [IReadOnlyRepository.cs](#ireadonlyrepositorycs)
  - [IRepository.cs](#irepositorycs)
- [Transactions](#transactions)
  - [ITransactionScope.cs](#itransactionscopecs)
  - [IUnitOfWork.cs](#iunitofworkcs)
  - [IUnitOfWorkFactory.cs](#iunitofworkfactorycs)
- [Diagnostics](#diagnostics)
  - [DataHealthStatus.cs](#datahealthstatuscs)
  - [DataHealthResult.cs](#datahealthresultcs)
  - [IDataHealthContributor.cs](#idatahealthcontributorcs)
  - [IDataDiagnosticsContext.cs](#idatadiagnosticscontextcs)

---

## Overview

Core abstractions and contracts for the HoneyDrunk persistence system. This package has **minimal external dependencies** (only `HoneyDrunk.Kernel.Abstractions`), making it suitable for defining contracts in shared libraries or domain projects.

**Location:** `HoneyDrunk.Data.Abstractions/`

The separation of abstractions allows consuming projects to depend only on contracts without pulling in runtime dependencies like Entity Framework Core or database providers.

---

## Design Constraints

### Contract-Only Package

Data Abstractions is a **contract definition package**, not an implementation.

| Dependency | Status | Notes |
|------------|--------|-------|
| `HoneyDrunk.Kernel.Abstractions` | ✅ Allowed | For context contracts |
| `Microsoft.EntityFrameworkCore` | ❌ Forbidden | Implementation concern |
| Database-specific types | ❌ Forbidden | Provider concern |

> **Rule:** If it requires a database connection, it doesn't belong here.

### Tenant-Aware by Convention

Abstractions support multi-tenant patterns. Tenant identity is available via `ITenantAccessor`, but **contracts do not enforce tenant usage**—application code is responsible for applying tenant context where needed.

### Async-First

All I/O operations are async. Synchronous overloads are not provided.

### Expression-Based Queries

Repository queries use `Expression<Func<T, bool>>` for deferred execution. **Note:** This design assumes LINQ-capable providers. Non-LINQ providers (Dapper, raw ADO.NET) would require expression translation or alternative contract implementations.

[↑ Back to top](#table-of-contents)

---

## Tenancy

### TenantId.cs

```csharp
public readonly record struct TenantId(string Value)
{
    public bool IsEmpty => string.IsNullOrWhiteSpace(Value);
    
    public static implicit operator string(TenantId tenantId) => tenantId.Value;
    public static TenantId FromString(string value) => new(value);
    
    public override string ToString() => Value ?? string.Empty;
}
```

#### Purpose

Typed tenant identifier that flows through the data layer. Uses a `record struct` for value semantics.

#### Design Notes

- **Implicit string conversion** is provided for convenience but weakens type safety. Prefer explicit `TenantId` usage in APIs where tenant identity is a domain concern.
- **Allocation characteristics** depend on usage patterns. Boxing (interface calls, dictionary keys) and `ToString()` may allocate. The struct itself avoids heap allocation for simple value passing.

#### Usage Example

```csharp
// Create from string
var tenantId = TenantId.FromString("tenant-123");

// Check if empty
if (tenantId.IsEmpty)
{
    throw new InvalidOperationException("Tenant context required");
}

// Implicit string conversion (use sparingly)
string connectionKey = tenantId;  // "tenant-123"

// Value equality
var other = TenantId.FromString("tenant-123");
Console.WriteLine(tenantId == other);  // true
```

[↑ Back to top](#table-of-contents)

---

### ITenantAccessor.cs

```csharp
public interface ITenantAccessor
{
    TenantId GetCurrentTenantId();
}
```

#### Purpose

Provides access to the current tenant identifier. Implementations extract tenant from ambient context (HTTP headers, message properties, etc.).

#### Design Notes

- Returning `default` (empty `TenantId`) does not prevent operations—consumers must check and enforce tenant requirements.
- The contract provides tenant *availability*, not tenant *enforcement*.

#### Usage Example

```csharp
public class OrderService
{
    private readonly ITenantAccessor _tenantAccessor;

    public async Task<Order?> GetOrderAsync(Guid orderId)
    {
        var tenantId = _tenantAccessor.GetCurrentTenantId();
        
        // Application code must enforce tenant requirement
        if (tenantId.IsEmpty)
        {
            throw new InvalidOperationException("Tenant context required");
        }
        
        // Application code must apply tenant filtering
        return await _repository.FindOneAsync(o => 
            o.Id == orderId && o.TenantId == tenantId.Value);
    }
}
```

[↑ Back to top](#table-of-contents)

---

### ITenantResolutionStrategy.cs

```csharp
public interface ITenantResolutionStrategy
{
    ValueTask<string> ResolveConnectionStringAsync(
        TenantId tenantId,
        CancellationToken cancellationToken = default);
    
    string? ResolveSchema(TenantId tenantId);
}
```

#### Purpose

Maps tenant identity to database resources for connection-level or schema-level isolation.

#### Supported Isolation Models

| Model | Implementation | Contract Support |
|-------|----------------|------------------|
| **Database per tenant** | Return different connection strings | ✅ Fully expressed |
| **Schema per tenant** | Return same connection, different schema | ✅ Fully expressed |
| **Row-level isolation** | Same connection and schema, filter queries | ⚠️ Not expressed by this contract |

**Note:** Row-level isolation is a query-filtering concern, not a resolution concern. This contract supports connection/schema resolution only. Row-level filtering must be implemented via query interceptors or repository logic.

#### Usage Example

```csharp
public class ConfigBasedTenantResolver : ITenantResolutionStrategy
{
    private readonly IConfiguration _config;

    public ValueTask<string> ResolveConnectionStringAsync(
        TenantId tenantId,
        CancellationToken ct)
    {
        // Database per tenant
        var connectionString = _config.GetConnectionString(tenantId.Value);
        return ValueTask.FromResult(connectionString);
    }

    public string? ResolveSchema(TenantId tenantId)
    {
        // Schema per tenant (alternative to database-per-tenant)
        return $"tenant_{tenantId.Value}";
    }
}
```

#### Wiring and Invocation

`ITenantResolutionStrategy` is a **contract without default wiring**. The calling layer depends on your chosen isolation model:

| Isolation Model | Who Calls Strategy | When | Wiring Location |
|-----------------|-------------------|------|-----------------|
| **Database per tenant** | Application startup or per-request factory | DbContext construction | `IDbContextFactory` or custom middleware |
| **Schema per tenant** | DbContext | Model creation | `OnModelCreating` override |
| **Row-level isolation** | N/A | N/A | Uses `ITenantAccessor` + query filters instead |

##### Database-Per-Tenant Wiring Example

```csharp
// Custom factory that resolves connection per tenant
public class TenantAwareDbContextFactory : IDbContextFactory<AppDbContext>
{
    private readonly ITenantAccessor _tenantAccessor;
    private readonly ITenantResolutionStrategy _resolver;
    private readonly IServiceProvider _services;

    public async Task<AppDbContext> CreateDbContextAsync(CancellationToken ct = default)
    {
        var tenantId = _tenantAccessor.GetCurrentTenantId();
        var connectionString = await _resolver.ResolveConnectionStringAsync(tenantId, ct);
        
        var optionsBuilder = new DbContextOptionsBuilder<AppDbContext>();
        optionsBuilder.UseSqlServer(connectionString);
        
        return new AppDbContext(
            optionsBuilder.Options,
            _tenantAccessor,
            _services.GetRequiredService<IDataDiagnosticsContext>());
    }
}
```

##### Schema-Per-Tenant Wiring Example

```csharp
public class AppDbContext : HoneyDrunkDbContext
{
    private readonly ITenantResolutionStrategy _resolver;

    protected override string? Schema => 
        _resolver.ResolveSchema(CurrentTenantId);

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        // Schema is set via base class using Schema property
        base.OnModelCreating(modelBuilder);
    }
}
```

##### Row-Level Isolation (No Resolution Strategy)

For row-level isolation, `ITenantResolutionStrategy` is **not used**. Instead:

```csharp
public class AppDbContext : HoneyDrunkDbContext
{
    protected override void ApplyConfigurations(ModelBuilder modelBuilder)
    {
        // Tenant filtering via query filters
        modelBuilder.Entity<Order>()
            .HasQueryFilter(o => o.TenantId == CurrentTenantId.Value);
        
        modelBuilder.Entity<Customer>()
            .HasQueryFilter(c => c.TenantId == CurrentTenantId.Value);
    }
}
```

#### Tenant Entity Convention

For row-level isolation, tenant-scoped entities must have a tenant identifier property. There is no enforced interface—use your own convention:

```csharp
// Option 1: String property (matches TenantId.Value)
public class Order
{
    public string TenantId { get; set; } = string.Empty;
    // ... other properties
}

// Option 2: Typed property (if you prefer stronger typing)
public class Order
{
    public TenantId TenantId { get; set; }
    // ... other properties
}

// Option 3: Interface (if you want to enforce via contract)
public interface ITenantEntity
{
    string TenantId { get; set; }
}

public class Order : ITenantEntity
{
    public string TenantId { get; set; } = string.Empty;
}
```

**Note:** HoneyDrunk.Data does not define `ITenantEntity` as a contract. The entity shape is application-specific. Choose the convention that fits your domain.

[↑ Back to top](#table-of-contents)

---

## Repositories

### IReadOnlyRepository.cs

```csharp
public interface IReadOnlyRepository<TEntity>
    where TEntity : class
{
    ValueTask<TEntity?> FindByIdAsync(
        object id,
        CancellationToken cancellationToken = default);
    
    Task<IReadOnlyList<TEntity>> FindAsync(
        Expression<Func<TEntity, bool>> predicate,
        CancellationToken cancellationToken = default);
    
    Task<TEntity?> FindOneAsync(
        Expression<Func<TEntity, bool>> predicate,
        CancellationToken cancellationToken = default);
    
    Task<bool> ExistsAsync(
        Expression<Func<TEntity, bool>> predicate,
        CancellationToken cancellationToken = default);
    
    Task<int> CountAsync(
        Expression<Func<TEntity, bool>>? predicate = null,
        CancellationToken cancellationToken = default);
}
```

#### Purpose

Read-only repository interface for query operations. Use this interface when you only need to read data without modifications.

#### Design Notes

- **`FindByIdAsync(object id)`** uses `object` to avoid generic key type complexity. This trades compile-time safety for flexibility. Composite keys require provider-specific handling.
- **Expression-based queries** assume LINQ-capable providers. Expression translation varies by provider—queries that work with EF Core may fail with other providers.
- **No query-shaping controls** (tracking, includes, pagination, ordering) are defined. These are provider-specific concerns; consumers may need to extend or escape to provider APIs.

#### Method Reference

| Method | Description |
|--------|-------------|
| `FindByIdAsync` | Find single entity by primary key |
| `FindAsync` | Find all entities matching predicate |
| `FindOneAsync` | Find first entity matching predicate |
| `ExistsAsync` | Check if any entity matches predicate |
| `CountAsync` | Count entities matching predicate |

#### Usage Example

```csharp
public class OrderQueryService
{
    private readonly IReadOnlyRepository<Order> _orders;

    public async Task<IReadOnlyList<Order>> GetPendingOrdersAsync(CancellationToken ct)
    {
        return await _orders.FindAsync(
            o => o.Status == OrderStatus.Pending,
            ct);
    }

    public async Task<bool> HasOrdersAsync(string customerId, CancellationToken ct)
    {
        return await _orders.ExistsAsync(
            o => o.CustomerId == customerId,
            ct);
    }
}
```

[↑ Back to top](#table-of-contents)

---

### IRepository.cs

```csharp
public interface IRepository<TEntity> : IReadOnlyRepository<TEntity>
    where TEntity : class
{
    Task AddAsync(
        TEntity entity,
        CancellationToken cancellationToken = default);
    
    Task AddRangeAsync(
        IEnumerable<TEntity> entities,
        CancellationToken cancellationToken = default);
    
    void Update(TEntity entity);
    
    void Remove(TEntity entity);
    
    void RemoveRange(IEnumerable<TEntity> entities);
}
```

#### Purpose

Full repository interface extending read operations with mutations. Changes are staged until the unit of work is saved.

#### Design Notes

- Repositories are **coordination wrappers**, not safety boundaries. They do not guarantee tenant isolation, query validation, or tracking behavior.
- Mutation methods stage changes in the underlying context. Actual persistence occurs via `IUnitOfWork.SaveChangesAsync()`.

#### Method Reference

| Method | Description |
|--------|-------------|
| `AddAsync` | Stage a new entity for insertion |
| `AddRangeAsync` | Stage multiple entities for insertion |
| `Update` | Mark entity as modified |
| `Remove` | Mark entity for deletion |
| `RemoveRange` | Mark multiple entities for deletion |

#### Usage Example

```csharp
public class OrderService
{
    private readonly IUnitOfWork<AppDbContext> _unitOfWork;

    public async Task CreateOrderAsync(Order order, CancellationToken ct)
    {
        var repo = _unitOfWork.Repository<Order>();
        
        await repo.AddAsync(order, ct);
        await _unitOfWork.SaveChangesAsync(ct);
    }

    public async Task CancelOrderAsync(Guid orderId, CancellationToken ct)
    {
        var repo = _unitOfWork.Repository<Order>();
        
        var order = await repo.FindByIdAsync(orderId, ct);
        if (order is null) return;
        
        order.Status = OrderStatus.Cancelled;
        repo.Update(order);
        
        await _unitOfWork.SaveChangesAsync(ct);
    }
}
```

[↑ Back to top](#table-of-contents)

---

## Transactions

### ITransactionScope.cs

```csharp
public interface ITransactionScope : IAsyncDisposable
{
    Guid TransactionId { get; }
    
    Task CommitAsync(CancellationToken cancellationToken = default);
    
    Task RollbackAsync(CancellationToken cancellationToken = default);
}
```

#### Purpose

Explicit transaction boundary for fine-grained control. Use when you need to coordinate multiple save operations within a single database connection.

#### Design Notes

Transaction scope is **connection-local**. It does not provide distributed transaction support across multiple databases or external systems.

#### Usage Example

```csharp
public async Task TransferFundsAsync(
    Guid fromAccount,
    Guid toAccount,
    decimal amount,
    CancellationToken ct)
{
    await using var scope = await _unitOfWork.BeginTransactionAsync(ct);
    
    try
    {
        var accounts = _unitOfWork.Repository<Account>();
        
        var from = await accounts.FindByIdAsync(fromAccount, ct);
        var to = await accounts.FindByIdAsync(toAccount, ct);
        
        from!.Balance -= amount;
        to!.Balance += amount;
        
        accounts.Update(from);
        accounts.Update(to);
        
        await _unitOfWork.SaveChangesAsync(ct);
        await scope.CommitAsync(ct);
    }
    catch
    {
        await scope.RollbackAsync(ct);
        throw;
    }
}
```

[↑ Back to top](#table-of-contents)

---

### IUnitOfWork.cs

```csharp
public interface IUnitOfWork : IAsyncDisposable
{
    bool HasPendingChanges { get; }
    
    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
    
    Task<ITransactionScope> BeginTransactionAsync(
        CancellationToken cancellationToken = default);
}

public interface IUnitOfWork<TContext> : IUnitOfWork
    where TContext : class
{
    IRepository<TEntity> Repository<TEntity>()
        where TEntity : class;
}
```

#### Purpose

Coordinates changes across multiple repositories and persists them within a single context.

#### Design Notes

- **Context-local atomicity only.** `SaveChangesAsync` is atomic within a single `DbContext` and database connection. No cross-database or distributed transaction guarantees.
- **Generic variant** (`IUnitOfWork<TContext>`) provides typed repository access. The non-generic `IUnitOfWorkFactory` returns untyped `IUnitOfWork`—consumers needing typed access may require casting or a generic factory.

#### Usage Example

```csharp
public class CheckoutService
{
    private readonly IUnitOfWork<StoreDbContext> _unitOfWork;

    public async Task CheckoutAsync(Cart cart, CancellationToken ct)
    {
        var orders = _unitOfWork.Repository<Order>();
        var inventory = _unitOfWork.Repository<InventoryItem>();
        var audit = _unitOfWork.Repository<AuditLog>();

        // Create order
        var order = new Order { Items = cart.Items };
        await orders.AddAsync(order, ct);

        // Reduce inventory
        foreach (var item in cart.Items)
        {
            var inv = await inventory.FindByIdAsync(item.ProductId, ct);
            inv!.Quantity -= item.Quantity;
            inventory.Update(inv);
        }

        // Audit log
        await audit.AddAsync(new AuditLog 
        { 
            Action = "Checkout",
            EntityId = order.Id.ToString()
        }, ct);

        // Context-local atomic save
        await _unitOfWork.SaveChangesAsync(ct);
    }
}
```

[↑ Back to top](#table-of-contents)

---

### IUnitOfWorkFactory.cs

```csharp
public interface IUnitOfWorkFactory
{
    IUnitOfWork Create();
}
```

#### Purpose

Factory for creating unit of work instances. Useful when you need to control the lifetime explicitly (e.g., background jobs).

#### Design Notes

Returns non-generic `IUnitOfWork`. If typed repository access is needed, consumers must cast or use a generic factory variant (if provided by the implementation).

#### Usage Example

```csharp
public class BatchProcessor
{
    private readonly IUnitOfWorkFactory _factory;

    public async Task ProcessBatchAsync(IEnumerable<Item> items)
    {
        foreach (var batch in items.Chunk(100))
        {
            await using var unitOfWork = _factory.Create();
            
            // Process batch with dedicated unit of work
            // Note: Cast to IUnitOfWork<TContext> if typed access needed
            // ...
            
            await unitOfWork.SaveChangesAsync();
        }
    }
}
```

[↑ Back to top](#table-of-contents)

---

## Diagnostics

### DataHealthStatus.cs

```csharp
public enum DataHealthStatus
{
    Healthy = 0,
    Degraded = 1,
    Unhealthy = 2
}
```

#### Purpose

Represents the health status of a data component. Follows standard health check semantics.

| Status | Description |
|--------|-------------|
| `Healthy` | Component is operational |
| `Degraded` | Component works but with issues |
| `Unhealthy` | Component is not operational |

[↑ Back to top](#table-of-contents)

---

### DataHealthResult.cs

```csharp
public sealed record DataHealthResult(
    DataHealthStatus Status,
    string? Description = null,
    IReadOnlyDictionary<string, object>? Data = null)
{
    public static DataHealthResult Healthy(string? description = null);
    public static DataHealthResult Degraded(string description);
    public static DataHealthResult Unhealthy(string description);
}
```

#### Purpose

Represents the result of a data health check with status, description, and optional diagnostic data.

#### Usage Example

```csharp
public async ValueTask<DataHealthResult> CheckHealthAsync(CancellationToken ct)
{
    try
    {
        var stopwatch = Stopwatch.StartNew();
        await _context.Database.CanConnectAsync(ct);
        stopwatch.Stop();

        if (stopwatch.ElapsedMilliseconds > 1000)
        {
            return DataHealthResult.Degraded(
                $"Database connection slow: {stopwatch.ElapsedMilliseconds}ms");
        }

        return DataHealthResult.Healthy(
            $"Connected in {stopwatch.ElapsedMilliseconds}ms");
    }
    catch (Exception ex)
    {
        return DataHealthResult.Unhealthy($"Connection failed: {ex.Message}");
    }
}
```

[↑ Back to top](#table-of-contents)

---

### IDataHealthContributor.cs

```csharp
public interface IDataHealthContributor
{
    string Name { get; }
    
    ValueTask<DataHealthResult> CheckHealthAsync(
        CancellationToken cancellationToken = default);
}
```

#### Purpose

Interface for data persistence components to participate in health checks. Contributors are passive—invoked by the host health system on demand.

#### Scope Boundary

This contract is for **data persistence health** (databases, data stores). General infrastructure health (caches, message queues) belongs in separate health abstractions, not Data.

#### Usage Example

```csharp
// Appropriate: Database connectivity
public class SqlServerHealthContributor : IDataHealthContributor
{
    public string Name => "SQL Server";

    public async ValueTask<DataHealthResult> CheckHealthAsync(CancellationToken ct)
    {
        try
        {
            await _context.Database.CanConnectAsync(ct);
            return DataHealthResult.Healthy();
        }
        catch (Exception ex)
        {
            return DataHealthResult.Unhealthy(ex.Message);
        }
    }
}
```

[↑ Back to top](#table-of-contents)

---

### IDataDiagnosticsContext.cs

```csharp
public interface IDataDiagnosticsContext
{
    string? CorrelationId { get; }
    string? OperationId { get; }
    string? NodeId { get; }
    IReadOnlyDictionary<string, string> Tags { get; }
}
```

#### Purpose

Provides diagnostic context for data operations. Used to enrich SQL commands, logs, and telemetry.

#### Design Notes

Properties are `string?` for transport compatibility. If Kernel defines strongly-typed identity primitives, implementations should extract and convert appropriately. String representation here is a serialization convenience, not a type downgrade.

#### Usage Example

```csharp
public class CorrelationCommandInterceptor : DbCommandInterceptor
{
    private readonly IDataDiagnosticsContext _context;

    private void AddCorrelationComment(DbCommand command)
    {
        var correlationId = _context.CorrelationId;
        if (string.IsNullOrEmpty(correlationId)) return;

        // Prepend correlation ID as SQL comment
        command.CommandText = $"/* correlation:{correlationId} */\n{command.CommandText}";
    }
}
```

[↑ Back to top](#table-of-contents)

---

## Summary

The Abstractions package provides foundational contracts for the Data system. By having minimal dependencies, it can be referenced by projects that need persistence concepts without pulling in runtime implementations.

Key design characteristics:
- **Minimal dependencies** - Only `HoneyDrunk.Kernel.Abstractions`
- **Contract-only** - No EF Core, no database-specific types, no behavior
- **Tenant-aware** - `TenantId` and `ITenantAccessor` available; enforcement is consumer responsibility
- **Async-first** - All I/O operations are async
- **Expression-based** - Queries use expressions; assumes LINQ-capable providers
- **Diagnostics contracts** - Health and telemetry participation interfaces

**Limitations to understand:**
- Expression-based queries are not provider-neutral in practice
- Tenant identity is available but not automatically applied
- Repository contracts don't express query-shaping (tracking, includes, pagination)
- Unit of work atomicity is context-local, not distributed
- `IUnitOfWorkFactory` returns untyped `IUnitOfWork`

---

[← Back to File Guide](FILE_GUIDE.md) | [↑ Back to top](#table-of-contents)
