using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore.Diagnostics;
using SmartPOS.Infrastructure.Data;
using System.Data.Common;

namespace SmartPOS.Infrastructure.Interceptors;

/// <summary>
/// EF Core DbConnection Interceptor that applies critical SQLite PRAGMAs
/// automatically on every new database connection.
///
/// This ensures WAL mode and performance settings are always active
/// regardless of how the connection was opened (pooled or new).
///
/// PRAGMAs applied:
///   - journal_mode=WAL  : concurrent reads during writes, no full-file locks
///   - synchronous=NORMAL: safe durability, ~3x faster than FULL
///   - cache_size        : 50MB in-memory page cache
///   - wal_autocheckpoint: checkpoint WAL every 1000 pages
///   - mmap_size         : 256MB memory-mapped I/O for faster reads
/// </summary>
public class SqliteWalInterceptor : DbConnectionInterceptor
{
    public override void ConnectionOpened(DbConnection connection, ConnectionEndEventData eventData)
    {
        ApplyPragmas(connection);
        base.ConnectionOpened(connection, eventData);
    }

    public override async Task ConnectionOpenedAsync(
        DbConnection connection,
        ConnectionEndEventData eventData,
        CancellationToken cancellationToken = default)
    {
        ApplyPragmas(connection);
        await base.ConnectionOpenedAsync(connection, eventData, cancellationToken);
    }

    private static void ApplyPragmas(DbConnection connection)
    {
        // Only apply to SQLite connections
        if (connection is not SqliteConnection sqliteConnection)
            return;

        try
        {
            using var cmd = sqliteConnection.CreateCommand();

            // WAL mode: reads and writes happen concurrently without blocking
            cmd.CommandText = "PRAGMA journal_mode=WAL;";
            cmd.ExecuteNonQuery();

            // NORMAL synchronous: safe against OS crashes, faster than FULL
            cmd.CommandText = "PRAGMA synchronous=NORMAL;";
            cmd.ExecuteNonQuery();

            // 50MB in-memory page cache (negative = KB, so -51200 = 50MB)
            cmd.CommandText = "PRAGMA cache_size=-51200;";
            cmd.ExecuteNonQuery();

            // Auto-checkpoint WAL file every 1000 pages (~4MB)
            cmd.CommandText = "PRAGMA wal_autocheckpoint=1000;";
            cmd.ExecuteNonQuery();

            // 256MB memory-mapped I/O for faster sequential reads
            cmd.CommandText = "PRAGMA mmap_size=268435456;";
            cmd.ExecuteNonQuery();

            // Busy timeout: wait up to 30s if DB is locked before giving up
            cmd.CommandText = "PRAGMA busy_timeout=30000;";
            cmd.ExecuteNonQuery();

            // Enable foreign key enforcement (disabled by default in SQLite)
            cmd.CommandText = "PRAGMA foreign_keys=ON;";
            cmd.ExecuteNonQuery();

            // Store temporary tables and indices in memory
            cmd.CommandText = "PRAGMA temp_store=MEMORY;";
            cmd.ExecuteNonQuery();
        }
        catch
        {
            // Never crash the application due to PRAGMA failures
            // The app can still function without optimizations
        }
    }
}
