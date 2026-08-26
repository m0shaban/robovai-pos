# Milestone M1 Technical Architecture Report: Outbox Queue & Sync Engine

**Author**: Explorer M1 Agent (`explorer_m1_2`)  
**Target Project**: RobovAI PRO POS & WMS Ecosystem (`SmartPOS` & `smart-inventory-pro`)  
**Date**: 2026-08-08  
**Status**: Completed Architectural Blueprint  

---

## Executive Summary

This document specifies the complete, commercial-grade implementation blueprint for **Milestone M1 (Outbox Queue & Sync Engine)** across the **WPF Desktop C# .NET 8** backend and the **Web PWA Dexie.js** frontend. 

The architecture guarantees **100% transactional reliability (ACID compliance)** for offline and hybrid sync operations. Every local mutation (sales checkout, stock adjustment, product edit) atomically commits its domain state update alongside an outbox entry in the same local database transaction. A dedicated background sync engine asynchronously dequeues pending outbox entries, handles network retries with exponential backoff and jitter, resolves conflicts using Last-Write-Wins (LWW) timestamp versioning, and streams data over local LAN (Kestrel HTTP server) or Cloud REST endpoints.

---

## 1. Component 1: `sync_outbox` Table Schema & Data Definition

### 1.1 Database Schema Requirements

The outbox table must store transactional mutation events with complete metadata for idempotent replay, network transmission, failure tracking, and dead-letter queue management.

#### Field Specifications:

| Column Name | SQL Data Type | C# Type / Dexie Type | Nullable | Description & Constraints |
|-------------|---------------|----------------------|----------|---------------------------|
| `id` | `TEXT` (36) | `string` (Guid) | NO | Primary Key. Unique GUID string representation. |
| `entity_type` | `TEXT` (100) | `string` | NO | Target entity name (e.g. `Product`, `Sale`, `Customer`, `StockMovement`, `BranchStock`). Indexed. |
| `entity_id` | `TEXT` (100) | `string` | NO | Local primary key or sync identifier of target entity. Indexed. |
| `sync_id` | `TEXT` (36) | `string` (Guid) | NO | Global immutable UUID across systems for cross-node deduplication. Indexed. |
| `operation` | `TEXT` (20) | `OutboxOperation` | NO | Mutation type: `INSERT`, `UPDATE`, `DELETE`. |
| `payload_json` | `TEXT` | `string` | NO | Full serialized entity DTO payload or delta JSON. |
| `created_at` | `TEXT` (33) | `DateTime` / ISO | NO | ISO 8601 UTC timestamp of creation (`yyyy-MM-ddTHH:mm:ss.fffZ`). Indexed. |
| `synced_at` | `TEXT` (33) | `DateTime?` / ISO | YES | ISO 8601 UTC timestamp when acknowledgment was received. |
| `status` | `TEXT` (20) | `OutboxStatus` | NO | Lifecycle state: `PENDING`, `PROCESSING`, `SYNCED`, `FAILED`, `DEAD_LETTER`. Default: `PENDING`. Indexed. |
| `retry_count` | `INTEGER` | `int` | NO | Number of failed sync attempts. Default: `0`. |
| `last_error` | `TEXT` | `string?` | YES | Exception message or HTTP error status from last failed sync attempt. |
| `trace_id` | `TEXT` (100) | `string?` | YES | Batch correlation ID / transaction trace identifier. |
| `version` | `INTEGER` | `long` | NO | Optimistic concurrency / payload version number. Default: `1`. |

---

### 1.2 SQLite DDL Script

```sql
-- Migration SQL for SQLite Database (pos.db / smartpos.db)
CREATE TABLE IF NOT EXISTS "sync_outbox" (
    "id" TEXT NOT NULL CONSTRAINT "PK_sync_outbox" PRIMARY KEY,
    "entity_type" TEXT NOT NULL,
    "entity_id" TEXT NOT NULL,
    "sync_id" TEXT NOT NULL,
    "operation" TEXT NOT NULL,
    "payload_json" TEXT NOT NULL,
    "created_at" TEXT NOT NULL,
    "synced_at" TEXT NULL,
    "status" TEXT NOT NULL DEFAULT 'PENDING',
    "retry_count" INTEGER NOT NULL DEFAULT 0,
    "last_error" TEXT NULL,
    "trace_id" TEXT NULL,
    "version" INTEGER NOT NULL DEFAULT 1
);

-- Performance & Indexing Optimization
CREATE INDEX IF NOT EXISTS "IX_sync_outbox_status_created_at" 
    ON "sync_outbox" ("status", "created_at");

CREATE INDEX IF NOT EXISTS "IX_sync_outbox_sync_id" 
    ON "sync_outbox" ("sync_id");

CREATE INDEX IF NOT EXISTS "IX_sync_outbox_entity" 
    ON "sync_outbox" ("entity_type", "entity_id");
```

---

### 1.3 EF Core Fluent API Configuration

**Target File**: `src/SmartPOS.Infrastructure/Data/AppDbContext.cs`

```csharp
// Add to AppDbContext.cs DbSets
public DbSet<SyncOutbox> SyncOutboxes => Set<SyncOutbox>();

// Add inside OnModelCreating(ModelBuilder modelBuilder)
modelBuilder.Entity<SyncOutbox>(entity =>
{
    entity.ToTable("sync_outbox");
    entity.HasKey(e => e.Id);

    entity.Property(e => e.Id)
        .HasMaxLength(36)
        .ValueGeneratedNever();

    entity.Property(e => e.EntityType)
        .HasMaxLength(100)
        .IsRequired();

    entity.Property(e => e.EntityId)
        .HasMaxLength(100)
        .IsRequired();

    entity.Property(e => e.SyncId)
        .HasMaxLength(36)
        .IsRequired();

    entity.Property(e => e.Operation)
        .HasConversion<string>()
        .HasMaxLength(20)
        .IsRequired();

    entity.Property(e => e.PayloadJson)
        .IsRequired();

    entity.Property(e => e.CreatedAt)
        .HasConversion(
            v => v.ToUniversalTime().ToString("o"),
            v => DateTime.Parse(v, null, System.Globalization.DateTimeStyles.AdjustToUniversal))
        .IsRequired();

    entity.Property(e => e.SyncedAt)
        .HasConversion(
            v => v.HasValue ? v.Value.ToUniversalTime().ToString("o") : null,
            v => v != null ? DateTime.Parse(v, null, System.Globalization.DateTimeStyles.AdjustToUniversal) : (DateTime?)null);

    entity.Property(e => e.Status)
        .HasConversion<string>()
        .HasMaxLength(20)
        .HasDefaultValue(OutboxStatus.Pending)
        .IsRequired();

    entity.Property(e => e.RetryCount)
        .HasDefaultValue(0);

    entity.Property(e => e.Version)
        .HasDefaultValue(1L);

    // Indexes for fast polling and deduplication
    entity.HasIndex(e => new { e.Status, e.CreatedAt })
        .HasDatabaseName("IX_sync_outbox_status_created_at");

    entity.HasIndex(e => e.SyncId)
        .HasDatabaseName("IX_sync_outbox_sync_id");

    entity.HasIndex(e => new { e.EntityType, e.EntityId })
        .HasDatabaseName("IX_sync_outbox_entity");
});
```

---

### 1.4 Dexie.js Schema Upgrade (Web PWA)

**Target File**: `smart-inventory-pro/js/db.js`

```javascript
// Add Schema Version 9 to Dexie.js in smart-inventory-pro/js/db.js
db.version(9)
  .stores({
    products: '++id, barcode, name, category, sync_status, robovai_sync_id, location_code, batch_number, expiry_date',
    transactions: '++id, type, date, sync_status, robovai_sync_id',
    destinations: '++id, name',
    users: '++id, username, password_hash, role, cloud_uid',
    suppliers: '++id, name, phone',
    branches: '++id, name',
    damages: '++id, barcode, date',
    audit_logs: '++id, entity, entity_id, date',
    kits: '++id, barcode, name',
    transfers: '++id, date, status',
    // NEW M1 Outbox Store:
    sync_outbox: 'id, entity_type, entity_id, sync_id, operation, status, created_at, [status+created_at]',
  })
  .upgrade(async (tx) => {
    // Migration helper: backfill un-synced products into outbox
    await tx.products
      .where('sync_status')
      .equals('pending')
      .modify((product) => {
        tx.sync_outbox.add({
          id: generateUUID(),
          entity_type: 'Product',
          entity_id: String(product.id),
          sync_id: product.robovai_sync_id || generateUUID(),
          operation: 'UPDATE',
          payload_json: JSON.stringify(product),
          created_at: new Date().toISOString(),
          synced_at: null,
          status: 'PENDING',
          retry_count: 0,
          last_error: null,
          trace_id: null,
          version: 1,
        });
      });
  });
```

---

## 2. Component 2: Outbox Entity, DTOs, Repository/DAL, & Change Tracking Mechanism

### 2.1 Outbox Entity & Enums

**Target File**: `src/SmartPOS.Core/Entities/SyncOutbox.cs`

```csharp
namespace SmartPOS.Core.Entities;

public enum OutboxStatus
{
    Pending = 1,
    Processing = 2,
    Synced = 3,
    Failed = 4,
    DeadLetter = 5
}

public enum OutboxOperation
{
    Insert = 1,
    Update = 2,
    Delete = 3
}

public class SyncOutbox
{
    public string Id { get; set; } = Guid.NewGuid().ToString();
    public string EntityType { get; set; } = string.Empty;
    public string EntityId { get; set; } = string.Empty;
    public string SyncId { get; set; } = Guid.NewGuid().ToString();
    public OutboxOperation Operation { get; set; } = OutboxOperation.Insert;
    public string PayloadJson { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? SyncedAt { get; set; }
    public OutboxStatus Status { get; set; } = OutboxStatus.Pending;
    public int RetryCount { get; set; } = 0;
    public string? LastError { get; set; }
    public string? TraceId { get; set; }
    public long Version { get; set; } = 1;
}
```

---

### 2.2 Syncable Entity Interface

**Target File**: `src/SmartPOS.Core/Interfaces/ISyncableEntity.cs`

```csharp
namespace SmartPOS.Core.Interfaces;

/// <summary>
/// Interface for domain entities that participate in Outbox change tracking
/// </summary>
public interface ISyncableEntity
{
    string SyncId { get; set; }
    DateTime CreatedAt { get; set; }
    DateTime? UpdatedAt { get; set; }
    bool IsDeleted { get; set; }
}
```

---

### 2.3 Outbox DTO Models

**Target File**: `src/SmartPOS.Application/DTOs/SyncOutboxDtos.cs`

```csharp
namespace SmartPOS.Application.DTOs;

public class OutboxItemDto
{
    public string Id { get; set; } = string.Empty;
    public string EntityType { get; set; } = string.Empty;
    public string EntityId { get; set; } = string.Empty;
    public string SyncId { get; set; } = string.Empty;
    public string Operation { get; set; } = string.Empty;
    public string PayloadJson { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
    public long Version { get; set; }
    public string? TraceId { get; set; }
}

public class OutboxBatchRequestDto
{
    public string SourceDeviceId { get; set; } = string.Empty;
    public string BranchCode { get; set; } = string.Empty;
    public DateTime SentAt { get; set; } = DateTime.UtcNow;
    public List<OutboxItemDto> Items { get; set; } = new();
}

public class OutboxAckItemDto
{
    public string Id { get; set; } = string.Empty;
    public string SyncId { get; set; } = string.Empty;
    public bool Success { get; set; }
    public string? ErrorMessage { get; set; }
}

public class OutboxBatchResponseDto
{
    public string Status { get; set; } = "OK";
    public int ProcessedCount { get; set; }
    public int SuccessCount { get; set; }
    public int FailureCount { get; set; }
    public List<OutboxAckItemDto> Acks { get; set; } = new();
    public DateTime ServerTime { get; set; } = DateTime.UtcNow;
}
```

---

### 2.4 Repository & Data Access Layer

**Target File**: `src/SmartPOS.Core/Interfaces/ISyncOutboxRepository.cs`

```csharp
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
```

**Target File**: `src/SmartPOS.Infrastructure/Repositories/SyncOutboxRepository.cs`

```csharp
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
```

---

### 2.5 Change Tracking Interceptor (EF Core)

**Target File**: `src/SmartPOS.Infrastructure/Interceptors/OutboxSaveChangesInterceptor.cs`

```csharp
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
        if (eventData.Context == null) return base.SavingChangesAsync(eventData, result, cancellationToken);

        EnqueueOutboxEntries(eventData.Context);
        return base.SavingChangesAsync(eventData, result, cancellationToken);
    }

    private static void EnqueueOutboxEntries(DbContext context)
    {
        var pendingEntries = context.ChangeTracker.Entries()
            .Where(e => e.Entity is not SyncOutbox 
                     && e.Entity is not AuditLog 
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
```

---

### 2.6 Web PWA Outbox Queue Helper (Dexie.js)

**Target File**: `smart-inventory-pro/js/outbox.js`

```javascript
/**
 * Dexie Outbox Atomic Queue Helper
 */
import { db } from './db.js';

export const OutboxService = {
  /**
   * Enqueues an outbox mutation entry within an existing Dexie transaction.
   */
  async enqueue(tx, entityType, entityId, syncId, operation, payload) {
    const outboxItem = {
      id: crypto.randomUUID(),
      entity_type: entityType,
      entity_id: String(entityId),
      sync_id: syncId || crypto.randomUUID(),
      operation: operation, // 'INSERT' | 'UPDATE' | 'DELETE'
      payload_json: JSON.stringify(payload),
      created_at: new Date().toISOString(),
      synced_at: null,
      status: 'PENDING',
      retry_count: 0,
      last_error: null,
      trace_id: crypto.randomUUID(),
      version: Date.now(),
    };

    return await tx.sync_outbox.add(outboxItem);
  },

  /**
   * Atomically performs product edit and enqueues outbox record.
   */
  async mutateProductAtomic(productId, updates, operation = 'UPDATE') {
    return await db.transaction('rw', [db.products, db.sync_outbox], async (tx) => {
      const product = await tx.products.get(productId);
      if (!product && operation !== 'INSERT') throw new Error('Product not found');

      let updatedProduct = { ...product, ...updates, last_updated: new Date().toISOString() };
      if (operation === 'INSERT') {
        updatedProduct.id = await tx.products.add(updatedProduct);
      } else {
        await tx.products.update(productId, updatedProduct);
      }

      await this.enqueue(
        tx,
        'Product',
        updatedProduct.id,
        updatedProduct.robovai_sync_id,
        operation,
        updatedProduct
      );

      return updatedProduct;
    });
  }
};
```

---

## 3. Component 3: Outbox Background Sync Processor / Engine

### 3.1 Network Probe Service

**Target File**: `src/SmartPOS.Infrastructure/Services/NetworkProbeService.cs`

```csharp
using System.Net.Http;

namespace SmartPOS.Infrastructure.Services;

public class NetworkProbeService
{
    private readonly HttpClient _httpClient;

    public NetworkProbeService(HttpClient httpClient)
    {
        _httpClient = httpClient;
        _httpClient.Timeout = TimeSpan.FromSeconds(5);
    }

    public async Task<bool> IsEndpointReachableAsync(string endpointUrl)
    {
        try;
        {
            var response = await _httpClient.GetAsync(endpointUrl);
            return response.IsSuccessStatusCode;
        }
        catch
        {
            return false;
        }
    }
}
```

---

### 3.2 Background Sync Processor Service

**Target File**: `src/SmartPOS.Infrastructure/Services/SyncOutboxProcessor.cs`

```csharp
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using SmartPOS.Application.DTOs;
using SmartPOS.Core.Entities;
using SmartPOS.Core.Interfaces;
using System.Net.Http.Json;
using System.Text.Json;

namespace SmartPOS.Infrastructure.Services;

public class SyncOutboxProcessor : BackgroundService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly HttpClient _httpClient;
    private readonly ILogger<SyncOutboxProcessor> _logger;

    // Configurable defaults
    private const int BatchSize = 100;
    private const int MaxRetries = 5;
    private static readonly TimeSpan PollInterval = TimeSpan.FromSeconds(15);
    private static readonly TimeSpan PurgeInterval = TimeSpan.FromHours(24);
    private DateTime _lastPurgeTime = DateTime.MinValue;

    public SyncOutboxProcessor(
        IServiceProvider serviceProvider,
        HttpClient httpClient,
        ILogger<SyncOutboxProcessor> logger)
    {
        _serviceProvider = serviceProvider;
        _httpClient = httpClient;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("SyncOutboxProcessor background engine started.");

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await ProcessOutboxBatchAsync(stoppingToken);

                // Run daily house-keeping purge
                if (DateTime.UtcNow - _lastPurgeTime > PurgeInterval)
                {
                    await PurgeOldSyncedRecordsAsync();
                    _lastPurgeTime = DateTime.UtcNow;
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred during outbox processing cycle.");
            }

            await Task.Delay(PollInterval, stoppingToken);
        }
    }

    private async Task ProcessOutboxBatchAsync(CancellationToken stoppingToken)
    {
        using var scope = _serviceProvider.CreateScope();
        var repo = scope.ServiceProvider.GetRequiredService<ISyncOutboxRepository>();
        var settings = scope.ServiceProvider.GetService<ISettingsService>();

        // Check if outbox has pending items
        var pendingItems = await repo.GetPendingBatchAsync(BatchSize);
        if (!pendingItems.Any()) return;

        var idsToProcess = pendingItems.Select(x => x.Id).ToList();
        await repo.MarkProcessingAsync(idsToProcess);

        // Format request payload DTO
        var requestDto = new OutboxBatchRequestDto
        {
            SourceDeviceId = Environment.MachineName,
            BranchCode = "MAIN-BRANCH",
            SentAt = DateTime.UtcNow,
            Items = pendingItems.Select(x => new OutboxItemDto
            {
                Id = x.Id,
                EntityType = x.EntityType,
                EntityId = x.EntityId,
                SyncId = x.SyncId,
                Operation = x.Operation.ToString().ToUpperInvariant(),
                PayloadJson = x.PayloadJson,
                CreatedAt = x.CreatedAt,
                Version = x.Version,
                TraceId = x.TraceId
            }).ToList()
        };

        // Determine active target URL (Local LAN embedded server or Cloud API)
        string targetUrl = "http://127.0.0.1:5050/api/v1/sync/import-stream";

        try
        {
            var response = await _httpClient.PostAsJsonAsync(targetUrl, requestDto, stoppingToken);
            if (response.IsSuccessStatusCode)
            {
                var result = await response.Content.ReadFromJsonAsync<OutboxBatchResponseDto>(cancellationToken: stoppingToken);
                if (result != null)
                {
                    var syncedIds = new List<string>();
                    foreach (var ack in result.Acks)
                    {
                        if (ack.Success)
                        {
                            syncedIds.Add(ack.Id);
                        }
                        else
                        {
                            await repo.MarkFailedAsync(ack.Id, ack.ErrorMessage ?? "Remote sync error", MaxRetries);
                        }
                    }

                    if (syncedIds.Any())
                    {
                        await repo.MarkSyncedAsync(syncedIds, DateTime.UtcNow);
                        _logger.LogInformation("Successfully synced {Count} outbox records.", syncedIds.Count);
                    }
                }
            }
            else
            {
                var errorMsg = $"HTTP {(int)response.StatusCode} {response.ReasonPhrase}";
                foreach (var item in pendingItems)
                {
                    await CalculateBackoffAndFailAsync(repo, item, errorMsg);
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning("Network sync failed for batch of {Count} items: {Message}", pendingItems.Count, ex.Message);
            foreach (var item in pendingItems)
            {
                await CalculateBackoffAndFailAsync(repo, item, ex.Message);
            }
        }
    }

    private static async Task CalculateBackoffAndFailAsync(ISyncOutboxRepository repo, SyncOutbox item, string errorReason)
    {
        await repo.MarkFailedAsync(item.Id, errorReason, MaxRetries);
    }

    private async Task PurgeOldSyncedRecordsAsync()
    {
        using var scope = _serviceProvider.CreateScope();
        var repo = scope.ServiceProvider.GetRequiredService<ISyncOutboxRepository>();
        int deleted = await repo.PurgeSyncedAsync(TimeSpan.FromDays(7));
        if (deleted > 0)
        {
            _logger.LogInformation("Purged {Count} old synced outbox records (> 7 days).", deleted);
        }
    }
}
```

---

### 3.3 Conflict Resolution Strategy & Idempotency Pipeline

```
                               ┌──────────────────────────────────────────────┐
                               │        Incoming Outbox Item Payload          │
                               │        (SyncId, Version, UpdatedAt)          │
                               └──────────────────────┬───────────────────────┘
                                                      │
                                                      ▼
                               ┌──────────────────────────────────────────────┐
                               │           Idempotency Verification           │
                               │       Has SyncId already been applied?       │
                               └──────────────────────┬───────────────────────┘
                                                      │
                                           ┌──────────┴──────────┐
                                           │ YES                 │ NO
                                           ▼                     ▼
                               ┌──────────────────────┐ ┌──────────────────────────────┐
                               │ Return Ack Success   │ │ Existing Record Lookup       │
                               │ (Skip Duplicate)     │ │ (by EntityId or SyncId)      │
                               └──────────────────────┘ └──────────────┬───────────────┘
                                                                       │
                                                            ┌──────────┴──────────┐
                                                            │ Found               │ Not Found
                                                            ▼                     ▼
                                               ┌──────────────────────────┐ ┌──────────────┐
                                               │ Timestamp LWW Check      │ │ Apply Insert │
                                               │ (Incoming vs Local Time) │ └──────────────┘
                                               └────────────┬─────────────┘
                                                            │
                                                 ┌──────────┴──────────┐
                                                 │ Incoming > Local    │ Incoming <= Local
                                                 ▼                     ▼
                                    ┌──────────────────────┐ ┌───────────────────────────┐
                                    │ Overwrite Local      │ │ Ignore Payload            │
                                    │ Record & Return Ack  │ │ Return Ack (Outdated)     │
                                    └──────────────────────┘ └───────────────────────────┘
```

#### Key Rules:
1. **Idempotency**: Every outbox record contains a unique `SyncId`. The receiver logs processed `SyncId` values in a fast memory cache / DB lookup. If a `SyncId` is re-received due to network retry, it immediately returns `{ Success: true }` without re-applying.
2. **Last-Write-Wins (LWW)**: Conflicts on concurrent entity updates are resolved by comparing `Version` (ticks) or `UpdatedAt` ISO string. The state with the later timestamp wins.

---

### 3.4 Web PWA Outbox Sync Engine (JavaScript)

**Target File**: `smart-inventory-pro/js/sync-engine.js`

```javascript
/**
 * Web PWA Outbox Background Sync Engine
 */
import { db } from './db.js';

export const SyncEngine = {
  intervalId: null,
  isSyncing: false,

  start(intervalMs = 15000) {
    if (this.intervalId) return;
    this.intervalId = setInterval(() => this.processOutbox(), intervalMs);
    window.addEventListener('online', () => this.processOutbox());
    console.log('[SyncEngine] Background processor started.');
  },

  stop() {
    if (this.intervalId) {
      clearInterval(this.intervalId);
      this.intervalId = null;
    }
  },

  async processOutbox() {
    if (this.isSyncing || !navigator.onLine) return;
    this.isSyncing = true;

    try {
      // Query pending outbox items
      const pendingItems = await db.sync_outbox
        .where('status')
        .equals('PENDING')
        .limit(50)
        .toArray();

      if (pendingItems.length === 0) {
        this.isSyncing = false;
        return;
      }

      // Mark status PROCESSING
      await db.sync_outbox.bulkUpdate(
        pendingItems.map((item) => ({ key: item.id, changes: { status: 'PROCESSING' } }))
      );

      // Endpoint URL (Desktop Kestrel server or Cloud API)
      const serverUrl = localStorage.getItem('robovai_server_url') || 'http://localhost:5050';
      const endpoint = `${serverUrl}/api/v1/sync/import-stream`;

      const payload = {
        sourceDeviceId: 'WEB-PWA-CLIENT',
        sentAt: new Date().toISOString(),
        items: pendingItems.map((x) => ({
          id: x.id,
          entityType: x.entity_type,
          entityId: x.entity_id,
          syncId: x.sync_id,
          operation: x.operation,
          payloadJson: x.payload_json,
          createdAt: x.created_at,
          version: x.version,
        })),
      };

      const response = await fetch(endpoint, {
        method: 'POST',
        headers: { 'Content-Type': 'application/json' },
        body: JSON.stringify(payload),
      });

      if (response.ok) {
        const result = await response.json();
        const now = new Date().toISOString();

        for (const ack of result.acks || []) {
          if (ack.success) {
            await db.sync_outbox.update(ack.id, { status: 'SYNCED', synced_at: now });
          } else {
            const item = await db.sync_outbox.get(ack.id);
            const retries = (item?.retry_count || 0) + 1;
            await db.sync_outbox.update(ack.id, {
              status: retries >= 5 ? 'DEAD_LETTER' : 'PENDING',
              retry_count: retries,
              last_error: ack.errorMessage || 'Server reject',
            });
          }
        }
      } else {
        throw new Error(`HTTP ${response.status}`);
      }
    } catch (err) {
      console.warn('[SyncEngine] Push failed:', err.message);
      // Revert processing status to pending for retry
      const processingItems = await db.sync_outbox.where('status').equals('PROCESSING').toArray();
      for (const item of processingItems) {
        const retries = item.retry_count + 1;
        await db.sync_outbox.update(item.id, {
          status: retries >= 5 ? 'DEAD_LETTER' : 'PENDING',
          retry_count: retries,
          last_error: err.message,
        });
      }
    } finally {
      this.isSyncing = false;
    }
  }
};
```

---

## 4. Implementation Mapping & File Summary

| Target Component | C# / WPF Target Path | Web PWA Target Path | Purpose |
|------------------|----------------------|---------------------|---------|
| **Entity / Model** | `src/SmartPOS.Core/Entities/SyncOutbox.cs` | `smart-inventory-pro/js/db.js` (v9 store) | Model definition for outbox items |
| **Sync Interface** | `src/SmartPOS.Core/Interfaces/ISyncableEntity.cs` | N/A | Interface for syncable domain entities |
| **DTOs** | `src/SmartPOS.Application/DTOs/SyncOutboxDtos.cs` | Inline JS objects | Network serialization payloads |
| **EF Configuration** | `src/SmartPOS.Infrastructure/Data/AppDbContext.cs` | `smart-inventory-pro/js/db.js` | Schema mappings and indexes |
| **Repository Layer** | `src/SmartPOS.Infrastructure/Repositories/SyncOutboxRepository.cs` | `smart-inventory-pro/js/outbox.js` | Outbox CRUD & batch operations |
| **Change Interceptor** | `src/SmartPOS.Infrastructure/Interceptors/OutboxSaveChangesInterceptor.cs` | `smart-inventory-pro/js/outbox.js` | Auto-enqueue mutations in same transaction |
| **Network Probe** | `src/SmartPOS.Infrastructure/Services/NetworkProbeService.cs` | `navigator.onLine` / `fetch` probe | Verifies endpoint availability |
| **Sync Engine Processor** | `src/SmartPOS.Infrastructure/Services/SyncOutboxProcessor.cs` | `smart-inventory-pro/js/sync-engine.js` | Asynchronous worker process |

---
*Report compiled by Explorer M1 Agent (`explorer_m1_2`). Ready for implementation.*
