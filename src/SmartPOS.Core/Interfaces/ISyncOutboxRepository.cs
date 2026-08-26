using SmartPOS.Core.Entities;

namespace SmartPOS.Core.Interfaces;

public interface ISyncOutboxRepository
{
    Task<SyncOutbox> EnqueueAsync(
        string entityType, 
        string entityId, 
        string syncId, 
        OutboxOperation operation, 
        string payloadJson, 
        string? traceId = null);

    Task<List<SyncOutbox>> GetPendingBatchAsync(int batchSize = 100);
    Task MarkProcessingAsync(IEnumerable<string> ids);
    Task MarkSyncedAsync(IEnumerable<string> ids, DateTime syncedAt);
    Task MarkFailedAsync(string id, string errorReason, int maxRetries = 5);
    Task<int> PurgeSyncedAsync(TimeSpan olderThan);
    Task<List<SyncOutbox>> GetDeadLetterItemsAsync(int limit = 50);
    Task RequeueDeadLetterItemsAsync(IEnumerable<string> ids);
}
