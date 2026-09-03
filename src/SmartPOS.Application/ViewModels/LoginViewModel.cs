using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using SmartPOS.Core.Entities;
using SmartPOS.Core.Interfaces;
using System.Windows;

namespace SmartPOS.Application.ViewModels;

public partial class LoginViewModel : ObservableObject
{
    private readonly IRepository<User> _userRepository;
    private readonly IUserService _userService;

    public ISettingsService Settings { get; }

    [ObservableProperty]
    private string _username = string.Empty;

    [ObservableProperty]
    private string _password = string.Empty;

    [ObservableProperty]
    private string _errorMessage = string.Empty;

    [ObservableProperty]
    private bool _isBusy;

    public event Action? RequestClose;

    public LoginViewModel(IRepository<User> userRepository, IUserService userService, ISettingsService settings)
    {
        _userRepository = userRepository;
        _userService = userService;
        Settings = settings;
    }

    [RelayCommand]
    private async Task LoginAsync()
    {
        if (string.IsNullOrWhiteSpace(Username) || string.IsNullOrWhiteSpace(Password))
        {
            ErrorMessage = SmartPOS.Core.Localization.Loc.Tr("Loc_Login_Required", "الرجاء إدخال اسم المستخدم وكلمة المرور");
            return;
        }

        IsBusy = true;
        ErrorMessage = string.Empty;

        try
        {
            var normalizedUsername = Username.Trim();
            var normalizedPassword = Password.Trim();

            var users = await _userRepository.GetAllAsync();
            var user = users.FirstOrDefault(u =>
                u.Username.Equals(normalizedUsername, StringComparison.OrdinalIgnoreCase));

            if (user == null)
            {
                ErrorMessage = SmartPOS.Core.Localization.Loc.Tr("Loc_Login_UserNotFound", "اسم المستخدم غير موجود");
                return;
            }

            if (!user.IsActive)
            {
                ErrorMessage = SmartPOS.Core.Localization.Loc.Tr("Loc_Login_UserInactive", "هذا المستخدم غير نشط. تواصل مع المدير.");
                return;
            }

            // Plain-text comparison (prototype).
            // For production: replace with BCrypt.Verify(normalizedPassword, user.PasswordHash)
            if (user.PasswordHash != normalizedPassword)
            {
                ErrorMessage = SmartPOS.Core.Localization.Loc.Tr("Loc_Login_InvalidPassword", "كلمة المرور غير صحيحة");
                return;
            }

            _userService.SetUser(user);
            RequestClose?.Invoke();
        }
        catch (Exception ex)
        {
            var prefix = SmartPOS.Core.Localization.Loc.Tr("Loc_Login_Error", "خطأ في تسجيل الدخول");
            ErrorMessage = $"{prefix}: {ex.Message}";
        }
        finally
        {
            IsBusy = false;
        }
    }

    [RelayCommand]
    private void Exit()
    {
        System.Windows.Application.Current.Shutdown();
    }
}
