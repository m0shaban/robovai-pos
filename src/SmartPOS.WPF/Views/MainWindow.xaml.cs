using Microsoft.Extensions.DependencyInjection;
using CommunityToolkit.Mvvm.Messaging;
using SmartPOS.Core.Interfaces;
using SmartPOS.Infrastructure.Services;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using App = SmartPOS.WPF.App;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using SmartPOS.Core.Entities;

namespace SmartPOS.WPF.Views;

public partial class MainWindow : Window, INotifyPropertyChanged
{
    // Page cache — avoid re-creating pages (and ViewModels / DB queries) on every navigation.
    private readonly Dictionary<int, Page> _pageCache = new();
    private IBarcodeService? _barcodeService;
    private IAuthorizationService? _authService;
    private SmartPOS.WPF.Services.IThemeService? _themeService;

    public Core.Entities.User? CurrentUser { get; private set; }
    public ISettingsService? Settings { get; private set; }

    // --- RBAC Properties ---
    public bool CanViewDashboard => _authService?.HasPermission(Permissions.ViewDashboard) ?? false;
    public bool CanAccessPOS => _authService?.HasPermission(Permissions.AccessPOS) ?? false;
    public bool CanManageProducts => _authService?.HasPermission(Permissions.ManageProducts) ?? false;
    public bool CanManageCategories => _authService?.HasPermission(Permissions.ManageCategories) ?? false;
    public bool CanManageCustomers => _authService?.HasPermission(Permissions.ManageCustomers) ?? false;
    public bool CanManageSuppliers => _authService?.HasPermission(Permissions.ManageSuppliers) ?? false;
    public bool CanManagePurchases => _authService?.HasPermission(Permissions.ManagePurchases) ?? false;
    public bool CanViewReports => _authService?.HasPermission(Permissions.ViewReports) ?? false;
    public bool CanManageExpenses => _authService?.HasPermission(Permissions.ManageExpenses) ?? false;
    public bool CanManageShifts => _authService?.HasPermission(Permissions.ManageShifts) ?? false;
    public bool CanManageReturns => _authService?.HasPermission(Permissions.ManageReturns) ?? false;
    public bool CanManageUsers => _authService?.HasPermission(Permissions.ManageUsers) ?? false;
    
    // Always visible to everyone (but inside they might have limits)
    public bool CanViewSettings => _authService?.HasPermission(Permissions.ViewDashboard) ?? true; // Just a placeholder, settings is usually accessible but restricted inside

    public event PropertyChangedEventHandler? PropertyChanged;
    protected void OnPropertyChanged([CallerMemberName] string? name = null)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
    }

    public void RefreshPermissions()
    {
        OnPropertyChanged(nameof(CanViewDashboard));
        OnPropertyChanged(nameof(CanAccessPOS));
        OnPropertyChanged(nameof(CanManageProducts));
        OnPropertyChanged(nameof(CanManageCategories));
        OnPropertyChanged(nameof(CanManageCustomers));
        OnPropertyChanged(nameof(CanManageSuppliers));
        OnPropertyChanged(nameof(CanManagePurchases));
        OnPropertyChanged(nameof(CanViewReports));
        OnPropertyChanged(nameof(CanManageExpenses));
        OnPropertyChanged(nameof(CanManageShifts));
        OnPropertyChanged(nameof(CanManageReturns));
        OnPropertyChanged(nameof(CanManageUsers));
    }

    public MainWindow()
    {
        InitializeComponent();

        try
        {
            var host = ((App)System.Windows.Application.Current).Host;
            CurrentUser = host.Services.GetService<IUserService>()?.CurrentUser;
            Settings = host.Services.GetService<ISettingsService>();
            _authService = host.Services.GetService<IAuthorizationService>();
        }
        catch { }

        DataContext = this;

        // Navigate to Dashboard by default (or POS if Cashier)
        if (CurrentUser?.Role == Core.Entities.UserRole.Cashier)
        {
            MenuListBox.SelectedIndex = 1;
        }
        else
        {
            MainFrame.Navigate(GetOrCreatePage(0));
        }

        // Wire Window Mode change notifications
        try
        {
            WeakReferenceMessenger.Default.Register<SmartPOS.Application.Messages.WindowModeChangedMessage>(this, (r, m) =>
            {
                Dispatcher.Invoke(() => ApplyWindowMode(m.Value));
            });
        }
        catch { }

        // Wire Language change notifications
        try
        {
            var host = ((App)System.Windows.Application.Current).Host;
            var locService = host.Services.GetService<SmartPOS.Core.Interfaces.ILocalizationService>();
            if (locService != null)
            {
                FlowDirection = locService.IsRtl ? FlowDirection.RightToLeft : FlowDirection.LeftToRight;
            }

            WeakReferenceMessenger.Default.Register<SmartPOS.Application.Messages.LanguageChangedMessage>(this, (r, m) =>
            {
                Dispatcher.Invoke(() =>
                {
                    FlowDirection = m.Value == "en" ? FlowDirection.LeftToRight : FlowDirection.RightToLeft;
                    if (MenuListBox != null && PageTitle != null)
                    {
                        PageTitle.Text = GetPageTitle(MenuListBox.SelectedIndex);
                    }
                });
            });
        }
        catch { }

        // Wire Theme & UI Scaling
        try
        {
            var host = ((App)System.Windows.Application.Current).Host;
            _themeService = host.Services.GetService<SmartPOS.WPF.Services.IThemeService>();
            if (_themeService != null)
            {
                ApplyZoom(_themeService.CurrentZoomFactor);
                _themeService.ZoomChanged += (s, factor) => Dispatcher.Invoke(() => ApplyZoom(factor));
            }
        }
        catch { /* theme service optional */ }

        // Wire barcode scanner
        try
        {
            var host = ((App)System.Windows.Application.Current).Host;
            _barcodeService = host.Services.GetService<IBarcodeService>();
            if (_barcodeService is BarcodeService svc)
            {
                svc.StartListening();
                svc.BarcodeScanned += (s, barcode) =>
                {
                    Dispatcher.Invoke(() =>
                    {
                        // Clean up any accidental character that leaked into focused TextBox
                        if (System.Windows.Input.Keyboard.FocusedElement is TextBox tb && !string.IsNullOrEmpty(tb.Text) && !string.IsNullOrEmpty(barcode))
                        {
                            if (tb.Text == barcode)
                            {
                                tb.Text = string.Empty;
                            }
                            else if (tb.Text.EndsWith(barcode[0].ToString()))
                            {
                                tb.Text = tb.Text[..^1];
                            }
                        }

                        CommunityToolkit.Mvvm.Messaging.WeakReferenceMessenger.Default.Send(
                            new SmartPOS.Application.Messages.BarcodeScannedMessage(barcode));
                    });
                };
                PreviewKeyDown += MainWindow_PreviewKeyDown;
                PreviewTextInput += MainWindow_PreviewTextInput;
            }
        }
        catch { /* barcode service optional */ }
    }

    private void MainWindow_PreviewTextInput(object sender, TextCompositionEventArgs e)
    {
        if (_barcodeService is BarcodeService svc)
        {
            // Capture actual characters typed (handles D1 -> "1", NumPad1 -> "1", etc.)
            if (svc.ProcessKeyInput(e.Text))
            {
                e.Handled = true; // Suppress character insertion into focused TextBox if from fast scanner
            }
        }
    }

    private void MainWindow_PreviewKeyDown(object sender, KeyEventArgs e)
    {
        if (_barcodeService is BarcodeService svc)
        {
            // Only capture Enter key from KeyDown, because TextInput doesn't reliably fire for Enter
            if (e.Key == Key.Return || e.Key == Key.Enter)
            {
                if (svc.ProcessKeyInput("\r"))
                {
                    e.Handled = true; // Suppress default form submit if from scanner
                }
            }
        }
    }

    private Page GetOrCreatePage(int index)
    {
        // Settings page is never cached (always needs fresh data)
        if (index == 17)
            return new SettingsPage();

        if (_pageCache.TryGetValue(index, out var cached))
            return cached;

        Page page = index switch
        {
            0 => (Page)new DashboardPage(),
            1 => new POSPage(),
            2 => new RentalsPage(),
            3 => new ShiftManagementPage(),
            4 => new InvoicesPage(),
            5 => new ReturnsPage(),
            6 => new PurchaseOrdersPage(),
            7 => new ExpensesPage(),
            8 => new ProductsPage(),
            9 => ((App)System.Windows.Application.Current).Host.Services.GetRequiredService<StockAuditPage>(),
            10 => new CategoriesPage(),
            11 => new SuppliersPage(),
            12 => new CustomersPage(),
            13 => new LoyaltyPage(),
            14 => new ReportsPage(),
            15 => new UsersPage(),
            16 => new AuditLogPage(),
            17 => new SettingsPage(),
            18 => new FeaturesPage(),
            _ => new DashboardPage()
        };

        _pageCache[index] = page;
        return page;
    }

    private static readonly string[] PageTitleResourceKeys =
    [
        "Loc_NavDashboard", "Loc_NavPOS", "Loc_NavSessions", "Loc_NavShifts",
        "Loc_NavInvoices", "Loc_NavReturns", "Loc_NavPurchases", "Loc_NavExpenses", 
        "Loc_NavProducts", "Loc_NavStockAudit", "Loc_NavCategories", "Loc_NavSuppliers", "Loc_NavCustomers", 
        "Loc_NavLoyalty", "Loc_NavReports", "Loc_NavUsers", "Loc_NavAuditLog",
        "Loc_NavSettings", "Loc_NavFeatures"
    ];

    private static readonly string[] PageTitlesFallback =
    [
        "لوحة المعلومات", "نقطة البيع", "إدارة الجلسات", "إدارة الورديات",
        "الفواتير", "المرتجعات", "المشتريات", "المصروفات", 
        "المنتجات", "جرد وتسوية المخزون", "الأقسام", "الموردين", "العملاء", 
        "نقاط الولاء", "التقارير", "إدارة المستخدمين", "سجل النشاط",
        "الإعدادات", "مميزات البرنامج"
    ];

    public static string GetPageTitle(int index)
    {
        if (index >= 0 && index < PageTitleResourceKeys.Length)
        {
            var key = PageTitleResourceKeys[index];
            if (System.Windows.Application.Current != null && System.Windows.Application.Current.Resources.Contains(key))
            {
                return System.Windows.Application.Current.Resources[key]?.ToString() ?? PageTitlesFallback[index];
            }
            if (index < PageTitlesFallback.Length) return PageTitlesFallback[index];
        }
        return string.Empty;
    }

    private void MenuListBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (MenuListBox == null || PageTitle == null || MainFrame == null)
            return;

        var idx = MenuListBox.SelectedIndex;
        if (idx < 0 || idx >= PageTitleResourceKeys.Length) return;

        // Backend Protection: Prevent navigation to unauthorized pages
        if (_authService != null)
        {
            bool authorized = idx switch
            {
                0 => _authService.HasPermission(Permissions.ViewDashboard),
                1 => _authService.HasPermission(Permissions.AccessPOS),
                2 => _authService.HasPermission(Permissions.AccessPOS), // Rentals is POS related
                3 => _authService.HasPermission(Permissions.ManageShifts),
                4 => _authService.HasPermission(Permissions.ViewDashboard), // Invoices = Dashboard view
                5 => _authService.HasPermission(Permissions.ManageReturns),
                6 => _authService.HasPermission(Permissions.ManagePurchases),
                7 => _authService.HasPermission(Permissions.ManageExpenses),
                8 => _authService.HasPermission(Permissions.ManageProducts),
                9 => _authService.HasPermission(Permissions.ManageProducts), // Stock Audit
                10 => _authService.HasPermission(Permissions.ManageCategories),
                11 => _authService.HasPermission(Permissions.ManageSuppliers),
                12 => _authService.HasPermission(Permissions.ManageCustomers),
                13 => _authService.HasPermission(Permissions.ManageCustomers), // Loyalty
                14 => _authService.HasPermission(Permissions.ViewReports),
                15 => _authService.HasPermission(Permissions.ManageUsers),
                16 => _authService.HasPermission(Permissions.ManageUsers), // AuditLog
                17 => _authService.HasPermission(Permissions.ManageSettings),
                18 => _authService.HasPermission(Permissions.ManageUsers), // Features (Admin)
                _ => true
            };

            if (!authorized)
            {
                // Fallback to POS if Cashier, else Dashboard, else stay
                if (CurrentUser?.Role == UserRole.Cashier)
                {
                    if (idx != 1) MenuListBox.SelectedIndex = 1;
                }
                else
                {
                    if (idx != 0) MenuListBox.SelectedIndex = 0;
                }
                return;
            }
        }

        PageTitle.Text = GetPageTitle(idx);
        MainFrame.Navigate(GetOrCreatePage(idx));
    }

    private void LogoutButton_Click(object sender, RoutedEventArgs e)
    {
        var confirmMsg = (System.Windows.Application.Current?.Resources["Loc_ConfirmLogoutMessage"] as string) ?? "هل أنت متأكد من تسجيل الخروج؟";
        var confirmTitle = (System.Windows.Application.Current?.Resources["Loc_ConfirmLogoutTitle"] as string) ?? "تسجيل الخروج";
        var result = MessageBox.Show(confirmMsg, confirmTitle, MessageBoxButton.YesNo, MessageBoxImage.Question);

        if (result == MessageBoxResult.Yes)
        {
            try
            {
                var app = (App?)System.Windows.Application.Current;
                if (app?.Host != null)
                {
                    var userService = app.Host.Services.GetService<IUserService>();
                    userService?.Logout();

                    _pageCache.Clear();

                    var loginWindow = app.Host.Services.GetRequiredService<LoginWindow>();
                    Hide();
                    if (loginWindow.ShowDialog() == true)
                    {
                        _pageCache.Clear();

                        // Refresh current user after re-login
                        CurrentUser = app.Host.Services.GetService<IUserService>()?.CurrentUser;
                        DataContext = null;
                        DataContext = this;
                        RefreshPermissions(); // Inform UI that properties have changed

                        // Navigate based on new user role
                        if (CurrentUser?.Role == Core.Entities.UserRole.Cashier)
                        {
                            MenuListBox.SelectedIndex = 1;
                        }
                        else
                        {
                            MenuListBox.SelectedIndex = 0;
                            MainFrame.Navigate(GetOrCreatePage(0));
                        }

                        PageTitle.Text = CurrentUser?.Role == Core.Entities.UserRole.Cashier
                            ? GetPageTitle(1)
                            : GetPageTitle(0);

                        Show();
                    }
                    else
                    {
                        System.Windows.Application.Current?.Shutdown();
                    }
                }
            }
            catch
            {
                System.Windows.Application.Current?.Shutdown();
            }
        }
    }

    private void CloseApp_Click(object sender, RoutedEventArgs e)
    {
        var confirmMsg = (System.Windows.Application.Current?.Resources["Loc_ConfirmCloseMessage"] as string) ?? "هل أنت متأكد من إغلاق البرنامج؟";
        var confirmTitle = (System.Windows.Application.Current?.Resources["Loc_ConfirmCloseTitle"] as string) ?? "تأكيد الإغلاق";
        var result = MessageBox.Show(confirmMsg, confirmTitle, MessageBoxButton.YesNo, MessageBoxImage.Question);

        if (result == MessageBoxResult.Yes)
        {
            System.Windows.Application.Current?.Shutdown();
        }
    }

    private void ApplyZoom(double factor)
    {
        RootScaleTransform.ScaleX = factor;
        RootScaleTransform.ScaleY = factor;
        if (ZoomPercentText != null)
        {
            ZoomPercentText.Text = $"{(int)Math.Round(factor * 100)}%";
        }
    }

    private void QuickThemeToggle_Click(object sender, RoutedEventArgs e)
    {
        _themeService?.ToggleThemeMode();
    }

    private void ZoomIn_Click(object sender, RoutedEventArgs e)
    {
        _themeService?.ZoomIn();
    }

    private void ZoomOut_Click(object sender, RoutedEventArgs e)
    {
        _themeService?.ZoomOut();
    }

    private void ZoomReset_Click(object sender, RoutedEventArgs e)
    {
        _themeService?.ResetZoom();
    }

    private void SettingsButton_Click(object sender, RoutedEventArgs e)
    {
        // Navigate to Settings page (index 17)
        MenuListBox.SelectedIndex = 17;
        MainFrame.Navigate(GetOrCreatePage(17));
        PageTitle.Text = GetPageTitle(17);
    }

    // ─── Window Control: Minimize / Restore-Maximize / Drag ──────────────────

    private void MinimizeWindow_Click(object sender, RoutedEventArgs e)
    {
        WindowState = WindowState.Minimized;
    }

    private void RestoreMaxWindow_Click(object sender, RoutedEventArgs e)
    {
        if (WindowState == WindowState.Maximized)
        {
            WindowState = WindowState.Normal;
            RestoreMaxIcon.Kind = MaterialDesignThemes.Wpf.PackIconKind.WindowMaximize;
        }
        else
        {
            WindowState = WindowState.Maximized;
            RestoreMaxIcon.Kind = MaterialDesignThemes.Wpf.PackIconKind.WindowRestore;
        }
    }

    private void TopBar_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
    {
        // Allow dragging only in windowed (non-fullscreen) mode
        if (WindowStyle != WindowStyle.None || WindowState != WindowState.Maximized)
        {
            if (e.ClickCount == 2)
            {
                RestoreMaxWindow_Click(sender, e);
            }
            else
            {
                try { DragMove(); } catch { }
            }
        }
        else if (e.ClickCount == 2)
        {
            // Double-click on topbar in fullscreen → switch to windowed
            ApplyWindowMode("Windowed");
            Settings?.SaveSettingAsync("WindowMode", "Windowed");
        }
    }

    /// <summary>
    /// Apply window mode from Settings. Call this on startup and when user changes it.
    /// mode: "Fullscreen" | "Windowed" | "Maximized"
    /// </summary>
    public void ApplyWindowMode(string mode)
    {
        switch (mode?.Trim().ToLowerInvariant())
        {
            case "windowed":
                WindowStyle = WindowStyle.None;   // Keep our custom chrome (no OS chrome)
                ResizeMode = ResizeMode.CanResize;
                WindowState = WindowState.Normal;
                Width = 1400;
                Height = 850;
                WindowStartupLocation = WindowStartupLocation.CenterScreen;
                Left = (SystemParameters.PrimaryScreenWidth - Width) / 2;
                Top = (SystemParameters.PrimaryScreenHeight - Height) / 2;
                WindowControlsBar.Visibility = Visibility.Visible;
                break;

            case "maximized":
                WindowStyle = WindowStyle.None;
                ResizeMode = ResizeMode.CanResize;
                WindowState = WindowState.Maximized;
                WindowControlsBar.Visibility = Visibility.Visible;
                break;

            case "fullscreen":
            default:
                WindowStyle = WindowStyle.None;
                ResizeMode = ResizeMode.NoResize;
                WindowState = WindowState.Maximized;
                WindowControlsBar.Visibility = Visibility.Collapsed;
                break;
        }
        // Update restore/max icon
        if (RestoreMaxIcon != null)
            RestoreMaxIcon.Kind = WindowState == WindowState.Maximized
                ? MaterialDesignThemes.Wpf.PackIconKind.WindowRestore
                : MaterialDesignThemes.Wpf.PackIconKind.WindowMaximize;
    }
}
