# 📦 HoneyDrunk.Data - Complete File Guide

## Overview

**Node-local persistence infrastructure for Grid applications**

This library provides standardized persistence patterns for individual Grid nodes. It abstracts entity lifecycle and coordination through repositories and unit of work, while consuming tenant identity and correlation context from Kernel. Data does not own cross-node data fabric concerns—each node manages its own persistence independently.

**Key Concepts:**
- **Repository**: Abstracts entity lifecycle and coordination (not query semantics—LINQ remains provider-shaped)
- **Unit of Work**: Coordinates changes across repositories within a single DbContext with context-local atomicity
- **Tenancy**: Consumes tenant identity from Kernel context; isolation depends on deployment and resolution strategy
- **Correlation**: Best-effort SQL command tagging for providers that support command interception (EF Core relational)
- **Health Diagnostics**: Exposes health contributors invoked by the host; does not monitor or poll autonomously
- **Kernel Integration**: Telemetry enrichment, tenant propagation, and diagnostics context from Kernel

---

## 📚 Documentation Structure

This guide is organized into focused documents by domain:

### 🏛️ Architecture

| Document | Description |
|----------|-------------|
| [Architecture](Architecture.md) | **Dependency flow, layer responsibilities, and integration patterns** |

### 🔷 HoneyDrunk.Data.Abstractions

| Domain | Document | Description |
|--------|----------|-------------|
| 📋 **Abstractions** | [Abstractions.md](Abstractions.md) | Core contracts and types (repositories, unit of work, tenancy, diagnostics) |

### 🔷 HoneyDrunk.Data (Orchestration)

| Domain | Document | Description |
|--------|----------|-------------|
| 🔧 **Orchestration** | [Orchestration.md](Orchestration.md) | Provider-neutral orchestration layer with Kernel integration |

### 🔷 HoneyDrunk.Data.EntityFramework

| Domain | Document | Description |
|--------|----------|-------------|
| 🗃️ **EntityFramework** | [EntityFramework.md](EntityFramework.md) | EF Core provider implementation (DbContext, repositories, unit of work) |

### 🔷 HoneyDrunk.Data.SqlServer

| Domain | Document | Description |
|--------|----------|-------------|
| 🔵 **SqlServer** | [SqlServer.md](SqlServer.md) | SQL Server and Azure SQL specialization |

### 🔷 HoneyDrunk.Data.Migrations

| Domain | Document | Description |
|--------|----------|-------------|
| 📦 **Migrations** | [Migrations.md](Migrations.md) | Migration orchestration conventions for CI/CD workflows |

### 🔷 HoneyDrunk.Data.Testing

| Domain | Document | Description |
|--------|----------|-------------|
| 🧪 **Testing** | [Testing.md](Testing.md) | SQLite test helpers (convenience utilities, not behavioral mirrors of production providers) |

---

## 🔷 Quick Start

### Basic Concepts

**Data Access Flow:**

```
Application                     Data Layer                      Database
     ↓                              ↓                              ↓
IUnitOfWork<TContext> → Get Repository → Execute Query/Command
     ↓                              ↓                              ↓
Repository<Order>          EfRepository<Order>            SQL Server
SaveChangesAsync()         DbContext.SaveChangesAsync()   Commit Transaction
```

**Tenant Resolution Flow:**

```
HTTP Request                    Kernel Context                  Data Layer
     ↓                              ↓                              ↓
X-Tenant-Id header → IOperationContext.TenantId → ITenantAccessor
     ↓                              ↓                              ↓
                              KernelTenantAccessor         Tenant-aware queries
```

**Correlation Tracking Flow (EF Core Relational only):**

```
Grid Context                    Command Interceptor              SQL Command
     ↓                              ↓                              ↓
CorrelationId → CorrelationCommandInterceptor → /* correlation:xxx */ SELECT...
     ↓                              ↓                              ↓
                              SQL comment prefix              Query Plan Cache
```

### Installation

```sh
# Full stack with SQL Server
dotnet add package HoneyDrunk.Data.SqlServer

# Or just Entity Framework Core (bring your own provider)
dotnet add package HoneyDrunk.Data.EntityFramework

# Or abstractions only (for libraries)
dotnet add package HoneyDrunk.Data.Abstractions
```

### Basic Usage

```csharp
// Program.cs - Setup
var builder = WebApplication.CreateBuilder(args);

// Step 1: Register Kernel (required)
builder.Services.AddHoneyDrunkGrid(opts => { /* ... */ });

// Step 2: Register Data layer
builder.Services.AddHoneyDrunkData();

// Step 3: Register SQL Server provider
builder.Services.AddHoneyDrunkDataSqlServer<MyDbContext>(sqlOptions =>
{
    sqlOptions.ConnectionString = builder.Configuration.GetConnectionString("Default");
    sqlOptions.EnableRetryOnFailure = true;
});

var app = builder.Build();
app.Run();
```

```csharp
// Using Repositories
app.MapGet("/orders/{id}", async (Guid id, IUnitOfWork<MyDbContext> unitOfWork) =>
{
    var repo = unitOfWork.Repository<Order>();
    var order = await repo.FindByIdAsync(id);
    return order is not null ? Results.Ok(order) : Results.NotFound();
});
```

```csharp
// Using Unit of Work for atomic operations
app.MapPost("/orders", async (CreateOrderRequest request, IUnitOfWork<MyDbContext> unitOfWork) =>
{
    var orderRepo = unitOfWork.Repository<Order>();
    var auditRepo = unitOfWork.Repository<AuditLog>();

    var order = new Order { /* ... */ };
    await orderRepo.AddAsync(order);
    await auditRepo.AddAsync(new AuditLog { Action = "OrderCreated", EntityId = order.Id });

    await unitOfWork.SaveChangesAsync();

    return Results.Created($"/orders/{order.Id}", order);
});
```

---

## 🔷 Design Philosophy

### Core Principles

1. **Provider agnostic abstractions** - Core contracts have no EF Core or database dependencies
2. **Kernel-integrated** - Telemetry enrichment, tenant propagation, and diagnostics from Kernel context
3. **Tenant-aware by design** - Tenant identity flows from Kernel; isolation depends on resolution strategy and deployment
4. **Correlation-friendly** - Best-effort SQL tagging where providers support command interception
5. **Health-participating** - Exposes health contributors invoked by the host on demand
6. **Fail-fast validation** - Required configuration validated at startup for registered providers

### What Data Is

- Node-local persistence infrastructure for HoneyDrunk.OS Grid nodes
- Provider-agnostic abstractions with EF Core and SQL Server implementations
- Kernel-integrated for tenant propagation and telemetry enrichment
- Tenant-aware with pluggable resolution strategies (one per deployment)
- Health-participating with contributors invoked by the host

### What Data Is NOT

- Not an ORM (uses EF Core as the ORM implementation)
- Not a query builder (uses LINQ via EF Core; queries remain provider-shaped)
- Not a schema design tool (migrations are EF Core's responsibility)
- Not a connection pool manager (uses provider connection pooling)
- Not a caching layer (caching is a separate concern)
- Not an isolation enforcer (tenant isolation depends on strategy and deployment)
- Not a health monitor (participates in checks when invoked, does not poll)

### Why These Patterns?

**Separation of Abstractions:**
- `HoneyDrunk.Data.Abstractions` has minimal dependencies (only Kernel.Abstractions)
- Can be referenced by domain projects for contract definitions
- Runtime implementations live in provider packages

**Repository Pattern:**
- Abstracts entity lifecycle and coordination, not storage semantics
- LINQ expressions remain provider-translated (not persistence-ignorant)
- Testable with in-memory implementations for unit tests
- Read/write separation for query optimization

**Unit of Work:**
- Coordinates changes across multiple repositories within a single DbContext
- Context-local atomicity (single connection, single database)
- No distributed transaction support implied or provided
- Transaction management abstraction for explicit scopes

**Migration Orchestration:**
- Data does not own schema design (that's application responsibility)
- Provides migration runner conventions for CI/CD workflows
- Wraps EF Core migration infrastructure with standardized patterns

---

## 📦 Project Structure

```
HoneyDrunk.Data/
├── HoneyDrunk.Data.Abstractions/       # Pure contracts, minimal dependencies
│   ├── Diagnostics/
│   │   ├── DataHealthStatus.cs         # Health status enum
│   │   ├── DataHealthResult.cs         # Health check result record
│   │   ├── IDataDiagnosticsContext.cs  # Diagnostics context interface
│   │   └── IDataHealthContributor.cs   # Health contributor interface
│   ├── Repositories/
│   │   ├── IReadOnlyRepository.cs      # Read-only repository interface
│   │   └── IRepository.cs              # Full repository interface
│   ├── Tenancy/
│   │   ├── TenantId.cs                 # Strongly-typed tenant identifier
│   │   ├── ITenantAccessor.cs          # Tenant accessor interface
│   │   └── ITenantResolutionStrategy.cs # Tenant resolution interface
│   └── Transactions/
│       ├── ITransactionScope.cs        # Transaction scope interface
│       ├── IUnitOfWork.cs              # Unit of work interface
│       └── IUnitOfWorkFactory.cs       # Unit of work factory interface
│
├── HoneyDrunk.Data/                     # Orchestration layer (Kernel integration)
│   ├── Configuration/
│   │   └── DataOptions.cs              # Data layer configuration
│   ├── Diagnostics/
│   │   ├── DataActivitySource.cs       # Activity source for telemetry
│   │   └── KernelDataDiagnosticsContext.cs # Kernel-aware diagnostics
│   ├── Registration/
│   │   └── ServiceCollectionExtensions.cs # DI registration
│   └── Tenancy/
│       └── KernelTenantAccessor.cs     # Kernel-aware tenant accessor
│
├── HoneyDrunk.Data.EntityFramework/    # EF Core provider
│   ├── Context/
│   │   └── HoneyDrunkDbContext.cs      # Base DbContext with tenant awareness
│   ├── Diagnostics/
│   │   ├── CorrelationCommandInterceptor.cs # SQL correlation tagging (relational only)
│   │   └── DbContextHealthContributor.cs    # Database health contributor
│   ├── Modeling/
│   │   └── ModelBuilderConventions.cs  # Model conventions (snake_case, etc.)
│   ├── Registration/
│   │   ├── EfDataOptions.cs            # EF-specific options
│   │   └── ServiceCollectionExtensions.cs # DI registration
│   ├── Repositories/
│   │   └── EfRepository.cs             # EF Core repository implementation
│   └── Transactions/
│       ├── EfTransactionScope.cs       # EF Core transaction scope
│       ├── EfUnitOfWork.cs             # EF Core unit of work
│       └── EfUnitOfWorkFactory.cs      # Unit of work factory
│
├── HoneyDrunk.Data.SqlServer/          # SQL Server specialization
│   ├── Conventions/
│   │   └── SqlServerModelConventions.cs # datetime2, decimal precision
│   ├── Diagnostics/
│   │   └── SqlServerHealthContributor.cs # Enhanced SQL Server health contributor
│   └── Registration/
│       ├── SqlServerDataOptions.cs     # SQL Server options
│       └── ServiceCollectionExtensions.cs # DI registration
│
├── HoneyDrunk.Data.Migrations/         # Migration orchestration conventions
│   ├── Factories/
│   │   └── MigrationDbContextFactory.cs # Design-time factory
│   └── Helpers/
│       └── MigrationRunner.cs          # Programmatic migration runner
│
└── HoneyDrunk.Data.Testing/            # Test helpers (convenience, not behavioral parity)
    ├── Factories/
    │   └── SqliteTestDbContextFactory.cs # SQLite in-memory factory
    ├── Fixtures/
    │   └── SqliteDbContextFixture.cs   # xUnit fixture
    └── Helpers/
        ├── DatabaseResetHelper.cs      # Database reset utilities
        └── TestDoubles.cs              # Test doubles for abstractions
```

---

## 🆕 Key Features

### Repository Pattern
- Generic `IRepository<T>` and `IReadOnlyRepository<T>` interfaces
- LINQ expression-based queries with `FindAsync`, `FindOneAsync`, `ExistsAsync`
- Atomic mutations with `AddAsync`, `AddRangeAsync`, `Update`, `Remove`
- Abstracts lifecycle and coordination; LINQ remains provider-translated

### Unit of Work Pattern
- Coordinates changes across multiple repositories within a single DbContext
- Repository caching for performance
- Context-local `SaveChangesAsync` with change tracking (single connection, single database)
- Explicit transaction scopes via `BeginTransactionAsync`

### Tenant-Aware Data Access
- `TenantId` strongly-typed identifier consumed from Kernel context
- `ITenantAccessor` for accessing current tenant identity
- `ITenantResolutionStrategy` for pluggable resolution (connection, schema, or database—one per deployment)
- `KernelTenantAccessor` integrates with `IOperationContextAccessor`
- Isolation guarantees depend on resolution strategy and deployment configuration

### Correlation Tracking (Provider-Specific)
- `CorrelationCommandInterceptor` adds Grid correlation IDs to SQL commands
- Applies to EF Core relational providers that support command interception
- Best-effort tagging; not guaranteed for all providers or future implementations
- SQL comments for query plan cache friendliness

### Health Participation
- `IDataHealthContributor` interface for health check participation
- `DbContextHealthContributor<T>` for database connectivity checks
- `SqlServerHealthContributor<T>` with enhanced SQL Server diagnostics
- Contributors are invoked by the host on demand; Data does not monitor or poll

### SQL Server Support
- `AddHoneyDrunkDataSqlServer<T>()` for SQL Server
- `AddHoneyDrunkDataAzureSql<T>()` for Azure SQL
- Retry-on-failure with configurable attempts and delay
- Model conventions for `datetime2` and decimal precision

### Testing Support
- `SqliteTestDbContextFactory<T>` for in-memory SQLite databases
- `SqliteDbContextFixture<T>` xUnit fixture with seeding support
- `DatabaseResetHelper` for test isolation
- `TestDoubles` for tenant and diagnostics mocking
- **Note:** SQLite helpers are testing conveniences; behavioral parity with production providers (e.g., SQL Server) is not guaranteed

---

## 🔗 Relationships

### Upstream Dependencies

**HoneyDrunk.Data.Abstractions:**
- `HoneyDrunk.Kernel.Abstractions` - For context contracts

**HoneyDrunk.Data:**
- `HoneyDrunk.Data.Abstractions` - Core contracts
- `HoneyDrunk.Kernel` - Context propagation, telemetry
- `Microsoft.Extensions.DependencyInjection.Abstractions` - DI registration
- `Microsoft.Extensions.Options` - Configuration

**HoneyDrunk.Data.EntityFramework:**
- `HoneyDrunk.Data.Abstractions` - Core contracts
- `HoneyDrunk.Data` - Orchestration layer
- `Microsoft.EntityFrameworkCore` - ORM
- `Microsoft.EntityFrameworkCore.Relational` - Relational extensions

**HoneyDrunk.Data.SqlServer:**
- `HoneyDrunk.Data.EntityFramework` - EF Core provider
- `Microsoft.EntityFrameworkCore.SqlServer` - SQL Server provider

**HoneyDrunk.Data.Migrations:**
- `HoneyDrunk.Data.SqlServer` - SQL Server support
- `Microsoft.EntityFrameworkCore.Design` - Design-time tooling

**HoneyDrunk.Data.Testing:**
- `HoneyDrunk.Data.EntityFramework` - EF Core provider
- `Microsoft.EntityFrameworkCore.Sqlite` - SQLite provider
- xUnit and test SDK packages

### Downstream Consumers

Applications using HoneyDrunk.Data:
- **API Services** - CRUD operations with repository pattern
- **Grid Nodes** - Tenant-aware data access (per-node persistence)
- **Background Services** - Unit of work for batch operations
- **Multi-tenant Services** - Tenant-aware data access with configured resolution strategy

---

## 📖 Additional Resources

### Official Documentation
- [README.md](../README.md) - Project overview and quick start
- [CHANGELOG.md](../HoneyDrunk.Data/CHANGELOG.md) - Version history

### Related Projects
- [HoneyDrunk.Kernel](https://github.com/HoneyDrunkStudios/HoneyDrunk.Kernel) - Core Grid primitives (required)
- [HoneyDrunk.Standards](https://github.com/HoneyDrunkStudios/HoneyDrunk.Standards) - Analyzers and conventions

### External References
- [Entity Framework Core](https://docs.microsoft.com/en-us/ef/core/) - EF Core documentation
- [Repository Pattern](https://martinfowler.com/eaaCatalog/repository.html) - Martin Fowler's description
- [Unit of Work Pattern](https://martinfowler.com/eaaCatalog/unitOfWork.html) - Martin Fowler's description

---

## 💡 Motto

**"Where data finds its home."** - Node-local persistence with tenant awareness and Grid integration.

---

*Last Updated: 2026-01-01*  
*Target Framework: .NET 10.0*
