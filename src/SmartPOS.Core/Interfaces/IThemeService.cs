using System;
using System.Collections.Generic;
using SmartPOS.Core.Models;

namespace SmartPOS.Core.Interfaces;

public interface IThemeService
{
    BaseThemeMode CurrentThemeMode { get; }
    ColorPalettePreset CurrentPalette { get; }
    double CurrentZoomFactor { get; }
    IReadOnlyList<ColorPaletteInfo> AvailablePalettes { get; }

    event EventHandler? ThemeChanged;
    event EventHandler<double>? ZoomChanged;

    void Initialize();
    void ApplyTheme(BaseThemeMode mode, ColorPalettePreset palette, bool save = true);
    void SetZoomFactor(double factor, bool save = true);
    void ZoomIn();
    void ZoomOut();
    void ResetZoom();
    void ToggleThemeMode();
    void ApplyCustomAccent(string hexColor, bool save = true);
}
