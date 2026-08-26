using Microsoft.EntityFrameworkCore;
using SmartPOS.Core.Entities;
using SmartPOS.Core.Interfaces;
using SmartPOS.Infrastructure.Data;

namespace SmartPOS.Infrastructure.Repositories;

public class SyncOutboxRepository : ISyncOutboxRepository
{
    private readonly AppDbContext _context;

    public SyncOutboxRepository(AppDbContext context)
    {
        _context = context;
    }

    public async Task<SyncOutbox> EnqueueAsync(
        string entityType, 
        string entityId, 
        string syncId, 
        OutboxOperation operation, 
        string payloadJson, 
        string? traceId = null)
    {
        var entry = new SyncOutbox
        {
            Id = Guid.NewGuid().ToString(),
            EntityType = entityType,
            EntityId = entityId,
            SyncId = string.IsNullOrEmpty(syncId) ? Guid.NewGuid().ToString() : syncId,
            Operation = operation,
            PayloadJson = payloadJson,
            CreatedAt = DateTime.UtcNow,
            Status = OutboxStatus.Pending,
            RetryCount = 0,
            TraceId = traceId,
            Version = 1
        };

        await _context.SyncOutboxes.AddAsync(entry);
        return entry;
    }

    public async Task<List<SyncOutbox>> GetPendingBatchAsync(int batchSize = 100)
    {
        return await _context.SyncOutboxes
            .Where(x => x.Status == OutboxStatus.Pending)
            .OrderBy(x => x.CreatedAt)
            .Take(batchSize)
            .ToListAsync();
    }

    public async Task MarkProcessingAsync(IEnumerable<string> ids)
    {
        var idList = ids.ToList();
        if (!idList.Any()) return;

        await _context.SyncOutboxes
            .Where(x => idList.Contains(x.Id))
            .ExecuteUpdateAsync(s => s.SetProperty(b => b.Status, OutboxStatus.Processing));
    }

    public async Task MarkSyncedAsync(IEnumerable<string> ids, DateTime syncedAt)
    {
        var idList = ids.ToList();
        if (!idList.Any()) return;

        await _context.SyncOutboxes
            .Where(x => idList.Contains(x.Id))
            .ExecuteUpdateAsync(s => s
                .SetProperty(b => b.Status, OutboxStatus.Synced)
                .SetProperty(b => b.SyncedAt, syncedAt));
    }

    public async Task MarkFailedAsync(string id, string errorReason, int maxRetries = 5)
    {
        var item = await _context.SyncOutboxes.FirstOrDefaultAsync(x => x.Id == id);
        if (item == null) return;

        item.RetryCount += 1;
        item.LastError = errorReason;

        if (item.RetryCount >= maxRetries)
        {
            item.Status = OutboxStatus.DeadLetter;
        }
        else
        {
            item.Status = OutboxStatus.Pending; // Available for next retry pass
        }

        await _context.SaveChangesAsync();
    }

    public async Task<int> PurgeSyncedAsync(TimeSpan olderThan)
    {
        var cutoff = DateTime.UtcNow.Subtract(olderThan);
        return await _context.SyncOutboxes
            .Where(x => x.Status == OutboxStatus.Synced && x.SyncedAt.HasValue && x.SyncedAt.Value < cutoff)
            .ExecuteDeleteAsync();
    }

    public async Task<List<SyncOutbox>> GetDeadLetterItemsAsync(int limit = 50)
    {
        return await _context.SyncOutboxes
            .Where(x => x.Status == OutboxStatus.DeadLetter)
            .OrderByDescending(x => x.CreatedAt)
            .Take(limit)
            .ToListAsync();
    }

    public async Task RequeueDeadLetterItemsAsync(IEnumerable<string> ids)
    {
        var idList = ids.ToList();
        if (!idList.Any()) return;

        await _context.SyncOutboxes
            .Where(x => idList.Contains(x.Id))
            .ExecuteUpdateAsync(s => s
                .SetProperty(b => b.Status, OutboxStatus.Pending)
                .SetProperty(b => b.RetryCount, 0)
                .SetProperty(b => b.LastError, (string?)null));
    }
}
