using SQLite;
using Zubrilka.Models;

namespace Zubrilka.Data;

/// <summary>
/// Owns the single SQLite connection for the app and guarantees the schema exists
/// before any repository touches the database. Registered as a singleton in DI.
/// </summary>
public class AppDatabase
{
    // File name of the on-device database, placed in the app's private data directory.
    private const string DatabaseFileName = "zubrilka.db3";

    // Open for read/write, create if missing, and use shared cache for concurrent access.
    private const SQLiteOpenFlags Flags =
        SQLiteOpenFlags.ReadWrite | SQLiteOpenFlags.Create | SQLiteOpenFlags.SharedCache;

    // Built lazily on first use, then reused for the app's lifetime.
    private SQLiteAsyncConnection? _connection;

    // Serializes initialization so concurrent callers can't create the schema twice.
    private readonly SemaphoreSlim _initLock = new(1, 1);

    /// <summary>
    /// Returns the shared connection, creating the file and tables on first call.
    /// Repositories call this instead of holding their own connection.
    /// </summary>
    public async Task<SQLiteAsyncConnection> GetConnectionAsync()
    {
        // Fast path: already initialized, no locking needed.
        if (_connection is not null)
            return _connection;

        await _initLock.WaitAsync();
        try
        {
            // Double-check: another caller may have initialized while we waited on the lock.
            if (_connection is null)
            {
                // AppDataDirectory is Android's private per-app storage (no permissions needed).
                var path = Path.Combine(FileSystem.AppDataDirectory, DatabaseFileName);
                var connection = new SQLiteAsyncConnection(path, Flags);

                // Create tables if they don't exist yet (safe to call every startup).
                await connection.CreateTableAsync<Block>();
                await connection.CreateTableAsync<Card>();
                await connection.CreateTableAsync<AppSettings>();

                _connection = connection;
            }
        }
        finally
        {
            _initLock.Release();
        }

        return _connection;
    }
}
