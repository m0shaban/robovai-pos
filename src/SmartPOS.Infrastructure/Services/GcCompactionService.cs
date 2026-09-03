using System;
using System.Runtime;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using SmartPOS.Infrastructure.Data;

namespace SmartPOS.Infrastructure.Services;

/// <summary>
/// Automated background service that periodically triggers Large Object Heap (LOH) compaction,
/// full generational Garbage Collection, and SQLite WAL checkpointing + PRAGMA optimize.
/// Runs every 15 minutes.
/// </summary>
public class GcCompactionService : IHostedService, IDisposable
{
    private readonly IDbContextFactory<AppDbContext>? _contextFactory;
    private readonly ILogger<GcCompactionService>? _logger;
    private Timer? _timer;
    private static readonly TimeSpan CompactionInterval = TimeSpan.FromMinutes(15);

    public GcCompactionService(
        IDbContextFactory<AppDbContext>? contextFactory = null,
        ILogger<GcCompactionService>? logger = null)
    {
        _contextFactory = contextFactory;
        _logger = logger;
    }

    public Task StartAsync(CancellationToken cancellationToken)
    {
        _logger?.LogInformation("GcCompactionService starting. Periodic LOH compaction scheduled every {Interval} minutes.", CompactionInterval.TotalMinutes);

        // Start timer after initial 15-minute delay, repeating every 15 minutes
        _timer = new Timer(ExecuteGcCompaction, null, CompactionInterval, CompactionInterval);

        return Task.CompletedTask;
    }

    /// <summary>
    /// Manually trigger GC collection and LOH compaction on demand.
    /// </summary>
    public void TriggerCompaction()
    {
        ExecuteGcCompaction(null);
    }

    private void ExecuteGcCompaction(object? state)
    {
        try
        {
            long memoryBefore = GC.GetTotalMemory(false);

            // Instruct CLR GC to compact Large Object Heap on the next full GC
            GCSettings.LargeObjectHeapCompactionMode = GCLargeObjectHeapCompactionMode.CompactOnce;
            GC.Collect(2, GCCollectionMode.Forced, blocking: true, compacting: true);
            GC.WaitForPendingFinalizers();

            long memoryAfter = GC.GetTotalMemory(false);
            long freedBytes = memoryBefore - memoryAfter;

            _logger?.LogInformation("LOH Compaction completed. Memory before: {Before:N0} bytes, after: {After:N0} bytes (freed {Freed:N0} bytes).",
                memoryBefore, memoryAfter, freedBytes);

            // Execute periodic WAL checkpoint and query optimizer to keep DB compact and lightning-fast
            if (_contextFactory != null)
            {
                _ = Task.Run(async () =>
                {
                    try
                    {
                        await using var ctx = await _contextFactory.CreateDbContextAsync();
                        await ctx.Database.ExecuteSqlRawAsync("PRAGMA wal_checkpoint(PASSIVE);");
                        await ctx.Database.ExecuteSqlRawAsync("PRAGMA optimize;");
                    }
                    catch { /* non-critical background maintenance */ }
                });
            }
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Error occurred during background GC LOH compaction.");
        }
    }

    public Task StopAsync(CancellationToken cancellationToken)
    {
        _logger?.LogInformation("GcCompactionService stopping.");
        _timer?.Change(Timeout.Infinite, 0);
        return Task.CompletedTask;
    }

    public void Dispose()
    {
        _timer?.Dispose();
        _timer = null;
    }
}
