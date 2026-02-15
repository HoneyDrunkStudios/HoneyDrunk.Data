using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;

namespace HoneyDrunk.Data.Canary.Infrastructure;

/// <summary>
/// Shared SQLite connection for canary outbox tests.
/// File-based to allow concurrent DbContext instances (required for concurrency tests).
/// </summary>
public sealed class CanarySqliteFixture : IAsyncLifetime, IDisposable
{
    private SqliteConnection? _keepAliveConnection;
    private string _dbPath = null!;
    private bool _disposed;

    public string ConnectionString { get; private set; } = null!;

    public async Task InitializeAsync()
    {
        _dbPath = Path.Combine(Path.GetTempPath(), $"canary_{Guid.NewGuid():N}.db");
        ConnectionString = $"DataSource={_dbPath}";

        // Keep one connection open to prevent SQLite from deleting the file
        _keepAliveConnection = new SqliteConnection(ConnectionString);
        await _keepAliveConnection.OpenAsync();

        await using var context = CreateContext();
        await context.Database.EnsureCreatedAsync();
    }

    public CanaryDbContext CreateContext()
    {
        var options = new DbContextOptionsBuilder<CanaryDbContext>()
            .UseSqlite(ConnectionString)
            .Options;

        return new CanaryDbContext(options);
    }

    public async Task DisposeAsync()
    {
        if (_disposed)
        {
            return;
        }

        if (_keepAliveConnection is not null)
        {
            await _keepAliveConnection.DisposeAsync();
            _keepAliveConnection = null;
        }

        TryDeleteDatabase();
        _disposed = true;
    }

    public void Dispose()
    {
        if (_disposed)
        {
            return;
        }

        _keepAliveConnection?.Dispose();
        _keepAliveConnection = null;

        TryDeleteDatabase();
        _disposed = true;
    }

    private void TryDeleteDatabase()
    {
        try
        {
            if (File.Exists(_dbPath))
                File.Delete(_dbPath);
        }
        catch (IOException)
        {
            // Best-effort cleanup in CI
        }
    }
}
