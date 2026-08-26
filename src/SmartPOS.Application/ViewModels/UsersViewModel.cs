using System.Collections.ObjectModel;
using System.Windows;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using SmartPOS.Application.Extensions;
using SmartPOS.Core.Entities;
using SmartPOS.Core.Interfaces;

namespace SmartPOS.Application.ViewModels;

public partial class UsersViewModel : BaseViewModel
{
    private readonly IRepository<User> _userRepository;
    private readonly User _currentUser;
    private readonly IAuthorizationService _authService;

    // --- Data Caching & Binding ---
    private List<User> _allUsers = new();

    [ObservableProperty]
    private ObservableCollection<User> _users = new();

    public ObservableCollection<UserRole> Roles { get; } = new(Enum.GetValues<UserRole>());

    [ObservableProperty]
    private User? _selectedUser;

    [ObservableProperty]
    private string _searchText = string.Empty;

    public bool IsSuperAdmin => _currentUser.Role == UserRole.SuperAdmin;
    public bool IsAdmin => _currentUser.Role == UserRole.Admin || _currentUser.Role == UserRole.SuperAdmin;

    /// <summary>
    /// Roles available for assignment based on current user's own role.
    /// SuperAdmin can assign any role. Admin can only assign Cashier/Inventory/Manager.
    /// </summary>
    public ObservableCollection<UserRole> AvailableRoles => _currentUser.Role == UserRole.SuperAdmin
        ? new(Enum.GetValues<UserRole>())
        : new(new[] { UserRole.Cashier, UserRole.Inventory, UserRole.Manager });

    // --- Form Properties ---
    [ObservableProperty] private string _formUsername = string.Empty;
    [ObservableProperty] private string _formFullName = string.Empty;
    [ObservableProperty] private string _formPassword = string.Empty;
    [ObservableProperty] private UserRole _formRole = UserRole.Cashier;
    [ObservableProperty] private string? _formPhone;
    [ObservableProperty] private string? _formEmail;
    [ObservableProperty] private string? _formAdminPin;
    [ObservableProperty] private bool _formIsActive = true;
    [ObservableProperty] private decimal _formDailyMealLimit;

    // Granular Permissions
    [ObservableProperty] private bool _permOpenDrawer;
    [ObservableProperty] private bool _permVoidItem;
    [ObservableProperty] private bool _permHighDiscount;
    [ObservableProperty] private bool _permProvideAdminPin;
    [ObservableProperty] private bool _permManageUsers;
    [ObservableProperty] private bool _permViewProfit;

    public UsersViewModel(IRepository<User> userRepository, User currentUser, IAuthorizationService authService)
    {
        _userRepository = userRepository;
        _currentUser = currentUser;
        _authService = authService;

        _ = InitializeAsync();
    }

    private async Task InitializeAsync()
    {
        await ExecuteBusyAsync(LoadUsersCoreAsync, "⏳ جاري تحميل المستخدمين...", $"✅ تم تحميل {Users.Count} مستخدم");
    }

    private async Task LoadUsersCoreAsync()
    {
        var users = await _userRepository.GetAllAsync();
        _allUsers = users.ToList();
        FilterUsers();
    }

    // --- Commands ---
    [RelayCommand]
    private async Task LoadUsersAsync() => await ExecuteBusyAsync(LoadUsersCoreAsync, "جاري تحديث القائمة...");

    [RelayCommand]
    private void SelectUser(User user)
    {
        SelectedUser = user;
        
        // Map permissions back to the form if the user is selected from the grid
    }

    partial void OnSelectedUserChanged(User? value)
    {
        if (value == null)
        {
            FormUsername = string.Empty;
            FormFullName = string.Empty;
            FormPassword = string.Empty;
            FormRole = UserRole.Cashier;
            FormPhone = null;
            FormEmail = null;
            FormAdminPin = null;
            FormIsActive = true;
            FormDailyMealLimit = 0;

            PermOpenDrawer = false;
            PermVoidItem = false;
            PermHighDiscount = false;
            PermProvideAdminPin = false;
            PermManageUsers = false;
            PermViewProfit = false;
        }
        else
        {
            FormUsername = value.Username;
            FormFullName = value.FullName;
            FormRole = value.Role;
            FormPhone = value.Phone;
            FormEmail = value.Email;
            FormAdminPin = value.AdminPin;
            FormIsActive = value.IsActive;
            FormDailyMealLimit = value.DailyMealLimit;

            PermOpenDrawer = value.Permissions.HasFlag(Permissions.OpenCashDrawer);
            PermVoidItem = value.Permissions.HasFlag(Permissions.VoidItem);
            PermHighDiscount = value.Permissions.HasFlag(Permissions.ApplyHighDiscount);
            PermProvideAdminPin = value.Permissions.HasFlag(Permissions.ProvideAdminPin);
            PermManageUsers = value.Permissions.HasFlag(Permissions.ManageUsers);
            PermViewProfit = value.Permissions.HasFlag(Permissions.ViewProfit);
        }
    }

    [RelayCommand]
    private void ClearForm()
    {
        SelectedUser = null;
        FormUsername = string.Empty;
        FormFullName = string.Empty;
        FormPassword = string.Empty;
        FormPhone = string.Empty;
        FormEmail = string.Empty;
        FormAdminPin = string.Empty;
        FormRole = UserRole.Cashier;
        FormIsActive = true;
        FormDailyMealLimit = 0;
        
        // Clear all permissions
        PermOpenDrawer = false;
        PermVoidItem = false;
        PermHighDiscount = false;
        PermProvideAdminPin = false;
        PermManageUsers = false;
        PermViewProfit = false;
        
        // Cannot clear PasswordBox from VM directly unless we use messaging or code-behind,
        // but typically we don't bind PasswordBox.Password directly for security reasons.
    }

    [RelayCommand]
    private async Task SaveUserAsync()
    {
        if (string.IsNullOrWhiteSpace(FormUsername) || string.IsNullOrWhiteSpace(FormFullName))
        {
            MessageBox.Show("اسم المستخدم والاسم الكامل مطلوبان", "تنبيه", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        if (SelectedUser == null && string.IsNullOrWhiteSpace(FormPassword))
        {
            MessageBox.Show("كلمة المرور مطلوبة للمستخدم الجديد", "تنبيه", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        await ExecuteBusyAsync(async () =>
        {
            var duplicates = await _userRepository.FindAsync(u => u.Username == FormUsername.Trim() && (SelectedUser == null || u.Id != SelectedUser.Id));
            if (duplicates.Any())
            {
                MessageBox.Show("اسم المستخدم موجود مسبقاً", "خطأ", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            if (!_currentUser.Role.HasFlag(UserRole.SuperAdmin) && FormRole == UserRole.SuperAdmin)
            {
                MessageBox.Show("لا تملك الصلاحية لإنشاء أو تعديل حساب بصلاحيات المطور", "مرفوض", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            if (SelectedUser != null && SelectedUser.Role == UserRole.SuperAdmin && !_currentUser.Role.HasFlag(UserRole.SuperAdmin))
            {
                MessageBox.Show("لا يمكنك التعديل على حساب المطور", "مرفوض", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            Permissions calculatedPermissions = FormRole switch
            {
                UserRole.SuperAdmin => Permissions.All,
                UserRole.Admin => Permissions.All,
                UserRole.Manager => Permissions.AccessPOS | Permissions.ViewDashboard | Permissions.ManageProducts | Permissions.ManageCategories | Permissions.ManageCustomers | Permissions.ManageSuppliers | Permissions.ManagePurchases | Permissions.ViewReports | Permissions.ManageExpenses | Permissions.ManageShifts | Permissions.ManageReturns,
                UserRole.Cashier => Permissions.AccessPOS | Permissions.ManageShifts | Permissions.ManageReturns,
                _ => Permissions.None
            };

            if (PermOpenDrawer) calculatedPermissions |= Permissions.OpenCashDrawer;
            if (PermVoidItem) calculatedPermissions |= Permissions.VoidItem;
            if (PermHighDiscount) calculatedPermissions |= Permissions.ApplyHighDiscount;
            if (PermProvideAdminPin) calculatedPermissions |= Permissions.ProvideAdminPin;
            if (PermManageUsers) calculatedPermissions |= Permissions.ManageUsers;
            if (PermViewProfit) calculatedPermissions |= Permissions.ViewProfit;

            if (SelectedUser == null)
            {
                var user = new User
                {
                    Username = FormUsername.Trim(),
                    FullName = FormFullName.Trim(),
                    PasswordHash = FormPassword, // Stored as plain-text as per prototype
                    Role = FormRole,
                    Phone = FormPhone,
                    Email = FormEmail,
                    AdminPin = FormAdminPin,
                    Permissions = calculatedPermissions,
                    IsActive = true,
                    DailyMealLimit = FormDailyMealLimit,
                    CreatedAt = DateTime.Now
                };
                await _userRepository.AddAsync(user);
            }
            else
            {
                SelectedUser.Username = FormUsername.Trim();
                SelectedUser.FullName = FormFullName.Trim();
                SelectedUser.Role = FormRole;
                SelectedUser.Phone = FormPhone;
                SelectedUser.Email = FormEmail;
                SelectedUser.AdminPin = FormAdminPin;
                SelectedUser.Permissions = calculatedPermissions;
                SelectedUser.IsActive = FormIsActive;
                SelectedUser.DailyMealLimit = FormDailyMealLimit;

                if (!string.IsNullOrWhiteSpace(FormPassword))
                {
                    SelectedUser.PasswordHash = FormPassword;
                }

                await _userRepository.UpdateAsync(SelectedUser);
            }

            await LoadUsersCoreAsync();
            ClearForm();

        }, "جاري الحفظ...", "✅ تم الحفظ بنجاح");
    }

    [RelayCommand]
    private async Task DeleteUserAsync(User? user)
    {
        if (user == null) return;

        if (user.Role == UserRole.SuperAdmin)
        {
            MessageBox.Show("لا يمكن حذف حساب المدير الرئيسي (Super Admin).", "مرفوض", MessageBoxButton.OK, MessageBoxImage.Error);
            return;
        }

        if (user.Id == _currentUser.Id)
        {
            MessageBox.Show("لا يمكنك حذف حسابك الخاص الذي تستخدمه حالياً.", "مرفوض", MessageBoxButton.OK, MessageBoxImage.Error);
            return;
        }

        bool authorized = await _authService.RequestAdminOverrideAsync("حذف مستخدم من النظام");
        if (!authorized) return;

        if (MessageBox.Show($"هل أنت متأكد من حذف المستخدم \"{user.FullName}\" بشكل نهائي؟", "تأكيد", MessageBoxButton.YesNo, MessageBoxImage.Warning) == MessageBoxResult.Yes)
        {
            await ExecuteBusyAsync(async () =>
            {
                await _userRepository.DeleteAsync(user.Id);
                await LoadUsersCoreAsync();
            }, "جاري الحذف...", "✅ تم الحذف بنجاح");
        }
    }

    [RelayCommand]
    private async Task ToggleUserActiveAsync(User? user)
    {
        if (user == null) return;
        
        if (user.Id == _currentUser.Id)
        {
            MessageBox.Show("لا يمكنك تعطيل حسابك الحالي", "مرفوض", MessageBoxButton.OK, MessageBoxImage.Error);
            return;
        }

        if (user.Role == UserRole.SuperAdmin && _currentUser.Role != UserRole.SuperAdmin)
        {
            MessageBox.Show("لا تملك صلاحية تعديل SuperAdmin", "مرفوض", MessageBoxButton.OK, MessageBoxImage.Error);
            return;
        }

        user.IsActive = !user.IsActive;
        await ExecuteBusyAsync(async () =>
        {
            await _userRepository.UpdateAsync(user);
            await LoadUsersCoreAsync();
        }, "جاري الحفظ...");
    }

    [RelayCommand]
    private async Task ResetPasswordAsync(User? user)
    {
        if (user == null) return;

        if (user.Role == UserRole.SuperAdmin && _currentUser.Role != UserRole.SuperAdmin)
        {
            MessageBox.Show("لا تملك صلاحية تعديل SuperAdmin", "مرفوض", MessageBoxButton.OK, MessageBoxImage.Error);
            return;
        }

        var result = MessageBox.Show($"هل تريد إعادة تعيين كلمة المرور للمستخدم '{user.Username}' إلى الافتراضية '123456'؟", "إعادة تعيين", MessageBoxButton.YesNo, MessageBoxImage.Question);
        if (result == MessageBoxResult.Yes)
        {
            user.PasswordHash = "123456"; // Plain text default
            await ExecuteBusyAsync(async () =>
            {
                await _userRepository.UpdateAsync(user);
            }, "جاري الحفظ...", "✅ تم إعادة تعيين كلمة المرور إلى 123456");
        }
    }

    // --- Handlers & Helpers ---
    partial void OnSearchTextChanged(string value) => FilterUsers();

    private void FilterUsers()
    {
        var query = _allUsers.AsEnumerable();
        
        // Security: Only SuperAdmin can see SuperAdmin accounts
        if (_currentUser.Role != UserRole.SuperAdmin)
        {
            query = query.Where(u => u.Role != UserRole.SuperAdmin);
        }

        if (!string.IsNullOrWhiteSpace(SearchText))
        {
            query = query.Where(u =>
                u.Username.Contains(SearchText, StringComparison.OrdinalIgnoreCase) ||
                u.FullName.Contains(SearchText, StringComparison.OrdinalIgnoreCase));
        }

        Users.SyncWith(query.OrderBy(u => u.Role).ThenBy(u => u.Username));
    }
}
