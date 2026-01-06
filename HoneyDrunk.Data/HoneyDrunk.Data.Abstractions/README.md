# HoneyDrunk.Data.Abstractions

Contracts-only project defining persistence abstractions for HoneyDrunk nodes. This project contains no implementation details and no provider-specific types.

## Purpose

Defines the public contract surface for persistence operations. Consumer nodes are expected to depend on these abstractions rather than concrete implementations.

**Note:** These are contracts, not guarantees. Enforcement of layering, tenant isolation, and coordination semantics lives in higher layers (orchestration, providers, application code), not in this package.

## Allowed Dependencies

- `HoneyDrunk.Kernel.Abstractions` - For context and operation contracts
- `HoneyDrunk.Standards` - Analyzer package only (PrivateAssets=all)

## What Must Never Be Added

- **No EntityFrameworkCore references** - This is provider-agnostic
- **No SQL or database-specific types** - No connection strings, DbContext, etc.
- **No transport or auth references** - HoneyDrunk.Transport and HoneyDrunk.Auth are not persistence concerns
- **No implementation classes** - Only interfaces and value types

## Namespace Layout

```
HoneyDrunk.Data.Abstractions
├── Diagnostics/          # Health checks, telemetry context
│   ├── DataHealthResult.cs
│   ├── IDataDiagnosticsContext.cs
│   └── IDataHealthContributor.cs
├── Repositories/         # Repository contracts
│   ├── IReadOnlyRepository.cs
│   └── IRepository.cs
├── Tenancy/              # Tenant identity and resolution contracts
│   ├── ITenantAccessor.cs
│   ├── ITenantResolutionStrategy.cs
│   └── TenantId.cs
└── Transactions/         # Unit of work and transaction scopes
    ├── ITransactionScope.cs
    ├── IUnitOfWork.cs
    └── IUnitOfWorkFactory.cs
```

## Key Concepts

### Tenancy

`TenantId` is a typed identifier for tenant context. `ITenantAccessor` provides access to the current tenant identity. `ITenantResolutionStrategy` is a contract for mapping tenant identity to database resources (connection strings, schemas)—implementation is provider-specific or application-specific; no default implementation is provided.

### Repositories

Repository interfaces for data access patterns. `IReadOnlyRepository<T>` defines query operations; `IRepository<T>` extends it with mutations. The separation is conceptual—providers may implement both via a single type, and consumers are not required to use the read-only interface.

### Transactions

`IUnitOfWork` exposes a coordination boundary for persistence operations. `ITransactionScope` provides explicit transaction boundaries when needed. Atomicity and coordination semantics depend on the provider implementation—these contracts do not guarantee cross-store transactions or distributed safety.

### Diagnostics

`IDataHealthContributor` allows data components to participate in health checks. `IDataDiagnosticsContext` exposes diagnostic context (correlation ID, node ID) when available—it does not guarantee context exists or define propagation semantics.
