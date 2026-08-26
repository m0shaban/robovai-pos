## Dispatch for Worker M3-1

**Working Directory**: `f:\Raw\kasher\kasher\.agents\worker_m3_1`
**Role**: Implementation worker (`teamwork_preview_worker`)

### Required Context Files to Read:
1. `f:\Raw\kasher\kasher\.agents\ORIGINAL_REQUEST.md`
2. `f:\Raw\kasher\kasher\PROJECT.md`
3. `f:\Raw\kasher\kasher\.agents\explorer_m3_1\analysis.md`
4. `f:\Raw\kasher\kasher\.agents\explorer_m3_2\analysis.md`
5. `f:\Raw\kasher\kasher\.agents\explorer_m3_3\analysis.md`

### Mandatory Integrity Warning:
DO NOT CHEAT. All implementations must be genuine. DO NOT hardcode test results, create dummy/facade implementations, or circumvent the intended task. A teamwork_preview_auditor will independently verify your work. Integrity violations WILL be detected and your work WILL be rejected.

### Tasks to Implement:
1. **SQLite Connection & PRAGMA Initialization**:
   - `DatabasePathHelper.cs`: Add `GetConnectionString()` using `SqliteConnectionStringBuilder` (`Busy Timeout = 30000`, `Mode = ReadWriteCreate`, `Pooling = true`).
   - `App.xaml.cs`: Use `DatabasePathHelper.GetConnectionString()` for `initContext` and replace `AddDbContext<AppDbContext>` with `services.AddDbContextFactory<AppDbContext>(...)`.
   - `DbInitializer.cs`: Execute PRAGMA statements (`journal_mode=WAL;`, `busy_timeout=30000;`, `synchronous=NORMAL;`, `temp_store=MEMORY;`, `foreign_keys=ON;`).

2. **EF Core `IDbContextFactory` Refactoring & `.AsNoTracking()`**:
   - Refactor ViewModels (`DashboardViewModel`, `ProductsViewModel`, `ReportsViewModel`, `InvoicesViewModel`, `ExpensesViewModel`, `MainPOSViewModel`, `CustomersViewModel`, `CategoriesViewModel`, `ShiftManagementViewModel`, `LoyaltyViewModel`, `ReturnsViewModel`, `SuppliersViewModel`, `PurchaseOrdersViewModel`, `RentalsViewModel`, `UsersViewModel`, `AuditLogViewModel`, `WmsQrBridgeViewModel`, `AIPredictionService`, `AuthorizationService`, `SettingsService`, `Repository<T>`, `CustomerInvoicesWindow`) to accept `IDbContextFactory<AppDbContext>`.
   - Enforce `.AsNoTracking()` on all read queries.
   - Use short-lived `using var context = _contextFactory.CreateDbContext();` for all DB operations.
   - Add `TestDbContextFactory` in `SmartPOS.UnitTests` so all unit tests pass with `IDbContextFactory`.

3. **Unmanaged Paint & OpenCV Camera Handle Disposal**:
   - `ReportsViewModel.cs`: Reuse static/instance `SolidColorPaint` / `LinearGradientPaint` fields; implement `IDisposable`.
   - `ReportsPage.xaml.cs`: Only resolve `ReportsViewModel` in `Page_Loaded` if `_viewModel == null`.
   - `WmsQrBridgeViewModel.cs` / `SettingsViewModel.cs` / `SettingsPage.xaml.cs`: Implement `IDisposable`, call `StopWmsQrScan()`, release OpenCV `VideoCapture(0)` handle, null out WPF bitmap frame references on unload.

4. **Barcode Scanner Messenger Lifecycle & GC Compaction Service**:
   - `MainPOSViewModel.cs` & `POSPage.xaml.cs`: Add `RegisterMessenger()` / `UnregisterMessenger()`; call in `Page_Loaded` and `Page_Unloaded`.
   - `BarcodeService.cs` & `IBarcodeService.cs`: Implement `IDisposable` to release COM serial ports.
   - `GcCompactionService.cs`: Create `IHostedService` in `SmartPOS.Infrastructure/Services/GcCompactionService.cs` that triggers LOH compaction every 15 minutes (`GCSettings.LargeObjectHeapCompactionMode = GCLargeObjectHeapCompactionMode.CompactOnce; GC.Collect(...)`). Register `services.AddHostedService<GcCompactionService>()` in `App.xaml.cs`.

### Verification Commands to Run:
- `dotnet build src/SmartPOS.sln`
- `dotnet test src/SmartPOS.UnitTests`

### Output Requirement:
Write `f:\Raw\kasher\kasher\.agents\worker_m3_1\changes.md` and `f:\Raw\kasher\kasher\.agents\worker_m3_1\handoff.md` detailing build/test outputs and implemented changes. Send completion message to parent.
