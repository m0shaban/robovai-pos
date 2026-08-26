using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.Messaging;
using System.Windows;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.EntityFrameworkCore;
using SmartPOS.Application.Extensions;
using SmartPOS.Core.Entities;
using SmartPOS.Infrastructure.Data;

namespace SmartPOS.Application.ViewModels;

/// <summary>
/// View model for managing returns. (v5.1 Factory Pattern applied)
/// </summary>
public partial class ReturnsViewModel : BaseViewModel, IDisposable, CommunityToolkit.Mvvm.Messaging.IRecipient<SmartPOS.Application.Messages.BarcodeScannedMessage>
{
    private readonly IDbContextFactory<AppDbContext> _contextFactory;
    private readonly User _currentUser;
    private readonly SmartPOS.Core.Interfaces.IAuthorizationService _authService;

    [ObservableProperty]
    private ObservableCollection<Return> _returns = new();

    private List<Return> _allReturns = new();

    [ObservableProperty]
    private string _searchText = string.Empty;

    [ObservableProperty]
    private ObservableCollection<Sale> _sales = new();

    [ObservableProperty]
    private Sale? _selectedSale;

    [ObservableProperty]
    private ReturnReason _selectedReason = ReturnReason.CustomerRequest;

    [ObservableProperty]
    private string? _notesInput;

    [ObservableProperty]
    private int _totalReturnsCount;

    [ObservableProperty]
    private int _pendingReturnsCount;

    [ObservableProperty]
    private int _approvedReturnsCount;

    [ObservableProperty]
    private decimal _totalReturnsAmount;

    public ObservableCollection<ReturnReason> Reasons { get; } = new(Enum.GetValues<ReturnReason>());

    public ReturnsViewModel(IDbContextFactory<AppDbContext> contextFactory, User currentUser, SmartPOS.Core.Interfaces.IAuthorizationService authService)
    {
        _contextFactory = contextFactory;
        _currentUser = currentUser;
        _authService = authService;
        
        CommunityToolkit.Mvvm.Messaging.WeakReferenceMessenger.Default.RegisterAll(this);

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
            if (string.IsNullOrWhiteSpace(message.Value)) return;
            var scanned = message.Value.Trim();
            var foundSale = Sales.FirstOrDefault(s => s.InvoiceNumber.Equals(scanned, StringComparison.OrdinalIgnoreCase));
            if (foundSale != null)
            {
                SelectedSale = foundSale;
            }
            else
            {
                // Search directly in DB for older invoices
                try
                {
                    await using var ctx = await _contextFactory.CreateDbContextAsync();
                    var dbSale = await ctx.Sales
                        .AsNoTracking()
                        .Include(s => s.Customer)
                        .Include(s => s.SaleDetails)
                        .ThenInclude(sd => sd.Product)
                        .FirstOrDefaultAsync(s => s.InvoiceNumber == scanned && !s.IsDeleted);

                    if (dbSale != null)
                    {
                        Sales.Insert(0, dbSale);
                        SelectedSale = dbSale;
                    }
                    else
                    {
                        SearchText = scanned;
                    }
                }
                catch { }
            }
        });
    }

    private async Task InitializeAsync()
    {
        await ExecuteBusyAsync(LoadCoreAsync, "⏳ جاري تحميل المرتجعات...", $"✅ تم تحميل {Returns.Count} مرتجع");
    }

        [ObservableProperty]
        private ReturnStatus? _filterStatus;

        private async Task LoadCoreAsync()
        {
            await using var ctx = await _contextFactory.CreateDbContextAsync();
            var sales = await ctx.Sales
                .AsNoTracking()
                .Include(s => s.Customer)
                .Include(s => s.SaleDetails)
                .ThenInclude(sd => sd.Product)
                .Where(s => !s.IsDeleted)
                .OrderByDescending(s => s.SaleDate)
                .Take(100)
                .ToListAsync();
                
            Sales.SyncWith(sales);

            var returns = await ctx.Returns
                .AsNoTracking()
                .Include(r => r.Customer)
                .Include(r => r.Sale)
                .Include(r => r.ReturnDetails)
                .ThenInclude(rd => rd.Product)
                .Where(r => !r.IsDeleted)
                .OrderByDescending(r => r.ReturnDate)
                .Take(200)
                .ToListAsync();

            _allReturns = returns;
            FilterReturns();

            TotalReturnsCount = await ctx.Returns.CountAsync(r => !r.IsDeleted);
            PendingReturnsCount = await ctx.Returns.CountAsync(r => r.Status == ReturnStatus.Pending && !r.IsDeleted);
            ApprovedReturnsCount = await ctx.Returns.CountAsync(r => r.Status == ReturnStatus.Approved && !r.IsDeleted);
            
            var approvedReturns = await ctx.Returns.Where(r => r.Status == ReturnStatus.Approved && !r.IsDeleted).ToListAsync();
            TotalReturnsAmount = approvedReturns.Sum(r => r.TotalAmount);
        }

        partial void OnSearchTextChanged(string value) => FilterReturns();

        private void FilterReturns()
        {
            var query = _allReturns.AsEnumerable();

            if (FilterStatus.HasValue)
            {
                query = query.Where(r => r.Status == FilterStatus.Value);
            }

            if (!string.IsNullOrWhiteSpace(SearchText))
            {
                query = query.Where(r =>
                    r.ReturnNumber.Contains(SearchText, StringComparison.OrdinalIgnoreCase) ||
                    (r.Sale?.InvoiceNumber?.Contains(SearchText, StringComparison.OrdinalIgnoreCase) ?? false) ||
                    (r.Customer?.Name?.Contains(SearchText, StringComparison.OrdinalIgnoreCase) ?? false));
            }

            Returns.SyncWith(query);
        }

        [RelayCommand]
        private void FilterAll() { FilterStatus = null; FilterReturns(); }

        [RelayCommand]
        private void FilterPending() { FilterStatus = ReturnStatus.Pending; FilterReturns(); }

        [RelayCommand]
        private void FilterApproved() { FilterStatus = ReturnStatus.Approved; FilterReturns(); }

        [RelayCommand]
        private void FilterRejected() { FilterStatus = ReturnStatus.Rejected; FilterReturns(); }

    [RelayCommand]
    private async Task LoadAsync()
    {
        await ExecuteBusyAsync(LoadCoreAsync, "جاري تحديث المرتجعات...");
    }

    [RelayCommand]
    private async Task CreateReturnAsync()
    {
        if (SelectedSale == null)
        {
            MessageBox.Show("اختر فاتورة أولاً", "تنبيه", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        await ExecuteBusyAsync(async () =>
        {
            await using var ctx = await _contextFactory.CreateDbContextAsync();
            var existingReturn = await ctx.Returns.FirstOrDefaultAsync(r => r.SaleId == SelectedSale.Id && r.Status != ReturnStatus.Rejected && !r.IsDeleted);
            if (existingReturn != null)
            {
                MessageBox.Show("عذراً، تم إنشاء مرتجع لهذه الفاتورة مسبقاً.", "فاتورة مسترجعة", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            var saleWithDetails = await ctx.Sales
                .Include(s => s.SaleDetails)
                .FirstOrDefaultAsync(s => s.Id == SelectedSale.Id);

            if (saleWithDetails == null) return;

            var returnEntity = new Return
            {
                ReturnNumber = $"RET-{DateTime.Now:yyyyMMddHHmmss}",
                ReturnDate = DateTime.Now,
                SaleId = saleWithDetails.Id,
                CustomerId = saleWithDetails.CustomerId ?? 1,
                ProcessedByUserId = _currentUser.Id,
                TotalAmount = saleWithDetails.TotalAmount,
                Reason = SelectedReason,
                Notes = NotesInput,
                Status = ReturnStatus.Pending,
                IsRefunded = false,
                CreatedAt = DateTime.Now,
                ReturnDetails = new List<ReturnDetail>()
            };

            foreach (var detail in saleWithDetails.SaleDetails)
            {
                returnEntity.ReturnDetails.Add(new ReturnDetail
                {
                    ProductId = detail.ProductId,
                    Quantity = detail.Quantity,
                    UnitPrice = detail.UnitPrice,
                    Subtotal = detail.LineTotal,
                    Reason = SelectedReason.ToString(),
                    CreatedAt = DateTime.Now
                });
            }

            ctx.Returns.Add(returnEntity);
            await ctx.SaveChangesAsync();

            NotesInput = string.Empty;
            await LoadCoreAsync();

            MessageBox.Show("تم إنشاء المرتجع بنجاح", "نجح", MessageBoxButton.OK, MessageBoxImage.Information);
        }, "جاري إنشاء المرتجع...");
    }

    [RelayCommand]
    private async Task ApproveReturnAsync(Return? returnEntity)
    {
        if (returnEntity == null) return;

        bool authorized = await _authService.RequestAdminOverrideAsync("الموافقة على المرتجعات واسترداد الأموال");
        if (!authorized) return;

        await ExecuteBusyAsync(async () =>
        {
            await using var ctx = await _contextFactory.CreateDbContextAsync();
            await using var transaction = await ctx.Database.BeginTransactionAsync();
            try
            {
                var ret = await ctx.Returns
                    .Include(r => r.ReturnDetails)
                    .Include(r => r.Sale)           // Need sale to check payment method
                    .FirstOrDefaultAsync(r => r.Id == returnEntity.Id);

                if (ret != null && ret.Status == ReturnStatus.Pending)
                {
                ret.Status = ReturnStatus.Approved;
                ret.IsRefunded = true;
                ret.RefundDate = DateTime.Now;

                // Mark the original sale as Refunded
                var originalSale = await ctx.Sales.FindAsync(ret.SaleId);
                if (originalSale != null)
                    originalSale.Status = SaleStatus.Refunded;

                // Find current open shift for this user
                var currentShift = await ctx.Shifts
                    .FirstOrDefaultAsync(s => s.UserId == _currentUser.Id && s.Status == ShiftStatus.Open);

                var refundMethod = ret.Sale?.PaymentMethod ?? PaymentMethod.Cash;
                decimal appliedToDebt = 0;
                decimal cashToRefund = ret.TotalAmount;

                if (refundMethod == PaymentMethod.Deferred && ret.CustomerId > 1)
                {
                    var customer = await ctx.Customers.FindAsync(ret.CustomerId);
                    if (customer != null)
                    {
                        appliedToDebt = Math.Min(customer.CurrentDebt, ret.TotalAmount);
                        cashToRefund = ret.TotalAmount - appliedToDebt;
                        customer.CurrentDebt -= appliedToDebt;
                    }
                }

                // If it's a normal cash/card sale, cashToRefund = TotalAmount, appliedToDebt = 0.
                if (refundMethod != PaymentMethod.Deferred)
                {
                    cashToRefund = ret.TotalAmount;
                    appliedToDebt = 0;
                }

                var negativeSales = new List<Sale>();

                if (appliedToDebt > 0)
                {
                    negativeSales.Add(new Sale
                    {
                        InvoiceNumber = $"REF-DEF-{ret.ReturnNumber}",
                        SaleDate = DateTime.Now,
                        Subtotal = -appliedToDebt,
                        TotalAmount = -appliedToDebt,
                        AmountPaid = -appliedToDebt,
                        ChangeAmount = 0,
                        PaymentMethod = PaymentMethod.Deferred,
                        UserId = _currentUser.Id,
                        CustomerId = ret.CustomerId > 1 ? ret.CustomerId : null,
                        ShiftId = currentShift?.Id,
                        Status = SaleStatus.Refunded,
                        Notes = $"تسوية دين - مرتجع {ret.ReturnNumber}",
                        SaleDetails = new List<SaleDetail>()
                    });
                }

                if (cashToRefund > 0)
                {
                    negativeSales.Add(new Sale
                    {
                        InvoiceNumber = $"REF-CSH-{ret.ReturnNumber}",
                        SaleDate = DateTime.Now,
                        Subtotal = -cashToRefund,
                        TotalAmount = -cashToRefund,
                        AmountPaid = -cashToRefund,
                        ChangeAmount = 0,
                        PaymentMethod = refundMethod == PaymentMethod.Deferred ? PaymentMethod.Cash : refundMethod,
                        UserId = _currentUser.Id,
                        CustomerId = ret.CustomerId > 1 ? ret.CustomerId : null,
                        ShiftId = currentShift?.Id,
                        Status = SaleStatus.Refunded,
                        Notes = $"استرداد نقدي/بطاقة - مرتجع {ret.ReturnNumber}",
                        SaleDetails = new List<SaleDetail>()
                    });
                }

                foreach (var detail in ret.ReturnDetails)
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
                            ProductId = detail.ProductId,
                            Quantity = detail.Quantity,
                            Type = MovementType.Return,
                            Reference = ret.ReturnNumber,
                            MovementDate = DateTime.Now
                        });

                        foreach (var ns in negativeSales)
                        {
                            // We distribute the products across the negative sales proportionally?
                            // Actually, just add the returned items to the FIRST negative sale to keep stock movement linked,
                            // since the SaleDetails just act as a record for the return.
                            if (ns == negativeSales.First())
                            {
                                ns.SaleDetails.Add(new SaleDetail
                                {
                                    ProductId = detail.ProductId,
                                    Quantity = -detail.Quantity,
                                    UnitPrice = detail.UnitPrice,
                                    LineTotal = -detail.Subtotal
                                });
                            }
                        }
                    }
                }

                ctx.Sales.AddRange(negativeSales);
                await ctx.SaveChangesAsync();
                await LoadCoreAsync();

                string msg = "";
                if (appliedToDebt > 0 && cashToRefund == 0)
                    msg = "تم قبول المرتجع وخصم كامل المبلغ من دين العميل وإعادة المنتجات للمخزون.";
                else if (appliedToDebt > 0 && cashToRefund > 0)
                    msg = $"تم قبول المرتجع. تم خصم {appliedToDebt:N2} من دين العميل، وإرجاع نقدي بقيمة {cashToRefund:N2} من الدرج.";
                else
                    msg = "تم قبول المرتجع وإعادة المنتجات للمخزون وخصم المبلغ من الدرج بنجاح.";
                MessageBox.Show(msg, "نجح", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            
            await transaction.CommitAsync();
            }
            catch (Exception)
            {
                await transaction.RollbackAsync();
                throw;
            }
        }, "جاري قبول المرتجع...");
    }

    [RelayCommand]
    private async Task RejectReturnAsync(Return? returnEntity)
    {
        if (returnEntity == null) return;

        bool authorized = await _authService.RequestAdminOverrideAsync("رفض طلب المرتجع");
        if (!authorized) return;

        await ExecuteBusyAsync(async () =>
        {
            await using var ctx = await _contextFactory.CreateDbContextAsync();
            var ret = await ctx.Returns.FindAsync(returnEntity.Id);
            if (ret != null && ret.Status == ReturnStatus.Pending)
            {
                ret.Status = ReturnStatus.Rejected;
                await ctx.SaveChangesAsync();
                await LoadCoreAsync();
                MessageBox.Show("تم رفض المرتجع بنجاح.", "نجح", MessageBoxButton.OK, MessageBoxImage.Information);
            }
        }, "جاري رفض المرتجع...");
    }
}
