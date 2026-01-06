# 🧪 Testing - SQLite Factories, Fixtures, and Helpers

[← Back to File Guide](FILE_GUIDE.md)

---

## Table of Contents

- [Overview](#overview)
- [Design Guidance](#design-guidance)
- [Factories](#factories)
  - [SqliteTestDbContextFactory.cs](#sqlitetestdbcontextfactorycs)
- [Fixtures](#fixtures)
  - [SqliteDbContextFixture.cs](#sqlitedbcontextfixturecs)
- [Helpers](#helpers)
  - [DatabaseResetHelper.cs](#databaseresethelpercs)
  - [TestDoubles.cs](#testdoublescs)
- [Usage Patterns](#usage-patterns)

---

## Overview

Test infrastructure for HoneyDrunk.Data. Provides SQLite in-memory database factories, xUnit fixtures, and helper utilities for data layer testing.

**Location:** `HoneyDrunk.Data.Testing/`

This package provides:
- SQLite in-memory database factories for fast unit/integration tests
- xUnit fixtures with seeding support
- Database reset utilities
- Test doubles for tenant and diagnostics dependencies

---

## Design Guidance

### Intended Usage

This package is intended for test projects. No technical barrier prevents production usage—these are conventions, not enforced constraints.

| Usage Context | Guidance |
|---------------|----------|
| Unit test project | ✅ Primary use case |
| Integration test project | ✅ With awareness of SQLite limitations |
| Production application | ❌ Strongly discouraged |

### SQLite vs Production Database

SQLite is a testing convenience, **not a behavioral mirror** of production databases.

| Aspect | SQLite Behavior | SQL Server Behavior |
|--------|-----------------|---------------------|
| Schemas | Not supported | Supported |
| Computed columns | Limited | Full support |
| Sequences | Not supported | Supported |
| JSON functions | `json_extract` | `OPENJSON` |
| Transaction isolation | Different semantics | Full isolation levels |
| Concurrency | Limited | Full support |
| Foreign key enforcement | Off by default | On by default |

**Implications:**
- Tests passing with SQLite may fail with SQL Server
- Provider-specific behavior (retry, timeouts, locking) is not tested
- For high-fidelity integration tests, consider Testcontainers or real database instances

### EnsureCreated Limitations

These helpers use `EnsureCreated()` which:
- Bypasses migrations entirely
- Does not populate migration history table
- May not reflect production schema for complex models

For migration-dependent tests, use a real database with applied migrations.

[↑ Back to top](#table-of-contents)

---

## Factories

### SqliteTestDbContextFactory.cs

```csharp
public sealed class SqliteTestDbContextFactory<TContext> : IAsyncDisposable
    where TContext : DbContext
{
    private readonly Func<DbContextOptions<TContext>, TContext> _contextFactory;
    private SqliteConnection? _connection;
    private bool _contextCreated;

    public SqliteTestDbContextFactory(Func<DbContextOptions<TContext>, TContext> contextFactory)
    {
        ArgumentNullException.ThrowIfNull(contextFactory);
        _contextFactory = contextFactory;
    }

    public TContext Create()
    {
        if (_contextCreated)
        {
            throw new InvalidOperationException(
                "Create() can only be called once per factory instance. " +
                "Create a new factory for each test or use the fixture pattern.");
        }

        _connection = new SqliteConnection("DataSource=:memory:");
        _connection.Open();

        var options = new DbContextOptionsBuilder<TContext>()
            .UseSqlite(_connection)
            .Options;

        var context = _contextFactory(options);
        context.Database.EnsureCreated();
        _contextCreated = true;

        return context;
    }

    public async ValueTask DisposeAsync()
    {
        if (_connection is not null)
        {
            await _connection.DisposeAsync().ConfigureAwait(false);
            _connection = null;
        }
    }
}
```

#### Purpose

Creates a SQLite in-memory DbContext instance for testing. Each factory instance creates exactly one context.

#### Key Features

| Feature | Description |
|---------|-------------|
| In-memory database | Fast, no disk I/O |
| Schema creation | `EnsureCreated()` called automatically (bypasses migrations) |
| Connection scoping | Database exists only while connection is open |
| Single-use | Factory creates one context; create new factory for new database |

#### Design Notes

- **Connection-scoped database.** SQLite in-memory databases are tied to the connection. The database is destroyed when the connection closes.
- **Single-use factory.** Calling `Create()` twice throws an exception. This prevents accidental connection overwrites and leaked resources.
- **Dispose both context and factory.** The factory owns the connection; disposing only the context leaves the connection open.

#### Usage Example

```csharp
public class OrderRepositoryTests : IAsyncDisposable
{
    private readonly SqliteTestDbContextFactory<AppDbContext> _factory;
    private readonly AppDbContext _context;

    public OrderRepositoryTests()
    {
        _factory = new SqliteTestDbContextFactory<AppDbContext>(
            options => new AppDbContext(
                options,
                TestDoubles.CreateTenantAccessor("test-tenant"),
                TestDoubles.CreateDiagnosticsContext()));
        
        _context = _factory.Create();
    }

    [Fact]
    public async Task AddAsync_ShouldPersistOrder()
    {
        // Arrange
        var repo = new EfRepository<Order, AppDbContext>(_context);
        var order = new Order { Id = Guid.NewGuid(), Total = 99.99m };

        // Act
        await repo.AddAsync(order);
        await _context.SaveChangesAsync();

        // Assert
        var found = await repo.FindByIdAsync(order.Id);
        Assert.NotNull(found);
        Assert.Equal(99.99m, found.Total);
    }

    public async ValueTask DisposeAsync()
    {
        await _context.DisposeAsync();
        await _factory.DisposeAsync();  // Closes connection, destroys database
    }
}
```

[↑ Back to top](#table-of-contents)

---

## Fixtures

### SqliteDbContextFixture.cs

```csharp
public abstract class SqliteDbContextFixture<TContext> : IAsyncLifetime
    where TContext : DbContext
{
    private SqliteConnection? _connection;

    public TContext Context { get; private set; } = null!;

    public async Task InitializeAsync()
    {
        _connection = new SqliteConnection("DataSource=:memory:");
        await _connection.OpenAsync().ConfigureAwait(false);

        var optionsBuilder = new DbContextOptionsBuilder<TContext>()
            .UseSqlite(_connection);

        ConfigureOptions(optionsBuilder);

        Context = CreateContext(optionsBuilder.Options);
        await Context.Database.EnsureCreatedAsync().ConfigureAwait(false);
        await SeedAsync(Context).ConfigureAwait(false);
    }

    public async Task DisposeAsync()
    {
        if (Context is not null)
        {
            await Context.DisposeAsync().ConfigureAwait(false);
        }

        if (_connection is not null)
        {
            await _connection.DisposeAsync().ConfigureAwait(false);
            _connection = null;
        }
    }

    protected abstract TContext CreateContext(DbContextOptions<TContext> options);

    protected virtual Task SeedAsync(TContext context, CancellationToken ct = default)
        => Task.CompletedTask;

    protected virtual void ConfigureOptions(DbContextOptionsBuilder<TContext> optionsBuilder)
    {
    }
}
```

#### Purpose

xUnit fixture base class for shared database context across tests in a class. Supports async initialization and seeding.

#### Key Features

| Feature | Description |
|---------|-------------|
| `IAsyncLifetime` | Async initialization and cleanup via xUnit |
| `IClassFixture<T>` compatible | Shared across all tests in a class |
| Seeding support | Override `SeedAsync()` for test data |
| Configuration hook | Override `ConfigureOptions()` for customization |

#### Design Notes

- **Async lifecycle only.** Uses `IAsyncLifetime` exclusively; xUnit handles the lifecycle.
- **Shared state.** All tests in the class share the same context and data. Suitable for read-only tests.
- **Mutation tests.** If tests modify data, use the factory pattern with per-test instances instead.

#### Usage Example

**Step 1: Create a fixture**

```csharp
public class AppDbContextFixture : SqliteDbContextFixture<AppDbContext>
{
    protected override AppDbContext CreateContext(DbContextOptions<AppDbContext> options)
    {
        return new AppDbContext(
            options,
            TestDoubles.CreateTenantAccessor("test-tenant"),
            TestDoubles.CreateDiagnosticsContext());
    }

    protected override async Task SeedAsync(AppDbContext context, CancellationToken ct)
    {
        context.Orders.AddRange(
            new Order { Id = Guid.NewGuid(), Total = 100m, Status = OrderStatus.Pending },
            new Order { Id = Guid.NewGuid(), Total = 200m, Status = OrderStatus.Completed },
            new Order { Id = Guid.NewGuid(), Total = 300m, Status = OrderStatus.Cancelled }
        );
        
        await context.SaveChangesAsync(ct);
    }
}
```

**Step 2: Use in tests**

```csharp
public class OrderQueryTests : IClassFixture<AppDbContextFixture>
{
    private readonly AppDbContextFixture _fixture;

    public OrderQueryTests(AppDbContextFixture fixture)
    {
        _fixture = fixture;
    }

    [Fact]
    public async Task GetPendingOrders_ShouldReturnOnlyPending()
    {
        // Arrange
        var repo = new EfRepository<Order, AppDbContext>(_fixture.Context);

        // Act
        var pending = await repo.FindAsync(o => o.Status == OrderStatus.Pending);

        // Assert
        Assert.Single(pending);
        Assert.Equal(100m, pending[0].Total);
    }

    [Fact]
    public async Task CountOrders_ShouldReturnThree()
    {
        // Arrange
        var repo = new EfRepository<Order, AppDbContext>(_fixture.Context);

        // Act
        var count = await repo.CountAsync();

        // Assert
        Assert.Equal(3, count);
    }
}
```

[↑ Back to top](#table-of-contents)

---

## Helpers

### DatabaseResetHelper.cs

```csharp
public static class DatabaseResetHelper
{
    public static async Task ClearDataAsync<TContext>(
        TContext context,
        CancellationToken cancellationToken = default)
        where TContext : DbContext
    {
        ArgumentNullException.ThrowIfNull(context);

        var entityTypes = context.Model.GetEntityTypes()
            .Where(e => !e.IsOwned())
            .ToList();

        foreach (var entityType in entityTypes)
        {
            var tableName = entityType.GetTableName();
            var schema = entityType.GetSchema();

            if (string.IsNullOrEmpty(tableName)) continue;

            var fullTableName = string.IsNullOrEmpty(schema)
                ? $"\"{tableName}\""
                : $"\"{schema}\".\"{tableName}\"";

            await context.Database
                .ExecuteSqlRawAsync($"DELETE FROM {fullTableName}", cancellationToken)
                .ConfigureAwait(false);
        }
    }

    public static async Task ResetDatabaseAsync<TContext>(
        TContext context,
        CancellationToken cancellationToken = default)
        where TContext : DbContext
    {
        ArgumentNullException.ThrowIfNull(context);

        await context.Database.EnsureDeletedAsync(cancellationToken).ConfigureAwait(false);
        await context.Database.EnsureCreatedAsync(cancellationToken).ConfigureAwait(false);
    }

    public static void DetachAllEntities<TContext>(TContext context)
        where TContext : DbContext
    {
        ArgumentNullException.ThrowIfNull(context);
        context.ChangeTracker.Clear();
    }
}
```

#### Purpose

Utility methods for resetting database state between tests.

#### Method Reference

| Method | Description | Use Case |
|--------|-------------|----------|
| `ClearDataAsync` | Delete all rows, keep schema | Fast reset between tests |
| `ResetDatabaseAsync` | Drop and recreate database | Full reset when needed |
| `DetachAllEntities` | Clear change tracker | Ensure clean tracking state |

#### Design Notes

- **`ClearDataAsync` limitations.** Deletes tables in model order without foreign key ordering. SQLite often ignores FK constraints by default; real relational databases may fail. Use `ResetDatabaseAsync` for SQLite in-memory databases.
- **Prefer `ResetDatabaseAsync` for SQLite.** Database recreation is fast for in-memory databases and avoids FK ordering issues.

#### Usage Example

```csharp
public class OrderMutationTests : IAsyncLifetime
{
    private SqliteTestDbContextFactory<AppDbContext> _factory = null!;
    private AppDbContext _context = null!;

    public async Task InitializeAsync()
    {
        _factory = new SqliteTestDbContextFactory<AppDbContext>(CreateContext);
        _context = _factory.Create();
    }

    public async Task DisposeAsync()
    {
        await _context.DisposeAsync();
        await _factory.DisposeAsync();
    }

    [Fact]
    public async Task CreateOrder_ShouldPersist()
    {
        // Arrange
        var order = new Order { Total = 100m };

        // Act
        _context.Orders.Add(order);
        await _context.SaveChangesAsync();

        // Assert
        Assert.Equal(1, await _context.Orders.CountAsync());
    }

    private static AppDbContext CreateContext(DbContextOptions<AppDbContext> options)
        => new(options, TestDoubles.CreateTenantAccessor("test"), TestDoubles.CreateDiagnosticsContext());
}
```

[↑ Back to top](#table-of-contents)

---

### TestDoubles.cs

```csharp
public static class TestDoubles
{
    public static ITenantAccessor CreateTenantAccessor(string tenantId)
    {
        return new TestTenantAccessor(TenantId.FromString(tenantId));
    }

    public static ITenantAccessor CreateEmptyTenantAccessor()
    {
        return new TestTenantAccessor(default);
    }

    public static IDataDiagnosticsContext CreateDiagnosticsContext(
        string? correlationId = null,
        string? operationId = null,
        string? nodeId = null)
    {
        return new TestDataDiagnosticsContext(correlationId, operationId, nodeId);
    }

    private sealed class TestTenantAccessor : ITenantAccessor
    {
        private readonly TenantId _tenantId;

        public TestTenantAccessor(TenantId tenantId)
        {
            _tenantId = tenantId;
        }

        public TenantId GetCurrentTenantId() => _tenantId;
    }

    private sealed class TestDataDiagnosticsContext : IDataDiagnosticsContext
    {
        public TestDataDiagnosticsContext(string? correlationId, string? operationId, string? nodeId)
        {
            CorrelationId = correlationId;
            OperationId = operationId;
            NodeId = nodeId;

            var tags = new Dictionary<string, string>();
            if (!string.IsNullOrEmpty(correlationId))
                tags["correlation.id"] = correlationId;
            if (!string.IsNullOrEmpty(operationId))
                tags["operation.id"] = operationId;
            if (!string.IsNullOrEmpty(nodeId))
                tags["node.id"] = nodeId;

            Tags = new ReadOnlyDictionary<string, string>(tags);
        }

        public string? CorrelationId { get; }
        public string? OperationId { get; }
        public string? NodeId { get; }
        public IReadOnlyDictionary<string, string> Tags { get; }
    }
}
```

#### Purpose

Factory methods for creating test doubles of data abstractions.

#### Test Doubles

| Method | Creates | Use Case |
|--------|---------|----------|
| `CreateTenantAccessor(string)` | `ITenantAccessor` with specific tenant | Multi-tenant tests |
| `CreateEmptyTenantAccessor()` | `ITenantAccessor` returning empty tenant | No-tenant scenarios |
| `CreateDiagnosticsContext` | `IDataDiagnosticsContext` | Correlation/telemetry tests |

#### Design Notes

- **Empty tenant accessor available.** `CreateEmptyTenantAccessor` returns a `default` TenantId for scenarios where tenant context is intentionally absent.
- **Test-specific values.** Use identifiable values like `"test-tenant"` or `"test-correlation-123"` to make test output readable.

[↑ Back to top](#table-of-contents)

---

## Usage Patterns

### Pattern 1: Factory per Test Class

Best for test classes where tests may share state within the class but each class gets a fresh database.

```csharp
public class OrderTests : IAsyncLifetime
{
    private SqliteTestDbContextFactory<AppDbContext> _factory = null!;
    private AppDbContext _context = null!;

    public async Task InitializeAsync()
    {
        _factory = new SqliteTestDbContextFactory<AppDbContext>(CreateContext);
        _context = _factory.Create();
    }

    public async Task DisposeAsync()
    {
        await _context.DisposeAsync();
        await _factory.DisposeAsync();
    }

    [Fact]
    public async Task Test1() { /* uses _context */ }

    [Fact]
    public async Task Test2() { /* uses same _context—may see data from Test1 */ }

    private static AppDbContext CreateContext(DbContextOptions<AppDbContext> options)
        => new(options, TestDoubles.CreateTenantAccessor("test"), TestDoubles.CreateDiagnosticsContext());
}
```

### Pattern 2: Shared Fixture for Read-Only Tests

Best for query tests that share seeded data without modification.

```csharp
public class OrderQueryTests : IClassFixture<SeededDbContextFixture>
{
    private readonly SeededDbContextFixture _fixture;

    public OrderQueryTests(SeededDbContextFixture fixture)
    {
        _fixture = fixture;
    }

    [Fact]
    public async Task Query1() { /* reads from seeded data */ }

    [Fact]
    public async Task Query2() { /* reads from same seeded data */ }
}
```

### Pattern 3: Fresh Database per Test

Best for mutation tests requiring complete isolation. More expensive but guarantees no cross-test contamination.

```csharp
public class IsolatedMutationTests
{
    [Fact]
    public async Task CreateOrder_Test1()
    {
        await using var factory = new SqliteTestDbContextFactory<AppDbContext>(CreateContext);
        await using var context = factory.Create();
        
        // Test with fresh database
        context.Orders.Add(new Order { Total = 100m });
        await context.SaveChangesAsync();
        
        Assert.Equal(1, await context.Orders.CountAsync());
    }

    [Fact]
    public async Task CreateOrder_Test2()
    {
        await using var factory = new SqliteTestDbContextFactory<AppDbContext>(CreateContext);
        await using var context = factory.Create();
        
        // Completely independent database
        Assert.Equal(0, await context.Orders.CountAsync());
    }

    private static AppDbContext CreateContext(DbContextOptions<AppDbContext> options)
        => new(options, TestDoubles.CreateTenantAccessor("test"), TestDoubles.CreateDiagnosticsContext());
}
```

[↑ Back to top](#table-of-contents)

---

## Summary

The Testing package provides SQLite-based test infrastructure:

- **SqliteTestDbContextFactory** - Single-use factory for in-memory database contexts
- **SqliteDbContextFixture** - xUnit fixture for shared contexts across test classes
- **DatabaseResetHelper** - Database recreation and change tracker utilities
- **TestDoubles** - Test doubles for tenant and diagnostics dependencies

### Limitations

This package provides **SQLite testing conveniences only**. It does not include:
- Real SQL Server integration test support
- Testcontainers integration
- Transaction rollback-based isolation
- Migration-aware test databases

For high-fidelity integration tests, consider using Testcontainers or a dedicated test database with applied migrations.

### Key Guidance

- SQLite behavior differs from production databases—passing tests don't guarantee production behavior
- Dispose both context and factory to properly clean up resources
- Use identifiable test tenant values rather than empty/null tenants
- Choose the appropriate pattern based on test isolation needs

---

[← Back to File Guide](FILE_GUIDE.md) | [↑ Back to top](#table-of-contents)
