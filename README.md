# HoneyDrunk.Data

[![Validate PR](https://github.com/HoneyDrunkStudios/HoneyDrunk.Data/actions/workflows/validate-pr.yml/badge.svg)](https://github.com/HoneyDrunkStudios/HoneyDrunk.Data/actions/workflows/validate-pr.yml)
[![License: MIT](https://img.shields.io/badge/License-MIT-yellow.svg)](https://opensource.org/licenses/MIT)
[![.NET 10](https://img.shields.io/badge/.NET-10.0-512BD4)](https://dotnet.microsoft.com/download/dotnet/10.0)

> **Persistence conventions for HoneyDrunk.OS** - Repository patterns, tenant-aware data access contracts, and EF Core implementation for the Grid.

## 📦 What Is This?

HoneyDrunk.Data provides persistence conventions and an EF Core implementation for HoneyDrunk.OS ("the Hive"). It offers repository patterns, tenant identity access, and telemetry integration for Nodes across the Grid.

### What This Package Provides

- **Tenant Identity Access** - `ITenantAccessor` extracts tenant from Kernel context; application code applies filtering
- **Repository Contracts** - Generic repository interfaces for data access patterns
- **Unit of Work** - Coordinated change tracking within a single DbContext
- **Transaction Wrappers** - EF Core transaction scope wrappers for explicit control
- **Correlation Tagging** - SQL command comments with correlation IDs (EF Core relational providers)
- **Health Contributors** - Database connectivity health contributors (opt-in, provider-specific)
- **EF Core Implementation** - Repository and unit of work implementations for Entity Framework Core

### What This Package Does Not Provide

- **Automatic tenant filtering** — Application responsibility; no built-in query filters
- **Tenant resolution wiring** — `ITenantResolutionStrategy` is a contract only; no default implementation or wiring exists in v0.1.0
- **Distributed transactions** — Context-local atomicity only
- **Non-EF provider implementations** — EF Core is the only provider in v0.1.0

---

## ⚠️ v0.1.0 Limitations

The following features exist as **contracts only** in v0.1.0 and require application-specific implementation:

| Feature | Contract | v0.1.0 Status |
|---------|----------|---------------|
| Tenant identity access | `ITenantAccessor` | ✅ Implemented via `KernelTenantAccessor` |
| Tenant resolution (DB/schema per tenant) | `ITenantResolutionStrategy` | ⚠️ Contract only — no wiring; application must implement |
| Tenant filtering | Global query filters | ⚠️ Not automatic — application must configure per entity |
| Health aggregation | `IDataHealthContributor` | ⚠️ Contributors exist — application wires into health system |

**Bottom line:** v0.1.0 provides **tenant identity access**, not full multi-tenant data isolation. Applications must implement:
- `ITenantResolutionStrategy` if using database-per-tenant or schema-per-tenant
- Query filters per entity type for row-level isolation
- Health endpoint wiring for contributor aggregation

---

## 🚀 Quick Start

### Installation

```sh
# Full stack with SQL Server
dotnet add package HoneyDrunk.Data.SqlServer

# Or just Entity Framework Core (bring your own provider)
dotnet add package HoneyDrunk.Data.EntityFramework

# Or just the abstractions (contracts only, no Kernel dependency)
dotnet add package HoneyDrunk.Data.Abstractions
```

### Web API Setup

This example shows a web application with Kernel, EF Core, and SQL Server. Simpler setups are possible—see package-specific documentation.

> **Registration order matters.** See [Registration Contract](docs/Architecture.md#registration-contract) for details on ordering and failure modes.

```csharp
using HoneyDrunk.Data.Registration;
using HoneyDrunk.Data.SqlServer.Registration;

var builder = WebApplication.CreateBuilder(args);

// 1. Kernel (required for orchestration layer)
builder.Services.AddHoneyDrunkGrid(options =>
{
    options.NodeId = "data-node";
    options.StudioId = "demo-studio";
    options.Environment = "development";
});

// 2. Data orchestration layer (provides KernelTenantAccessor, diagnostics)
builder.Services.AddHoneyDrunkData();

// 3. SQL Server provider
builder.Services.AddHoneyDrunkDataSqlServer<MyDbContext>(sqlOptions =>
{
    sqlOptions.ConnectionString = builder.Configuration.GetConnectionString("Default")
        ?? throw new InvalidOperationException("Connection string required");
    sqlOptions.EnableRetryOnFailure = true;
    sqlOptions.MaxRetryCount = 3;
});

var app = builder.Build();

// Optional: Validate configuration at startup
app.Services.ValidateHoneyDrunkDataConfiguration();

app.MapGet("/orders/{id}", async (Guid id, IUnitOfWork<MyDbContext> unitOfWork) =>
{
    var repo = unitOfWork.Repository<Order>();
    var order = await repo.FindByIdAsync(id);
    return order is not null ? Results.Ok(order) : Results.NotFound();
});

app.Run();
```

### Abstractions-Only Usage

For libraries that only need contracts without runtime dependencies:

```csharp
// Reference only HoneyDrunk.Data.Abstractions
// No Kernel, no EF Core, no provider dependencies

public class OrderService
{
    private readonly IRepository<Order> _orders;
    private readonly ITenantAccessor _tenantAccessor;

    public async Task<Order?> GetOrderAsync(Guid id, CancellationToken ct)
    {
        var tenantId = _tenantAccessor.GetCurrentTenantId();
        
        // Application must apply tenant filtering
        return await _orders.FindOneAsync(
            o => o.Id == id && o.TenantId == tenantId.Value, ct);
    }
}
```

---

## 🎯 Key Features (v0.1.0)

### 🏢 Tenant Identity Access

Tenant identity is extracted from Kernel context. Application code is responsible for applying tenant filtering:

```csharp
// Tenant accessor provides identity from Kernel's IOperationContext
public interface ITenantAccessor
{
    TenantId GetCurrentTenantId();
}

// Resolution strategy contract for connection/schema mapping
// Implementation is application-specific
public interface ITenantResolutionStrategy
{
    ValueTask<string> ResolveConnectionStringAsync(TenantId tenantId, CancellationToken ct);
    string? ResolveSchema(TenantId tenantId);
}
```

**Note:** `ITenantResolutionStrategy` is a contract for application-specific implementation. No default implementation is provided.

### 📚 Repository Contracts

Repository interfaces for data access patterns:

```csharp
// Query operations
public interface IReadOnlyRepository<TEntity>
{
    ValueTask<TEntity?> FindByIdAsync(object id, CancellationToken ct = default);
    Task<IReadOnlyList<TEntity>> FindAsync(Expression<Func<TEntity, bool>> predicate, CancellationToken ct = default);
    Task<TEntity?> FindOneAsync(Expression<Func<TEntity, bool>> predicate, CancellationToken ct = default);
    Task<bool> ExistsAsync(Expression<Func<TEntity, bool>> predicate, CancellationToken ct = default);
    Task<int> CountAsync(Expression<Func<TEntity, bool>>? predicate = null, CancellationToken ct = default);
}

// Full repository adds mutations
public interface IRepository<TEntity> : IReadOnlyRepository<TEntity>
{
    Task AddAsync(TEntity entity, CancellationToken ct = default);
    Task AddRangeAsync(IEnumerable<TEntity> entities, CancellationToken ct = default);
    void Update(TEntity entity);
    void Remove(TEntity entity);
}
```

**Note:** The EF Core implementation provides `EfRepository<T>` which implements `IRepository<T>`. No separate read-only implementation exists—read-only access is a usage pattern, not a separate type.

### 🔄 Unit of Work

Coordinates changes within a single DbContext:

```csharp
public class OrderService
{
    private readonly IUnitOfWork<MyDbContext> _unitOfWork;

    public async Task CreateOrderAsync(Order order, CancellationToken ct)
    {
        var orderRepo = _unitOfWork.Repository<Order>();
        var auditRepo = _unitOfWork.Repository<AuditLog>();

        await orderRepo.AddAsync(order, ct);
        await auditRepo.AddAsync(new AuditLog { Action = "OrderCreated" }, ct);

        // Context-local save—atomic within single DbContext
        await _unitOfWork.SaveChangesAsync(ct);
    }
}
```

**Note:** `SaveChangesAsync` atomicity is scoped to a single DbContext and database connection. Cross-database or distributed transactions are not supported.

### 🔗 Correlation Tagging

SQL commands tagged with correlation IDs (EF Core relational providers with interception support):

```sql
/* correlation:01JXYZ... */
SELECT * FROM Orders WHERE Id = @p0
```

**Note:** Tagging requires EF Core relational provider with command interception enabled. Non-EF providers and non-relational providers do not support this feature.

### 🏥 Health Contributors

Database connectivity health contributors (opt-in, provider-specific):

```csharp
public interface IDataHealthContributor
{
    string Name { get; }
    ValueTask<DataHealthResult> CheckHealthAsync(CancellationToken ct = default);
}
```

**Note:** Health contributors are passive—invoked by host health system on demand. No aggregation or endpoint integration is provided; applications wire contributors into their health check infrastructure.

---

## 📖 Documentation

### Package Documentation
- **[Abstractions](docs/Abstractions.md)** - Core contracts (tenant, repository, unit of work, diagnostics)
- **[Orchestration](docs/Orchestration.md)** - Kernel integration layer
- **[EntityFramework](docs/EntityFramework.md)** - EF Core provider implementation
- **[SqlServer](docs/SqlServer.md)** - SQL Server specialization
- **[Migrations](docs/Migrations.md)** - Migration tooling conventions
- **[Testing](docs/Testing.md)** - SQLite test infrastructure
- **[Architecture](docs/Architecture.md)** - Layer responsibilities and design decisions

---

## 🏗️ Project Structure

```
HoneyDrunk.Data/
├── HoneyDrunk.Data.Abstractions/       # Contracts (no Kernel, no EF Core)
│   ├── Diagnostics/                     # Health check contracts
│   ├── Repositories/                    # Repository interfaces
│   ├── Tenancy/                         # Tenant identity contracts
│   └── Transactions/                    # Unit of work contracts
│
├── HoneyDrunk.Data/                     # Orchestration layer (requires Kernel)
│   ├── Configuration/                   # DataOptions
│   ├── Diagnostics/                     # Kernel diagnostics integration
│   ├── Registration/                    # DI extensions
│   └── Tenancy/                         # Kernel tenant accessor
│
├── HoneyDrunk.Data.EntityFramework/    # EF Core provider (relational)
│   ├── Context/                         # HoneyDrunkDbContext base
│   ├── Diagnostics/                     # Command interceptor, health
│   ├── Modeling/                        # Optional conventions
│   ├── Registration/                    # DI extensions
│   ├── Repositories/                    # EfRepository implementation
│   └── Transactions/                    # EfUnitOfWork implementation
│
├── HoneyDrunk.Data.SqlServer/          # SQL Server specialization
│   ├── Conventions/                     # Optional SQL Server conventions
│   ├── Diagnostics/                     # SQL Server health contributor
│   └── Registration/                    # SQL Server DI extensions
│
├── HoneyDrunk.Data.Migrations/         # Migration tooling
│   ├── Factories/                       # Design-time factory base
│   └── Helpers/                         # Migration runner utilities
│
└── HoneyDrunk.Data.Testing/            # Test infrastructure (SQLite)
    ├── Factories/                       # SQLite test factories
    ├── Fixtures/                        # xUnit fixtures
    └── Helpers/                         # Reset helpers, test doubles
```

---

## 🆕 What's New in v0.1.0

### Abstractions
- `TenantId` typed tenant identifier
- `ITenantAccessor` for tenant identity access
- `ITenantResolutionStrategy` contract (implementation is application-specific)
- `IRepository<T>` and `IReadOnlyRepository<T>` repository contracts
- `IUnitOfWork` and `ITransactionScope` for coordinated persistence
- `IDataHealthContributor` for health check participation

### Orchestration Layer
- `KernelTenantAccessor` extracting tenant from `IOperationContextAccessor`
- `KernelDataDiagnosticsContext` for telemetry enrichment
- `DataActivitySource` for application-layer tracing
- `AddHoneyDrunkData()` registration

### Entity Framework Provider
- `HoneyDrunkDbContext` base with tenant identity and correlation access
- `EfRepository<T>` repository implementation
- `EfUnitOfWork<T>` with repository caching
- `CorrelationCommandInterceptor` for SQL tagging (relational providers)
- `ModelBuilderConventions` optional naming utilities

### SQL Server Support
- `AddHoneyDrunkDataSqlServer<T>()` and `AddHoneyDrunkDataAzureSql<T>()` registration
- Retry-on-failure and command timeout configuration
- `SqlServerModelConventions` optional datetime2 and decimal utilities

### Testing Support
- `SqliteTestDbContextFactory<T>` for in-memory testing
- `SqliteDbContextFixture<T>` xUnit fixture
- `DatabaseResetHelper` for test isolation
- `TestDoubles` for tenant and diagnostics dependencies

---

## 🔗 Related Projects

| Project | Relationship |
|---------|--------------|
| **[HoneyDrunk.Kernel](https://github.com/HoneyDrunkStudios/HoneyDrunk.Kernel)** | Orchestration layer depends on Kernel for context access |
| **[HoneyDrunk.Standards](https://github.com/HoneyDrunkStudios/HoneyDrunk.Standards)** | Analyzers and coding conventions |
| **HoneyDrunk.Transport** | Messaging infrastructure *(in development)* |
| **HoneyDrunk.Auth** | Authentication and authorization *(in development)* |

**Note:** `HoneyDrunk.Data.Abstractions` has no external dependencies and can be used independently. The orchestration layer (`HoneyDrunk.Data`) depends on Kernel runtime.

---

## 📄 License

This project is licensed under the [MIT License](LICENSE).

---

<div align="center">

**Built with 🍯 by HoneyDrunk Studios**

[GitHub](https://github.com/HoneyDrunkStudios/HoneyDrunk.Data) • [Issues](https://github.com/HoneyDrunkStudios/HoneyDrunk.Data/issues)

</div>