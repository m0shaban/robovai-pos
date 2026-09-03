using System.Collections.ObjectModel;
using System.Windows;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.EntityFrameworkCore;
using SmartPOS.Application.Extensions;
using SmartPOS.Core.Entities;
using SmartPOS.Core.Interfaces;
using SmartPOS.Infrastructure.Data;
using QuestPDF.Fluent;
using LiveChartsCore;
using LiveChartsCore.SkiaSharpView;
using LiveChartsCore.SkiaSharpView.Painting;
using SkiaSharp;
using SmartPOS.Application.Utilities;
using System.Globalization;

namespace SmartPOS.Application.ViewModels;

/// <summary>
/// Factory pattern fix (v5.1)
/// </summary>
public partial class ReportsViewModel : BaseViewModel
{
    private readonly IDbContextFactory<AppDbContext> _contextFactory;
    private readonly IPrintingService _printingService;
    private readonly ISettingsService _settingsService;

    // --- Statistics ---
    [ObservableProperty]
    private decimal _todaySales;

    [ObservableProperty]
    private decimal _todayProfit;

    [ObservableProperty]
    private int _todayTransactions;

    [ObservableProperty]
    private decimal _weekSales;

    [ObservableProperty]
    private decimal _monthSales;

    [ObservableProperty]
    private decimal _totalExpenses;

    [ObservableProperty]
    private decimal _netProfit;

    [ObservableProperty]
    private decimal _inventoryValuation;

    [ObservableProperty]
    private decimal _customPeriodSales;

    [ObservableProperty]
    private decimal _customPeriodProfit;

    // --- Date Filters (Income Report) ---
    [ObservableProperty]
    private DateTime _startDate = DateTime.Today;

    [ObservableProperty]
    private DateTime _endDate = DateTime.Today;

    // --- Date Filters (Sales Search) ---
    [ObservableProperty]
    private DateTime _selectedStartDate = DateTime.Now.Date;

    [ObservableProperty]
    private DateTime _selectedEndDate = DateTime.Now.Date.AddDays(1).AddSeconds(-1);

    // --- Collections ---
    [ObservableProperty]
    private ObservableCollection<Sale> _recentSales = new();

    [ObservableProperty]
    private ObservableCollection<Product> _topProducts = new();

    [ObservableProperty]
    private ObservableCollection<Expense> _recentExpenses = new();

    // ─── Chart Data ──────────────────────────────────────────────────────────
    [ObservableProperty]
    private ISeries[] _salesChartSeries = Array.Empty<ISeries>();

    [ObservableProperty]
    private Axis[] _salesChartXAxes = Array.Empty<Axis>();

    [ObservableProperty]
    private ISeries[] _profitChartSeries = Array.Empty<ISeries>();

    [ObservableProperty]
    private ISeries[] _categoryChartSeries = Array.Empty<ISeries>();

    [ObservableProperty]
    private ObservableCollection<CategoryLegendItem> _categoryLegend = new();

    public ReportsViewModel(IDbContextFactory<AppDbContext> contextFactory, IPrintingService printingService, ISettingsService settingsService)
    {
        _contextFactory = contextFactory;
        _printingService = printingService;
        _settingsService = settingsService;

        _ = InitializeAsync();
    }

    private async Task InitializeAsync()
    {
        await ExecuteBusyAsync(LoadReportsCoreAsync, "⏳ جاري تحميل الإحصائيات...", "✅ تم تحميل البيانات");
    }

    private async Task LoadReportsCoreAsync()
    {
        await LoadSalesStatisticsAsync();
        await LoadTopProductsAsync();
        await LoadRecentExpensesAsync();
        await LoadInventoryValuationAsync();
        await FilterReportsCoreAsync();
        await LoadChartsAsync();
    }

    [RelayCommand]
    public async Task LoadReportsAsync()
    {
        await ExecuteBusyAsync(LoadReportsCoreAsync, "جاري تحديث التقارير...");
    }

    private async Task LoadInventoryValuationAsync()
    {
        await using var ctx = await _contextFactory.CreateDbContextAsync();
        var products = await ctx.Products.AsNoTracking().ToListAsync();
        InventoryValuation = products.Sum(p => p.PurchasePrice * p.Stock);
    }

    private async Task LoadSalesStatisticsAsync()
    {
        await using var ctx = await _contextFactory.CreateDbContextAsync();
        var today = DateTime.Now.Date;
        var weekStart = today.AddDays(-(int)today.DayOfWeek);
        var monthStart = new DateTime(today.Year, today.Month, 1);

        var todaySalesList = await ctx.Sales
            .AsNoTracking()
            .Where(s => s.SaleDate >= today && s.SaleDate < today.AddDays(1)
                     && s.Status == SaleStatus.Completed && !s.IsDeleted
                     && !s.InvoiceNumber.StartsWith(SaleRecordKinds.DebtPaymentPrefix))
            .ToListAsync();
        TodaySales = todaySalesList.Sum(s => s.TotalAmount);
        TodayTransactions = todaySalesList.Count;

        var todayDetailsList = await ctx.SaleDetails
            .AsNoTracking()
            .Where(sd => sd.Sale.SaleDate >= today && sd.Sale.SaleDate < today.AddDays(1) && sd.Sale.Status == SaleStatus.Completed && !sd.IsDeleted)
            .ToListAsync();
        TodayProfit = todayDetailsList.Sum(sd => sd.LineProfit);

        var todayExpensesList = await ctx.Expenses
            .AsNoTracking()
            .Where(e => e.ExpenseDate >= today && e.ExpenseDate < today.AddDays(1) && !e.IsDeleted)
            .ToListAsync();
        NetProfit = TodayProfit - todayExpensesList.Sum(e => e.Amount);

        var weekSalesList = await ctx.Sales
            .AsNoTracking()
            .Where(s => s.SaleDate >= weekStart && s.SaleDate < today.AddDays(1)
                     && s.Status == SaleStatus.Completed && !s.IsDeleted
                     && !s.InvoiceNumber.StartsWith(SaleRecordKinds.DebtPaymentPrefix))
            .ToListAsync();
        WeekSales = weekSalesList.Sum(s => s.TotalAmount);

        var monthSalesList = await ctx.Sales
            .AsNoTracking()
            .Where(s => s.SaleDate >= monthStart && s.SaleDate < today.AddDays(1)
                     && s.Status == SaleStatus.Completed && !s.IsDeleted
                     && !s.InvoiceNumber.StartsWith(SaleRecordKinds.DebtPaymentPrefix))
            .ToListAsync();
        MonthSales = monthSalesList.Sum(s => s.TotalAmount);

        var sales = await ctx.Sales
            .AsNoTracking()
            .Include(s => s.User)
            .Where(s => !s.InvoiceNumber.StartsWith(SaleRecordKinds.DebtPaymentPrefix))
            .OrderByDescending(s => s.SaleDate)
            .Take(10)
            .ToListAsync();
        RecentSales.SyncWith(sales);

        var monthExpensesList = await ctx.Expenses
            .AsNoTracking()
            .Where(e => e.ExpenseDate >= monthStart && !e.IsDeleted)
            .ToListAsync();
        TotalExpenses = monthExpensesList.Sum(e => e.Amount);
    }

    private async Task LoadTopProductsAsync()
    {
        await using var ctx = await _contextFactory.CreateDbContextAsync();
        var monthStart = new DateTime(DateTime.Now.Year, DateTime.Now.Month, 1);

        var saleDetails = await ctx.SaleDetails
            .AsNoTracking()
            .Include(sd => sd.Product)
            .ThenInclude(p => p.Category)
            .Where(sd => sd.Sale.SaleDate >= monthStart && sd.Sale.Status == SaleStatus.Completed && !sd.IsDeleted)
            .ToListAsync();

        var topProducts = saleDetails
            .GroupBy(sd => sd.ProductId)
            .Select(g => new
            {
                Product = g.First().Product,
                TotalQuantity = g.Sum(sd => sd.Quantity)
            })
            .OrderByDescending(x => x.TotalQuantity)
            .Take(10)
            .Select(x => x.Product)
            .ToList();

        TopProducts.SyncWith(topProducts);
    }

    private async Task LoadRecentExpensesAsync()
    {
        await using var ctx = await _contextFactory.CreateDbContextAsync();
        var expenses = await ctx.Expenses
            .AsNoTracking()
            .Include(e => e.User)
            .Where(e => !e.IsDeleted)
            .OrderByDescending(e => e.ExpenseDate)
            .Take(10)
            .ToListAsync();

        RecentExpenses.SyncWith(expenses);
    }

    private async Task FilterReportsCoreAsync()
    {
        await using var ctx = await _contextFactory.CreateDbContextAsync();
        var start = StartDate.Date;
        var endExclusive = EndDate.Date.AddDays(1);

        var customSalesList = await ctx.Sales
            .AsNoTracking()
            .Where(s => s.SaleDate >= start && s.SaleDate < endExclusive
                     && s.Status == SaleStatus.Completed && !s.IsDeleted
                     && !s.InvoiceNumber.StartsWith(SaleRecordKinds.DebtPaymentPrefix))
            .ToListAsync();
        CustomPeriodSales = customSalesList.Sum(s => s.TotalAmount);

        var customDetailsList = await ctx.SaleDetails
            .AsNoTracking()
            .Where(sd => sd.Sale.SaleDate >= start && sd.Sale.SaleDate < endExclusive && sd.Sale.Status == SaleStatus.Completed && !sd.IsDeleted)
            .ToListAsync();
        CustomPeriodProfit = customDetailsList.Sum(sd => sd.LineProfit);
    }

    [RelayCommand]
    private async Task FilterReportsAsync()
    {
        await ExecuteBusyAsync(FilterReportsCoreAsync, "جاري حساب الإيرادات...");
    }

    [RelayCommand]
    private async Task FilterByDateAsync()
    {
        await ExecuteBusyAsync(async () =>
        {
            await using var ctx = await _contextFactory.CreateDbContextAsync();
            var start = SelectedStartDate.Date;
            var endExclusive = SelectedEndDate.Date.AddDays(1);

            var sales = await ctx.Sales
                .AsNoTracking()
                .Include(s => s.User)
                .Where(s => s.SaleDate >= start && s.SaleDate < endExclusive
                         && s.Status == SaleStatus.Completed && !s.IsDeleted
                         && !s.InvoiceNumber.StartsWith(SaleRecordKinds.DebtPaymentPrefix))
                .OrderByDescending(s => s.SaleDate)
                .ToListAsync();

            RecentSales.SyncWith(sales);

            var totalSales = sales.Sum(s => s.TotalAmount);
            MessageBox.Show($"إجمالي المبيعات في الفترة المحددة: {totalSales:F2} ج.م\nعدد العمليات: {sales.Count}",
                "نتيجة البحث", MessageBoxButton.OK, MessageBoxImage.Information);

        }, "جاري فلترة المبيعات...");
    }

    // --- PDF Generation Commands ---
    [RelayCommand]
    private async Task PrintPurchasesReportAsync()
    {
        try
        {
            await using var ctx = await _contextFactory.CreateDbContextAsync();
        var products = await ctx.Products.AsNoTracking().Include(p => p.Category).Where(p => !p.IsDeleted).ToListAsync();

            var dialog = new Microsoft.Win32.SaveFileDialog
            {
                Filter = "PDF Files|*.pdf",
                FileName = $"تقرير_المشتريات_{DateTime.Now:yyyyMMdd_HHmmss}.pdf",
                DefaultExt = ".pdf",
                Title = "حفظ تقرير المشتريات"
            };

            if (dialog.ShowDialog() == true)
            {
                QuestPDF.Settings.License = QuestPDF.Infrastructure.LicenseType.Community;
                var doc = Document.Create(container =>
                {
                    container.Page(page =>
                    {
                        page.Size(QuestPDF.Helpers.PageSizes.A4);
                        page.Margin(2, QuestPDF.Infrastructure.Unit.Centimetre);
                        page.PageColor(QuestPDF.Helpers.Colors.White);
                        page.DefaultTextStyle(x => x.FontFamily("Segoe UI").FontSize(10).DirectionFromRightToLeft());

                        page.Header().Column(col =>
                        {
                            col.Item().Text("تقرير المشتريات الشامل").Bold().FontSize(20).FontColor("#1E3A5F").AlignCenter();
                            col.Item().Text($"التاريخ: {DateTime.Now:dd/MM/yyyy}").FontSize(12).FontColor("#4B5563").AlignCenter();
                            col.Item().PaddingVertical(10).LineHorizontal(1).LineColor("#CBD5E1");
                        });

                        page.Content().PaddingVertical(10).Column(col =>
                        {
                            col.Spacing(15);
                            col.Item().Table(table =>
                            {
                                table.ColumnsDefinition(c =>
                                {
                                    c.RelativeColumn(3); // المنتج
                                    c.RelativeColumn(1); // الكمية
                                    c.RelativeColumn(1); // التكلفة
                                    c.RelativeColumn(1.5f); // الإجمالي
                                });

                                table.Header(h =>
                                {
                                    h.Cell().Background("#1E3A5F").Padding(5).Text("المنتج").Bold().FontColor(QuestPDF.Helpers.Colors.White);
                                    h.Cell().Background("#1E3A5F").Padding(5).Text("الكمية بالمخزن").Bold().FontColor(QuestPDF.Helpers.Colors.White).AlignCenter();
                                    h.Cell().Background("#1E3A5F").Padding(5).Text("تكلفة الوحدة").Bold().FontColor(QuestPDF.Helpers.Colors.White).AlignCenter();
                                    h.Cell().Background("#1E3A5F").Padding(5).Text("إجمالي التكلفة").Bold().FontColor(QuestPDF.Helpers.Colors.White).AlignRight();
                                });

                                foreach (var p in products)
                                {
                                    table.Cell().BorderBottom(0.5f).BorderColor("#E5E7EB").Padding(5).Text(p.Name);
                                    table.Cell().BorderBottom(0.5f).BorderColor("#E5E7EB").Padding(5).Text(p.Stock.ToString()).AlignCenter();
                                    table.Cell().BorderBottom(0.5f).BorderColor("#E5E7EB").Padding(5).Text($"{p.PurchasePrice:N2}").AlignCenter();
                                    table.Cell().BorderBottom(0.5f).BorderColor("#E5E7EB").Padding(5).Text($"{(p.PurchasePrice * p.Stock):N2}").AlignRight();
                                }
                            });

                            col.Item().AlignRight().Text($"إجمالي قيمة المشتريات بالمخزن: {InventoryValuation:N2} ج.م").Bold().FontSize(14).FontColor("#DC2626");
                        });

                        page.Footer().AlignCenter().Text($"تمت الطباعة بواسطة SmartPOS | {DateTime.Now:dd/MM/yyyy HH:mm:ss}").FontSize(9).FontColor("#9CA3AF");
                    });
                });

                doc.GeneratePdf(dialog.FileName);
                MessageBox.Show("تم حفظ تقرير المشتريات بنجاح!", "نجح", MessageBoxButton.OK, MessageBoxImage.Information);
                try { System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo { FileName = dialog.FileName, UseShellExecute = true }); } catch { }
            }
        }
        catch (Exception ex)
        {
            MessageBox.Show($"خطأ في إنشاء التقرير: {ex.Message}", "خطأ", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    [RelayCommand]
    private async Task PrintIncomeReportAsync()
    {
        var dialog = new Microsoft.Win32.SaveFileDialog
        {
            Filter = "PDF Files|*.pdf",
            FileName = $"تقرير_الإيرادات_{DateTime.Now:yyyyMMdd_HHmmss}.pdf",
            DefaultExt = ".pdf",
            Title = "حفظ تقرير الإيرادات"
        };

        if (dialog.ShowDialog() != true) return;

        var savePath = dialog.FileName;

        await ExecuteBusyAsync(async () =>
        {
            try
            {
                await using var ctx = await _contextFactory.CreateDbContextAsync();
                var start = StartDate.Date;
                var endExclusive = EndDate.Date.AddDays(1);

                var sales = await ctx.Sales
                    .AsNoTracking()
                    .Include(s => s.SaleDetails)
                    .Where(s => s.SaleDate >= start && s.SaleDate < endExclusive
                             && !s.IsDeleted
                             && s.Status == SaleStatus.Completed
                             && !s.InvoiceNumber.StartsWith(SaleRecordKinds.DebtPaymentPrefix))
                    .OrderByDescending(s => s.SaleDate)
                    .ToListAsync();

                var totalSales = sales.Sum(s => s.TotalAmount);
                var totalProfit = sales.SelectMany(s => s.SaleDetails).Sum(d => (d.UnitPrice - d.UnitCost) * d.Quantity);
                var totalTransactions = sales.Count;

                QuestPDF.Settings.License = QuestPDF.Infrastructure.LicenseType.Community;
                var doc = Document.Create(container =>
                {
                    container.Page(page =>
                    {
                        page.Size(QuestPDF.Helpers.PageSizes.A4);
                        page.Margin(2, QuestPDF.Infrastructure.Unit.Centimetre);
                        page.PageColor(QuestPDF.Helpers.Colors.White);
                        page.DefaultTextStyle(x => x.FontFamily("Segoe UI").FontSize(11).DirectionFromRightToLeft());

                        page.Header().Column(col =>
                        {
                            col.Item().Text("تقرير الإيرادات الشامل").Bold().FontSize(22).FontColor("#1E3A5F").AlignCenter();
                            col.Item().Text($"الفترة: من {StartDate:dd/MM/yyyy} إلى {EndDate:dd/MM/yyyy}").FontSize(12).FontColor("#4B5563").AlignCenter();
                            col.Item().PaddingVertical(10).LineHorizontal(1).LineColor("#CBD5E1");
                        });

                        page.Content().PaddingVertical(20).Column(col =>
                        {
                            col.Spacing(16);

                            // Summary Box
                            col.Item().Border(1).BorderColor("#E5E7EB").Padding(15).Column(c =>
                            {
                                c.Item().Text("ملخص مالي").Bold().FontSize(14).FontColor("#1E3A5F");
                                c.Item().PaddingTop(12).Row(row =>
                                {
                                    row.RelativeItem().Column(inner =>
                                    {
                                        inner.Item().Text("إجمالي المبيعات").FontSize(11).FontColor("#6B7280");
                                        inner.Item().Text($"{totalSales:N2} ج.م").Bold().FontSize(20).FontColor("#059669");
                                    });
                                    row.RelativeItem().Column(inner =>
                                    {
                                        inner.Item().Text("صافي الأرباح").FontSize(11).FontColor("#6B7280");
                                        inner.Item().Text($"{totalProfit:N2} ج.م").Bold().FontSize(20).FontColor("#2563EB");
                                    });
                                    row.RelativeItem().Column(inner =>
                                    {
                                        inner.Item().Text("عدد الفواتير").FontSize(11).FontColor("#6B7280");
                                        inner.Item().Text($"{totalTransactions}").Bold().FontSize(20).FontColor("#7C3AED");
                                    });
                                });
                            });

                            // Sales Table
                            if (sales.Any())
                            {
                                col.Item().Text("تفاصيل الفواتير").Bold().FontSize(13).FontColor("#1E3A5F");
                                col.Item().Table(table =>
                                {
                                    table.ColumnsDefinition(c =>
                                    {
                                        c.RelativeColumn(2);    // التاريخ
                                        c.RelativeColumn(2);    // رقم الفاتورة
                                        c.RelativeColumn(1.5f); // الإجمالي
                                        c.RelativeColumn(1.5f); // المدفوع
                                    });

                                    table.Header(h =>
                                    {
                                        h.Cell().Background("#1E3A5F").Padding(6).Text("التاريخ").Bold().FontColor(QuestPDF.Helpers.Colors.White);
                                        h.Cell().Background("#1E3A5F").Padding(6).Text("رقم الفاتورة").Bold().FontColor(QuestPDF.Helpers.Colors.White).AlignCenter();
                                        h.Cell().Background("#1E3A5F").Padding(6).Text("الإجمالي").Bold().FontColor(QuestPDF.Helpers.Colors.White).AlignCenter();
                                        h.Cell().Background("#1E3A5F").Padding(6).Text("المدفوع").Bold().FontColor(QuestPDF.Helpers.Colors.White).AlignRight();
                                    });

                                    foreach (var s in sales)
                                    {
                                        table.Cell().BorderBottom(0.5f).BorderColor("#E5E7EB").Padding(5).Text(s.SaleDate.ToString("dd/MM/yyyy HH:mm"));
                                        table.Cell().BorderBottom(0.5f).BorderColor("#E5E7EB").Padding(5).Text(s.InvoiceNumber ?? "-").AlignCenter();
                                        table.Cell().BorderBottom(0.5f).BorderColor("#E5E7EB").Padding(5).Text($"{s.TotalAmount:N2}").AlignCenter();
                                        table.Cell().BorderBottom(0.5f).BorderColor("#E5E7EB").Padding(5).Text($"{s.AmountPaid:N2}").AlignRight();
                                    }
                                });
                            }
                            else
                            {
                                col.Item().AlignCenter().Text("لا توجد مبيعات في هذه الفترة").FontSize(13).FontColor("#9CA3AF");
                            }
                        });

                        page.Footer().AlignCenter().Text($"تمت الطباعة بواسطة SmartPOS | {DateTime.Now:dd/MM/yyyy HH:mm:ss}").FontSize(9).FontColor("#9CA3AF");
                    });
                });

                doc.GeneratePdf(savePath);
                MessageBox.Show("تم حفظ تقرير الإيرادات بنجاح!", "نجح", MessageBoxButton.OK, MessageBoxImage.Information);
                try { System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo { FileName = savePath, UseShellExecute = true }); } catch { }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"خطأ في إنشاء التقرير: {ex.Message}", "خطأ", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }, "جاري إنشاء تقرير الإيرادات...");
    }

    // ─── Profit Report ────────────────────────────────────────────────────────
    [ObservableProperty]
    private ObservableCollection<ProfitItem> _profitItems = new();

    [ObservableProperty]
    private decimal _totalGrossProfit;

    [ObservableProperty]
    private DateTime _profitStartDate = DateTime.Now.AddDays(-30);

    [ObservableProperty]
    private DateTime _profitEndDate = DateTime.Now;

    [RelayCommand]
    private async Task LoadProfitReportAsync()
    {
        await ExecuteBusyAsync(async () =>
        {
            await using var ctx = await _contextFactory.CreateDbContextAsync();
            var start = ProfitStartDate.Date;
            var end = ProfitEndDate.Date.AddDays(1);

            var details = await ctx.SaleDetails
                .AsNoTracking()
                .Include(d => d.Product)
                .Where(d => !d.IsDeleted && d.Sale.SaleDate >= start && d.Sale.SaleDate < end && d.Sale.Status == SaleStatus.Completed)
                .ToListAsync();

            var grouped = details
                .GroupBy(d => d.ProductId)
                .Select(g => new ProfitItem
                {
                    ProductName = g.First().Product?.Name ?? "غير معروف",
                    TotalQuantity = g.Sum(d => d.Quantity),
                    TotalRevenue = g.Sum(d => d.LineTotal),
                    TotalCost = g.Sum(d => d.UnitCost * d.Quantity),
                    GrossProfit = g.Sum(d => (d.UnitPrice - d.UnitCost) * d.Quantity)
                })
                .OrderByDescending(p => p.GrossProfit)
                .ToList();

            ProfitItems.SyncWith(grouped);
            TotalGrossProfit = grouped.Sum(p => p.GrossProfit);
        }, "جاري تحميل تقرير الأرباح...");
    }

    // ─── Cashier Stats ────────────────────────────────────────────────────────
    [ObservableProperty]
    private ObservableCollection<CashierStat> _cashierStats = new();

    [ObservableProperty]
    private DateTime _cashierStatsStart = DateTime.Now.AddDays(-30);

    [ObservableProperty]
    private DateTime _cashierStatsEnd = DateTime.Now;

    [RelayCommand]
    private async Task LoadCashierStatsAsync()
    {
        await ExecuteBusyAsync(async () =>
        {
            await using var ctx = await _contextFactory.CreateDbContextAsync();
            var start = CashierStatsStart.Date;
            var end = CashierStatsEnd.Date.AddDays(1);

            var sales = await ctx.Sales
                .AsNoTracking()
                .Include(s => s.User)
                .Where(s => !s.IsDeleted
                         && s.SaleDate >= start
                         && s.SaleDate < end
                         && s.Status == SaleStatus.Completed
                         && !s.InvoiceNumber.StartsWith(SaleRecordKinds.DebtPaymentPrefix))
                .ToListAsync();

            var stats = sales
                .GroupBy(s => s.UserId)
                .Select(g => new CashierStat
                {
                    CashierName = g.First().User?.FullName ?? "غير معروف",
                    TotalSales = g.Sum(s => s.TotalAmount),
                    TotalTransactions = g.Count(),
                    AverageTicket = g.Count() > 0 ? g.Sum(s => s.TotalAmount) / g.Count() : 0
                })
                .OrderByDescending(s => s.TotalSales)
                .ToList();

            CashierStats.SyncWith(stats);
        }, "جاري تحميل إحصائيات الكاشير...");
    }

    // ─── Debt Dashboard ───────────────────────────────────────────────────────
    [ObservableProperty]
    private ObservableCollection<Core.Entities.Customer> _customersWithDebt = new();

    [ObservableProperty]
    private ObservableCollection<Core.Entities.Supplier> _suppliersWithDebt2 = new();

    [ObservableProperty]
    private decimal _totalCustomerDebt;

    [ObservableProperty]
    private decimal _totalSupplierDebt;

    [ObservableProperty]
    private decimal _netDebtPosition;

    [RelayCommand]
    private async Task LoadDebtDashboardAsync()
    {
        await ExecuteBusyAsync(async () =>
        {
            await using var ctx = await _contextFactory.CreateDbContextAsync();
            var customers = await ctx.Customers
                .AsNoTracking()
                .Where(c => !c.IsDeleted && c.CurrentDebt > 0)
                .OrderByDescending(c => (double)c.CurrentDebt)
                .ToListAsync();
            CustomersWithDebt.SyncWith(customers);
            TotalCustomerDebt = customers.Sum(c => c.CurrentDebt);

            var suppliers = await ctx.Suppliers
                .AsNoTracking()
                .Where(s => !s.IsDeleted && s.DebtAmount > 0)
                .OrderByDescending(s => (double)s.DebtAmount)
                .ToListAsync();
            SuppliersWithDebt2.SyncWith(suppliers);
            TotalSupplierDebt = suppliers.Sum(s => s.DebtAmount);

            NetDebtPosition = TotalCustomerDebt - TotalSupplierDebt;
        }, "جاري تحميل لوحة الديون...");
    }

    // ─── Expiry Tracking ─────────────────────────────────────────────────────
    [ObservableProperty]
    private ObservableCollection<Core.Entities.Product> _expiringProducts = new();

    [ObservableProperty]
    private int _expiryAlertDays = 30;

    [RelayCommand]
    private async Task LoadExpiryTrackingAsync()
    {
        await ExecuteBusyAsync(async () =>
        {
            await using var ctx = await _contextFactory.CreateDbContextAsync();
            var alertDate = DateTime.Today.AddDays(ExpiryAlertDays);
        var products = await ctx.Products
                .AsNoTracking()
                .Include(p => p.Category)
                .Include(p => p.Supplier)
                .Where(p => p.IsActive && !p.IsDeleted && p.ExpiryDate.HasValue && p.ExpiryDate.Value <= alertDate)
                .OrderBy(p => p.ExpiryDate)
                .ToListAsync();
            ExpiringProducts.SyncWith(products);
        }, "جاري تحميل بيانات الصلاحية...");
    }

    // ─── CSV Export ───────────────────────────────────────────────────────────
    [RelayCommand]
    private async Task ExportSalesToCsvAsync()
    {
        await ExecuteBusyAsync(async () =>
        {
            await using var ctx = await _contextFactory.CreateDbContextAsync();
            var start = SelectedStartDate.Date;
            var end = SelectedEndDate.Date.AddDays(1);

            var sales = await ctx.Sales
                .AsNoTracking()
                .Include(s => s.User)
                .Include(s => s.Customer)
                .Where(s => !s.IsDeleted
                         && s.SaleDate >= start
                         && s.SaleDate < end
                         && !s.InvoiceNumber.StartsWith(SaleRecordKinds.DebtPaymentPrefix))
                .OrderByDescending(s => s.SaleDate)
                .ToListAsync();

            var defaultFolder = _settingsService?.DefaultExportFolder;
            if (!string.IsNullOrWhiteSpace(defaultFolder) && !System.IO.Directory.Exists(defaultFolder))
            {
                try { System.IO.Directory.CreateDirectory(defaultFolder); } catch { }
            }

            var dlg = new Microsoft.Win32.SaveFileDialog
            {
                Title = "تصدير المبيعات إلى CSV",
                FileName = $"مبيعات_{start:yyyy-MM-dd}_{end:yyyy-MM-dd}.csv",
                InitialDirectory = !string.IsNullOrWhiteSpace(defaultFolder) && System.IO.Directory.Exists(defaultFolder)
                    ? defaultFolder
                    : Environment.GetFolderPath(Environment.SpecialFolder.Desktop),
                DefaultExt = ".csv",
                Filter = "CSV Files (*.csv)|*.csv"
            };
            if (dlg.ShowDialog() != true) return;

            static string Escape(object? val)
            {
                var s = val?.ToString() ?? "";
                return $"\"{s.Replace("\"", "\"\"")}\"";
            }

            var lines = new System.Text.StringBuilder();
            lines.AppendLine("رقم الفاتورة,التاريخ,الكاشير,العميل,المجموع,المدفوع,طريقة الدفع,الحالة");
            foreach (var s in sales)
            {
                var custName = s.Customer?.Name ?? "-";
                lines.AppendLine($"{Escape(s.InvoiceNumber)},{Escape(s.SaleDate.ToString("dd/MM/yyyy HH:mm"))},{Escape(s.User?.FullName)},{Escape(custName)},{s.TotalAmount:F2},{s.AmountPaid:F2},{Escape(s.PaymentMethod)},{Escape(s.Status)}");
            }

            // UTF8 with BOM ensures Excel opens Arabic correctly
            await System.IO.File.WriteAllTextAsync(dlg.FileName, lines.ToString(), new System.Text.UTF8Encoding(true));

            if (_settingsService?.AutoOpenExportedFile == true)
            {
                try { System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo(dlg.FileName) { UseShellExecute = true }); } catch { }
            }

            MessageBox.Show($"✅ تم تصدير {sales.Count} فاتورة بنجاح\n{dlg.FileName}", "تم التصدير", MessageBoxButton.OK, MessageBoxImage.Information);
        }, "جاري تصدير البيانات...");
    }

    [RelayCommand]
    private async Task ExportProfitToCsvAsync()
    {
        if (ProfitItems.Count == 0)
        {
            MessageBox.Show("يرجى تحميل تقرير الأرباح أولاً", "تنبيه", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        var defaultFolder = _settingsService?.DefaultExportFolder;
        if (!string.IsNullOrWhiteSpace(defaultFolder) && !System.IO.Directory.Exists(defaultFolder))
        {
            try { System.IO.Directory.CreateDirectory(defaultFolder); } catch { }
        }

        var dlg = new Microsoft.Win32.SaveFileDialog
        {
            Title = "تصدير تقرير الأرباح إلى CSV",
            FileName = $"أرباح_{ProfitStartDate:yyyy-MM-dd}_{ProfitEndDate:yyyy-MM-dd}.csv",
            InitialDirectory = !string.IsNullOrWhiteSpace(defaultFolder) && System.IO.Directory.Exists(defaultFolder)
                ? defaultFolder
                : Environment.GetFolderPath(Environment.SpecialFolder.Desktop),
            DefaultExt = ".csv",
            Filter = "CSV Files (*.csv)|*.csv"
        };
        if (dlg.ShowDialog() != true) return;

        static string Escape(object? val)
        {
            var s = val?.ToString() ?? "";
            return $"\"{s.Replace("\"", "\"\"")}\"";
        }

        var lines = new System.Text.StringBuilder();
        lines.AppendLine("المنتج,الكمية المباعة,الإيراد الإجمالي,التكلفة الإجمالية,الربح الإجمالي");
        foreach (var p in ProfitItems)
        {
            lines.AppendLine($"{Escape(p.ProductName)},{p.TotalQuantity},{p.TotalRevenue:F2},{p.TotalCost:F2},{p.GrossProfit:F2}");
        }

        await System.IO.File.WriteAllTextAsync(dlg.FileName, lines.ToString(), new System.Text.UTF8Encoding(true));

        if (_settingsService?.AutoOpenExportedFile == true)
        {
            try { System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo(dlg.FileName) { UseShellExecute = true }); } catch { }
        }

        MessageBox.Show($"✅ تم تصدير تقرير الأرباح بنجاح:\n{dlg.FileName}", "تم التصدير", MessageBoxButton.OK, MessageBoxImage.Information);
    }

    // \u2500\u2500\u2500 Charts \u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500
    private async Task LoadChartsAsync()
    {
        await using var ctx = await _contextFactory.CreateDbContextAsync();
        var today = DateTime.Now.Date;

        // \u2500\u2500 1. Bar Chart: \u0622\u062e\u0631 7 \u0623\u064a\u0627\u0645 \u0645\u0628\u064a\u0639\u0627\u062a \u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500
        var last7Days = Enumerable.Range(0, 7)
            .Select(i => today.AddDays(-6 + i))
            .ToList();

        var salesLast7 = await ctx.Sales
            .AsNoTracking()
            .Where(s => s.SaleDate >= today.AddDays(-6)
                     && !s.IsDeleted
                     && s.Status == SaleStatus.Completed
                     && !s.InvoiceNumber.StartsWith(SaleRecordKinds.DebtPaymentPrefix))
            .ToListAsync();

        var dailySales = last7Days
            .Select(d => (double)(salesLast7
                .Where(s => s.SaleDate.Date == d)
                .Sum(s => s.TotalAmount)))
            .ToArray();

        var englishCulture = new CultureInfo("en-US");
        var dayLabels = last7Days
            .Select(d => d.ToString("ddd dd/MM", englishCulture))
            .ToArray();

        SalesChartSeries = new ISeries[]
        {
            new ColumnSeries<double>
            {
                Name = "Daily Sales",
                Values = dailySales,
                Fill = new SolidColorPaint(SKColor.Parse("#06B6D4")),
                Stroke = null,
                MaxBarWidth = 40,
                DataLabelsPaint = new SolidColorPaint(SKColors.White),
                DataLabelsSize = 11,
                DataLabelsPosition = LiveChartsCore.Measure.DataLabelsPosition.Top,
                DataLabelsFormatter = p => p.Coordinate.PrimaryValue > 0 ? $"{p.Coordinate.PrimaryValue:N0}" : "",
                YToolTipLabelFormatter = p => $"{p.Coordinate.PrimaryValue:N2} {ArabicChartText.Shape("ج.م")}"
            }
        };

        SalesChartXAxes = new Axis[]
        {
            new Axis
            {
                Labels = dayLabels,
                LabelsPaint = new SolidColorPaint(SKColor.Parse("#94A3B8")),
                SeparatorsPaint = new SolidColorPaint(SKColor.Parse("#1E293B")),
            }
        };

        // \u2500\u2500 2. Line Chart: \u0623\u0631\u0628\u0627\u062d \u0622\u062e\u0631 7 \u0623\u064a\u0627\u0645 \u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500
        var saleIdsLast7 = salesLast7.Select(s => s.Id).ToHashSet();
        var detailsLast7 = await ctx.SaleDetails
            .AsNoTracking()
            .Where(d => saleIdsLast7.Contains(d.SaleId) && !d.IsDeleted)
            .ToListAsync();

        var dailyProfit = last7Days.Select(d =>
        {
            var ids = salesLast7.Where(s => s.SaleDate.Date == d).Select(s => s.Id).ToHashSet();
            return (double)(detailsLast7.Where(sd => ids.Contains(sd.SaleId)).Sum(sd => sd.LineProfit));
        }).ToArray();

        ProfitChartSeries = new ISeries[]
        {
            new LineSeries<double>
            {
                Name = "Daily Profit",
                Values = dailyProfit,
                Stroke = new SolidColorPaint(SKColor.Parse("#10B981")) { StrokeThickness = 3 },
                GeometryFill = new SolidColorPaint(SKColor.Parse("#10B981")),
                GeometryStroke = new SolidColorPaint(SKColors.White) { StrokeThickness = 2 },
                GeometrySize = 10,
                LineSmoothness = 0.5,
                Fill = new LinearGradientPaint(
                    new[] { SKColor.Parse("#10B981").WithAlpha(80), SKColors.Transparent },
                    new SKPoint(0.5f, 0f), new SKPoint(0.5f, 1f)),
                YToolTipLabelFormatter = p => $"{p.Coordinate.PrimaryValue:N2} {ArabicChartText.Shape("ج.م")}"
            }
        };

        // \u2500\u2500 3. Pie Chart: \u0645\u0628\u064a\u0639\u0627\u062a \u0627\u0644\u0634\u0647\u0631 \u062d\u0633\u0628 \u0627\u0644\u062a\u0635\u0646\u064a\u0641 \u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500
        var monthStart = new DateTime(today.Year, today.Month, 1);
        var categoryColors = new[] { "#06B6D4", "#10B981", "#F59E0B", "#EF4444", "#8B5CF6", "#EC4899", "#14B8A6", "#F97316" };

        var categoryData = await ctx.SaleDetails
            .AsNoTracking()
            .Include(d => d.Product).ThenInclude(p => p!.Category)
            .Where(d => d.Sale.SaleDate >= monthStart && !d.IsDeleted && d.Sale.Status == SaleStatus.Completed)
            .ToListAsync();

        var grouped = categoryData
            .GroupBy(d => d.Product?.Category?.Name ?? "غير مصنف")
            .Select((g, i) => new { Name = g.Key, Total = (double)g.Sum(d => d.LineTotal), Color = categoryColors[i % categoryColors.Length] })
            .Where(x => x.Total > 0)
            .OrderByDescending(x => x.Total)
            .Take(8)
            .ToList();

        CategoryLegend.SyncWith(grouped.Select(g => new CategoryLegendItem { Name = g.Name, Color = g.Color, Value = g.Total }));

        CategoryChartSeries = grouped.Select(g => (ISeries)new PieSeries<double>
        {
            Name = ArabicChartText.Shape(g.Name),
            Values = new[] { g.Total },
            Fill = new SolidColorPaint(SKColor.Parse(g.Color)),
            Stroke = null,
            InnerRadius = 50,
            DataLabelsPaint = new SolidColorPaint(SKColors.White),
            DataLabelsSize = 11,
            DataLabelsFormatter = p => $"{p.Coordinate.PrimaryValue:N2}",
            ToolTipLabelFormatter = p => $"{p.Coordinate.PrimaryValue:N2} {ArabicChartText.Shape("ج.م")}"
        }).ToArray();
    }
}

// ─── Report DTOs ──────────────────────────────────────────────────────────────
public class CategoryLegendItem
{
    public string Name { get; set; } = string.Empty;
    public string Color { get; set; } = string.Empty;
    public double Value { get; set; }
}

public class ProfitItem
{
    public string ProductName { get; set; } = string.Empty;
    public int TotalQuantity { get; set; }
    public decimal TotalRevenue { get; set; }
    public decimal TotalCost { get; set; }
    public decimal GrossProfit { get; set; }
    public decimal MarginPct => TotalRevenue > 0 ? GrossProfit / TotalRevenue * 100 : 0;
}

public class CashierStat
{
    public string CashierName { get; set; } = string.Empty;
    public decimal TotalSales { get; set; }
    public int TotalTransactions { get; set; }
    public decimal AverageTicket { get; set; }
}
