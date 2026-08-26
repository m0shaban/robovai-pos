# Technical Implementation Plan: LiveCharts Paint Reuse, OpenCV Camera Cleanup & Barcode Messenger Lifecycle

**Module**: `src/SmartPOS.WPF`, `src/SmartPOS.Application`, `src/SmartPOS.Infrastructure`  
**Milestone**: M3 — Desktop WPF Memory & DB Lock Resolution (Part 3: LiveCharts, Camera Handle & Barcode Messenger)  
**Author**: Explorer M3-3 (`teamwork_preview_explorer`)  
**Date**: 2026-08-08  

---

## 1. Executive Summary

This report provides the exact, step-by-step implementation strategy for resolving three critical unmanaged resource leaks, bitmap allocation churn issues, and UI thread messenger lifecycle bugs in `SmartPOS.WPF`:

1. **LiveCharts & SkiaSharp Paint Reuse in `ReportsViewModel.cs` / `ReportsPage.xaml.cs`**:
   - Replaces per-refresh `SolidColorPaint` and `LinearGradientPaint` allocations with cached reusable paint fields.
   - Implements `IDisposable` on `ReportsViewModel` to free SkiaSharp native objects.
   - Fixes `ReportsPage.xaml.cs` to retain a single ViewModel instance across `Page_Loaded` cycles instead of re-instantiating.

2. **OpenCV Camera Handle Disposal & Bitmap Churn in `WmsQrBridgeViewModel.cs` / `SettingsPage.xaml.cs`**:
   - Implements `IDisposable` on `SettingsViewModel` and invokes `StopWmsQrScan()` in `SettingsPage.xaml.cs` `Page_Unloaded`.
   - Reuses or safely disposes `WriteableBitmap` frames to eliminate 12.5 FPS memory churn on the WPF UI thread.

3. **Barcode Scanner Messenger Lifecycle in `POSPage.xaml.cs`, `MainPOSViewModel.cs` & `BarcodeService.cs`**:
   - Restores `MainPOSViewModel` registration with `WeakReferenceMessenger` on page re-navigation (`Page_Loaded`).
   - Implements `IDisposable` on `IBarcodeService` and `BarcodeService` to release COM serial ports cleanly.

---

## 2. Target 1: LiveCharts Paint Reuse & ViewModel Lifecycle (`ReportsViewModel.cs` & `ReportsPage.xaml.cs`)

### 2.1 Problem Analysis
- **File 1**: `src/SmartPOS.Application/ViewModels/ReportsViewModel.cs` (lines 722–840)
  - Inside `LoadChartsAsync()`, every chart refresh allocates new SkiaSharp paint objects:
    - `new SolidColorPaint(SKColor.Parse("#06B6D4"))` (line 756)
    - `new SolidColorPaint(SKColors.White)` (lines 759, 798, 835)
    - `new SolidColorPaint(SKColor.Parse("#94A3B8"))` (line 772)
    - `new SolidColorPaint(SKColor.Parse("#1E293B"))` (line 773)
    - `new SolidColorPaint(SKColor.Parse("#10B981"))` (lines 796, 797)
    - `new LinearGradientPaint(...)` (lines 801–803)
    - `new SolidColorPaint(SKColor.Parse(g.Color))` (line 832 inside loop)
  - SkiaSharp paint objects encapsulate native C++ `SkPaint` instances. Failing to dispose or reuse them causes continuous growth of the unmanaged Skia heap during 24+ hour operations.
  - `ReportsViewModel` does not implement `IDisposable`, leaving discarded series arrays rooted in memory.

- **File 2**: `src/SmartPOS.WPF/Views/ReportsPage.xaml.cs` (lines 18–32)
  - `Page_Loaded` calls `host.Services.GetRequiredService<ReportsViewModel>()` every time the page is loaded.
  - Re-instantiating `ReportsViewModel` creates orphaned view models, triggers double data loading, and leaves old LiveCharts series bound to internal event handlers.

### 2.2 Detailed Implementation Strategy

#### Step 1.1: Add Cached Paint Fields and Implement `IDisposable` in `ReportsViewModel.cs`
1. Update `ReportsViewModel` class declaration to implement `IDisposable`:
   ```csharp
   public partial class ReportsViewModel : BaseViewModel, IDisposable
   ```
2. Declare reusable private static/instance paint fields:
   ```csharp
   // --- Cached LiveCharts SkiaSharp Paints ---
   private readonly SolidColorPaint _salesBarFillPaint = new(SKColor.Parse("#06B6D4"));
   private readonly SolidColorPaint _whiteTextPaint = new(SKColors.White);
   private readonly SolidColorPaint _axisLabelPaint = new(SKColor.Parse("#94A3B8"));
   private readonly SolidColorPaint _axisSeparatorPaint = new(SKColor.Parse("#1E293B"));
   private readonly SolidColorPaint _profitLineStrokePaint = new(SKColor.Parse("#10B981")) { StrokeThickness = 3 };
   private readonly SolidColorPaint _profitLineFillPaint = new(SKColor.Parse("#10B981"));
   private readonly SolidColorPaint _profitLineGeometryStrokePaint = new(SKColors.White) { StrokeThickness = 2 };
   private readonly LinearGradientPaint _profitGradientPaint = new(
       new[] { SKColor.Parse("#10B981").WithAlpha(80), SKColors.Transparent },
       new SKPoint(0.5f, 0f), new SKPoint(0.5f, 1f));
   private readonly Dictionary<string, SolidColorPaint> _categoryPaintCache = new();
   ```
3. Update `LoadChartsAsync()` (lines 750–840) to assign cached paints instead of `new SolidColorPaint(...)`:
   ```csharp
   SalesChartSeries = new ISeries[]
   {
       new ColumnSeries<double>
       {
           Name = "Daily Sales",
           Values = dailySales,
           Fill = _salesBarFillPaint,
           Stroke = null,
           MaxBarWidth = 40,
           DataLabelsPaint = _whiteTextPaint,
           DataLabelsSize = 11,
           DataLabelsPosition = LiveChartsCore.Measure.DataLabelsPosition.Top,
           DataLabelsFormatter = p => p.Coordinate.PrimaryValue > 0 ? $"{p.Coordinate.PrimaryValue:N0}" : "",
           YToolTipLabelFormatter = p => $"{p.Coordinate.PrimaryValue:N2} {ArabicChartText.Shape("ج.م")}"
       }
   };

   SalesChartXAxes = new Axis[]
   {
       new Axis
       {
           Labels = dayLabels,
           LabelsPaint = _axisLabelPaint,
           SeparatorsPaint = _axisSeparatorPaint,
       }
   };
   ```
4. Helper method for category pie paints:
   ```csharp
   private SolidColorPaint GetCategoryPaint(string hexColor)
   {
       if (!_categoryPaintCache.TryGetValue(hexColor, out var paint))
       {
           paint = new SolidColorPaint(SKColor.Parse(hexColor));
           _categoryPaintCache[hexColor] = paint;
       }
       return paint;
   }
   ```
5. Implement `Dispose()` method:
   ```csharp
   public void Dispose()
   {
       _salesBarFillPaint.Dispose();
       _whiteTextPaint.Dispose();
       _axisLabelPaint.Dispose();
       _axisSeparatorPaint.Dispose();
       _profitLineStrokePaint.Dispose();
       _profitLineFillPaint.Dispose();
       _profitLineGeometryStrokePaint.Dispose();
       _profitGradientPaint.Dispose();

       foreach (var paint in _categoryPaintCache.Values)
       {
           paint.Dispose();
       }
       _categoryPaintCache.Clear();
   }
   ```

#### Step 1.2: Refactor `ReportsPage.xaml.cs` to Retain ViewModel Instance
Modify `Page_Loaded` in `src/SmartPOS.WPF/Views/ReportsPage.xaml.cs` (lines 18–32):
```csharp
private async void Page_Loaded(object sender, RoutedEventArgs e)
{
    try
    {
        if (_viewModel == null)
        {
            var host = ((App)System.Windows.Application.Current).Host;
            _viewModel = host.Services.GetRequiredService<ReportsViewModel>();
            DataContext = _viewModel;
        }

        await LoadData();
    }
    catch (Exception ex)
    {
        MessageBox.Show($"خطأ في تحميل الصفحة: {ex.Message}", "خطأ", MessageBoxButton.OK, MessageBoxImage.Error);
    }
}
```

---

## 3. Target 2: OpenCV Camera Handle & Bitmap Churn Cleanup (`WmsQrBridgeViewModel.cs` & `SettingsPage.xaml.cs`)

### 3.1 Problem Analysis
- **File 1**: `src/SmartPOS.Application/ViewModels/WmsQrBridgeViewModel.cs` (lines 416–463)
  - `StartWmsQrScan()` instantiates `_wmsCamera = new VideoCapture(0)` and loops while reading frames.
  - In the frame polling loop (line 437), `var frame = mat.ToWriteableBitmap()` allocates a new WPF `WriteableBitmap` every 80ms (~12.5 FPS) without reusing buffers or disposing old frames.
  - If the user leaves `SettingsPage`, `StopWmsQrScan()` is never called because `SettingsViewModel` does not implement `IDisposable`.

- **File 2**: `src/SmartPOS.WPF/Views/SettingsPage.xaml.cs` (lines 122–128)
  - `Page_Unloaded` attempts `if (Vm is IDisposable disposable) disposable.Dispose();`.
  - Since `SettingsViewModel` is not `IDisposable`, navigating away leaves the camera handle locked and polling in a background thread.

### 3.2 Detailed Implementation Strategy

#### Step 2.1: Implement `IDisposable` on `SettingsViewModel` / `WmsQrBridgeViewModel.cs`
1. Update `SettingsViewModel` class declaration (`SettingsViewModel.cs`):
   ```csharp
   public partial class SettingsViewModel : BaseViewModel, IDisposable
   ```
2. In `WmsQrBridgeViewModel.cs` (partial class of `SettingsViewModel`), add `Dispose()`:
   ```csharp
   public void Dispose()
   {
       StopWmsQrScan();
   }
   ```

#### Step 2.2: Ensure `Page_Unloaded` Stops Scanning in `SettingsPage.xaml.cs`
Update `Page_Unloaded` in `src/SmartPOS.WPF/Views/SettingsPage.xaml.cs` (lines 122–128):
```csharp
private void Page_Unloaded(object sender, RoutedEventArgs e)
{
    if (Vm != null)
    {
        Vm.StopWmsQrScanCommand.Execute(null);
    }
    if (Vm is IDisposable disposable)
    {
        disposable.Dispose();
    }
}
```

#### Step 2.3: Bitmap Churn Prevention & Robust Disposal in `StartWmsQrScan()`
Refactor `StartWmsQrScan()` and `StopWmsQrScan()` in `WmsQrBridgeViewModel.cs` (lines 416–463):
```csharp
[RelayCommand]
private async Task StartWmsQrScan()
{
    if (_cameraRunning) return;
    _wmsCameraCts = new CancellationTokenSource();
    _wmsCamera = new VideoCapture(0);
    _cameraRunning = true;
    OnPropertyChanged(nameof(WmsCameraVisibility));

    await Task.Run(async () =>
    {
        using var mat = new Mat();
        using var detector = new QRCodeDetector();

        while (!_wmsCameraCts.Token.IsCancellationRequested)
        {
            if (!_wmsCamera.Read(mat) || mat.Empty())
            {
                await Task.Delay(60);
                continue;
            }

            var frame = mat.ToWriteableBitmap();
            frame.Freeze();

            System.Windows.Application.Current.Dispatcher.Invoke(() =>
            {
                var oldFrame = WmsCameraFrame;
                WmsCameraFrame = frame;
                // Allow GC to reclaim old frame reference promptly
                oldFrame = null;
            });

            var decoded = detector.DetectAndDecode(mat, out _);
            if (!string.IsNullOrWhiteSpace(decoded))
            {
                System.Windows.Application.Current.Dispatcher.Invoke(() => OnWmsQrScanned(decoded));
                break;
            }

            await Task.Delay(80);
        }
    }, _wmsCameraCts.Token);

    StopWmsQrScan();
}

[RelayCommand]
private void StopWmsQrScan()
{
    _wmsCameraCts?.Cancel();
    _wmsCameraCts?.Dispose();
    _wmsCameraCts = null;

    if (_wmsCamera != null)
    {
        if (_wmsCamera.IsOpened())
        {
            _wmsCamera.Release();
        }
        _wmsCamera.Dispose();
        _wmsCamera = null;
    }

    _cameraRunning = false;
    WmsCameraFrame = null;
    OnPropertyChanged(nameof(WmsCameraVisibility));
}
```

---

## 4. Target 3: Barcode Scanner Messenger Lifecycle Restoration (`POSPage.xaml.cs`, `MainPOSViewModel.cs` & `BarcodeService.cs`)

### 4.1 Problem Analysis
- **File 1**: `src/SmartPOS.WPF/Views/POSPage.xaml.cs` (lines 29–35)
  - `Page_Unloaded` calls `_viewModel.Dispose()`, which unregisters `MainPOSViewModel` from `WeakReferenceMessenger.Default`.
  - When returning to `POSPage` from another tab, `MainWindow` retrieves the cached `POSPage` instance from `_pageCache`.
  - The constructor of `POSPage` does NOT run again. `MainPOSViewModel` is NEVER re-registered with `WeakReferenceMessenger`. Barcode scan events (`BarcodeScannedMessage`) are silently ignored for the rest of the application runtime.

- **File 2**: `src/SmartPOS.Infrastructure/Services/BarcodeService.cs` (lines 11–134)
  - `BarcodeService` manages COM serial ports (`SerialPort`), but does not implement `IDisposable`.
  - Disposing the DI container or changing scanner settings leaves COM serial port streams un-closed.

### 4.2 Detailed Implementation Strategy

#### Step 3.1: Add Explicit Messenger Subscription Methods in `MainPOSViewModel.cs`
Update `MainPOSViewModel.cs` (lines 147, 154–157):
```csharp
public void RegisterMessenger()
{
    if (!WeakReferenceMessenger.Default.IsRegistered<SmartPOS.Application.Messages.BarcodeScannedMessage>(this))
    {
        WeakReferenceMessenger.Default.Register<SmartPOS.Application.Messages.BarcodeScannedMessage>(this, (r, m) => Receive(m));
    }
}

public void UnregisterMessenger()
{
    WeakReferenceMessenger.Default.Unregister<SmartPOS.Application.Messages.BarcodeScannedMessage>(this);
}

public void Dispose()
{
    UnregisterMessenger();
}
```

#### Step 3.2: Manage Messenger Subscription on Navigation in `POSPage.xaml.cs`
Update `POSPage.xaml.cs` (lines 23–35):
```csharp
private void Page_Loaded(object sender, System.Windows.RoutedEventArgs e)
{
    // Focus on barcode input
    BarcodeTextBox.Focus();

    // Restore barcode messenger subscription when user returns to POS tab
    _viewModel.RegisterMessenger();
}

private void Page_Unloaded(object sender, System.Windows.RoutedEventArgs e)
{
    // Unregister messenger when leaving POS tab to avoid background message handling
    _viewModel.UnregisterMessenger();
}
```

#### Step 3.3: Implement `IDisposable` on `IBarcodeService` and `BarcodeService.cs`
1. Update `src/SmartPOS.Core/Interfaces/IBarcodeService.cs` (line 7):
   ```csharp
   public interface IBarcodeService : IDisposable
   ```
2. Update `src/SmartPOS.Infrastructure/Services/BarcodeService.cs` (line 11):
   ```csharp
   public class BarcodeService : IBarcodeService
   {
       ...
       public void Dispose()
       {
           StopListening();
           CloseSerial();
           _serialPort?.Dispose();
           _serialPort = null;
       }
   }
   ```

---

## 5. Verification Matrix & Checklist for Implementer

| Task | File Path | Method / Code Change | Verification Method |
| :--- | :--- | :--- | :--- |
| **Paint Reuse** | `ReportsViewModel.cs` | Replace `new SolidColorPaint()` in `LoadChartsAsync()` with cached fields (`_salesBarFillPaint`, etc.); implement `IDisposable`. | Check native memory usage after 50+ report refreshes. Verify Skia objects are reused. |
| **ViewModel Caching** | `ReportsPage.xaml.cs` | Add `if (_viewModel == null)` check in `Page_Loaded`. | Confirm single `ReportsViewModel` instance is reused when switching tabs back to Reports. |
| **Camera Cleanup** | `WmsQrBridgeViewModel.cs` | Implement `Dispose()`, update `StopWmsQrScan()` to call `Release()` & `Dispose()`. | Start QR camera scan, switch tabs. Verify webcam LED turns OFF immediately. |
| **Bitmap Churn** | `WmsQrBridgeViewModel.cs` | Null out `oldFrame` reference and handle WPF frame disposal cleanly. | Run camera scan for 5 minutes; verify GC Gen 0/1 memory allocation rate stabilizes. |
| **Barcode Lifecycle** | `POSPage.xaml.cs` & `MainPOSViewModel.cs` | Add `RegisterMessenger()` / `UnregisterMessenger()`; call in `Page_Loaded` / `Page_Unloaded`. | Switch away from POS tab, return to POS tab, scan a barcode. Confirm barcode is received. |
| **SerialPort Dispose** | `BarcodeService.cs` & `IBarcodeService.cs` | Implement `IDisposable` on `BarcodeService` to release `SerialPort`. | Test COM scanner mode configuration changes; confirm port is closed without throwing `UnauthorizedAccessException`. |

---
