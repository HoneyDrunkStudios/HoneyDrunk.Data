# HoneyDrunk.Data.Testing

SQLite-based test infrastructure for HoneyDrunk.Data.

## Purpose

Provides test infrastructure:
- SQLite in-memory database factories
- xUnit fixtures for DbContext testing
- Database reset utilities
- Test doubles for data abstractions

**Usage guidance:** This package should only be referenced from test projects (`*.Tests.csproj`). It should not be added as a dependency to runtime services, API projects, or shared libraries.

## Allowed Dependencies

- `HoneyDrunk.Data.Abstractions` - For test double contracts
- `HoneyDrunk.Data.EntityFramework` - The EF Core provider layer
- `Microsoft.EntityFrameworkCore.Sqlite` - For in-memory testing
- Test framework packages (xUnit, etc.)
- `HoneyDrunk.Standards` - Analyzer package only (PrivateAssets=all)

## What Must Never Be Added

- **No production project references** - This is test-only
- **No transport or auth references** - HoneyDrunk.Transport and HoneyDrunk.Auth are not data concerns

## SQLite Limitations

SQLite is a testing convenience, **not a behavioral mirror** of production databases:

- Schema-per-tenant strategies do not work
- SQL Server-specific behavior (locking, isolation levels, JSON functions) is not tested
- Foreign key constraints are off by default in SQLite
- Provider-specific features (retry, timeouts) are not exercised

For high-fidelity integration tests, consider Testcontainers or a real database instance.

## Namespace Layout

```
HoneyDrunk.Data.Testing
├── Factories/            # DbContext factories
│   └── SqliteTestDbContextFactory.cs
├── Fixtures/             # xUnit fixtures
│   └── SqliteDbContextFixture.cs
└── Helpers/              # Utility methods
    ├── DatabaseResetHelper.cs
    └── TestDoubles.cs
```

## Usage Examples

### Using the Factory

```csharp
public class MyRepositoryTests : IAsyncDisposable
{
    private readonly SqliteTestDbContextFactory<MyDbContext> _factory;
    private readonly MyDbContext _context;

    public MyRepositoryTests()
    {
        _factory = new SqliteTestDbContextFactory<MyDbContext>(
            options => new MyDbContext(
                options,
                TestDoubles.CreateTenantAccessor("test-tenant"),
                TestDoubles.CreateDiagnosticsContext()));
        _context = _factory.Create();
    }

    [Fact]
    public async Task AddAsync_ShouldPersistEntity()
    {
        // Direct repository usage is acceptable in tests for isolation
        var repo = new EfRepository<MyEntity, MyDbContext>(_context);
        
        await repo.AddAsync(new MyEntity { Name = "Test" });
        await _context.SaveChangesAsync();
        
        var result = await repo.FindOneAsync(e => e.Name == "Test");
        Assert.NotNull(result);
    }

    public async ValueTask DisposeAsync()
    {
        await _context.DisposeAsync();
        await _factory.DisposeAsync();  // Closes connection, destroys database
    }
}
```

**Note:** Direct `EfRepository` usage in tests is acceptable for isolation. For tests that should mirror production patterns, use `IUnitOfWork<TContext>` via DI.

### Using the Fixture

```csharp
public class MyDbContextFixture : SqliteDbContextFixture<MyDbContext>
{
    protected override MyDbContext CreateContext(DbContextOptions<MyDbContext> options)
    {
        return new MyDbContext(
            options,
            TestDoubles.CreateTenantAccessor("test-tenant"),
            TestDoubles.CreateDiagnosticsContext());
    }

    protected override async Task SeedAsync(MyDbContext context, CancellationToken ct)
    {
        context.Entities.Add(new MyEntity { Name = "Seeded" });
        await context.SaveChangesAsync(ct);
    }
}

public class MyServiceTests : IClassFixture<MyDbContextFixture>
{
    private readonly MyDbContextFixture _fixture;

    public MyServiceTests(MyDbContextFixture fixture)
    {
        _fixture = fixture;
    }

    [Fact]
    public async Task ShouldFindSeededEntity()
    {
        var entity = await _fixture.Context.Entities
            .FirstOrDefaultAsync(e => e.Name == "Seeded");
        
        Assert.NotNull(entity);
    }
}
```

### Using Test Doubles

```csharp
var tenantAccessor = TestDoubles.CreateTenantAccessor("tenant-123");
var diagnostics = TestDoubles.CreateDiagnosticsContext(
    correlationId: "corr-abc",
    operationId: "op-xyz",
    nodeId: "test-node");

var context = new MyDbContext(options, tenantAccessor, diagnostics);

// Or for no-tenant scenarios:
var emptyTenantAccessor = TestDoubles.CreateEmptyTenantAccessor();
```

### Resetting Between Tests

```csharp
[Fact]
public async Task ShouldStartWithCleanState()
{
    // Option 1: Prefer database recreation for SQLite (recommended)
    await DatabaseResetHelper.ResetDatabaseAsync(_context);
    
    // Option 2: Clear data only (may fail with FK constraints on real databases)
    await DatabaseResetHelper.ClearDataAsync(_context);
    
    // Option 3: Just clear change tracker
    DatabaseResetHelper.DetachAllEntities(_context);
    
    // Test with clean database...
}
```

**Note:** `ClearDataAsync` deletes tables in model order without FK ordering. This works for SQLite (FK constraints off by default) but may fail on SQL Server. Prefer `ResetDatabaseAsync` for SQLite in-memory databases.
