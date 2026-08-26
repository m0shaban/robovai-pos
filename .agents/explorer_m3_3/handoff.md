# Handoff Report: Explorer M3-3 — LiveCharts, OpenCV Camera Handle & Barcode Scanner Lifecycle Plan

## 1. Observation
- **LiveCharts Paint Allocations**: In `src/SmartPOS.Application/ViewModels/ReportsViewModel.cs` lines 756, 759, 772, 773, 796, 797, 798, 801-803, 832, 835, `LoadChartsAsync()` creates new `SolidColorPaint` and `LinearGradientPaint` instances on every load without `Dispose()`.
- **ReportsPage ViewModel Re-creation**: In `src/SmartPOS.WPF/Views/ReportsPage.xaml.cs` lines 23-24, `Page_Loaded` resolves a new `ReportsViewModel` instance via `host.Services.GetRequiredService<ReportsViewModel>()` on every tab navigation.
- **OpenCV Camera Handle & Bitmap Churn**: In `src/SmartPOS.Application/ViewModels/WmsQrBridgeViewModel.cs` lines 416-463, `StartWmsQrScan()` allocates `new VideoCapture(0)` and creates a new `WriteableBitmap` frame every 80ms (~12.5 FPS). In `src/SmartPOS.WPF/Views/SettingsPage.xaml.cs` lines 122-128, `Page_Unloaded` attempts `Vm is IDisposable`, but `SettingsViewModel` does not implement `IDisposable` or call `StopWmsQrScan()`.
- **Barcode Scanner Permanent Disconnect**: In `src/SmartPOS.WPF/Views/POSPage.xaml.cs` lines 29-35, `Page_Unloaded` calls `_viewModel.Dispose()`, which unregisters `MainPOSViewModel` from `WeakReferenceMessenger.Default` (`MainPOSViewModel.cs` line 156). When returning to the cached `POSPage`, constructor does not re-run and `MainPOSViewModel` is never re-registered with `WeakReferenceMessenger`.

## 2. Logic Chain
1. **Unmanaged Skia Memory Leak**: SkiaSharp paints wrap native C++ `SkPaint` handles. Repeated allocations in `LoadChartsAsync()` combined with re-instantiating `ReportsViewModel` on every page load cause native memory growth that standard GC cannot sweep.
   *Fix*: Cache reusable paint fields (`_salesBarFillPaint`, `_whiteTextPaint`, etc.), implement `IDisposable` on `ReportsViewModel`, and check `if (_viewModel == null)` in `ReportsPage.xaml.cs`.
2. **Camera Hardware Lock & Frame Churn**: Navigating away from `SettingsPage` during QR scanning leaves the background `VideoCapture(0)` thread running and native hardware handles locked because `SettingsViewModel` lacks `IDisposable`. Generating ~12.5 `WriteableBitmap` objects/sec causes UI thread GC pressure.
   *Fix*: Implement `IDisposable` on `SettingsViewModel`, invoke `StopWmsQrScanCommand` in `SettingsPage.xaml.cs` `Page_Unloaded`, and manage `WriteableBitmap` reference cleanup.
3. **Barcode Scanner Listener Disconnect**: `POSPage` is cached in `MainWindow._pageCache`. Navigating away unregisters `MainPOSViewModel` from `WeakReferenceMessenger`, but navigating back never re-registers it because the constructor isn't re-executed.
   *Fix*: Add `RegisterMessenger()` / `UnregisterMessenger()` in `MainPOSViewModel` and call `RegisterMessenger()` in `POSPage.xaml.cs` `Page_Loaded` and `UnregisterMessenger()` in `Page_Unloaded`. Implement `IDisposable` on `BarcodeService`.

## 3. Caveats
- No source code outside `.agents/explorer_m3_3/` was modified during this investigation (read-only mode).
- Testing camera disposal requires physical or simulated webcam device availability.

## 4. Conclusion
The implementation strategy for M3 Part 3 is fully formulated and documented in `f:\Raw\kasher\kasher\.agents\explorer_m3_3\analysis.md`. Following this strategy will resolve LiveCharts SkiaSharp memory leaks, OpenCV camera handle locking, bitmap churn, and barcode scanner disconnection on page re-navigation.

## 5. Verification Method
1. **LiveCharts Paint Reuse**: Run `dotnet build src/SmartPOS.WPF/SmartPOS.WPF.csproj`. Open Reports page, trigger date filter changes 20 times. Verify memory remains stable and no duplicate `ReportsViewModel` instances are created.
2. **Camera Handle Disposal**: Open Settings page, start WMS QR scan, switch to POS tab. Verify webcam hardware LED turns off and `VideoCapture` handle is freed.
3. **Barcode Messenger**: Open POS tab, switch to Settings tab, return to POS tab, scan a test barcode. Verify `BarcodeScannedMessage` is received and product is added to cart.
