# 🔧 دليل إصلاح جميع ViewModels - نمط موحّد

**الهدف:** توحيد نمط التحميل في جميع ViewModels لضمان ظهور البيانات فوراً

---

## 📌 النمط الموحّد المقترح

### كل ViewModel يجب أن يتبع هذا النمط:

```csharp
public partial class [ModelName]ViewModel : ObservableObject
{
    // ========== 1. Dependencies ==========
    private readonly [IRepository] _repository;
    private readonly INotificationService _notificationService;

    // ========== 2. Observable Properties ==========
    [ObservableProperty]
    private ObservableCollection<[Entity]> items = new();

    [ObservableProperty]
    private bool isLoading;

    [ObservableProperty]
    private string statusMessage = "جاهز";

    // ========== 3. Commands ==========
    [RelayCommand]
    private async Task LoadData()
    {
        try
        {
            IsLoading = true;
            StatusMessage = "⏳ جاري التحميل...";

            var data = await _repository.GetAllAsync();
            Items.Clear();

            foreach (var item in data)
            {
                Items.Add(item);
            }

            StatusMessage = $"✅ تم تحميل {Items.Count} عنصر";
        }
        catch (Exception ex)
        {
            StatusMessage = "❌ خطأ في التحميل";
            _notificationService?.ShowError(ex.Message);
        }
        finally
        {
            IsLoading = false;
        }
    }

    // ========== 4. Constructor ==========
    public [ModelName]ViewModel(
        [IRepository] repository,
        INotificationService notificationService)
    {
        _repository = repository;
        _notificationService = notificationService;

        // ✅ تحميل البيانات مباشرة
        _ = InitializeAsync();
    }

    // ========== 5. Initialization ==========
    private async Task InitializeAsync()
    {
        try
        {
            await LoadDataCommand.ExecuteAsync(null);
        }
        catch (Exception ex)
        {
            StatusMessage = "❌ فشل تحميل البيانات";
        }
    }
}
```

---

## 🎯 تطبيق النمط على كل Model

### 1️⃣ MainPOSViewModel

**الملف:** `f:\Raw\kasher\kasher\src\SmartPOS.Application\ViewModels\MainPOSViewModel.cs`

**التغييرات المقترحة:**

```csharp
public partial class MainPOSViewModel : ObservableObject
{
    // ... Dependencies ...

    // ❌ الحالية (خاطئة):
    public MainPOSViewModel(...)
    {
        _ = LoadQuickDataAsync();
        _ = LoadSettingsAsync();
        _ = CheckActiveShiftAsync();
    }

    // ✅ الحالية الجديدة (صحيحة):
    public MainPOSViewModel(...)
    {
        _ = InitializeAsync();
    }

    private async Task InitializeAsync()
    {
        try
        {
            IsLoading = true;
            await LoadSettingsAsync();
            await LoadQuickDataAsync();
            await CheckActiveShiftAsync();
            StatusMessage = "✅ البيانات جاهزة";
        }
        catch (Exception ex)
        {
            StatusMessage = "❌ خطأ في التهيئة";
            _notificationService?.ShowError($"خطأ في التهيئة: {ex.Message}");
        }
        finally
        {
            IsLoading = false;
        }
    }
}
```

### 2️⃣ ProductsViewModel

**الملف:** `f:\Raw\kasher\kasher\src\SmartPOS.Application\ViewModels\ProductsViewModel.cs`

**التغييرات المقترحة:**

```csharp
public partial class ProductsViewModel : ObservableObject
{
    private readonly IRepository<Product> _productRepository;
    private readonly IRepository<Category> _categoryRepository;

    // Observable Collections
    [ObservableProperty]
    private ObservableCollection<ProductDto> products = new();

    [ObservableProperty]
    private ObservableCollection<CategoryDto> categories = new();

    [ObservableProperty]
    private bool isLoading;

    // ❌ الحالي:
    public ProductsViewModel(...)
    {
        _productRepository = productRepository;
        _categoryRepository = categoryRepository;
        // لا يوجد تحميل هنا
    }

    // ✅ الجديد:
    public ProductsViewModel(...)
    {
        _productRepository = productRepository;
        _categoryRepository = categoryRepository;

        _ = InitializeAsync(); // تحميل فوري
    }

    private async Task InitializeAsync()
    {
        try
        {
            IsLoading = true;

            var products = await _productRepository.GetAllAsync();
            var categories = await _categoryRepository.GetAllAsync();

            Products = new(products
                .Select(p => new ProductDto(p))
                .OrderBy(p => p.Name));

            Categories = new(categories
                .Select(c => new CategoryDto(c))
                .OrderBy(c => c.Name));

            StatusMessage = $"✅ {Products.Count} منتج و {Categories.Count} فئة";
        }
        catch (Exception ex)
        {
            StatusMessage = "❌ خطأ في تحميل المنتجات";
        }
        finally
        {
            IsLoading = false;
        }
    }
}
```

### 3️⃣ DashboardViewModel

**الملف:** `f:\Raw\kasher\kasher\src\SmartPOS.Application\ViewModels\DashboardViewModel.cs`

**التغييرات المقترحة:**

```csharp
public partial class DashboardViewModel : ObservableObject
{
    private readonly AppDbContext _context;
    private readonly User _currentUser;

    [ObservableProperty]
    private decimal totalSales;

    [ObservableProperty]
    private decimal totalExpenses;

    [ObservableProperty]
    private int totalTransactions;

    [ObservableProperty]
    private bool isLoading;

    // ❌ الحالي:
    public DashboardViewModel(AppDbContext context, User currentUser)
    {
        _context = context;
        _currentUser = currentUser;
        // التحميل في Page_Loaded
    }

    // ✅ الجديد:
    public DashboardViewModel(AppDbContext context, User currentUser)
    {
        _context = context;
        _currentUser = currentUser;

        _ = InitializeAsync();
    }

    private async Task InitializeAsync()
    {
        try
        {
            IsLoading = true;
            await LoadDashboardData();
        }
        catch (Exception ex)
        {
            StatusMessage = "❌ خطأ في تحميل البيانات";
        }
        finally
        {
            IsLoading = false;
        }
    }

    private async Task LoadDashboardData()
    {
        // احسب البيانات من Database
        var transactions = await _context.Transactions
            .Where(t => t.UserId == _currentUser.Id)
            .ToListAsync();

        TotalSales = transactions
            .Where(t => t.Type == TransactionType.Sale)
            .Sum(t => t.Total);

        TotalExpenses = transactions
            .Where(t => t.Type == TransactionType.Expense)
            .Sum(t => t.Total);

        TotalTransactions = transactions.Count;
    }
}
```

### 4️⃣ CustomersViewModel

**الملف:** `f:\Raw\kasher\kasher\src\SmartPOS.Application\ViewModels\CustomersViewModel.cs`

**الحالة:** ✅ جيدة - بحاجة إلى تحسين طفيف فقط

```csharp
public partial class CustomersViewModel : ObservableObject
{
    [ObservableProperty]
    private ObservableCollection<CustomerDto> customers = new();

    [ObservableProperty]
    private bool isLoading;

    // ✅ نمط جيد:
    public CustomersViewModel(IRepository<Customer> repository)
    {
        _ = InitializeAsync(); // تحميل فوري
    }

    [RelayCommand]
    private async Task LoadCustomers()
    {
        try
        {
            IsLoading = true;
            var data = await _repository.GetAllAsync();
            Customers = new(data.Select(c => new CustomerDto(c)));
        }
        finally
        {
            IsLoading = false;
        }
    }

    private async Task InitializeAsync()
    {
        await LoadCustomersCommand.ExecuteAsync(null);
    }
}
```

### 5️⃣ SuppliersViewModel

**الملف:** `f:\Raw\kasher\kasher\src\SmartPOS.Application\ViewModels\SuppliersViewModel.cs`

**الحالة:** ✅ جيدة - نفس نمط CustomersViewModel

### 6️⃣ ExpensesViewModel

**الملف:** `f:\Raw\kasher\kasher\src\SmartPOS.Application\ViewModels\ExpensesViewModel.cs`

**الحالة:** ✅ جيدة - نفس النمط

### 7️⃣ ReturnsViewModel

**الملف:** `f:\Raw\kasher\kasher\src\SmartPOS.Application\ViewModels\ReturnsViewModel.cs`

**الحالة:** ✅ جيدة - نفس النمط

### 8️⃣ ReportsViewModel

**الملف:** `f:\Raw\kasher\kasher\src\SmartPOS.Application\ViewModels\ReportsViewModel.cs`

**الحالة:** ⚠️ محتاجة تحسين

```csharp
// حالياً:
public async void PrintIncomeReportAsync()
{
    // ❌ async void خطر - قد لا يعرض الأخطاء
}

// الصحيح:
public async Task PrintIncomeReportAsync()
{
    try
    {
        // ... code ...
    }
    catch (Exception ex)
    {
        _notificationService?.ShowError(ex.Message);
    }
}

// استدعاء آمن:
private async Task InitializeAsync()
{
    await PrintIncomeReportAsync();
}
```

### 9️⃣ CategoriesViewModel

**الملف:** قد لا يكون موجوداً - يجب إنشاؤه

**الكود المقترح:**

```csharp
public partial class CategoriesViewModel : ObservableObject
{
    private readonly IRepository<Category> _repository;

    [ObservableProperty]
    private ObservableCollection<CategoryDto> categories = new();

    [ObservableProperty]
    private bool isLoading;

    public CategoriesViewModel(IRepository<Category> repository)
    {
        _repository = repository;
        _ = InitializeAsync();
    }

    [RelayCommand]
    private async Task LoadCategories()
    {
        try
        {
            IsLoading = true;
            var data = await _repository.GetAllAsync();
            Categories = new(data
                .Select(c => new CategoryDto(c))
                .OrderBy(c => c.Name));
        }
        finally
        {
            IsLoading = false;
        }
    }

    private async Task InitializeAsync()
    {
        await LoadCategoriesCommand.ExecuteAsync(null);
    }
}
```

### 🔟 Models الأخرى

- **UsersViewModel**
- **ShiftManagementViewModel**
- **LoyaltyViewModel**
- **PurchaseOrdersViewModel**
- **InvoicesViewModel**
- **FeaturesViewModel**

**النمط:** نفس النمط أعلاه مع تغيير Entity و Repository حسب الحاجة

---

## 📝 Template نهائي

### نسخ واستخدم هذا Template:

```csharp
// ========================================
// [ModelName]ViewModel.cs
// ========================================

using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using SmartPOS.Core.Entities;
using System.Collections.ObjectModel;

namespace SmartPOS.Application.ViewModels;

public partial class [ModelName]ViewModel : ObservableObject
{
    // ========== Dependencies ==========
    private readonly IRepository<[Entity]> _repository;

    // ========== Observable Properties ==========
    [ObservableProperty]
    private ObservableCollection<[Entity]> items = new();

    [ObservableProperty]
    private bool isLoading;

    [ObservableProperty]
    private string statusMessage = "جاهز";

    // ========== Constructor ==========
    public [ModelName]ViewModel(IRepository<[Entity]> repository)
    {
        _repository = repository;
        _ = InitializeAsync();
    }

    // ========== Commands ==========
    [RelayCommand]
    private async Task Load()
    {
        try
        {
            IsLoading = true;
            StatusMessage = "⏳ جاري التحميل...";

            var data = await _repository.GetAllAsync();
            Items.Clear();

            foreach (var item in data)
            {
                Items.Add(item);
            }

            StatusMessage = $"✅ تم تحميل {Items.Count}";
        }
        catch (Exception ex)
        {
            StatusMessage = "❌ خطأ";
        }
        finally
        {
            IsLoading = false;
        }
    }

    // ========== Initialization ==========
    private async Task InitializeAsync()
    {
        await LoadCommand.ExecuteAsync(null);
    }
}
```

---

## ✅ قائمة التحقق للإصلاح

```
[ ] 1. تحديث MainPOSViewModel
[ ] 2. تحديث ProductsViewModel
[ ] 3. تحديث DashboardViewModel
[ ] 4. تحديث CategoriesViewModel
[ ] 5. تحديث UsersViewModel
[ ] 6. تحديث ShiftManagementViewModel
[ ] 7. تحديث LoyaltyViewModel
[ ] 8. تحديث PurchaseOrdersViewModel
[ ] 9. تحديث InvoicesViewModel
[ ] 10. تحديث FeaturesViewModel

بعد التحديث:
[ ] 11. Build: dotnet build -c Release
[ ] 12. Publish: dotnet publish -c Release
[ ] 13. Test: حذف Database وإعادة تشغيل
[ ] 14. Verify: التحقق من ظهور البيانات
[ ] 15. Build Installer: iscc.exe SmartPOS.InnoSetup.iss
```

---

## 🚀 الأوامر النهائية

```powershell
# بعد تطبيق جميع التغييرات:

# 1. بناء Release
cd "F:\Raw\kasher\kasher\src\SmartPOS.WPF"
dotnet build -c Release

# 2. نشر
dotnet publish -c Release -r win-x64 --self-contained true -p:PublishSingleFile=true -o "..\..\publish\final-exe"

# 3. بناء مثبت جديد
& "C:\Program Files (x86)\Inno Setup 6\iscc.exe" "F:\Raw\kasher\kasher\installer\SmartPOS.InnoSetup.iss"

# 4. اختبار
# - حذف Database
# - تشغيل البرنامج
# - التحقق من ظهور البيانات
```

---

**النتيجة المتوقعة:** جميع البيانات تظهر فوراً بعد تشغيل البرنامج ✅
