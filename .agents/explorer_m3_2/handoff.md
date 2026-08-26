# Handoff Report: Explorer M3-2

**Module**: Milestone M3-2 — SQLite Connection String, WAL & PRAGMA Initialization, and GcCompactionService  
**Author**: Explorer M3-2 (`teamwork_preview_explorer`)  
**Working Directory**: `f:\Raw\kasher\kasher\.agents\explorer_m3_2`  
**Date**: 2026-08-08  

---

## 1. Observation

Direct code inspection of `src/SmartPOS.Infrastructure` and `src/SmartPOS.WPF` revealed the following exact locations and states:

1. **`src/SmartPOS.Infrastructure/Data/DatabasePathHelper.cs` (lines 1-40)**:
   - Contains `GetDatabasePath()` returning `%LocalAppData%\RoboVAI\SmartPOS\smartpos.db`.
   - Lacks a centralized connection string builder method that includes SQLite options.
2. **`src/SmartPOS.WPF/App.xaml.cs` (lines 136-138 & 214-219)**:
   - Line 137: `optionsBuilder.UseSqlite($"Data Source={dbPath}", b => b.MigrationsAssembly("SmartPOS.Infrastructure"));`
   - Line 217: `options.UseSqlite($"Data Source={dbPath}", b => b.MigrationsAssembly("SmartPOS.Infrastructure"));`
   - Uses plain connection string without `Busy Timeout = 30000`, `Mode = ReadWriteCreate`, or `Pooling = true`.
3. **`src/SmartPOS.Infrastructure/Data/DbInitializer.cs` (lines 16-65)**:
   - Executes `context.Database.MigrateAsync()` followed by data seeders (`SeedUsersAsync`, `Customers.Add`).
   - Does not execute any SQLite PRAGMA tuning queries (`journal_mode=WAL;`, `busy_timeout=30000;`, `synchronous=NORMAL;`, `temp_store=MEMORY;`, `foreign_keys=ON;`), leaving SQLite in default `DELETE` journal mode.
4. **`src/SmartPOS.Infrastructure/Services/`**:
   - `GcCompactionService.cs` does NOT exist in this directory.
   - Long-running sessions suffer from Large Object Heap (LOH) fragmentation over 24+ hours of continuous WPF execution.

---

## 2. Logic Chain

1. **Observations 1 & 2** show that SQLite connection strings are constructed as raw strings (`Data Source={dbPath}`) in two distinct places in `App.xaml.cs`.
   - *Reasoning*: Constructing raw connection strings duplicates configuration and omits essential SQLite parameters. Adding `GetConnectionString()` to `DatabasePathHelper.cs` centralizes configuration using `SqliteConnectionStringBuilder` with `DataSource = dbPath`, `Mode = SqliteOpenMode.ReadWriteCreate`, `DefaultTimeout = 30` (30000ms), and `Pooling = true`.
2. **Observation 3** shows that SQLite runs under default `DELETE` journal mode without busy timeout handling at database startup.
   - *Reasoning*: Under default `DELETE` journal mode, write operations take an exclusive file lock on `smartpos.db`, blocking concurrent reads and triggering `SQLite Error 5: 'database is locked'` when background operations occur. Executing `PRAGMA journal_mode=WAL;`, `PRAGMA busy_timeout=30000;`, `PRAGMA synchronous=NORMAL;`, `PRAGMA temp_store=MEMORY;`, and `PRAGMA foreign_keys=ON;` in `DbInitializer.InitializeAsync()` enables Write-Ahead Logging, allowing non-blocking concurrent reads during writes and configuring a 30s busy timeout queue.
3. **Observation 4** shows that LOH memory allocations accumulate without periodic compaction in WPF desktop sessions.
   - *Reasoning*: Creating `GcCompactionService.cs` as an `IHostedService` in `SmartPOS.Infrastructure/Services/` with a 15-minute timer executing `GCSettings.LargeObjectHeapCompactionMode = GCLargeObjectHeapCompactionMode.CompactOnce; GC.Collect(2, GCCollectionMode.Forced, blocking: true, compacting: true); GC.WaitForPendingFinalizers();` defragments the LOH. Registering it with `services.AddHostedService<GcCompactionService>();` in `App.xaml.cs` ensures automatic lifecycle management with `_host.StartAsync()`.

---

## 3. Caveats

- **Existing Database File Upgrade**: Enabling `PRAGMA journal_mode=WAL;` creates `.db-wal` and `.db-shm` sidecar files next to `smartpos.db`. File copy/backup routines (such as `TryAutoBackupAsync`) should copy the database file while SQLite connection is idle or use SQLite backup API if running while active.
- No other caveats identified; scope is fully specified for Milestone M3-2.

---

## 4. Conclusion

A complete, actionable implementation strategy for Milestone M3-2 has been produced and documented in `.agents/explorer_m3_2/analysis.md`. The implementer can directly execute edits across `DatabasePathHelper.cs`, `App.xaml.cs`, `DbInitializer.cs`, and create `GcCompactionService.cs` following the exact line numbers and code snippets provided.

---

## 5. Verification Method

1. **Build Test**:
   Execute `dotnet build src/SmartPOS.WPF/SmartPOS.WPF.csproj` to confirm clean compilation.
2. **Database PRAGMA Inspection**:
   Run the application, then verify database PRAGMA settings via SQLite CLI:
   ```sql
   PRAGMA journal_mode; -- Expect 'wal'
   PRAGMA busy_timeout; -- Expect 30000
   PRAGMA synchronous;  -- Expect 1
   ```
3. **GcCompactionService Execution**:
   Verify background service starts with host startup and logs/executes LOH compaction every 15 minutes.
