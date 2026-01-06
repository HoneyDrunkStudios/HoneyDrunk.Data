# 🔵 SqlServer - SQL Server and Azure SQL Specialization

[← Back to File Guide](FILE_GUIDE.md)

---

## Table of Contents

- [Overview](#overview)
- [Design Constraints](#design-constraints)
- [Conventions](#conventions)
  - [SqlServerModelConventions.cs](#sqlservermodelconventionscs)
- [Diagnostics](#diagnostics)
  - [SqlServerHealthContributor.cs](#sqlserverhealthcontributorcs)
- [Registration](#registration)
  - [SqlServerDataOptions.cs](#sqlserverdataoptionscs)
  - [ServiceCollectionExtensions.cs](#servicecollectionextensionscs)

---

## Overview

SQL Server specialization for HoneyDrunk.Data. Provides SQL Server-specific configuration, model conventions, and health diagnostics.

**Location:** `HoneyDrunk.Data.SqlServer/`

This package provides:
- SQL Server connection configuration with retry support
- SQL Server-specific model conventions (optional, opt-in)
- Enhanced health contributor with SQL Server metadata

---

## Design Constraints

### SQL Server Only

This package is exclusively for SQL Server (including Azure SQL variants).

| Database | Status | Notes |
|----------|--------|-------|
| SQL Server | ✅ Supported | On-premises, VM, containers |
| Azure SQL Database | ✅ Supported | Same provider, Azure-specific options available |
| Azure SQL Managed Instance | ✅ Supported | SQL Server compatible, same provider |
| PostgreSQL | ❌ Separate package | Would require `HoneyDrunk.Data.PostgreSQL` |
| MySQL | ❌ Separate package | Would require `HoneyDrunk.Data.MySQL` |
| SQLite | ❌ Testing only | Use `HoneyDrunk.Data.Testing` |

> **Rule:** Database-specific code for other providers belongs in their own packages.

### Azure SQL Variants

Azure SQL Database and Azure SQL Managed Instance use the same SQL Server provider. Configuration differences (retry policies, connection options) are handled via options, not separate provider calls.

| Scenario | Registration Method |
|----------|---------------------|
| On-premises SQL Server | `AddHoneyDrunkDataSqlServer()` |
| SQL Server in VM/container | `AddHoneyDrunkDataSqlServer()` |
| Azure SQL Database | `AddHoneyDrunkDataSqlServer()` with Azure-appropriate options |
| Azure SQL Managed Instance | `AddHoneyDrunkDataSqlServer()` (SQL Server compatible) |

[↑ Back to top](#table-of-contents)

---

## Conventions

### SqlServerModelConventions.cs

```csharp
public static class SqlServerModelConventions
{
    public static ModelBuilder UseDateTime2ForAllDateTimeProperties(this ModelBuilder modelBuilder)
    {
        ArgumentNullException.ThrowIfNull(modelBuilder);

        foreach (var property in modelBuilder.Model.GetEntityTypes()
            .SelectMany(t => t.GetProperties())
            .Where(p => p.ClrType == typeof(DateTime) || p.ClrType == typeof(DateTime?)))
        {
            property.SetColumnType("datetime2");
        }

        return modelBuilder;
    }

    public static ModelBuilder ConfigureDecimalPrecision(
        this ModelBuilder modelBuilder,
        int precision = 18,
        int scale = 2)
    {
        ArgumentNullException.ThrowIfNull(modelBuilder);

        foreach (var property in modelBuilder.Model.GetEntityTypes()
            .SelectMany(t => t.GetProperties())
            .Where(p => p.ClrType == typeof(decimal) || p.ClrType == typeof(decimal?)))
        {
            property.SetPrecision(precision);
            property.SetScale(scale);
        }

        return modelBuilder;
    }
}
```

#### Purpose

SQL Server-specific model conventions. These are **optional, opt-in utilities**—not applied by default.

#### Available Conventions

| Method | Description |
|--------|-------------|
| `UseDateTime2ForAllDateTimeProperties` | Use `datetime2` instead of `datetime` for DateTime properties |
| `ConfigureDecimalPrecision` | Set precision/scale for all decimal properties |

#### datetime2 Considerations

| Column Type | Precision | Storage | Range |
|-------------|-----------|---------|-------|
| `datetime` | 3.33ms | 8 bytes | 1753-9999 |
| `datetime2` | 100ns (default) | 6-8 bytes | 0001-9999 |

`datetime2` is generally preferred for new development due to higher precision and larger date range. Storage size varies by precision—the convention uses default precision, not a storage-optimized setting.

#### Design Notes

- **Not applied by default.** Applications must explicitly call these methods.
- **Schema impact.** These conventions modify column types. Evaluate compatibility with existing schemas before applying.

#### Usage Example

```csharp
protected override void OnModelCreating(ModelBuilder modelBuilder)
{
    base.OnModelCreating(modelBuilder);

    // Optional: apply SQL Server conventions
    modelBuilder
        .UseDateTime2ForAllDateTimeProperties()
        .ConfigureDecimalPrecision(precision: 18, scale: 4);
}
```

[↑ Back to top](#table-of-contents)

---

## Diagnostics

### SqlServerHealthContributor.cs

```csharp
public class SqlServerHealthContributor<TContext> : IDataHealthContributor
    where TContext : DbContext
{
    private readonly TContext _context;

    public SqlServerHealthContributor(TContext context)
    {
        ArgumentNullException.ThrowIfNull(context);
        _context = context;
    }

    public string Name => $"SQL Server ({typeof(TContext).Name})";

    public async ValueTask<DataHealthResult> CheckHealthAsync(CancellationToken ct = default)
    {
        try
        {
            var connection = _context.Database.GetDbConnection();
            var wasOpen = connection.State == System.Data.ConnectionState.Open;
            
            try
            {
                if (!wasOpen)
                {
                    await connection.OpenAsync(ct).ConfigureAwait(false);
                }

                await using var command = connection.CreateCommand();
                command.CommandText = "SELECT @@SERVERNAME, DB_NAME()";
            
                await using var reader = await command.ExecuteReaderAsync(ct).ConfigureAwait(false);
            
                if (await reader.ReadAsync(ct).ConfigureAwait(false))
                {
                    var serverName = reader.GetString(0);
                    var databaseName = reader.GetString(1);

                    return DataHealthResult.Healthy($"Connected to {serverName}/{databaseName}");
                }

                return DataHealthResult.Healthy("Connection successful");
            }
            finally
            {
                // Restore connection state if we opened it
                if (!wasOpen && connection.State == System.Data.ConnectionState.Open)
                {
                    await connection.CloseAsync().ConfigureAwait(false);
                }
            }
        }
        catch (Exception ex)
        {
            return DataHealthResult.Unhealthy($"Connection failed: {ex.Message}");
        }
    }
}
```

#### Purpose

Enhanced SQL Server health contributor with server metadata. Invoked by host health system on demand.

#### Health Check Information

| Data | Source |
|------|--------|
| Server name | `@@SERVERNAME` |
| Database name | `DB_NAME()` |

#### Design Notes

- **Connection state preserved.** The health check opens the connection only if needed and restores original state afterward.
- **SQL Server specific.** This contributor executes SQL Server-specific metadata queries. Only register it for DbContexts configured with SQL Server provider.

#### Usage Example

```csharp
// Registered via AddHoneyDrunkDataSqlServer<T>() when RegisterHealthContributors = true

// Health check output
{
    "name": "SQL Server (AppDbContext)",
    "status": "Healthy",
    "description": "Connected to SQLSERVER01/AppDb"
}
```

[↑ Back to top](#table-of-contents)

---

## Registration

### SqlServerDataOptions.cs

```csharp
public sealed class SqlServerDataOptions
{
    public string? ConnectionString { get; set; }
    
    public bool EnableRetryOnFailure { get; set; } = true;
    
    public int MaxRetryCount { get; set; } = 3;
    
    public int MaxRetryDelaySeconds { get; set; } = 30;
    
    public int? CommandTimeoutSeconds { get; set; }
}
```

#### Purpose

Configuration options for SQL Server connections.

#### Properties

| Property | Default | Description |
|----------|---------|-------------|
| `ConnectionString` | `null` | SQL Server connection string (**required**) |
| `EnableRetryOnFailure` | `true` | Enable transient fault handling |
| `MaxRetryCount` | `3` | Maximum retry attempts |
| `MaxRetryDelaySeconds` | `30` | Maximum delay between retries |
| `CommandTimeoutSeconds` | `null` | Command timeout (uses provider default if null) |

#### Design Notes

- **ConnectionString is required.** Registration will fail at runtime if not provided. Consider adding startup validation.

#### Usage Example

```csharp
builder.Services.AddHoneyDrunkDataSqlServer<AppDbContext>(options =>
{
    options.ConnectionString = builder.Configuration.GetConnectionString("SqlServer")
        ?? throw new InvalidOperationException("SqlServer connection string is required");
    options.EnableRetryOnFailure = true;
    options.MaxRetryCount = 5;
    options.MaxRetryDelaySeconds = 60;
    options.CommandTimeoutSeconds = 30;
});
```

[↑ Back to top](#table-of-contents)

---

### ServiceCollectionExtensions.cs

```csharp
public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddHoneyDrunkDataSqlServer<TContext>(
        this IServiceCollection services,
        Action<SqlServerDataOptions> configureSqlServer,
        Action<EfDataOptions>? configureEfOptions = null)
        where TContext : DbContext
    {
        var sqlOptions = new SqlServerDataOptions();
        configureSqlServer(sqlOptions);
        
        if (string.IsNullOrWhiteSpace(sqlOptions.ConnectionString))
        {
            throw new InvalidOperationException(
                "ConnectionString is required for SQL Server configuration.");
        }

        return services.AddHoneyDrunkDataEntityFramework<TContext>(
            options => ConfigureSqlServer(options, sqlOptions),
            configureEfOptions);
    }

    public static IServiceCollection AddHoneyDrunkDataAzureSql<TContext>(
        this IServiceCollection services,
        Action<SqlServerDataOptions> configureSqlServer,
        Action<EfDataOptions>? configureEfOptions = null)
        where TContext : DbContext
    {
        var sqlOptions = new SqlServerDataOptions();
        configureSqlServer(sqlOptions);
        
        if (string.IsNullOrWhiteSpace(sqlOptions.ConnectionString))
        {
            throw new InvalidOperationException(
                "ConnectionString is required for Azure SQL configuration.");
        }

        return services.AddHoneyDrunkDataEntityFramework<TContext>(
            options => ConfigureAzureSql(options, sqlOptions),
            configureEfOptions);
    }

    private static void ConfigureSqlServer(
        DbContextOptionsBuilder options,
        SqlServerDataOptions sqlOptions)
    {
        options.UseSqlServer(sqlOptions.ConnectionString, sqlServerOptions =>
        {
            if (sqlOptions.EnableRetryOnFailure)
            {
                sqlServerOptions.EnableRetryOnFailure(
                    maxRetryCount: sqlOptions.MaxRetryCount,
                    maxRetryDelay: TimeSpan.FromSeconds(sqlOptions.MaxRetryDelaySeconds),
                    errorNumbersToAdd: null);
            }

            if (sqlOptions.CommandTimeoutSeconds.HasValue)
            {
                sqlServerOptions.CommandTimeout(sqlOptions.CommandTimeoutSeconds.Value);
            }
        });
    }

    private static void ConfigureAzureSql(
        DbContextOptionsBuilder options,
        SqlServerDataOptions sqlOptions)
    {
        options.UseAzureSql(sqlOptions.ConnectionString, azureSqlOptions =>
        {
            if (sqlOptions.EnableRetryOnFailure)
            {
                azureSqlOptions.EnableRetryOnFailure(
                    maxRetryCount: sqlOptions.MaxRetryCount,
                    maxRetryDelay: TimeSpan.FromSeconds(sqlOptions.MaxRetryDelaySeconds),
                    errorNumbersToAdd: null);
            }

            if (sqlOptions.CommandTimeoutSeconds.HasValue)
            {
                azureSqlOptions.CommandTimeout(sqlOptions.CommandTimeoutSeconds.Value);
            }
        });
    }
}
```

#### Purpose

Dependency injection registration for SQL Server and Azure SQL.

#### Registration Methods

| Method | Description |
|--------|-------------|
| `AddHoneyDrunkDataSqlServer<T>()` | Register SQL Server provider using `UseSqlServer()` |
| `AddHoneyDrunkDataAzureSql<T>()` | Register Azure SQL provider using `UseAzureSql()` |

#### Design Notes

- **Two registration methods.** `AddHoneyDrunkDataSqlServer` uses EF Core's `UseSqlServer()`; `AddHoneyDrunkDataAzureSql` uses `UseAzureSql()` which may have Azure-specific optimizations.
- **Same options.** Both methods use `SqlServerDataOptions` for configuration.
- **Fail-fast validation.** `ConnectionString` is validated at registration time.

#### Usage Example

**SQL Server (on-premises or VM):**

```csharp
builder.Services.AddHoneyDrunkData();

builder.Services.AddHoneyDrunkDataSqlServer<AppDbContext>(options =>
{
    options.ConnectionString = builder.Configuration.GetConnectionString("SqlServer")
        ?? throw new InvalidOperationException("Connection string required");
    options.EnableRetryOnFailure = true;
    options.MaxRetryCount = 3;
});
```

**Azure SQL Database:**
```csharp
builder.Services.AddHoneyDrunkData();

builder.Services.AddHoneyDrunkDataAzureSql<AppDbContext>(options =>
{
    options.ConnectionString = builder.Configuration.GetConnectionString("AzureSql");
    options.EnableRetryOnFailure = true;
    options.MaxRetryCount = 5;  // Higher for Azure transient faults
    options.CommandTimeoutSeconds = 60;  // Consider serverless cold start
});
```

**Note:** Both methods work for Azure SQL. Choose `AddHoneyDrunkDataAzureSql` if you want EF Core's Azure-specific provider defaults.

[↑ Back to top](#table-of-contents)

---

## Summary

The SqlServer package provides SQL Server-specific functionality:

- **Configuration** - Connection strings, retry policies, timeout options
- **Model Conventions** - Optional `datetime2` and decimal precision utilities
- **Health Contributor** - Server metadata in health checks

Use `AddHoneyDrunkDataSqlServer()` for all SQL Server variants including Azure SQL Database and Azure SQL Managed Instance. Configure Azure-specific options (retry counts, timeouts) as appropriate for your deployment.

---

[← Back to File Guide](FILE_GUIDE.md) | [↑ Back to top](#table-of-contents)
