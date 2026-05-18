using HoneyDrunk.Data.Testing.Fixtures;

namespace HoneyDrunk.Data.Canary.Infrastructure;

/// <summary>
/// Shared SQLite connection for canary outbox tests.
/// File-based to allow concurrent DbContext instances (required for concurrency tests).
/// </summary>
public sealed class CanarySqliteFixture : IAsyncLifetime, IDisposable
{
    private SqliteTestDatabase<CanaryDbContext>? _database;
    private bool _disposed;

    public string ConnectionString => _database?.ConnectionString
        ?? throw new InvalidOperationException("The SQLite canary fixture has not been initialized.");

    public async Task InitializeAsync()
    {
        _database = SqliteTestDatabaseFactory.CreateFileBacked<CanaryDbContext>(
            options => new CanaryDbContext(options));

        await Task.CompletedTask;
    }

    public CanaryDbContext CreateContext()
    {
        return _database?.CreateContext()
            ?? throw new InvalidOperationException("The SQLite canary fixture has not been initialized.");
    }

    public async Task DisposeAsync()
    {
        if (_disposed)
        {
            return;
        }

        if (_database is not null)
        {
            await _database.DisposeAsync();
            _database = null;
        }

        _disposed = true;
    }

    public void Dispose()
    {
        if (_disposed)
        {
            return;
        }

        _database?.Dispose();
        _database = null;
        _disposed = true;
    }
}
