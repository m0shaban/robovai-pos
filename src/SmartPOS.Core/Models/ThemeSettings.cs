namespace SmartPOS.Core.Models;

public enum BaseThemeMode
{
    Dark,
    Light,
    System
}

public enum ColorPalettePreset
{
    DeepSpace,    // Blue #3B82F6
    FluentAzure,  // Microsoft Fluent #0078D4
    CyanSky,      // Microsoft Cyan / Sky #00B4D8
    EmeraldGreen, // Mint/Green #10B981
    RoyalGold,    // Amber/Gold #F59E0B
    CyberPurple,  // Purple/Violet #8B5CF6
    CrimsonRose,  // Rose/Red #E11D48
    Custom        // User defined custom accent color
}

public class ColorPaletteInfo
{
    public ColorPalettePreset Preset { get; set; }
    public string NameAr { get; set; } = string.Empty;
    public string NameEn { get; set; } = string.Empty;
    public string PrimaryColorHex { get; set; } = string.Empty;
    public string SecondaryColorHex { get; set; } = string.Empty;
    public string AccentColorHex { get; set; } = string.Empty;
}
