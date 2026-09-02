using System.Collections.Generic;
using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.Messaging;
using System.Drawing.Printing;
using System.IO;
using System.Windows;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.EntityFrameworkCore;
using Microsoft.Win32;
using SmartPOS.Core.Entities;
using SmartPOS.Core.Interfaces;
using SmartPOS.Infrastructure.Data;
using SmartPOS.Infrastructure.Services;

namespace SmartPOS.Application.ViewModels;

public partial class SettingsViewModel : BaseViewModel, IDisposable, CommunityToolkit.Mvvm.Messaging.IRecipient<SmartPOS.Application.Messages.BarcodeScannedMessage>
{
    private readonly ISettingsService _settingsService;
    private readonly IBackupService _backupService;
    private readonly ILicenseService _licenseService;
    private readonly IPrintingService _printingService;
    private readonly IBarcodeService _barcodeService;
    private readonly User _currentUser;

    [ObservableProperty]
    private string _lastScannedBarcode = string.Empty;

    // ── Store ─────────────────────────────────────────────────────────────────
    [ObservableProperty] private string _storeName = string.Empty;
    [ObservableProperty] private string _storeAddress = string.Empty;
    [ObservableProperty] private string _storePhone = string.Empty;
    [ObservableProperty] private string _storeTaxNumber = string.Empty;
    [ObservableProperty] private string _storeLogoPath = string.Empty;
    [ObservableProperty] private string _footerMessage = string.Empty;
    [ObservableProperty] private decimal _taxPercentage;

    // ── Receipt Printer ──────────────────────────────────────────────────────
    [ObservableProperty] private string _printerName = string.Empty;
    [ObservableProperty] private List<string> _availablePrinters = new();
    [ObservableProperty] private int _receiptWidth = 80;
    [ObservableProperty] private string _receiptLanguage = "Both";
    [ObservableProperty] private string _receiptFontSize = "طبيعي";

    public List<int> ReceiptWidthOptions => new() { 58, 80 };
    public List<string> ReceiptLanguageOptions => new() { "Arabic", "English", "Both" };
    public List<string> ReceiptFontSizeOptions => new() { "صغير جداً", "صغير", "طبيعي", "كبير", "كبير جداً" };

    [ObservableProperty] private bool _autoPrintReceipt = true;

    // ── Cash Drawer ───────────────────────────────────────────────────────────
    [ObservableProperty] private string _cashDrawerPrinter = string.Empty;
    [ObservableProperty] private bool _autoOpenDrawer = true;
    [ObservableProperty] private string _drawerPin = "1";

    public List<string> DrawerPinOptions => new() { "1 (Pin 2 – الأكثر شيوعاً)", "2 (Pin 5 – APG)" };

    // ── Barcode Scanner ───────────────────────────────────────────────────────
    [ObservableProperty] private string _barcodeMode = "HID";
    [ObservableProperty] private string _barcodeCOMPort = "COM1";
    [ObservableProperty] private int _barcodeBaudRate = 9600;
    [ObservableProperty] private int _barcodeTimeoutMs = 100;
    [ObservableProperty] private List<string> _availableCOMPorts = new();

    public List<string> BarcodeModeOptions => new() { "HID", "Serial" };
    public List<int> BaudRateOptions => new() { 2400, 4800, 9600, 19200, 38400, 57600, 115200 };
    public bool IsSerialBarcode => BarcodeMode == "Serial";

    partial void OnBarcodeModeChanged(string value) => OnPropertyChanged(nameof(IsSerialBarcode));

    // ── Shift / Z-Report ─────────────────────────────────────────────────────
    [ObservableProperty] private bool _printZReportOnClose = true;
    [ObservableProperty] private bool _saveZReportPdfOnClose = true;

    // ── White-Label ───────────────────────────────────────────────────────────
    [ObservableProperty] private string _appTitle = string.Empty;
    [ObservableProperty] private string _appLogoPath = string.Empty;

    // ── Activation ────────────────────────────────────────────────────────────
    [ObservableProperty] private string _deviceId = string.Empty;
    [ObservableProperty] private string _activationStatusTitle = string.Empty;
    [ObservableProperty] private string _activationStatusDetails = string.Empty;
    [ObservableProperty] private string _licenseExpiry = string.Empty;
    [ObservableProperty] private string _licenseRemaining = string.Empty;

    // ── Telegram & Licensing ──────────────────────────────────────────────────
    [ObservableProperty] private string _telegramBotToken = "8802777585:AAHdhh-LQGgGP09Ge1MGb_kYG21Dk-ZCHZM";
    [ObservableProperty] private string _telegramChatId = "624875667";
    [ObservableProperty] private string _activationKeyInput = string.Empty;

    // ── Permissions ───────────────────────────────────────────────────────────
    public bool IsAdmin => _currentUser?.Role == UserRole.Admin || _currentUser?.Role == UserRole.SuperAdmin;
    public bool IsSuperAdmin => _currentUser?.Role == UserRole.SuperAdmin;

    private readonly LanHttpServerService? _lanServer;
    private readonly SmartPOS.Core.Interfaces.IThemeService? _themeService;
    private readonly SmartPOS.Core.Interfaces.ILocalizationService? _localizationService;

    // ── Theme & UI Customization ─────────────────────────────────────────────
    [ObservableProperty] private int _selectedThemeModeIndex = 0;
    [ObservableProperty] private int _selectedColorPaletteIndex = 2; // Default CyanSky
    [ObservableProperty] private string _customAccentHex = "#00B4D8";
    [ObservableProperty] private double _uiZoomFactor = 1.0;
    [ObservableProperty] private string _uiZoomPercentText = "100%";
    [ObservableProperty] private string _windowMode = "Fullscreen";
    [ObservableProperty] private int _selectedWindowModeIndex = 0;
    [ObservableProperty] private string _appLanguage = "ar";
    [ObservableProperty] private int _selectedLanguageIndex = 0;

    // ── Regional Market & Currency & Payment Methods ─────────────────────────
    [ObservableProperty] private string _selectedCountryCode = "EG";
    [ObservableProperty] private string _currencySymbol = "ج.م";
    [ObservableProperty] private string _currencyName = "جنيه مصري";
    [ObservableProperty] private string _businessSector = "General";
    [ObservableProperty] private ObservableCollection<PaymentMethodSettingItem> _paymentMethodSettings = new();

    public List<string> CountryCodeOptions => new()
    {
        "SA (المملكة العربية السعودية)",
        "EG (جمهورية مصر العربية)",
        "AE (الإمارات العربية المتحدة)",
        "KW (دولة الكويت)",
        "QA (دولة قطر)",
        "BH (مملكة البحرين)",
        "OM (سلطنة عُمان)",
        "OTHER (سوق عام / دول أخرى)"
    };

    // ── LAN Network & Multi-Branch & Synchronization ───────────────────────────
    [ObservableProperty] private string _lanServerUrl = "http://localhost:7890";
    [ObservableProperty] private string _wmsWebUrl = "http://localhost:7890/wms/";
    [ObservableProperty] private string _lanSessionToken = string.Empty;
    [ObservableProperty] private string _lanServerStatus = "🟢 سيرفر الربط المحلي (Port 7890) نشط وجاهز لمزامنة الكاشيرات الفرعية";
    [ObservableProperty] private string _branchName = "الفرع الرئيسي";
    [ObservableProperty] private string _branchCode = "BR-01";
    [ObservableProperty] private int _branchCount = 1;
    [ObservableProperty] private string _branchRole = "CentralServer";
    [ObservableProperty] private string _syncMode = "LAN_AutoSync";

    // ─────────────────────────────────────────────────────────────────────────
    public SettingsViewModel(
        ISettingsService settingsService,
        IBackupService backupService,
        ILicenseService licenseService,
        IPrintingService printingService,
        IBarcodeService barcodeService,
        User currentUser,
        IDbContextFactory<AppDbContext> dbContextFactory,
        LanHttpServerService? lanServer = null,
        SmartPOS.Core.Interfaces.IThemeService? themeService = null,
        SmartPOS.Core.Interfaces.ILocalizationService? localizationService = null)
    {
        _settingsService = settingsService;
        _backupService = backupService;
        _licenseService = licenseService;
        _printingService = printingService;
        _barcodeService = barcodeService;
        _currentUser = currentUser;
        _lanServer = lanServer;
        _themeService = themeService;
        _localizationService = localizationService;

        // تهيئة جسر WMS QR
        InitWmsBridge(dbContextFactory, licenseService);

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
            LastScannedBarcode = message.Value;
        });
    }

    private async Task InitializeAsync()
    {
        await ExecuteBusyAsync(async () =>
        {
            LoadPrinters();
            LoadCOMPorts();
            LoadSettings();
            DeviceId = _licenseService.GetDeviceId();
            if (_lanServer != null)
            {
                LanServerUrl = _lanServer.ServerUrl;
                WmsWebUrl = !string.IsNullOrEmpty(_lanServer.ServerUrl) ? $"{_lanServer.ServerUrl}/wms/" : "http://localhost:7890/wms/";
                LanSessionToken = _lanServer.SessionToken;
                LanServerStatus = !string.IsNullOrEmpty(_lanServer.ServerUrl)
                    ? $"🟢 سيرفر الربط المحلي يعمل بنجاح على: {_lanServer.ServerUrl}"
                    : "🟢 سيرفر الربط المحلي (Port 7890) نشط وجاهز لمزامنة الكاشيرات الفرعية";
            }
            await RefreshActivationStatusCoreAsync();
        }, "⏳ جاري تحميل الإعدادات...", "✅ تم التحميل");
    }

    // ── Loaders ───────────────────────────────────────────────────────────────
    private void LoadPrinters()
    {
        AvailablePrinters = _printingService.GetAvailablePrinters();
    }

    private void LoadCOMPorts()
    {
        try
        {
            AvailableCOMPorts = System.IO.Ports.SerialPort.GetPortNames()
                .OrderBy(p => p).ToList();
            if (!AvailableCOMPorts.Any())
                AvailableCOMPorts = new List<string> { "COM1", "COM2", "COM3", "COM4" };
        }
        catch
        {
            AvailableCOMPorts = new List<string> { "COM1", "COM2", "COM3", "COM4" };
        }
    }

    private void LoadSettings()
    {
        // Store
        StoreName = _settingsService.StoreName;
        StoreAddress = _settingsService.StoreAddress;
        StorePhone = _settingsService.StorePhone;
        StoreTaxNumber = _settingsService.StoreTaxNumber;
        StoreLogoPath = _settingsService.StoreLogoPath;
        FooterMessage = _settingsService.FooterMessage;
        TaxPercentage = _settingsService.TaxPercentage;

        // Printer
        PrinterName = _settingsService.PrinterName;
        ReceiptWidth = _settingsService.ReceiptWidth;
        ReceiptLanguage = _settingsService.ReceiptLanguage;
        ReceiptFontSize = _settingsService.ReceiptFontSize;
        AutoPrintReceipt = _settingsService.AutoPrintReceipt;

        // Cash Drawer
        CashDrawerPrinter = _settingsService.CashDrawerPrinter;
        AutoOpenDrawer = _settingsService.AutoOpenDrawer;
        DrawerPin = _settingsService.DrawerPin == "2"
            ? "2 (Pin 5 – APG)"
            : "1 (Pin 2 – الأكثر شيوعاً)";

        // Barcode
        BarcodeMode = _settingsService.BarcodeMode;
        BarcodeCOMPort = _settingsService.BarcodeCOMPort;
        BarcodeBaudRate = _settingsService.BarcodeBaudRate;
        BarcodeTimeoutMs = _settingsService.BarcodeTimeoutMs;

        // Shift
        PrintZReportOnClose = _settingsService.PrintZReportOnClose;
        SaveZReportPdfOnClose = _settingsService.SaveZReportPdfOnClose;

        // Telegram
        var savedToken = _settingsService.GetSetting("TelegramBotToken");
        TelegramBotToken = !string.IsNullOrWhiteSpace(savedToken) ? savedToken : "8802777585:AAHdhh-LQGgGP09Ge1MGb_kYG21Dk-ZCHZM";

        var savedChatId = _settingsService.GetSetting("TelegramChatId");
        TelegramChatId = !string.IsNullOrWhiteSpace(savedChatId) ? savedChatId : "624875667";

        // Theme & UI Customization
        var modeStr = _settingsService.AppThemeMode;
        SelectedThemeModeIndex = modeStr switch
        {
            "Light" => 1,
            "System" => 2,
            _ => 0
        };

        var paletteStr = _settingsService.AppColorPalette;
        SelectedColorPaletteIndex = paletteStr switch
        {
            "FluentAzure" => 1,
            "CyanSky" => 2,
            "EmeraldGreen" => 3,
            "RoyalGold" => 4,
            "CyberPurple" => 5,
            "CrimsonRose" => 6,
            "Custom" => 7,
            _ => 2 // Default CyanSky
        };
        CustomAccentHex = _settingsService.CustomAccentColorHex;

        UiZoomFactor = _settingsService.AppUiZoomFactor;
        UiZoomPercentText = $"{(int)Math.Round(UiZoomFactor * 100)}%";

        // Window & Screen Display Mode
        WindowMode = _settingsService.GetSetting("WindowMode") ?? "Fullscreen";
        SelectedWindowModeIndex = WindowMode switch
        {
            "Maximized" => 1,
            "Windowed" => 2,
            _ => 0
        };

        // App Interface Language
        AppLanguage = _settingsService.AppLanguage;
        SelectedLanguageIndex = AppLanguage == "en" ? 1 : 0;

        // Regional Market & Currency
        SelectedCountryCode = _settingsService.CountryCode;
        CurrencySymbol = _settingsService.CurrencySymbol;
        CurrencyName = _settingsService.CurrencyName;
        BusinessSector = _settingsService.BusinessSector;
        InitPaymentMethodsList(_settingsService.ActivePaymentMethodsJson, SelectedCountryCode);

        // Multi-Branch & Synchronization
        BranchName = _settingsService.BranchName;
        BranchCode = _settingsService.BranchCode;
        BranchCount = _settingsService.BranchCount;
        BranchRole = _settingsService.BranchRole;
        SyncMode = _settingsService.SyncMode;
    }

    private void InitPaymentMethodsList(string savedJson, string country)
    {
        var allMethods = new List<PaymentMethodSettingItem>
        {
            // Global / Basic
            new() { Method = PaymentMethod.Cash, DisplayName = "كاش (Cash)", MarketCategory = "عام", IsEnabled = true },
            new() { Method = PaymentMethod.Card, DisplayName = "بطاقة / فيزا / ماستر (Card)", MarketCategory = "عام", IsEnabled = true },
            new() { Method = PaymentMethod.Deferred, DisplayName = "آجل (حساب العميل)", MarketCategory = "عام", IsEnabled = true },
            new() { Method = PaymentMethod.ApplePay, DisplayName = "Apple Pay", MarketCategory = "عام / الخليج", IsEnabled = country != "EG" },

            // Saudi Arabia
            new() { Method = PaymentMethod.Mada, DisplayName = "مدى (Mada)", MarketCategory = "السعودية", IsEnabled = country == "SA" },
            new() { Method = PaymentMethod.StcPay, DisplayName = "STC Pay", MarketCategory = "السعودية", IsEnabled = country == "SA" },
            new() { Method = PaymentMethod.Urpay, DisplayName = "Urpay", MarketCategory = "السعودية", IsEnabled = false },
            new() { Method = PaymentMethod.AlRajhiTransfer, DisplayName = "تحويل مصرف الراجحي", MarketCategory = "السعودية", IsEnabled = false },
            new() { Method = PaymentMethod.SNBTransfer, DisplayName = "تحويل البنك الأهلي SNB", MarketCategory = "السعودية", IsEnabled = false },
            new() { Method = PaymentMethod.Tamara, DisplayName = "تمارا (Tamara - تقسيط)", MarketCategory = "السعودية / الخليج", IsEnabled = false },
            new() { Method = PaymentMethod.Tabby, DisplayName = "تابي (Tabby - تقسيط)", MarketCategory = "السعودية / الخليج", IsEnabled = false },

            // UAE
            new() { Method = PaymentMethod.SamsungPay, DisplayName = "Samsung Pay", MarketCategory = "الإمارات / الخليج", IsEnabled = country == "AE" },
            new() { Method = PaymentMethod.PayBy, DisplayName = "PayBy", MarketCategory = "الإمارات", IsEnabled = country == "AE" },
            new() { Method = PaymentMethod.CareemPay, DisplayName = "Careem Pay", MarketCategory = "الإمارات", IsEnabled = false },

            // Kuwait
            new() { Method = PaymentMethod.Knet, DisplayName = "كي نت (KNET)", MarketCategory = "الكويت", IsEnabled = country == "KW" },
            new() { Method = PaymentMethod.BoubyanPay, DisplayName = "Boubyan Pay", MarketCategory = "الكويت", IsEnabled = country == "KW" },

            // Qatar
            new() { Method = PaymentMethod.Naps, DisplayName = "نابس (NAPS)", MarketCategory = "قطر", IsEnabled = country == "QA" },
            new() { Method = PaymentMethod.QPay, DisplayName = "كيو بي (QPay)", MarketCategory = "قطر", IsEnabled = country == "QA" },

            // Bahrain
            new() { Method = PaymentMethod.BenefitPay, DisplayName = "بنفت بي (BenefitPay)", MarketCategory = "البحرين", IsEnabled = country == "BH" },

            // Oman
            new() { Method = PaymentMethod.OmanNet, DisplayName = "عمان نت (OmanNet)", MarketCategory = "عمان", IsEnabled = country == "OM" },
            new() { Method = PaymentMethod.Thawani, DisplayName = "ثواني (Thawani Pay)", MarketCategory = "عمان", IsEnabled = country == "OM" },

            // Egypt
            new() { Method = PaymentMethod.InstaPay, DisplayName = "انستا باي (InstaPay)", MarketCategory = "مصر", IsEnabled = country == "EG" },
            new() { Method = PaymentMethod.VodafoneCash, DisplayName = "فودافون كاش (Vodafone Cash)", MarketCategory = "مصر", IsEnabled = country == "EG" },
            new() { Method = PaymentMethod.OrangeCash, DisplayName = "أورنج كاش (Orange Cash)", MarketCategory = "مصر", IsEnabled = false },
            new() { Method = PaymentMethod.EtisalatCash, DisplayName = "اتصالات كاش (Etisalat Cash)", MarketCategory = "مصر", IsEnabled = false },
            new() { Method = PaymentMethod.Meeza, DisplayName = "بطاقة ميزة (Meeza)", MarketCategory = "مصر", IsEnabled = country == "EG" },
            new() { Method = PaymentMethod.BankTransfer, DisplayName = "تحويل بنكي", MarketCategory = "عام", IsEnabled = false },
            new() { Method = PaymentMethod.StaffMeal, DisplayName = "وجبة ضيافة موظفين", MarketCategory = "مطاعم", IsEnabled = false },

            // Custom Methods
            new() { Method = PaymentMethod.Custom1, DisplayName = "طريقة دفع مخصصة 1 (مثال: قسيمة شراء)", MarketCategory = "مخصص", IsEnabled = false },
            new() { Method = PaymentMethod.Custom2, DisplayName = "طريقة دفع مخصصة 2 (مثال: كوبون)", MarketCategory = "مخصص", IsEnabled = false },
            new() { Method = PaymentMethod.Custom3, DisplayName = "طريقة دفع مخصصة 3", MarketCategory = "مخصص", IsEnabled = false }
        };

        if (!string.IsNullOrWhiteSpace(savedJson))
        {
            try
            {
                var enabledIds = System.Text.Json.JsonSerializer.Deserialize<List<int>>(savedJson);
                if (enabledIds != null && enabledIds.Count > 0)
                {
                    foreach (var item in allMethods)
                    {
                        item.IsEnabled = enabledIds.Contains((int)item.Method);
                    }
                }
            }
            catch { /* fallback to default above */ }
        }

        PaymentMethodSettings = new ObservableCollection<PaymentMethodSettingItem>(allMethods);
    }

    partial void OnSelectedCountryCodeChanged(string value)
    {
        var rawCode = value?.Split(' ')[0]?.Trim().ToUpperInvariant() ?? "EG";
        switch (rawCode)
        {
            case "SA":
                CurrencySymbol = "ر.س";
                CurrencyName = "ريال سعودي";
                break;
            case "EG":
                CurrencySymbol = "ج.م";
                CurrencyName = "جنيه مصري";
                break;
            case "AE":
                CurrencySymbol = "د.إ";
                CurrencyName = "درهم إماراتي";
                break;
            case "KW":
                CurrencySymbol = "د.ك";
                CurrencyName = "دينار كويتي";
                break;
            case "QA":
                CurrencySymbol = "ر.ق";
                CurrencyName = "ريال قطري";
                break;
            case "BH":
                CurrencySymbol = "د.ب";
                CurrencyName = "دينار بحريني";
                break;
            case "OM":
                CurrencySymbol = "ر.ع";
                CurrencyName = "ريال عماني";
                break;
            default:
                CurrencySymbol = "$";
                CurrencyName = "دولار / أخرى";
                break;
        }
        InitPaymentMethodsList(string.Empty, rawCode);
    }

    [RelayCommand]
    private void SelectThemeMode(string mode)
    {
        if (Enum.TryParse<SmartPOS.Core.Models.BaseThemeMode>(mode, true, out var parsedMode))
        {
            SelectedThemeModeIndex = (int)parsedMode;
            ApplyActiveTheme();
        }
    }

    [RelayCommand]
    private void SelectColorPalette(string palette)
    {
        if (Enum.TryParse<SmartPOS.Core.Models.ColorPalettePreset>(palette, true, out var parsedPalette))
        {
            SelectedColorPaletteIndex = (int)parsedPalette;
            ApplyActiveTheme();
        }
    }

    [RelayCommand]
    private void ApplyCustomColor(string? hex = null)
    {
        var targetHex = !string.IsNullOrWhiteSpace(hex) ? hex : CustomAccentHex;
        if (string.IsNullOrWhiteSpace(targetHex) || !targetHex.StartsWith("#")) targetHex = "#00B4D8";
        CustomAccentHex = targetHex;
        SelectedColorPaletteIndex = (int)SmartPOS.Core.Models.ColorPalettePreset.Custom;
        _themeService?.ApplyCustomAccent(targetHex);
    }

    [RelayCommand]
    private void SelectZoomFactor(string factorStr)
    {
        if (double.TryParse(factorStr, System.Globalization.NumberStyles.Any, System.Globalization.CultureInfo.InvariantCulture, out var factor))
        {
            UiZoomFactor = factor;
            UiZoomPercentText = $"{(int)Math.Round(factor * 100)}%";
            ApplyActiveZoom();
        }
    }

    [RelayCommand]
    private async Task SelectWindowModeAsync(string mode)
    {
        if (string.IsNullOrWhiteSpace(mode)) return;
        WindowMode = mode;
        SelectedWindowModeIndex = mode switch
        {
            "Maximized" => 1,
            "Windowed" => 2,
            _ => 0
        };
        await _settingsService.SaveSettingAsync("WindowMode", mode);
        CommunityToolkit.Mvvm.Messaging.WeakReferenceMessenger.Default.Send(new SmartPOS.Application.Messages.WindowModeChangedMessage(mode));
    }

    [RelayCommand]
    private async Task SelectLanguageAsync(string lang)
    {
        if (string.IsNullOrWhiteSpace(lang)) return;
        var cleanLang = lang.Trim().ToLowerInvariant() == "en" ? "en" : "ar";
        AppLanguage = cleanLang;
        SelectedLanguageIndex = cleanLang == "en" ? 1 : 0;
        await _settingsService.SaveSettingAsync("AppLanguage", cleanLang);
        _localizationService?.SetLanguage(cleanLang);
    }

    partial void OnUiZoomFactorChanged(double value)
    {
        UiZoomPercentText = $"{(int)Math.Round(value * 100)}%";
        ApplyActiveZoom();
    }

    private void ApplyActiveTheme()
    {
        var mode = (SmartPOS.Core.Models.BaseThemeMode)SelectedThemeModeIndex;
        var palette = (SmartPOS.Core.Models.ColorPalettePreset)SelectedColorPaletteIndex;
        if (palette == SmartPOS.Core.Models.ColorPalettePreset.Custom)
        {
            _themeService?.ApplyCustomAccent(CustomAccentHex);
        }
        else
        {
            _themeService?.ApplyTheme(mode, palette);
        }
    }

    private void ApplyActiveZoom()
    {
        _themeService?.SetZoomFactor(UiZoomFactor);
    }

    // ── Save ──────────────────────────────────────────────────────────────────
    [RelayCommand]
    private async Task SaveSettingsAsync()
    {
        await ExecuteBusyAsync(async () =>
        {
            try
            {
                // Store
                await _settingsService.SaveSettingAsync("StoreName", StoreName);
                await _settingsService.SaveSettingAsync("StoreAddress", StoreAddress);
                await _settingsService.SaveSettingAsync("StorePhone", StorePhone);
                await _settingsService.SaveSettingAsync("StoreTaxNumber", StoreTaxNumber);
                await _settingsService.SaveSettingAsync("StoreLogoPath", StoreLogoPath);
                await _settingsService.SaveSettingAsync("FooterMessage", FooterMessage);
                await _settingsService.SaveSettingAsync("TaxPercentage", TaxPercentage.ToString());

                // Printer
                await _settingsService.SaveSettingAsync("PrinterName", PrinterName);
                await _settingsService.SaveSettingAsync("ReceiptWidth", ReceiptWidth.ToString());
                await _settingsService.SaveSettingAsync("ReceiptLanguage", ReceiptLanguage);
                await _settingsService.SaveSettingAsync("ReceiptFontSize", ReceiptFontSize);
                await _settingsService.SaveSettingAsync("AutoPrintReceipt", AutoPrintReceipt.ToString().ToLower());

                // Cash Drawer
                await _settingsService.SaveSettingAsync("CashDrawerPrinter", CashDrawerPrinter);
                await _settingsService.SaveSettingAsync("AutoOpenDrawer", AutoOpenDrawer.ToString().ToLower());
                await _settingsService.SaveSettingAsync("DrawerPin", DrawerPin?.StartsWith("2") == true ? "2" : "1");

                // Barcode
                await _settingsService.SaveSettingAsync("BarcodeMode", BarcodeMode);
                await _settingsService.SaveSettingAsync("BarcodeCOMPort", BarcodeCOMPort);
                await _settingsService.SaveSettingAsync("BarcodeBaudRate", BarcodeBaudRate.ToString());
                await _settingsService.SaveSettingAsync("BarcodeTimeoutMs", BarcodeTimeoutMs.ToString());

                // Shift
                await _settingsService.SaveSettingAsync("PrintZReportOnClose", PrintZReportOnClose.ToString().ToLower());
                await _settingsService.SaveSettingAsync("SaveZReportPdfOnClose", SaveZReportPdfOnClose.ToString().ToLower());

                // Telegram
                await _settingsService.SaveSettingAsync("TelegramBotToken", TelegramBotToken);
                await _settingsService.SaveSettingAsync("TelegramChatId", TelegramChatId);

                // Regional Market & Currency
                var cleanCountry = SelectedCountryCode?.Split(' ')[0]?.Trim().ToUpperInvariant() ?? "EG";
                await _settingsService.SaveSettingAsync("CountryCode", cleanCountry);
                await _settingsService.SaveSettingAsync("CurrencySymbol", CurrencySymbol);
                await _settingsService.SaveSettingAsync("CurrencyName", CurrencyName);
                await _settingsService.SaveSettingAsync("BusinessSector", BusinessSector);

                // Multi-Branch & Synchronization
                await _settingsService.SaveSettingAsync("BranchName", BranchName);
                await _settingsService.SaveSettingAsync("BranchCode", BranchCode);
                await _settingsService.SaveSettingAsync("BranchCount", BranchCount.ToString());
                await _settingsService.SaveSettingAsync("BranchRole", BranchRole);
                await _settingsService.SaveSettingAsync("SyncMode", SyncMode);

                // Window Mode & Language
                await _settingsService.SaveSettingAsync("WindowMode", WindowMode);
                await _settingsService.SaveSettingAsync("AppLanguage", AppLanguage);

                // Payment Methods
                var enabledMethodIds = PaymentMethodSettings.Where(p => p.IsEnabled).Select(p => (int)p.Method).ToList();
                var paymentJson = System.Text.Json.JsonSerializer.Serialize(enabledMethodIds);
                await _settingsService.SaveSettingAsync("ActivePaymentMethodsJson", paymentJson);

                // White-Label (SuperAdmin only)
                if (IsSuperAdmin)
                {
                    await _settingsService.SaveSettingAsync("AppTitle", AppTitle);
                    await _settingsService.SaveSettingAsync("AppLogoPath", AppLogoPath);
                }

                // Re-configure barcode service with new settings
                _barcodeService.Configure(BarcodeMode, BarcodeCOMPort, BarcodeBaudRate, BarcodeTimeoutMs);

                MessageBox.Show(
                    "✅ تم حفظ جميع الإعدادات بنجاح.\n\nإعادة تشغيل البرنامج مطلوبة لتطبيق تغييرات هوية التطبيق.",
                    "حفظ الإعدادات", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"خطأ في حفظ الإعدادات: {ex.Message}", "خطأ", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }, "جاري حفظ الإعدادات...");
    }

    // ── Printer Actions ───────────────────────────────────────────────────────
    [RelayCommand]
    private void TestPrinter()
    {
        var name = string.IsNullOrWhiteSpace(PrinterName) ? null : PrinterName;
        if (name == null)
        {
            MessageBox.Show("يرجى اختيار طابعة الإيصال أولاً.", "تنبيه", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }
        var ok = _printingService.TestPrinter(name);
        MessageBox.Show(ok
            ? $"✅ تم إرسال صفحة الاختبار إلى:\n{name}"
            : $"❌ فشل الاتصال بالطابعة:\n{name}",
            ok ? "نجاح" : "خطأ", MessageBoxButton.OK,
            ok ? MessageBoxImage.Information : MessageBoxImage.Error);
    }

    [RelayCommand]
    private void OpenCashDrawerFromSettings()
    {
        // Use cash drawer printer if set, else fall back to receipt printer
        var drawerPrinter = !string.IsNullOrWhiteSpace(CashDrawerPrinter) ? CashDrawerPrinter : PrinterName;
        if (string.IsNullOrWhiteSpace(drawerPrinter))
        {
            MessageBox.Show("يرجى اختيار طابعة الإيصال أو طابعة الدرج أولاً.", "تنبيه", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }
        var pin = DrawerPin.StartsWith("2") ? "2" : "1";
        var ok = _printingService.OpenCashDrawer(drawerPrinter, pin);
        MessageBox.Show(ok
            ? $"✅ تم إرسال أمر فتح الدرج (Pin {pin})\nعبر طابعة: {drawerPrinter}"
            : $"❌ فشل إرسال أمر فتح الدرج.\nتأكد من توصيل كابل RJ11 بين الدرج والطابعة.",
            ok ? "نجاح" : "خطأ", MessageBoxButton.OK,
            ok ? MessageBoxImage.Information : MessageBoxImage.Error);
    }

    [RelayCommand]
    private void RefreshPrinters()
    {
        LoadPrinters();
        LoadCOMPorts();
        MessageBox.Show("تم تحديث قائمة الأجهزة.", "تحديث", MessageBoxButton.OK, MessageBoxImage.Information);
    }

    // ── Logo ──────────────────────────────────────────────────────────────────
    [RelayCommand]
    private void SelectLogo()
    {
        var dlg = new OpenFileDialog
        {
            Filter = "Image Files (*.png;*.jpg;*.jpeg)|*.png;*.jpg;*.jpeg",
            Title = "اختر شعار البرنامج"
        };
        if (dlg.ShowDialog() == true) AppLogoPath = dlg.FileName;
    }

    [RelayCommand]
    private void SelectStoreLogo()
    {
        var dlg = new OpenFileDialog
        {
            Filter = "Image Files (*.png;*.jpg;*.jpeg)|*.png;*.jpg;*.jpeg",
            Title = "اختر شعار الفاتورة (يفضل أبيض وأسود)"
        };
        if (dlg.ShowDialog() == true) StoreLogoPath = dlg.FileName;
    }

    // ── Backup / Restore / Factory Reset ──────────────────────────────────────
    [RelayCommand]
    private async Task BackupDatabaseAsync()
    {
        var dlg = new SaveFileDialog
        {
            Filter = "Database Backup (*.db)|*.db",
            FileName = $"SmartPOS_Backup_{DateTime.Now:yyyyMMdd_HHmm}.db"
        };
        if (dlg.ShowDialog() == true)
        {
            var folder = Path.GetDirectoryName(dlg.FileName);
            if (folder != null)
                await ExecuteBusyAsync(async () =>
                {
                    try
                    {
                        var path = await _backupService.CreateBackupAsync(folder);
                        MessageBox.Show($"تم إنشاء النسخة الاحتياطية:\n{path}", "نسخ احتياطي", MessageBoxButton.OK, MessageBoxImage.Information);
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show($"فشل النسخ الاحتياطي: {ex.Message}", "خطأ", MessageBoxButton.OK, MessageBoxImage.Error);
                    }
                }, "جاري إنشاء النسخة الاحتياطية...");
        }
    }

    [RelayCommand]
    private async Task RestoreDatabaseAsync()
    {
        if (MessageBox.Show("تحذير: ستُستبدل البيانات الحالية بالكامل. متأكد؟",
            "تأكيد الاستعادة", MessageBoxButton.YesNo, MessageBoxImage.Warning) != MessageBoxResult.Yes) return;

        var dlg = new OpenFileDialog { Filter = "Database Backup (*.db)|*.db" };
        if (dlg.ShowDialog() == true)
            await ExecuteBusyAsync(async () =>
            {
                try
                {
                    await _backupService.RestoreBackupAsync(dlg.FileName);
                    MessageBox.Show("تم الاستعادة. سيُعاد تشغيل البرنامج.", "نجاح", MessageBoxButton.OK, MessageBoxImage.Information);
                    System.Diagnostics.Process.Start(System.Diagnostics.Process.GetCurrentProcess().MainModule?.FileName ?? "");
                    System.Windows.Application.Current.Shutdown();
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"فشل الاستعادة: {ex.Message}", "خطأ", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }, "جاري استعادة قاعدة البيانات...");
    }

    /// <summary>
    /// Loads the bundled demo database (SmartPOS_Demo.db) shipped next to the exe.
    /// Lets new users explore the system with realistic sample data before going live.
    /// </summary>
    [RelayCommand]
    private async Task LoadDemoDatabaseAsync()
    {
        // Locate demo file next to the running executable
        var exeDir = AppDomain.CurrentDomain.BaseDirectory;
        var demoPath = Path.Combine(exeDir, "SmartPOS_Demo.db");

        if (!File.Exists(demoPath))
        {
            // Fallback: let the user browse for it
            var dlg = new OpenFileDialog
            {
                Title = "اختر ملف البيانات التجريبية",
                Filter = "Demo Database (SmartPOS_Demo.db)|SmartPOS_Demo.db|كل الملفات (*.db)|*.db",
            };
            if (dlg.ShowDialog() != true) return;
            demoPath = dlg.FileName;
        }

        if (MessageBox.Show(
            "سيتم استبدال البيانات الحالية ببيانات تجريبية جاهزة تحتوي على:\n" +
            "• 21 صنف في 6 فئات\n• 742 فاتورة بيع (30 يوم)\n• 6 عملاء + نقاط الولاء\n• 4 موردين + أوامر شراء\n• 6 أجهزة تأجير\n• 20 مصروف\n\n" +
            "هل تريد المتابعة؟",
            "تحميل البيانات التجريبية",
            MessageBoxButton.YesNo, MessageBoxImage.Question) != MessageBoxResult.Yes) return;

        await ExecuteBusyAsync(async () =>
        {
            try
            {
                await _backupService.RestoreBackupAsync(demoPath);
                MessageBox.Show(
                    "✅ تم تحميل البيانات التجريبية بنجاح!\n\nبيانات الدخول:\n" +
                    "👑 superadmin / super@2026\n🔑 admin / admin@2026\n💼 cashier1 / cashier@2026\n\n" +
                    "سيُعاد تشغيل البرنامج الآن.",
                    "نجاح", MessageBoxButton.OK, MessageBoxImage.Information);
                System.Diagnostics.Process.Start(System.Diagnostics.Process.GetCurrentProcess().MainModule?.FileName ?? "");
                System.Windows.Application.Current.Shutdown();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"فشل تحميل البيانات التجريبية: {ex.Message}", "خطأ", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }, "جاري تحميل البيانات التجريبية...");
    }

    [RelayCommand]
    private async Task FactoryResetAsync()
    {
        if (_currentUser?.Role != UserRole.SuperAdmin)
        {
            MessageBox.Show("هذا الإجراء متاح للمالك (SuperAdmin) فقط.", "مرفوض", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }
        if (MessageBox.Show("⚠️ سيتم حذف كل البيانات بالكامل.\nهل أنت متأكد؟",
            "تصفير النظام", MessageBoxButton.YesNo, MessageBoxImage.Warning) != MessageBoxResult.Yes) return;
        if (MessageBox.Show("آخر تأكيد – لا يمكن التراجع!",
            "تأكيد نهائي", MessageBoxButton.YesNo, MessageBoxImage.Stop) != MessageBoxResult.Yes) return;

        await ExecuteBusyAsync(async () =>
        {
            try
            {
                var backupDir = Path.Combine(
                    Environment.GetFolderPath(Environment.SpecialFolder.Desktop), "RoboVAI POS - نسخ احتياطية");
                Directory.CreateDirectory(backupDir);
                await _backupService.CreateBackupAsync(backupDir);

                var dbPath = SmartPOS.Infrastructure.Data.DatabasePathHelper.GetDatabasePath();

                // Clear SQLite connection pool
                Microsoft.Data.Sqlite.SqliteConnection.ClearAllPools();
                GC.Collect();
                GC.WaitForPendingFinalizers();

                using (var connection = new Microsoft.Data.Sqlite.SqliteConnection($"Data Source={dbPath}"))
                {
                    await connection.OpenAsync();

                    // 1. Temporarily disable foreign keys to allow atomic bulk deletion
                    using (var pragmaOff = connection.CreateCommand())
                    {
                        pragmaOff.CommandText = "PRAGMA foreign_keys = OFF;";
                        await pragmaOff.ExecuteNonQueryAsync();
                    }

                    var tablesToClear = new[]
                    {
                        // Child / Dependent tables first
                        "SaleDetails", "ReturnDetails", "PurchaseOrderDetails", "StockMovements",
                        "SupplierPayments", "LoyaltyTransactions", "CustomerLoyalties",
                        "RentalSessions", "AuditLogs", "Expenses", "SyncOutboxes",
                        // Parent tables
                        "Sales", "Returns", "PurchaseOrders", "RentalDevices",
                        "Products", "Categories", "Suppliers", "Customers", "Shifts"
                    };

                    using (var transaction = connection.BeginTransaction())
                    {
                        try
                        {
                            foreach (var table in tablesToClear)
                            {
                                using var cmd = connection.CreateCommand();
                                cmd.Transaction = transaction;
                                cmd.CommandText = $"DELETE FROM {table}";
                                await cmd.ExecuteNonQueryAsync();
                            }

                            using var seqCmd = connection.CreateCommand();
                            seqCmd.Transaction = transaction;
                            seqCmd.CommandText = "DELETE FROM sqlite_sequence WHERE name IN ('" + string.Join("','", tablesToClear) + "')";
                            await seqCmd.ExecuteNonQueryAsync();

                            transaction.Commit();
                        }
                        catch
                        {
                            transaction.Rollback();
                            throw;
                        }
                    }

                    // 2. Re-enable foreign keys
                    using (var pragmaOn = connection.CreateCommand())
                    {
                        pragmaOn.CommandText = "PRAGMA foreign_keys = ON;";
                        await pragmaOn.ExecuteNonQueryAsync();
                    }

                    using var vacuumCmd = connection.CreateCommand();
                    vacuumCmd.CommandText = "VACUUM";
                    await vacuumCmd.ExecuteNonQueryAsync();
                }

                MessageBox.Show($"تم تصفير النظام بنجاح!\n(تم الاحتفاظ بالإعدادات والمستخدمين)\nنسخة احتياطية في:\n{backupDir}\n\nسيُعاد تشغيل البرنامج الآن.",
                    "تم", MessageBoxButton.OK, MessageBoxImage.Information);
                var exe = System.Diagnostics.Process.GetCurrentProcess().MainModule?.FileName;
                if (!string.IsNullOrEmpty(exe)) System.Diagnostics.Process.Start(exe);
                System.Windows.Application.Current.Shutdown();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"فشل التصفير: {ex.Message}", "خطأ", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }, "جاري تصفير النظام...");
    }

    // ── Activation ────────────────────────────────────────────────────────────
    [RelayCommand]
    public async Task RefreshActivationStatusAsync()
        => await ExecuteBusyAsync(RefreshActivationStatusCoreAsync, "جاري فحص حالة التفعيل...");

    private async Task RefreshActivationStatusCoreAsync()
    {
        try
        {
            DeviceId = _licenseService.GetDeviceId();
            var s = await _licenseService.GetStatusAsync();
            if (s.IsValid && s.IsTrial) { ActivationStatusTitle = "الحالة: تجربة مجانية ✅"; ActivationStatusDetails = $"المتبقي {Math.Max(0, s.DaysRemaining)} يوم."; }
            else if (s.IsValid) { ActivationStatusTitle = "الحالة: مُفعّل ✅"; ActivationStatusDetails = "البرنامج يعمل بكامل الصلاحيات."; }
            else if (s.IsInGrace) { ActivationStatusTitle = "الحالة: انتهى (مهلة تجديد) ⚠️"; ActivationStatusDetails = "برجاء إدخال كود التجديد."; }
            else if (s.IsTrial) { ActivationStatusTitle = "الحالة: انتهت التجربة ❌"; ActivationStatusDetails = "اطلب كود التفعيل من robovai.tech"; }
            else { ActivationStatusTitle = "الحالة: غير مُفعّل ❌"; ActivationStatusDetails = "أرسل رقم الجهاز عبر واتساب."; }

            if (s.ExpiresAtUtc is null) { LicenseExpiry = ""; LicenseRemaining = ""; return; }
            var local = s.ExpiresAtUtc.Value.ToLocalTime();
            LicenseExpiry = $"تاريخ الانتهاء: {local:yyyy/MM/dd HH:mm}";
            LicenseRemaining = $"المتبقي: {Math.Max(0, s.DaysRemaining)} يوم";
        }
        catch
        {
            ActivationStatusTitle = "الحالة: غير معروفة";
            ActivationStatusDetails = "تعذر قراءة حالة التفعيل.";
            LicenseExpiry = "";
            LicenseRemaining = "";
        }
    }
}

public class PaymentMethodSettingItem : ObservableObject
{
    public PaymentMethod Method { get; set; }
    public string DisplayName { get; set; } = string.Empty;
    public string MarketCategory { get; set; } = string.Empty;

    private bool _isEnabled;
    public bool IsEnabled
    {
        get => _isEnabled;
        set => SetProperty(ref _isEnabled, value);
    }
}

