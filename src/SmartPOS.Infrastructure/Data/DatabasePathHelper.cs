using System;
using System.IO;
using Microsoft.Data.Sqlite;

namespace SmartPOS.Infrastructure.Data;

public static class DatabasePathHelper
{
    private const string DatabaseFileName = "smartpos.db";
    private const string AppFolder = "RoboVAI\\SmartPOS";

    /// <summary>
    /// Returns the absolute, consistent path to the database file.
    /// Always uses %LocalAppData%\RoboVAI\SmartPOS\smartpos.db
    /// because the app is installed in Program Files (read-only).
    /// Using a fixed path prevents the race condition where seeding
    /// writes to one location and the UI reads from another.
    /// </summary>
    public static string GetDatabasePath()
    {
        // Developer override via environment variable
        var envPath = Environment.GetEnvironmentVariable("SMARTPOS_DB_PATH");
        if (!string.IsNullOrWhiteSpace(envPath))
        {
            var dir = Path.GetDirectoryName(envPath);
            if (!string.IsNullOrEmpty(dir))
                Directory.CreateDirectory(dir);
            return envPath;
        }

        // Fixed path: %LocalAppData%\RoboVAI\SmartPOS\smartpos.db
        var appDataDir = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            AppFolder);

        Directory.CreateDirectory(appDataDir);
        return Path.Combine(appDataDir, DatabaseFileName);
    }

    /// <summary>
    /// Returns the hardened connection string with WAL mode, Busy Timeout (30s), and connection pooling.
    /// WAL (Write-Ahead Logging) allows concurrent reads during writes — critical for long-running POS sessions.
    /// </summary>
    public static string GetConnectionString()
    {
        var dbPath = GetDatabasePath();
        // Base connection string with pooling and busy timeout
        var csb = new SqliteConnectionStringBuilder
        {
            DataSource = dbPath,
            Mode = SqliteOpenMode.ReadWriteCreate,
            DefaultTimeout = 30, // 30 seconds Busy Timeout (30000 ms)
            Pooling = true,
            // Cache 50MB in memory for frequently accessed pages
            Cache = SqliteCacheMode.Shared,
        };
        return csb.ToString();
    }

    /// <summary>
    /// Applies critical SQLite PRAGMAs for stability to an open connection.
    /// Call this immediately after opening a new connection.
    /// WAL mode: allows concurrent readers during writes (no full-file locks).
    /// synchronous=NORMAL: safe but faster than FULL.
    /// cache_size: 50MB page cache per connection.
    /// </summary>
    public static void ApplyWalPragmas(Microsoft.Data.Sqlite.SqliteConnection connection)
    {
        using var cmd = connection.CreateCommand();
        // Enable WAL (Write-Ahead Logging) — prevents full file locks
        cmd.CommandText = "PRAGMA journal_mode=WAL;";
        cmd.ExecuteNonQuery();

        // Durability: safe for power loss, faster than FULL
        cmd.CommandText = "PRAGMA synchronous=NORMAL;";
        cmd.ExecuteNonQuery();

        // 50MB in-memory page cache (default is 2MB)
        cmd.CommandText = "PRAGMA cache_size=-51200;";
        cmd.ExecuteNonQuery();

        // Auto-checkpoint WAL file every 1000 pages (~4MB)
        cmd.CommandText = "PRAGMA wal_autocheckpoint=1000;";
        cmd.ExecuteNonQuery();

        // Enable memory-mapped I/O (256MB) for faster reads
        cmd.CommandText = "PRAGMA mmap_size=268435456;";
        cmd.ExecuteNonQuery();
    }

    public static string GetDesignTimeDatabasePath() => GetDatabasePath();
}