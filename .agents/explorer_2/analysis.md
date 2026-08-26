# Technical Analysis: WPF Desktop Memory Leaks, UI Deadlocks & SQLite Database Lock Resolution

**Project**: SmartPOS / RobovAI PRO POS & WMS Ecosystem  
**Target Module**: `src/SmartPOS.WPF`, `src/SmartPOS.Infrastructure`, `src/SmartPOS.Application`  
**Investigator**: Explorer 2 (`teamwork_preview_explorer`)  
**Date**: 2026-08-08  

---

## 1. Executive Summary

A comprehensive technical investigation was performed on `src/SmartPOS.WPF` and its infrastructure/application layers to resolve long-uptime performance degradation, memory growth, UI thread deadlocks, and SQLite `database is locked` errors (Requirement 3).

### Key Findings
1. **EF Core ChangeTracker Accumulation & Long-Lived DbContext**: ViewModels are registered with transient lifetime in DI, but are cached indefinitely inside `MainWindow._pageCache` (WPF `Page` instances). Because ViewModels retain a single `AppDbContext` instance over hours of app runtime and execute tracked queries without `.AsNoTracking()`, EF Core's `ChangeTracker` bloats to thousands of entities, causing heavy memory growth and query slowdowns.
2. **SQLite Database Locks**: SQLite connection string is plain (`Data Source={dbPath}`) without `Busy Timeout=30000` or connection pooling. Furthermore, SQLite runs in default `DELETE` journal mode where readers block writers and writers block readers. Concurrent background transactions (such as auto-backups or WMS sync) immediately fail with `SQLite Error 5: 'database is locked'`.
3. **LiveCharts & SkiaSharp Unmanaged Resource Leaks**: In `ReportsViewModel.cs` (`LoadChartsAsync`), new `SolidColorPaint` and `LinearGradientPaint` objects (wrapping native Skia C++ objects) are instantiated on every chart refresh without `Dispose()`. In `ReportsPage.xaml.cs`, re-creating `ReportsViewModel` on `Page_Loaded` leaves previous view models and LiveCharts series event listeners rooted in memory.
4. **Video Camera Preview Handle & Bitmap Churn**: In `WmsQrBridgeViewModel.cs`, QR camera scanning uses OpenCV `VideoCapture(0)`. A new `WriteableBitmap` is allocated every 80ms (12-15 FPS) on the UI thread without buffer reuse. If a user navigates away from `SettingsPage` during a scan, `StopWmsQrScan()` is never invoked, leaking unmanaged C++ camera handles.
5. **Barcode Scanner Permanent Disconnect on Navigation**: In `POSPage.xaml.cs`, navigating away triggers `Page_Unloaded`, which calls `_viewModel.Dispose()`, unregistering `MainPOSViewModel` from `WeakReferenceMessenger`. When navigating back to the cached `POSPage`, `MainPOSViewModel` is **never re-registered**, causing the barcode scanner to permanently stop receiving scan events until the app restarts.

---

## 2. DbContext Lifetime Scope, Dependency Injection & ChangeTracker Accumulation

### Current Architecture & Failure Mechanism
- **DI Registration** (`src/SmartPOS.WPF/App.xaml.cs` lines 214-220):
  ```csharp
  services.AddDbContext<AppDbContext>(options =>
  {
      var dbPath = DatabasePathHelper.GetDatabasePath();
      options.UseSqlite($"Data Source={dbPath}", b => b.MigrationsAssembly("SmartPOS.Infrastructure"));
  }, ServiceLifetime.Transient);
  ```
- **Page Caching in MainWindow** (`src/SmartPOS.WPF/Views/MainWindow.xaml.cs` lines 18, 130-164):
  ```csharp
  private readonly Dictionary<int, Page> _pageCache = new();
  ```
  When `GetOrCreatePage(index)` is called, WPF `Page` instances (e.g. `DashboardPage`, `POSPage`, `ProductsPage`) are stored in `_pageCache`.
- **ViewModel Lifetime**:
  Each `Page` resolves its corresponding `ViewModel` in its constructor (e.g. `_viewModel = host.Services.GetRequiredService<DashboardViewModel>()`).
  Because `Page` objects remain cached in `_pageCache` for the entire app lifetime, the `ViewModel` instances and their injected `AppDbContext` (`_context`) remain alive indefinitely.
- **ChangeTracker Bloat**:
  In `DashboardViewModel.cs` (lines 82-127), `ProductsViewModel.cs`, `InvoicesViewModel.cs`, and `ExpensesViewModel.cs`, queries are executed without `.AsNoTracking()`:
  ```csharp
  var todaySalesList = await _context.Sales
      .Where(s => s.SaleDate.Date == today && s.Status == SaleStatus.Completed && !s.IsDeleted)
      .ToListAsync(); // <-- Tracked by _context.ChangeTracker!
  ```
  Every time `LoadDashboardData` or `Page_Loaded` is executed, additional entities are appended to `_context.ChangeTracker`. Over 24+ hours, the tracked entity count grows to thousands of objects, holding references to old UI data and degrading EF Core change tracking performance.

### Recommended Fix Strategy
1. Register `IDbContextFactory<AppDbContext>` in DI (`App.xaml.cs`):
   ```csharp
   services.AddDbContextFactory<AppDbContext>(options =>
   {
       var dbPath = DatabasePathHelper.GetDatabasePath();
       var connectionString = new SqliteConnectionStringBuilder
       {
           DataSource = dbPath,
           Mode = SqliteOpenMode.ReadWriteCreate,
           DefaultTimeout = 30, // 30 seconds Busy Timeout
           Pooling = true
       }.ToString();

       options.UseSqlite(connectionString, b => b.MigrationsAssembly("SmartPOS.Infrastructure"));
   });
   ```
2. Convert ViewModels from holding long-lived `AppDbContext` to using short-lived contexts created per operation:
   ```csharp
   using var context = _contextFactory.CreateDbContext();
   var sales = await context.Sales.AsNoTracking()...ToListAsync();
   ```
3. Enforce `.AsNoTracking()` for all read-only listing queries (Dashboard, Reports, Products, Invoices, Expenses, Audit logs).

---

## 3. SQLite Database Lock Investigation & PRAGMA Configuration

### Current Failure Mechanism
- **Connection String**: Currently uses plain string `Data Source=smartpos.db` (`DatabasePathHelper.cs` lines 18-37).
- **Default SQLite Journal Mode**: Default is `DELETE`. In this mode, writing acquires an exclusive file lock on `smartpos.db`, blocking all reads. Conversely, any active read transaction blocks all write operations.
- **Missing Busy Timeout**: When a lock conflict occurs, SQLite returns immediately with `SQLite Error 5: 'database is locked'` because no busy handler or timeout is configured.

### Recommended Fix Strategy
1. **Connection String Parameters**:
   Configure `SqliteConnectionStringBuilder` in `DatabasePathHelper` or `App.xaml.cs`:
   - `Busy Timeout = 30000` (30 seconds waiting period before throwing lock exception).
   - `Cache = Shared` or `Pooling = True`.
2. **Execute Critical PRAGMA Statements on Connection Startup**:
   In `DbInitializer.cs` (lines 16-22) and `AppDbContext` connection initialization:
   ```csharp
   await context.Database.ExecuteSqlRawAsync("PRAGMA journal_mode=WAL;");
   await context.Database.ExecuteSqlRawAsync("PRAGMA busy_timeout=30000;");
   await context.Database.ExecuteSqlRawAsync("PRAGMA synchronous=NORMAL;");
   await context.Database.ExecuteSqlRawAsync("PRAGMA temp_store=MEMORY;");
   await context.Database.ExecuteSqlRawAsync("PRAGMA foreign_keys=ON;");
   ```
3. **Benefits of WAL Mode (`PRAGMA journal_mode=WAL;`)**:
   - Write-Ahead Logging allows **concurrent readers while a write operation is in progress**.
   - Readers read from `smartpos.db`, while writers append to `smartpos.db-wal`.
   - `PRAGMA synchronous=NORMAL;` drastically reduces disk I/O wait times during transactions while remaining crash-safe in WAL mode.

---

## 4. Memory Leak Analysis: LiveCharts, Video Camera & Barcode Threads

### 4.1 LiveCharts & SkiaSharp Unmanaged Memory Leaks
- **Location**: `src/SmartPOS.Application/ViewModels/ReportsViewModel.cs` (lines 750-840).
- **Observation**:
  ```csharp
  SalesChartSeries = new ISeries[]
  {
      new ColumnSeries<double>
      {
          Fill = new SolidColorPaint(SKColor.Parse("#06B6D4")), // Native Skia paint!
          DataLabelsPaint = new SolidColorPaint(SKColors.White)
      }
  };
  ```
  On every date filter change or report refresh, new `SolidColorPaint` and `LinearGradientPaint` instances are allocated. SkiaSharp paints wrap unmanaged C++ Skia objects (`SKPaint`). They do not get collected by standard GC unless explicitly disposed or swept via SkiaSharp finalizer.
- **Page Reload Leak**:
  In `src/SmartPOS.WPF/Views/ReportsPage.xaml.cs` (lines 23-24):
  ```csharp
  _viewModel = host.Services.GetRequiredService<ReportsViewModel>();
  DataContext = _viewModel;
  ```
  Every time `ReportsPage` is loaded, a NEW `ReportsViewModel` is resolved and set to `DataContext`. The previous `ReportsViewModel` and its LiveCharts series objects remain attached to the WPF `CartesianChart` / `PieChart` controls, leaking memory indefinitely.
- **Fix**:
  - Cache static/reusable `SKColor` and `SolidColorPaint` instances in `ArabicChartText` or a static chart theme helper.
  - Implement `IDisposable` on `ReportsViewModel` to clean up series arrays and paints.
  - Keep `ReportsPage` `DataContext` bound to a single ViewModel instance instead of re-instantiating on `Page_Loaded`.

### 4.2 Video Camera Preview Handles (OpenCvSharp)
- **Location**: `src/SmartPOS.Application/ViewModels/WmsQrBridgeViewModel.cs` (lines 140-162, 417-463).
- **Observation**:
  - OpenCV camera polling loop:
    ```csharp
    var frame = mat.ToWriteableBitmap(); // Allocates new WriteableBitmap object every 80ms!
    frame.Freeze();
    Dispatcher.Invoke(() => WmsCameraFrame = frame);
    ```
    This creates 12-15 `WriteableBitmap` objects per second on the UI thread without buffer reuse, causing heavy GC pressure and memory spikes.
  - Unmanaged Handle Leak: `_wmsCamera = new VideoCapture(0);` is allocated. If the user navigates away from `SettingsPage` while scanning is active, `StopWmsQrScan()` is never called, leaving the native `VideoCapture` handle open in a background thread indefinitely.
- **Fix**:
  - Implement `IDisposable` on `SettingsViewModel` / `WmsQrBridgeViewModel` to automatically call `StopWmsQrScan()`.
  - In `SettingsPage.xaml.cs` `Page_Unloaded`, explicitly stop camera scanning.
  - Reuse a single `WriteableBitmap` buffer or dispose previous frame bitmap before assigning a new one.

### 4.3 Barcode Scanner Background Threads & COM Ports
- **Location**: `src/SmartPOS.Infrastructure/Services/BarcodeService.cs` (lines 11-134).
- **Observation**:
  - `SerialPort.DataReceived` events run on ThreadPool threads.
  - `BarcodeService` lacks an `IDisposable` implementation. Re-configuring settings leaves previous `SerialPort` handles un-disposed.
- **Navigation Disconnect Bug in POSPage**:
  In `src/SmartPOS.WPF/Views/POSPage.xaml.cs` (lines 29-35):
  ```csharp
  private void Page_Unloaded(object sender, RoutedEventArgs e)
  {
      if (_viewModel is IDisposable disposable)
          disposable.Dispose();
  }
  ```
  When user navigates away from POS to any other tab, `Page_Unloaded` disposes `MainPOSViewModel`, calling `WeakReferenceMessenger.Default.UnregisterAll(this)`.
  When user returns to POS, `POSPage` is fetched from `MainWindow._pageCache` (constructor does NOT run). `MainPOSViewModel` is **never re-registered** with `WeakReferenceMessenger`. Barcode scanner events (`BarcodeScannedMessage`) are silently ignored for the rest of the session.
- **Fix**:
  - Move messenger registration to `OnNavigatedTo` / `Page_Loaded` or avoid unregistering `MainPOSViewModel` on page unload if cached.
  - Make `BarcodeService` implement `IDisposable` to cleanly close COM serial ports.

---

## 5. UI Thread Deadlocks & Navigation Lifecycle Analysis

### Threading & Synchronous Lock Tracing
1. **Messenger Dispatch**:
   `BarcodeService` invokes `BarcodeScanned` on SerialPort background thread -> sent via `WeakReferenceMessenger` -> received in `MainPOSViewModel.Receive()`:
   ```csharp
   System.Windows.Application.Current.Dispatcher.InvokeAsync(async () =>
   {
       await AddProductByBarcode(message.Value);
   });
   ```
   `Dispatcher.InvokeAsync` is used correctly here, but inside `AddProductByBarcode`, database operations must use short-lived DbContexts to prevent concurrent context execution on the UI thread.
2. **Page Loaded Refresh Calls**:
   `DashboardPage`, `ReportsPage`, `InvoicesPage`, etc., invoke `ExecuteAsync` on commands inside `Page_Loaded`. When returning from sub-dialogs or switching tabs, these concurrent background data fetches hit the single shared `AppDbContext`, triggering EF Core concurrency exceptions (`A second operation was started on this context instance before a previous operation completed`).

---

## 6. Background GC Compaction & ChangeTracker Cleanup Architecture

To ensure 24+ hour continuous operation without memory degradation:
1. **Background GC Compaction Service**:
   Implement `GcCompactionHostedService : IHostedService` (or `System.Threading.Timer`) in `SmartPOS.Infrastructure/Services`:
   - Runs every 15-30 minutes automatically.
   - Triggers Large Object Heap (LOH) compaction and generational collection:
     ```csharp
     GCSettings.LargeObjectHeapCompactionMode = GCLargeObjectHeapCompactionMode.CompactOnce;
     GC.Collect(2, GCCollectionMode.Optimized, blocking: false, compacting: true);
     ```
2. **Automated ChangeTracker Cleanup**:
   By switching to `IDbContextFactory<AppDbContext>` for short-lived DbContext operations, contexts are automatically disposed when operations complete, eliminating `ChangeTracker` accumulation at the architectural level.

---

## 7. Affected Files & Classes Reference Matrix

| Project | File Path | Class / Method | Issue Description |
| :--- | :--- | :--- | :--- |
| `SmartPOS.WPF` | `App.xaml.cs` | `BuildHost()` | `AppDbContext` registered as `Transient` without factory; SQLite connection lacks WAL and BusyTimeout. |
| `SmartPOS.Infrastructure` | `Data/DatabasePathHelper.cs` | `GetDatabasePath()` | Returns raw path without `Busy Timeout=30000` or connection string parameters. |
| `SmartPOS.Infrastructure` | `Data/DbInitializer.cs` | `InitializeAsync()` | Missing initial execution of `PRAGMA journal_mode=WAL;` and `PRAGMA busy_timeout=30000;`. |
| `SmartPOS.Infrastructure` | `Repositories/UnitOfWork.cs` | `Dispose()` | Context not disposed by UnitOfWork, leading to long-lived tracking when scopes aren't managed. |
| `SmartPOS.Infrastructure` | `Services/BarcodeService.cs` | `Configure()`, `CloseSerial()` | Lacks `IDisposable`; serial port handles not cleanly freed. |
| `SmartPOS.Application` | `ViewModels/DashboardViewModel.cs` | `LoadDashboardDataCoreAsync()` | Queries lack `.AsNoTracking()`; long-lived context accumulates tracked entities. |
| `SmartPOS.Application` | `ViewModels/MainPOSViewModel.cs` | Constructor & `Receive()` | Context held long-lived; unregisters from Messenger on page unload breaking barcode scan. |
| `SmartPOS.Application` | `ViewModels/ReportsViewModel.cs` | `LoadChartsAsync()` | SkiaSharp `SolidColorPaint` allocated every refresh without disposal; series leak. |
| `SmartPOS.Application` | `ViewModels/WmsQrBridgeViewModel.cs` | `StartWmsQrScan()`, `StopWmsQrScan()` | `VideoCapture(0)` handle leak on page leave; `WriteableBitmap` allocated every 80ms. |
| `SmartPOS.WPF` | `Views/MainWindow.xaml.cs` | `GetOrCreatePage()` | Page cache keeps ViewModels and long-lived contexts alive for app lifetime. |
| `SmartPOS.WPF` | `Views/POSPage.xaml.cs` | `Page_Unloaded()` | Disposes `MainPOSViewModel` permanently on tab switch, breaking barcode listener. |
| `SmartPOS.WPF` | `Views/ReportsPage.xaml.cs` | `Page_Loaded()` | Re-creates `ReportsViewModel` on every load, leaking old ViewModel & LiveCharts series. |
| `SmartPOS.WPF` | `Views/SettingsPage.xaml.cs` | `Page_Unloaded()` | Lacks explicit cleanup call for camera scan stop. |

---

## 8. Recommended Fix Action Plan for Implementer

### Step 1: SQLite Connection & WAL Mode Setup
1. Update `DatabasePathHelper` or `App.xaml.cs` to use `SqliteConnectionStringBuilder`:
   - `DataSource = dbPath`
   - `DefaultTimeout = 30` (30000ms BusyTimeout)
   - `Mode = ReadWriteCreate`
   - `Pooling = true`
2. In `DbInitializer.InitializeAsync`, execute PRAGMA SQL commands immediately after `MigrateAsync()`:
   ```csharp
   await context.Database.ExecuteSqlRawAsync("PRAGMA journal_mode=WAL;");
   await context.Database.ExecuteSqlRawAsync("PRAGMA busy_timeout=30000;");
   await context.Database.ExecuteSqlRawAsync("PRAGMA synchronous=NORMAL;");
   await context.Database.ExecuteSqlRawAsync("PRAGMA temp_store=MEMORY;");
   ```

### Step 2: DbContext Factory Scoping & AsNoTracking Enforcement
1. In `App.xaml.cs`, register `services.AddDbContextFactory<AppDbContext>(...)`.
2. Refactor ViewModels (`DashboardViewModel`, `ProductsViewModel`, `ReportsViewModel`, `InvoicesViewModel`, etc.) to accept `IDbContextFactory<AppDbContext>` instead of long-lived `AppDbContext`.
3. In ViewModel query methods, use `using var context = _dbContextFactory.CreateDbContext();` and append `.AsNoTracking()` to all read queries.

### Step 3: Barcode & Page Lifecycle Fix
1. In `POSPage.xaml.cs`, remove `_viewModel.Dispose()` from `Page_Unloaded`, or re-register `MainPOSViewModel` with `WeakReferenceMessenger` in `Page_Loaded`.
2. In `BarcodeService.cs`, implement `IDisposable` to close `SerialPort`.

### Step 4: LiveCharts & Camera Resource Optimization
1. In `ReportsViewModel.cs`, reuse static/cached `SolidColorPaint` instances and implement cleanup of series arrays prior to re-assigning.
2. In `ReportsPage.xaml.cs`, retain a single `ReportsViewModel` instance instead of re-instantiating on every `Page_Loaded`.
3. In `WmsQrBridgeViewModel.cs`, call `StopWmsQrScan()` in `Page_Unloaded` of `SettingsPage.xaml.cs` and implement `IDisposable`.

### Step 5: Automated Background GC Compaction Service
1. Add `GcCompactionHostedService` to `SmartPOS.Infrastructure` (or `SmartPOS.WPF/Services`).
2. Register as `IHostedService` in `App.xaml.cs`:
   `services.AddHostedService<GcCompactionHostedService>();`
3. Timer triggers `GCSettings.LargeObjectHeapCompactionMode = GCLargeObjectHeapCompactionMode.CompactOnce; GC.Collect(2, GCCollectionMode.Optimized, blocking: false, compacting: true);` every 15-30 minutes.

---
