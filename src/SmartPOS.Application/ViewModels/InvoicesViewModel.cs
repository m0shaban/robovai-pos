using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.Messaging;
using System.Drawing.Printing;
using System.IO;
using System.Windows;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.EntityFrameworkCore;
using Microsoft.Win32;
using QuestPDF.Fluent;
using SmartPOS.Application.DTOs;
using SmartPOS.Application.Extensions;
using SmartPOS.Application.Utilities;
using SmartPOS.Core.Entities;
using SmartPOS.Core.Interfaces;
using SmartPOS.Infrastructure.Data;
using SmartPOS.Infrastructure.Services;

namespace SmartPOS.Application.ViewModels;

/// <summary>
/// Refactored to use IDbContextFactory for v5.1
/// </summary>
public partial class InvoicesViewModel : BaseViewModel, IDisposable, CommunityToolkit.Mvvm.Messaging.IRecipient<SmartPOS.Application.Messages.BarcodeScannedMessage>
{
    private readonly IDbContextFactory<AppDbContext> _contextFactory;
    private readonly IPrintingService _printingService;
    private readonly ISettingsService _settingsService;
    private readonly IReportService _reportService;
    private readonly IAuthorizationService _authService;
    private readonly User _currentUser;

    [ObservableProperty]
    private ObservableCollection<Sale> _sales = new();

    [ObservableProperty]
    private Sale? _selectedSale;

    [ObservableProperty]
    private DateTime _startDate = DateTime.Now.Date.AddDays(-7);

    [ObservableProperty]
    private DateTime _endDate = DateTime.Now.Date.AddDays(1).AddSeconds(-1);

    public InvoicesViewModel(IDbContextFactory<AppDbContext> contextFactory, IPrintingService printingService, ISettingsService settingsService, IReportService reportService, IAuthorizationService authService, User currentUser)
    {
        _contextFactory = contextFactory;
        _printingService = printingService;
        _settingsService = settingsService;
        _reportService = reportService;
        _authService = authService;
        _currentUser = currentUser;

        CommunityToolkit.Mvvm.Messaging.WeakReferenceMessenger.Default.RegisterAll(this);

        _ = InitializeAsync();
    }

    public void Dispose()
    {
        CommunityToolkit.Mvvm.Messaging.WeakReferenceMessenger.Default.UnregisterAll(this);
    }

    public void Receive(SmartPOS.Application.Messages.BarcodeScannedMessage message)
    {
        System.Windows.Application.Current.Dispatcher.InvokeAsync(() =>
        {
            var scanned = message.Value;
            var foundSale = Sales.FirstOrDefault(s => s.InvoiceNumber == scanned);
            if (foundSale != null)
            {
                SelectedSale = foundSale;
            }
        });
    }

    private async Task InitializeAsync()
    {
        await ExecuteBusyAsync(LoadSalesCoreAsync, "⏳ جاري تحميل الفواتير...", $"✅ تم تحميل {Sales.Count} فاتورة");
    }

    private async Task LoadSalesCoreAsync()
    {
        await using var ctx = await _contextFactory.CreateDbContextAsync();
        var sales = await ctx.Sales
            .AsNoTracking()
            .Include(s => s.User)
            .Include(s => s.Customer)
            .Where(s => !s.InvoiceNumber.StartsWith(SaleRecordKinds.DebtPaymentPrefix))
            .OrderByDescending(s => s.SaleDate)
            .Take(200)
            .ToListAsync();

        Sales.SyncWith(sales);
    }

    [RelayCommand]
    private async Task LoadSalesAsync()
    {
        await ExecuteBusyAsync(LoadSalesCoreAsync, "جاري تحديث الفواتير...");
    }

    [RelayCommand]
    private async Task FilterAsync()
    {
        await ExecuteBusyAsync(async () =>
        {
            await using var ctx = await _contextFactory.CreateDbContextAsync();
            var sales = await ctx.Sales
                .AsNoTracking()
                .Include(s => s.User)
                .Include(s => s.Customer)
                .Where(s => s.SaleDate >= StartDate
                         && s.SaleDate <= EndDate
                         && !s.InvoiceNumber.StartsWith(SaleRecordKinds.DebtPaymentPrefix))
                .OrderByDescending(s => s.SaleDate)
                .ToListAsync();

            Sales.SyncWith(sales);
        }, "جاري الفلترة...");
    }

    [RelayCommand]
    private async Task PrintInvoiceAsync(Sale? sale)
    {
        if (sale == null) return;

        await ExecuteBusyAsync(async () =>
        {
            await using var ctx = await _contextFactory.CreateDbContextAsync();
            var saleWithDetails = await ctx.Sales
                .Include(s => s.SaleDetails)
                .ThenInclude(sd => sd.Product)
                .Include(s => s.User)
                .Include(s => s.Customer)
                .FirstOrDefaultAsync(s => s.Id == sale.Id);

            if (saleWithDetails == null) return;

            var receiptData = new ReceiptData
            {
                StoreName = _settingsService.StoreName ?? "Smart POS",
                StoreAddress = _settingsService.StoreAddress ?? "",
                Phone = _settingsService.StorePhone ?? "",
                InvoiceNumber = saleWithDetails.InvoiceNumber,
                SaleDate = saleWithDetails.SaleDate,
                CashierName = saleWithDetails.User?.FullName ?? "",
                Items = saleWithDetails.SaleDetails.Select(i => new ReceiptItem
                {
                    Name = i.Product?.Name ?? i.ProductId.ToString(),
                    Quantity = i.Quantity,
                    UnitPrice = i.UnitPrice,
                    Total = i.LineTotal
                }).ToList(),
                Subtotal = saleWithDetails.Subtotal,
                DiscountAmount = saleWithDetails.DiscountAmount,
                TaxAmount = saleWithDetails.TaxAmount,
                TotalAmount = saleWithDetails.TotalAmount,
                AmountPaid = saleWithDetails.AmountPaid,
                ChangeAmount = saleWithDetails.ChangeAmount,
                PaymentMethod = saleWithDetails.PaymentMethod.ToString(),
                Footer = _settingsService.FooterMessage ?? "شكراً لزيارتكم"
            };

            var printerName = ResolvePrinterName();

            if (string.IsNullOrEmpty(printerName))
            {
                MessageBox.Show("لا توجد طابعة متاحة", "تنبيه", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            await _printingService.PrintReceiptAsync(printerName, receiptData);
        }, "جاري الطباعة...");
    }

    [RelayCommand]
    private async Task ExportInvoicePdfAsync(Sale? sale)
    {
        if (sale == null) return;

        try
        {
            await using var ctx = await _contextFactory.CreateDbContextAsync();
            var saleWithDetails = await ctx.Sales
                .AsNoTracking()
                .Include(s => s.SaleDetails)
                .ThenInclude(sd => sd.Product)
                .Include(s => s.User)
                .Include(s => s.Customer)
                .FirstOrDefaultAsync(s => s.Id == sale.Id);

            if (saleWithDetails == null) return;

            var model = new PosReceiptModel(
                StoreName: _settingsService.StoreName ?? "SmartPOS",
                StoreAddress: _settingsService.StoreAddress ?? "",
                StorePhone: _settingsService.StorePhone ?? "",
                InvoiceNumber: saleWithDetails.InvoiceNumber,
                SaleDate: saleWithDetails.SaleDate,
                CashierName: saleWithDetails.User?.FullName ?? "",
                CustomerName: saleWithDetails.Customer?.Name ?? "",
                PaymentMethod: TranslatePaymentMethod(saleWithDetails.PaymentMethod),
                Items: saleWithDetails.SaleDetails.Select(d => new PosReceiptItem(
                                    d.Product?.Name ?? d.ProductId.ToString(),
                                    d.Quantity, d.UnitPrice, d.LineTotal)).ToList(),
                Subtotal: saleWithDetails.Subtotal,
                DiscountAmount: saleWithDetails.DiscountAmount,
                TaxAmount: saleWithDetails.TaxAmount,
                TotalAmount: saleWithDetails.TotalAmount,
                AmountPaid: saleWithDetails.AmountPaid,
                ChangeAmount: saleWithDetails.ChangeAmount,
                FooterMessage: _settingsService.FooterMessage ?? "شكراً لزيارتكم"
            );

            var pdf = ((ReportService)_reportService).GeneratePosReceiptPdf(model);

            var defaultFolder = _settingsService?.DefaultExportFolder;
            if (!string.IsNullOrWhiteSpace(defaultFolder) && !System.IO.Directory.Exists(defaultFolder))
            {
                try { System.IO.Directory.CreateDirectory(defaultFolder); } catch { }
            }

            var dlg = new SaveFileDialog
            {
                Title = "حفظ الفاتورة كـ PDF",
                FileName = $"فاتورة-{saleWithDetails.InvoiceNumber}.pdf",
                InitialDirectory = !string.IsNullOrWhiteSpace(defaultFolder) && System.IO.Directory.Exists(defaultFolder)
                    ? defaultFolder
                    : Environment.GetFolderPath(Environment.SpecialFolder.Desktop),
                DefaultExt = ".pdf",
                Filter = "PDF Files (*.pdf)|*.pdf"
            };

            if (dlg.ShowDialog() == true)
            {
                await File.WriteAllBytesAsync(dlg.FileName, pdf);

                if (_settingsService?.AutoOpenExportedFile == true)
                {
                    try { System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo(dlg.FileName) { UseShellExecute = true }); } catch { }
                }

                MessageBox.Show($"✅ تم حفظ الفاتورة بنجاح:\n{dlg.FileName}", "تم الحفظ", MessageBoxButton.OK, MessageBoxImage.Information);
            }
        }
        catch (Exception ex)
        {
            MessageBox.Show($"خطأ في تصدير الفاتورة: {ex.Message}", "خطأ", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    [RelayCommand]
    private async Task VoidSaleAsync(Sale? sale)
    {
        if (sale == null) return;

        bool authorized = await _authService.RequestAdminOverrideAsync("إلغاء فاتورة / Void Invoice");
        if (!authorized) return;

        var confirmMsg = string.Format(SmartPOS.Core.Localization.Loc.Tr("Loc_Inv_VoidConfirm", "هل تريد إلغاء الفاتورة {0}؟\nسيتم استعادة المخزون تلقائياً."), sale.InvoiceNumber);
        var confirmTitle = SmartPOS.Core.Localization.Loc.Tr("Loc_Inv_VoidTitle", "تأكيد الإلغاء");
        if (MessageBox.Show(confirmMsg, confirmTitle, MessageBoxButton.YesNo, MessageBoxImage.Warning) != MessageBoxResult.Yes) return;

        await ExecuteBusyAsync(async () =>
        {
            await using var ctx = await _contextFactory.CreateDbContextAsync();
            var entity = await ctx.Sales
                .Include(s => s.SaleDetails)
                .FirstOrDefaultAsync(s => s.Id == sale.Id);

            if (entity == null) return;

            // --- SHIFT AUDIT FIX ---
            if (entity.ShiftId.HasValue)
            {
                var shift = await ctx.Shifts.FindAsync(entity.ShiftId.Value);
                if (shift != null && shift.Status == ShiftStatus.Closed)
                {
                    var closedShiftErr = SmartPOS.Core.Localization.Loc.Tr("Loc_Inv_ClosedShiftError", "لا يمكن إلغاء (Void) فاتورة تابعة لوردية مغلقة لأن ذلك سيؤدي إلى تدمير التقارير التاريخية.\n\nيرجى استخدام شاشة 'المرتجعات' لعمل مرتجع لهذه الفاتورة وتسوية حساباتها في الوردية الحالية.");
                    var errHeader = SmartPOS.Core.Localization.Loc.Tr("Loc_Inv_AccountingError", "خطأ محاسبي");
                    MessageBox.Show(closedShiftErr, errHeader, MessageBoxButton.OK, MessageBoxImage.Error);
                    return;
                }
            }
            // -----------------------

            entity.IsDeleted = true;
            entity.Status = SaleStatus.Cancelled;

            // If this was a deferred (آجل) sale, reduce customer debt
            if (entity.PaymentMethod == PaymentMethod.Deferred && entity.CustomerId.HasValue)
            {
                var customer = await ctx.Customers.FindAsync(entity.CustomerId.Value);
                if (customer != null)
                {
                    // Allow debt to go negative (credit) if they already paid this invoice in advance or earlier in the shift
                    customer.CurrentDebt -= entity.TotalAmount;
                }
            }

            // Restore stock
            foreach (var detail in entity.SaleDetails)
            {
                var product = await ctx.Products.FindAsync(detail.ProductId);
                if (product != null)
                {
                    product.Stock += detail.Quantity;
                    ctx.StockMovements.Add(new StockMovement
                    {
                        ProductId = detail.ProductId,
                        Quantity = detail.Quantity,
                        Type = MovementType.Return,
                        Reference = $"VOID-{entity.InvoiceNumber}",
                        Notes = "إلغاء فاتورة",
                        MovementDate = DateTime.Now
                    });
                }
            }

            await ctx.SaveChangesAsync();
            await LoadSalesCoreAsync();
            var voidSuccessMsg = SmartPOS.Core.Localization.Loc.Tr("Loc_Inv_VoidSuccess", "تم إلغاء الفاتورة واستعادة المخزون بنجاح.");
            var voidDoneTitle = SmartPOS.Core.Localization.Loc.Tr("Loc_Inv_VoidDoneTitle", "تم الإلغاء");
            MessageBox.Show(voidSuccessMsg, voidDoneTitle, MessageBoxButton.OK, MessageBoxImage.Information);
        }, SmartPOS.Core.Localization.Loc.Tr("Loc_Inv_VoidingBusy", "جاري إلغاء الفاتورة..."));
    }

    // Reports that are also found in ReportsViewModel
    [RelayCommand]
    private async Task PrintPurchaseReportAsync()
    {
        try
        {
            await using var ctx = await _contextFactory.CreateDbContextAsync();
            var products = await ctx.Products.AsNoTracking().Include(p => p.Category).ToListAsync();
            var totalValuation = products.Sum(p => p.PurchasePrice * p.Stock);

            var doc = Document.Create(container =>
            {
                container.Page(page =>
                {
                    page.Size(QuestPDF.Helpers.PageSizes.A4);
                    page.Margin(2, QuestPDF.Infrastructure.Unit.Centimetre);
                    page.DefaultTextStyle(x => x.FontFamily("Segoe UI").FontSize(11).DirectionFromRightToLeft());

                    page.Header().Column(col =>
                    {
                        col.Item().Background("#1E3A5F").Padding(14).Row(row =>
                        {
                            row.RelativeItem().Column(c =>
                            {
                                c.Item().Text("كشف المشتريات والمخزون").Bold().FontSize(20).FontColor(QuestPDF.Helpers.Colors.White);
                                c.Item().Text($"بتاريخ: {DateTime.Now:dd/MM/yyyy HH:mm}").FontSize(10).FontColor("#CBD5E1");
                            });
                            row.AutoItem().AlignRight().AlignMiddle().Text("SmartPOS").Bold().FontSize(14).FontColor("#94A3B8");
                        });
                    });

                    page.Content().PaddingVertical(12).Column(col =>
                    {
                        col.Spacing(8);
                        col.Item().Table(table =>
                        {
                            table.ColumnsDefinition(c =>
                            {
                                c.ConstantColumn(35);
                                c.RelativeColumn(3);
                                c.RelativeColumn(2);
                                c.RelativeColumn();
                                c.RelativeColumn();
                                c.RelativeColumn();
                            });
                            table.Header(h =>
                            {
                                foreach (var t in new[] { "#", "المنتج", "التصنيف", "سعر الشراء", "الكمية", "الإجمالي" })
                                    h.Cell().Background("#1E3A5F").Padding(6).Text(t).Bold().FontSize(10).FontColor(QuestPDF.Helpers.Colors.White).AlignCenter();
                            });
                            int idx = 1;
                            foreach (var p in products)
                            {
                                var bg = idx % 2 == 0 ? "#F8FAFC" : "#FFFFFF";
                                table.Cell().Background(bg).Padding(4).Text(idx++.ToString()).AlignCenter().FontSize(10);
                                table.Cell().Background(bg).Padding(4).Text(p.Name).FontSize(10);
                                table.Cell().Background(bg).Padding(4).Text(p.Category?.Name ?? "-").FontSize(10);
                                table.Cell().Background(bg).Padding(4).Text($"{p.PurchasePrice:N2}").AlignCenter().FontSize(10);
                                table.Cell().Background(bg).Padding(4).Text(p.Stock.ToString()).AlignCenter().FontSize(10);
                                table.Cell().Background(bg).Padding(4).Text($"{(p.PurchasePrice * p.Stock):N2}").AlignRight().FontSize(10);
                            }
                        });

                        col.Item().PaddingTop(12).AlignRight().Text($"إجمالي قيمة المخزون: {totalValuation:N2} ج.م").Bold().FontSize(14).FontColor("#1E3A5F");
                    });

                    page.Footer().AlignCenter().Text($"طُبع في: {DateTime.Now:dd/MM/yyyy HH:mm:ss}").FontSize(8).FontColor("#9CA3AF");
                });
            });

            var pdfBytes = doc.GeneratePdf();

            var dlg = new SaveFileDialog
            {
                Title = "حفظ كشف المشتريات كـ PDF",
                FileName = $"كشف-المشتريات-{DateTime.Now:yyyyMMdd-HHmm}.pdf",
                DefaultExt = ".pdf",
                Filter = "PDF Files|*.pdf"
            };

            if (dlg.ShowDialog() == true)
            {
                await File.WriteAllBytesAsync(dlg.FileName, pdfBytes);
                MessageBox.Show($"تم حفظ كشف المشتريات:\n{dlg.FileName}", "تم الحفظ", MessageBoxButton.OK, MessageBoxImage.Information);
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
        try
        {
            await using var ctx = await _contextFactory.CreateDbContextAsync();
            var start = StartDate.Date;
            var endExclusive = EndDate.Date.AddDays(1);

            var sales = await ctx.Sales
                .Include(s => s.SaleDetails)
                .ThenInclude(sd => sd.Product)
                .Include(s => s.User)
                .Where(s => s.SaleDate >= start && s.SaleDate < endExclusive
                         && s.Status == SaleStatus.Completed
                         && !s.IsDeleted
                         && !s.InvoiceNumber.StartsWith(SaleRecordKinds.DebtPaymentPrefix))
                .OrderByDescending(s => s.SaleDate)
                .ToListAsync();

            var totalSales = sales.Sum(s => s.TotalAmount);
            var totalProfit = sales.Sum(s => s.SaleDetails.Sum(sd => (sd.UnitPrice - (sd.Product?.PurchasePrice ?? 0)) * sd.Quantity));
            var expensesList = await ctx.Expenses
                .Where(e => e.ExpenseDate >= start && e.ExpenseDate < endExclusive && !e.IsDeleted)
                .ToListAsync();
            var totalExpenses = expensesList.Sum(e => e.Amount);

            var doc = Document.Create(container =>
            {
                container.Page(page =>
                {
                    page.Size(QuestPDF.Helpers.PageSizes.A4);
                    page.Margin(2, QuestPDF.Infrastructure.Unit.Centimetre);
                    page.DefaultTextStyle(x => x.FontFamily("Segoe UI").FontSize(11).DirectionFromRightToLeft());

                    page.Header().Column(col =>
                    {
                        col.Item().Background("#059669").Padding(14).Row(row =>
                        {
                            row.RelativeItem().Column(c =>
                            {
                                c.Item().Text("كشف الإيرادات").Bold().FontSize(20).FontColor(QuestPDF.Helpers.Colors.White);
                                c.Item().Text($"من {start:dd/MM/yyyy} إلى {EndDate:dd/MM/yyyy}").FontSize(10).FontColor("#A7F3D0");
                            });
                            row.AutoItem().AlignRight().AlignMiddle().Text("SmartPOS").Bold().FontSize(14).FontColor("#6EE7B7");
                        });
                    });

                    page.Content().PaddingVertical(12).Column(col =>
                    {
                        col.Spacing(10);
                        col.Item().Row(row =>
                        {
                            row.RelativeItem().Border(1).BorderColor("#E5E7EB").Padding(10).Column(c =>
                            {
                                c.Item().Text("إجمالي المبيعات").FontSize(10).FontColor("#6B7280");
                                c.Item().Text($"{totalSales:N2} ج.م").Bold().FontSize(14).FontColor("#059669");
                            });
                            row.ConstantItem(12);
                            row.RelativeItem().Border(1).BorderColor("#E5E7EB").Padding(10).Column(c =>
                            {
                                c.Item().Text("صافي الأرباح").FontSize(10).FontColor("#6B7280");
                                c.Item().Text($"{totalProfit:N2} ج.م").Bold().FontSize(14).FontColor("#2563EB");
                            });
                            row.ConstantItem(12);
                            row.RelativeItem().Border(1).BorderColor("#E5E7EB").Padding(10).Column(c =>
                            {
                                c.Item().Text("المصروفات").FontSize(10).FontColor("#6B7280");
                                c.Item().Text($"{totalExpenses:N2} ج.م").Bold().FontSize(14).FontColor("#DC2626");
                            });
                        });

                        col.Item().Table(table =>
                        {
                            table.ColumnsDefinition(c =>
                            {
                                c.ConstantColumn(35);
                                c.RelativeColumn(2);
                                c.RelativeColumn(2);
                                c.RelativeColumn();
                                c.RelativeColumn();
                                c.RelativeColumn();
                            });
                            table.Header(h =>
                            {
                                foreach (var t in new[] { "#", "رقم الفاتورة", "التاريخ", "الكاشير", "الدفع", "الإجمالي" })
                                    h.Cell().Background("#059669").Padding(6).Text(t).Bold().FontSize(10).FontColor(QuestPDF.Helpers.Colors.White).AlignCenter();
                            });
                            int idx = 1;
                            foreach (var s in sales)
                            {
                                var bg = idx % 2 == 0 ? "#F0FDF4" : "#FFFFFF";
                                table.Cell().Background(bg).Padding(4).Text(idx++.ToString()).AlignCenter().FontSize(10);
                                table.Cell().Background(bg).Padding(4).Text(s.InvoiceNumber).FontSize(10);
                                table.Cell().Background(bg).Padding(4).Text(s.SaleDate.ToString("dd/MM/yyyy HH:mm")).FontSize(10);
                                table.Cell().Background(bg).Padding(4).Text(s.User?.FullName ?? "-").FontSize(10);
                                table.Cell().Background(bg).Padding(4).Text(TranslatePaymentMethod(s.PaymentMethod)).FontSize(10);
                                table.Cell().Background(bg).Padding(4).Text($"{s.TotalAmount:N2}").AlignRight().FontSize(10);
                            }
                        });

                        col.Item().PaddingTop(8).Text($"عدد الفواتير: {sales.Count}").FontSize(11);
                    });

                    page.Footer().AlignCenter().Text($"طُبع في: {DateTime.Now:dd/MM/yyyy HH:mm:ss}").FontSize(8).FontColor("#9CA3AF");
                });
            });

            var pdfBytes = doc.GeneratePdf();

            var dlg = new SaveFileDialog
            {
                Title = "حفظ كشف الإيرادات كـ PDF",
                FileName = $"كشف-الإيرادات-{DateTime.Now:yyyyMMdd-HHmm}.pdf",
                DefaultExt = ".pdf",
                Filter = "PDF Files|*.pdf"
            };

            if (dlg.ShowDialog() == true)
            {
                await File.WriteAllBytesAsync(dlg.FileName, pdfBytes);
                MessageBox.Show($"تم حفظ كشف الإيرادات:\n{dlg.FileName}", "تم الحفظ", MessageBoxButton.OK, MessageBoxImage.Information);
            }
        }
        catch (Exception ex)
        {
            MessageBox.Show($"خطأ في إنشاء التقرير: {ex.Message}", "خطأ", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private static bool IsVirtualPrinter(string printerName)
    {
        var name = printerName.ToLowerInvariant();
        return name.Contains("onenote") || name.Contains("microsoft print to pdf") || name.Contains("print to pdf") || name.Contains("xps") || name.Contains("fax");
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

    private static string TranslatePaymentMethod(PaymentMethod pm)
    {
        return pm switch
        {
            PaymentMethod.Cash => "كاش",
            PaymentMethod.Card => "فيزا / بطاقة",
            PaymentMethod.VodafoneCash => "فودافون كاش",
            PaymentMethod.InstaPay => "انستا باي",
            PaymentMethod.BankTransfer => "تحويل بنكي",
            PaymentMethod.MobileMoney => "محفظة إلكترونية",
            PaymentMethod.Split => "دفع مقسم",
            PaymentMethod.Deferred => "آجل",
            _ => pm.ToString()
        };
    }
}
