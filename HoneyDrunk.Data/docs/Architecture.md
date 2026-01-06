# 🏛️ Architecture - Dependency Flow and Layer Responsibilities

[← Back to File Guide](FILE_GUIDE.md)

---

## Table of Contents

- [Overview](#overview)
- [Layer Diagram](#layer-diagram)
- [Package Dependencies](#package-dependencies)
- [Layer Responsibilities](#layer-responsibilities)
- [Registration Contract](#registration-contract)
- [Integration Patterns](#integration-patterns)
- [Provider Implementation Checklist](#provider-implementation-checklist)
- [Design Decisions](#design-decisions)

---

## Overview

HoneyDrunk.Data follows a layered architecture with clear separation between abstractions, orchestration, and provider implementations. This design enables:

- **Provider flexibility** - Alternative providers can implement the same abstractions, though query semantics remain provider-shaped (LINQ expressions translate differently across providers)
- **Testability** - Mock abstractions for unit testing; use SQLite for integration testing (with awareness of behavioral differences from production providers)
- **Tenant awareness** - Tenant identity is available throughout layers; application of tenant context depends on resolution strategy and explicit usage
- **Observability hooks** - Correlation tracking and health contributors where providers support them (not universal across all execution paths)

---

## Layer Diagram

```
┌─────────────────────────────────────────────────────────────────────────────┐
│                              Application Layer                               │
│                    (Your API Controllers, Services, etc.)                    │
│                                                                              │
│   Note: Applications may reference any layer directly based on needs.        │
│         The diagram shows typical dependency flow, not mandatory paths.      │
└─────────────────────────────────────────────────────────────────────────────┘
                                       │
                                       ▼
┌─────────────────────────────────────────────────────────────────────────────┐
│                        HoneyDrunk.Data.Abstractions                          │
│                         (Contracts only, no behavior)                        │
│                                                                              │
│  ┌─────────────────┐  ┌─────────────────┐  ┌─────────────────────────────┐  │
│  │   Repositories  │  │   Transactions  │  │         Tenancy             │  │
│  │ IRepository<T>  │  │ IUnitOfWork     │  │ ITenantAccessor             │  │
│  │ IReadOnly...    │  │ ITransaction... │  │ ITenantResolution...        │  │
│  └─────────────────┘  └─────────────────┘  └─────────────────────────────┘  │
│                                                                              │
│  ┌─────────────────────────────────────────────────────────────────────────┐│
│  │                           Diagnostics                                    ││
│  │ IDataHealthContributor, IDataDiagnosticsContext, DataHealthResult       ││
│  └─────────────────────────────────────────────────────────────────────────┘│
└─────────────────────────────────────────────────────────────────────────────┘
                                       │
                                       ▼
┌─────────────────────────────────────────────────────────────────────────────┐
│                            HoneyDrunk.Data                                   │
│                        (Orchestration Layer)                                 │
│              Persistence-neutral, Kernel runtime required                    │
│                                                                              │
│  ┌─────────────────┐  ┌─────────────────┐  ┌─────────────────────────────┐  │
│  │  Configuration  │  │   Diagnostics   │  │         Tenancy             │  │
│  │  DataOptions    │  │ DataActivity... │  │ KernelTenantAccessor        │  │
│  │                 │  │ KernelDataDia...│  │                             │  │
│  └─────────────────┘  └─────────────────┘  └─────────────────────────────┘  │
│                                                                              │
│  ┌─────────────────────────────────────────────────────────────────────────┐│
│  │                    AddHoneyDrunkData() Registration                      ││
│  └─────────────────────────────────────────────────────────────────────────┘│
└─────────────────────────────────────────────────────────────────────────────┘
                                       │
                                       ▼
┌─────────────────────────────────────────────────────────────────────────────┐
│                     HoneyDrunk.Data.EntityFramework                          │
│                        (EF Core Provider)                                    │
│                                                                              │
│  ┌─────────────────┐  ┌─────────────────┐  ┌─────────────────────────────┐  │
│  │     Context     │  │  Repositories   │  │       Transactions          │  │
│  │ HoneyDrunkDb... │  │ EfRepository<T> │  │ EfUnitOfWork<T>             │  │
│  └─────────────────┘  └─────────────────┘  └─────────────────────────────┘  │
│                                                                              │
│  ┌─────────────────┐  ┌─────────────────┐  ┌─────────────────────────────┐  │
│  │   Diagnostics   │  │    Modeling     │  │       Registration          │  │
│  │ Correlation...  │  │ ModelBuilder... │  │ AddHoneyDrunkDataEF<T>()    │  │
│  │ DbContextHea... │  │                 │  │                             │  │
│  └─────────────────┘  └─────────────────┘  └─────────────────────────────┘  │
└─────────────────────────────────────────────────────────────────────────────┘
                                       │
                                       ▼
┌─────────────────────────────────────────────────────────────────────────────┐
│                       HoneyDrunk.Data.SqlServer                              │
│                     (SQL Server Specialization)                              │
│                                                                              │
│  ┌─────────────────┐  ┌─────────────────┐  ┌─────────────────────────────┐  │
│  │   Conventions   │  │   Diagnostics   │  │       Registration          │  │
│  │ SqlServerMod... │  │ SqlServerHea... │  │ AddHoneyDrunkDataSqlServer  │  │
│  │                 │  │                 │  │ AddHoneyDrunkDataAzureSql   │  │
│  └─────────────────┘  └─────────────────┘  └─────────────────────────────┘  │
└─────────────────────────────────────────────────────────────────────────────┘
                                       │
                                       ▼
┌─────────────────────────────────────────────────────────────────────────────┐
│                            Database Layer                                    │
│                    (SQL Server, Azure SQL, etc.)                            │
└─────────────────────────────────────────────────────────────────────────────┘
```

---

## Package Dependencies

```
┌─────────────────────────────────────┐
│     HoneyDrunk.Kernel.Abstractions  │ ◄── External dependency
└─────────────────────────────────────┘
                    │
                    ▼
┌─────────────────────────────────────┐
│   HoneyDrunk.Data.Abstractions      │ ◄── Zero implementation dependencies
│   (Contracts only)                  │     Can be referenced standalone
└─────────────────────────────────────┘
                    │
                    ▼
┌─────────────────────────────────────┐
│        HoneyDrunk.Kernel            │ ◄── External dependency
└─────────────────────────────────────┘
                    │
                    ▼
┌─────────────────────────────────────┐
│        HoneyDrunk.Data              │ ◄── Orchestration (no EF Core)
│     (Kernel integration)            │     Optional for non-Kernel providers
└─────────────────────────────────────┘
                    │
                    ▼
┌─────────────────────────────────────┐
│  HoneyDrunk.Data.EntityFramework    │ ◄── EF Core provider
│   (Microsoft.EntityFrameworkCore)   │
└─────────────────────────────────────┘
                    │
                    ▼
┌─────────────────────────────────────┐
│    HoneyDrunk.Data.SqlServer        │ ◄── SQL Server specific
│ (Microsoft.EntityFrameworkCore.     │
│          SqlServer)                 │
└─────────────────────────────────────┘
                    │
                    ▼
┌─────────────────────────────────────┐
│    HoneyDrunk.Data.Migrations       │ ◄── CI/CD tooling
│ (Microsoft.EntityFrameworkCore.     │     Executable in pipelines,
│          Design)                    │     not shipped with runtime
└─────────────────────────────────────┘

┌─────────────────────────────────────┐
│     HoneyDrunk.Data.Testing         │ ◄── Test helpers only
│ (Microsoft.EntityFrameworkCore.     │     Behavioral parity with prod
│          Sqlite + xUnit)            │     is NOT guaranteed
└─────────────────────────────────────┘
```

---

## Layer Responsibilities

### HoneyDrunk.Data.Abstractions

**Purpose:** Define contracts with no implementation dependencies

| Component | Responsibility |
|-----------|----------------|
| `IRepository<T>` | Entity lifecycle and coordination contract |
| `IReadOnlyRepository<T>` | Read-only query contract |
| `IUnitOfWork` | Change coordination contract (context-local scope) |
| `ITransactionScope` | Transaction boundary contract |
| `ITenantAccessor` | Tenant identity access contract |
| `ITenantResolutionStrategy` | Tenant-to-resource mapping contract |
| `IDataHealthContributor` | Health check participation contract |
| `IDataDiagnosticsContext` | Telemetry context contract |

**Design Constraint:** No EF Core, no database-specific types, no implementation logic. Contracts only—no observability behavior exists at this layer.

### HoneyDrunk.Data

**Purpose:** Persistence-neutral orchestration with Kernel integration

| Component | Responsibility |
|-----------|----------------|
| `DataOptions` | Configuration options |
| `KernelTenantAccessor` | Extract tenant from `IOperationContextAccessor` |
| `KernelDataDiagnosticsContext` | Extract diagnostics from Kernel context |
| `DataActivitySource` | Create telemetry activities |
| `ServiceCollectionExtensions` | `AddHoneyDrunkData()` registration |

**Design Constraint:** No EF Core references, no database-specific code. Requires Kernel runtime—this layer is persistence-neutral but not runtime-neutral.

### HoneyDrunk.Data.EntityFramework

**Purpose:** Implement abstractions using Entity Framework Core

| Component | Responsibility |
|-----------|----------------|
| `HoneyDrunkDbContext` | Base DbContext with tenant identity access |
| `EfRepository<T>` | Thin coordination wrapper over DbSet (not a safety boundary) |
| `EfUnitOfWork<T>` | Coordinates changes within a single DbContext and connection |
| `EfTransactionScope` | Transaction scope wrapping `IDbContextTransaction` |
| `CorrelationCommandInterceptor` | Best-effort correlation tagging for relational providers |
| `DbContextHealthContributor<T>` | Passive health contributor invoked by host |
| `ModelBuilderConventions` | Optional naming conventions (configurable) |

**Design Constraint:** Minimize SQL Server-specific code. Some provider-specific behaviors (retry, timeouts) may leak through EF Core configuration—these should be encapsulated in provider packages where possible.

### HoneyDrunk.Data.SqlServer

**Purpose:** SQL Server-specific configuration and optimizations

| Component | Responsibility |
|-----------|----------------|
| `SqlServerDataOptions` | Connection string, retry, timeout config |
| `SqlServerModelConventions` | `datetime2`, decimal precision conventions |
| `SqlServerHealthContributor<T>` | Enhanced SQL Server diagnostics contributor |
| `ServiceCollectionExtensions` | `AddHoneyDrunkDataSqlServer<T>()` |

**Design Constraint:** Only SQL Server concerns, no PostgreSQL, MySQL, etc.

### HoneyDrunk.Data.Migrations

**Purpose:** Migration orchestration conventions for CI/CD workflows

| Component | Responsibility |
|-----------|----------------|
| `MigrationDbContextFactory<T>` | Design-time context factory base |
| `MigrationRunner` | Programmatic migration execution for CI/CD pipelines |

**Design Constraint:** CI/CD tooling—executable in pipelines but not shipped with production runtime assemblies. "Tooling only" means not referenced by application code, not that it's non-executable.

### HoneyDrunk.Data.Testing

**Purpose:** Test infrastructure for data layer testing

| Component | Responsibility |
|-----------|----------------|
| `SqliteTestDbContextFactory<T>` | In-memory SQLite for tests |
| `SqliteDbContextFixture<T>` | xUnit fixture with seeding |
| `DatabaseResetHelper` | Clear data, reset database |
| `TestDoubles` | Mock tenant accessor, diagnostics |

**Design Constraint:** Test-only, never referenced by production code. **Important:** SQLite has different type systems, transaction semantics, and concurrency behavior than SQL Server. These helpers are testing conveniences, not behavioral mirrors of production.

---

## Registration Contract

### Canonical Registration Order

Services must be registered in this order for correct behavior:

```csharp
var builder = WebApplication.CreateBuilder(args);

// 1. Kernel (required for orchestration layer)
builder.Services.AddHoneyDrunkGrid(options => { /* ... */ });

// 2. Data orchestration layer (registers ITenantAccessor, IDataDiagnosticsContext)
builder.Services.AddHoneyDrunkData(options => { /* ... */ });

// 3. Provider (registers DbContext, IUnitOfWork, interceptors, health contributors)
builder.Services.AddHoneyDrunkDataSqlServer<AppDbContext>(sqlOptions => { /* ... */ });
```

### What Happens If Order Is Wrong

| Mistake | Runtime Behavior |
|---------|------------------|
| Skip Kernel, use orchestration | `KernelTenantAccessor` throws on `IOperationContextAccessor` resolution |
| Skip orchestration, use provider | Provider registration succeeds but `ITenantAccessor`/`IDataDiagnosticsContext` are not registered; DbContext constructor injection fails |
| Provider before orchestration | Same as above—provider expects orchestration services to exist |

### Registration Method Dependencies

| Method | Requires | Registers |
|--------|----------|-----------|
| `AddHoneyDrunkGrid()` | Nothing | Kernel services, `IOperationContextAccessor` |
| `AddHoneyDrunkData()` | Kernel services | `ITenantAccessor`, `IDataDiagnosticsContext`, `DataOptions` |
| `AddHoneyDrunkDataEntityFramework<T>()` | `AddHoneyDrunkData()` services | `DbContext`, `IUnitOfWork<T>`, interceptors, health contributors |
| `AddHoneyDrunkDataSqlServer<T>()` | `AddHoneyDrunkData()` services | Same as EF + SQL Server configuration |
| `AddHoneyDrunkDataAzureSql<T>()` | `AddHoneyDrunkData()` services | Same as EF + Azure SQL configuration |

### Implicit vs Explicit Registration

**Provider methods do NOT implicitly call `AddHoneyDrunkData()`.** Each registration is explicit:

```csharp
// ❌ Wrong - will fail at runtime
builder.Services.AddHoneyDrunkDataSqlServer<AppDbContext>(/* ... */);

// ✅ Correct - explicit orchestration registration
builder.Services.AddHoneyDrunkData();
builder.Services.AddHoneyDrunkDataSqlServer<AppDbContext>(/* ... */);
```

### Startup Validation

Two validation methods are available:

**Registration-time validation** (on `IServiceCollection`):
```csharp
var builder = WebApplication.CreateBuilder(args);

builder.Services.AddHoneyDrunkGrid(/* ... */);
builder.Services.AddHoneyDrunkData();
builder.Services.AddHoneyDrunkDataSqlServer<AppDbContext>(/* ... */);

// Validate registrations exist before building
builder.Services.ValidateHoneyDrunkDataRegistration();

var app = builder.Build();
```

**Runtime validation** (on `IServiceProvider`):
```csharp
var app = builder.Build();

// Validates services are resolvable (creates a scope, resolves services)
app.Services.ValidateHoneyDrunkDataConfiguration();

app.Run();
```

**Validation checks:**
- `IOperationContextAccessor` — Kernel must be registered
- `ITenantAccessor` — Orchestration layer must be registered
- `IDataDiagnosticsContext` — Orchestration layer must be registered

**Note:** Provider-specific validation (DbContext, connection strings) is not included. Provider registration failures will surface at first use.

[↑ Back to top](#table-of-contents)

---

## Integration Patterns

### Kernel Integration

```csharp
// Tenant identity flows from HTTP header → Kernel context → Data layer
// Application of tenant context depends on resolution strategy and explicit usage
public class KernelTenantAccessor : ITenantAccessor
{
    private readonly IOperationContextAccessor _contextAccessor;

    public TenantId GetCurrentTenantId()
    {
        var tenantId = _contextAccessor.CurrentContext?.TenantId;
        return string.IsNullOrEmpty(tenantId) 
            ? default 
            : TenantId.FromString(tenantId);
    }
}
```

### Correlation Tracking

```csharp
// SQL commands tagged with correlation ID (relational providers with interception support only)
// Best-effort tagging—not guaranteed in all execution paths
/* correlation:01JXYZ... */
SELECT * FROM Orders WHERE TenantId = @p0

// Appears in SQL Server query store, profiler, logs when interceptor is active
```

### Health Contributor Integration

```csharp
// Health contributors are passive—invoked by host on demand, not proactively monitoring
public class DbContextHealthContributor<TContext> : IDataHealthContributor
{
    public async ValueTask<DataHealthResult> CheckHealthAsync(CancellationToken ct)
    {
        // Only executes when host invokes health check
        await _context.Database.CanConnectAsync(ct);
        return DataHealthResult.Healthy("Database connection successful");
    }
}
```

---

## Provider Implementation Checklist

### What "Provider Agnostic" Means

The abstraction layer (`HoneyDrunk.Data.Abstractions`) defines contracts that **assume LINQ-capable query providers**. Expression-based queries (`Expression<Func<T, bool>>`) are the primary query interface.

**Practical implications:**
- EF Core providers translate expressions to SQL
- Non-LINQ providers (Dapper, raw ADO.NET) would require expression interpreters or alternative query strategies
- "Provider agnostic" means the contracts are reusable, not that query semantics are identical

### Implementing a New Provider

A new provider package (e.g., `HoneyDrunk.Data.Dapper`) must implement:

| Contract | Implementation Required | Notes |
|----------|------------------------|-------|
| `IRepository<T>` | ✅ Required | Core data access |
| `IReadOnlyRepository<T>` | ✅ Required (via IRepository) | Query operations |
| `IUnitOfWork<TContext>` | ✅ Required | Change coordination |
| `ITransactionScope` | ✅ Required | Transaction boundary |
| `IDataHealthContributor` | ⚠️ Optional | Database connectivity health |
| `ITenantAccessor` | ❌ Reuse from orchestration | Kernel integration |
| `IDataDiagnosticsContext` | ❌ Reuse from orchestration | Kernel integration |
| `ITenantResolutionStrategy` | ⚠️ Application-specific | Not provider responsibility |

### Reusable Components

New providers can reuse from `HoneyDrunk.Data` (orchestration layer):

- `KernelTenantAccessor` - Tenant identity extraction
- `KernelDataDiagnosticsContext` - Correlation/diagnostics
- `DataActivitySource` - Application-layer telemetry
- `DataOptions` - Configuration options

### Provider-Specific Concerns

Each provider must handle:

| Concern | Provider Responsibility |
|---------|------------------------|
| Connection management | Provider-specific (connection pooling, lifetime) |
| Query translation | How expressions become queries (SQL, API calls, etc.) |
| Transaction semantics | Provider's transaction model |
| Retry/resilience | Provider-specific resilience policies |
| Health diagnostics | Provider-specific connectivity checks |
| Correlation tagging | How correlation IDs are attached (if supported) |

### Expression Translation Challenge

For non-LINQ providers, expression translation options:

1. **Expression interpreter** - Parse and translate expressions to provider's query language
2. **Specification pattern** - Define predefined query specifications that map to provider queries
3. **Alternative contract** - Extend abstractions with provider-specific query methods

**Note:** This is a known limitation. The current abstraction design favors LINQ-capable providers. Non-LINQ providers require additional design work.

---

## Design Decisions

### Why Separate Abstractions?

**Problem:** Domain projects shouldn't depend on EF Core just to define repository interfaces.

**Solution:** `HoneyDrunk.Data.Abstractions` has no EF Core references. Domain projects can reference abstractions without pulling in provider dependencies.

**Caveat:** Abstractions define contracts only—no observability, enforcement, or behavior exists at this layer.

### Why an Orchestration Layer?

**Problem:** Kernel integration (tenant, diagnostics) shouldn't require EF Core.

**Solution:** `HoneyDrunk.Data` provides Kernel integration without EF Core. Alternative providers can use the same orchestration layer.

**Caveat:** This layer is persistence-neutral but requires Kernel runtime. Providers that don't use Kernel may reference abstractions directly.

### Why Provider Specialization?

**Problem:** SQL Server has specific features (retry, Azure defaults) that don't apply to other databases.

**Solution:** `HoneyDrunk.Data.SqlServer` encapsulates SQL Server concerns. Future providers (PostgreSQL, MySQL) get their own packages.

### Why Separate Migrations?

**Problem:** Migration tooling (`Microsoft.EntityFrameworkCore.Design`) should never be in production assemblies.

**Solution:** `HoneyDrunk.Data.Migrations` is a CI/CD tooling package. It contains executable code for pipelines but is not referenced with application runtime code.

### Why Separate Testing?

**Problem:** Test infrastructure (SQLite, xUnit) shouldn't be in production packages.

**Solution:** `HoneyDrunk.Data.Testing` provides test helpers that are never shipped with production code.

**Caveat:** SQLite differs behaviorally from SQL Server (types, transactions, concurrency). Tests using SQLite helpers validate logic flow, not production database behavior.

---

[← Back to File Guide](FILE_GUIDE.md) | [↑ Back to top](#table-of-contents)
