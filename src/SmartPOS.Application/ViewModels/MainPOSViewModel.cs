using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.Messaging;
using System.Drawing.Printing;
using System.Windows;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.EntityFrameworkCore;
using SmartPOS.Application.DTOs;
using SmartPOS.Application.Extensions;
using SmartPOS.Core.Entities;
using SmartPOS.Core.Interfaces;
using SmartPOS.Infrastructure.Data;

namespace SmartPOS.Application.ViewModels;

public partial class MainPOSViewModel : BaseViewModel, IDisposable, CommunityToolkit.Mvvm.Messaging.IRecipient<SmartPOS.Application.Messages.BarcodeScannedMessage>
{
    private readonly IDbContextFactory<AppDbContext> _contextFactory;
    private readonly IShiftRepository _shiftRepository;
    private readonly IPrintingService _printingService;
    private readonly IBarcodeService _barcodeService;
    private readonly ISettingsService _settingsService;
    private readonly INotificationService _notificationService;
    private readonly ISoundService? _soundService;
    private readonly User _currentUser;

    // --- POS State ---
    [ObservableProperty]
    private ObservableCollection<CartItem> _cartItems = new();

    [ObservableProperty]
    private CartItem? _selectedCartItem;

    [ObservableProperty]
    private string _barcodeInput = string.Empty;

    // --- Grid vs Compact List View ---
    [ObservableProperty]
    private bool _isCompactListView = false;

    // --- Parked Orders ---
    [ObservableProperty]
    private ObservableCollection<SmartPOS.Application.DTOs.ParkedOrder> _parkedOrders = new();

    [ObservableProperty]
    private int _parkedOrdersCount = 0;

    [ObservableProperty]
    private bool _isParkedOrdersOpen = false;

    // --- Touch Numpad ---
    [ObservableProperty]
    private bool _isTouchNumpadVisible = false;

    [ObservableProperty]
    private string _numpadInput = string.Empty;

    // --- Quick Cash Tender & Change ---
    [ObservableProperty]
    private ObservableCollection<decimal> _quickTenderAmounts = new();

    [ObservableProperty]
    private decimal _changeDue;

    // --- Fast Customer Search & Loyalty ---
    [ObservableProperty]
    private ObservableCollection<Customer> _allCustomers = new();

    [ObservableProperty]
    private ObservableCollection<Customer> _filteredCustomers = new();

    [ObservableProperty]
    private string _customerSearchText = string.Empty;

    [ObservableProperty]
    private bool _isCustomerDropdownOpen = false;

    [ObservableProperty]
    private decimal _customerLoyaltyPoints = 0;

    [ObservableProperty]
    private decimal _customerLoyaltyCashValue = 0;

    // --- Totals & Math ---
    [ObservableProperty]
    private decimal _subtotal;

    [ObservableProperty]
    private decimal _discountPercentage;

    [ObservableProperty]
    private decimal _discountAmount;

    [ObservableProperty]
    private decimal _taxPercentage = 0;

    [ObservableProperty]
    private decimal _taxAmount;

    [ObservableProperty]
    private decimal _totalAmount;

    [ObservableProperty] private decimal _discountValue;

    [ObservableProperty] private bool _shouldPrintReceipt = true;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(ChangeAmount))]
    private decimal _amountPaid;

    [ObservableProperty]
    private decimal _changeAmount;

    // --- Currency & Market ---
    [ObservableProperty]
    private string _currencySymbol = "ج.م";

    // --- Custom Discount ---
    [ObservableProperty]
    private bool _isCustomDiscountOpen = false;

    [ObservableProperty]
    private string _customDiscountInput = string.Empty;

    [ObservableProperty]
    private bool _isCustomDiscountPercentage = true;

    // --- Selections ---
    [ObservableProperty]
    private ObservableCollection<PaymentMethod> _paymentMethods = new();

    [ObservableProperty]
    private PaymentMethod _selectedPaymentMethod = PaymentMethod.Cash;

    [RelayCommand]
    private void SetPaymentMethod(PaymentMethod method)
    {
        SelectedPaymentMethod = method;
    }

    [RelayCommand]
    private void ToggleCustomDiscountPopup()
    {
        IsCustomDiscountOpen = !IsCustomDiscountOpen;
        if (IsCustomDiscountOpen)
        {
            CustomDiscountInput = DiscountPercentage > 0 ? DiscountPercentage.ToString("0.##") : string.Empty;
        }
    }

    [RelayCommand]
    private void ApplyCustomDiscountSubmit()
    {
        var cleanInput = CustomDiscountInput?.Replace(',', '.').Trim() ?? string.Empty;
        if (decimal.TryParse(cleanInput, System.Globalization.NumberStyles.Any, System.Globalization.CultureInfo.InvariantCulture, out var val) && val >= 0)
        {
            if (IsCustomDiscountPercentage)
            {
                var pct = Math.Clamp(val, 0, 100);
                DiscountPercentage = pct;
                DiscountAmount = Subtotal * (pct / 100m);
                _notificationService.ShowSuccess($"✅ تم تطبيق خصم {pct}%");
            }
            else
            {
                DiscountAmount = Math.Min(Subtotal, val);
                DiscountPercentage = Subtotal > 0 ? Math.Round((DiscountAmount / Subtotal) * 100, 2) : 0;
                _notificationService.ShowSuccess($"✅ تم تطبيق خصم بقيمة {val:N2} {CurrencySymbol}");
            }
            CalculateTotals();
            IsCustomDiscountOpen = false;
            _soundService?.PlayClick();
        }
        else
        {
            _notificationService.ShowWarning("يرجى إدخال قيمة خصم صحيحة (مثال: 7.5 أو 15)");
        }
    }

    [ObservableProperty]
    private OrderType _selectedOrderType = OrderType.DineIn;

    [ObservableProperty]
    private Table? _selectedTable;

    [ObservableProperty]
    private string _invoiceNumber = string.Empty;

    [ObservableProperty]
    private bool _isProcessing;

    // --- Quick Products & Categories ---
    [ObservableProperty]
    private ObservableCollection<Category> _categories = new();

    [ObservableProperty]
    private Category? _selectedCategory;

    [ObservableProperty]
    private ObservableCollection<Product> _quickProducts = new();

    // --- Customers ---
    [ObservableProperty]
    private ObservableCollection<Customer> _customers = new();

    [ObservableProperty]
    private Customer? _selectedCustomer;

    // --- Staff Members ---
    [ObservableProperty]
    private ObservableCollection<User> _staffMembers = new();

    [ObservableProperty]
    private User? _selectedStaffMember;

    // --- Shift State ---
    [ObservableProperty]
    private bool _isShiftOpen;

    [ObservableProperty]
    private string _shiftWarningMessage = string.Empty;

    private readonly IAuthorizationService _authService;

    public MainPOSViewModel(
        IDbContextFactory<AppDbContext> contextFactory,
        IShiftRepository shiftRepository,
        IPrintingService printingService,
        IBarcodeService barcodeService,
        ISettingsService settingsService,
        INotificationService notificationService,
        IAuthorizationService authService,
        User currentUser,
        ISoundService? soundService = null)
    {
        _contextFactory = contextFactory;
        _shiftRepository = shiftRepository;
        _printingService = printingService;
        _barcodeService = barcodeService;
        _settingsService = settingsService;
        _notificationService = notificationService;
        _authService = authService;
        _currentUser = currentUser;
        _soundService = soundService;

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
        System.Windows.Application.Current.Dispatcher.InvokeAsync(async () =>
        {
            await AddProductByBarcode(message.Value);
        });
    }

    private async Task InitializeAsync()
    {
        await ExecuteBusyAsync(async () =>
        {
            await LoadSettingsAsync();
            await LoadQuickDataAsync();
            await CheckActiveShiftAsync();
        }, "⏳ جاري التهيئة...", "✅ جاهز");
    }

    private async Task LoadSettingsAsync()
    {
        try
        {
            await _settingsService.LoadSettingsAsync();
        }
        catch { /* ignore */ }

        TaxPercentage = _settingsService.TaxPercentage;
        ShouldPrintReceipt = _settingsService.AutoPrintReceipt;
        CurrencySymbol = !string.IsNullOrWhiteSpace(_settingsService.CurrencySymbol) ? _settingsService.CurrencySymbol : "ج.م";

        // Configure Payment Methods based on JSON or Country
        var methods = new List<PaymentMethod>();
        var json = _settingsService.ActivePaymentMethodsJson;
        if (!string.IsNullOrWhiteSpace(json))
        {
            try
            {
                var ids = System.Text.Json.JsonSerializer.Deserialize<List<int>>(json);
                if (ids != null && ids.Count > 0)
                {
                    methods.AddRange(ids.Select(id => (PaymentMethod)id));
                }
            }
            catch { /* fallback below */ }
        }

        if (methods.Count == 0)
        {
            if (_settingsService.CountryCode == "SA")
            {
                methods.AddRange(new[]
                {
                    PaymentMethod.Cash,
                    PaymentMethod.Mada,
                    PaymentMethod.Card,
                    PaymentMethod.StcPay,
                    PaymentMethod.ApplePay,
                    PaymentMethod.Deferred
                });
            }
            else
            {
                methods.AddRange(new[]
                {
                    PaymentMethod.Cash,
                    PaymentMethod.Card,
                    PaymentMethod.VodafoneCash,
                    PaymentMethod.InstaPay,
                    PaymentMethod.Deferred,
                    PaymentMethod.StaffMeal
                });
            }
        }

        PaymentMethods = new ObservableCollection<PaymentMethod>(methods);
        if (!PaymentMethods.Contains(SelectedPaymentMethod))
        {
            SelectedPaymentMethod = PaymentMethods.FirstOrDefault();
        }
    }

    private async Task CheckActiveShiftAsync()
    {
        try
        {
            IsShiftOpen = await _shiftRepository.HasActiveShiftAsync(_currentUser.Id);

            if (!IsShiftOpen)
            {
                ShiftWarningMessage = "⚠️ تحذير: لا توجد وردية مفتوحة! يرجى فتح وردية قبل البدء في البيع.";
            }
            else
            {
                ShiftWarningMessage = string.Empty;
            }
        }
        catch
        {
            IsShiftOpen = false;
            ShiftWarningMessage = "⚠️ خطأ في فحص الوردية";
        }
    }

    private async Task LoadQuickDataAsync()
    {
        try
        {
            await using var context = await _contextFactory.CreateDbContextAsync();
            var categories = await context.Categories
                .AsNoTracking()
                .Where(c => !c.IsDeleted)
                .OrderBy(c => c.Name)
                .ToListAsync();

            Categories.SyncWith(categories);
            SelectedCategory = categories.FirstOrDefault();

            var customers = await context.Customers
                .AsNoTracking()
                .Where(c => c.IsActive && !c.IsDeleted)
                .OrderBy(c => c.Name)
                .ToListAsync();
            Customers.SyncWith(customers);

            var staff = await context.Users
                .AsNoTracking()
                .Where(u => u.IsActive && !u.IsDeleted)
                .OrderBy(u => u.FullName)
                .ToListAsync();
            StaffMembers.SyncWith(staff);

            await LoadQuickProductsAsync();
        }
        catch { /* ignore */ }
    }

    private async Task LoadQuickProductsAsync()
    {
        await using var context = await _contextFactory.CreateDbContextAsync();
        var query = context.Products.AsNoTracking().Where(p => p.IsActive && !p.IsDeleted);

        if (SelectedCategory != null)
        {
            query = query.Where(p => p.CategoryId == SelectedCategory.Id);
        }

        var products = await query.OrderBy(p => p.Name).Take(24).ToListAsync();
        QuickProducts.SyncWith(products);
    }

    partial void OnSelectedCategoryChanged(Category? value)
    {
        _ = LoadQuickProductsAsync();
    }

    [RelayCommand]
    private void SelectCategory(Category category)
    {
        SelectedCategory = category;
    }

    // --- Cart Management ---

    private string _lastScannedBarcode = string.Empty;
    private DateTime _lastScanTime = DateTime.MinValue;

    [RelayCommand]
    private async Task AddProductByBarcode(string? barcode = null)
    {
        var scannedCode = (barcode ?? BarcodeInput)?.Trim();
        if (string.IsNullOrWhiteSpace(scannedCode)) return;

        // Prevent double-firing (Enter key + Global Hook) within 500ms
        if (scannedCode == _lastScannedBarcode && (DateTime.Now - _lastScanTime).TotalMilliseconds < 500)
        {
            BarcodeInput = string.Empty;
            return;
        }

        _lastScannedBarcode = scannedCode;
        _lastScanTime = DateTime.Now;
        BarcodeInput = string.Empty;

        try
        {
            IsProcessing = true;
            StatusMessage = "جاري البحث عن المنتج...";

            await using var lookupContext = await _contextFactory.CreateDbContextAsync();
            Product? product = null;
            decimal parsedWeightKg = 1;

            // 1. Barcode Weight Scale Parser (EAN-13 starting with 20-29 or 99)
            if (scannedCode.Length == 13 && (scannedCode.StartsWith("20") || scannedCode.StartsWith("21") || 
                scannedCode.StartsWith("22") || scannedCode.StartsWith("23") || scannedCode.StartsWith("24") || 
                scannedCode.StartsWith("25") || scannedCode.StartsWith("26") || scannedCode.StartsWith("27") || 
                scannedCode.StartsWith("28") || scannedCode.StartsWith("29") || scannedCode.StartsWith("99")))
            {
                var itemSku5 = scannedCode.Substring(2, 5);
                var itemSku4 = scannedCode.Substring(2, 4);

                product = await lookupContext.Products
                    .AsNoTracking()
                    .Include(p => p.Category)
                    .FirstOrDefaultAsync(p => (p.Barcode == scannedCode || p.Barcode == itemSku5 || p.Barcode == itemSku4) && p.IsActive && !p.IsDeleted);

                if (product != null && decimal.TryParse(scannedCode.Substring(7, 5), System.Globalization.NumberStyles.Any, System.Globalization.CultureInfo.InvariantCulture, out var rawVal))
                {
                    parsedWeightKg = rawVal / 1000m; // Convert grams to KG (e.g. 1500g = 1.5kg)
                }
            }

            // 2. Standard Product Barcode Lookup
            if (product == null)
            {
                product = await lookupContext.Products
                    .AsNoTracking()
                    .Include(p => p.Category)
                    .FirstOrDefaultAsync(p => (p.Barcode == scannedCode || p.Barcode == scannedCode.TrimStart('0')) && p.IsActive && !p.IsDeleted);
            }

            // 3. Invoice Barcode Scan (e.g. INV-2026...)
            if (product == null && (scannedCode.StartsWith("INV", StringComparison.OrdinalIgnoreCase) || scannedCode.Contains("-")))
            {
                var sale = await lookupContext.Sales
                    .AsNoTracking()
                    .Include(s => s.Customer)
                    .Include(s => s.SaleDetails)
                    .FirstOrDefaultAsync(s => s.InvoiceNumber == scannedCode);

                if (sale != null)
                {
                    _soundService?.PlayBarcodeBeep();
                    _notificationService.ShowSuccess($"🧾 فاتورة رقم {sale.InvoiceNumber} - الإجمالي: {sale.TotalAmount:N2} {CurrencySymbol} - العميل: {sale.Customer?.Name ?? "نقدي"}");
                    StatusMessage = $"تم مسح فاتورة رقم {sale.InvoiceNumber}";
                    return;
                }
            }

            if (product == null)
            {
                StatusMessage = "المنتج غير مسجل!";
                _soundService?.PlayWarningAlert();
                var result = MessageBox.Show($"المنتج صاحب الباركود '{scannedCode}' غير مسجل في قائمة المنتجات.\nهل ترغب في حفظ الباركود لإضافته؟", "منتج غير مسجل", MessageBoxButton.YesNo, MessageBoxImage.Question);
                if (result == MessageBoxResult.Yes)
                {
                    BarcodeInput = scannedCode;
                }
                return;
            }

            if (product.Stock <= 0)
            {
                StatusMessage = "نفذت الكمية!";
                _soundService?.PlayWarningAlert();
                MessageBox.Show($"المنتج '{product.Name}' نفذت كميته من المخزن (الرصيد: 0).", "نفذت الكمية", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            AddOrUpdateCartItemWithQuantity(product, parsedWeightKg);
            _soundService?.PlayBarcodeBeep();
            StatusMessage = $"تمت الإضافة: {product.Name} (السعر: {product.SellingPrice:N2} {CurrencySymbol})";
        }
        catch (Exception ex)
        {
            StatusMessage = "خطأ في إضافة المنتج";
            MessageBox.Show($"خطأ: {ex.Message}", "خطأ", MessageBoxButton.OK, MessageBoxImage.Error);
        }
        finally
        {
            IsProcessing = false;
        }
    }
    private void AddOrUpdateCartItemWithQuantity(Product product, decimal quantityToAdd = 1)
    {
        var existingItem = CartItems.FirstOrDefault(i => i.ProductId == product.Id);
        int qtyInt = (int)Math.Max(1, quantityToAdd);

        if (existingItem != null)
        {
            if (existingItem.Quantity + qtyInt <= product.Stock)
            {
                existingItem.Quantity += qtyInt;
                OnPropertyChanged(nameof(CartItems));
            }
            else
            {
                MessageBox.Show($"لا يمكن إضافة المزيد. الكمية المتاحة في المخزن: {product.Stock}", "تجاوز المخزون", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }
        }
        else
        {
            var cartItem = new CartItem
            {
                ProductId = product.Id,
                Barcode = product.Barcode,
                Name = product.Name,
                UnitPrice = product.SellingPrice,
                UnitCost = product.PurchasePrice,
                Quantity = qtyInt,
                DiscountPercentage = 0,
                AvailableStock = product.Stock
            };

            CartItems.Add(cartItem);
        }

        CalculateTotals();
    }

    private void AddOrUpdateCartItem(Product product)
    {
        AddOrUpdateCartItemWithQuantity(product, 1);
    }

    [RelayCommand]
    private async Task AddProductToCart(Product product)
    {
        if (product == null) return;

        try
        {
            await using var cartCtx = await _contextFactory.CreateDbContextAsync();
            var dbProduct = await cartCtx.Products.AsNoTracking().FirstOrDefaultAsync(p => p.Id == product.Id && p.IsActive && !p.IsDeleted);

            if (dbProduct == null)
            {
                MessageBox.Show("المنتج غير متاح.", "تنبيه", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            if (dbProduct.Stock <= 0)
            {
                MessageBox.Show("المنتج غير متوفر في المخزون.", "تنبيه", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            AddOrUpdateCartItem(dbProduct);

            // ── Low Stock Alert ────────────────────────────────────────────────
            var cartQty = CartItems.FirstOrDefault(i => i.ProductId == dbProduct.Id)?.Quantity ?? 1;
            var remaining = dbProduct.Stock - cartQty;
            if (remaining >= 0 && remaining <= dbProduct.MinStockLevel)
                _notificationService.ShowWarning($"⚠️ تنبيه: مخزون '{dbProduct.Name}' منخفض (متبقي: {remaining})");
        }
        catch (Exception ex)
        {
            MessageBox.Show($"خطأ في إضافة المنتج: {ex.Message}", "خطأ", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    [RelayCommand]
    private void IncreaseQuantity(CartItem item)
    {
        if (item.CanIncreaseQuantity)
        {
            item.Quantity++;
            OnPropertyChanged(nameof(CartItems));
            CalculateTotals();
        }
        else
        {
            MessageBox.Show($"الحد الأقصى للمخزون: {item.AvailableStock}", "تجاوز الكمية", MessageBoxButton.OK, MessageBoxImage.Warning);
        }
    }

    [RelayCommand]
    private async Task DecreaseQuantity(CartItem item)
    {
        if (item.Quantity > 1)
        {
            item.Quantity--;
            OnPropertyChanged(nameof(CartItems));
            CalculateTotals();
        }
        else
        {
            await RemoveItem(item);
        }
    }

    [RelayCommand]
    private async Task RemoveItem(CartItem item)
    {
        if (!_authService.HasPermission(Permissions.VoidItem))
        {
            var authorized = await _authService.RequestAdminOverrideAsync($"مسح عنصر من الفاتورة: {item.Name}");
            if (!authorized) return;
        }

        CartItems.Remove(item);
        CalculateTotals();
    }

    [RelayCommand]
    private async Task ApplyDiscount()
    {
        if (DiscountPercentage > 0 || DiscountAmount > 0)
        {
            if (!_authService.HasPermission(Permissions.ApplyDiscount))
            {
                var authorized = await _authService.RequestAdminOverrideAsync("تطبيق خصم عام على الفاتورة");
                if (!authorized)
                {
                    DiscountPercentage = 0;
                    DiscountAmount = 0;
                    CalculateTotals();
                    return;
                }
            }
            
            if (DiscountPercentage > 15 || (DiscountAmount > 0 && DiscountAmount > (Subtotal * 0.15m)))
            {
                if (!_authService.HasPermission(Permissions.ApplyHighDiscount))
                {
                    var authorized = await _authService.RequestAdminOverrideAsync("تطبيق خصم استثنائي (أكثر من 15%)");
                    if (!authorized)
                    {
                        DiscountPercentage = 0;
                        DiscountAmount = 0;
                        CalculateTotals();
                        return;
                    }
                }
            }
        }
        
        CalculateTotals();
    }

    private void CalculateTotals()
    {
        Subtotal = CartItems.Sum(i => i.Subtotal);

        if (DiscountPercentage > 0 && DiscountPercentage <= 100)
            DiscountAmount = Subtotal * (DiscountPercentage / 100);

        var afterDiscount = Subtotal - DiscountAmount;

        if (TaxPercentage > 0)
            TaxAmount = afterDiscount * (TaxPercentage / 100);

        TotalAmount = Math.Max(0, afterDiscount + TaxAmount);

        if (AmountPaid >= TotalAmount)
        {
            ChangeAmount = AmountPaid - TotalAmount;
            ChangeDue = ChangeAmount;
        }
        else
        {
            ChangeAmount = 0;
            ChangeDue = 0;
        }

        UpdateQuickTenderAmounts();
    }

    private void UpdateQuickTenderAmounts()
    {
        QuickTenderAmounts.Clear();
        if (TotalAmount <= 0) return;

        // 1. Exact Amount
        QuickTenderAmounts.Add(TotalAmount);

        // 2. Next rounded standard Egyptian denominations
        decimal[] standardBills = { 20m, 50m, 100m, 200m, 500m, 1000m, 2000m };
        foreach (var bill in standardBills)
        {
            if (bill > TotalAmount && !QuickTenderAmounts.Contains(bill))
            {
                QuickTenderAmounts.Add(bill);
                if (QuickTenderAmounts.Count >= 5) break;
            }
        }

        // If high total, add next rounded 100
        var nextHundred = Math.Ceiling(TotalAmount / 100m) * 100m;
        if (nextHundred > TotalAmount && !QuickTenderAmounts.Contains(nextHundred))
        {
            QuickTenderAmounts.Add(nextHundred);
        }
    }

    [RelayCommand]
    private void SelectQuickTender(decimal amount)
    {
        AmountPaid = amount;
        _soundService?.PlayClick();
    }

    [RelayCommand]
    private void SetExactCashTender()
    {
        AmountPaid = TotalAmount;
        _soundService?.PlayClick();
    }

    partial void OnAmountPaidChanged(decimal value) => CalculateTotals();
    partial void OnDiscountPercentageChanged(decimal value) => CalculateTotals();
    partial void OnTaxPercentageChanged(decimal value) => CalculateTotals();

    // --- Order Checkout ---

    [RelayCommand]
    private async Task SubmitOrder()
    {
        if (CartItems.Count == 0)
        {
            _notificationService.ShowWarning("السلة فارغة!", "لا يمكن إتمام البيع");
            return;
        }

        if (SelectedPaymentMethod == PaymentMethod.Deferred)
        {
            if (SelectedCustomer == null)
            {
                _notificationService.ShowWarning("يجب تحديد عميل للبيع الآجل!", "خطأ في البيع");
                return;
            }

            if (!_authService.HasPermission(Permissions.ManageCustomers)) // assuming Admin has this
            {
                var authorized = await _authService.RequestAdminOverrideAsync("بيع آجل للعميل: " + SelectedCustomer.Name);
                if (!authorized)
                {
                    _notificationService.ShowWarning("تم إلغاء العملية، لم يتم الموافقة على البيع الآجل.", "إلغاء");
                    return;
                }
            }

            // Optional: check credit limit
            if (SelectedCustomer.CreditLimit > 0 && (SelectedCustomer.CurrentDebt + TotalAmount) > SelectedCustomer.CreditLimit)
            {
                var authorized = await _authService.RequestAdminOverrideAsync($"تخطي الحد الائتماني للعميل: {SelectedCustomer.Name}");
                if (!authorized)
                {
                    _notificationService.ShowWarning("العميل تخطى الحد الائتماني المسموح به.", "رفض العملية");
                    return;
                }
            }
        }
        else if (SelectedPaymentMethod == PaymentMethod.StaffMeal)
        {
            if (SelectedStaffMember == null)
            {
                _notificationService.ShowWarning("يجب تحديد الموظف لوجبة الضيافة!", "خطأ في البيع");
                return;
            }

            decimal totalCost = CartItems.Sum(i => i.UnitCost * i.Quantity);
            var today = DateTime.Today;
            
            await using var mealCheckCtx = await _contextFactory.CreateDbContextAsync();
            var todaysMealsCost = await mealCheckCtx.Sales
                .Where(s => s.PaymentMethod == PaymentMethod.StaffMeal 
                            && s.ConsumedByUserId == SelectedStaffMember.Id
                            && s.SaleDate >= today)
                .SelectMany(s => s.SaleDetails)
                .SumAsync(d => d.UnitCost * d.Quantity);

            if (todaysMealsCost + totalCost > SelectedStaffMember.DailyMealLimit)
            {
                var authorized = await _authService.RequestAdminOverrideAsync($"تخطي الحد اليومي لوجبات الموظف: {SelectedStaffMember.FullName}\nالمتبقي: {Math.Max(0, SelectedStaffMember.DailyMealLimit - todaysMealsCost):N2} ج.م\nالتكلفة الحالية: {totalCost:N2} ج.م");
                if (!authorized)
                {
                    _notificationService.ShowWarning("الموظف تخطى الحد المسموح به للوجبات اليومية.", "رفض العملية");
                    return;
                }
            }

            // NOTE: expense is tracked below inside the main checkout transaction context
            // Temporarily store expense data for use in the transaction block
            var pendingExpenseForMeal = new Expense
            {
                Description = $"ضيافة موظفين: {SelectedStaffMember.FullName}",
                Amount = totalCost,
                ExpenseDate = DateTime.Now,
                Category = ExpenseCategory.Other,
                Notes = $"وجبة ضيافة - رقم الفاتورة سيتم إرفاقه",
                UserId = _currentUser.Id
            };
            AmountPaid = 0;
        }
        else if (SelectedPaymentMethod == PaymentMethod.Deferred)
        {
            AmountPaid = 0;
            ChangeAmount = 0;
        }
        else
        {
            if (AmountPaid <= 0)
            {
                AmountPaid = TotalAmount;
                CalculateTotals();
            }

            if (AmountPaid < TotalAmount)
            {
                _notificationService.ShowWarning("المبلغ المدفوع أقل من الإجمالي!", "خطأ في الدفع");
                return;
            }
        }

        var activeShift = await _shiftRepository.GetActiveShiftByUserIdAsync(_currentUser.Id);

        if (activeShift == null)
        {
            var confirmResult = _notificationService.Confirm(
                "لا توجد وردية مفتوحة!\n\nلا يمكن إتمام البيع بدون وردية نشطة.\nهل تريد فتح وردية الآن؟",
                "تنبيه: وردية مطلوبة");

            if (confirmResult)
            {
                _notificationService.ShowInfo("يرجى الذهاب إلى صفحة 'إدارة الورديات' لفتح وردية جديدة.");
            }
            return;
        }

        try
        {
            IsProcessing = true;
            StatusMessage = "جاري تنفيذ عملية البيع...";

            // FIX: Create a single short-lived context for the entire checkout transaction.
            // This context is disposed after the transaction commits, preventing ChangeTracker accumulation.
            await using var checkoutContext = await _contextFactory.CreateDbContextAsync();
            await using var transaction = await checkoutContext.Database.BeginTransactionAsync();
            try
            {
                var sale = new Sale
                {
                InvoiceNumber = InvoiceNumber,
                SaleDate = DateTime.Now,
                Subtotal = Subtotal,
                DiscountAmount = DiscountAmount,
                TaxAmount = TaxAmount,
                TotalAmount = TotalAmount,
                AmountPaid = AmountPaid,
                ChangeAmount = ChangeAmount,
                PaymentMethod = SelectedPaymentMethod,
                OrderType = SelectedOrderType,
                TableId = (SelectedOrderType == OrderType.DineIn) ? SelectedTable?.Id : null,
                CustomerId = SelectedCustomer?.Id,
                ConsumedByUserId = (SelectedPaymentMethod == PaymentMethod.StaffMeal) ? SelectedStaffMember?.Id : null,
                ShiftId = activeShift.Id,
                Status = SaleStatus.Completed,
                UserId = _currentUser.Id,
                SaleDetails = new List<SaleDetail>()
            };

            if (SelectedPaymentMethod == PaymentMethod.Deferred && SelectedCustomer != null)
            {
                var customerToUpdate = await checkoutContext.Customers.FindAsync(SelectedCustomer.Id);
                if (customerToUpdate != null)
                {
                    customerToUpdate.CurrentDebt += TotalAmount;
                    checkoutContext.Customers.Update(customerToUpdate);
                }
            }

            // Add staff meal expense directly to checkout context
            if (SelectedPaymentMethod == PaymentMethod.StaffMeal && SelectedStaffMember != null)
            {
                var mealExpense = new Expense
                {
                    Description = $"ضيافة موظفين: {SelectedStaffMember.FullName}",
                    Amount = CartItems.Sum(i => i.UnitCost * i.Quantity),
                    ExpenseDate = DateTime.Now,
                    Category = ExpenseCategory.Other,
                    Notes = $"وجبة ضيافة - رقم الطلب: {InvoiceNumber}",
                    UserId = _currentUser.Id
                };
                checkoutContext.Expenses.Add(mealExpense);
            }

            foreach (var item in CartItems)
            {
                var saleDetail = new SaleDetail
                {
                    ProductId = item.ProductId,
                    Quantity = item.Quantity,
                    UnitPrice = item.UnitPrice,
                    UnitCost = item.UnitCost,
                    DiscountPercentage = item.DiscountPercentage,
                    DiscountAmount = item.DiscountAmount,
                    LineTotal = item.Total
                };
                sale.SaleDetails.Add(saleDetail);

                var product = await checkoutContext.Products.FindAsync(item.ProductId);
                if (product != null)
                {
                    // Atomic DB update to prevent lost-update concurrency issues
                    await checkoutContext.Products
                        .Where(p => p.Id == item.ProductId)
                        .ExecuteUpdateAsync(s => s.SetProperty(p => p.Stock, p => p.Stock - item.Quantity));

                    // Update memory for UI, but tell EF not to overwrite the atomic DB change
                    product.Stock -= item.Quantity;
                    checkoutContext.Entry(product).Property(x => x.Stock).IsModified = false;

                    var stockMovement = new StockMovement
                    {
                        ProductId = item.ProductId,
                        Quantity = -item.Quantity,
                        Type = MovementType.Sale,
                        Reference = InvoiceNumber,
                        MovementDate = DateTime.Now
                    };
                    checkoutContext.StockMovements.Add(stockMovement);
                }
            }

            checkoutContext.Sales.Add(sale);

            await checkoutContext.SaveChangesAsync();
            await transaction.CommitAsync();

            StatusMessage = "جاري الحفظ...";

            if (ShouldPrintReceipt)
            {
                StatusMessage = "جاري طباعة الإيصال...";
                await PrintReceipt(sale);
            }
            else if (_settingsService.AutoOpenDrawer)
            {
                // If not printing, but drawer should open, we manually kick it
                StatusMessage = "جاري فتح الدرج...";
                _printingService.OpenCashDrawer(_settingsService.CashDrawerPrinter ?? _settingsService.PrinterName, _settingsService.DrawerPin);
            }

            // ── Kitchen Ticket ────────────────────────────────────────────
            if (_settingsService.KitchenPrinterEnabled && !string.IsNullOrWhiteSpace(_settingsService.KitchenPrinterName))
            {
                var kitchenTicket = new KitchenTicketData
                {
                    OrderNumber = InvoiceNumber,
                    OrderType   = SelectedOrderType.ToString(),
                    TableName   = SelectedTable?.Name ?? "-",
                    CashierName = _currentUser.FullName,
                    OrderTime   = DateTime.Now,
                    Items = CartItems.Select(i => new KitchenTicketItem
                    {
                        ProductName = i.Name,
                        Quantity    = i.Quantity
                    }).ToList()
                };
                _ = _printingService.PrintKitchenTicketAsync(_settingsService.KitchenPrinterName, kitchenTicket);
            }

            StatusMessage = "تمت عملية البيع بنجاح!";
            _notificationService.ShowSuccess($"تمت عملية البيع بنجاح!\nرقم الفاتورة: {InvoiceNumber}\nالمتبقي للعميل: {ChangeAmount:F2} ج.م");

            // Manually clear without invoking the protected ClearCart command
            CartItems.Clear();
            BarcodeInput = string.Empty;
            Subtotal = 0;
            DiscountPercentage = 0;
            DiscountAmount = 0;
            TaxAmount = 0;
            TotalAmount = 0;
            AmountPaid = 0;
            ChangeAmount = 0;
            GenerateInvoiceNumber();
            StatusMessage = "جاهز";
        }
        catch (Exception)
        {
            await transaction.RollbackAsync();
            throw; // rethrow to the outer try/catch
        }
        }
        catch (Exception ex)
        {
            _notificationService.ShowError($"خطأ أثناء إتمام البيع: {ex.Message}");
            StatusMessage = "❌ فشل البيع";
        }
        finally
        {
            IsProcessing = false;
        }
    }

    [RelayCommand]
    private async Task ClearCart()
    {
        if (CartItems.Count > 0)
        {
            if (!_authService.HasPermission(Permissions.VoidItem))
            {
                var authorized = await _authService.RequestAdminOverrideAsync("إلغاء الفاتورة بالكامل (تفريغ السلة)");
                if (!authorized) return;
            }
        }

        CartItems.Clear();
        BarcodeInput = string.Empty;
        Subtotal = 0;
        DiscountPercentage = 0;
        DiscountAmount = 0;
        TaxAmount = 0;
        TotalAmount = 0;
        AmountPaid = 0;
        ChangeAmount = 0;
        GenerateInvoiceNumber();
        StatusMessage = "جاهز";
    }

    [RelayCommand]
    private async Task HoldSale()
    {
        if (!_authService.HasPermission(Permissions.HoldSale))
        {
            var authorized = await _authService.RequestAdminOverrideAsync("تعليق الفاتورة الحالية");
            if (!authorized) return;
        }

        MessageBox.Show("تم تعليق الفاتورة بنجاح!", "تعليق", MessageBoxButton.OK, MessageBoxImage.Information);
    }

    [RelayCommand]
    private async Task OpenCashDrawer()
    {
        if (!_authService.HasPermission(Permissions.OpenCashDrawer))
        {
            var authorized = await _authService.RequestAdminOverrideAsync("فتح درج النقدية يدوياً بدون فاتورة");
            if (!authorized) return;
        }

        // Use dedicated drawer printer if set, else fall back to receipt printer
        var drawerPrinter = !string.IsNullOrWhiteSpace(_settingsService.CashDrawerPrinter)
            ? _settingsService.CashDrawerPrinter
            : ResolvePrinterName();

        if (!string.IsNullOrWhiteSpace(drawerPrinter))
        {
            _printingService.OpenCashDrawer(drawerPrinter, _settingsService.DrawerPin);
            StatusMessage = "تم فتح درج النقدية";
            await _authService.LogAuditAsync("ManualDrawerOpen", "تم فتح الدرج يدوياً عن طريق الزر");
        }
        else
        {
            MessageBox.Show("لا توجد طابعة متصلة لفتح درج النقدية.\nيرجى ضبط الطابعة في الإعدادات.",
                "تنبيه", MessageBoxButton.OK, MessageBoxImage.Warning);
        }
    }

    private void GenerateInvoiceNumber()
    {
        InvoiceNumber = $"INV-{DateTime.Now:yyyyMMdd}-{DateTime.Now.Ticks % 10000:D4}";
    }

    // --- Printers ---
    private async Task PrintReceipt(Sale sale)
    {
        string paymentMethodArabic = sale.PaymentMethod switch
        {
            PaymentMethod.Cash => "كاش",
            PaymentMethod.Card => "فيزا / بطاقة",
            PaymentMethod.VodafoneCash => "فودافون كاش",
            PaymentMethod.InstaPay => "انستا باي",
            PaymentMethod.BankTransfer => "تحويل بنكي",
            PaymentMethod.MobileMoney => "محفظة إلكترونية",
            PaymentMethod.Split => "دفع مقسم",
            _ => sale.PaymentMethod.ToString()
        };

        var receiptData = new ReceiptData
        {
            StoreName     = _settingsService.StoreName    ?? "Smart POS",
            StoreAddress  = _settingsService.StoreAddress ?? "",
            Phone         = _settingsService.StorePhone   ?? "",
            InvoiceNumber = sale.InvoiceNumber,
            SaleDate      = sale.SaleDate,
            CashierName   = _currentUser.FullName,
            CustomerName  = sale.Customer?.Name ?? "",
            Items = CartItems.Select(i => new ReceiptItem
            {
                Name      = i.Name,
                Quantity  = i.Quantity,
                UnitPrice = i.UnitPrice,
                Total     = i.Total
            }).ToList(),
            Subtotal       = sale.Subtotal,
            DiscountAmount = sale.DiscountAmount,
            TaxAmount      = sale.TaxAmount,
            TotalAmount    = sale.TotalAmount,
            AmountPaid     = sale.AmountPaid,
            ChangeAmount   = sale.ChangeAmount,
            PaymentMethod  = paymentMethodArabic,
            Footer         = _settingsService.FooterMessage ?? "شكراً لزيارتكم"
        };

        var printerName = ResolvePrinterName();
        if (!string.IsNullOrEmpty(printerName))
        {
            bool openDrawer = sale.PaymentMethod == PaymentMethod.Cash;
            await _printingService.PrintReceiptAsync(
                printerName,
                receiptData,
                _settingsService.ReceiptWidth,
                _settingsService.ReceiptLanguage,
                _settingsService.DrawerPin,
                openDrawer);

            // Open drawer after print if auto-open is enabled and payment is cash
            if (_settingsService.AutoOpenDrawer && sale.PaymentMethod == PaymentMethod.Cash)
            {
                var drawerPrinter = !string.IsNullOrWhiteSpace(_settingsService.CashDrawerPrinter)
                    ? _settingsService.CashDrawerPrinter
                    : printerName;
                // Drawer is already opened inside ESC/POS receipt stream; this is a belt-and-suspenders call
                // for printers that use GDI fallback path
            }
        }
        else
        {
            MessageBox.Show("لا توجد طابعة حرارية متصلة.\nيرجى ضبط الطابعة في الإعدادات.",
                "خطأ في الطباعة", MessageBoxButton.OK, MessageBoxImage.Warning);
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

        // 1. Check user's preferred printer (only if it's not a virtual/PDF printer)
        var preferred = _settingsService.PrinterName;
        if (!string.IsNullOrWhiteSpace(preferred) && !IsVirtualPrinter(preferred))
        {
            var match = printers.FirstOrDefault(p => string.Equals(p, preferred, StringComparison.OrdinalIgnoreCase));
            if (!string.IsNullOrWhiteSpace(match)) return match;
        }

        // 2. Try the system default printer (only if not virtual)
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

        // 3. Fallback: first NON-virtual printer in the list
        var nonVirtual = printers.FirstOrDefault(p => !IsVirtualPrinter(p));
        if (!string.IsNullOrWhiteSpace(nonVirtual)) return nonVirtual;

        // 4. No physical printer found — return null to trigger warning dialog
        return null;
    }

    // ──────────────────────────── 1. Parked Orders (تعليق واسترجاع الفواتير) ────────────────────────────
    [RelayCommand]
    private void ParkCurrentOrder()
    {
        if (CartItems.Count == 0)
        {
            _notificationService.ShowWarning("السلة فارغة! لا يمكن تعليق طلب فارغ.");
            _soundService?.PlayWarningAlert();
            return;
        }

        var order = new SmartPOS.Application.DTOs.ParkedOrder
        {
            OrderNumber = string.IsNullOrWhiteSpace(InvoiceNumber) ? $"ORD-{DateTime.Now:HHmmss}" : InvoiceNumber,
            ParkedAt = DateTime.Now,
            Items = new List<CartItem>(CartItems),
            Customer = SelectedCustomer,
            TotalAmount = TotalAmount,
            Notes = SelectedTable != null ? $"طاولة: {SelectedTable.Name}" : string.Empty
        };

        ParkedOrders.Insert(0, order);
        ParkedOrdersCount = ParkedOrders.Count;

        // Clear current cart
        CartItems.Clear();
        SelectedCustomer = null;
        DiscountPercentage = 0;
        DiscountAmount = 0;
        AmountPaid = 0;
        CalculateTotals();
        GenerateInvoiceNumber();

        _soundService?.PlayWarningAlert();
        _notificationService.ShowSuccess($"✅ تم تعليق الفاتورة #{order.OrderNumber} (العدد المعلق: {ParkedOrdersCount})");
    }

    [RelayCommand]
    private void RecallParkedOrder(SmartPOS.Application.DTOs.ParkedOrder? order)
    {
        if (order == null) return;

        // If current cart has items, ask or auto-park
        if (CartItems.Count > 0)
        {
            var res = MessageBox.Show("السلة الحالية بها أصناف. هل تريد تعليق السلة الحالية قبل استرجاع الفاتورة المعلقة؟",
                "استرجاع الفاتورة المعلقة", MessageBoxButton.YesNoCancel, MessageBoxImage.Question);
            if (res == MessageBoxResult.Cancel) return;
            if (res == MessageBoxResult.Yes)
            {
                ParkCurrentOrder();
            }
            else
            {
                CartItems.Clear();
            }
        }

        CartItems = new ObservableCollection<CartItem>(order.Items);
        SelectedCustomer = order.Customer;
        ParkedOrders.Remove(order);
        ParkedOrdersCount = ParkedOrders.Count;
        IsParkedOrdersOpen = false;

        CalculateTotals();
        _soundService?.PlayBarcodeBeep();
        _notificationService.ShowSuccess($"✅ تم استرجاع الفاتورة #{order.OrderNumber}");
    }

    [RelayCommand]
    private void DeleteParkedOrder(SmartPOS.Application.DTOs.ParkedOrder? order)
    {
        if (order == null) return;
        ParkedOrders.Remove(order);
        ParkedOrdersCount = ParkedOrders.Count;
        _notificationService.ShowInfo($"تم حذف الفاتورة المعلقة #{order.OrderNumber}");
    }

    [RelayCommand]
    private void ToggleParkedOrdersPopup()
    {
        IsParkedOrdersOpen = !IsParkedOrdersOpen;
        _soundService?.PlayClick();
    }

    // ──────────────────────────── 2. Touch Numpad (لوحة الأرقام اللمسية) ────────────────────────────
    [RelayCommand]
    private void ToggleTouchNumpad()
    {
        IsTouchNumpadVisible = !IsTouchNumpadVisible;
        if (IsTouchNumpadVisible && SelectedCartItem != null)
        {
            NumpadInput = SelectedCartItem.Quantity.ToString();
        }
        _soundService?.PlayClick();
    }

    [RelayCommand]
    private void NumpadDigit(string digit)
    {
        if (NumpadInput.Length >= 10) return;

        if (digit == "." || digit == ",")
        {
            if (string.IsNullOrEmpty(NumpadInput))
            {
                NumpadInput = "0.";
            }
            else if (!NumpadInput.Contains("."))
            {
                NumpadInput += ".";
            }
        }
        else
        {
            if (NumpadInput == "0" && digit != "0")
                NumpadInput = digit;
            else
                NumpadInput += digit;
        }
        _soundService?.PlayClick();
    }

    [RelayCommand]
    private void NumpadClear()
    {
        NumpadInput = string.Empty;
        _soundService?.PlayClick();
    }

    [RelayCommand]
    private void NumpadBackspace()
    {
        if (NumpadInput.Length > 0)
        {
            NumpadInput = NumpadInput[..^1];
            _soundService?.PlayClick();
        }
    }

    [RelayCommand]
    private void NumpadApplyQty()
    {
        if (SelectedCartItem == null)
        {
            _notificationService.ShowWarning("يرجى اختيار صنف من السلة أولاً");
            return;
        }

        var clean = NumpadInput.Replace(',', '.').Trim();
        if (decimal.TryParse(clean, System.Globalization.NumberStyles.Any, System.Globalization.CultureInfo.InvariantCulture, out var parsedQty) && parsedQty > 0)
        {
            var qty = (int)Math.Max(1, Math.Round(parsedQty));
            if (qty <= SelectedCartItem.AvailableStock)
            {
                SelectedCartItem.Quantity = qty;
                OnPropertyChanged(nameof(CartItems));
                CalculateTotals();
                NumpadInput = string.Empty;
                IsTouchNumpadVisible = false;
                _soundService?.PlayBarcodeBeep();
            }
            else
            {
                _notificationService.ShowWarning($"الكمية المطلوبة تتجاوز المخزون المتاح ({SelectedCartItem.AvailableStock})");
                _soundService?.PlayWarningAlert();
            }
        }
    }

    [RelayCommand]
    private void NumpadApplyPrice()
    {
        if (SelectedCartItem == null)
        {
            _notificationService.ShowWarning("يرجى اختيار صنف من السلة أولاً");
            return;
        }

        var clean = NumpadInput.Replace(',', '.').Trim();
        if (decimal.TryParse(clean, System.Globalization.NumberStyles.Any, System.Globalization.CultureInfo.InvariantCulture, out var price) && price >= 0)
        {
            SelectedCartItem.UnitPrice = price;
            OnPropertyChanged(nameof(CartItems));
            CalculateTotals();
            NumpadInput = string.Empty;
            IsTouchNumpadVisible = false;
            _soundService?.PlayBarcodeBeep();
            _notificationService.ShowSuccess($"✅ تم تحديث السعر إلى {price:N2} {CurrencySymbol}");
        }
        else
        {
            _notificationService.ShowWarning("يرجى إدخال سعر صحيح (مثال: 7.5 أو 12.50)");
        }
    }

    [RelayCommand]
    private void NumpadAddQuickQty(string deltaStr)
    {
        if (SelectedCartItem == null)
        {
            _notificationService.ShowWarning("يرجى اختيار صنف من السلة أولاً");
            return;
        }

        if (int.TryParse(deltaStr, out var delta))
        {
            var targetQty = Math.Max(1, SelectedCartItem.Quantity + delta);
            if (targetQty <= SelectedCartItem.AvailableStock)
            {
                SelectedCartItem.Quantity = targetQty;
                OnPropertyChanged(nameof(CartItems));
                CalculateTotals();
                _soundService?.PlayClick();
            }
            else
            {
                _notificationService.ShowWarning($"المخزون المتاح: {SelectedCartItem.AvailableStock}");
                _soundService?.PlayWarningAlert();
            }
        }
    }

    // ──────────────────────────── 3. Quick Discounts (الخصم السريع) ────────────────────────────
    [RelayCommand]
    private void ApplyQuickPercentDiscount(string pctStr)
    {
        if (decimal.TryParse(pctStr, System.Globalization.NumberStyles.Any, System.Globalization.CultureInfo.InvariantCulture, out var pct))
        {
            DiscountPercentage = pct;
            DiscountAmount = Subtotal * (pct / 100m);
            CalculateTotals();
            _soundService?.PlayClick();
            _notificationService.ShowSuccess($"✅ تم تطبيق خصم {pct}% على الفاتورة");
        }
    }

    [RelayCommand]
    private void ApplyQuickFixedDiscount(string amtStr)
    {
        if (decimal.TryParse(amtStr, System.Globalization.NumberStyles.Any, System.Globalization.CultureInfo.InvariantCulture, out var amt))
        {
            DiscountAmount = Math.Min(Subtotal, amt);
            DiscountPercentage = Subtotal > 0 ? Math.Round((DiscountAmount / Subtotal) * 100, 2) : 0;
            CalculateTotals();
            _soundService?.PlayClick();
            _notificationService.ShowSuccess($"✅ تم تطبيق خصم بقيمة {amt:N0} ج.م");
        }
    }

    [RelayCommand]
    private void ClearDiscount()
    {
        DiscountPercentage = 0;
        DiscountAmount = 0;
        CalculateTotals();
        _soundService?.PlayClick();
    }

    // ──────────────────────────── 4. Fast Customer & Loyalty (العميل والولاء) ────────────────────────────
    [RelayCommand]
    private async Task SearchCustomer(string query)
    {
        CustomerSearchText = query;
        if (string.IsNullOrWhiteSpace(query))
        {
            FilteredCustomers = new ObservableCollection<Customer>(AllCustomers.Take(20));
            IsCustomerDropdownOpen = false;
            return;
        }

        var q = query.Trim().ToLowerInvariant();
        var matches = AllCustomers.Where(c => 
            (c.Name != null && c.Name.ToLowerInvariant().Contains(q)) || 
            (c.Phone != null && c.Phone.Contains(q)))
            .Take(15)
            .ToList();

        FilteredCustomers = new ObservableCollection<Customer>(matches);
        IsCustomerDropdownOpen = matches.Count > 0;
    }

    [RelayCommand]
    private async Task SelectCustomer(Customer? customer)
    {
        SelectedCustomer = customer;
        IsCustomerDropdownOpen = false;
        if (customer != null)
        {
            CustomerSearchText = $"{customer.Name} ({customer.Phone})";
            // Calculate loyalty points
            try
            {
                await using var ctx = await _contextFactory.CreateDbContextAsync();
                var loyalty = await ctx.CustomerLoyalties.AsNoTracking().FirstOrDefaultAsync(l => l.CustomerId == customer.Id);
                CustomerLoyaltyPoints = loyalty?.Points ?? 0;
                CustomerLoyaltyCashValue = Math.Round(CustomerLoyaltyPoints * 0.05m, 2); // 1 point = 0.05 EGP
            }
            catch { }
            _soundService?.PlayClick();
            _notificationService.ShowSuccess($"تم تحديد العميل: {customer.Name}");
        }
    }

    [RelayCommand]
    private void ClearSelectedCustomer()
    {
        SelectedCustomer = null;
        CustomerSearchText = string.Empty;
        CustomerLoyaltyPoints = 0;
        CustomerLoyaltyCashValue = 0;
        IsCustomerDropdownOpen = false;
        _soundService?.PlayClick();
    }

    [RelayCommand]
    private void RedeemLoyaltyPoints()
    {
        if (SelectedCustomer == null || CustomerLoyaltyPoints <= 0)
        {
            _notificationService.ShowWarning("لا يوجد رصيد نقاط كافٍ للاستبدال.");
            return;
        }

        if (CustomerLoyaltyCashValue > 0)
        {
            DiscountAmount += CustomerLoyaltyCashValue;
            if (Subtotal > 0)
                DiscountPercentage = Math.Round((DiscountAmount / Subtotal) * 100, 2);
            CalculateTotals();
            _soundService?.PlayBarcodeBeep();
            _notificationService.ShowSuccess($"✅ تم استبدال {CustomerLoyaltyPoints:N0} نقطة بخصم {CustomerLoyaltyCashValue:N2} ج.م!");
            CustomerLoyaltyPoints = 0;
            CustomerLoyaltyCashValue = 0;
        }
    }

    // ──────────────────────────── 5. Grid vs Compact List View (التبديل بين الشبكة والقائمة) ────────────────────────────
    [RelayCommand]
    private void ToggleCompactListView()
    {
        IsCompactListView = !IsCompactListView;
        _soundService?.PlayClick();
    }
}
