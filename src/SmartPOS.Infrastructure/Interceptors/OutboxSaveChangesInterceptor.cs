using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using SmartPOS.Core.Entities;
using SmartPOS.Core.Interfaces;
using System.Text.Json;

namespace SmartPOS.Infrastructure.Interceptors;

public class OutboxSaveChangesInterceptor : SaveChangesInterceptor
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = false,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull
    };

    public override ValueTask<InterceptionResult<int>> SavingChangesAsync(
        DbContextEventData eventData,
        InterceptionResult<int> result,
        CancellationToken cancellationToken = default)
    {
        if (eventData.Context != null)
        {
            EnqueueOutboxEntries(eventData.Context);
        }
        return base.SavingChangesAsync(eventData, result, cancellationToken);
    }

    public override InterceptionResult<int> SavingChanges(
        DbContextEventData eventData,
        InterceptionResult<int> result)
    {
        if (eventData.Context != null)
        {
            EnqueueOutboxEntries(eventData.Context);
        }
        return base.SavingChanges(eventData, result);
    }

    private static void EnqueueOutboxEntries(DbContext context)
    {
        var pendingEntries = context.ChangeTracker.Entries()
            .Where(e => e.Entity is not SyncOutbox 
                     && e.Entity is not AuditLog 
                     && e.Entity is not AppSetting
                     && e.State is EntityState.Added or EntityState.Modified or EntityState.Deleted)
            .ToList();

        if (!pendingEntries.Any()) return;

        var traceId = Guid.NewGuid().ToString("N");

        foreach (var entry in pendingEntries)
        {
            var entityType = entry.Entity.GetType().Name;
            var operation = entry.State switch
            {
                EntityState.Added => OutboxOperation.Insert,
                EntityState.Modified => OutboxOperation.Update,
                EntityState.Deleted => OutboxOperation.Delete,
                _ => OutboxOperation.Insert
            };

            // Retrieve entity ID and SyncId
            string entityId = "0";
            string syncId = Guid.NewGuid().ToString();

            if (entry.Entity is BaseEntity baseEntity && baseEntity.Id != 0)
            {
                entityId = baseEntity.Id.ToString();
            }

            if (entry.Entity is ISyncableEntity syncableEntity)
            {
                if (string.IsNullOrEmpty(syncableEntity.SyncId))
                {
                    syncableEntity.SyncId = syncId;
                }
                else
                {
                    syncId = syncableEntity.SyncId;
                }
            }

            // Serialize entity state
            var payloadJson = JsonSerializer.Serialize(entry.Entity, entry.Entity.GetType(), JsonOptions);

            var outboxEntry = new SyncOutbox
            {
                Id = Guid.NewGuid().ToString(),
                EntityType = entityType,
                EntityId = entityId,
                SyncId = syncId,
                Operation = operation,
                PayloadJson = payloadJson,
                CreatedAt = DateTime.UtcNow,
                Status = OutboxStatus.Pending,
                RetryCount = 0,
                TraceId = traceId,
                Version = DateTime.UtcNow.Ticks
            };

            context.Set<SyncOutbox>().Add(outboxEntry);
        }
    }
}
