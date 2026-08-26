using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using SmartPOS.Core.Entities;
using SmartPOS.Core.Interfaces;
using SmartPOS.Infrastructure.Data;
using System.Collections.Concurrent;

namespace SmartPOS.Infrastructure.Services;

public class SettingsService : ISettingsService
{
    private readonly IDbContextFactory<AppDbContext> _contextFactory;
    private ConcurrentDictionary<string, string> _settingsCache = new();

    public SettingsService(IDbContextFactory<AppDbContext> contextFactory)
    {
        _contextFactory = contextFactory;
    }

    public async Task LoadSettingsAsync()
    {
        try
        {
            await using var context = await _contextFactory.CreateDbContextAsync();
            var settings = await context.AppSettings.AsNoTracking().ToListAsync();
            _settingsCache = new ConcurrentDictionary<string, string>(
                settings.ToDictionary(s => s.Key, s => s.Value));
        }
        catch
        {
            try
            {
                await using var context = await _contextFactory.CreateDbContextAsync();
                await context.Database.ExecuteSqlRawAsync(
                    @"CREATE TABLE IF NOT EXISTS ""AppSettings"" (""Key"" TEXT NOT NULL CONSTRAINT ""PK_AppSettings"" PRIMARY KEY, ""Value"" TEXT NOT NULL);");
                var settings = await context.AppSettings.AsNoTracking().ToListAsync();
                _settingsCache = new ConcurrentDictionary<string, string>(
                    settings.ToDictionary(s => s.Key, s => s.Value));
            }
            catch
            {
                _settingsCache = new ConcurrentDictionary<string, string>();
            }
        }
    }

    public string GetSetting(string key, string defaultValue = "")
        => _settingsCache.TryGetValue(key, out var value) ? value : defaultValue;

    public async Task SaveSettingAsync(string key, string value)
    {
        var safeValue = value ?? string.Empty;
        try
        {
            await using var context = await _contextFactory.CreateDbContextAsync();
            var setting = await context.AppSettings.FindAsync(key);
            if (setting == null)
            {
                setting = new AppSetting { Key = key, Value = safeValue };
                context.AppSettings.Add(setting);
            }
            else
            {
                setting.Value = safeValue;
            }
            await context.SaveChangesAsync();
        }
        catch
        {
            try
            {
                await using var context = await _contextFactory.CreateDbContextAsync();
                await context.Database.ExecuteSqlRawAsync(
                    @"CREATE TABLE IF NOT EXISTS ""AppSettings"" (""Key"" TEXT NOT NULL CONSTRAINT ""PK_AppSettings"" PRIMARY KEY, ""Value"" TEXT NOT NULL);");
                var setting = await context.AppSettings.FindAsync(key);
                if (setting == null)
                {
                    setting = new AppSetting { Key = key, Value = safeValue };
                    context.AppSettings.Add(setting);
                }
                else
                {
                    setting.Value = safeValue;
                }
                await context.SaveChangesAsync();
            }
            catch { /* safe fallback */ }
        }
        _settingsCache.AddOrUpdate(key, safeValue, (_, _) => safeValue);
    }

    // ─── Store Info ───────────────────────────────────────────────────────────
    public string StoreName    => GetSetting("StoreName",    "RobovAI PRO POS");
    public string StoreAddress => GetSetting("StoreAddress", "");
    public string StorePhone   => GetSetting("StorePhone",   "");
    public string StoreTaxNumber => GetSetting("StoreTaxNumber", "");
    public string StoreLogoPath  => GetSetting("StoreLogoPath", "");
    public string FooterMessage => GetSetting("FooterMessage", "شكراً لزيارتكم - Thank You");
    public decimal TaxPercentage => decimal.TryParse(GetSetting("TaxPercentage", "0"), out var v) ? v : 0;

    // ─── Receipt Printer ─────────────────────────────────────────────────────
    public string PrinterName    => GetSetting("PrinterName", "");
    public int    ReceiptWidth   => int.TryParse(GetSetting("ReceiptWidth", "80"), out var w) ? w : 80;
    public bool   AutoPrintReceipt => GetSetting("AutoPrintReceipt", "true").Equals("true", StringComparison.OrdinalIgnoreCase);
    /// "Arabic" | "English" | "Both"
    public string ReceiptLanguage => GetSetting("ReceiptLanguage", "Both");

    public string ReceiptFontSize => GetSetting("ReceiptFontSize", "طبيعي");
    public int ReceiptFontSizeOffset => ReceiptFontSize switch
    {
        "صغير جداً" => -2,
        "صغير" => -1,
        "طبيعي" => 0,
        "كبير" => 1,
        "كبير جداً" => 2,
        _ => 0
    };

    // ─── Cash Drawer ─────────────────────────────────────────────────────────
    public string CashDrawerPrinter => GetSetting("CashDrawerPrinter", ""); // empty = same as PrinterName
    public bool   AutoOpenDrawer    => GetSetting("AutoOpenDrawer", "true").Equals("true", StringComparison.OrdinalIgnoreCase);
    public string DrawerPin         => GetSetting("DrawerPin", "1"); // "1" = pin2, "2" = pin5

    // ─── Barcode Scanner ─────────────────────────────────────────────────────
    public string BarcodeMode      => GetSetting("BarcodeMode", "HID");   // "HID" or "Serial"
    public string BarcodeCOMPort   => GetSetting("BarcodeCOMPort", "COM1");
    public int    BarcodeBaudRate  => int.TryParse(GetSetting("BarcodeBaudRate", "9600"), out var b) ? b : 9600;
    public int    BarcodeTimeoutMs => int.TryParse(GetSetting("BarcodeTimeoutMs", "100"), out var t) ? t : 100;

    // ─── Shift Closing ───────────────────────────────────────────────────────
    public bool PrintZReportOnClose   => GetSetting("PrintZReportOnClose",   "true").Equals("true",  StringComparison.OrdinalIgnoreCase);
    public bool SaveZReportPdfOnClose => GetSetting("SaveZReportPdfOnClose", "true").Equals("true", StringComparison.OrdinalIgnoreCase);

    // ─── White-Label ─────────────────────────────────────────────────────────
    public string AppTitle    => GetSetting("AppTitle",    "RobovAI PRO POS");
    public string AppLogoPath => GetSetting("AppLogoPath", "/Assets/logo.png");

    // ─── Auto Backup ─────────────────────────────────────────────────────────
    public bool   AutoBackupEnabled => GetSetting("AutoBackupEnabled", "false").Equals("true", StringComparison.OrdinalIgnoreCase);
    public string BackupFolder      => GetSetting("BackupFolder", Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Desktop), "SmartPOS_Backups"));
    public int    MaxBackupCount    => int.TryParse(GetSetting("MaxBackupCount", "7"), out var n) ? n : 7;

    // ─── Kitchen Printer ─────────────────────────────────────────────────────
    public bool   KitchenPrinterEnabled => GetSetting("KitchenPrinterEnabled", "false").Equals("true", StringComparison.OrdinalIgnoreCase);
    public string KitchenPrinterName    => GetSetting("KitchenPrinterName", "");

    // ─── Theme & UI Customization ─────────────────────────────────────────────
    public string AppThemeMode         => GetSetting("AppThemeMode", "Dark");
    public string AppColorPalette      => GetSetting("AppColorPalette", "CyanSky");
    public string CustomAccentColorHex => GetSetting("CustomAccentColorHex", "#00B4D8");
    public double AppUiZoomFactor      => double.TryParse(GetSetting("AppUiZoomFactor", "1.0"), System.Globalization.NumberStyles.Any, System.Globalization.CultureInfo.InvariantCulture, out var z) ? Math.Clamp(z, 0.75, 2.0) : 1.0;

    // ─── Regional Market, Currency & Payment Methods ─────────────────────────
    public string CurrencySymbol           => GetSetting("CurrencySymbol", "ج.م");
    public string CurrencyName             => GetSetting("CurrencyName", "جنيه مصري");
    public string CountryCode              => GetSetting("CountryCode", "EG");
    public string ActivePaymentMethodsJson => GetSetting("ActivePaymentMethodsJson", "");
    public bool   IsFirstRunCompleted      => GetSetting("IsFirstRunCompleted", "false").Equals("true", StringComparison.OrdinalIgnoreCase);
    public string BusinessSector           => GetSetting("BusinessSector", "General");

    // ─── Multi-Branch & Synchronization ──────────────────────────────────────
    public string BranchName               => GetSetting("BranchName", "الفرع الرئيسي");
    public string BranchCode               => GetSetting("BranchCode", "BR-01");
    public int    BranchCount              => int.TryParse(GetSetting("BranchCount", "1"), out var n) ? n : 1;
    public string BranchRole               => GetSetting("BranchRole", "CentralServer");
    public string SyncMode                 => GetSetting("SyncMode", "LAN_AutoSync");
    public int    AutoLockTimeoutMinutes   => int.TryParse(GetSetting("AutoLockTimeoutMinutes", "15"), out var m) ? m : 15;
}
