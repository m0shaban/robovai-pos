# Handoff Report — Explorer 2 (Desktop WPF & Database Layer Investigation)

**Date**: 2026-08-08  
**Agent**: `explorer_2` (teamwork_preview_explorer)  
**Task**: Desktop WPF Memory Leak, UI Deadlock, and SQLite Database Lock Investigation (Requirement 3)  
**Target Path**: `f:\Raw\kasher\kasher`  
**Analysis File**: `f:\Raw\kasher\kasher\.agents\explorer_2\analysis.md`  

---

## 1. Observation

Direct code observations from investigation:

- **DbContext Lifetime & ChangeTracker Accumulation**:
  - `src/SmartPOS.WPF/App.xaml.cs` (lines 214-218):
    ```csharp
    services.AddDbContext<AppDbContext>(options =>
    {
        var dbPath = DatabasePathHelper.GetDatabasePath();
        options.UseSqlite($"Data Source={dbPath}", b => b.MigrationsAssembly("SmartPOS.Infrastructure"));
    }, ServiceLifetime.Transient);
    ```
  - `src/SmartPOS.WPF/Views/MainWindow.xaml.cs` (lines 18, 130-164):
    `private readonly Dictionary<int, Page> _pageCache = new();` caches all navigation pages (and their injected ViewModels) permanently.
  - `src/SmartPOS.Application/ViewModels/DashboardViewModel.cs` (lines 82-127):
    `_context.Sales.Where(...).ToListAsync()` queries entities without `.AsNoTracking()`, causing all queried entities to accumulate in `_context.ChangeTracker`.

- **SQLite Locking & PRAGMA Settings**:
  - `src/SmartPOS.Infrastructure/Data/DatabasePathHelper.cs` (lines 18-37): Returns `Data Source={dbPath}` without `Busy Timeout=30000` or `Pooling=True`.
  - `src/SmartPOS.Infrastructure/Data/DbInitializer.cs` (lines 16-65): Missing execution of SQLite WAL mode commands (`PRAGMA journal_mode=WAL;`, `PRAGMA busy_timeout=30000;`, `PRAGMA synchronous=NORMAL;`).

- **LiveCharts & SkiaSharp Unmanaged Memory Leaks**:
  - `src/SmartPOS.Application/ViewModels/ReportsViewModel.cs` (lines 750-839): Instantiates `new SolidColorPaint(...)` and `new LinearGradientPaint(...)` every chart refresh without `Dispose()`.
  - `src/SmartPOS.WPF/Views/ReportsPage.xaml.cs` (lines 23-24):
    ```csharp
    _viewModel = host.Services.GetRequiredService<ReportsViewModel>();
    DataContext = _viewModel;
    ```
    Re-creates `ReportsViewModel` on every `Page_Loaded`, leaving old ViewModels and LiveCharts series event listeners rooted in memory.

- **Video Camera Preview Handles**:
  - `src/SmartPOS.Application/ViewModels/WmsQrBridgeViewModel.cs` (lines 159-160, 417-453): Uses OpenCV `VideoCapture(0)` and `Mat`. Allocates `mat.ToWriteableBitmap()` every 80ms on the UI thread without buffer reuse. If user leaves `SettingsPage` during a scan, `StopWmsQrScan()` is never called, leaving native C++ handles open.

- **Barcode Scanner Navigation Disconnect Bug**:
  - `src/SmartPOS.WPF/Views/POSPage.xaml.cs` (lines 29-35): `Page_Unloaded` calls `_viewModel.Dispose()`, which executes `WeakReferenceMessenger.Default.UnregisterAll(this)`.
  - When returning to the cached `POSPage`, constructor is not re-executed, so `MainPOSViewModel` is **never re-registered** with `WeakReferenceMessenger`, permanently disabling barcode scan event handling.

---

## 2. Logic Chain

1. **Observation**: `AppDbContext` is registered as Transient in DI, but ViewModels are injected with `AppDbContext` and stored in `MainWindow._pageCache`.
   **Reasoning**: Page caching keeps ViewModels alive for the duration of the application. The injected `AppDbContext` becomes effectively Singleton-lived.
   **Step Conclusion**: As users navigate and refresh data, `_context.ChangeTracker` accumulates thousands of tracked entities, causing progressive memory growth and query degradation.

2. **Observation**: SQLite connection string lacks `Busy Timeout` and `DbInitializer` never runs `PRAGMA journal_mode=WAL;`.
   **Reasoning**: In SQLite default `DELETE` mode, writing locks the entire database file against readers. Any concurrent read/write operation throws an immediate `SQLite Error 5: 'database is locked'` exception.
   **Step Conclusion**: Configuring WAL mode (`PRAGMA journal_mode=WAL;`), `Busy Timeout = 30000`, and `synchronous=NORMAL` enables concurrent readers during writes and prevents database lock crashes.

3. **Observation**: LiveCharts paints (`SolidColorPaint`) wrap native Skia objects and are instantiated every render without disposal; `ReportsPage` re-creates `ReportsViewModel` on every load.
   **Reasoning**: LiveCharts controls maintain subscriptions to `ISeries` properties. Replacing `DataContext` without unregistering old series leaves native SkiaSharp paints and old view models in memory.
   **Step Conclusion**: Reusing static paints, disposing old series arrays, and retaining a single `ReportsViewModel` instance resolves LiveCharts memory bloat.

4. **Observation**: OpenCV `VideoCapture(0)` is opened in `WmsQrBridgeViewModel.cs` and `mat.ToWriteableBitmap()` allocates new bitmaps every 80ms; `SettingsPage.xaml.cs` `Page_Unloaded` does not stop scanning.
   **Reasoning**: Unmanaged camera handles remain active in background tasks when switching tabs, and continuous bitmap creation overwhelms the GC.
   **Step Conclusion**: Stopping camera scan on page unload and implementing `IDisposable` eliminates camera handle leaks and bitmap memory churn.

5. **Observation**: Navigating away from `POSPage` calls `_viewModel.Dispose()` which unregisters `MainPOSViewModel` from `WeakReferenceMessenger`. Navigating back loads `POSPage` from cache without re-registering.
   **Reasoning**: `BarcodeScannedMessage` events published by `BarcodeService` fail to reach `MainPOSViewModel` after the first tab switch.
   **Step Conclusion**: Moving messenger registration to page load/appear lifecycle restores barcode scanner functionality across tab navigation.

---

## 3. Caveats

- **Test Environment Dependency**: Verification was performed via static source code analysis, dependency tracing, and lifetime inspection. Hardware devices (physical COM barcode scanners and USB webcams) were verified structurally via `BarcodeService.cs` and `WmsQrBridgeViewModel.cs` code paths.
- **Migration WAL Handling**: When SQLite switches to WAL mode, SQLite creates `.db-wal` and `.db-shm` sidecar files next to `smartpos.db`. `BackupService.cs` (lines 35-38) already includes logic to copy `-wal` and `-shm` files during backups.

---

## 4. Conclusion

The root causes for memory leaks, UI thread deadlocks, barcode scanner disconnects, and SQLite database locks in `src/SmartPOS.WPF` have been identified and traced down to exact file paths and line numbers:
- **DbContext Lifetime**: Must transition from long-lived `AppDbContext` to short-lived `IDbContextFactory<AppDbContext>` scoping with mandatory `.AsNoTracking()`.
- **SQLite Concurrency**: Must enable `PRAGMA journal_mode=WAL;`, `PRAGMA busy_timeout=30000;`, and `PRAGMA synchronous=NORMAL;`.
- **Unmanaged Leaks**: Must reuse SkiaSharp paints, clean up LiveCharts series, stop OpenCV camera capture on page unload, and fix `POSPage` messenger registration lifecycle.
- **Background Maintenance**: Must add a periodic background GC compaction service (`GCSettings.LargeObjectHeapCompactionMode = CompactOnce`).

The complete, actionable fix action plan has been documented in `f:\Raw\kasher\kasher\.agents\explorer_2\analysis.md`.

---

## 5. Verification Method

To independently verify the investigation and future implementation:

1. **Inspect Code Locations**:
   - `src/SmartPOS.WPF/App.xaml.cs` (lines 214-260)
   - `src/SmartPOS.Infrastructure/Data/DatabasePathHelper.cs` (lines 18-37)
   - `src/SmartPOS.Infrastructure/Data/DbInitializer.cs` (lines 16-65)
   - `src/SmartPOS.Application/ViewModels/DashboardViewModel.cs` (lines 82-127)
   - `src/SmartPOS.Application/ViewModels/ReportsViewModel.cs` (lines 750-840)
   - `src/SmartPOS.Application/ViewModels/WmsQrBridgeViewModel.cs` (lines 140-162, 417-463)
   - `src/SmartPOS.WPF/Views/POSPage.xaml.cs` (lines 29-35)
   - `src/SmartPOS.WPF/Views/ReportsPage.xaml.cs` (lines 18-32)

2. **Run Solution Build Command**:
   ```powershell
   dotnet build src/SmartPOS.sln -c Debug
   ```

3. **Run Unit Tests Command**:
   ```powershell
   dotnet test src/SmartPOS.UnitTests/SmartPOS.UnitTests.csproj
   ```

4. **Runtime Invalidation Conditions**:
   - If `AppDbContext` is retained as a long-lived field in ViewModels, `ChangeTracker` bloat will persist.
   - If `PRAGMA journal_mode=WAL;` is omitted, concurrent read/write operations will continue throwing database lock errors.
