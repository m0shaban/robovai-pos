# 🧾 Latest Changes (Feb 2026)

> Date: 2026-02-09

This document summarizes the latest changes applied to Smart POS after the Al‑Atmani 2026 migration.

## ✅ Latest (2026-02-09) — RoboVAI POS Release Packaging

- **Rebrand to RoboVAI POS (UI + metadata)**
  - App window titles and key screens now display **RoboVAI POS**.
  - App/product metadata updated to `1.0.2`.

- **New logo/icon wired end-to-end**
  - Executable icon set via `ApplicationIcon`.
  - Logo image displayed in the Login window and Main window drawer.

- **Activation & subscription management inside Settings**
  - Added a new section: status, expiry, remaining days, Device ID copy.
  - Buttons: open activation window, refresh status, WhatsApp message with Device ID, renew/enter code.

- **Landing page HTML prepared for Blogger**
  - New file: `LANDING_PAGE_ROBOVAI.html` (scoped styles; intended as an internal page with existing site header/footer).

- **Installer output standardized to one file**
  - Inno Setup now outputs: `installer/Output/Setup.exe`.
  - Installer removes legacy **SmartPOS** shortcuts during install to avoid launching older builds.

## ✅ Highlights

- **Al‑Atmani 2026 UI applied across remaining pages**
  - Dark mode first, deep-space background, glass/acrylic surfaces.
  - Unified cards/buttons using `AlAtmani.GlassCard` and `AlAtmani.SoftButton`.
  - Updated pages include: Products, Reports, Settings, Customers, Suppliers, Invoices, Features, Expenses, Purchase Orders, Categories, Tables, Shift Management.
  - Shell adjustments: Main window content background is now dark/transparent; login window is now acrylic/dark.

- **Compatibility without visual drift**
  - `ModernCard` and `ModernButton` are now _compatibility aliases_ that are based on Al‑Atmani styles, so older XAML still renders consistently in the new design system.

- **SpaceTheme is now optional (no legacy bleed)**
  - `Themes/SpaceTheme.xaml` is **not merged by default** anymore.
  - If you need the legacy Space Edition look for reference/testing, you can enable it explicitly via config:

```json
{
  "Ui": {
    "EnableLegacySpaceTheme": true
  }
}
```

## 🔧 Files Touched (high level)

- Theme & configuration
  - `src/SmartPOS.WPF/App.xaml` (removed default SpaceTheme merge; modern aliases)
  - `src/SmartPOS.WPF/App.xaml.cs` (conditional legacy theme loading)
  - `src/SmartPOS.WPF/appsettings.json` (added `Ui:EnableLegacySpaceTheme`)

- UI pages & windows
  - Multiple `src/SmartPOS.WPF/Views/*.xaml` pages converted to Al‑Atmani surfaces.

## 🧪 Verification

- `dotnet build SmartPOS.sln -c Release` ✅ (0 warnings / 0 errors)

---

## 🇪🇬 ملخص سريع (Arabic)

- تم توحيد تصميم الواجهة على **Al‑Atmani 2026** (داكن + Glass/Acrylic) لمعظم الصفحات.
- `ModernCard/ModernButton` أصبحوا aliases على ستايلات Al‑Atmani لتجنب أي تداخل.
- `SpaceTheme.xaml` لم يعد يعمل افتراضيًا لتجنب “Legacy bleed”، ويمكن تفعيله فقط من `appsettings.json` عبر `Ui:EnableLegacySpaceTheme`.
