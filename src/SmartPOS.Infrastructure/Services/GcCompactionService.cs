using System;
using System.Runtime;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace SmartPOS.Infrastructure.Services;

/// <summary>
/// Automated background service that periodically triggers Large Object Heap (LOH) compaction
/// and full generational Garbage Collection to eliminate memory fragmentation in long-running desktop sessions.
/// Runs every 15 minutes.
/// </summary>
public class GcCompactionService : IHostedService, IDisposable
{
    private readonly ILogger<GcCompactionService>? _logger;
    private Timer? _timer;
    private static readonly TimeSpan CompactionInterval = TimeSpan.FromMinutes(15);

    public GcCompactionService(ILogger<GcCompactionService>? logger = null)
    {
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
