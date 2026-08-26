using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using SmartPOS.Core.Interfaces;
using SmartPOS.Infrastructure.Data;
using System.Net.Http.Json;

namespace SmartPOS.Infrastructure.Services;

/// <summary>
/// Background service that automatically synchronizes sales data from local SQLite DB
/// to the Cloud REST/WebSocket server for executive monitoring.
/// Operates asynchronously without blocking local cashier transactions.
/// </summary>
public class CloudSyncService : IHostedService, IDisposable
{
    private readonly IDbContextFactory<AppDbContext> _contextFactory;
    private readonly ISettingsService _settingsService;
    private readonly ILogger<CloudSyncService>? _logger;
    private readonly HttpClient _httpClient;

    private CancellationTokenSource? _cts;
    private Task? _syncTask;
    private const int DefaultIntervalSeconds = 20;

    public CloudSyncService(
        IDbContextFactory<AppDbContext> contextFactory,
        ISettingsService settingsService,
        ILogger<CloudSyncService>? logger = null)
    {
        _contextFactory = contextFactory;
        _settingsService = settingsService;
        _logger = logger;
        _httpClient = new HttpClient
        {
            Timeout = TimeSpan.FromSeconds(10)
        };
    }

    public Task StartAsync(CancellationToken cancellationToken)
    {
        _logger?.LogInformation("Cloud Sync Service starting...");
        _cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        _syncTask = RunSyncLoopAsync(_cts.Token);
        return Task.CompletedTask;
    }

    private async Task RunSyncLoopAsync(CancellationToken ct)
    {
        // Initial delay before first sync
        await Task.Delay(5000, ct);

        while (!ct.IsCancellationRequested)
        {
            try
            {
                await SyncUnsyncedSalesAsync(ct);
            }
            catch (OperationCanceledException) { break; }
            catch (Exception ex)
            {
                _logger?.LogWarning(ex, "Error in Cloud Sync loop. Will retry next cycle.");
            }

            try
            {
                await Task.Delay(TimeSpan.FromSeconds(DefaultIntervalSeconds), ct);
            }
            catch (OperationCanceledException) { break; }
        }
    }

    private async Task SyncUnsyncedSalesAsync(CancellationToken ct)
    {
        var cloudServerUrl = "http://localhost:7895/api/cloud/sync/sales";
        var storeName = _settingsService.StoreName ?? "الفرع الرئيسي";

        await using var context = await _contextFactory.CreateDbContextAsync(ct);

        // Fetch last 50 completed sales for cloud sync telemetry
        var recentSales = await context.Sales
            .AsNoTracking()
            .Include(s => s.User)
            .Include(s => s.SaleDetails)
            .Where(s => !s.IsDeleted && s.Status == Core.Entities.SaleStatus.Completed)
            .OrderByDescending(s => s.SaleDate)
            .Take(50)
            .Select(s => new
            {
                invoiceNumber = s.InvoiceNumber,
                saleDate = s.SaleDate.ToString("yyyy-MM-ddTHH:mm:ss"),
                totalAmount = s.TotalAmount,
                subtotal = s.Subtotal,
                discountAmount = s.DiscountAmount,
                paymentMethod = s.PaymentMethod.ToString(),
                cashierName = s.User != null ? s.User.FullName : "الكاشير",
                branchName = storeName,
                itemsCount = s.SaleDetails.Count
            })
            .ToListAsync(ct);

        if (recentSales.Count == 0) return;

        var payload = new
        {
            branchName = storeName,
            sales = recentSales
        };

        try
        {
            var response = await _httpClient.PostAsJsonAsync(cloudServerUrl, payload, ct);
            if (response.IsSuccessStatusCode)
            {
                _logger?.LogInformation("Cloud Sync: successfully synced {Count} sales to Cloud API.", recentSales.Count);
            }
        }
        catch (HttpRequestException)
        {
            // Offline or server not running — silent retry next interval
        }
    }

    public async Task StopAsync(CancellationToken cancellationToken)
    {
        _logger?.LogInformation("Cloud Sync Service stopping.");
        _cts?.Cancel();
        if (_syncTask != null)
        {
            try { await _syncTask.WaitAsync(TimeSpan.FromSeconds(3), cancellationToken); }
            catch { /* Ignore timeout */ }
        }
    }

    public void Dispose()
    {
        _cts?.Dispose();
        _httpClient.Dispose();
    }
}
