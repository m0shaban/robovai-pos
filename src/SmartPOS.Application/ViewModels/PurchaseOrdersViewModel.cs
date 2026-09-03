using System.Collections.ObjectModel;
using System.IO;
using System.Windows;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using Microsoft.EntityFrameworkCore;
using Microsoft.Win32;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;
using SmartPOS.Application.Extensions;
using SmartPOS.Core.Entities;
using SmartPOS.Infrastructure.Data;

namespace SmartPOS.Application.ViewModels;

public partial class PurchaseOrdersViewModel : BaseViewModel, IDisposable, CommunityToolkit.Mvvm.Messaging.IRecipient<SmartPOS.Application.Messages.BarcodeScannedMessage>
{
    /// <summary>
    /// Refactored to use IDbContextFactory for better lifecycle management (v5.1).
    /// </summary>
    private readonly IDbContextFactory<AppDbContext> _contextFactory;
    private readonly User _currentUser;
    private readonly SmartPOS.Core.Interfaces.IAuthorizationService _authService;

    // ─── Tab Index ────────────────────────────────────────────────────────────
    [ObservableProperty]
    private int _selectedTabIndex = 0;

    // ─── New Order Form ───────────────────────────────────────────────────────
    [ObservableProperty]
    private ObservableCollection<Supplier> _suppliers = new();

    [ObservableProperty]
    private Supplier? _selectedSupplier;

    [ObservableProperty]
    private ObservableCollection<Product> _products = new();

    [ObservableProperty]
    private Product? _selectedProduct;

    [ObservableProperty]
    private ObservableCollection<PurchaseOrderDetail> _orderItems = new();

    [ObservableProperty]
    private PurchaseOrderDetail? _selectedOrderItem;

    [ObservableProperty]
    private string _invoiceNumber = string.Empty;

    [ObservableProperty]
    private DateTime _orderDate = DateTime.Now;

    [ObservableProperty]
    private decimal _totalAmount;

    [ObservableProperty]
    private decimal _paidAmount;

    [ObservableProperty]
    private decimal _remainingAmount;

    // Temporary item entry fields
    [ObservableProperty]
    private int _quantity = 1;

    [ObservableProperty]
    private decimal _costPrice;

    // ─── Purchase History ─────────────────────────────────────────────────────
    [ObservableProperty]
    private ObservableCollection<PurchaseOrder> _purchaseHistory = new();

    [ObservableProperty]
    private PurchaseOrder? _selectedHistoryOrder;

    [ObservableProperty]
    private DateTime _historyStartDate = DateTime.Now.AddDays(-30);

    [ObservableProperty]
    private DateTime _historyEndDate = DateTime.Now;

    [ObservableProperty]
    private Supplier? _historyFilterSupplier;

    [ObservableProperty]
    private string _historySearchText = string.Empty;

    // ─── Supplier Debts ───────────────────────────────────────────────────────
    [ObservableProperty]
    private ObservableCollection<Supplier> _suppliersWithDebt = new();

    [ObservableProperty]
    private Supplier? _selectedDebtSupplier;

    [ObservableProperty]
    private decimal _paymentAmount;

    [ObservableProperty]
    private string _paymentNotes = string.Empty;

    [ObservableProperty]
    private ObservableCollection<SupplierPayment> _supplierPaymentHistory = new();

    // ─── Low Stock Alerts ─────────────────────────────────────────────────────
    [ObservableProperty]
    private ObservableCollection<Product> _lowStockProducts = new();

    // ─── Auth & Role ──────────────────────────────────────────────────────────
    public bool IsAdmin => _currentUser.Role == UserRole.Admin || _currentUser.Role == UserRole.Manager || _currentUser.Role == UserRole.SuperAdmin;

    // ─── Constructor ──────────────────────────────────────────────────────────
    private readonly SmartPOS.Core.Interfaces.ISettingsService? _settingsService;

    public PurchaseOrdersViewModel(IDbContextFactory<AppDbContext> contextFactory, User currentUser, SmartPOS.Core.Interfaces.IAuthorizationService authService, SmartPOS.Core.Interfaces.ISettingsService? settingsService = null)
    {
        _contextFactory = contextFactory;
        _currentUser = currentUser;
        _authService = authService;
        _settingsService = settingsService;

        CommunityToolkit.Mvvm.Messaging.WeakReferenceMessenger.Default.RegisterAll(this);

        GenerateInvoiceNumber();
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
            if (string.IsNullOrWhiteSpace(message.Value)) return;
            var product = Products.FirstOrDefault(p => p.Barcode == message.Value || p.Barcode == message.Value.TrimStart('0'));
            if (product != null)
            {
                SelectedProduct = product;
                CostPrice = product.PurchasePrice;
                Quantity = 1;
            }
            else
            {
                MessageBox.Show($"المنتج صاحب الباركود [{message.Value}] غير موجود في قائمة المنتجات.", "بحث بالباركود", MessageBoxButton.OK, MessageBoxImage.Information);
            }
        });
    }

    private void GenerateInvoiceNumber()
        => InvoiceNumber = $"PO-{DateTime.Now:yyyyMMdd}-{DateTime.Now:HHmmss}";

    private async Task InitializeAsync()
        => await ExecuteBusyAsync(LoadAllDataAsync, "⏳ جاري تحميل البيانات...", "✅ تم تحميل البيانات");

    [RelayCommand]
    private async Task RefreshAllAsync()
        => await ExecuteBusyAsync(LoadAllDataAsync, "⏳ جاري تحديث البيانات...");

    private async Task LoadAllDataAsync()
    {
        await LoadSuppliersAndProductsAsync();
        await LoadPurchaseHistoryAsync();
        await LoadSupplierDebtsAsync();
        await LoadLowStockAsync();
    }

    // ─── Load Helpers ─────────────────────────────────────────────────────────
    private async Task LoadSuppliersAndProductsAsync()
    {
        await using var ctx = await _contextFactory.CreateDbContextAsync();
        var suppliers = await ctx.Suppliers
            .AsNoTracking()
            .Where(s => s.IsActive && !s.IsDeleted)
            .OrderBy(s => s.Name)
            .ToListAsync();
        Suppliers.SyncWith(suppliers);

        var products = await ctx.Products
            .AsNoTracking()
            .Include(p => p.Category)
            .Where(p => p.IsActive && !p.IsDeleted)
            .OrderBy(p => p.Name)
            .ToListAsync();
        Products.SyncWith(products);
    }

    private async Task LoadPurchaseHistoryAsync()
    {
        await using var ctx = await _contextFactory.CreateDbContextAsync();
        var start = HistoryStartDate.Date;
        var end = HistoryEndDate.Date.AddDays(1);

        var query = ctx.PurchaseOrders
            .AsNoTracking()
            .Include(o => o.Supplier)
            .Include(o => o.OrderDetails).ThenInclude(d => d.Product)
            .Where(o => !o.IsDeleted && o.OrderDate >= start && o.OrderDate < end);

        if (HistoryFilterSupplier != null)
            query = query.Where(o => o.SupplierId == HistoryFilterSupplier.Id);

        if (!string.IsNullOrWhiteSpace(HistorySearchText))
            query = query.Where(o => o.OrderNumber.Contains(HistorySearchText));

        var orders = await query.OrderByDescending(o => o.OrderDate).ToListAsync();
        PurchaseHistory.SyncWith(orders);
    }

    private async Task LoadSupplierDebtsAsync()
    {
        await using var ctx = await _contextFactory.CreateDbContextAsync();
        var debtors = await ctx.Suppliers
            .AsNoTracking()
            .Where(s => s.IsActive && !s.IsDeleted && s.DebtAmount > 0)
            .OrderByDescending(s => (double)s.DebtAmount)
            .ToListAsync();
        SuppliersWithDebt.SyncWith(debtors);

        if (SelectedDebtSupplier != null)
            await LoadPaymentHistoryForSupplierAsync(SelectedDebtSupplier.Id);
    }

    private async Task LoadPaymentHistoryForSupplierAsync(int supplierId)
    {
        await using var ctx = await _contextFactory.CreateDbContextAsync();
        var payments = await ctx.SupplierPayments
            .AsNoTracking()
            .Where(p => p.SupplierId == supplierId && !p.IsDeleted)
            .OrderByDescending(p => p.PaymentDate)
            .Take(50)
            .ToListAsync();
        SupplierPaymentHistory.SyncWith(payments);
    }

    private async Task LoadLowStockAsync()
    {
        await using var ctx = await _contextFactory.CreateDbContextAsync();
        var lowStock = await ctx.Products
            .AsNoTracking()
            .Include(p => p.Category)
            .Include(p => p.Supplier)
            .Where(p => p.IsActive && !p.IsDeleted && p.Stock <= p.MinStockLevel)
            .OrderBy(p => p.Stock)
            .ToListAsync();
        LowStockProducts.SyncWith(lowStock);
    }

    // ─── New Order Commands ───────────────────────────────────────────────────
    partial void OnSelectedProductChanged(Product? value)
    {
        if (value != null) CostPrice = value.PurchasePrice;
    }

    partial void OnPaidAmountChanged(decimal value) => CalculateTotals();

    [RelayCommand]
    private void AddItem()
    {
        if (SelectedProduct == null) return;
        if (Quantity <= 0)
        {
            MessageBox.Show("الكمية يجب أن تكون أكبر من صفر", "تنبيه", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }
        if (CostPrice <= 0)
        {
            MessageBox.Show("يرجى تحديد سعر الشراء", "تنبيه", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        var existingItem = OrderItems.FirstOrDefault(i => i.ProductId == SelectedProduct.Id);
        if (existingItem != null)
        {
            var index = OrderItems.IndexOf(existingItem);
            existingItem.Quantity += Quantity;
            existingItem.TotalCost = existingItem.Quantity * existingItem.UnitCost;
            OrderItems[index] = existingItem;
        }
        else
        {
            OrderItems.Add(new PurchaseOrderDetail
            {
                ProductId = SelectedProduct.Id,
                Product = SelectedProduct,
                Quantity = Quantity,
                UnitCost = CostPrice,
                TotalCost = Quantity * CostPrice
            });
        }
        CalculateTotals();
        Quantity = 1;
    }

    [RelayCommand]
    private void RemoveItem(PurchaseOrderDetail item)
    {
        OrderItems.Remove(item);
        CalculateTotals();
    }

    private void CalculateTotals()
    {
        TotalAmount = OrderItems.Sum(i => i.TotalCost);
        RemainingAmount = TotalAmount - PaidAmount;
    }

    [RelayCommand]
    private async Task SaveOrderAsync()
    {
        if (SelectedSupplier == null)
        {
            MessageBox.Show("الرجاء اختيار المورد", "تنبيه", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }
        if (OrderItems.Count == 0)
        {
            MessageBox.Show("الرجاء إضافة منتجات للفاتورة", "تنبيه", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        // Confirmation before saving
        var confirm = MessageBox.Show(
            $"تأكيد حفظ فاتورة الشراء\n\nالمورد: {SelectedSupplier.Name}\nعدد الأصناف: {OrderItems.Count}\nإجمالي: {TotalAmount:N2} ج.م\nمدفوع: {PaidAmount:N2} ج.م\nمتبقي (آجل): {RemainingAmount:N2} ج.م",
            "تأكيد الحفظ", MessageBoxButton.YesNo, MessageBoxImage.Question);
        if (confirm != MessageBoxResult.Yes) return;

        await ExecuteBusyAsync(async () =>
        {
            await using var ctx = await _contextFactory.CreateDbContextAsync();
            try
            {
                var order = new PurchaseOrder
                {
                    OrderNumber = InvoiceNumber,
                    OrderDate = OrderDate,
                    ReceivedDate = null,
                    TotalAmount = TotalAmount,
                    PaidAmount = PaidAmount,
                    Status = PurchaseOrderStatus.Pending,
                    SupplierId = SelectedSupplier.Id
                };
                ctx.PurchaseOrders.Add(order);
                await ctx.SaveChangesAsync();

                foreach (var item in OrderItems)
                {
                    ctx.PurchaseOrderDetails.Add(new PurchaseOrderDetail
                    {
                        PurchaseOrderId = order.Id,
                        ProductId = item.ProductId,
                        Quantity = item.Quantity,
                        UnitCost = item.UnitCost,
                        TotalCost = item.TotalCost
                    });

                    // Update purchase price (but do not increment stock yet)
                    var product = await ctx.Products.FindAsync(item.ProductId);
                    if (product != null)
                    {
                        product.PurchasePrice = item.UnitCost;
                    }
                }

                if (PaidAmount > 0)
                {
                    ctx.Expenses.Add(new Expense
                    {
                        Amount = PaidAmount,
                        ExpenseDate = DateTime.Now,
                        Description = $"دفعة مقدمة - فاتورة مشتريات رقم {InvoiceNumber}",
                        Notes = $"المورد: {SelectedSupplier.Name}",
                        Category = ExpenseCategory.Supplies,
                        UserId = _currentUser.Id
                    });
                }

                await ctx.SaveChangesAsync();
                MessageBox.Show("✅ تم حفظ الفاتورة بنجاح كقيد الانتظار.\n\nلم يتم تحديث المخزون أو ديون المورد حتى يتم الموافقة عليها.", "تم الحفظ", MessageBoxButton.OK, MessageBoxImage.Information);

                OrderItems.Clear();
                GenerateInvoiceNumber();
                PaidAmount = 0;
                CalculateTotals();
                await LoadAllDataAsync();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"خطأ في حفظ الفاتورة:\n{ex.InnerException?.Message ?? ex.Message}", "خطأ", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }, "جاري حفظ فاتورة المشتريات...");
    }

    // ─── Purchase History Commands ────────────────────────────────────────────
    [RelayCommand]
    private async Task FilterHistoryAsync()
        => await ExecuteBusyAsync(LoadPurchaseHistoryAsync, "جاري البحث...");

    [RelayCommand]
    private async Task ApproveOrderAsync(PurchaseOrder? order)
    {
        if (order == null || order.Status != PurchaseOrderStatus.Pending) return;

        var confirm = MessageBox.Show(
            $"هل أنت متأكد من الموافقة على استلام الفاتورة رقم {order.OrderNumber}؟\nسيتم تحديث المخزون وحسابات المورد.",
            "تأكيد الاستلام", MessageBoxButton.YesNo, MessageBoxImage.Question);
        
        if (confirm != MessageBoxResult.Yes) return;

        await ExecuteBusyAsync(async () =>
        {
            await using var ctx = await _contextFactory.CreateDbContextAsync();
            await using var transaction = await ctx.Database.BeginTransactionAsync();
            try
            {
                var orderWithDetails = await ctx.PurchaseOrders
                    .Include(o => o.OrderDetails)
                    .FirstOrDefaultAsync(o => o.Id == order.Id);

                if (orderWithDetails == null) return;

                // 1. Update Stock
                foreach (var detail in orderWithDetails.OrderDetails)
                {
                    var product = await ctx.Products.FindAsync(detail.ProductId);
                    if (product != null)
                    {
                        await ctx.Products
                            .Where(p => p.Id == detail.ProductId)
                            .ExecuteUpdateAsync(s => s.SetProperty(p => p.Stock, p => p.Stock + detail.Quantity));
                            
                        product.Stock += detail.Quantity;
                        ctx.Entry(product).Property(x => x.Stock).IsModified = false;
                        
                        ctx.StockMovements.Add(new StockMovement
                        {
                            ProductId = product.Id,
                            Quantity = detail.Quantity,
                            Type = MovementType.Purchase,
                            Reference = orderWithDetails.OrderNumber,
                            MovementDate = DateTime.Now
                        });
                    }
                }

                // 2. Update Supplier Debt
                if (orderWithDetails.RemainingAmount > 0)
                {
                    var supplier = await ctx.Suppliers.FindAsync(orderWithDetails.SupplierId);
                    if (supplier != null)
                    {
                        supplier.DebtAmount += orderWithDetails.RemainingAmount;
                    }
                }

                // 3. Mark as Received
                orderWithDetails.Status = PurchaseOrderStatus.Received;
                orderWithDetails.ReceivedDate = DateTime.Now;

                await ctx.SaveChangesAsync();
                MessageBox.Show("✅ تمت الموافقة بنجاح وتم تحديث المخزون.", "نجاح", MessageBoxButton.OK, MessageBoxImage.Information);

                // Refresh history
                await LoadPurchaseHistoryAsync();
                await transaction.CommitAsync();
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                MessageBox.Show($"حدث خطأ أثناء الموافقة:\n{ex.Message}", "خطأ", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        });
    }

    [RelayCommand]
    private async Task ExportOrderPdfAsync(PurchaseOrder? order)
    {
        if (order == null) return;

        try
        {
            await using var ctx = await _contextFactory.CreateDbContextAsync();
            var orderWithDetails = await ctx.PurchaseOrders
                .AsNoTracking()
                .Include(o => o.Supplier)
                .Include(o => o.OrderDetails).ThenInclude(d => d.Product)
                .FirstOrDefaultAsync(o => o.Id == order.Id);

            if (orderWithDetails == null) return;

            QuestPDF.Settings.License = LicenseType.Community;

            var pdfBytes = Document.Create(container =>
            {
                container.Page(page =>
                {
                    page.Size(PageSizes.A4);
                    page.Margin(1.5f, Unit.Centimetre);
                    page.PageColor(Colors.White);
                    page.DefaultTextStyle(x => x.FontFamily("Segoe UI").FontSize(10).DirectionFromRightToLeft());

                    // Header
                    page.Header().Column(col =>
                    {
                        col.Item().Background("#1E3A5F").Padding(14).Row(row =>
                        {
                            row.RelativeItem().Column(c =>
                            {
                                c.Item().Text("فاتورة مشتريات").Bold().FontSize(22).FontColor(Colors.White);
                                c.Item().Text($"رقم: {orderWithDetails.OrderNumber}").FontSize(10).FontColor("#94A3B8");
                            });
                            row.AutoItem().AlignRight().AlignMiddle().Column(c =>
                            {
                                c.Item().Text("SmartPOS").Bold().FontSize(14).FontColor("#94A3B8");
                                c.Item().Text($"التاريخ: {orderWithDetails.OrderDate:dd/MM/yyyy}").FontSize(10).FontColor("#94A3B8");
                            });
                        });
                        col.Item().LineHorizontal(2).LineColor("#1E3A5F");
                    });

                    page.Content().PaddingVertical(12).Column(col =>
                    {
                        col.Spacing(10);

                        // Supplier info box
                        col.Item().Background("#F1F5F9").Padding(10).Row(row =>
                        {
                            row.RelativeItem().Column(c =>
                            {
                                c.Item().Text("بيانات المورد").Bold().FontSize(12).FontColor("#1E3A5F");
                                c.Item().Text($"الاسم: {orderWithDetails.Supplier?.Name ?? "-"}").FontSize(10);
                                c.Item().Text($"الهاتف: {orderWithDetails.Supplier?.Phone ?? "-"}").FontSize(10);
                            });
                            row.RelativeItem().AlignRight().Column(c =>
                            {
                                c.Item().Text($"حالة الفاتورة: {TranslateStatus(orderWithDetails.Status)}").Bold().FontSize(11).FontColor("#059669");
                                c.Item().Text($"تاريخ الاستلام: {orderWithDetails.ReceivedDate?.ToString("dd/MM/yyyy") ?? "-"}").FontSize(10);
                            });
                        });

                        // Items table
                        col.Item().Table(table =>
                        {
                            table.ColumnsDefinition(c =>
                            {
                                c.ConstantColumn(35);
                                c.RelativeColumn(3);
                                c.RelativeColumn();
                                c.RelativeColumn();
                                c.RelativeColumn();
                            });
                            table.Header(h =>
                            {
                                foreach (var t in new[] { "#", "المنتج", "الكمية", "سعر الوحدة", "الإجمالي" })
                                    h.Cell().Background("#1E3A5F").Padding(6).Text(t).Bold().FontSize(10).FontColor(Colors.White).AlignCenter();
                            });

                            int idx = 1;
                            foreach (var d in orderWithDetails.OrderDetails)
                            {
                                var bg = idx % 2 == 0 ? "#F8FAFC" : "#FFFFFF";
                                table.Cell().Background(bg).Padding(5).Text(idx++.ToString()).AlignCenter().FontSize(10);
                                table.Cell().Background(bg).Padding(5).Text(d.Product?.Name ?? "-").FontSize(10);
                                table.Cell().Background(bg).Padding(5).Text(d.Quantity.ToString()).AlignCenter().FontSize(10);
                                table.Cell().Background(bg).Padding(5).Text($"{d.UnitCost:N2}").AlignCenter().FontSize(10);
                                table.Cell().Background(bg).Padding(5).Text($"{d.TotalCost:N2}").AlignRight().FontSize(10).Bold();
                            }
                        });

                        // Totals summary
                        col.Item().AlignRight().Column(c =>
                        {
                            c.Item().Text($"إجمالي الفاتورة: {orderWithDetails.TotalAmount:N2} ج.م").Bold().FontSize(13).FontColor("#1E3A5F");
                            c.Item().Text($"المدفوع: {orderWithDetails.PaidAmount:N2} ج.م").FontSize(11).FontColor("#059669");
                            if (orderWithDetails.RemainingAmount > 0)
                                c.Item().Text($"المتبقي (آجل): {orderWithDetails.RemainingAmount:N2} ج.م").FontSize(11).FontColor("#DC2626").Bold();
                        });
                    });

                    page.Footer().AlignCenter().Text($"طُبع في: {DateTime.Now:dd/MM/yyyy HH:mm:ss}").FontSize(8).FontColor("#9CA3AF");
                });
            }).GeneratePdf();

            var defaultFolder = _settingsService?.DefaultExportFolder;
            if (!string.IsNullOrWhiteSpace(defaultFolder) && !System.IO.Directory.Exists(defaultFolder))
            {
                try { System.IO.Directory.CreateDirectory(defaultFolder); } catch { }
            }

            var dlg = new SaveFileDialog
            {
                Title = "حفظ فاتورة المشتريات كـ PDF",
                FileName = $"فاتورة-مشتريات-{orderWithDetails.OrderNumber}.pdf",
                InitialDirectory = !string.IsNullOrWhiteSpace(defaultFolder) && System.IO.Directory.Exists(defaultFolder)
                    ? defaultFolder
                    : Environment.GetFolderPath(Environment.SpecialFolder.Desktop),
                DefaultExt = ".pdf",
                Filter = "PDF Files (*.pdf)|*.pdf"
            };

            if (dlg.ShowDialog() == true)
            {
                await File.WriteAllBytesAsync(dlg.FileName, pdfBytes);

                if (_settingsService?.AutoOpenExportedFile == true)
                {
                    try { System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo(dlg.FileName) { UseShellExecute = true }); } catch { }
                }

                MessageBox.Show($"✅ تم حفظ الفاتورة بنجاح:\n{dlg.FileName}", "تم", MessageBoxButton.OK, MessageBoxImage.Information);
            }
        }
        catch (Exception ex)
        {
            MessageBox.Show($"خطأ في تصدير PDF:\n{ex.Message}", "خطأ", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private static string TranslateStatus(PurchaseOrderStatus s) => s switch
    {
        PurchaseOrderStatus.Pending => "معلقة",
        PurchaseOrderStatus.Received => "مستلمة",
        PurchaseOrderStatus.Cancelled => "ملغاة",
        PurchaseOrderStatus.PartiallyReceived => "مستلمة جزئياً",
        _ => s.ToString()
    };

    // ─── Supplier Debt Payment Commands ──────────────────────────────────────
    partial void OnSelectedDebtSupplierChanged(Supplier? value)
    {
        if (value != null)
        {
            PaymentAmount = value.DebtAmount;
            _ = LoadPaymentHistoryForSupplierAsync(value.Id);
        }
    }

    [RelayCommand]
    private async Task PaySupplierDebtAsync()
    {
        if (SelectedDebtSupplier == null)
        {
            MessageBox.Show("يرجى اختيار مورد", "تنبيه", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }
        if (PaymentAmount <= 0)
        {
            MessageBox.Show("يرجى إدخال مبلغ صحيح أكبر من الصفر", "تنبيه", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }
        if (PaymentAmount > SelectedDebtSupplier.DebtAmount)
        {
            MessageBox.Show($"المبلغ المدخل ({PaymentAmount:N2}) أكبر من الرصيد المستحق ({SelectedDebtSupplier.DebtAmount:N2})", "تنبيه", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        var confirm = MessageBox.Show(
            $"تأكيد سداد دين المورد\n\nالمورد: {SelectedDebtSupplier.Name}\nالمبلغ المسدد: {PaymentAmount:N2} ج.م\nالرصيد المتبقي: {SelectedDebtSupplier.DebtAmount - PaymentAmount:N2} ج.م",
            "تأكيد السداد", MessageBoxButton.YesNo, MessageBoxImage.Question);
        if (confirm != MessageBoxResult.Yes) return;

        await ExecuteBusyAsync(async () =>
        {
            await using var ctx = await _contextFactory.CreateDbContextAsync();
            try
            {
                var supplier = await ctx.Suppliers.FindAsync(SelectedDebtSupplier.Id);
                if (supplier == null) return;

                supplier.DebtAmount -= PaymentAmount;

                ctx.SupplierPayments.Add(new SupplierPayment
                {
                    SupplierId = SelectedDebtSupplier.Id,
                    Amount = PaymentAmount,
                    PaymentDate = DateTime.Now,
                    Notes = string.IsNullOrWhiteSpace(PaymentNotes) ? null : PaymentNotes,
                    Reference = $"PAY-{DateTime.Now:yyyyMMddHHmmss}"
                });

                ctx.Expenses.Add(new Expense
                {
                    Amount = PaymentAmount,
                    ExpenseDate = DateTime.Now,
                    Description = $"سداد مديونية للمورد: {supplier.Name}",
                    Notes = $"سداد عبر شاشة ديون الموردين. {PaymentNotes}",
                    Category = ExpenseCategory.Supplies,
                    UserId = _currentUser.Id
                });

                await ctx.SaveChangesAsync();
                MessageBox.Show("✅ تم تسجيل السداد وتحديث رصيد المورد بنجاح", "تم", MessageBoxButton.OK, MessageBoxImage.Information);

                PaymentAmount = 0;
                PaymentNotes = string.Empty;
                await LoadSupplierDebtsAsync();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"خطأ في تسجيل السداد:\n{ex.InnerException?.Message ?? ex.Message}", "خطأ", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }, "جاري تسجيل السداد...");
    }

    // ─── Low Stock Commands ───────────────────────────────────────────────────
    [RelayCommand]
    private async Task RefreshLowStockAsync()
        => await ExecuteBusyAsync(LoadLowStockAsync, "جاري تحديث قائمة المخزون المنخفض...");

    [RelayCommand]
    private void QuickOrderLowStock(Product? product)
    {
        if (product == null) return;
        SelectedTabIndex = 0; // go to New Order tab
        SelectedProduct = Products.FirstOrDefault(p => p.Id == product.Id);
        if (SelectedProduct != null)
        {
            Quantity = Math.Max(1, product.MinStockLevel * 2 - product.Stock);
            CostPrice = product.PurchasePrice;
            if (product.SupplierId.HasValue)
                SelectedSupplier = Suppliers.FirstOrDefault(s => s.Id == product.SupplierId.Value);
        }
    }

}
