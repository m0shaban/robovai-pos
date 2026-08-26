using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Media;
using MaterialDesignThemes.Wpf;
using Microsoft.Win32;
using SmartPOS.Core.Interfaces;
using SmartPOS.Core.Models;

namespace SmartPOS.WPF.Services;

public class ThemeService : IThemeService
{
    private readonly ISettingsService _settingsService;
    private readonly PaletteHelper _paletteHelper = new();

    public BaseThemeMode CurrentThemeMode { get; private set; } = BaseThemeMode.Dark;
    public ColorPalettePreset CurrentPalette { get; private set; } = ColorPalettePreset.CyanSky;
    public double CurrentZoomFactor { get; private set; } = 1.0;

    public event EventHandler? ThemeChanged;
    public event EventHandler<double>? ZoomChanged;

    public IReadOnlyList<ColorPaletteInfo> AvailablePalettes { get; } = new List<ColorPaletteInfo>
    {
        new()
        {
            Preset = ColorPalettePreset.CyanSky,
            NameAr = "السماوي الفيروزي (Sky Cyan)",
            NameEn = "Microsoft Sky Cyan",
            PrimaryColorHex = "#00B4D8",
            SecondaryColorHex = "#0077B6",
            AccentColorHex = "#90E0EF"
        },
        new()
        {
            Preset = ColorPalettePreset.CrimsonRose,
            NameAr = "القرمزي والعنابي الفاخر (Crimson Rose)",
            NameEn = "Crimson Velvet Rose",
            PrimaryColorHex = "#E11D48",
            SecondaryColorHex = "#BE123C",
            AccentColorHex = "#FB7185"
        },
        new()
        {
            Preset = ColorPalettePreset.FluentAzure,
            NameAr = "ميكروسوفت فلوينت (Fluent Azure)",
            NameEn = "Microsoft Fluent Azure",
            PrimaryColorHex = "#0078D4",
            SecondaryColorHex = "#005A9E",
            AccentColorHex = "#2886DE"
        },
        new()
        {
            Preset = ColorPalettePreset.DeepSpace,
            NameAr = "الفضاء الكوني (Deep Space)",
            NameEn = "Deep Space Blue",
            PrimaryColorHex = "#3B82F6",
            SecondaryColorHex = "#1D4ED8",
            AccentColorHex = "#60A5FA"
        },
        new()
        {
            Preset = ColorPalettePreset.EmeraldGreen,
            NameAr = "الزمرد الملكي (Emerald Mint)",
            NameEn = "Emerald Mint",
            PrimaryColorHex = "#10B981",
            SecondaryColorHex = "#059669",
            AccentColorHex = "#34D399"
        },
        new()
        {
            Preset = ColorPalettePreset.RoyalGold,
            NameAr = "الذهب الملكي (Royal Amber)",
            NameEn = "Royal Gold Amber",
            PrimaryColorHex = "#F59E0B",
            SecondaryColorHex = "#D97706",
            AccentColorHex = "#FBBF24"
        },
        new()
        {
            Preset = ColorPalettePreset.CyberPurple,
            NameAr = "البنفسج السيبراني (Cyber Purple)",
            NameEn = "Cyber Violet Purple",
            PrimaryColorHex = "#8B5CF6",
            SecondaryColorHex = "#6D28D9",
            AccentColorHex = "#A78BFA"
        },
        new()
        {
            Preset = ColorPalettePreset.Custom,
            NameAr = "تخصيص حر (Custom Accent)",
            NameEn = "Custom User Palette",
            PrimaryColorHex = "#00B4D8",
            SecondaryColorHex = "#0077B6",
            AccentColorHex = "#90E0EF"
        }
    };

    public ThemeService(ISettingsService settingsService)
    {
        _settingsService = settingsService;
    }

    public void Initialize()
    {
        // 1. Read stored settings
        var savedModeStr = _settingsService.AppThemeMode;
        if (Enum.TryParse<BaseThemeMode>(savedModeStr, true, out var parsedMode))
            CurrentThemeMode = parsedMode;
        else
            CurrentThemeMode = BaseThemeMode.Dark;

        var savedPaletteStr = _settingsService.AppColorPalette;
        if (Enum.TryParse<ColorPalettePreset>(savedPaletteStr, true, out var parsedPalette))
            CurrentPalette = parsedPalette;
        else
            CurrentPalette = ColorPalettePreset.CyanSky;

        CurrentZoomFactor = _settingsService.AppUiZoomFactor;
        if (CurrentZoomFactor < 0.75 || CurrentZoomFactor > 2.0)
            CurrentZoomFactor = 1.0;

        // 2. Apply theme without saving back
        ApplyTheme(CurrentThemeMode, CurrentPalette, save: false);
        SetZoomFactor(CurrentZoomFactor, save: false);

        // 3. Listen to Windows system theme changes if in System mode
        SystemEvents.UserPreferenceChanged += (s, e) =>
        {
            if (CurrentThemeMode == BaseThemeMode.System)
            {
                System.Windows.Application.Current?.Dispatcher.Invoke(() =>
                {
                    ApplyTheme(BaseThemeMode.System, CurrentPalette, save: false);
                });
            }
        };
    }

    private record ThemeEnvironment(
        Color Background,
        Color Surface,
        Color Surface2,
        Color TextPrimary,
        Color TextSecondary,
        Color Border,
        Color InputBg,
        Color InputBorder,
        Color InputFg,
        Color Primary,
        Color Secondary,
        Color Accent
    );

    private ThemeEnvironment BuildEnvironment(bool isDark, ColorPalettePreset palette)
    {
        if (palette == ColorPalettePreset.Custom)
        {
            Color customPrimary;
            try
            {
                var hex = _settingsService.CustomAccentColorHex;
                if (string.IsNullOrWhiteSpace(hex) || !hex.StartsWith("#")) hex = "#00B4D8";
                customPrimary = (Color)ColorConverter.ConvertFromString(hex);
            }
            catch
            {
                customPrimary = (Color)ColorConverter.ConvertFromString("#00B4D8");
            }

            byte r = customPrimary.R;
            byte g = customPrimary.G;
            byte b = customPrimary.B;

            var customSec = Color.FromRgb((byte)Math.Max(0, r - 35), (byte)Math.Max(0, g - 35), (byte)Math.Max(0, b - 35));
            var customAcc = Color.FromRgb((byte)Math.Min(255, r + 45), (byte)Math.Min(255, g + 45), (byte)Math.Min(255, b + 45));

            if (isDark)
            {
                return new ThemeEnvironment(
                    Background: Color.FromRgb((byte)(r * 0.08 + 6), (byte)(g * 0.08 + 8), (byte)(b * 0.08 + 12)),
                    Surface: Color.FromRgb((byte)(r * 0.16 + 12), (byte)(g * 0.16 + 14), (byte)(b * 0.16 + 20)),
                    Surface2: Color.FromRgb((byte)(r * 0.25 + 18), (byte)(g * 0.25 + 22), (byte)(b * 0.25 + 30)),
                    TextPrimary: (Color)ColorConverter.ConvertFromString("#FAFAFA"),
                    TextSecondary: (Color)ColorConverter.ConvertFromString("#CBD5E1"),
                    Border: Color.FromRgb((byte)(r * 0.32 + 24), (byte)(g * 0.32 + 28), (byte)(b * 0.32 + 38)),
                    InputBg: Color.FromRgb((byte)(r * 0.12 + 10), (byte)(g * 0.12 + 12), (byte)(b * 0.12 + 16)),
                    InputBorder: customPrimary,
                    InputFg: (Color)ColorConverter.ConvertFromString("#FAFAFA"),
                    Primary: customPrimary,
                    Secondary: customSec,
                    Accent: customAcc
                );
            }
            else
            {
                return new ThemeEnvironment(
                    Background: Color.FromRgb((byte)(242 + r * 0.05), (byte)(242 + g * 0.05), (byte)(242 + b * 0.05)),
                    Surface: (Color)ColorConverter.ConvertFromString("#FFFFFF"),
                    Surface2: Color.FromRgb((byte)(228 + r * 0.08), (byte)(228 + g * 0.08), (byte)(228 + b * 0.08)),
                    TextPrimary: (Color)ColorConverter.ConvertFromString("#0F172A"),
                    TextSecondary: (Color)ColorConverter.ConvertFromString("#475569"),
                    Border: Color.FromRgb((byte)(218 + r * 0.10), (byte)(218 + g * 0.10), (byte)(218 + b * 0.10)),
                    InputBg: (Color)ColorConverter.ConvertFromString("#FFFFFF"),
                    InputBorder: customPrimary,
                    InputFg: (Color)ColorConverter.ConvertFromString("#0F172A"),
                    Primary: customPrimary,
                    Secondary: customSec,
                    Accent: customAcc
                );
            }
        }

        return palette switch
        {
            ColorPalettePreset.CyanSky => isDark
                ? new ThemeEnvironment(
                    Background: (Color)ColorConverter.ConvertFromString("#081426"),
                    Surface: (Color)ColorConverter.ConvertFromString("#0E2338"),
                    Surface2: (Color)ColorConverter.ConvertFromString("#15324D"),
                    TextPrimary: (Color)ColorConverter.ConvertFromString("#F0F9FF"),
                    TextSecondary: (Color)ColorConverter.ConvertFromString("#7DD3FC"),
                    Border: (Color)ColorConverter.ConvertFromString("#1A4366"),
                    InputBg: (Color)ColorConverter.ConvertFromString("#0A1B2E"),
                    InputBorder: (Color)ColorConverter.ConvertFromString("#0284C7"),
                    InputFg: (Color)ColorConverter.ConvertFromString("#F0F9FF"),
                    Primary: (Color)ColorConverter.ConvertFromString("#00B4D8"),
                    Secondary: (Color)ColorConverter.ConvertFromString("#0077B6"),
                    Accent: (Color)ColorConverter.ConvertFromString("#90E0EF"))
                : new ThemeEnvironment(
                    Background: (Color)ColorConverter.ConvertFromString("#F0F9FF"),
                    Surface: (Color)ColorConverter.ConvertFromString("#FFFFFF"),
                    Surface2: (Color)ColorConverter.ConvertFromString("#E0F2FE"),
                    TextPrimary: (Color)ColorConverter.ConvertFromString("#0C4A6E"),
                    TextSecondary: (Color)ColorConverter.ConvertFromString("#0369A1"),
                    Border: (Color)ColorConverter.ConvertFromString("#BAE6FD"),
                    InputBg: (Color)ColorConverter.ConvertFromString("#FFFFFF"),
                    InputBorder: (Color)ColorConverter.ConvertFromString("#38BDF8"),
                    InputFg: (Color)ColorConverter.ConvertFromString("#0C4A6E"),
                    Primary: (Color)ColorConverter.ConvertFromString("#0284C7"),
                    Secondary: (Color)ColorConverter.ConvertFromString("#0369A1"),
                    Accent: (Color)ColorConverter.ConvertFromString("#38BDF8")),

            ColorPalettePreset.CrimsonRose => isDark
                ? new ThemeEnvironment(
                    Background: (Color)ColorConverter.ConvertFromString("#1F070E"),
                    Surface: (Color)ColorConverter.ConvertFromString("#2E0C17"),
                    Surface2: (Color)ColorConverter.ConvertFromString("#461424"),
                    TextPrimary: (Color)ColorConverter.ConvertFromString("#FFF1F2"),
                    TextSecondary: (Color)ColorConverter.ConvertFromString("#FDA4AF"),
                    Border: (Color)ColorConverter.ConvertFromString("#5C1D32"),
                    InputBg: (Color)ColorConverter.ConvertFromString("#250912"),
                    InputBorder: (Color)ColorConverter.ConvertFromString("#BE123C"),
                    InputFg: (Color)ColorConverter.ConvertFromString("#FFF1F2"),
                    Primary: (Color)ColorConverter.ConvertFromString("#E11D48"),
                    Secondary: (Color)ColorConverter.ConvertFromString("#BE123C"),
                    Accent: (Color)ColorConverter.ConvertFromString("#FB7185"))
                : new ThemeEnvironment(
                    Background: (Color)ColorConverter.ConvertFromString("#FFF1F2"),
                    Surface: (Color)ColorConverter.ConvertFromString("#FFFFFF"),
                    Surface2: (Color)ColorConverter.ConvertFromString("#FFE4E6"),
                    TextPrimary: (Color)ColorConverter.ConvertFromString("#881337"),
                    TextSecondary: (Color)ColorConverter.ConvertFromString("#9F1239"),
                    Border: (Color)ColorConverter.ConvertFromString("#FECDD3"),
                    InputBg: (Color)ColorConverter.ConvertFromString("#FFFFFF"),
                    InputBorder: (Color)ColorConverter.ConvertFromString("#FB7185"),
                    InputFg: (Color)ColorConverter.ConvertFromString("#881337"),
                    Primary: (Color)ColorConverter.ConvertFromString("#E11D48"),
                    Secondary: (Color)ColorConverter.ConvertFromString("#BE123C"),
                    Accent: (Color)ColorConverter.ConvertFromString("#FB7185")),

            ColorPalettePreset.FluentAzure => isDark
                ? new ThemeEnvironment(
                    Background: (Color)ColorConverter.ConvertFromString("#0B1528"),
                    Surface: (Color)ColorConverter.ConvertFromString("#13223F"),
                    Surface2: (Color)ColorConverter.ConvertFromString("#1E345D"),
                    TextPrimary: (Color)ColorConverter.ConvertFromString("#F8FAFC"),
                    TextSecondary: (Color)ColorConverter.ConvertFromString("#94A3B8"),
                    Border: (Color)ColorConverter.ConvertFromString("#234273"),
                    InputBg: (Color)ColorConverter.ConvertFromString("#0F1C33"),
                    InputBorder: (Color)ColorConverter.ConvertFromString("#0078D4"),
                    InputFg: (Color)ColorConverter.ConvertFromString("#F8FAFC"),
                    Primary: (Color)ColorConverter.ConvertFromString("#0078D4"),
                    Secondary: (Color)ColorConverter.ConvertFromString("#005A9E"),
                    Accent: (Color)ColorConverter.ConvertFromString("#2886DE"))
                : new ThemeEnvironment(
                    Background: (Color)ColorConverter.ConvertFromString("#F4F6F9"),
                    Surface: (Color)ColorConverter.ConvertFromString("#FFFFFF"),
                    Surface2: (Color)ColorConverter.ConvertFromString("#E2E8F0"),
                    TextPrimary: (Color)ColorConverter.ConvertFromString("#0F172A"),
                    TextSecondary: (Color)ColorConverter.ConvertFromString("#475569"),
                    Border: (Color)ColorConverter.ConvertFromString("#CBD5E1"),
                    InputBg: (Color)ColorConverter.ConvertFromString("#FFFFFF"),
                    InputBorder: (Color)ColorConverter.ConvertFromString("#0078D4"),
                    InputFg: (Color)ColorConverter.ConvertFromString("#0F172A"),
                    Primary: (Color)ColorConverter.ConvertFromString("#0078D4"),
                    Secondary: (Color)ColorConverter.ConvertFromString("#005A9E"),
                    Accent: (Color)ColorConverter.ConvertFromString("#2886DE")),

            ColorPalettePreset.EmeraldGreen => isDark
                ? new ThemeEnvironment(
                    Background: (Color)ColorConverter.ConvertFromString("#061A14"),
                    Surface: (Color)ColorConverter.ConvertFromString("#0D2E24"),
                    Surface2: (Color)ColorConverter.ConvertFromString("#154536"),
                    TextPrimary: (Color)ColorConverter.ConvertFromString("#ECFDF5"),
                    TextSecondary: (Color)ColorConverter.ConvertFromString("#6EE7B7"),
                    Border: (Color)ColorConverter.ConvertFromString("#1A5745"),
                    InputBg: (Color)ColorConverter.ConvertFromString("#09241C"),
                    InputBorder: (Color)ColorConverter.ConvertFromString("#10B981"),
                    InputFg: (Color)ColorConverter.ConvertFromString("#ECFDF5"),
                    Primary: (Color)ColorConverter.ConvertFromString("#10B981"),
                    Secondary: (Color)ColorConverter.ConvertFromString("#059669"),
                    Accent: (Color)ColorConverter.ConvertFromString("#34D399"))
                : new ThemeEnvironment(
                    Background: (Color)ColorConverter.ConvertFromString("#F0FDF4"),
                    Surface: (Color)ColorConverter.ConvertFromString("#FFFFFF"),
                    Surface2: (Color)ColorConverter.ConvertFromString("#DCFCE7"),
                    TextPrimary: (Color)ColorConverter.ConvertFromString("#064E3B"),
                    TextSecondary: (Color)ColorConverter.ConvertFromString("#047857"),
                    Border: (Color)ColorConverter.ConvertFromString("#BBF7D0"),
                    InputBg: (Color)ColorConverter.ConvertFromString("#FFFFFF"),
                    InputBorder: (Color)ColorConverter.ConvertFromString("#34D399"),
                    InputFg: (Color)ColorConverter.ConvertFromString("#064E3B"),
                    Primary: (Color)ColorConverter.ConvertFromString("#059669"),
                    Secondary: (Color)ColorConverter.ConvertFromString("#047857"),
                    Accent: (Color)ColorConverter.ConvertFromString("#34D399")),

            ColorPalettePreset.RoyalGold => isDark
                ? new ThemeEnvironment(
                    Background: (Color)ColorConverter.ConvertFromString("#17120A"),
                    Surface: (Color)ColorConverter.ConvertFromString("#271F11"),
                    Surface2: (Color)ColorConverter.ConvertFromString("#3D301B"),
                    TextPrimary: (Color)ColorConverter.ConvertFromString("#FFFBEB"),
                    TextSecondary: (Color)ColorConverter.ConvertFromString("#FCD34D"),
                    Border: (Color)ColorConverter.ConvertFromString("#594627"),
                    InputBg: (Color)ColorConverter.ConvertFromString("#1F190D"),
                    InputBorder: (Color)ColorConverter.ConvertFromString("#D97706"),
                    InputFg: (Color)ColorConverter.ConvertFromString("#FFFBEB"),
                    Primary: (Color)ColorConverter.ConvertFromString("#F59E0B"),
                    Secondary: (Color)ColorConverter.ConvertFromString("#D97706"),
                    Accent: (Color)ColorConverter.ConvertFromString("#FBBF24"))
                : new ThemeEnvironment(
                    Background: (Color)ColorConverter.ConvertFromString("#FFFBEB"),
                    Surface: (Color)ColorConverter.ConvertFromString("#FFFFFF"),
                    Surface2: (Color)ColorConverter.ConvertFromString("#FEF3C7"),
                    TextPrimary: (Color)ColorConverter.ConvertFromString("#78350F"),
                    TextSecondary: (Color)ColorConverter.ConvertFromString("#B45309"),
                    Border: (Color)ColorConverter.ConvertFromString("#FDE68A"),
                    InputBg: (Color)ColorConverter.ConvertFromString("#FFFFFF"),
                    InputBorder: (Color)ColorConverter.ConvertFromString("#FBBF24"),
                    InputFg: (Color)ColorConverter.ConvertFromString("#78350F"),
                    Primary: (Color)ColorConverter.ConvertFromString("#D97706"),
                    Secondary: (Color)ColorConverter.ConvertFromString("#B45309"),
                    Accent: (Color)ColorConverter.ConvertFromString("#FBBF24")),

            ColorPalettePreset.CyberPurple => isDark
                ? new ThemeEnvironment(
                    Background: (Color)ColorConverter.ConvertFromString("#130924"),
                    Surface: (Color)ColorConverter.ConvertFromString("#20113B"),
                    Surface2: (Color)ColorConverter.ConvertFromString("#311A58"),
                    TextPrimary: (Color)ColorConverter.ConvertFromString("#FAF5FF"),
                    TextSecondary: (Color)ColorConverter.ConvertFromString("#C4B5FD"),
                    Border: (Color)ColorConverter.ConvertFromString("#4C2887"),
                    InputBg: (Color)ColorConverter.ConvertFromString("#190C30"),
                    InputBorder: (Color)ColorConverter.ConvertFromString("#8B5CF6"),
                    InputFg: (Color)ColorConverter.ConvertFromString("#FAF5FF"),
                    Primary: (Color)ColorConverter.ConvertFromString("#8B5CF6"),
                    Secondary: (Color)ColorConverter.ConvertFromString("#6D28D9"),
                    Accent: (Color)ColorConverter.ConvertFromString("#A78BFA"))
                : new ThemeEnvironment(
                    Background: (Color)ColorConverter.ConvertFromString("#FAF5FF"),
                    Surface: (Color)ColorConverter.ConvertFromString("#FFFFFF"),
                    Surface2: (Color)ColorConverter.ConvertFromString("#F3E8FF"),
                    TextPrimary: (Color)ColorConverter.ConvertFromString("#581C87"),
                    TextSecondary: (Color)ColorConverter.ConvertFromString("#7E22CE"),
                    Border: (Color)ColorConverter.ConvertFromString("#E9D5FF"),
                    InputBg: (Color)ColorConverter.ConvertFromString("#FFFFFF"),
                    InputBorder: (Color)ColorConverter.ConvertFromString("#A78BFA"),
                    InputFg: (Color)ColorConverter.ConvertFromString("#581C87"),
                    Primary: (Color)ColorConverter.ConvertFromString("#7C3AED"),
                    Secondary: (Color)ColorConverter.ConvertFromString("#6D28D9"),
                    Accent: (Color)ColorConverter.ConvertFromString("#A78BFA")),

            _ => isDark // DeepSpace
                ? new ThemeEnvironment(
                    Background: (Color)ColorConverter.ConvertFromString("#0B1120"),
                    Surface: (Color)ColorConverter.ConvertFromString("#1E293B"),
                    Surface2: (Color)ColorConverter.ConvertFromString("#334155"),
                    TextPrimary: (Color)ColorConverter.ConvertFromString("#F3F4F6"),
                    TextSecondary: (Color)ColorConverter.ConvertFromString("#9CA3AF"),
                    Border: (Color)ColorConverter.ConvertFromString("#374151"),
                    InputBg: (Color)ColorConverter.ConvertFromString("#131E31"),
                    InputBorder: (Color)ColorConverter.ConvertFromString("#3B82F6"),
                    InputFg: (Color)ColorConverter.ConvertFromString("#F3F4F6"),
                    Primary: (Color)ColorConverter.ConvertFromString("#3B82F6"),
                    Secondary: (Color)ColorConverter.ConvertFromString("#1D4ED8"),
                    Accent: (Color)ColorConverter.ConvertFromString("#60A5FA"))
                : new ThemeEnvironment(
                    Background: (Color)ColorConverter.ConvertFromString("#F8FAFC"),
                    Surface: (Color)ColorConverter.ConvertFromString("#FFFFFF"),
                    Surface2: (Color)ColorConverter.ConvertFromString("#F1F5F9"),
                    TextPrimary: (Color)ColorConverter.ConvertFromString("#0F172A"),
                    TextSecondary: (Color)ColorConverter.ConvertFromString("#64748B"),
                    Border: (Color)ColorConverter.ConvertFromString("#E2E8F0"),
                    InputBg: (Color)ColorConverter.ConvertFromString("#FFFFFF"),
                    InputBorder: (Color)ColorConverter.ConvertFromString("#93C5FD"),
                    InputFg: (Color)ColorConverter.ConvertFromString("#0F172A"),
                    Primary: (Color)ColorConverter.ConvertFromString("#2563EB"),
                    Secondary: (Color)ColorConverter.ConvertFromString("#1D4ED8"),
                    Accent: (Color)ColorConverter.ConvertFromString("#60A5FA"))
        };
    }

    public void ApplyTheme(BaseThemeMode mode, ColorPalettePreset palette, bool save = true)
    {
        CurrentThemeMode = mode;
        CurrentPalette = palette;

        bool isDark = mode switch
        {
            BaseThemeMode.Light => false,
            BaseThemeMode.System => IsWindowsInDarkTheme(),
            _ => true
        };

        var env = BuildEnvironment(isDark, palette);

        // 1. Apply to MaterialDesign palette
        try
        {
            var theme = _paletteHelper.GetTheme();
            theme.SetBaseTheme(isDark ? BaseTheme.Dark : BaseTheme.Light);
            theme.SetPrimaryColor(env.Primary);
            theme.SetSecondaryColor(env.Secondary);
            _paletteHelper.SetTheme(theme);
        }
        catch { /* Fallback gracefully if MaterialDesign theme is loading */ }

        // 2. Update Application-level AlAtmani & Fluent Brushes
        var app = System.Windows.Application.Current;
        if (app != null)
        {
            SetAppResource("AlAtmani.BackgroundBrush", new SolidColorBrush(env.Background));
            SetAppResource("AlAtmani.SurfaceBrush", new SolidColorBrush(env.Surface));
            SetAppResource("AlAtmani.Surface2Brush", new SolidColorBrush(env.Surface2));
            SetAppResource("AlAtmani.TextPrimaryBrush", new SolidColorBrush(env.TextPrimary));
            SetAppResource("AlAtmani.TextSecondaryBrush", new SolidColorBrush(env.TextSecondary));
            SetAppResource("AlAtmani.BorderBrush", new SolidColorBrush(env.Border));
            SetAppResource("AlAtmani.InputBackgroundBrush", new SolidColorBrush(env.InputBg));
            SetAppResource("AlAtmani.InputBorderBrush", new SolidColorBrush(env.InputBorder));
            SetAppResource("AlAtmani.InputForegroundBrush", new SolidColorBrush(env.InputFg));
            SetAppResource("AlAtmani.CardBackgroundBrush", new SolidColorBrush(env.Surface));
            SetAppResource("AlAtmani.AcrylicBrush", new SolidColorBrush(env.Surface) { Opacity = 0.95 });
            SetAppResource("AlAtmani.AcrylicBrushStrong", new SolidColorBrush(env.Surface) { Opacity = 0.98 });
            SetAppResource("MaterialDesignPaper", new SolidColorBrush(env.Surface));
            SetAppResource("MaterialDesignBackground", new SolidColorBrush(env.Background));
            SetAppResource("MaterialDesignCardBackground", new SolidColorBrush(env.Surface));
            SetAppResource("MaterialDesignBody", new SolidColorBrush(env.TextPrimary));
            SetAppResource("MaterialDesignTextBoxBorder", new SolidColorBrush(env.Border));

            // Accent & Selected Brushes
            SetAppResource("AlAtmani.AccentBrush", new SolidColorBrush(env.Primary));
            SetAppResource("AlAtmani.ElectricCyan", env.Primary);
            SetAppResource("AlAtmani.SelectedCardBorderBrush", new SolidColorBrush(env.Primary));
            SetAppResource("AlAtmani.SelectedCardBackgroundBrush", new SolidColorBrush(env.Primary) { Opacity = 0.16 });
            SetAppResource("PrimaryHueMidBrush", new SolidColorBrush(env.Primary));
            SetAppResource("PrimaryHueLightBrush", new SolidColorBrush(env.Accent));
            SetAppResource("PrimaryHueDarkBrush", new SolidColorBrush(env.Secondary));
            SetAppResource("SecondaryHueMidBrush", new SolidColorBrush(env.Secondary));
            SetAppResource("SecondaryHueLightBrush", new SolidColorBrush(env.Accent));
            SetAppResource("SecondaryHueDarkBrush", new SolidColorBrush(env.Secondary));
        }

        // 3. Save settings if requested
        if (save)
        {
            _ = _settingsService.SaveSettingAsync("AppThemeMode", mode.ToString());
            _ = _settingsService.SaveSettingAsync("AppColorPalette", palette.ToString());
        }

        ThemeChanged?.Invoke(this, EventArgs.Empty);
    }

    public void SetZoomFactor(double factor, bool save = true)
    {
        CurrentZoomFactor = Math.Round(Math.Clamp(factor, 0.80, 1.75), 2);

        if (save)
        {
            _ = _settingsService.SaveSettingAsync("AppUiZoomFactor", CurrentZoomFactor.ToString("0.00", System.Globalization.CultureInfo.InvariantCulture));
        }

        ZoomChanged?.Invoke(this, CurrentZoomFactor);
    }

    public void ZoomIn()
    {
        double next = CurrentZoomFactor switch
        {
            < 0.90 => 0.90,
            < 1.00 => 1.00,
            < 1.10 => 1.10,
            < 1.25 => 1.25,
            < 1.50 => 1.50,
            _ => 1.75
        };
        SetZoomFactor(next);
    }

    public void ZoomOut()
    {
        double next = CurrentZoomFactor switch
        {
            > 1.50 => 1.50,
            > 1.25 => 1.25,
            > 1.10 => 1.10,
            > 1.00 => 1.00,
            > 0.90 => 0.90,
            _ => 0.80
        };
        SetZoomFactor(next);
    }

    public void ResetZoom()
    {
        SetZoomFactor(1.0);
    }

    public void ToggleThemeMode()
    {
        var next = CurrentThemeMode switch
        {
            BaseThemeMode.Dark => BaseThemeMode.Light,
            BaseThemeMode.Light => BaseThemeMode.System,
            _ => BaseThemeMode.Dark
        };
        ApplyTheme(next, CurrentPalette);
    }

    public void ApplyCustomAccent(string hexColor, bool save = true)
    {
        if (string.IsNullOrWhiteSpace(hexColor) || !hexColor.StartsWith("#"))
            hexColor = "#00B4D8";

        if (save)
        {
            _ = _settingsService.SaveSettingAsync("CustomAccentColorHex", hexColor);
            _ = _settingsService.SaveSettingAsync("AppColorPalette", ColorPalettePreset.Custom.ToString());
        }

        ApplyTheme(CurrentThemeMode, ColorPalettePreset.Custom, save: false);
    }

    private static void SetAppResource(string key, object value)
    {
        if (System.Windows.Application.Current != null)
        {
            System.Windows.Application.Current.Resources[key] = value;
        }
    }

    private static bool IsWindowsInDarkTheme()
    {
        try
        {
            using var key = Registry.CurrentUser.OpenSubKey(@"Software\Microsoft\Windows\CurrentVersion\Themes\Personalize");
            var val = key?.GetValue("AppsUseLightTheme");
            if (val is int lightVal)
                return lightVal == 0;
        }
        catch { }
        return true;
    }
}
