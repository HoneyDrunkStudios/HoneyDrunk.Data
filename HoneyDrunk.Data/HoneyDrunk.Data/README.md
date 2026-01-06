# HoneyDrunk.Data

Persistence-neutral orchestration layer for the HoneyDrunk.Data node. Connects Kernel context to data layer behavior without depending on any specific database provider.

## Purpose

This project bridges the gap between the abstract contracts in `HoneyDrunk.Data.Abstractions` and the concrete provider implementations. It provides:

- Kernel-aware implementations of `ITenantAccessor` and `IDataDiagnosticsContext`
- DI registration helpers and shared configuration options
- Application-layer activity sources for telemetry

**Note:** This package does not implement repository or unit of work contracts—those are provider concerns (e.g., `HoneyDrunk.Data.EntityFramework`).

## Allowed Dependencies

- `HoneyDrunk.Kernel` and `HoneyDrunk.Kernel.Abstractions` - For context and operation integration
- `HoneyDrunk.Data.Abstractions` - The contracts this package implements (tenant accessor, diagnostics context)
- `Microsoft.Extensions.DependencyInjection.Abstractions` - For registration helpers
- `Microsoft.Extensions.Options` - For configuration
- `HoneyDrunk.Standards` - Analyzer package only (PrivateAssets=all)

**Boundary note:** Only this orchestration layer depends on `HoneyDrunk.Kernel`. Abstractions and provider packages should depend on `Kernel.Abstractions` only, unless runtime wiring is specifically required.

## What Must Never Be Added

- **No EntityFrameworkCore references** - That belongs in the EF provider
- **No database-specific code** - No SQL Server, PostgreSQL, etc.
- **No transport or auth references** - HoneyDrunk.Transport and HoneyDrunk.Auth are not data concerns

## Namespace Layout

```
HoneyDrunk.Data
├── Configuration/        # Options and settings
│   └── DataOptions.cs
├── Diagnostics/          # Telemetry infrastructure
│   ├── DataActivitySource.cs
│   └── KernelDataDiagnosticsContext.cs
├── Registration/         # DI registration helpers
│   └── ServiceCollectionExtensions.cs
└── Tenancy/              # Kernel-aware tenant identity access
    └── KernelTenantAccessor.cs
```

## Usage

```csharp
// Register orchestration layer (provides tenant accessor, diagnostics context)
services.AddHoneyDrunkData(options =>
{
    options.DefaultConnectionStringName = "MainDb";
    options.EnableQueryTagging = true;  // Hint for providers that support it
});

// Then add a provider package (e.g., HoneyDrunk.Data.SqlServer)
// Provider registration is separate and documented in provider packages
```

## Kernel Integration

When Kernel context is available and services are wired through DI, this package:

- **Exposes tenant identity** via `KernelTenantAccessor` from `IOperationContext.TenantId`
- **Provides diagnostic context** via `KernelDataDiagnosticsContext` (correlation ID, operation ID, node ID)
- **Offers activity sources** via `DataActivitySource` for application-layer tracing

**Note:** Context availability depends on Kernel middleware and operation lifecycle. Background jobs, console utilities, and tests running outside Kernel context will have empty/default values. Persistence enrichment is available when context is present, not guaranteed in all scenarios.
