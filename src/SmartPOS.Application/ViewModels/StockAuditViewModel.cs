using System.Collections.ObjectModel;
using System.Windows;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using Microsoft.EntityFrameworkCore;
using SmartPOS.Core.Entities;
using SmartPOS.Infrastructure.Data;
using SmartPOS.Infrastructure.Services;

namespace SmartPOS.Application.ViewModels;

public partial class StockAuditItemViewModel : ObservableObject
{
    public int ProductId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Barcode { get; set; } = string.Empty;
    public string CategoryName { get; set; } = string.Empty;
    public decimal UnitPrice { get; set; }
    public int SystemQuantity { get; set; }

    [ObservableProperty]
    private int _physicalQuantity;

    public int Variance => PhysicalQuantity - SystemQuantity;

    public decimal VarianceValue => Variance * UnitPrice;

    public string StatusText => Variance switch
    {
        0 => "مطابق ✔",
        > 0 => $"زيادة (+{Variance})",
        < 0 => $"عجز ({Variance})"
    };

    public string StatusColor => Variance switch
    {
        0 => "#10B981",
        > 0 => "#3B82F6",
        < 0 => "#EF4444"
    };

    partial void OnPhysicalQuantityChanged(int value)
    {
        OnPropertyChanged(nameof(Variance));
        OnPropertyChanged(nameof(VarianceValue));
        OnPropertyChanged(nameof(StatusText));
        OnPropertyChanged(nameof(StatusColor));
    }
}

public partial class StockAuditViewModel : BaseViewModel, IDisposable, CommunityToolkit.Mvvm.Messaging.IRecipient<SmartPOS.Application.Messages.BarcodeScannedMessage>
{
    private readonly IDbContextFactory<AppDbContext> _contextFactory;
    private readonly TelegramBotService? _telegramBot;
    private readonly User _currentUser;

    private List<StockAuditItemViewModel> _allItems = new();

    [ObservableProperty]
    private ObservableCollection<StockAuditItemViewModel> _auditItems = new();

    [ObservableProperty]
    private ObservableCollection<Category> _categories = new();

    [ObservableProperty]
    private Category? _selectedCategory;

    [ObservableProperty]
    private string _searchText = string.Empty;

    [ObservableProperty]
    private string _scannedBarcode = string.Empty;

    [ObservableProperty]
    private int _totalItemsCount;

    [ObservableProperty]
    private int _matchedCount;

    [ObservableProperty]
    private int _shortageCount;

    [ObservableProperty]
    private int _surplusCount;

    [ObservableProperty]
    private decimal _totalVarianceValue;

    public StockAuditViewModel(
        IDbContextFactory<AppDbContext> contextFactory,
        User currentUser,
        TelegramBotService? telegramBot = null)
    {
        _contextFactory = contextFactory;
        _currentUser = currentUser;
        _telegramBot = telegramBot;

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
            if (string.IsNullOrWhiteSpace(message.Value)) return;
            ScannedBarcode = message.Value;
            QuickScanBarcode();
        });
    }

    private async Task InitializeAsync()
    {
        await ExecuteBusyAsync(LoadDataAsync, "⏳ جاري تحميل بيانات الجرد...", "✅ تم التحميل");
    }

    private async Task LoadDataAsync()
    {
        await using var ctx = await _contextFactory.CreateDbContextAsync();

        var categoriesList = await ctx.Categories.AsNoTracking().ToListAsync();
        Categories = new ObservableCollection<Category>(categoriesList);

        var products = await ctx.Products
            .Include(p => p.Category)
            .AsNoTracking()
            .ToListAsync();

        _allItems = products.Select(p => new StockAuditItemViewModel
        {
            ProductId = p.Id,
            Name = p.Name,
            Barcode = p.Barcode,
            CategoryName = p.Category?.Name ?? "بدون قسم",
            UnitPrice = p.SellingPrice,
            SystemQuantity = p.Stock,
            PhysicalQuantity = p.Stock
        }).ToList();

        ApplyFilter();
    }

    partial void OnSearchTextChanged(string value) => ApplyFilter();
    partial void OnSelectedCategoryChanged(Category? value) => ApplyFilter();

    private void ApplyFilter()
    {
        var filtered = _allItems.AsEnumerable();

        if (!string.IsNullOrWhiteSpace(SearchText))
        {
            var query = SearchText.Trim().ToLower();
            filtered = filtered.Where(x => x.Name.ToLower().Contains(query) || x.Barcode.Contains(query));
        }

        if (SelectedCategory != null)
        {
            filtered = filtered.Where(x => x.CategoryName == SelectedCategory.Name);
        }

        AuditItems = new ObservableCollection<StockAuditItemViewModel>(filtered);
        UpdateStatistics();
    }

    private void UpdateStatistics()
    {
        TotalItemsCount = _allItems.Count;
        MatchedCount = _allItems.Count(x => x.Variance == 0);
        ShortageCount = _allItems.Count(x => x.Variance < 0);
        SurplusCount = _allItems.Count(x => x.Variance > 0);
        TotalVarianceValue = _allItems.Sum(x => x.VarianceValue);
    }

    [RelayCommand]
    private void QuickScanBarcode()
    {
        if (string.IsNullOrWhiteSpace(ScannedBarcode)) return;

        var code = ScannedBarcode.Trim();
        var item = _allItems.FirstOrDefault(x => x.Barcode == code);
        if (item != null)
        {
            item.PhysicalQuantity++;
            UpdateStatistics();
            ScannedBarcode = string.Empty;
        }
        else
        {
            MessageBox.Show($"❌ الصنف صاحب الباركود [{code}] غير موجود بالمخزن.", "تنبيـه", MessageBoxButton.OK, MessageBoxImage.Warning);
        }
    }

    [RelayCommand]
    private async Task ApplyAdjustmentAsync()
    {
        var changedItems = _allItems.Where(x => x.Variance != 0).ToList();
        if (!changedItems.Any())
        {
            MessageBox.Show("لا توجد أي فروقات بين الكمية الفعلية وكمية النظام.", "تسوية الجرد", MessageBoxButton.OK, MessageBoxImage.Information);
            return;
        }

        var confirm = MessageBox.Show(
            $"هل أنت تأكد من تطبيق تسوية الجرد لـ ({changedItems.Count}) صنف؟\nسيتم تحديث رصيد المخزن بالنظام مباشرة.",
            "تأكيد تسوية الجرد",
            MessageBoxButton.YesNo,
            MessageBoxImage.Question);

        if (confirm != MessageBoxResult.Yes) return;

        await ExecuteBusyAsync(async () =>
        {
            await using var ctx = await _contextFactory.CreateDbContextAsync();

            foreach (var item in changedItems)
            {
                var p = await ctx.Products.FindAsync(item.ProductId);
                if (p != null)
                {
                    p.Stock = item.PhysicalQuantity;
                    item.SystemQuantity = item.PhysicalQuantity;
                }
            }

            await ctx.SaveChangesAsync();
            UpdateStatistics();

            MessageBox.Show("🎉 تم تطبيق تسوية الجرد وتحديث بيانات المخزن بنجاح!", "تسوية الجرد", MessageBoxButton.OK, MessageBoxImage.Information);
        }, "جاري حفظ تسوية الجرد...");
    }

    [RelayCommand]
    private void ResetAudit()
    {
        foreach (var item in _allItems)
        {
            item.PhysicalQuantity = item.SystemQuantity;
        }
        UpdateStatistics();
    }
}
