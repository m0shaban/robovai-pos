using System.Collections.ObjectModel;
using System.Windows;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using SmartPOS.Application.Extensions;
using SmartPOS.Core.Entities;
using SmartPOS.Core.Interfaces;

namespace SmartPOS.Application.ViewModels;

public partial class CategoriesViewModel : BaseViewModel
{
    private readonly IRepository<Category> _categoryRepository;
    private readonly User _currentUser;
    private readonly IAuthorizationService _authService;

    // --- Data Caching & Binding ---
    private List<Category> _allCategories = new();

    [ObservableProperty]
    private ObservableCollection<Category> _categories = new();

    [ObservableProperty]
    private string _searchText = string.Empty;

    public bool IsAdmin =>
        _currentUser.Role == UserRole.SuperAdmin ||
        _currentUser.Role == UserRole.Admin ||
        _currentUser.Role == UserRole.Manager;

    // --- Form Properties ---
    [ObservableProperty] private bool _isFormVisible;
    [ObservableProperty] private Category? _selectedCategory;
    [ObservableProperty] private string _formName = string.Empty;
    [ObservableProperty] private string? _formDescription;
    [ObservableProperty] private string _formColorCode = "#3F51B5";
    [ObservableProperty] private bool _formIsActive = true;

    public CategoriesViewModel(IRepository<Category> categoryRepository, User currentUser, IAuthorizationService authService)
    {
        _categoryRepository = categoryRepository;
        _currentUser = currentUser;
        _authService = authService;

        _ = InitializeAsync();
    }

    private async Task InitializeAsync()
    {
        await ExecuteBusyAsync(LoadCategoriesCoreAsync, "⏳ جاري تحميل الأقسام...", $"✅ تم تحميل {Categories.Count} قسم");
    }

    private async Task LoadCategoriesCoreAsync()
    {
        var categories = await _categoryRepository.GetAllAsync();
        _allCategories = categories.ToList();
        FilterCategories();
    }

    // --- Commands ---
    [RelayCommand]
    public async Task LoadCategoriesAsync() => await ExecuteBusyAsync(LoadCategoriesCoreAsync, "جاري التحديث...");

    [RelayCommand]
    private void ShowAddForm()
    {
        ClearForm();
        IsFormVisible = true;
    }

    [RelayCommand]
    private void EditCategory(Category category)
    {
        SelectedCategory = category;
        FormName = category.Name;
        FormDescription = category.Description;
        FormColorCode = category.ColorCode ?? "#3F51B5";
        FormIsActive = category.IsActive;
        IsFormVisible = true;
    }

    [RelayCommand]
    private void CancelForm()
    {
        ClearForm();
        IsFormVisible = false;
    }

    [RelayCommand]
    private async Task SaveCategoryAsync()
    {
        if (string.IsNullOrWhiteSpace(FormName))
        {
            MessageBox.Show("أدخل اسم القسم", "تنبيه", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        await ExecuteBusyAsync(async () =>
        {
            if (SelectedCategory == null)
            {
                var category = new Category
                {
                    Name = FormName.Trim(),
                    Description = FormDescription,
                    ColorCode = FormColorCode,
                    IsActive = FormIsActive,
                    CreatedAt = DateTime.Now
                };
                await _categoryRepository.AddAsync(category);
            }
            else
            {
                SelectedCategory.Name = FormName.Trim();
                SelectedCategory.Description = FormDescription;
                SelectedCategory.ColorCode = FormColorCode;
                SelectedCategory.IsActive = FormIsActive;
                await _categoryRepository.UpdateAsync(SelectedCategory);
            }

            await LoadCategoriesCoreAsync();
            CancelForm();

        }, "جاري حفظ القسم...", "✅ تم الحفظ بنجاح");
    }

    [RelayCommand]
    private async Task DeleteCategoryAsync(Category? category)
    {
        if (category == null) return;

        bool authorized = await _authService.RequestAdminOverrideAsync("حذف قسم من النظام");
        if (!authorized) return;

        if (MessageBox.Show($"هل أنت متأكد من حذف القسم \"{category.Name}\"؟", "تأكيد", MessageBoxButton.YesNo, MessageBoxImage.Warning) == MessageBoxResult.Yes)
        {
            await ExecuteBusyAsync(async () =>
            {
                await _categoryRepository.DeleteAsync(category.Id);
                await LoadCategoriesCoreAsync();
            }, "جاري الحذف...", "✅ تم حذف القسم بنجاح");
        }
    }

    // --- Handlers & Helpers ---
    partial void OnSearchTextChanged(string value) => FilterCategories();

    private void FilterCategories()
    {
        var filtered = string.IsNullOrWhiteSpace(SearchText)
            ? _allCategories
            : _allCategories.Where(c => c.Name.Contains(SearchText, StringComparison.OrdinalIgnoreCase));

        Categories.SyncWith(filtered);
    }

    private void ClearForm()
    {
        SelectedCategory = null;
        FormName = string.Empty;
        FormDescription = null;
        FormColorCode = "#3F51B5";
        FormIsActive = true;
    }
}
