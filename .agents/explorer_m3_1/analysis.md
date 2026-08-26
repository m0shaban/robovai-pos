# Technical Implementation Strategy Report: EF Core IDbContextFactory & .AsNoTracking() Refactoring

**Project**: SmartPOS / RobovAI PRO POS & WMS Ecosystem  
**Module Target**: `src/SmartPOS.WPF`, `src/SmartPOS.Infrastructure`, `src/SmartPOS.Application`, `src/SmartPOS.UnitTests`  
**Milestone**: M3-1 (EF Core DbContext Lifetime & Query Optimization)  
**Author**: Explorer M3-1 (`teamwork_preview_explorer`)  
**Date**: 2026-08-08  

---

## 1. Executive Summary

This report establishes the exact, step-by-step technical implementation strategy for refactoring EF Core data access across the WPF Desktop ecosystem (Milestone M3-1). 

### Problem Core
In the legacy architecture:
1. `AppDbContext` is registered as a `Transient` dependency in `App.xaml.cs`.
2. WPF `Page` controls are cached indefinitely in `MainWindow._pageCache`. ViewModels resolved by these pages retain a single `AppDbContext` instance throughout 24+ hours of application execution.
3. Read queries executed across ViewModels (Dashboard, Reports, Products, Invoices, Expenses, MainPOS, etc.) omit `.AsNoTracking()`.
4. As a result, EF Core's `ChangeTracker` continuously accumulates thousands of entity instances, consuming tens of megabytes of unmanaged/managed memory and progressively degrading query execution speed. Furthermore, long-lived contexts shared across background async tasks and UI events trigger EF Core concurrency exceptions and SQLite database locks.

### Solution Architecture
1. **Connection String Hardening**: Implement `SqliteConnectionStringBuilder` with `Busy Timeout = 30` (30,000 ms) and `Pooling = true` in `DatabasePathHelper.cs`.
2. **Factory Registration**: Replace direct `AddDbContext<AppDbContext>` registration in `App.xaml.cs` with `services.AddDbContextFactory<AppDbContext>(...)`.
3. **Short-Lived DbContext Pattern**: Refactor ViewModels and Services from holding long-lived `AppDbContext` references to accepting `IDbContextFactory<AppDbContext>`. Each data operation (reads, writes, updates, deletes) creates a short-lived context via `using var context = _contextFactory.CreateDbContext()`.
4. **Read Query Tracking Elimination**: Enforce `.AsNoTracking()` on 100% of read-only listing, counting, and aggregation queries.
5. **Unit Test Compatibility**: Provide a generic `TestDbContextFactory` adapter in `SmartPOS.UnitTests` so that existing unit tests compile and run seamlessly with EF Core InMemory/SQLite test contexts.

---

## 2. Database Connection String & DI Registration Setup

### 2.1 Connection String Enhancement (`DatabasePathHelper.cs`)
**Target File**: `src/SmartPOS.Infrastructure/Data/DatabasePathHelper.cs`

Add `GetConnectionString()` to configure SQLite connection timeouts and pooling centrally:

```csharp
using Microsoft.Data.Sqlite;

namespace SmartPOS.Infrastructure.Data;

public static class DatabasePathHelper
{
    private const string DatabaseFileName = "smartpos.db";
    private const string AppFolder = "RoboVAI\\SmartPOS";

    public static string GetDatabasePath()
    {
        var envPath = Environment.GetEnvironmentVariable("SMARTPOS_DB_PATH");
        if (!string.IsNullOrWhiteSpace(envPath))
        {
            var dir = Path.GetDirectoryName(envPath);
            if (!string.IsNullOrEmpty(dir))
                Directory.CreateDirectory(dir);
            return envPath;
        }

        var appDataDir = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            AppFolder);

        Directory.CreateDirectory(appDataDir);
        return Path.Combine(appDataDir, DatabaseFileName);
    }

    public static string GetDesignTimeDatabasePath() => GetDatabasePath();

    /// <summary>
    /// Returns the hardened connection string with Busy Timeout (30s) and connection pooling.
    /// </summary>
    public static string GetConnectionString()
    {
        var dbPath = GetDatabasePath();
        return new SqliteConnectionStringBuilder
        {
            DataSource = dbPath,
            Mode = SqliteOpenMode.ReadWriteCreate,
            DefaultTimeout = 30, // 30 seconds Busy Timeout to prevent database locks
            Pooling = true
        }.ToString();
    }
}
```

### 2.2 Dependency Injection Registration (`App.xaml.cs`)
**Target File**: `src/SmartPOS.WPF/App.xaml.cs` (lines 136-138, lines 214-220)

Replace line 214-220 in `BuildHost()`:

```csharp
// BEFORE:
services.AddDbContext<AppDbContext>(options =>
{
    var dbPath = DatabasePathHelper.GetDatabasePath();
    options.UseSqlite($"Data Source={dbPath}", b => b.MigrationsAssembly("SmartPOS.Infrastructure"));
}, ServiceLifetime.Transient);

// AFTER:
services.AddDbContextFactory<AppDbContext>(options =>
{
    var connectionString = DatabasePathHelper.GetConnectionString();
    options.UseSqlite(connectionString, b => b.MigrationsAssembly("SmartPOS.Infrastructure"));
});
```

Update Database Initialization in `OnStartup` (lines 136-138):

```csharp
// BEFORE:
var optionsBuilder = new DbContextOptionsBuilder<AppDbContext>();
optionsBuilder.UseSqlite($"Data Source={dbPath}", b => b.MigrationsAssembly("SmartPOS.Infrastructure"));
using var initContext = new AppDbContext(optionsBuilder.Options);

// AFTER:
var optionsBuilder = new DbContextOptionsBuilder<AppDbContext>();
optionsBuilder.UseSqlite(DatabasePathHelper.GetConnectionString(), b => b.MigrationsAssembly("SmartPOS.Infrastructure"));
using var initContext = new AppDbContext(optionsBuilder.Options);
```

---

## 3. ViewModel & Service Refactoring Strategy

### 3.1 Refactoring Blueprint & Patterns

#### Pattern A: Read-Only Query (Listing/Aggregation)
```csharp
// Field declaration
private readonly IDbContextFactory<AppDbContext> _contextFactory;

// Constructor injection
public MyViewModel(IDbContextFactory<AppDbContext> contextFactory)
{
    _contextFactory = contextFactory;
}

// Method execution
private async Task LoadDataAsync()
{
    using var context = _contextFactory.CreateDbContext();
    var items = await context.Items
        .AsNoTracking() // Eliminates ChangeTracker overhead
        .Where(x => !x.IsDeleted)
        .ToListAsync();

    MyItems.SyncWith(items);
}
```

#### Pattern B: Mutation / Add / Update / Delete
```csharp
private async Task SaveItemAsync(MyItem item)
{
    using var context = _contextFactory.CreateDbContext();
    
    if (item.Id > 0)
    {
        var entity = await context.Items.FindAsync(item.Id);
        if (entity != null)
        {
            entity.Name = item.Name;
            entity.UpdatedAt = DateTime.Now;
            await context.SaveChangesAsync();
        }
    }
    else
    {
        context.Items.Add(item);
        await context.SaveChangesAsync();
    }
}
```

---

## 4. Comprehensive File Refactoring Reference Matrix

The following table details every file, class signature change, line range, and required `.AsNoTracking()` placement:

| # | Target File Path | Class Name | Constructor Modification | Target Query Methods & `.AsNoTracking()` Placement |
|---|------------------|------------|--------------------------|----------------------------------------------------|
| 1 | `src/SmartPOS.Application/ViewModels/DashboardViewModel.cs` | `DashboardViewModel` | Change `AppDbContext context` to `IDbContextFactory<AppDbContext> contextFactory` | `LoadDashboardDataCoreAsync()`: `context.Sales.AsNoTracking()`, `context.SaleDetails.AsNoTracking()`, `context.Products.AsNoTracking()`, `context.Customers.AsNoTracking()`, `context.Shifts.AsNoTracking()`. |
| 2 | `src/SmartPOS.Application/Services/AIPredictionService.cs` | `AIPredictionService` | Change `AppDbContext context` to `IDbContextFactory<AppDbContext> contextFactory` | `GetInventoryPredictionsAsync()`: `using var context = _contextFactory.CreateDbContext();` and `context.SaleDetails.AsNoTracking()`. |
| 3 | `src/SmartPOS.Application/ViewModels/ProductsViewModel.cs` | `ProductsViewModel` | Change `AppDbContext context` to `IDbContextFactory<AppDbContext> contextFactory` | `LoadProducts()`: `context.Products.AsNoTracking()`. `LoadCategories()`: `context.Categories.AsNoTracking()`. Short-lived contexts in `SaveProduct`, `DeleteProduct`, `ImportExcelAsync`. |
| 4 | `src/SmartPOS.Application/ViewModels/ReportsViewModel.cs` | `ReportsViewModel` | Change `AppDbContext context` to `IDbContextFactory<AppDbContext> contextFactory` | `LoadInventoryValuationAsync`, `LoadSalesStatisticsAsync`, `LoadTopProductsAsync`, `LoadRecentExpensesAsync`, `FilterReportsCoreAsync`, `LoadChartsAsync`: wrap in `using var context = _contextFactory.CreateDbContext();` with `.AsNoTracking()`. |
| 5 | `src/SmartPOS.Application/ViewModels/InvoicesViewModel.cs` | `InvoicesViewModel` | Change `AppDbContext context` to `IDbContextFactory<AppDbContext> contextFactory` | `LoadSalesCoreAsync`, `FilterAsync`, `ViewDetailsAsync`: `context.Sales.AsNoTracking()`, `context.SaleDetails.AsNoTracking()`. `CancelSaleAsync`: short-lived mutation context. |
| 6 | `src/SmartPOS.Application/ViewModels/ExpensesViewModel.cs` | `ExpensesViewModel` | Change `AppDbContext context` to `IDbContextFactory<AppDbContext> contextFactory` | `LoadExpensesCoreAsync`, `FilterExpensesAsync`: `context.Expenses.AsNoTracking()`. `SaveExpenseAsync`, `DeleteExpenseAsync`: short-lived mutation context. |
| 7 | `src/SmartPOS.Application/ViewModels/MainPOSViewModel.cs` | `MainPOSViewModel` | Change `AppDbContext context` to `IDbContextFactory<AppDbContext> contextFactory` | `LoadProductsAsync`, `LoadCategoriesAsync`, `LoadCustomersAsync`, `SearchProductsAsync`: `.AsNoTracking()`. `CheckoutAsync`: short-lived context for sale write & stock update. |
| 8 | `src/SmartPOS.Application/ViewModels/CustomersViewModel.cs` | `CustomersViewModel` | Change `AppDbContext context` to `IDbContextFactory<AppDbContext> contextFactory` | `LoadCustomersCoreAsync`, `LoadCustomerHistoryAsync`: `.AsNoTracking()`. `SaveCustomerAsync`, `DeleteCustomerAsync`: short-lived context. |
| 9 | `src/SmartPOS.Application/ViewModels/CategoriesViewModel.cs` | `CategoriesViewModel` | Change `AppDbContext context` to `IDbContextFactory<AppDbContext> contextFactory` | `LoadCategoriesCoreAsync`: `.AsNoTracking()`. `SaveCategoryAsync`, `DeleteCategoryAsync`: short-lived context. |
| 10 | `src/SmartPOS.Application/ViewModels/ShiftManagementViewModel.cs` | `ShiftManagementViewModel` | Change `AppDbContext context` to `IDbContextFactory<AppDbContext> contextFactory` | `LoadShiftDataAsync`, `CalculateZReportAsync`: `.AsNoTracking()`. `OpenShiftAsync`, `CloseShiftAsync`: short-lived mutation context. |
| 11 | `src/SmartPOS.Application/ViewModels/LoyaltyViewModel.cs` | `LoyaltyViewModel` | Change `AppDbContext context` to `IDbContextFactory<AppDbContext> contextFactory` | `LoadLoyaltyDataAsync`: `.AsNoTracking()`. `AddPointsAsync`, `RedeemPointsAsync`: short-lived mutation context. |
| 12 | `src/SmartPOS.Application/ViewModels/ReturnsViewModel.cs` | `ReturnsViewModel` | Change `AppDbContext context` to `IDbContextFactory<AppDbContext> contextFactory` | `LookupInvoiceAsync`: `.AsNoTracking()`. `ProcessReturnAsync`: short-lived return transaction context. |
| 13 | `src/SmartPOS.Application/ViewModels/SuppliersViewModel.cs` | `SuppliersViewModel` | Change `AppDbContext context` to `IDbContextFactory<AppDbContext> contextFactory` | `LoadSuppliersCoreAsync`: `.AsNoTracking()`. `SaveSupplierAsync`, `DeleteSupplierAsync`: short-lived context. |
| 14 | `src/SmartPOS.Application/ViewModels/PurchaseOrdersViewModel.cs` | `PurchaseOrdersViewModel` | Change `AppDbContext context` to `IDbContextFactory<AppDbContext> contextFactory` | `LoadOrdersCoreAsync`, `LoadOrderDetailsAsync`: `.AsNoTracking()`. `CreateOrderAsync`, `ReceiveOrderAsync`: short-lived context. |
| 15 | `src/SmartPOS.Application/ViewModels/RentalsViewModel.cs` | `RentalsViewModel` | Change `AppDbContext context` to `IDbContextFactory<AppDbContext> contextFactory` | `LoadDevicesAsync`, `LoadActiveSessionsAsync`: `.AsNoTracking()`. `StartSessionAsync`, `EndSessionAsync`: short-lived context. |
| 16 | `src/SmartPOS.Application/ViewModels/UsersViewModel.cs` | `UsersViewModel` | Change `AppDbContext context` to `IDbContextFactory<AppDbContext> contextFactory` | `LoadUsersCoreAsync`: `.AsNoTracking()`. `SaveUserAsync`, `ToggleUserStatusAsync`: short-lived context. |
| 17 | `src/SmartPOS.Application/ViewModels/AuditLogViewModel.cs` | `AuditLogViewModel` | Change `AppDbContext context` to `IDbContextFactory<AppDbContext> contextFactory` | `LoadAuditLogsCoreAsync`, `FilterLogsAsync`: `context.AuditLogs.AsNoTracking()`. |
| 18 | `src/SmartPOS.Application/ViewModels/WmsQrBridgeViewModel.cs` | `WmsQrBridgeViewModel` | Change `AppDbContext? _dbCtx` to `IDbContextFactory<AppDbContext>` | `CheckWmsSyncStateAsync`: short-lived context with `.AsNoTracking()`. |
| 19 | `src/SmartPOS.WPF/Services/AuthorizationService.cs` | `AuthorizationService` | Change `AppDbContext dbContext` to `IDbContextFactory<AppDbContext> dbContextFactory` | `RequestAdminOverrideAsync`: `context.Users.AsNoTracking()`. `LogAuditAsync`: short-lived mutation context. |
| 20 | `src/SmartPOS.Infrastructure/Services/SettingsService.cs` | `SettingsService` | Replace `IServiceScopeFactory` with `IDbContextFactory<AppDbContext>` | `LoadSettingsAsync`: `context.AppSettings.AsNoTracking()`. `SaveSettingAsync`: short-lived context. |
| 21 | `src/SmartPOS.Infrastructure/Repositories/Repository.cs` | `Repository<T>` | Replace `AppDbContext _context` with `IDbContextFactory<AppDbContext>` | `GetAllAsync()`, `FindAsync()`: `context.Set<T>().AsNoTracking()`. Write operations: short-lived context. |
| 22 | `src/SmartPOS.WPF/Views/CustomerInvoicesWindow.xaml.cs` | `CustomerInvoicesWindow` | Change constructor parameter to accept `IDbContextFactory<AppDbContext>` | `LoadInvoicesAsync`: `context.Sales.AsNoTracking()`. |

---

## 5. Specific Code Transformations

### 5.1 `DashboardViewModel.cs` Transformation Example
```csharp
// File: src/SmartPOS.Application/ViewModels/DashboardViewModel.cs

public partial class DashboardViewModel : BaseViewModel
{
    private readonly IDbContextFactory<AppDbContext> _contextFactory;
    private readonly User _currentUser;
    private readonly AIPredictionService _aiPredictionService;

    public DashboardViewModel(IDbContextFactory<AppDbContext> contextFactory, User currentUser)
    {
        _contextFactory = contextFactory;
        _currentUser = currentUser;
        _aiPredictionService = new AIPredictionService(contextFactory);

        _ = InitializeAsync();
    }

    private async Task LoadDashboardDataCoreAsync()
    {
        var today = DateTime.Today;
        var startOfMonth = new DateTime(today.Year, today.Month, 1);

        using var context = _contextFactory.CreateDbContext();

        // 1. Today's Transactions and Sales
        var todaySalesList = await context.Sales
            .AsNoTracking()
            .Where(s => s.SaleDate.Date == today
                     && s.Status == SaleStatus.Completed
                     && !s.IsDeleted
                     && !s.InvoiceNumber.StartsWith(SaleRecordKinds.DebtPaymentPrefix))
            .ToListAsync();

        TodayTransactions = todaySalesList.Count;
        TodaySales = todaySalesList.Sum(s => s.TotalAmount);

        // 2. Today's Profit
        var todayDetailsList = await context.SaleDetails
            .AsNoTracking()
            .Where(sd => sd.Sale.SaleDate.Date == today && sd.Sale.Status == SaleStatus.Completed && !sd.Sale.IsDeleted)
            .ToListAsync();

        TodayProfit = todayDetailsList.Sum(sd => (sd.UnitPrice - sd.UnitCost) * sd.Quantity - sd.DiscountAmount);

        // 3. Month's Sales
        var monthSalesList = await context.Sales
            .AsNoTracking()
            .Where(s => s.SaleDate >= startOfMonth
                     && s.Status == SaleStatus.Completed
                     && !s.IsDeleted
                     && !s.InvoiceNumber.StartsWith(SaleRecordKinds.DebtPaymentPrefix))
            .ToListAsync();

        MonthSales = monthSalesList.Sum(s => s.TotalAmount);

        // 4. Recent Sales
        var recent = await context.Sales
            .AsNoTracking()
            .Include(s => s.User)
            .Where(s => !s.IsDeleted && !s.InvoiceNumber.StartsWith(SaleRecordKinds.DebtPaymentPrefix))
            .OrderByDescending(s => s.SaleDate)
            .Take(10)
            .ToListAsync();

        RecentSales.SyncWith(recent);

        // 5. Low Stock Products
        var lowStock = await context.Products
            .AsNoTracking()
            .Include(p => p.Category)
            .Where(p => p.Stock <= p.MinStockLevel && p.IsActive && !p.IsDeleted)
            .OrderBy(p => p.Stock)
            .Take(10)
            .ToListAsync();

        LowStockProducts.SyncWith(lowStock);
        LowStockCount = lowStock.Count;

        // 5.5 AI Stock Predictions
        var predictions = await _aiPredictionService.GetInventoryPredictionsAsync();
        AiStockPredictions.SyncWith(predictions);

        // Total Products & Customers
        TotalProducts = await context.Products.AsNoTracking().CountAsync(p => !p.IsDeleted);
        TotalCustomers = await context.Customers.AsNoTracking().CountAsync(c => !c.IsDeleted);

        // 6. Current Shift Sales
        var currentShift = await context.Shifts
            .AsNoTracking()
            .FirstOrDefaultAsync(s => s.Status == ShiftStatus.Open && s.UserId == _currentUser.Id);

        if (currentShift != null)
        {
            var shiftSalesList = await context.Sales
                .AsNoTracking()
                .Where(s => s.ShiftId == currentShift.Id
                            && s.Status == SaleStatus.Completed
                            && !s.IsDeleted
                            && !s.InvoiceNumber.StartsWith(SaleRecordKinds.DebtPaymentPrefix))
                .ToListAsync();

            CurrentShiftSales = shiftSalesList.Sum(s => s.TotalAmount);
            HasActiveShift = true;
        }
        else
        {
            CurrentShiftSales = 0;
            HasActiveShift = false;
        }
    }
}
```

---

## 6. Unit Testing Strategy & Test Factory Adapter

To prevent breaking the unit test suite (`SmartPOS.UnitTests`), create a generic test adapter class in the test project:

**File**: `src/SmartPOS.UnitTests/Infrastructure/TestDbContextFactory.cs`

```csharp
using Microsoft.EntityFrameworkCore;
using SmartPOS.Infrastructure.Data;

namespace SmartPOS.UnitTests.Infrastructure
{
    public class TestDbContextFactory : IDbContextFactory<AppDbContext>
    {
        private readonly DbContextOptions<AppDbContext> _options;

        public TestDbContextFactory(DbContextOptions<AppDbContext> options)
        {
            _options = options;
        }

        public AppDbContext CreateDbContext()
        {
            return new AppDbContext(_options);
        }
    }
}
```

### Test Updates Example (`DashboardViewModelTests.cs`)
```csharp
// BEFORE:
using var context = GetInMemoryDbContext();
var viewModel = new DashboardViewModel(context, user);

// AFTER:
var options = new DbContextOptionsBuilder<AppDbContext>()
    .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
    .Options;
var factory = new TestDbContextFactory(options);

// Seed data
using (var seedContext = factory.CreateDbContext())
{
    seedContext.Shifts.Add(activeShift);
    seedContext.Sales.AddRange(sale1, sale2);
    await seedContext.SaveChangesAsync();
}

var viewModel = new DashboardViewModel(factory, user);
await viewModel.LoadDashboardDataCommand.ExecuteAsync(null);
Assert.Equal(200, viewModel.CurrentShiftSales);
```

---

## 7. Verification Method & Acceptance Criteria

1. **Compilation Verification**:
   Run `dotnet build src/SmartPOS.sln` to ensure zero compilation errors across all projects.

2. **Unit Test Verification**:
   Run `dotnet test src/SmartPOS.UnitTests/SmartPOS.UnitTests.csproj` to confirm 100% test pass rate.

3. **Memory & Concurrency Invalidation Condition**:
   - ChangeTracker entry count must be 0 after any ViewModel data load completes.
   - Rapidly switching between tabs or executing background sync operations must not produce `InvalidOperationException: A second operation was started on this context instance before a previous operation completed`.
   - SQLite concurrent operations must not produce `SQLite Error 5: 'database is locked'`.
