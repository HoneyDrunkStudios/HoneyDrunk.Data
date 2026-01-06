# 🔧 Orchestration - Persistence-Neutral Kernel Integration

[← Back to File Guide](FILE_GUIDE.md)

---

## Table of Contents

- [Overview](#overview)
- [Design Constraints](#design-constraints)
- [Configuration](#configuration)
  - [DataOptions.cs](#dataoptionscs)
- [Tenancy](#tenancy)
  - [KernelTenantAccessor.cs](#kerneltenantaccessorcs)
- [Diagnostics](#diagnostics)
  - [DataActivitySource.cs](#dataactivitysourcecs)
  - [KernelDataDiagnosticsContext.cs](#kerneldatadiagnosticscontextcs)
- [Registration](#registration)
  - [ServiceCollectionExtensions.cs](#servicecollectionextensionscs)
- [Health Integration](#health-integration)

---

## Overview

The orchestration layer bridges HoneyDrunk.Kernel with the data layer. It provides Kernel-aware implementations of data abstractions without depending on any specific database provider.

**Location:** `HoneyDrunk.Data/`

This layer provides:
- Tenant identity extraction from Kernel's `IOperationContext`
- Diagnostic context extraction for correlation IDs and node identity
- Configuration options and service registration

**Scope clarification:** This layer is persistence-neutral (no EF Core dependency) but requires Kernel runtime. `HoneyDrunk.Data.Abstractions` can be used without Kernel; this orchestration layer cannot.

---

## Design Constraints

### No EF Core Dependencies

The orchestration layer has no EF Core or database-specific dependencies.

| Dependency | Status | Notes |
|------------|--------|-------|
| `HoneyDrunk.Kernel` | ✅ Required | For context integration |
| `HoneyDrunk.Data.Abstractions` | ✅ Allowed | Core contracts |
| `Microsoft.EntityFrameworkCore` | ❌ Forbidden | Provider concern |
| Database-specific types | ❌ Forbidden | Provider concern |

> **Rule:** If it requires Entity Framework or a database connection, it belongs in a provider package.

### Kernel Is Required

This layer assumes Kernel is present. It is not designed for standalone use without the Grid runtime.

**Note:** `HoneyDrunk.Data.Abstractions` has no Kernel runtime dependency and can be referenced by domain projects independently. This orchestration layer specifically requires Kernel.

[↑ Back to top](#table-of-contents)

---

## Configuration

### DataOptions.cs

```csharp
public sealed class DataOptions
{
    public string DefaultConnectionStringName { get; set; } = "Default";
    
    public bool EnableQueryTagging { get; set; } = true;
}
```

#### Purpose

Configuration options for the data layer. Provides settings consumed by provider implementations.

#### Properties

| Property | Default | Description |
|----------|---------|-------------|
| `DefaultConnectionStringName` | `"Default"` | Name of the default connection string in configuration |
| `EnableQueryTagging` | `true` | Hint for providers to enable correlation tagging (provider-dependent) |

#### Design Notes

- **`EnableQueryTagging`** is a configuration hint. Actual query tagging requires provider support (e.g., EF Core relational with command interception). Non-relational or non-EF providers may ignore this setting.

#### Usage Example

```csharp
builder.Services.AddHoneyDrunkData(options =>
{
    options.DefaultConnectionStringName = "MainDatabase";
    options.EnableQueryTagging = true;  // Provider must support this
});
```

[↑ Back to top](#table-of-contents)

---

## Tenancy

### KernelTenantAccessor.cs

```csharp
public sealed class KernelTenantAccessor : ITenantAccessor
{
    private readonly IOperationContextAccessor _contextAccessor;

    public KernelTenantAccessor(IOperationContextAccessor contextAccessor)
    {
        ArgumentNullException.ThrowIfNull(contextAccessor);
        _contextAccessor = contextAccessor;
    }

    public TenantId GetCurrentTenantId()
    {
        var tenantId = _contextAccessor.CurrentContext?.TenantId;
        return string.IsNullOrEmpty(tenantId) 
            ? default 
            : TenantId.FromString(tenantId);
    }
}
```

#### Purpose

Extracts tenant identity from Kernel's operation context. This bridges the Grid context with tenant-aware data access.

#### How It Works

```
HTTP Request              Kernel Middleware           Data Layer
     ↓                         ↓                         ↓
X-Tenant-Id: abc → IOperationContext.TenantId → KernelTenantAccessor
     ↓                         ↓                         ↓
                       OperationContext          TenantId.FromString("abc")
```

#### Design Notes

- **No automatic filtering.** This accessor provides tenant identity only. Application code must explicitly apply tenant filtering to queries.
- Returning `default` (empty `TenantId`) does not prevent operations—consumers must validate and enforce tenant requirements.

#### Usage Example

```csharp
// The accessor is automatically registered
// and injected into repositories/services

public class OrderRepository
{
    private readonly ITenantAccessor _tenantAccessor;

    public async Task<Order?> FindByIdAsync(Guid id)
    {
        var tenantId = _tenantAccessor.GetCurrentTenantId();
        
        // Application code must explicitly filter by tenant
        // This is NOT automatic—omitting this filter is a security risk
        if (tenantId.IsEmpty)
            throw new InvalidOperationException("Tenant context required");
            
        return await _context.Orders
            .Where(o => o.TenantId == tenantId.Value)
            .FirstOrDefaultAsync(o => o.Id == id);
    }
}
```

[↑ Back to top](#table-of-contents)

---

## Diagnostics

### DataActivitySource.cs

```csharp
public static class DataActivitySource
{
    public const string Name = "HoneyDrunk.Data";

    private static readonly ActivitySource Source = new(Name);

    public static Activity? StartActivity(
        string operationName,
        ActivityKind kind = ActivityKind.Internal)
    {
        return Source.StartActivity(operationName, kind);
    }

    public static Activity? StartQueryActivity(string entityType)
    {
        var activity = Source.StartActivity($"Query {entityType}", ActivityKind.Client);
        activity?.SetTag("db.operation", "query");
        activity?.SetTag("db.entity", entityType);
        return activity;
    }

    public static Activity? StartSaveActivity()
    {
        var activity = Source.StartActivity("SaveChanges", ActivityKind.Client);
        activity?.SetTag("db.operation", "save");
        return activity;
    }
}
```

#### Purpose

Provides OpenTelemetry-compatible activity sources for data operations at the application layer.

#### Design Notes

- **Application-layer tracing.** These activities trace application intent (e.g., "query Order"), not actual database execution. EF Core and database providers emit their own diagnostic events for actual DB operations.
- Activities created here may not align exactly with database execution timing. For precise DB tracing, rely on provider-level diagnostics.

#### Activity Types

| Method | Operation | Tags |
|--------|-----------|------|
| `StartQueryActivity` | Query intent | `db.operation=query`, `db.entity=<type>` |
| `StartSaveActivity` | Save intent | `db.operation=save` |
| `StartActivity` | Custom operations | User-defined |

#### Usage Example

```csharp
public async Task<Order?> FindByIdAsync(Guid id, CancellationToken ct)
{
    // Traces application-layer query intent
    using var activity = DataActivitySource.StartQueryActivity("Order");
    activity?.SetTag("order.id", id.ToString());

    var order = await _context.Orders.FindAsync([id], ct);

    activity?.SetTag("order.found", order is not null);
    return order;
}
```

[↑ Back to top](#table-of-contents)

---

### KernelDataDiagnosticsContext.cs

```csharp
public sealed class KernelDataDiagnosticsContext : IDataDiagnosticsContext
{
    private readonly IOperationContextAccessor _operationContextAccessor;

    public KernelDataDiagnosticsContext(IOperationContextAccessor operationContextAccessor)
    {
        ArgumentNullException.ThrowIfNull(operationContextAccessor);
        _operationContextAccessor = operationContextAccessor;
    }

    public string? CorrelationId => _operationContextAccessor.Current?.CorrelationId;

    public string? OperationId => _operationContextAccessor.Current?.OperationId;

    public string? NodeId => _operationContextAccessor.Current?.GridContext?.NodeId;

    public IReadOnlyDictionary<string, string> Tags
    {
        get
        {
            var context = _operationContextAccessor.Current;
            if (context is null)
            {
                return new Dictionary<string, string>();
            }

            var tags = new Dictionary<string, string>();
            
            if (!string.IsNullOrEmpty(context.CorrelationId))
                tags["correlation.id"] = context.CorrelationId;
            
            if (!string.IsNullOrEmpty(context.OperationId))
                tags["operation.id"] = context.OperationId;
            
            if (!string.IsNullOrEmpty(context.GridContext?.NodeId))
                tags["node.id"] = context.GridContext.NodeId;
            
            return new ReadOnlyDictionary<string, string>(tags);
        }
    }
}
```

#### Purpose

Extracts diagnostic context from Kernel for data operations. Provides correlation IDs, operation IDs, node identity, and telemetry tags when Kernel context is available.

#### Properties

| Property | Source | Description |
|----------|--------|-------------|
| `CorrelationId` | `IOperationContext.CorrelationId` | Distributed tracing correlation ID |
| `OperationId` | `IOperationContext.OperationId` | Current operation identifier |
| `NodeId` | `IOperationContext.GridContext?.NodeId` | Current node identifier |
| `Tags` | Computed | Available context as key-value pairs |

#### Design Notes

- **Context-dependent.** All properties return `null` when Kernel context is not available (e.g., background jobs without operation context).
- **String representation** for transport compatibility. Values are extracted from Kernel context and converted to strings for SQL comment embedding and log enrichment.

#### Usage Example

```csharp
// Used by CorrelationCommandInterceptor to tag SQL commands (EF Core relational only)
public class CorrelationCommandInterceptor : DbCommandInterceptor
{
    private readonly IDataDiagnosticsContext _diagnosticsContext;

    private void AddCorrelationComment(DbCommand command)
    {
        var correlationId = _diagnosticsContext.CorrelationId;
        if (string.IsNullOrEmpty(correlationId)) return;

        command.CommandText = $"/* correlation:{correlationId} */\n{command.CommandText}";
    }
}
```

[↑ Back to top](#table-of-contents)

---

## Registration

### ServiceCollectionExtensions.cs

```csharp
public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddHoneyDrunkData(
        this IServiceCollection services,
        Action<DataOptions>? configureOptions = null)
    {
        ArgumentNullException.ThrowIfNull(services);

        var options = new DataOptions();
        configureOptions?.Invoke(options);

        services.AddSingleton(Options.Create(options));
        services.AddScoped<ITenantAccessor, KernelTenantAccessor>();
        services.AddScoped<IDataDiagnosticsContext, KernelDataDiagnosticsContext>();

        return services;
    }

    public static IServiceProvider ValidateHoneyDrunkDataConfiguration(
        this IServiceProvider services)
    {
        // Validate required services are registered
        _ = services.GetRequiredService<ITenantAccessor>();
        _ = services.GetRequiredService<IDataDiagnosticsContext>();
        
        return services;
    }
}
```

#### Purpose

Provides dependency injection registration for the data orchestration layer.

#### Registration Methods

| Method | Description |
|--------|-------------|
| `AddHoneyDrunkData()` | Register core data services |
| `ValidateHoneyDrunkDataConfiguration()` | Validate required services are registered |

#### Design Notes

- **`ValidateHoneyDrunkDataConfiguration()`** requires manual invocation. "Fail-fast at startup" is only achieved if the application explicitly calls this method. Consider integrating with Kernel's startup validation for automatic enforcement.

#### Usage Example

```csharp
var builder = WebApplication.CreateBuilder(args);

// Register Kernel first (required for orchestration layer)
builder.Services.AddHoneyDrunkGrid(options => { /* ... */ });

// Register Data orchestration layer
builder.Services.AddHoneyDrunkData(options =>
{
    options.DefaultConnectionStringName = "MainDb";
    options.EnableQueryTagging = true;
});

// Register provider
builder.Services.AddHoneyDrunkDataSqlServer<MyDbContext>(sqlOptions =>
{
    sqlOptions.ConnectionString = builder.Configuration.GetConnectionString("MainDb");
});

// Optional: Validate registrations before building
builder.Services.ValidateHoneyDrunkDataRegistration();

var app = builder.Build();

// Optional: Validate configuration at runtime (resolves services)
app.Services.ValidateHoneyDrunkDataConfiguration();

app.Run();
```

[↑ Back to top](#table-of-contents)

---

## Health Integration

### How Data Contributors Plug Into The Grid

Health contributors are **passive components**—they do not proactively monitor. The integration model:

```
┌─────────────────────────────────────────────────────────────────────────────┐
│                              Host Health System                              │
│                    (ASP.NET Core HealthChecks, Kernel)                       │
└─────────────────────────────────────────────────────────────────────────────┘
                                       │
                                       │ Invokes on-demand
                                       ▼
┌─────────────────────────────────────────────────────────────────────────────┐
│                        IDataHealthContributor                                │
│                    (Registered via DI, multiple allowed)                     │
│                                                                              │
│  ┌─────────────────────────────────────────────────────────────────────────┐│
│  │ DbContextHealthContributor<AppDbContext>                                 ││
│  │ SqlServerHealthContributor<AppDbContext>                                 ││
│  │ (Any custom contributors)                                                ││
│  └─────────────────────────────────────────────────────────────────────────┘│
└─────────────────────────────────────────────────────────────────────────────┘
```

**Wiring model:** Data provides contributors; the host (Kernel or ASP.NET Core) aggregates and invokes them. Contributors are registered in DI via `IEnumerable<IDataHealthContributor>`.

### Integration With ASP.NET Core Health Checks

```csharp
// Map health endpoint that invokes data contributors
app.MapGet("/health/data", async (IEnumerable<IDataHealthContributor> contributors, CancellationToken ct) =>
{
    var results = new List<object>();
    
    foreach (var contributor in contributors)
    {
        var result = await contributor.CheckHealthAsync(ct);
        results.Add(new 
        { 
            Name = contributor.Name, 
            Status = result.Status.ToString(),
            Description = result.Description 
        });
    }
    
    var overallStatus = results.Any(r => ((dynamic)r).Status == "Unhealthy") 
        ? "Unhealthy" 
        : "Healthy";
    
    return Results.Ok(new { Status = overallStatus, Components = results });
});
```

### Integration With Kernel Health (Future)

When Kernel provides a health aggregation system, Data contributors will participate via the same `IDataHealthContributor` interface. The aggregation responsibility moves to Kernel:

```csharp
// Future: Kernel aggregates all health contributors
builder.Services.AddHoneyDrunkHealth(options =>
{
    options.IncludeDataContributors = true;  // Auto-discovers IDataHealthContributor
});
```

**Current state:** Kernel health aggregation is not yet implemented. Applications must wire contributors into their health endpoint manually.

### Contributor Naming

The `Name` property should be:

| Requirement | Guidance |
|-------------|----------|
| **Unique per contributor type** | Include context type: `"SQL Server (AppDbContext)"` |
| **Stable across restarts** | Derive from type names, not instance IDs |
| **Environment-agnostic** | Don't include connection strings or server names in `Name` |
| **Descriptive in output** | Include provider and context: `"Database (OrdersDbContext)"` |

```csharp
// Good: Stable, descriptive
public string Name => $"SQL Server ({typeof(TContext).Name})";

// Bad: Includes runtime-specific info
public string Name => $"Database @ {_connectionString}";
```

### Concurrency and Frequency Safety

Health contributors **should be safe** for:

| Scenario | Expectation |
|----------|-------------|
| **Concurrent calls** | Multiple health checks can run simultaneously; contributors should not hold locks |
| **Frequent calls** | Health endpoints may be polled frequently (e.g., every 10s); contributors should complete quickly |
| **Timeout handling** | Contributors should respect `CancellationToken`; long operations should be avoided |

```csharp
public async ValueTask<DataHealthResult> CheckHealthAsync(CancellationToken ct = default)
{
    try
    {
        // Quick connectivity check—should complete in milliseconds
        var canConnect = await _context.Database.CanConnectAsync(ct);
        return canConnect 
            ? DataHealthResult.Healthy() 
            : DataHealthResult.Unhealthy("Cannot connect");
    }
    catch (OperationCanceledException)
    {
        return DataHealthResult.Unhealthy("Health check timed out");
    }
    catch (Exception ex)
    {
        return DataHealthResult.Unhealthy($"Error: {ex.Message}");
    }
}
```

**Note:** `SqlServerHealthContributor` executes a metadata query (`SELECT @@SERVERNAME, DB_NAME()`). This is lightweight but not zero-cost. Consider caching results if health checks are invoked very frequently.

[↑ Back to top](#table-of-contents)

---

## Summary

The orchestration layer provides the bridge between HoneyDrunk.Kernel and the data layer. Key responsibilities:

- **Tenant extraction** - `KernelTenantAccessor` extracts tenant identity from Grid context (does not enforce filtering)
- **Diagnostics context** - `KernelDataDiagnosticsContext` provides correlation/node info for telemetry enrichment
- **Application-layer telemetry** - `DataActivitySource` creates activities for application intent (not DB execution)
- **Configuration** - `DataOptions` provides settings consumed by provider implementations
- **Registration** - `AddHoneyDrunkData()` wires up Kernel-aware services

This layer has no EF Core or database dependencies. It is persistence-neutral but Kernel-required. Alternative providers implementing Data abstractions can use this orchestration layer if they also use Kernel runtime.

**Clarification:** `HoneyDrunk.Data.Abstractions` has no Kernel dependency and can be used independently. This orchestration layer specifically requires Kernel.

---

[← Back to File Guide](FILE_GUIDE.md) | [↑ Back to top](#table-of-contents)
