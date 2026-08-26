using System.Collections.ObjectModel;
using System.Drawing.Printing;
using System.IO;
using System.Windows;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.EntityFrameworkCore;
using QuestPDF.Fluent;
using SmartPOS.Application.Extensions;
using SmartPOS.Application.Utilities;
using SmartPOS.Core.Entities;
using SmartPOS.Core.Interfaces;
using SmartPOS.Infrastructure.Data;

namespace SmartPOS.Application.ViewModels;

public partial class ShiftManagementViewModel : BaseViewModel
{
    private readonly IDbContextFactory<AppDbContext> _contextFactory;
    private readonly IShiftRepository _shiftRepository;
    private readonly IPrintingService _printingService;
    private readonly ISettingsService _settingsService;
    private readonly User _currentUser;
    private readonly INotificationService _notificationService;
    private readonly IAuthorizationService _authorizationService;

    // --- Collections ---
    private List<Shift> _allShifts = new();

    [ObservableProperty]
    private ObservableCollection<Shift> _shifts = new();

    [ObservableProperty]
    private string _searchText = string.Empty;

    // --- Current Shift Info ---
    [ObservableProperty]
    private Shift? _currentShift;

    [ObservableProperty]
    private bool _isShiftOpen;

    [ObservableProperty]
    private decimal _currentShiftSales;

    [ObservableProperty]
    private decimal _currentShiftTotalSales;

    [ObservableProperty]
    private int _currentShiftTransactions;

    [ObservableProperty]
    private decimal _currentShiftExpenses;

    [ObservableProperty]
    private decimal _currentShiftDebtCollections;

    [ObservableProperty]
    private decimal _expectedBalance;

    [ObservableProperty]
    private decimal _totalCardSales;

    [ObservableProperty]
    private decimal _totalVodafoneCashSales;

    [ObservableProperty]
    private decimal _totalInstaPaySales;

    [ObservableProperty]
    private decimal _totalDeferredSales;

    [ObservableProperty]
    private decimal _shiftDifference;

    /// <summary>
    /// True when the current user is a Cashier.
    /// The Cashier should NOT see ExpectedBalance (Blind Z-Report).
    /// </summary>
    public bool IsCashier => _currentUser.Role == UserRole.Cashier;
    public bool IsAdminOrAbove => _currentUser.Role == UserRole.SuperAdmin || _currentUser.Role == UserRole.Admin || _currentUser.Role == UserRole.Manager;

    // --- Form Inputs ---
    [ObservableProperty]
    private decimal _openingBalanceInput;

    [ObservableProperty]
    private decimal _closingBalanceInput;

    [ObservableProperty]
    private string? _notesInput;

    public ShiftManagementViewModel(
        IDbContextFactory<AppDbContext> contextFactory,
        IShiftRepository shiftRepository,
        IPrintingService printingService,
        ISettingsService settingsService,
        User currentUser,
        INotificationService notificationService,
        IAuthorizationService authorizationService)
    {
        _contextFactory = contextFactory;
        _shiftRepository = shiftRepository;
        _printingService = printingService;
        _settingsService = settingsService;
        _currentUser = currentUser;
        _notificationService = notificationService;
        _authorizationService = authorizationService;

        _ = InitializeAsync();
    }

    private async Task InitializeAsync()
    {
        await ExecuteBusyAsync(LoadShiftsCoreAsync, "⏳ جاري تحميل الورديات...", $"✅ تم تحميل {Shifts.Count} وردية");
    }

    private async Task LoadShiftsCoreAsync()
    {
        await using var ctx = await _contextFactory.CreateDbContextAsync();
        var shiftData = await ctx.Shifts
            .AsNoTracking()
            .OrderByDescending(s => s.StartTime)
            .Take(100) // Increased to 100 for better local search
            .Select(s => new
            {
                Shift = s,
                User = s.User,
                TotalSales = (decimal)(s.Sales.Where(x => !x.IsDeleted && x.Status == SaleStatus.Completed && !x.InvoiceNumber.StartsWith(SaleRecordKinds.DebtPaymentPrefix)).Sum(x => (double?)x.TotalAmount) ?? 0.0),
                TransactionCount = s.Sales.Where(x => !x.IsDeleted && x.Status == SaleStatus.Completed && !x.InvoiceNumber.StartsWith(SaleRecordKinds.DebtPaymentPrefix)).Count()
            })
            .ToListAsync();

        var shifts = shiftData.Select(d =>
        {
            var s = d.Shift;
            s.User = d.User;
            s.TotalSales = d.TotalSales;
            s.TransactionCount = d.TransactionCount;
            return s;
        }).ToList();

        _allShifts = shifts;
        FilterShifts();

        CurrentShift = await _shiftRepository.GetActiveShiftByUserIdAsync(_currentUser.Id);
        IsShiftOpen = CurrentShift != null;

        if (IsShiftOpen && CurrentShift != null)
        {
            await LoadCurrentShiftStatsAsync(CurrentShift);
        }
        else
        {
            var lastClosedShift = _allShifts.FirstOrDefault(s => s.Status == ShiftStatus.Closed);
            if (lastClosedShift != null && lastClosedShift.ClosingBalance.HasValue)
            {
                OpeningBalanceInput = lastClosedShift.ClosingBalance.Value;
            }
            else
            {
                OpeningBalanceInput = 0;
            }
        }
    }

    private async Task LoadCurrentShiftStatsAsync(Shift shift)
    {
        await using var ctx = await _contextFactory.CreateDbContextAsync();
        var shiftEnd = shift.EndTime ?? DateTime.Now;

        // ── All aggregates in a single DB round-trip ──────────────────────────
        var salesAggRaw = await ctx.Sales
            .AsNoTracking()
            .Where(s => s.ShiftId == shift.Id
                     && s.Status == SaleStatus.Completed
                     && !s.IsDeleted
                     && !s.InvoiceNumber.StartsWith(SaleRecordKinds.DebtPaymentPrefix))
            .GroupBy(s => s.PaymentMethod)
            .Select(g => new
            {
                Method = g.Key,
                Total = g.Sum(s => (double?)s.TotalAmount) ?? 0.0,
                Count = g.Count()
            })
            .ToListAsync();

        var salesAgg = salesAggRaw.Select(x => new { x.Method, Total = (decimal)x.Total, x.Count }).ToList();

        CurrentShiftTransactions = salesAgg.Sum(x => x.Count);
        CurrentShiftTotalSales = salesAgg.Sum(x => x.Total);
        CurrentShiftSales = salesAgg.Where(x => x.Method == PaymentMethod.Cash).Sum(x => x.Total);
        TotalCardSales = salesAgg.Where(x => x.Method == PaymentMethod.Card).Sum(x => x.Total);
        TotalVodafoneCashSales = salesAgg.Where(x => x.Method == PaymentMethod.VodafoneCash).Sum(x => x.Total);
        TotalInstaPaySales = salesAgg.Where(x => x.Method == PaymentMethod.InstaPay).Sum(x => x.Total);
        TotalDeferredSales = salesAgg.Where(x => x.Method == PaymentMethod.Deferred).Sum(x => x.Total);

        // ── Expenses (cash outflows linked to this shift by user+time) ────────
        // ── Expenses (cash outflows linked to this shift by user+time) ────────
        CurrentShiftExpenses = (decimal)(await ctx.Expenses
            .AsNoTracking()
            .Where(e => e.UserId == shift.UserId
                     && e.ExpenseDate >= shift.StartTime
                     && e.ExpenseDate <= shiftEnd
                     && !e.Description.StartsWith("ضيافة")
                     && !e.IsDeleted)
            .SumAsync(e => (double?)e.Amount) ?? 0.0);

        // ── Refunds (cash already returned to customers this shift) ───────────
        // Returns create a negative sale with InvoiceNumber starting "REF-"
        var cashRefunds = (decimal)(await ctx.Sales
            .AsNoTracking()
            .Where(s => s.ShiftId == shift.Id
                     && s.PaymentMethod == PaymentMethod.Cash
                     && s.TotalAmount < 0
                     && !s.IsDeleted)
            .SumAsync(s => (double?)s.TotalAmount) ?? 0.0);
        // cashRefunds is already negative; we want to subtract it (i.e. add the absolute)
        var totalCashRefunds = Math.Abs(cashRefunds);

        // ── Debt Collections (cash collected from customers paying off debt) ──
        CurrentShiftDebtCollections = (decimal)(await ctx.Sales
            .AsNoTracking()
            .Where(s => s.ShiftId == shift.Id
                     && s.Status == SaleStatus.Completed
                     && !s.IsDeleted
                     && s.InvoiceNumber.StartsWith(SaleRecordKinds.DebtPaymentPrefix))
            .SumAsync(s => (double?)s.TotalAmount) ?? 0.0);

        // Expected drawer = Opening + Cash Sales + Debt Collections - Cash Refunds - Cash Expenses
        ExpectedBalance = shift.OpeningBalance + CurrentShiftSales + CurrentShiftDebtCollections - totalCashRefunds - CurrentShiftExpenses;
    }

    // --- Commands ---
    [RelayCommand]
    private async Task LoadShiftsAsync() => await ExecuteBusyAsync(LoadShiftsCoreAsync, "جاري تحديث الورديات...");

    partial void OnSearchTextChanged(string value) => FilterShifts();

    private void FilterShifts()
    {
        var query = _allShifts.AsEnumerable();

        if (!string.IsNullOrWhiteSpace(SearchText))
        {
            query = query.Where(s =>
                s.User.FullName.Contains(SearchText, StringComparison.OrdinalIgnoreCase) ||
                s.Id.ToString() == SearchText);
        }

        Shifts.SyncWith(query);
    }

    [RelayCommand]
    private async Task OpenShiftAsync()
    {
        if (IsShiftOpen)
        {
            _notificationService.ShowInfo("هناك وردية مفتوحة بالفعل");
            return;
        }

        if (OpeningBalanceInput < 0)
        {
            _notificationService.ShowWarning("رصيد الافتتاح غير صالح");
            return;
        }

        await ExecuteBusyAsync(async () =>
        {
            if (await _shiftRepository.HasActiveShiftAsync(_currentUser.Id))
            {
                _notificationService.ShowInfo("هناك وردية مفتوحة بالفعل");
                await LoadShiftsCoreAsync();
                return;
            }

            var shift = new Shift
            {
                StartTime = DateTime.Now,
                OpeningBalance = OpeningBalanceInput,
                Notes = NotesInput,
                UserId = _currentUser.Id,
                Status = ShiftStatus.Open,
                CreatedAt = DateTime.Now
            };

            await _shiftRepository.AddAsync(shift);

            OpeningBalanceInput = 0;
            NotesInput = string.Empty;

            await LoadShiftsCoreAsync();
            _notificationService.ShowSuccess("تم فتح الوردية بنجاح");

        }, "جاري فتح الوردية...");
    }

    [RelayCommand]
    private async Task CloseShiftAsync()
    {
        if (CurrentShift == null)
        {
            _notificationService.ShowInfo("لا توجد وردية مفتوحة");
            return;
        }

        if (ClosingBalanceInput < 0)
        {
            _notificationService.ShowWarning("رصيد الإغلاق غير صالح");
            return;
        }

        // Calculate blind difference before closing
        await LoadCurrentShiftStatsAsync(CurrentShift);
        var blindDifference = ClosingBalanceInput - ExpectedBalance;

        if (blindDifference != 0)
        {
            if (IsCashier)
            {
                var authorized = await _authorizationService.RequestAdminOverrideAsync($"إغلاق وردية بفارق مالي قدره: {Math.Abs(blindDifference):N2} ج.م");
                if (!authorized)
                {
                    _notificationService.ShowWarning("تم إلغاء عملية الإغلاق. يجب مصادقة المدير لإغلاق وردية بها عجز أو زيادة.");
                    return;
                }
            }
            else
            {
                if (string.IsNullOrWhiteSpace(NotesInput))
                {
                    _notificationService.ShowWarning("الرصيد الفعلي لا يطابق رصيد النظام. يرجى إدخال المبرر في حقل الملاحظات قبل الإغلاق.");
                    return;
                }
            }
        }

        await ExecuteBusyAsync(async () =>
        {
            await _shiftRepository.CloseShiftAsync(CurrentShift.Id, ClosingBalanceInput, NotesInput ?? string.Empty);

            var closedShift = await _shiftRepository.GetByIdAsync(CurrentShift.Id);
            if (closedShift != null)
            {
                await PrintZReportAsync(closedShift, blindDifference);
            }

            // Show difference to admin only
            if (blindDifference != 0 && IsAdminOrAbove)
            {
                var diffLabel = blindDifference > 0 ? "زيادة" : "عجز";
                _notificationService.ShowWarning($"فرق الوردية: {diffLabel} بمبلغ {Math.Abs(blindDifference):N2} ج.م");
            }

            ClosingBalanceInput = 0;
            NotesInput = string.Empty;

            await LoadShiftsCoreAsync();
            _notificationService.ShowSuccess("تم إغلاق الوردية بنجاح");

        }, "جاري إغلاق الوردية...");
    }

    // --- Print Helpers ---
    private async Task PrintZReportAsync(Shift shift, decimal blindDifference = 0)
    {
        try
        {
            await using var ctx = await _contextFactory.CreateDbContextAsync();
            var shiftEnd = shift.EndTime ?? DateTime.Now;

            var zSales = await ctx.Sales
                .AsNoTracking()
                .Where(s => s.ShiftId == shift.Id
                          && (s.Status == SaleStatus.Completed || s.Status == SaleStatus.Refunded)
                          && !s.IsDeleted
                          && !s.InvoiceNumber.StartsWith(SaleRecordKinds.DebtPaymentPrefix))
                .ToListAsync();

            // Net amounts per payment method (refunds are negative TotalAmount, so Sum handles them naturally)
            var totalCash = zSales.Where(s => s.PaymentMethod == PaymentMethod.Cash).Sum(s => s.TotalAmount);
            var totalCard = zSales.Where(s => s.PaymentMethod == PaymentMethod.Card).Sum(s => s.TotalAmount);
            var totalVodafoneCash = zSales.Where(s => s.PaymentMethod == PaymentMethod.VodafoneCash).Sum(s => s.TotalAmount);
            var totalInstaPay = zSales.Where(s => s.PaymentMethod == PaymentMethod.InstaPay).Sum(s => s.TotalAmount);
            var totalDeferred = zSales.Where(s => s.PaymentMethod == PaymentMethod.Deferred).Sum(s => s.TotalAmount);
            var totalSales = zSales.Sum(s => s.TotalAmount);

            var zDebtCollections = await ctx.Sales
                .AsNoTracking()
                .Where(s => s.ShiftId == shift.Id
                          && s.Status == SaleStatus.Completed
                          && !s.IsDeleted
                          && s.InvoiceNumber.StartsWith(SaleRecordKinds.DebtPaymentPrefix))
                .SumAsync(s => (double?)s.TotalAmount) ?? 0.0;
            
            // Add debt collections to total cash because it is physically in the drawer
            totalCash += (decimal)zDebtCollections;

            var zExpenses = await ctx.Expenses
                .AsNoTracking()
                .Where(e => e.UserId == shift.UserId && e.ExpenseDate >= shift.StartTime && e.ExpenseDate <= shiftEnd && !e.IsDeleted)
                .ToListAsync();
            var totalExpenses = zExpenses.Sum(e => e.Amount);

            var zDetails = await ctx.SaleDetails
                .AsNoTracking()
                .Where(sd => sd.Sale.ShiftId == shift.Id
                          && (sd.Sale.Status == SaleStatus.Completed || sd.Sale.Status == SaleStatus.Refunded)
                          && !sd.IsDeleted)
                .ToListAsync();
            var grossProfit = zDetails.Sum(sd => (sd.UnitPrice - sd.UnitCost) * sd.Quantity);

            var reportData = new ZReportData
            {
                ReportDate = DateTime.Now,
                CashierName = _currentUser.FullName,
                TotalTransactions = zSales.Count,
                TotalSales = totalSales,
                TotalCash = totalCash,
                TotalCard = totalCard,
                TotalVodafoneCash = totalVodafoneCash,
                TotalInstaPay = totalInstaPay,
                TotalDeferred = totalDeferred,
                TotalExpenses = totalExpenses,
                NetProfit = grossProfit - totalExpenses,
                OpeningBalance = shift.OpeningBalance,
                ClosingBalance = shift.ClosingBalance ?? 0,
                BlindDifference = blindDifference
            };

            // 1. Thermal print
            if (_settingsService.PrintZReportOnClose)
            {
                var printerName = ResolvePrinterName();
                if (!string.IsNullOrWhiteSpace(printerName))
                {
                    var ok = await _printingService.PrintZReportAsync(
                        printerName, reportData,
                        _settingsService.ReceiptWidth,
                        _settingsService.ReceiptLanguage);
                    if (!ok) _notificationService.ShowWarning("فشل الطباعة الحرارية لتقرير Z.");
                }
                else
                {
                    _notificationService.ShowWarning("لا توجد طابعة حرارية لطباعة تقرير Z.");
                }
            }

            // 2. PDF auto-save to Desktop/تقارير الورديات
            if (_settingsService.SaveZReportPdfOnClose)
            {
                try
                {
                    var folder = Path.Combine(
                        Environment.GetFolderPath(Environment.SpecialFolder.Desktop),
                        "تقارير الورديات");
                    Directory.CreateDirectory(folder);
                    var pdfPath = Path.Combine(folder,
                        $"وردية-{_currentUser.FullName}-{reportData.ReportDate:yyyyMMdd-HHmm}.pdf");
                    await File.WriteAllBytesAsync(pdfPath, GenerateZReportPdf(reportData));
                    _notificationService.ShowSuccess($"تم حفظ تقرير Z كـ PDF:\n{pdfPath}");
                }
                catch (Exception pdfEx)
                {
                    _notificationService.ShowWarning($"تعذّر حفظ PDF: {pdfEx.Message}");
                }
            }
        }
        catch (Exception ex)
        {
            _notificationService.ShowWarning($"تم إغلاق الوردية لكن فشلت طباعة التقرير: {ex.Message}");
        }
    }

    private static byte[] GenerateZReportPdf(ZReportData r)
    {
        return Document.Create(c => c.Page(page =>
        {
            page.Size(QuestPDF.Helpers.PageSizes.A5);
            page.Margin(1.5f, QuestPDF.Infrastructure.Unit.Centimetre);
            page.DefaultTextStyle(x => x.FontFamily("Segoe UI").FontSize(11).DirectionFromRightToLeft());

            page.Header().Background("#1E3A5F").Padding(12).Row(row =>
            {
                row.RelativeItem().Column(col =>
                {
                    col.Item().Text("تقرير إغلاق الوردية (Z-Report)").Bold().FontSize(15).FontColor(QuestPDF.Helpers.Colors.White);
                    col.Item().Text($"التاريخ: {r.ReportDate:dd/MM/yyyy HH:mm}").FontSize(9).FontColor("#CBD5E1");
                });
            });

            page.Content().PaddingVertical(10).Column(col =>
            {
                col.Spacing(6);
                col.Item().Row(row =>
                {
                    row.RelativeItem().Text($"الكاشير: {r.CashierName}").Bold();
                    row.RelativeItem().AlignRight().Text($"المعاملات: {r.TotalTransactions}");
                });
                col.Item().Background("#F1F5F9").Padding(8).Column(inner =>
                {
                    inner.Item().Text("المبيعات").Bold().FontColor("#1E3A5F");
                    inner.Spacing(3);
                    void Ln(string l, string v) => inner.Item().Row(r2 => { r2.RelativeItem().Text(l); r2.ConstantItem(110).AlignRight().Text(v); });
                    Ln("إجمالي المبيعات", $"{r.TotalSales:N2}");
                    Ln("نقداً", $"{r.TotalCash:N2}");
                    Ln("بطاقة", $"{r.TotalCard:N2}");
                    Ln("المصروفات", $"{r.TotalExpenses:N2}");
                });
                col.Item().Background("#1E3A5F").Padding(10).Row(row =>
                {
                    row.RelativeItem().Text("صافي الربح").Bold().FontSize(13).FontColor(QuestPDF.Helpers.Colors.White);
                    row.ConstantItem(130).AlignRight().Text($"{r.NetProfit:N2} ج.م").Bold().FontSize(13).FontColor(QuestPDF.Helpers.Colors.White);
                });
                if (r.ClosingBalance > 0)
                {
                    col.Item().Background("#F8FAFC").Padding(8).Column(inner =>
                    {
                        inner.Item().Text("الرصيد").Bold().FontColor("#1E3A5F");
                        inner.Spacing(3);
                        void Ln(string l, string v) => inner.Item().Row(r2 => { r2.RelativeItem().Text(l); r2.ConstantItem(110).AlignRight().Text(v); });
                        Ln("رصيد الافتتاح", $"{r.OpeningBalance:N2}");
                        Ln("رصيد الإغلاق", $"{r.ClosingBalance:N2}");
                        if (r.BlindDifference != 0)
                        {
                            var cl = r.BlindDifference > 0 ? "#16A34A" : "#DC2626";
                            var lb = r.BlindDifference > 0 ? "زيادة" : "عجز";
                            inner.Item().Row(r2 =>
                            {
                                r2.RelativeItem().Text(lb).Bold().FontColor(cl);
                                r2.ConstantItem(110).AlignRight().Text($"{Math.Abs(r.BlindDifference):N2}").Bold().FontColor(cl);
                            });
                        }
                    });
                }
            });
            page.Footer().AlignCenter().Text($"طُبع: {DateTime.Now:dd/MM/yyyy HH:mm:ss}").FontSize(8).FontColor("#9CA3AF");
        })).GeneratePdf();
    }



    private static bool IsVirtualPrinter(string printerName)
    {
        var name = printerName.ToLowerInvariant();
        return name.Contains("onenote")
               || name.Contains("microsoft print to pdf")
               || name.Contains("print to pdf")
               || name.Contains("xps")
               || name.Contains("fax");
    }

    private string? ResolvePrinterName()
    {
        var printers = _printingService.GetAvailablePrinters();
        if (printers.Count == 0) return null;

        // 1. Preferred printer from settings (skip if virtual)
        var preferred = _settingsService.PrinterName;
        if (!string.IsNullOrWhiteSpace(preferred) && !IsVirtualPrinter(preferred))
        {
            var match = printers.FirstOrDefault(p => string.Equals(p, preferred, StringComparison.OrdinalIgnoreCase));
            if (!string.IsNullOrWhiteSpace(match)) return match;
        }

        // 2. System default (skip if virtual)
        try
        {
            var defaultPrinter = new PrinterSettings().PrinterName;
            if (!string.IsNullOrWhiteSpace(defaultPrinter) && !IsVirtualPrinter(defaultPrinter))
            {
                var match = printers.FirstOrDefault(p => string.Equals(p, defaultPrinter, StringComparison.OrdinalIgnoreCase));
                if (!string.IsNullOrWhiteSpace(match)) return match;
            }
        }
        catch { /* ignore */ }

        // 3. First physical (non-virtual) printer
        var nonVirtual = printers.FirstOrDefault(p => !IsVirtualPrinter(p));
        if (!string.IsNullOrWhiteSpace(nonVirtual)) return nonVirtual;

        // 4. No physical printer — return null
        return null;
    }
}
