using System.Collections.ObjectModel;
using System.IO;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Windows;
using System.Windows.Media.Imaging;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.EntityFrameworkCore;
using Microsoft.Win32;
using OpenCvSharp;
using OpenCvSharp.WpfExtensions;
using QRCoder;
using SmartPOS.Core.Entities;
using SmartPOS.Infrastructure.Data;

namespace SmartPOS.Application.ViewModels;

/// <summary>
/// Partial class on SettingsViewModel — جسر مزامنة QR بين POS والمخزن (WMS) أوفلاين.
/// كل نظام مستقل، المزامنة اختيارية فقط بطلب المستخدم.
/// Refactored to use factory pattern (v5.1).
/// </summary>
public partial class SettingsViewModel
{
    // يتم حقنه من الـ Constructor الرئيسي
    private IDbContextFactory<AppDbContext>? _contextFactory;

    /// <summary>استدعِ هذا من SettingsViewModel constructor لتفعيل الجسر</summary>
    public void InitWmsBridge(IDbContextFactory<AppDbContext> contextFactory, SmartPOS.Core.Interfaces.ILicenseService licenseService)
    {
        _contextFactory = contextFactory;
        // _licenseService is already set as a readonly field in SettingsViewModel constructor
    }

    // ─── Pairing QR Properties ────────────────────────────────────────
    private byte[]? _pairingQrPng;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(PairingQrVisibility))]
    private System.Windows.Media.Imaging.BitmapSource? _pairingQrImageSource;

    public Visibility PairingQrVisibility =>
        _pairingQrImageSource != null ? Visibility.Visible : Visibility.Collapsed;

    [ObservableProperty]
    private string _pairingQrInfoText = "";

    private static string GetLocalIpAddress()
    {
        try
        {
            var host = System.Net.Dns.GetHostEntry(System.Net.Dns.GetHostName());
            foreach (var ip in host.AddressList)
            {
                if (ip.AddressFamily == System.Net.Sockets.AddressFamily.InterNetwork && !System.Net.IPAddress.IsLoopback(ip))
                {
                    return ip.ToString();
                }
            }
        }
        catch { }
        return "127.0.0.1";
    }

    [RelayCommand]
    private async Task GeneratePairingQr()
    {
        try
        {
            var localIp = GetLocalIpAddress();
            var deviceId = _licenseService?.GetDeviceId() ?? "POS-MASTER";
            var deviceName = Environment.MachineName;

            var payload = new
            {
                type = "pos-pair-v1",
                deviceId = deviceId,
                deviceName = deviceName,
                posVersion = "v6.0",
                serverIp = localIp,
                port = 7890,
                token = "admin",
                ts = DateTimeOffset.UtcNow.ToUnixTimeSeconds()
            };

            var json = System.Text.Json.JsonSerializer.Serialize(payload);

            var b64 = Convert.ToBase64String(System.Text.Encoding.UTF8.GetBytes(json))
                .TrimEnd('=')
                .Replace('+', '-')
                .Replace('/', '_');

            var deepLink = $"http://{localIp}:7890/wms/?pair={b64}";

            using var qrGen = new QRCodeGenerator();
            var qrData = qrGen.CreateQrCode(deepLink, QRCodeGenerator.ECCLevel.L);
            using var qrCode = new PngByteQRCode(qrData);
            _pairingQrPng = qrCode.GetGraphic(10);

            using var ms = new MemoryStream(_pairingQrPng);
            var bmp = new System.Windows.Media.Imaging.BitmapImage();
            bmp.BeginInit();
            bmp.CacheOption = System.Windows.Media.Imaging.BitmapCacheOption.OnLoad;
            bmp.StreamSource = ms;
            bmp.EndInit();
            bmp.Freeze();

            PairingQrImageSource = bmp;
            PairingQrInfoText = $"⚡ FastPair QR جاهز (مسح فوري 0.05 ثانية)\nالشبكة: http://{localIp}:7890/wms/\nالجهاز: {deviceName}";
            WmsAddSyncLog($"[FastPair] تم توليد QR عالي السرعة للجهاز {deviceName} ({localIp})");
        }
        catch (Exception ex)
        {
            MessageBox.Show($"خطأ في توليد QR الربط:\n{ex.Message}", "خطأ", MessageBoxButton.OK, MessageBoxImage.Error);
        }
        await Task.CompletedTask;
    }

    [RelayCommand]
    private void SavePairingQr()
    {
        if (_pairingQrPng == null) return;
        var dlg = new SaveFileDialog
        {
            FileName = $"pos-pairing-{Environment.MachineName}.png",
            Filter = "PNG Image|*.png"
        };
        if (dlg.ShowDialog() == true)
            File.WriteAllBytes(dlg.FileName, _pairingQrPng);
    }

    // ─── Export Properties ───────────────────────────────────────────
    public List<string> WmsExportTypes { get; } = new()
    {
        "لقطة المخزون الحالي (الأصناف + الكميات)",
        "قائمة الأصناف ناقصة المخزون",
        "طلب توريد (أصناف تحتاج تجهيز من المخزن)",
        "تقرير يومي مختصر (ملخص المبيعات)",
        "آخر 20 فاتورة بيع",
        "حركات اليوم (بيع + مشتريات)",
    };

    [ObservableProperty]
    private string _selectedWmsExportType = "لقطة المخزون الحالي (الأصناف + الكميات)";

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(WmsQrImageVisibility))]
    private BitmapSource? _wmsQrImageSource;

    public Visibility WmsQrImageVisibility =>
        _wmsQrImageSource != null ? Visibility.Visible : Visibility.Collapsed;

    [ObservableProperty]
    private string _wmsQrInfoText = "";

    // ─── Camera / Import Properties ──────────────────────────────────
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(WmsCameraVisibility))]
    private BitmapSource? _wmsCameraFrame;

    private bool _cameraRunning;
    public Visibility WmsCameraVisibility =>
        _wmsCameraFrame != null && _cameraRunning ? Visibility.Visible : Visibility.Collapsed;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(WmsImportResultVisibility))]
    private string _wmsImportSummary = "";

    public Visibility WmsImportResultVisibility =>
        !string.IsNullOrEmpty(_wmsImportSummary) ? Visibility.Visible : Visibility.Collapsed;

    // ─── Sync Log ─────────────────────────────────────────────────────
    [ObservableProperty]
    private ObservableCollection<string> _wmsSyncLog = new();

    // ─── Internal ─────────────────────────────────────────────────────
    private VideoCapture? _wmsCamera;
    private CancellationTokenSource? _wmsCameraCts;
    private WmsSyncPayload? _wmsPendingImport;
    private byte[]? _wmsLastQrPng;

    // ══════════════════════════════════════════════════════════════════
    //  GENERATE QR EXPORT (FastPair Protocol)
    // ══════════════════════════════════════════════════════════════════
    [RelayCommand]
    private async Task GenerateWmsQr()
    {
        if (_contextFactory == null)
        {
            MessageBox.Show("لم يتم تهيئة قاعدة البيانات.", "خطأ", MessageBoxButton.OK, MessageBoxImage.Error);
            return;
        }

        try
        {
            var localIp = GetLocalIpAddress();
            var payload = await BuildWmsPayloadAsync();

            var fastPairPayload = new
            {
                type = "pos-export-fastpair",
                exportType = SelectedWmsExportType,
                itemCount = payload.Data.Count,
                downloadUrl = $"http://{localIp}:7890/api/sync/products?chunk=0",
                token = "admin",
                ts = DateTimeOffset.UtcNow.ToUnixTimeSeconds()
            };

            var json = JsonSerializer.Serialize(fastPairPayload);

            using var qrGen = new QRCodeGenerator();
            var qrData = qrGen.CreateQrCode(json, QRCodeGenerator.ECCLevel.L);
            using var qrCode = new PngByteQRCode(qrData);
            _wmsLastQrPng = qrCode.GetGraphic(10);

            using var ms = new MemoryStream(_wmsLastQrPng);
            var bmp = new BitmapImage();
            bmp.BeginInit();
            bmp.CacheOption = BitmapCacheOption.OnLoad;
            bmp.StreamSource = ms;
            bmp.EndInit();
            bmp.Freeze();

            WmsQrImageSource = bmp;
            WmsQrInfoText = $"⚡ FastPair QR خفيف وجاهز ({payload.Data.Count} عنصر) • {DateTime.Now:HH:mm dd/MM}";
            WmsAddSyncLog($"[تصدير FastPair] {SelectedWmsExportType} — {payload.Data.Count} عنصر عبر رابط الشبكة السريع");
        }
        catch (Exception ex)
        {
            MessageBox.Show($"خطأ في توليد QR:\n{ex.Message}", "خطأ", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private async Task<WmsSyncPayload> BuildWmsPayloadAsync()
    {
        await using var ctx = await _contextFactory!.CreateDbContextAsync();
        var typeKey = GetWmsExportTypeKey();
        var payload = new WmsSyncPayload
        {
            V = 1,
            Src = "pos",
            Type = typeKey,
            Ts = DateTimeOffset.UtcNow.ToUnixTimeSeconds(),
            Data = new List<Dictionary<string, object>>()
        };

        if (typeKey == "stock")
        {
            var products = await ctx.Products
                .AsNoTracking()
                .Where(p => !p.IsDeleted)
                .Take(30)
                .ToListAsync();

            payload.Data = products.Select(p => new Dictionary<string, object>
            {
                ["b"] = p.Barcode ?? "",
                ["n"] = p.Name ?? "",
                ["q"] = p.Stock,
                ["mn"] = p.MinStockLevel,
                ["pr"] = p.SellingPrice,
            }).ToList();
        }
        else if (typeKey == "low_stock")
        {
            var products = await ctx.Products
                .AsNoTracking()
                .Where(p => !p.IsDeleted && p.Stock <= p.MinStockLevel)
                .Take(30)
                .ToListAsync();

            payload.Data = products.Select(p => new Dictionary<string, object>
            {
                ["b"] = p.Barcode ?? "",
                ["n"] = p.Name ?? "",
                ["q"] = p.Stock,
                ["mn"] = p.MinStockLevel,
            }).ToList();
        }
        else if (typeKey == "supply_request")
        {
            // طلب توريد: الأصناف تحت الحد الأدنى مع الكمية المطلوبة
            var products = await ctx.Products
                .AsNoTracking()
                .Where(p => !p.IsDeleted && p.Stock < p.MinStockLevel)
                .OrderBy(p => p.Stock - p.MinStockLevel)
                .Take(25)
                .ToListAsync();

            payload.Data = products.Select(p => new Dictionary<string, object>
            {
                ["b"] = p.Barcode ?? "",
                ["n"] = p.Name ?? "",
                ["q"] = p.Stock,
                ["mn"] = p.MinStockLevel,
                ["need"] = Math.Max(0, p.MinStockLevel - p.Stock),
            }).ToList();
        }
        else if (typeKey == "daily_report")
        {
            // تقرير يومي: ملخص مبيعات اليوم + أعلى 10 أصناف (بدون navigation property)
            var today = DateTime.Today;
            var todayIds = await ctx.Sales
                .AsNoTracking()
                .Where(s => s.SaleDate >= today)
                .Select(s => s.Id)
                .ToListAsync();

            var todaySales = await ctx.Sales
                .AsNoTracking()
                .Where(s => todayIds.Contains(s.Id))
                .ToListAsync();

            // أعلى 10 أصناف باستخدام SaleId list بدل Navigation
            var topItems = await ctx.SaleDetails
                .AsNoTracking()
                .Include(sd => sd.Product)
                .Where(sd => todayIds.Contains(sd.SaleId))
                .GroupBy(sd => new { sd.ProductId, sd.Product!.Name, sd.Product.Barcode })
                .Select(g => new
                {
                    g.Key.Barcode,
                    g.Key.Name,
                    Qty = g.Sum(x => x.Quantity),
                    Total = g.Sum(x => x.LineTotal)
                })
                .OrderByDescending(x => x.Total)
                .Take(10)
                .ToListAsync();

            // ملخص عام كأول عنصر
            payload.Data.Add(new Dictionary<string, object>
            {
                ["_summary"] = true,
                ["cnt"] = todaySales.Count,
                ["total"] = todaySales.Sum(s => s.TotalAmount),
                ["cash"] = todaySales.Where(s => s.PaymentMethod == PaymentMethod.Cash).Sum(s => s.TotalAmount),
                ["card"] = todaySales.Where(s => s.PaymentMethod != PaymentMethod.Cash).Sum(s => s.TotalAmount),
            });

            foreach (var item in topItems)
            {
                payload.Data.Add(new Dictionary<string, object>
                {
                    ["b"] = item.Barcode ?? "",
                    ["n"] = item.Name ?? "",
                    ["sq"] = item.Qty,
                    ["st"] = item.Total,
                });
            }
        }
        else if (typeKey == "sales")
        {
            var sales = await ctx.Sales
                .AsNoTracking()
                .OrderByDescending(s => s.SaleDate)
                .Take(20)
                .ToListAsync();

            payload.Data = sales.Select(s => new Dictionary<string, object>
            {
                ["id"] = s.Id,
                ["t"] = new DateTimeOffset(s.SaleDate).ToUnixTimeSeconds(),
                ["am"] = s.TotalAmount,
            }).ToList();
        }
        else // today
        {
            var today = DateTime.Today;
            var sales = await ctx.Sales
                .AsNoTracking()
                .Where(s => s.SaleDate >= today)
                .ToListAsync();

            payload.Data = sales.Select(s => new Dictionary<string, object>
            {
                ["id"] = s.Id,
                ["t"] = new DateTimeOffset(s.SaleDate).ToUnixTimeSeconds(),
                ["am"] = s.TotalAmount,
            }).ToList();
        }

        return payload;
    }

    private string GetWmsExportTypeKey() => SelectedWmsExportType switch
    {
        var s when s.Contains("آخر 20") => "sales",
        var s when s.Contains("اليوم") => "today",
        var s when s.Contains("ناقصة") => "low_stock",
        var s when s.Contains("توريد") => "supply_request",
        var s when s.Contains("تقرير يومي") => "daily_report",
        _ => "stock"
    };

    // ══════════════════════════════════════════════════════════════════
    //  SAVE / PRINT QR
    // ══════════════════════════════════════════════════════════════════
    [RelayCommand]
    private void SaveWmsQr()
    {
        if (_wmsLastQrPng == null) return;
        var dlg = new SaveFileDialog
        {
            FileName = $"pos-wms-{DateTime.Now:yyyyMMdd-HHmm}.png",
            Filter = "PNG Image|*.png"
        };
        if (dlg.ShowDialog() == true)
            File.WriteAllBytes(dlg.FileName, _wmsLastQrPng);
    }

    [RelayCommand]
    private void PrintWmsQr()
    {
        if (_wmsQrImageSource == null) return;
        var pd = new System.Windows.Controls.PrintDialog();
        if (pd.ShowDialog() != true) return;
        var img = new System.Windows.Controls.Image
        {
            Source = _wmsQrImageSource,
            Width = 280,
            Height = 280
        };
        img.Measure(new System.Windows.Size(280, 280));
        img.Arrange(new System.Windows.Rect(0, 0, 280, 280));
        pd.PrintVisual(img, "WMS QR Code");
    }

    // ══════════════════════════════════════════════════════════════════
    //  CAMERA SCAN  (استيراد QR من WMS)
    // ══════════════════════════════════════════════════════════════════
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
                System.Windows.Application.Current.Dispatcher.Invoke(() => WmsCameraFrame = frame);

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
        _wmsCamera?.Dispose();
        _wmsCamera = null;
        _cameraRunning = false;
        WmsCameraFrame = null;
    }

    private void OnWmsQrScanned(string json)
    {
        try
        {
            var payload = JsonSerializer.Deserialize<WmsSyncPayload>(json);
            if (payload?.V == null || payload.Data == null)
                throw new InvalidDataException("QR غير متوافق مع بروتوكول المزامنة.");

            _wmsPendingImport = payload;
            var src = payload.Src == "wms" ? "منصة WMS (المخزن)" : "نظام الكاشير";
            var typeLabel = payload.Type switch
            {
                "dispatch" => "فاتورة صرف",
                "stock" => "لقطة مخزون",
                "supply_request" => "طلب توريد",
                "daily_report" => "تقرير يومي",
                _ => payload.Type
            };
            WmsImportSummary =
                $"المصدر: {src}\n" +
                $"النوع: {typeLabel}\n" +
                $"عدد العناصر: {payload.Data.Count}\n" +
                $"وقت التصدير: {DateTimeOffset.FromUnixTimeSeconds(payload.Ts):dd/MM/yyyy HH:mm}";

            WmsAddSyncLog($"[استيراد] مسح QR ({typeLabel}) من {src} — {payload.Data.Count} عنصر");
        }
        catch (Exception ex)
        {
            MessageBox.Show($"فشل قراءة QR:\n{ex.Message}", "خطأ", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    // ══════════════════════════════════════════════════════════════════
    //  APPLY IMPORT
    // ══════════════════════════════════════════════════════════════════
    [RelayCommand]
    private async Task ApplyWmsImport()
    {
        if (_wmsPendingImport == null || _contextFactory == null) return;
        
        await using var ctx = await _contextFactory!.CreateDbContextAsync();

        var isDispatch = _wmsPendingImport.Type == "dispatch";
        var confirmMsg = isDispatch
            ? $"سيتم إضافة كميات فاتورة الصرف ({_wmsPendingImport.Data.Count} صنف) إلى مخزون الكاشير."
            : $"سيتم تحديث المخزون بناءً على {_wmsPendingImport.Data.Count} عنصر.\nلن يتم حذف أي صنف موجود.";

        if (MessageBox.Show(confirmMsg, "تأكيد المزامنة", MessageBoxButton.YesNo, MessageBoxImage.Question) != MessageBoxResult.Yes)
            return;

        int updated = 0, added = 0, failed = 0;

        // تجاهل الملخص (_summary) في كل الأنواع
        var activeItems = _wmsPendingImport.Data
            .Where(x => !x.Has("_summary"))
            .ToList();

        foreach (var item in activeItems)
        {
            try
            {
                var barcode = item.Str("b");
                if (string.IsNullOrEmpty(barcode)) continue;

                var existing = await ctx.Products
                    .FirstOrDefaultAsync(p => p.Barcode == barcode && !p.IsDeleted);

                if (existing != null)
                {
                    var qty = item.Int("q");

                    if (isDispatch)
                    {
                        // فاتورة صرف: نضيف الكمية المُستلمة على المخزون الحالي
                        existing.Stock += qty;
                        ctx.StockMovements.Add(new StockMovement
                        {
                            ProductId = existing.Id,
                            Quantity = qty,
                            Type = MovementType.Purchase,
                            Reference = "فاتورة صرف من WMS — QR Sync",
                            MovementDate = DateTime.Now
                        });
                    }
                    else
                    {
                        // تحديث عادي: استبدال الكمية
                        existing.Stock = qty;
                    }
                    existing.UpdatedAt = DateTime.Now;
                    await ctx.SaveChangesAsync();
                    updated++;
                }
                else
                {
                    var name = item.Str("n");
                    if (string.IsNullOrWhiteSpace(name)) continue; // لا نضيف صنف بدون اسم

                    var qty = item.Int("q");
                    var defaultCat = await ctx.Categories.FirstOrDefaultAsync();
                    var newProduct = new Product
                    {
                        Barcode = barcode,
                        Name = name,
                        Stock = qty,
                        MinStockLevel = item.Int("mn"),
                        SellingPrice = item.Dec("pr"),
                        CategoryId = defaultCat?.Id ?? 1,
                        IsActive = true,
                        CreatedAt = DateTime.Now,
                    };
                    ctx.Products.Add(newProduct);

                    if (isDispatch && qty > 0)
                    {
                        ctx.StockMovements.Add(new StockMovement
                        {
                            Product = newProduct,
                            Quantity = qty,
                            Type = MovementType.Purchase,
                            Reference = "استلام أولي من WMS — QR Sync",
                            MovementDate = DateTime.Now
                        });
                    }
                    await ctx.SaveChangesAsync();
                    added++;
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[WMS Import] فشل عنصر: {ex.Message}");
                failed++;
            }
        }

        var actionLabel = isDispatch ? "استلام" : "تطبيق";
        CancelWmsImport();
        WmsAddSyncLog($"[{actionLabel}] تحديث {updated} + إضافة {added}{(failed > 0 ? $" • فشل {failed}" : "")}");

        MessageBox.Show(
            $"✅ اكتملت المزامنة!\n\nتحديث: {updated} صنف\nإضافة: {added} صنف\n" +
            (failed > 0 ? $"فشل: {failed} عنصر" : ""),
            "نجاح المزامنة", MessageBoxButton.OK, MessageBoxImage.Information);
    }

    [RelayCommand]
    private void CancelWmsImport()
    {
        _wmsPendingImport = null;
        WmsImportSummary = "";
    }

    // ─── Helper ──────────────────────────────────────────────────────
    private void WmsAddSyncLog(string msg)
    {
        var entry = $"[{DateTime.Now:HH:mm:ss}] {msg}";
        System.Windows.Application.Current.Dispatcher.Invoke(() =>
        {
            WmsSyncLog.Insert(0, entry);
            if (WmsSyncLog.Count > 50) WmsSyncLog.RemoveAt(WmsSyncLog.Count - 1);
        });
    }
}

// ─── Shared QR Payload Model ─────────────────────────────────────────
public sealed class WmsSyncPayload
{
    [JsonPropertyName("v")] public int V { get; set; }
    [JsonPropertyName("src")] public string Src { get; set; } = "";
    [JsonPropertyName("type")] public string Type { get; set; } = "";
    [JsonPropertyName("ts")] public long Ts { get; set; }
    [JsonPropertyName("data")] public List<Dictionary<string, object>> Data { get; set; } = new();
}

/// <summary>
/// Helper آمن لقراءة بيانات من Dictionary<string, object>
/// System.Text.Json يهجر الأرقام كـ JsonElement عند ال Deserialize، هذا الهيلبر يتعامل مع الحالتين.
/// </summary>
internal static class QrDictHelper
{
    public static string? Str(this Dictionary<string, object> d, string key)
    {
        if (!d.TryGetValue(key, out var v)) return null;
        if (v is System.Text.Json.JsonElement je) return je.GetString();
        return v?.ToString();
    }

    public static int Int(this Dictionary<string, object> d, string key, int fallback = 0)
    {
        if (!d.TryGetValue(key, out var v)) return fallback;
        if (v is System.Text.Json.JsonElement je)
            return je.ValueKind == System.Text.Json.JsonValueKind.Number ? je.GetInt32() : fallback;
        try { return Convert.ToInt32(v); } catch { return fallback; }
    }

    public static decimal Dec(this Dictionary<string, object> d, string key, decimal fallback = 0m)
    {
        if (!d.TryGetValue(key, out var v)) return fallback;
        if (v is System.Text.Json.JsonElement je)
            return je.ValueKind == System.Text.Json.JsonValueKind.Number ? je.GetDecimal() : fallback;
        try { return Convert.ToDecimal(v); } catch { return fallback; }
    }

    public static bool Has(this Dictionary<string, object> d, string key)
        => d.ContainsKey(key);
}
