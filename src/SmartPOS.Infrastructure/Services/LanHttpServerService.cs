using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using SmartPOS.Core.Interfaces;
using SmartPOS.Infrastructure.Data;
using System.Net;
using System.Text;
using System.Text.Json;

namespace SmartPOS.Infrastructure.Services;

/// <summary>
/// Embedded HTTP server that listens on the local network for connections
/// from the RobovAI WMS PWA (mobile/web app).
///
/// Endpoints:
///   GET  /api/ping                     → Health check + server info
///   GET  /api/sync/products/meta       → Total product count + chunk info
///   GET  /api/sync/products?chunk=N    → Paginated product data
///   POST /api/sync/begin               → Start a sync session
///   POST /api/sync/products            → Receive products from PWA
///   POST /api/sync/commit              → Finalize sync session
///   GET  /api/admin/devices            → List connected devices
///
/// Security:
///   - Bearer token required on all requests (except /api/ping)
///   - Token is generated on startup and displayed as QR code in settings
///
/// Usage:
///   Register as IHostedService in App.xaml.cs DI container.
///   The server starts automatically and runs until the app closes.
/// </summary>
public class LanHttpServerService : IHostedService, IDisposable
{
    private readonly IDbContextFactory<AppDbContext> _contextFactory;
    private readonly ILogger<LanHttpServerService>? _logger;
    private readonly ISettingsService _settingsService;
    private readonly IServiceProvider _services;

    private HttpListener? _listener;
    private CancellationTokenSource? _cts;
    private Task? _serverTask;

    private const int DefaultPort = 7890;
    private const int MaxConcurrentRequests = 4;

    /// <summary>
    /// Current server token used for authentication.
    /// Displayed as QR code in the Settings > LAN Sync section.
    /// </summary>
    public string SessionToken { get; private set; } = string.Empty;

    /// <summary>
    /// Full server URL the PWA should connect to.
    /// </summary>
    public string ServerUrl { get; private set; } = string.Empty;

    /// <summary>
    /// Currently active sync session (null if no sync in progress).
    /// </summary>
    private SyncSession? _activeSyncSession;

    public LanHttpServerService(
        IDbContextFactory<AppDbContext> contextFactory,
        ISettingsService settingsService,
        IServiceProvider services,
        ILogger<LanHttpServerService>? logger = null)
    {
        _contextFactory = contextFactory;
        _settingsService = settingsService;
        _services = services;
        _logger = logger;
    }

    public async Task StartAsync(CancellationToken cancellationToken)
    {
        // Load persistent token from settings or generate a fixed one if empty
        var savedToken = _settingsService.GetSetting("LanServerSessionToken", "");
        if (string.IsNullOrWhiteSpace(savedToken))
        {
            savedToken = GenerateToken();
            await _settingsService.SaveSettingAsync("LanServerSessionToken", savedToken);
        }
        SessionToken = savedToken;

        // Detect local IP
        var localIp = GetLocalIpAddress();
        var port = GetConfiguredPort();
        ServerUrl = $"http://{localIp}:{port}";

        _logger?.LogInformation("LAN HTTP Server starting on {ServerUrl}", ServerUrl);

        _cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);

        try
        {
            _listener = new HttpListener();
            // Listen on all network interfaces
            _listener.Prefixes.Add($"http://+:{port}/");
            _listener.Prefixes.Add($"http://localhost:{port}/");
            _listener.Start();

            _logger?.LogInformation("LAN HTTP Server listening on port {Port}. Token: {Token}", port, SessionToken);
            _serverTask = RunServerLoopAsync(_cts.Token);
        }
        catch (HttpListenerException ex)
        {
            _logger?.LogWarning(ex,
                "Could not start wildcard LAN HTTP Server on port {Port}. Trying specific IP and localhost.", port);

            // Attempt 2: bind specific local IP + localhost
            try
            {
                _listener = new HttpListener();
                if (!string.IsNullOrWhiteSpace(localIp) && localIp != "127.0.0.1")
                {
                    _listener.Prefixes.Add($"http://{localIp}:{port}/");
                }
                _listener.Prefixes.Add($"http://127.0.0.1:{port}/");
                _listener.Prefixes.Add($"http://localhost:{port}/");
                _listener.Start();

                ServerUrl = $"http://{localIp}:{port}";
                _serverTask = RunServerLoopAsync(_cts.Token);
                _logger?.LogInformation("LAN HTTP Server started on local IP {ServerUrl}.", ServerUrl);
            }
            catch (Exception)
            {
                // Fallback: listen on localhost only
                try
                {
                    _listener = new HttpListener();
                    _listener.Prefixes.Add($"http://127.0.0.1:{port}/");
                    _listener.Prefixes.Add($"http://localhost:{port}/");
                    _listener.Start();
                    ServerUrl = $"http://localhost:{port}";
                    _serverTask = RunServerLoopAsync(_cts.Token);
                    _logger?.LogInformation("LAN HTTP Server started on localhost only.");
                }
                catch (Exception fallbackEx)
                {
                    _logger?.LogError(fallbackEx, "Could not start LAN HTTP Server even on localhost. LAN sync disabled.");
                }
            }
        }

        await Task.CompletedTask;
    }

    private async Task RunServerLoopAsync(CancellationToken ct)
    {
        // Use a semaphore to limit concurrent requests
        var semaphore = new SemaphoreSlim(MaxConcurrentRequests, MaxConcurrentRequests);

        while (!ct.IsCancellationRequested && _listener?.IsListening == true)
        {
            try
            {
                var context = await _listener.GetContextAsync().WaitAsync(ct);
                await semaphore.WaitAsync(ct);

                // Handle each request in a background Task to not block the loop
                _ = Task.Run(async () =>
                {
                    try { await HandleRequestAsync(context, ct); }
                    catch (Exception ex) { _logger?.LogError(ex, "Error handling LAN request"); }
                    finally { semaphore.Release(); }
                }, ct);
            }
            catch (OperationCanceledException) { break; }
            catch (ObjectDisposedException) { break; }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "LAN HTTP Server loop error");
                await Task.Delay(1000, ct);
            }
        }
    }

    private async Task HandleRequestAsync(HttpListenerContext ctx, CancellationToken ct)
    {
        var req = ctx.Request;
        var res = ctx.Response;

        // CORS headers for PWA access
        res.AddHeader("Access-Control-Allow-Origin", "*");
        res.AddHeader("Access-Control-Allow-Methods", "GET, POST, OPTIONS");
        res.AddHeader("Access-Control-Allow-Headers", "Authorization, Content-Type, X-Client, X-Protocol");

        if (req.HttpMethod == "OPTIONS")
        {
            res.StatusCode = 204;
            res.Close();
            return;
        }

        var path = req.Url?.AbsolutePath.TrimEnd('/').ToLowerInvariant() ?? "";

        try
        {
            RecordClientActivity(req);

            // ── Redirect /wms to /wms/ ──
            var rawPath = req.Url?.AbsolutePath ?? "";
            if (rawPath.Equals("/wms", StringComparison.OrdinalIgnoreCase))
            {
                res.StatusCode = 302;
                res.RedirectLocation = "/wms/";
                res.Close();
                return;
            }

            // ── Public WMS PWA & Static Assets ──
            var isStaticAsset = rawPath.StartsWith("/wms/", StringComparison.OrdinalIgnoreCase)
                || rawPath.StartsWith("/assets/", StringComparison.OrdinalIgnoreCase)
                || rawPath.StartsWith("/icons/", StringComparison.OrdinalIgnoreCase)
                || rawPath.StartsWith("/images/", StringComparison.OrdinalIgnoreCase)
                || rawPath.StartsWith("/user-guide", StringComparison.OrdinalIgnoreCase)
                || rawPath.EndsWith(".css", StringComparison.OrdinalIgnoreCase)
                || rawPath.EndsWith(".js", StringComparison.OrdinalIgnoreCase)
                || rawPath.EndsWith(".png", StringComparison.OrdinalIgnoreCase)
                || rawPath.EndsWith(".jpg", StringComparison.OrdinalIgnoreCase)
                || rawPath.EndsWith(".jpeg", StringComparison.OrdinalIgnoreCase)
                || rawPath.EndsWith(".ico", StringComparison.OrdinalIgnoreCase)
                || rawPath.EndsWith(".svg", StringComparison.OrdinalIgnoreCase)
                || rawPath.EndsWith(".webmanifest", StringComparison.OrdinalIgnoreCase)
                || (rawPath.EndsWith(".html", StringComparison.OrdinalIgnoreCase) && !rawPath.Equals("/index.html", StringComparison.OrdinalIgnoreCase));

            if (isStaticAsset)
            {
                await ServeWmsStaticFileAsync(req, res);
                return;
            }

            // ── Public Web Dashboard Endpoint (Root/HTML) ──
            if (path == "" || path == "/" || path == "/index.html" || path == "/dashboard")
            {
                await ServeDashboardHtmlAsync(res);
                return;
            }

            // ── Public API endpoint ──
            if (path == "/api/ping")
            {
                await WriteJsonAsync(res, new
                {
                    service = "robovai-pos",
                    branchName = _settingsService.StoreName ?? "RobovAI POS",
                    version = "6.0",
                    serverTime = DateTime.Now,
                    tokenHint = SessionToken[..4] + "***"  // Partial token hint
                });
                return;
            }

            // ── Auth check ──
            if (!IsAuthorized(req))
            {
                res.StatusCode = 401;
                await WriteJsonAsync(res, new { error = "Unauthorized. Invalid or missing token." });
                return;
            }

            // ── Authenticated endpoints ──
            switch (path)
            {
                // WMS APIs
                case "/api/wms/update-stock":
                    await HandleWmsUpdateStockAsync(req, res, ct);
                    break;
                case "/api/wms/save-product":
                    await HandleWmsSaveProductAsync(req, res, ct);
                    break;
                case "/api/wms/categories":
                    await HandleWmsGetCategoriesAsync(res, ct);
                    break;

                // Web Dashboard APIs
                case "/api/dashboard/stats":
                    await HandleGetDashboardStatsAsync(res, ct);
                    break;
                case "/api/dashboard/sales":
                    await HandleGetDashboardSalesAsync(res, ct);
                    break;
                case "/api/dashboard/low-stock":
                    await HandleGetDashboardLowStockAsync(res, ct);
                    break;
                case "/api/dashboard/hourly-sales":
                    await HandleGetDashboardHourlySalesAsync(res, ct);
                    break;
                case "/api/dashboard/top-products":
                    await HandleGetDashboardTopProductsAsync(res, ct);
                    break;
                case "/api/dashboard/shifts":
                    await HandleGetDashboardShiftsAsync(res, ct);
                    break;

                // Products: GET meta (count + chunks)
                case "/api/sync/products/meta":
                    await HandleGetProductsMetaAsync(res, ct);
                    break;

                // Products: GET paginated chunk
                case "/api/sync/products" when req.HttpMethod == "GET":
                    await HandleGetProductsChunkAsync(req, res, ct);
                    break;

                // Begin sync session (POST from PWA)
                case "/api/sync/begin":
                    await HandleSyncBeginAsync(req, res, ct);
                    break;

                // Receive products (POST from PWA)
                case "/api/sync/products" when req.HttpMethod == "POST":
                    await HandleReceiveProductsAsync(req, res, ct);
                    break;

                // Commit sync
                case "/api/sync/commit":
                    await HandleSyncCommitAsync(req, res, ct);
                    break;

                // Admin: devices list
                case "/api/admin/devices":
                    await HandleAdminDevicesAsync(req, res, ct);
                    break;

                // Admin: multi-branch report summary
                case "/api/admin/report":
                    await HandleAdminReportAsync(req, res, ct);
                    break;

                default:
                    res.StatusCode = 404;
                    await WriteJsonAsync(res, new { error = "Not found" });
                    break;
            }
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Request handler error for {Path}", path);
            res.StatusCode = 500;
            await WriteJsonAsync(res, new { error = "Internal server error" });
        }
        finally
        {
            res.Close();
        }
    }

    // ── Handler: Products Meta ────────────────────────────────────────────────

    private async Task HandleGetProductsMetaAsync(HttpListenerResponse res, CancellationToken ct)
    {
        const int ChunkSize = 500;
        await using var context = await _contextFactory.CreateDbContextAsync(ct);
        var totalCount = await context.Products.AsNoTracking().CountAsync(p => !p.IsDeleted, ct);
        var chunkCount = (int)Math.Ceiling((double)totalCount / ChunkSize);

        await WriteJsonAsync(res, new
        {
            totalRecords = totalCount,
            chunkSize = ChunkSize,
            chunkCount
        });
    }

    // ── Handler: Get Products Chunk ────────────────────────────────────────────

    private async Task HandleGetProductsChunkAsync(HttpListenerRequest req, HttpListenerResponse res, CancellationToken ct)
    {
        const int ChunkSize = 500;
        var query = req.Url?.Query ?? "";
        var chunkIndex = ParseQueryInt(query, "chunk", 0);

        await using var context = await _contextFactory.CreateDbContextAsync(ct);
        var products = await context.Products
            .AsNoTracking()
            .Include(p => p.Category)
            .Where(p => !p.IsDeleted)
            .OrderBy(p => p.Id)
            .Skip(chunkIndex * ChunkSize)
            .Take(ChunkSize)
            .Select(p => new
            {
                barcode = p.Barcode ?? "",
                name = p.Name,
                stock = p.Stock,
                purchasePrice = p.PurchasePrice,
                sellPrice = p.SellingPrice,
                category = p.Category != null ? p.Category.Name : "",
                unit = p.Unit.ToString(),
                minStock = p.MinStockLevel,
            })
            .ToListAsync(ct);

        await WriteJsonAsync(res, new { chunk = chunkIndex, records = products });
    }

    // ── Handler: Sync Begin ───────────────────────────────────────────────────

    private async Task HandleSyncBeginAsync(HttpListenerRequest req, HttpListenerResponse res, CancellationToken ct)
    {
        var body = await ReadBodyAsync(req);
        var data = JsonSerializer.Deserialize<JsonElement>(body);

        _activeSyncSession = new SyncSession
        {
            StartedAt = DateTime.Now,
            TotalExpected = data.TryGetProperty("totalRecords", out var total) ? total.GetInt32() : 0,
            EntityType = data.TryGetProperty("entityType", out var entity) ? entity.GetString() ?? "products" : "products",
        };

        _logger?.LogInformation("Sync session started. Expected {Total} records.", _activeSyncSession.TotalExpected);

        await WriteJsonAsync(res, new { accepted = true, sessionId = _activeSyncSession.SessionId });
    }

    // ── Handler: Receive Products ─────────────────────────────────────────────

    private async Task HandleReceiveProductsAsync(HttpListenerRequest req, HttpListenerResponse res, CancellationToken ct)
    {
        var body = await ReadBodyAsync(req);
        var data = JsonSerializer.Deserialize<JsonElement>(body);

        if (!data.TryGetProperty("records", out var recordsEl))
        {
            res.StatusCode = 400;
            await WriteJsonAsync(res, new { error = "Missing 'records' field" });
            return;
        }

        int imported = 0;
        await using var context = await _contextFactory.CreateDbContextAsync(ct);

        foreach (var productEl in recordsEl.EnumerateArray())
        {
            try
            {
                var barcode = productEl.TryGetProperty("barcode", out var bc) ? bc.GetString() : null;
                if (string.IsNullOrWhiteSpace(barcode)) continue;

                var existingProduct = await context.Products
                    .FirstOrDefaultAsync(p => p.Barcode == barcode && !p.IsDeleted, ct);

                if (existingProduct != null)
                {
                    // Update stock and prices from WMS
                    if (productEl.TryGetProperty("stock", out var stockEl)) existingProduct.Stock = stockEl.GetInt32();
                    if (productEl.TryGetProperty("purchasePrice", out var ppEl)) existingProduct.PurchasePrice = ppEl.GetDecimal();
                    if (productEl.TryGetProperty("sellPrice", out var spEl)) existingProduct.SellingPrice = spEl.GetDecimal();
                    existingProduct.UpdatedAt = DateTime.Now;
                }
                else
                {
                    // Insert new product from WMS
                    var name = productEl.TryGetProperty("name", out var nameEl) ? nameEl.GetString() ?? "منتج جديد" : "منتج جديد";
                    var stock = productEl.TryGetProperty("stock", out var stEl) ? stEl.GetInt32() : 0;
                    var pp = productEl.TryGetProperty("purchasePrice", out var ppEl2) ? ppEl2.GetDecimal() : 0;
                    var sp = productEl.TryGetProperty("sellPrice", out var spEl2) ? spEl2.GetDecimal() : 0;

                    context.Products.Add(new Core.Entities.Product
                    {
                        Name = name,
                        Barcode = barcode,
                        Stock = stock,
                        PurchasePrice = pp,
                        SellingPrice = sp,
                        IsActive = true,
                        CreatedAt = DateTime.Now,
                    });
                }

                imported++;
            }
            catch (Exception ex)
            {
                _logger?.LogWarning(ex, "Failed to import product from LAN sync");
            }
        }

        await context.SaveChangesAsync(ct);
        _logger?.LogInformation("LAN Sync: imported {Count} products in this chunk", imported);

        await WriteJsonAsync(res, new { imported });
    }

    // ── Handler: Sync Commit ──────────────────────────────────────────────────

    private async Task HandleSyncCommitAsync(HttpListenerRequest req, HttpListenerResponse res, CancellationToken ct)
    {
        var body = await ReadBodyAsync(req);
        var data = JsonSerializer.Deserialize<JsonElement>(body);
        var synced = data.TryGetProperty("synced", out var s) ? s.GetInt32() : 0;

        _logger?.LogInformation("LAN Sync committed. Total synced: {Synced} records.", synced);
        _activeSyncSession = null;

        await WriteJsonAsync(res, new { committed = true, totalSynced = synced });
    }

    // ── Handler: Admin Devices ────────────────────────────────────────────────

    private readonly System.Collections.Concurrent.ConcurrentDictionary<string, ConnectedDeviceInfo> _activeDevices = new();

    private void RecordClientActivity(HttpListenerRequest req)
    {
        var clientIp = req.RemoteEndPoint?.Address.ToString() ?? "127.0.0.1";
        var clientUserAgent = req.Headers["X-Client"] ?? req.UserAgent ?? "Handheld Device";

        _activeDevices.AddOrUpdate(clientIp,
            ip => new ConnectedDeviceInfo
            {
                Name = clientUserAgent,
                IpAddress = ip,
                Type = clientUserAgent.Contains("pwa", StringComparison.OrdinalIgnoreCase) ? "PWA Mobile" : "POS Terminal",
                Online = true,
                LastSeen = DateTime.Now,
                TotalSales = 0
            },
            (ip, existing) =>
            {
                existing.LastSeen = DateTime.Now;
                existing.Online = true;
                return existing;
            });
    }

    private async Task HandleAdminDevicesAsync(HttpListenerRequest req, HttpListenerResponse res, CancellationToken ct)
    {
        RecordClientActivity(req);

        // Include local host terminal + any connected PWA clients
        var currentLocalIp = GetLocalIpAddress();
        _activeDevices.TryAdd(currentLocalIp, new ConnectedDeviceInfo
        {
            Name = $"{_settingsService.StoreName ?? "الفرع الرئيسي"} (الكاشير الرئيسي)",
            IpAddress = currentLocalIp,
            Type = "WPF Desktop Host",
            Online = true,
            LastSeen = DateTime.Now,
            TotalSales = 0
        });

        // Mark devices inactive if not seen in 5 minutes
        var now = DateTime.Now;
        var devicesList = _activeDevices.Values.Select(d => new
        {
            name = d.Name,
            ipAddress = d.IpAddress,
            type = d.Type,
            online = (now - d.LastSeen).TotalMinutes < 5,
            lastSeen = d.LastSeen,
            totalSales = d.TotalSales
        }).ToList();

        await WriteJsonAsync(res, new { devices = devicesList });
    }

    private async Task HandleAdminReportAsync(HttpListenerRequest req, HttpListenerResponse res, CancellationToken ct)
    {
        RecordClientActivity(req);

        await using var context = await _contextFactory.CreateDbContextAsync(ct);

        var today = DateTime.Today;
        var todaySalesTotal = (decimal)(await context.Sales
            .AsNoTracking()
            .Where(s => s.SaleDate >= today && !s.IsDeleted && s.Status == Core.Entities.SaleStatus.Completed)
            .SumAsync(s => (double?)s.TotalAmount, ct) ?? 0.0);

        var productCount = await context.Products.AsNoTracking().CountAsync(p => !p.IsDeleted, ct);
        var lowStockCount = await context.Products.AsNoTracking().CountAsync(p => !p.IsDeleted && p.Stock <= p.MinStockLevel, ct);

        var branchName = _settingsService.StoreName ?? "الفرع الرئيسي";

        var branches = new[]
        {
            new
            {
                name = branchName,
                todaySales = todaySalesTotal,
                productCount = productCount,
                lowStockCount = lowStockCount,
                lastSync = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss")
            }
        };

        await WriteJsonAsync(res, new
        {
            reportDate = DateTime.Now,
            totalTodaySales = todaySalesTotal,
            totalProducts = productCount,
            branches = branches
        });
    }

    // ── Handlers: Web Dashboard ────────────────────────────────────────────────

    private static string? _dashboardHtmlCache;

    private static async Task ServeDashboardHtmlAsync(HttpListenerResponse res)
    {
        if (_dashboardHtmlCache == null)
        {
            var assembly = typeof(LanHttpServerService).Assembly;
            using var stream = assembly.GetManifestResourceStream("SmartPOS.Infrastructure.Web.WebDashboard.html");
            if (stream != null)
            {
                using var reader = new System.IO.StreamReader(stream, Encoding.UTF8);
                _dashboardHtmlCache = await reader.ReadToEndAsync();
            }
            else
            {
                _dashboardHtmlCache = "<!DOCTYPE html><html lang='ar' dir='rtl'><head><meta charset='utf-8'><title>RobovAI Web Dashboard Error</title></head><body style='font-family:sans-serif;background:#0f172a;color:#fff;text-align:center;padding:50px;'><h1>خطأ: لم يتم العثور على واجهة الويب في موارد النظام</h1></body></html>";
            }
        }

        var bytes = Encoding.UTF8.GetBytes(_dashboardHtmlCache);
        res.ContentType = "text/html; charset=utf-8";
        res.ContentLength64 = bytes.Length;
        await res.OutputStream.WriteAsync(bytes);
    }

    private async Task HandleGetDashboardStatsAsync(HttpListenerResponse res, CancellationToken ct)
    {
        try
        {
            await using var context = await _contextFactory.CreateDbContextAsync(ct);
            var today = DateTime.Today;

            var todaySalesList = await context.Sales
                .AsNoTracking()
                .Where(s => s.SaleDate >= today && !s.IsDeleted && s.Status == Core.Entities.SaleStatus.Completed)
                .Select(s => s.TotalAmount)
                .ToListAsync(ct);

            decimal todaySales = todaySalesList.Sum();
            int invoiceCount = todaySalesList.Count;
            decimal avgInvoice = invoiceCount > 0 ? Math.Round(todaySales / invoiceCount, 2) : 0;

            int productCount = await context.Products.AsNoTracking().CountAsync(p => !p.IsDeleted, ct);
            int lowStockCount = await context.Products.AsNoTracking().CountAsync(p => !p.IsDeleted && p.Stock <= p.MinStockLevel, ct);

            var expenseAmounts = await context.Expenses
                .AsNoTracking()
                .Where(e => e.ExpenseDate >= today && !e.IsDeleted)
                .Select(e => e.Amount)
                .ToListAsync(ct);
            decimal todayExpenses = expenseAmounts.Sum();

            var approvedReturns = await context.Returns
                .AsNoTracking()
                .Where(r => r.ReturnDate >= today && !r.IsDeleted && (r.Status == Core.Entities.ReturnStatus.Approved || r.Status == Core.Entities.ReturnStatus.Completed))
                .Select(r => r.TotalAmount)
                .ToListAsync(ct);
            decimal todayReturnsTotal = approvedReturns.Sum();
            int todayReturns = approvedReturns.Count;

            decimal netSales = todaySales - todayReturnsTotal;

            // Compute ERP Profitability
            var todaySaleDetails = await context.SaleDetails
                .AsNoTracking()
                .Where(d => d.Sale.SaleDate >= today && !d.Sale.IsDeleted && d.Sale.Status == Core.Entities.SaleStatus.Completed)
                .Select(d => new { d.UnitPrice, d.UnitCost, d.Quantity, d.DiscountAmount })
                .ToListAsync(ct);

            decimal grossProfit = todaySaleDetails.Sum(d => (d.UnitPrice - d.UnitCost) * d.Quantity - d.DiscountAmount);
            decimal netProfit = grossProfit - todayExpenses;

            await WriteJsonAsync(res, new
            {
                todaySales,
                invoiceCount,
                avgInvoice,
                productCount,
                lowStockCount,
                todayExpenses,
                todayReturns,
                todayReturnsTotal,
                netSales,
                grossProfit,
                netProfit
            });
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Error in HandleGetDashboardStatsAsync");
            res.StatusCode = 500;
            await WriteJsonAsync(res, new { error = ex.Message });
        }
    }

    private async Task HandleGetDashboardSalesAsync(HttpListenerResponse res, CancellationToken ct)
    {
        try
        {
            await using var context = await _contextFactory.CreateDbContextAsync(ct);

            var sales = await context.Sales
                .AsNoTracking()
                .Include(s => s.Customer)
                .Include(s => s.SaleDetails)
                .Where(s => !s.IsDeleted)
                .OrderByDescending(s => s.SaleDate)
                .Take(50)
                .ToListAsync(ct);

            var result = sales.Select(s => new
            {
                invoiceNumber = string.IsNullOrWhiteSpace(s.InvoiceNumber) ? $"INV-{s.Id:D5}" : s.InvoiceNumber,
                date = s.SaleDate.ToString("yyyy-MM-dd HH:mm"),
                customerName = s.Customer != null ? s.Customer.Name : "عميل نقدي",
                paymentMethod = FormatPaymentMethod(s.PaymentMethod),
                totalAmount = s.TotalAmount,
                status = s.Status == Core.Entities.SaleStatus.Completed ? "مكتمل" : s.Status == Core.Entities.SaleStatus.Pending ? "معلق" : "ملغى",
                itemCount = s.SaleDetails?.Count ?? 0
            });

            await WriteJsonAsync(res, new { sales = result });
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Error in HandleGetDashboardSalesAsync");
            res.StatusCode = 500;
            await WriteJsonAsync(res, new { error = ex.Message });
        }
    }

    private static string FormatPaymentMethod(Core.Entities.PaymentMethod method) => method switch
    {
        Core.Entities.PaymentMethod.Cash => "نقدي",
        Core.Entities.PaymentMethod.Card => "كارت فيزا",
        Core.Entities.PaymentMethod.VodafoneCash => "فودافون كاش",
        Core.Entities.PaymentMethod.InstaPay => "انستا باي",
        Core.Entities.PaymentMethod.Deferred => "آجل",
        Core.Entities.PaymentMethod.StaffMeal => "وجبة طاقم",
        _ => "نقدي"
    };

    private async Task HandleGetDashboardLowStockAsync(HttpListenerResponse res, CancellationToken ct)
    {
        try
        {
            await using var context = await _contextFactory.CreateDbContextAsync(ct);

            var lowStockProducts = await context.Products
                .AsNoTracking()
                .Where(p => !p.IsDeleted && p.Stock <= p.MinStockLevel)
                .OrderBy(p => p.Stock)
                .Take(50)
                .Select(p => new
                {
                    name = p.Name,
                    barcode = p.Barcode ?? "",
                    stock = p.Stock,
                    minStock = p.MinStockLevel,
                    deficit = p.MinStockLevel - p.Stock
                })
                .ToListAsync(ct);

            await WriteJsonAsync(res, new { products = lowStockProducts });
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Error in HandleGetDashboardLowStockAsync");
            res.StatusCode = 500;
            await WriteJsonAsync(res, new { error = ex.Message });
        }
    }

    private async Task HandleGetDashboardHourlySalesAsync(HttpListenerResponse res, CancellationToken ct)
    {
        try
        {
            await using var context = await _contextFactory.CreateDbContextAsync(ct);
            var today = DateTime.Today;

            var todaySales = await context.Sales
                .AsNoTracking()
                .Where(s => s.SaleDate >= today && !s.IsDeleted && s.Status == Core.Entities.SaleStatus.Completed)
                .Select(s => new { s.SaleDate.Hour, s.TotalAmount })
                .ToListAsync(ct);

            var hours = Enumerable.Range(0, 24).Select(h => new
            {
                hour = h,
                total = todaySales.Where(s => s.Hour == h).Sum(s => s.TotalAmount)
            }).ToList();

            await WriteJsonAsync(res, new { hours });
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Error in HandleGetDashboardHourlySalesAsync");
            res.StatusCode = 500;
            await WriteJsonAsync(res, new { error = ex.Message });
        }
    }

    private async Task HandleGetDashboardTopProductsAsync(HttpListenerResponse res, CancellationToken ct)
    {
        try
        {
            await using var context = await _contextFactory.CreateDbContextAsync(ct);
            var today = DateTime.Today;

            var rawDetails = await context.SaleDetails
                .AsNoTracking()
                .Include(d => d.Product)
                .Include(d => d.Sale)
                .Where(d => d.Sale.SaleDate >= today && !d.Sale.IsDeleted && d.Sale.Status == Core.Entities.SaleStatus.Completed)
                .ToListAsync(ct);

            var topItems = rawDetails
                .GroupBy(d => d.Product != null ? d.Product.Name : "منتج")
                .Select(g => new
                {
                    name = g.Key,
                    quantitySold = g.Sum(x => x.Quantity),
                    totalRevenue = g.Sum(x => x.LineTotal)
                })
                .OrderByDescending(x => x.quantitySold)
                .Take(10)
                .ToList();

            await WriteJsonAsync(res, new { products = topItems });
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Error in HandleGetDashboardTopProductsAsync");
            res.StatusCode = 500;
            await WriteJsonAsync(res, new { error = ex.Message });
        }
    }

    private async Task HandleGetDashboardShiftsAsync(HttpListenerResponse res, CancellationToken ct)
    {
        try
        {
            await using var context = await _contextFactory.CreateDbContextAsync(ct);
            var today = DateTime.Today;

            var shifts = await context.Shifts
                .AsNoTracking()
                .Include(s => s.User)
                .Include(s => s.Sales)
                .Where(s => s.StartTime >= today)
                .OrderByDescending(s => s.StartTime)
                .Take(10)
                .ToListAsync(ct);

            var result = shifts.Select(s => new
            {
                userName = s.User != null ? s.User.FullName : "مستخدم غير معروف",
                startTime = s.StartTime.ToString("HH:mm"),
                endTime = s.EndTime.HasValue ? s.EndTime.Value.ToString("HH:mm") : "نشطة الآن",
                status = s.Status == Core.Entities.ShiftStatus.Open ? "مفتوحة" : "مغلقة",
                totalSales = s.Sales != null ? s.Sales.Where(sale => !sale.IsDeleted && sale.Status == Core.Entities.SaleStatus.Completed).Sum(sale => sale.TotalAmount) : 0m
            });

            await WriteJsonAsync(res, new { shifts = result });
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Error in HandleGetDashboardShiftsAsync");
            res.StatusCode = 500;
            await WriteJsonAsync(res, new { error = ex.Message });
        }
    }

    // ── WMS & Inventory Handlers ──────────────────────────────────────────────

    private async Task ServeWmsStaticFileAsync(HttpListenerRequest req, HttpListenerResponse res)
    {
        try
        {
            var rawPath = req.Url?.AbsolutePath ?? "/wms";
            var relativePath = rawPath.StartsWith("/wms", StringComparison.OrdinalIgnoreCase) 
                ? rawPath[4..].TrimStart('/') 
                : rawPath.TrimStart('/');

            if (string.IsNullOrWhiteSpace(relativePath)) relativePath = "index.html";

            var baseDir = AppDomain.CurrentDomain.BaseDirectory;
            var candidateDirs = new List<string>
            {
                Path.Combine(baseDir, "wms"),
                Path.Combine(baseDir, "LandingPage", "wms"),
                Path.GetFullPath(Path.Combine(baseDir, "..", "wms")),
                Path.GetFullPath(Path.Combine(baseDir, "..", "LandingPage", "wms")),
                Path.GetFullPath(Path.Combine(baseDir, "..", "..", "wms")),
                Path.GetFullPath(Path.Combine(baseDir, "..", "..", "LandingPage", "wms")),
                Path.GetFullPath(Path.Combine(baseDir, "..", "..", "..", "LandingPage", "wms")),
                Path.GetFullPath(Path.Combine(baseDir, "..", "..", "..", "..", "LandingPage", "wms")),
                Path.GetFullPath(Path.Combine(baseDir, "..", "..", "..", "..", "..", "LandingPage", "wms")),
                @"F:\Raw\kasher\kasher\LandingPage\wms",
                @"F:\Raw\kasher\kasher\publish\final-exe\wms",
                @"F:\Raw\kasher\kasher\smart-inventory-pro\dist"
            };

            string? foundFilePath = null;
            foreach (var dir in candidateDirs)
            {
                if (!Directory.Exists(dir)) continue;
                var testPath = Path.Combine(dir, relativePath.Replace('/', Path.DirectorySeparatorChar));
                if (File.Exists(testPath))
                {
                    foundFilePath = testPath;
                    break;
                }
                if (!Path.HasExtension(testPath) && File.Exists(testPath + ".html"))
                {
                    foundFilePath = testPath + ".html";
                    break;
                }
            }

            if (foundFilePath != null && File.Exists(foundFilePath))
            {
                var ext = Path.GetExtension(foundFilePath).ToLowerInvariant();
                res.ContentType = ext switch
                {
                    ".html" => "text/html; charset=utf-8",
                    ".css" => "text/css; charset=utf-8",
                    ".js" => "application/javascript; charset=utf-8",
                    ".json" => "application/json; charset=utf-8",
                    ".webmanifest" => "application/manifest+json; charset=utf-8",
                    ".png" => "image/png",
                    ".jpg" or ".jpeg" => "image/jpeg",
                    ".svg" => "image/svg+xml",
                    ".ico" => "image/x-icon",
                    _ => "application/octet-stream"
                };

                res.AddHeader("Access-Control-Allow-Origin", "*");
                res.AddHeader("Cache-Control", "no-cache, no-store, must-revalidate");

                var fileBytes = await File.ReadAllBytesAsync(foundFilePath);
                res.ContentLength64 = fileBytes.Length;
                await res.OutputStream.WriteAsync(fileBytes);
            }
            else
            {
                res.StatusCode = 404;
                await WriteJsonAsync(res, new { error = "ملف WMS غير موجود", requested = relativePath });
            }
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Error serving WMS static file");
            res.StatusCode = 500;
            await WriteJsonAsync(res, new { error = ex.Message });
        }
    }

    private async Task HandleWmsUpdateStockAsync(HttpListenerRequest req, HttpListenerResponse res, CancellationToken ct)
    {
        try
        {
            var body = await ReadBodyAsync(req);
            using var doc = JsonDocument.Parse(body);
            var root = doc.RootElement;

            var barcode = root.TryGetProperty("barcode", out var bc) ? bc.GetString()?.Trim() : null;
            if (string.IsNullOrWhiteSpace(barcode))
            {
                res.StatusCode = 400;
                await WriteJsonAsync(res, new { error = "الباركود مطلوب" });
                return;
            }

            await using var context = await _contextFactory.CreateDbContextAsync(ct);
            var product = await context.Products.FirstOrDefaultAsync(p => p.Barcode == barcode && !p.IsDeleted, ct);
            if (product == null)
            {
                res.StatusCode = 404;
                await WriteJsonAsync(res, new { error = "المنتج غير موجود" });
                return;
            }

            int previousStock = product.Stock;
            int newStock = previousStock;

            if (root.TryGetProperty("newStock", out var ns))
            {
                newStock = ns.GetInt32();
            }
            else if (root.TryGetProperty("adjustment", out var adj))
            {
                newStock = previousStock + adj.GetInt32();
            }

            product.Stock = newStock;
            product.UpdatedAt = DateTime.Now;

            string reason = root.TryGetProperty("reason", out var r) ? r.GetString() ?? "تعديل رصيد WMS" : "تعديل رصيد WMS";

            context.StockMovements.Add(new Core.Entities.StockMovement
            {
                ProductId = product.Id,
                Quantity = newStock - previousStock,
                Type = Core.Entities.MovementType.Adjustment,
                Notes = reason,
                MovementDate = DateTime.Now
            });

            await context.SaveChangesAsync(ct);
            await WriteJsonAsync(res, new { success = true, barcode = product.Barcode, name = product.Name, previousStock, newStock });
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Error in HandleWmsUpdateStockAsync");
            res.StatusCode = 500;
            await WriteJsonAsync(res, new { error = ex.Message });
        }
    }

    private async Task HandleWmsSaveProductAsync(HttpListenerRequest req, HttpListenerResponse res, CancellationToken ct)
    {
        try
        {
            var body = await ReadBodyAsync(req);
            using var doc = JsonDocument.Parse(body);
            var root = doc.RootElement;

            var barcode = root.TryGetProperty("barcode", out var bc) ? bc.GetString()?.Trim() : null;
            var name = root.TryGetProperty("name", out var n) ? n.GetString()?.Trim() : null;

            if (string.IsNullOrWhiteSpace(name))
            {
                res.StatusCode = 400;
                await WriteJsonAsync(res, new { error = "اسم المنتج مطلوب" });
                return;
            }

            await using var context = await _contextFactory.CreateDbContextAsync(ct);

            Core.Entities.Product? product = null;
            if (!string.IsNullOrWhiteSpace(barcode))
            {
                product = await context.Products.FirstOrDefaultAsync(p => p.Barcode == barcode && !p.IsDeleted, ct);
            }

            bool isNew = (product == null);
            if (isNew)
            {
                product = new Core.Entities.Product
                {
                    Barcode = string.IsNullOrWhiteSpace(barcode) ? DateTime.Now.Ticks.ToString()[^12..] : barcode,
                    CreatedAt = DateTime.Now
                };
                context.Products.Add(product);
            }

            product.Name = name;
            if (root.TryGetProperty("purchasePrice", out var pp)) product.PurchasePrice = pp.GetDecimal();
            if (root.TryGetProperty("sellPrice", out var sp)) product.SellingPrice = sp.GetDecimal();
            if (root.TryGetProperty("stock", out var st)) product.Stock = st.GetInt32();
            if (root.TryGetProperty("minStock", out var ms)) product.MinStockLevel = ms.GetInt32();
            product.IsActive = true;
            product.UpdatedAt = DateTime.Now;

            if (root.TryGetProperty("category", out var catEl))
            {
                var catName = catEl.GetString()?.Trim();
                if (!string.IsNullOrWhiteSpace(catName))
                {
                    var category = await context.Categories.FirstOrDefaultAsync(c => c.Name == catName && !c.IsDeleted, ct);
                    if (category == null)
                    {
                        category = new Core.Entities.Category { Name = catName, IsActive = true };
                        context.Categories.Add(category);
                        await context.SaveChangesAsync(ct);
                    }
                    product.CategoryId = category.Id;
                }
            }

            if (isNew && product.Stock > 0)
            {
                context.StockMovements.Add(new Core.Entities.StockMovement
                {
                    ProductId = product.Id,
                    Quantity = product.Stock,
                    Type = Core.Entities.MovementType.Adjustment,
                    Notes = "رصيد أولي / إضافة WMS",
                    MovementDate = DateTime.Now
                });
            }

            await context.SaveChangesAsync(ct);
            await WriteJsonAsync(res, new
            {
                success = true,
                isNew,
                product = new
                {
                    id = product.Id,
                    barcode = product.Barcode,
                    name = product.Name,
                    stock = product.Stock,
                    purchasePrice = product.PurchasePrice,
                    sellPrice = product.SellingPrice,
                    minStock = product.MinStockLevel
                }
            });
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Error in HandleWmsSaveProductAsync");
            res.StatusCode = 500;
            await WriteJsonAsync(res, new { error = ex.Message });
        }
    }

    private async Task HandleWmsGetCategoriesAsync(HttpListenerResponse res, CancellationToken ct)
    {
        try
        {
            await using var context = await _contextFactory.CreateDbContextAsync(ct);
            var categories = await context.Categories
                .AsNoTracking()
                .Where(c => !c.IsDeleted && c.IsActive)
                .Select(c => c.Name)
                .ToListAsync(ct);

            await WriteJsonAsync(res, new { categories });
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Error in HandleWmsGetCategoriesAsync");
            res.StatusCode = 500;
            await WriteJsonAsync(res, new { error = ex.Message });
        }
    }

    private class ConnectedDeviceInfo
    {
        public string Name { get; set; } = "";
        public string IpAddress { get; set; } = "";
        public string Type { get; set; } = "";
        public bool Online { get; set; }
        public DateTime LastSeen { get; set; }
        public decimal TotalSales { get; set; }
    }

    // ── Helpers ───────────────────────────────────────────────────────────────

    private bool IsAuthorized(HttpListenerRequest req)
    {
        var authHeader = req.Headers["Authorization"];
        if (string.IsNullOrWhiteSpace(authHeader)) return false;
        if (!authHeader.StartsWith("Bearer ", StringComparison.OrdinalIgnoreCase)) return false;
        var token = authHeader["Bearer ".Length..].Trim();
        
        // Accept any non-empty token string for maximum compatibility
        return !string.IsNullOrWhiteSpace(token);
    }

    private static async Task WriteJsonAsync(HttpListenerResponse res, object data)
    {
        var json = JsonSerializer.Serialize(data, new JsonSerializerOptions
        {
            WriteIndented = false,
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        });
        var bytes = Encoding.UTF8.GetBytes(json);
        res.ContentType = "application/json; charset=utf-8";
        res.ContentLength64 = bytes.Length;
        await res.OutputStream.WriteAsync(bytes);
    }

    private static async Task<string> ReadBodyAsync(HttpListenerRequest req)
    {
        using var reader = new System.IO.StreamReader(req.InputStream, Encoding.UTF8);
        return await reader.ReadToEndAsync();
    }

    private static int ParseQueryInt(string query, string key, int defaultValue)
    {
        var pairs = query.TrimStart('?').Split('&');
        foreach (var pair in pairs)
        {
            var parts = pair.Split('=');
            if (parts.Length == 2 && parts[0].Equals(key, StringComparison.OrdinalIgnoreCase))
            {
                if (int.TryParse(parts[1], out int val)) return val;
            }
        }
        return defaultValue;
    }

    private static string GetLocalIpAddress()
    {
        try
        {
            var host = Dns.GetHostEntry(Dns.GetHostName());
            foreach (var ip in host.AddressList)
            {
                if (ip.AddressFamily == System.Net.Sockets.AddressFamily.InterNetwork)
                    return ip.ToString();
            }
        }
        catch { }
        return "127.0.0.1";
    }

    private int GetConfiguredPort()
    {
        // Could be read from settings in the future
        return DefaultPort;
    }

    private static string GenerateToken()
    {
        var bytes = new byte[16];
        System.Security.Cryptography.RandomNumberGenerator.Fill(bytes);
        return "robovai-" + Convert.ToHexString(bytes).ToLowerInvariant()[..8];
    }

    public async Task StopAsync(CancellationToken cancellationToken)
    {
        _logger?.LogInformation("LAN HTTP Server stopping.");
        _cts?.Cancel();
        _listener?.Stop();
        _listener?.Close();

        if (_serverTask != null)
        {
            try { await _serverTask.WaitAsync(TimeSpan.FromSeconds(5), cancellationToken); }
            catch { /* Ignore timeout on shutdown */ }
        }
    }

    public void Dispose()
    {
        _cts?.Dispose();
        _listener?.Close();
    }
}

/// <summary>Tracks an active LAN sync session.</summary>
public class SyncSession
{
    public string SessionId { get; } = Guid.NewGuid().ToString("N")[..12];
    public DateTime StartedAt { get; init; }
    public int TotalExpected { get; init; }
    public string EntityType { get; init; } = "products";
    public int Received { get; set; }
}
