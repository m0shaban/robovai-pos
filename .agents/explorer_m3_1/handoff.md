# Handoff Report: Milestone M3-1 — EF Core IDbContextFactory & .AsNoTracking() Refactoring Strategy

**From**: Explorer M3-1 (`teamwork_preview_explorer`)  
**To**: Parent / Implementer  
**Date**: 2026-08-08  
**Working Directory**: `f:\Raw\kasher\kasher\.agents\explorer_m3_1`  

---

## 1. Observation

1. **Long-Lived `AppDbContext` in ViewModels**:
   - `App.xaml.cs` (lines 214-220): Registers `AppDbContext` as a `Transient` dependency via `services.AddDbContext<AppDbContext>(..., ServiceLifetime.Transient)`.
   - `MainWindow.xaml.cs` (lines 18, 130-164): Caches WPF `Page` instances in `_pageCache` dictionary for the lifetime of the application.
   - `DashboardViewModel.cs` (lines 19, 62), `ProductsViewModel.cs` (lines 22, 54), `ReportsViewModel.cs` (lines 22, 97), `InvoicesViewModel.cs` (lines 23, 42), `ExpensesViewModel.cs` (lines 20, 68), `MainPOSViewModel.cs` (lines 18, 25), `CustomersViewModel.cs`, `CategoriesViewModel.cs`, `ShiftManagementViewModel.cs`, `LoyaltyViewModel.cs`, `ReturnsViewModel.cs`, `SuppliersViewModel.cs`, `PurchaseOrdersViewModel.cs`, `RentalsViewModel.cs`, `UsersViewModel.cs`, `AuditLogViewModel.cs`, `WmsQrBridgeViewModel.cs`, `SettingsViewModel.cs`, `AuthorizationService.cs`, `SettingsService.cs`: All inject `AppDbContext` directly in constructor and store it as a private field.
2. **Missing `.AsNoTracking()`**:
   - `DashboardViewModel.cs` (lines 82-140): `_context.Sales.Where(...)`, `_context.SaleDetails.Where(...)`, `_context.Products.Where(...)`, `_context.Customers.CountAsync()` queries execute without `.AsNoTracking()`.
   - `AIPredictionService.cs` (lines 35-44): `_context.SaleDetails.Where(...)` executes without `.AsNoTracking()`.
   - `ReportsViewModel.cs`, `InvoicesViewModel.cs`, `ExpensesViewModel.cs`, `CustomersViewModel.cs`, `ShiftManagementViewModel.cs`, `LoyaltyViewModel.cs`, `ReturnsViewModel.cs`, `AuditLogViewModel.cs`: Multiple listing and aggregation methods execute queries without `.AsNoTracking()`.
3. **SQLite Connection Configuration**:
   - `DatabasePathHelper.cs` (lines 18-37): Returns plain file path (`smartpos.db`) without setting `Busy Timeout=30000` or connection pooling.
4. **Unit Test Dependencies**:
   - Unit test classes (`DashboardViewModelTests.cs`, `ExpensesViewModelTests.cs`, `MainPOSViewModelTests.cs`, `ProductsViewModelTests.cs`, `ShiftManagementViewModelTests.cs`) pass `AppDbContext` directly to ViewModel constructors.

---

## 2. Logic Chain

1. **Observation**: Cached WPF `Page` instances retain ViewModels in memory indefinitely.
2. **Step**: Injected `AppDbContext` instances live as long as their owning ViewModels.
3. **Step**: When read queries are executed without `.AsNoTracking()`, EF Core tracks every returned entity in `_context.ChangeTracker`.
4. **Step**: Repeated data loading over hours appends thousands of entity references to `ChangeTracker`, causing memory bloat, GC pressure, and query slowdowns.
5. **Step**: Furthermore, sharing a single `AppDbContext` instance across async threads (e.g. WPF UI thread and background scanner/sync tasks) produces thread-concurrency crashes and SQLite file locks.
6. **Inference**: Refactoring DI to register `IDbContextFactory<AppDbContext>` and updating ViewModels/Services to create short-lived contexts via `using var context = _contextFactory.CreateDbContext()` ensures contexts live only for the duration of a single operation.
7. **Inference**: Applying `.AsNoTracking()` to all read queries eliminates entity tracking entirely for read operations, keeping `ChangeTracker` size at 0.
8. **Inference**: Configuring `SqliteConnectionStringBuilder` with `Busy Timeout = 30` (30s) and `Pooling = true` ensures SQLite waits for write locks to clear without throwing immediate `database is locked` exceptions.
9. **Inference**: Implementing `TestDbContextFactory` in `SmartPOS.UnitTests` allows unit tests to instantiate ViewModels with `IDbContextFactory<AppDbContext>` using `UseInMemoryDatabase` or SQLite in-memory without breaking tests.

---

## 3. Caveats

1. **Read-Only Investigation**: No application source code in `src/` was modified during this exploration phase.
2. **ViewModel Navigation Lifecycle**: In `POSPage.xaml.cs`, `Page_Unloaded` calls `_viewModel.Dispose()`, which unregisters `MainPOSViewModel` from `WeakReferenceMessenger`. The implementer must ensure messenger registration is handled cleanly during navigation so barcode scanner messages continue to work after switching tabs.
3. **Unit Test Updates**: Refactoring ViewModel constructors requires updating the constructor calls in all 5 ViewModel unit test classes using `TestDbContextFactory`.

---

## 4. Conclusion

A complete, actionable, 7-step implementation plan has been written to `f:\Raw\kasher\kasher\.agents\explorer_m3_1\analysis.md`. The plan details exact file paths, line ranges, class signatures, and code modifications for:
- Connection string hardening in `DatabasePathHelper.cs`.
- `IDbContextFactory<AppDbContext>` registration in `App.xaml.cs`.
- 18 ViewModels, 4 Services, 1 Repository, and 2 WPF Window/Page code-behinds.
- `.AsNoTracking()` placement across all read queries.
- Unit test adapter pattern (`TestDbContextFactory.cs`) to keep the test suite passing.

---

## 5. Verification Method

1. **Compilation Verification**:
   Execute `dotnet build src/SmartPOS.sln` — must succeed with 0 errors.
2. **Unit Test Verification**:
   Execute `dotnet test src/SmartPOS.UnitTests/SmartPOS.UnitTests.csproj` — all tests must pass.
3. **Memory & Concurrency Invalidation**:
   - Inspect `ChangeTracker.Entries().Count()` after data load operations — must equal 0.
   - Verify concurrent read/write queries do not throw `SQLite Error 5: 'database is locked'`.
