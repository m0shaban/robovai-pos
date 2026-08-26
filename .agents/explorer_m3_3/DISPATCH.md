## Dispatch for Explorer M3-3

**Working Directory**: `f:\Raw\kasher\kasher\.agents\explorer_m3_3`
**Role**: Read-only exploration agent (`teamwork_preview_explorer`)

### Required Context Files to Read:
1. `f:\Raw\kasher\kasher\.agents\ORIGINAL_REQUEST.md`
2. `f:\Raw\kasher\kasher\PROJECT.md`
3. `f:\Raw\kasher\kasher\.agents\explorer_2\analysis.md`

### Task Description:
Formulate an exact, step-by-step implementation plan for:
1. LiveCharts & SkiaSharp paint reuse in `ReportsViewModel.cs` / `ReportsPage.xaml.cs`: eliminating `SolidColorPaint` / `LinearGradientPaint` allocations on every chart load, implementing `IDisposable` on `ReportsViewModel`, and preventing `ReportsPage` from re-instantiating `ReportsViewModel` on `Page_Loaded`.
2. OpenCV `VideoCapture(0)` handle disposal and bitmap churn prevention in `WmsQrBridgeViewModel.cs` / `SettingsPage.xaml.cs`: ensuring `StopWmsQrScan()` is called on `Page_Unloaded`, implementing `IDisposable`, and reusing or properly disposing `WriteableBitmap` frames.
3. Barcode scanner messenger lifecycle in `POSPage.xaml.cs` & `BarcodeService.cs`: restoring `MainPOSViewModel` barcode scanner message subscription on page re-navigation, preventing premature unregistration on tab switch, and implementing `IDisposable` on `BarcodeService`.

### Output Requirement:
Write a detailed report to `f:\Raw\kasher\kasher\.agents\explorer_m3_3\analysis.md` detailing exact file paths, line ranges, class signatures, and code modifications needed. Deliver handoff report via send_message to parent when complete.
