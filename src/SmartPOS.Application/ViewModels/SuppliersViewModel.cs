using System.Collections.ObjectModel;
using System.Windows;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using SmartPOS.Application.Extensions;
using SmartPOS.Core.Entities;
using SmartPOS.Core.Interfaces;

namespace SmartPOS.Application.ViewModels;

public partial class SuppliersViewModel : BaseViewModel
{
    private readonly IRepository<Supplier> _supplierRepository;
    private readonly User _currentUser;
    private readonly IAuthorizationService _authService;

    // --- Collections ---
    private List<Supplier> _allSuppliers = new();

    [ObservableProperty]
    private ObservableCollection<Supplier> _suppliers = new();

    // --- State & Selection ---
    [ObservableProperty]
    private Supplier? _selectedSupplier;

    [ObservableProperty]
    private string? _searchText;

    public bool IsAdmin =>
        _currentUser.Role == UserRole.SuperAdmin ||
        _currentUser.Role == UserRole.Admin ||
        _currentUser.Role == UserRole.Manager;

    // --- Form Properties ---
    [ObservableProperty] private string _formName = string.Empty;
    [ObservableProperty] private string? _formContactPerson;
    [ObservableProperty] private string? _formPhone;
    [ObservableProperty] private string? _formEmail;
    [ObservableProperty] private string? _formAddress;
    [ObservableProperty] private bool _formIsActive = true;

    public SuppliersViewModel(IRepository<Supplier> supplierRepository, User currentUser, IAuthorizationService authService)
    {
        _supplierRepository = supplierRepository;
        _currentUser = currentUser;
        _authService = authService;

        _ = InitializeAsync();
    }

    private async Task InitializeAsync()
    {
        await ExecuteBusyAsync(LoadSuppliersCoreAsync, "⏳ جاري تحميل الموردين...", $"✅ تم تحميل {Suppliers.Count} مورد");
    }

    private async Task LoadSuppliersCoreAsync()
    {
        var data = await _supplierRepository.GetAllAsync();
        _allSuppliers = data.ToList();
        FilterSuppliers();
    }

    // --- Commands ---
    [RelayCommand]
    private async Task LoadSuppliersAsync() => await ExecuteBusyAsync(LoadSuppliersCoreAsync, "جاري تحديث القائمة...");

    [RelayCommand]
    private void Search()
    {
        FilterSuppliers();
    }

    [RelayCommand]
    private void EditSupplier(Supplier supplier)
    {
        SelectedSupplier = supplier;
    }

    [RelayCommand]
    private void ClearForm()
    {
        SelectedSupplier = null;
        FormName = string.Empty;
        FormContactPerson = string.Empty;
        FormPhone = string.Empty;
        FormEmail = string.Empty;
        FormAddress = string.Empty;
        FormIsActive = true;
    }

    [RelayCommand]
    private async Task SaveSupplierAsync()
    {
        if (string.IsNullOrWhiteSpace(FormName))
        {
            MessageBox.Show("يرجى إدخال اسم المورد", "تنبيه", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        await ExecuteBusyAsync(async () =>
        {
            if (SelectedSupplier == null)
            {
                var supplier = new Supplier
                {
                    Name = FormName,
                    ContactPerson = FormContactPerson,
                    Phone = FormPhone,
                    Email = FormEmail,
                    Address = FormAddress,
                    IsActive = FormIsActive
                };
                await _supplierRepository.AddAsync(supplier);
            }
            else
            {
                SelectedSupplier.Name = FormName;
                SelectedSupplier.ContactPerson = FormContactPerson;
                SelectedSupplier.Phone = FormPhone;
                SelectedSupplier.Email = FormEmail;
                SelectedSupplier.Address = FormAddress;
                SelectedSupplier.IsActive = FormIsActive;

                await _supplierRepository.UpdateAsync(SelectedSupplier);
            }

            await LoadSuppliersCoreAsync();
            ClearForm();

        }, "جاري حفظ المورد...", "✅ تم حفظ المورد بنجاح");
    }

    [RelayCommand]
    private async Task DeleteSupplierAsync(Supplier? supplier)
    {
        if (supplier == null) return;

        bool authorized = await _authService.RequestAdminOverrideAsync("حذف مورد من النظام");
        if (!authorized) return;

        if (MessageBox.Show($"هل أنت متأكد من حذف المورد \"{supplier.Name}\"؟", "تأكيد", MessageBoxButton.YesNo, MessageBoxImage.Warning) == MessageBoxResult.Yes)
        {
            await ExecuteBusyAsync(async () =>
            {
                try
                {
                    await _supplierRepository.DeleteAsync(supplier.Id);
                    await LoadSuppliersCoreAsync();
                    ClearForm();
                }
                catch (Exception ex)
                {
                    throw new Exception($"لا يمكن حذف المورد لأنه مرتبط بعمليات شراء أو منتجات.\n{ex.Message}");
                }
            }, "جاري الحذف...", "✅ تم حذف المورد بنجاح");
        }
    }

    // --- Property Changed Handlers ---
    partial void OnSearchTextChanged(string? value) => FilterSuppliers();

    partial void OnSelectedSupplierChanged(Supplier? value)
    {
        if (value == null) return; // Prevent wiping form during filtering or refresh

        FormName = value.Name;
        FormContactPerson = value.ContactPerson;
        FormPhone = value.Phone;
        FormEmail = value.Email;
        FormAddress = value.Address;
        FormIsActive = value.IsActive;
    }

    // --- Helpers ---
    private void FilterSuppliers()
    {
        var filtered = string.IsNullOrWhiteSpace(SearchText)
            ? _allSuppliers
            : _allSuppliers.Where(s =>
                s.Name.Contains(SearchText, StringComparison.OrdinalIgnoreCase) ||
                (s.Phone != null && s.Phone.Contains(SearchText)));

        Suppliers.SyncWith(filtered.OrderBy(s => s.Name));
    }
}
