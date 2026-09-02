namespace SmartPOS.Core.Interfaces;

public interface ISettingsService
{
    Task LoadSettingsAsync();
    string GetSetting(string key, string defaultValue = "");
    Task SaveSettingAsync(string key, string value);

    // ─── Store Info ───────────────────────────────────────────────────────────
    string StoreName { get; }
    string StoreAddress { get; }
    string StorePhone { get; }
    string StoreTaxNumber { get; }
    string StoreLogoPath { get; }
    string FooterMessage { get; }
    decimal TaxPercentage { get; }

    // ─── Receipt Printer ─────────────────────────────────────────────────────
    /// <summary>The thermal receipt printer name as it appears in Windows.</summary>
    string PrinterName { get; }

    /// <summary>Receipt paper width: 58 (chars=32) or 80 (chars=48). Default 80.</summary>
    int ReceiptWidth { get; }

    /// <summary>Automatically print the receipt after completing a sale.</summary>
    bool AutoPrintReceipt { get; }

    /// <summary>
    /// Language printed on the thermal receipt.
    /// "Arabic"  → full Arabic
    /// "English" → full English
    /// "Both"    → Arabic + English bilingual (default)
    /// </summary>
    string ReceiptLanguage { get; }

    /// <summary>Font size user choice: "صغير جداً", "صغير", "طبيعي", "كبير", "كبير جداً"</summary>
    string ReceiptFontSize { get; }

    /// <summary>Integer offset computed from ReceiptFontSize (e.g., -2 to +2)</summary>
    int ReceiptFontSizeOffset { get; }

    // ─── Cash Drawer ─────────────────────────────────────────────────────────
    /// <summary>
    /// Printer that drives the cash drawer (RJ11 kick port).
    /// Leave empty to use the same printer as the receipt printer.
    /// </summary>
    string CashDrawerPrinter { get; }

    /// <summary>Open the drawer automatically after every successful sale.</summary>
    bool AutoOpenDrawer { get; }

    /// <summary>
    /// ESC/POS drawer pulse: "1" = pin 2 (most common), "2" = pin 5.
    /// </summary>
    string DrawerPin { get; }

    // ─── Barcode Scanner ─────────────────────────────────────────────────────
    /// <summary>"HID" (USB keyboard emulation – default) or "Serial" (COM port).</summary>
    string BarcodeMode { get; }

    /// <summary>COM port number for serial barcode scanner, e.g. "COM3". Used only when BarcodeMode = "Serial".</summary>
    string BarcodeCOMPort { get; }

    /// <summary>Baud rate for serial scanner. Default 9600.</summary>
    int BarcodeBaudRate { get; }

    /// <summary>
    /// Milliseconds between keystrokes that still count as one barcode burst.
    /// Increase if scanner is slow (150–200 ms). Decrease for very fast scanners (50 ms).
    /// Default 100 ms.
    /// </summary>
    int BarcodeTimeoutMs { get; }

    // ─── Shift Closing ───────────────────────────────────────────────────────
    /// <summary>Print the Z-Report on the thermal printer when closing a shift.</summary>
    bool PrintZReportOnClose { get; }

    /// <summary>Save a PDF copy of the Z-Report automatically when closing a shift.</summary>
    bool SaveZReportPdfOnClose { get; }

    // ─── White-Label ─────────────────────────────────────────────────────────
    string AppTitle { get; }
    string AppLogoPath { get; }

    // ─── Auto Backup ─────────────────────────────────────────────────────────
    bool AutoBackupEnabled { get; }
    string BackupFolder { get; }
    int MaxBackupCount { get; }

    // ─── Kitchen Printer ─────────────────────────────────────────────────────
    bool KitchenPrinterEnabled { get; }
    string KitchenPrinterName { get; }

    // ─── Theme & UI Customization ─────────────────────────────────────────────
    /// <summary>"Dark" | "Light" | "System"</summary>
    string AppThemeMode { get; }
    /// <summary>"DeepSpace" | "FluentAzure" | "CyanSky" | "EmeraldGreen" | "RoyalGold" | "CyberPurple" | "CrimsonRose" | "Custom"</summary>
    string AppColorPalette { get; }
    /// <summary>Custom Accent Hex Code (e.g. #00B4D8 or #0284C7)</summary>
    string CustomAccentColorHex { get; }
    /// <summary>UI Zoom Factor: 0.8 to 1.75 (Default 1.0)</summary>
    double AppUiZoomFactor { get; }
    /// <summary>App UI Language: "ar" (Arabic RTL) | "en" (English LTR)</summary>
    string AppLanguage { get; }

    // ─── Regional Market, Currency & Payment Methods ─────────────────────────
    string CurrencySymbol { get; }
    string CurrencyName { get; }
    string CountryCode { get; }
    string ActivePaymentMethodsJson { get; }
    bool IsFirstRunCompleted { get; }
    string BusinessSector { get; }

    // ─── Multi-Branch & Synchronization ──────────────────────────────────────
    string BranchName { get; }
    string BranchCode { get; }
    int BranchCount { get; }
    string BranchRole { get; }
    string SyncMode { get; }
    int AutoLockTimeoutMinutes { get; }
}
