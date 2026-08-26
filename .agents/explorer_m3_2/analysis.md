# Implementation Strategy Report: SQLite Connection, WAL & PRAGMA Initialization, and GcCompactionService

**Module**: Milestone M3-2 — Desktop Memory & SQLite Concurrency Hardening  
**Target Projects**: `SmartPOS.Infrastructure`, `SmartPOS.WPF`  
**Investigator**: Explorer M3-2 (`teamwork_preview_explorer`)  
**Date**: 2026-08-08  

---

## 1. Executive Summary

This report delivers a precise, step-by-step implementation specification for Milestone M3-2, addressing SQLite connection pooling and timeout handling, Write-Ahead Logging (WAL) & PRAGMA initialization, and automated Large Object Heap (LOH) GC compaction service for 24+ hour uninterrupted WPF application uptime without database locks or memory bloat.

### Summary of Tasks
1. **Centralized SQLite Connection String (`DatabasePathHelper.cs` & `App.xaml.cs`)**: Upgrade connection creation from raw string `Data Source=smartpos.db` to a centralized `SqliteConnectionStringBuilder` configured with `Busy Timeout = 30000`, `Mode = ReadWriteCreate`, and `Pooling = true`.
2. **SQLite Concurrency & WAL PRAGMA Execution (`DbInitializer.cs`)**: Execute SQLite PRAGMA tuning commands (`journal_mode=WAL;`, `busy_timeout=30000;`, `synchronous=NORMAL;`, `temp_store=MEMORY;`, `foreign_keys=ON;`) on DB initialization before/after migration.
3. **Background GC Compaction Service (`GcCompactionService.cs` & `App.xaml.cs`)**: Create a new `IHostedService` in `SmartPOS.Infrastructure/Services/GcCompactionService.cs` that triggers Large Object Heap compaction and full generational garbage collection every 15 minutes, and register it in `App.xaml.cs`.

---

## 2. Task 1: SQLite Connection String Refactoring

### 2.1 Problem Analysis
Currently, SQLite connections are created by constructing raw strings like `Data Source={dbPath}` in `App.xaml.cs` (lines 137 and 217).
- **Missing `Busy Timeout`**: Default timeout is 0ms or short. Concurrent file access (e.g. background auto-backup or sync engines) immediately throws `SQLite Error 5: 'database is locked'`.
- **Missing `Pooling`**: Without connection pooling enabled (`Pooling = true`), open/close connection churn creates file handle overhead and lock contention.
- **Missing `Mode`**: Explicitly declaring `Mode = ReadWriteCreate` ensures correct file creation and read/write mode semantics across installation environments.

### 2.2 Detailed Implementation Instructions

#### File 1: `src/SmartPOS.Infrastructure/Data/DatabasePathHelper.cs`
- **File Path**: `f:\Raw\kasher\kasher\src\SmartPOS.Infrastructure\Data\DatabasePathHelper.cs`
- **Current Lines**: 1-40
- **Required Using Directives**: Add `using Microsoft.Data.Sqlite;`
- **New Method**: `public static string GetConnectionString()`

**Exact Code to Add in `DatabasePathHelper.cs`**:
```csharp
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
    /// Builds and returns the optimized SQLite connection string configured with
    /// Busy Timeout = 30000ms (30s), Mode = ReadWriteCreate, and Pooling = true.
    /// </summary>
    public static string GetConnectionString()
    {
        var dbPath = GetDatabasePath();
        var builder = new SqliteConnectionStringBuilder
        {
            DataSource = dbPath,
            Mode = SqliteOpenMode.ReadWriteCreate,
            DefaultTimeout = 30, // 30 seconds Busy Timeout (30000 ms)
            Pooling = true
        };
        return builder.ConnectionString;
    }

    public static string GetDesignTimeDatabasePath() => GetDatabasePath();
}
```

#### File 2: `src/SmartPOS.WPF/App.xaml.cs`
- **File Path**: `f:\Raw\kasher\kasher\src\SmartPOS.WPF\App.xaml.cs`

1. **In `OnStartup` Database Initialization Block (Lines 133-144)**:
   - *Existing Code*:
     ```csharp
     var dbPath = DatabasePathHelper.GetDatabasePath();
     AgentDebugLog.Write("DB_INIT", "INFO", "DatabasePathUsed", dbPath);

     var optionsBuilder = new DbContextOptionsBuilder<AppDbContext>();
     optionsBuilder.UseSqlite($"Data Source={dbPath}", b => b.MigrationsAssembly("SmartPOS.Infrastructure"));
     using var initContext = new AppDbContext(optionsBuilder.Options);
     ```
   - *Replacement Code*:
     ```csharp
     var dbPath = DatabasePathHelper.GetDatabasePath();
     var connectionString = DatabasePathHelper.GetConnectionString();
     AgentDebugLog.Write("DB_INIT", "INFO", "DatabasePathUsed", dbPath);

     var optionsBuilder = new DbContextOptionsBuilder<AppDbContext>();
     optionsBuilder.UseSqlite(connectionString, b => b.MigrationsAssembly("SmartPOS.Infrastructure"));
     using var initContext = new AppDbContext(optionsBuilder.Options);
     ```

2. **In `BuildHost` DI Registration Block (Lines 214-219)**:
   - *Existing Code*:
     ```csharp
     services.AddDbContext<AppDbContext>(options =>
     {
         var dbPath = DatabasePathHelper.GetDatabasePath();
         options.UseSqlite($"Data Source={dbPath}", b => b.MigrationsAssembly("SmartPOS.Infrastructure"));
     }, ServiceLifetime.Transient);
     ```
   - *Replacement Code*:
     ```csharp
     services.AddDbContext<AppDbContext>(options =>
     {
         var connectionString = DatabasePathHelper.GetConnectionString();
         options.UseSqlite(connectionString, b => b.MigrationsAssembly("SmartPOS.Infrastructure"));
     }, ServiceLifetime.Transient);
     ```

---

## 3. Task 2: SQLite PRAGMA Initialization in DbInitializer.cs

### 3.1 Problem Analysis
Default SQLite database operations run in `DELETE` journal mode.
- In `DELETE` journal mode, write operations take an exclusive file lock on `smartpos.db`, blocking all reader queries. Active reader queries similarly block write operations.
- Under POS transaction load (or when background processes query reports while cashier creates sales), lock escalation occurs, throwing `database is locked`.
- Setting Write-Ahead Logging (`PRAGMA journal_mode=WAL;`) modifies the SQLite database file header to use a separate log file (`smartpos.db-wal`). Readers read committed states concurrently without blocking writers, and writers append to the WAL without blocking readers.

### 3.2 Required PRAGMA Configurations & Technical Justification
1. `PRAGMA journal_mode=WAL;`: Enables Write-Ahead Logging. Concurrency mode allowing multi-reader single-writer non-blocking execution.
2. `PRAGMA busy_timeout=30000;`: Configures SQLite connection to wait up to 30,000 milliseconds (30 seconds) if a lock is encountered before returning SQLITE_BUSY.
3. `PRAGMA synchronous=NORMAL;`: In WAL mode, `NORMAL` sync mode flushes WAL file checkpoints safely without requiring expensive disk-sync barriers on every single transaction, dramatically increasing write IOPS while retaining full crash resilience.
4. `PRAGMA temp_store=MEMORY;`: Instructs SQLite to keep temporary tables, intermediate indices, and sort buffers in RAM rather than creating temp files on disk.
5. `PRAGMA foreign_keys=ON;`: Ensures foreign key relational integrity checks are enforced for all SQLite operations.

### 3.3 Detailed Implementation Instructions

#### File: `src/SmartPOS.Infrastructure/Data/DbInitializer.cs`
- **File Path**: `f:\Raw\kasher\kasher\src\SmartPOS.Infrastructure\Data\DbInitializer.cs`
- **Location**: `public static async Task InitializeAsync(AppDbContext context)` (Lines 16-65)

**Exact Code Modification in `DbInitializer.InitializeAsync`**:
```csharp
public static async Task InitializeAsync(AppDbContext context)
{
    // 1. Ensure schema is updated using Migrations with robust error recovery
    try
    {
        await context.Database.MigrateAsync();
    }
    catch (Exception ex)
    {
        // If migrations fail due to an old out-of-sync DB (e.g. created with EnsureCreated without history)
        var dbPath = DatabasePathHelper.GetDatabasePath();
        if (File.Exists(dbPath))
        {
            var ts = DateTime.Now.ToString("yyyyMMdd_HHmmss");
            var backupPath = dbPath + ".migration_error_bak_" + ts;
            try
            {
                // Close connection, clear SQLite pools, and run garbage collection to release file locks
                await context.Database.CloseConnectionAsync();
                Microsoft.Data.Sqlite.SqliteConnection.ClearAllPools();
                GC.Collect();
                GC.WaitForPendingFinalizers();

                File.Move(dbPath, backupPath);
                await context.Database.MigrateAsync();
            }
            catch
            {
                // Throw original exception if recovery fails
                throw ex;
            }
        }
        else
        {
            throw;
        }
    }

    // 2. Configure SQLite PRAGMA settings for WAL mode, concurrency, and performance
    await context.Database.ExecuteSqlRawAsync("PRAGMA journal_mode=WAL;");
    await context.Database.ExecuteSqlRawAsync("PRAGMA busy_timeout=30000;");
    await context.Database.ExecuteSqlRawAsync("PRAGMA synchronous=NORMAL;");
    await context.Database.ExecuteSqlRawAsync("PRAGMA temp_store=MEMORY;");
    await context.Database.ExecuteSqlRawAsync("PRAGMA foreign_keys=ON;");

    // 3. Seed all required data in dependency order
    var now = DateTime.Now;

    await SeedUsersAsync(context, now);
    
    // Seed only the default cash customer, no sample products or expenses
    if (!await context.Customers.AnyAsync())
    {
        context.Customers.Add(new Customer { Name = "عميل نقدي", Phone = "--", Address = "--", IsActive = true, CreatedAt = now });
        await context.SaveChangesAsync();
    }
}
```

---

## 4. Task 3: Automated Background GC Compaction Service (`GcCompactionService.cs`)

### 4.1 Problem Analysis
In long-running WPF applications (running 24+ hours on POS cashier terminals):
- Heavy UI operations, image rendering, reporting charts, and short-lived allocations generate objects >= 85,000 bytes, which are allocated on the Large Object Heap (LOH).
- Standard GC sweeps do NOT compact the LOH by default. Over time, LOH becomes fragmented, leading to memory growth and eventual `OutOfMemoryException` or sluggishness.
- Setting `GCSettings.LargeObjectHeapCompactionMode = GCLargeObjectHeapCompactionMode.CompactOnce` before a full Generation 2 GC (`GC.Collect(2, ...)`) forces the .NET CLR to compact the LOH, defragmenting heap pages and returning memory to the OS.

### 4.2 Detailed Implementation Instructions

#### File 1: Create `src/SmartPOS.Infrastructure/Services/GcCompactionService.cs`
- **Target File Path**: `f:\Raw\kasher\kasher\src\SmartPOS.Infrastructure\Services\GcCompactionService.cs`

**Complete Code Listing for `GcCompactionService.cs`**:
```csharp
using System;
using System.Runtime;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace SmartPOS.Infrastructure.Services;

/// <summary>
/// Automated background service that periodically triggers Large Object Heap (LOH) compaction
/// and full generational Garbage Collection to eliminate memory fragmentation in long-running desktop sessions.
/// Runs every 15 minutes.
/// </summary>
public class GcCompactionService : IHostedService, IDisposable
{
    private readonly ILogger<GcCompactionService>? _logger;
    private Timer? _timer;
    private static readonly TimeSpan CompactionInterval = TimeSpan.FromMinutes(15);

    public GcCompactionService(ILogger<GcCompactionService>? logger = null)
    {
        _logger = logger;
    }

    public Task StartAsync(CancellationToken cancellationToken)
    {
        _logger?.LogInformation("GcCompactionService starting. Periodic LOH compaction scheduled every {Interval} minutes.", CompactionInterval.TotalMinutes);

        // Start timer after initial 15-minute delay, repeating every 15 minutes
        _timer = new Timer(ExecuteGcCompaction, null, CompactionInterval, CompactionInterval);

        return Task.CompletedTask;
    }

    /// <summary>
    /// Manually trigger GC collection and LOH compaction on demand.
    /// </summary>
    public void TriggerCompaction()
    {
        ExecuteGcCompaction(null);
    }

    private void ExecuteGcCompaction(object? state)
    {
        try
        {
            long memoryBefore = GC.GetTotalMemory(false);

            // Instruct CLR GC to compact Large Object Heap on the next full GC
            GCSettings.LargeObjectHeapCompactionMode = GCLargeObjectHeapCompactionMode.CompactOnce;
            GC.Collect(2, GCCollectionMode.Forced, blocking: true, compacting: true);
            GC.WaitForPendingFinalizers();

            long memoryAfter = GC.GetTotalMemory(false);
            long freedBytes = memoryBefore - memoryAfter;

            _logger?.LogInformation("LOH Compaction completed. Memory before: {Before:N0} bytes, after: {After:N0} bytes (freed {Freed:N0} bytes).",
                memoryBefore, memoryAfter, freedBytes);
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Error occurred during background GC LOH compaction.");
        }
    }

    public Task StopAsync(CancellationToken cancellationToken)
    {
        _logger?.LogInformation("GcCompactionService stopping.");
        _timer?.Change(Timeout.Infinite, 0);
        return Task.CompletedTask;
    }

    public void Dispose()
    {
        _timer?.Dispose();
        _timer = null;
    }
}
```

#### File 2: Register Service in `src/SmartPOS.WPF/App.xaml.cs`
- **File Path**: `f:\Raw\kasher\kasher\src\SmartPOS.WPF\App.xaml.cs`
- **Location**: In `BuildHost()` method under `ConfigureServices` (around line 230).

**Registration Code**:
```csharp
// Add automated background GC LOH compaction service (runs every 15 minutes)
services.AddHostedService<GcCompactionService>();
```

---

## 5. Implementation Summary Matrix

| # | Target File Path | Action | Class / Method | Line Range | Description of Change |
|---|---|---|---|---|---|
| 1 | `src/SmartPOS.Infrastructure/Data/DatabasePathHelper.cs` | Edit | `DatabasePathHelper` | 1-40 | Add `GetConnectionString()` method using `SqliteConnectionStringBuilder` (`Busy Timeout=30000`, `Mode=ReadWriteCreate`, `Pooling=true`). |
| 2 | `src/SmartPOS.WPF/App.xaml.cs` | Edit | `App.OnStartup` | 136-138 | Use `DatabasePathHelper.GetConnectionString()` instead of raw string `Data Source={dbPath}` for `initContext`. |
| 3 | `src/SmartPOS.WPF/App.xaml.cs` | Edit | `App.BuildHost` | 214-219 | Use `DatabasePathHelper.GetConnectionString()` for `AppDbContext` DI registration. |
| 4 | `src/SmartPOS.Infrastructure/Data/DbInitializer.cs` | Edit | `DbInitializer.InitializeAsync` | 53-58 | Execute PRAGMA statements (`journal_mode=WAL;`, `busy_timeout=30000;`, `synchronous=NORMAL;`, `temp_store=MEMORY;`, `foreign_keys=ON;`). |
| 5 | `src/SmartPOS.Infrastructure/Services/GcCompactionService.cs` | Create | `GcCompactionService` | New File | Implement `IHostedService` that runs LOH compaction (`CompactOnce` + `GC.Collect`) every 15 minutes via timer. |
| 6 | `src/SmartPOS.WPF/App.xaml.cs` | Edit | `App.BuildHost` | ~230 | Register `services.AddHostedService<GcCompactionService>();` in container. |

---

## 6. Verification and Invalidation Criteria

### 6.1 Independent Verification Method
1. **Build Verification**:
   - Run `dotnet build src/SmartPOS.WPF/SmartPOS.WPF.csproj` to confirm compilation without errors or warnings.
2. **Database Mode & Lock Test**:
   - Launch application, let database initialize.
   - Inspect SQLite database using `sqlite3` CLI or DB Browser for SQLite:
     ```sql
     PRAGMA journal_mode; -- Should return 'wal'
     PRAGMA busy_timeout; -- Should return 30000
     PRAGMA synchronous;  -- Should return 1 (NORMAL)
     ```
3. **GC Compaction Test**:
   - Trigger `TriggerCompaction()` or observe memory behavior over 15 minutes.
   - Verify unmanaged memory drops and process working set stabilizes without LOH fragmentation growth.

### 6.2 Invalidation Conditions
- If SQLite throws `SQLite Error 5: 'database is locked'`, verify that `PRAGMA journal_mode=WAL;` was executed on the initialized database file.
- If `GcCompactionService` fails to start automatically, verify `AddHostedService` registration in `App.xaml.cs` and `_host.StartAsync()` execution.
