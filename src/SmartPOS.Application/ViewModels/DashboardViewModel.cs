using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.EntityFrameworkCore;
using SmartPOS.Application.Utilities;
using SmartPOS.Application.Extensions;
using SmartPOS.Core.Entities;
using SmartPOS.Infrastructure.Data;
using SmartPOS.Application.Services;
using System.Collections.ObjectModel;
using System.Windows;

namespace SmartPOS.Application.ViewModels;

/// <summary>
/// Dashboard ViewModel - Main analytics and reporting view.
///
/// FIX (v5.1): Uses IDbContextFactory&lt;AppDbContext&gt; instead of a long-lived AppDbContext.
/// Each data-loading operation creates a short-lived, scoped DbContext that is disposed immediately.
/// This eliminates ChangeTracker memory accumulation and prevents SQLite database lock conflicts
/// that caused the application to freeze and require a full system restart after extended use.
/// </summary>
public partial class DashboardViewModel : BaseViewModel
{
    private readonly IDbContextFactory<AppDbContext> _contextFactory;
    private readonly User _currentUser;

    // --- Statistics ---
    [ObservableProperty]
    private decimal _currentShiftSales;

    [ObservableProperty]
    private decimal _todaySales;

    [ObservableProperty]
    private decimal _todayProfit;

    [ObservableProperty]
    private int _todayTransactions;

    [ObservableProperty]
    private decimal _monthSales;

    [ObservableProperty]
    private int _lowStockCount;

    [ObservableProperty]
    private int _totalProducts;

    [ObservableProperty]
    private int _totalCustomers;

    [ObservableProperty]
    private bool _hasActiveShift;

    // --- Collections ---
    [ObservableProperty]
    private ObservableCollection<Sale> _recentSales = new();

    [ObservableProperty]
    private ObservableCollection<Product> _lowStockProducts = new();

    [ObservableProperty]
    private ObservableCollection<AIStockWarning> _aiStockPredictions = new();

    private readonly AIPredictionService _aiPredictionService;

    public DashboardViewModel(IDbContextFactory<AppDbContext> contextFactory, User currentUser)
    {
        _contextFactory = contextFactory;
        _currentUser = currentUser;
        _aiPredictionService = new AIPredictionService(_contextFactory);

        _ = InitializeAsync();
    }

    private async Task InitializeAsync()
    {
        await ExecuteBusyAsync(LoadDashboardDataCoreAsync, "⏳ جاري تحميل إحصائيات لوحة القيادة...", "✅ تم تحديث لوحة القيادة");
    }

    private async Task LoadDashboardDataCoreAsync()
    {
        var today = DateTime.Today;
        var startOfMonth = new DateTime(today.Year, today.Month, 1);

        // FIX: Each query uses a fresh, short-lived DbContext (disposed after the block).
        // This prevents ChangeTracker bloat and avoids holding database file locks
        // that compete with the POS cashier writes during peak usage.

        decimal todaySales = 0, todayProfit = 0, monthSales = 0, currentShiftSales = 0;
        int todayTransactions = 0, lowStockCount = 0, totalProducts = 0, totalCustomers = 0;
        bool hasActiveShift = false;
        List<Sale> recent = new();
        List<Product> lowStock = new();
        List<AIStockWarning> predictions = new();

        // Block 1: Today's transactions and sales
        await using (var context = await _contextFactory.CreateDbContextAsync())
        {
            var todaySalesList = await context.Sales
                .AsNoTracking()
                .Where(s => s.SaleDate.Date == today
                         && s.Status == SaleStatus.Completed
                         && !s.IsDeleted
                         && !s.InvoiceNumber.StartsWith(SaleRecordKinds.DebtPaymentPrefix))
                .ToListAsync();

            todayTransactions = todaySalesList.Count;
            todaySales = todaySalesList.Sum(s => s.TotalAmount);
        }

        // Block 2: Today's profit from sale details
        await using (var context = await _contextFactory.CreateDbContextAsync())
        {
            var todayDetailsList = await context.SaleDetails
                .AsNoTracking()
                .Where(sd => sd.Sale.SaleDate.Date == today && sd.Sale.Status == SaleStatus.Completed && !sd.Sale.IsDeleted)
                .ToListAsync();

            todayProfit = todayDetailsList.Sum(sd => (sd.UnitPrice - sd.UnitCost) * sd.Quantity - sd.DiscountAmount);
        }

        // Block 3: Month's sales total
        await using (var context = await _contextFactory.CreateDbContextAsync())
        {
            var monthSalesList = await context.Sales
                .AsNoTracking()
                .Where(s => s.SaleDate >= startOfMonth
                         && s.Status == SaleStatus.Completed
                         && !s.IsDeleted
                         && !s.InvoiceNumber.StartsWith(SaleRecordKinds.DebtPaymentPrefix))
                .ToListAsync();

            monthSales = monthSalesList.Sum(s => s.TotalAmount);
        }

        // Block 4: Recent 10 sales
        await using (var context = await _contextFactory.CreateDbContextAsync())
        {
            recent = await context.Sales
                .AsNoTracking()
                .Include(s => s.User)
                .Where(s => !s.IsDeleted && !s.InvoiceNumber.StartsWith(SaleRecordKinds.DebtPaymentPrefix))
                .OrderByDescending(s => s.SaleDate)
                .Take(10)
                .ToListAsync();
        }

        // Block 5: Low stock products
        await using (var context = await _contextFactory.CreateDbContextAsync())
        {
            lowStock = await context.Products
                .AsNoTracking()
                .Include(p => p.Category)
                .Where(p => p.Stock <= p.MinStockLevel && p.IsActive && !p.IsDeleted)
                .OrderBy(p => p.Stock)
                .Take(10)
                .ToListAsync();

            lowStockCount = lowStock.Count;
        }

        // Block 6: Counts and current shift
        await using (var context = await _contextFactory.CreateDbContextAsync())
        {
            totalProducts = await context.Products.AsNoTracking().CountAsync(p => !p.IsDeleted);
            totalCustomers = await context.Customers.AsNoTracking().CountAsync(c => !c.IsDeleted);

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

                currentShiftSales = shiftSalesList.Sum(s => s.TotalAmount);
                hasActiveShift = true;
            }
        }

        // Block 7: AI Predictions
        predictions = await _aiPredictionService.GetInventoryPredictionsAsync();

        // Apply all results to UI (must run on UI thread)
        TodayTransactions = todayTransactions;
        TodaySales = todaySales;
        TodayProfit = todayProfit;
        MonthSales = monthSales;
        LowStockCount = lowStockCount;
        TotalProducts = totalProducts;
        TotalCustomers = totalCustomers;
        HasActiveShift = hasActiveShift;
        CurrentShiftSales = currentShiftSales;

        RecentSales.SyncWith(recent);
        LowStockProducts.SyncWith(lowStock);
        AiStockPredictions.SyncWith(predictions);
    }

    /// <summary>
    /// Load dashboard data
    /// </summary>
    [RelayCommand]
    private async Task LoadDashboardData()
    {
        await ExecuteBusyAsync(LoadDashboardDataCoreAsync, "جاري التحديث...");
    }

    /// <summary>
    /// Refresh dashboard
    /// </summary>
    [RelayCommand]
    private async Task RefreshDashboard()
    {
        await LoadDashboardData();
    }
}
