# Handoff Report: Milestone M1 Outbox Queue & Sync Engine Architecture

**Author**: Explorer M1 Agent (`explorer_m1_2`)  
**Working Directory**: `f:\Raw\kasher\kasher\.agents\explorer_m1_2`  
**Target Recipient**: Implementer Agent / Lead Architect  
**Date**: 2026-08-08  

---

## 1. Observation

- **DbContext Analysis**: `src/SmartPOS.Infrastructure/Data/AppDbContext.cs:16-37` lists all current EF Core `DbSet` properties (`Products`, `Categories`, `Sales`, `Expenses`, `Shifts`, `AuditLogs`, `RentalDevices`, etc.). It currently lacks a `SyncOutbox` entity or `DbSet<SyncOutbox>` table registration.
- **Base Entity**: `src/SmartPOS.Core/Entities/BaseEntity.cs:6-12` defines `Id` (int auto-increment), `CreatedAt`, `UpdatedAt`, `IsDeleted`. Domain entities inherit from `BaseEntity`. Some entities currently lack a dedicated `SyncId` (global UUID string) property for cross-node mapping.
- **Repository Pattern**: `src/SmartPOS.Infrastructure/Repositories/Repository.cs:12-74` and `UnitOfWork.cs:11-73` implement generic repository and transaction management over `AppDbContext`.
- **Web PWA Schema**: `smart-inventory-pro/js/db.js:81-105` defines Dexie.js version 8 schema containing `products`, `transactions`, `destinations`, `users`, `suppliers`, `branches`, `damages`, `audit_logs`, `kits`, `transfers`. It includes `sync_status` ('pending'/'synced') and `robovai_sync_id` on products and transactions, but lacks a dedicated, indexed `sync_outbox` store.
- **Network / API Endpoints**: Existing system has `WmsQrBridgeViewModel.cs` for manual QR code optical scanning. There is currently no active background outbox sync worker or EF Core change tracking interceptor.

---

## 2. Logic Chain

1. **Transactional Reliability Requirement**: In a hybrid online/offline POS system (Requirement R1 & M1), domain entity modifications (sales checkout, stock updates) must never be lost if the app or network crashes immediately after an operation.
2. **Outbox Pattern Solution**: To achieve 100% transactional consistency (ACID), outbox records (`sync_outbox`) must be created inside the exact same database transaction as the entity mutation.
3. **Change Tracking Mechanism**: By implementing an EF Core `SaveChangesInterceptor` (`OutboxSaveChangesInterceptor`) in C# and an `OutboxService` transaction wrapper in Dexie.js, entity additions, modifications, and deletions are automatically intercepted and enqueued into `sync_outbox` before `SaveChangesAsync()` or `db.transaction()` commits.
4. **Asynchronous Decoupling**: A background worker (`SyncOutboxProcessor` in C# .NET 8 / `SyncEngine` in JS) periodically polls `sync_outbox` for `PENDING` records, batches them into NDJSON / HTTP JSON DTO payloads (`OutboxBatchRequestDto`), and pushes them to the target server (`http://<lan_ip>:5050/api/v1/sync/import-stream` or Cloud API).
5. **Resilience & Conflict Handling**: If network transmission fails, retry logic increments `RetryCount` with exponential backoff and jitter. If retries exceed 5, status transitions to `DEAD_LETTER` for admin visibility. Idempotency is enforced using `SyncId` global GUIDs, and conflicts are resolved using Last-Write-Wins (LWW) timestamp versioning.

---

## 3. Caveats

- **Existing Data Migration**: Historical records in `Product` or `Sale` created prior to M1 deployment will not have outbox entries unless a data seeding/migration script is executed (provided in `analysis.md` section 1.4 Dexie upgrade script and EF Core initializer).
- **SQLite Concurrency**: To ensure smooth concurrent writes between WPF UI transactions and `SyncOutboxProcessor` state updates (`PENDING` -> `PROCESSING` -> `SYNCED`), SQLite Write-Ahead Logging (`PRAGMA journal_mode=WAL;`) and `BusyTimeout = 30000ms` (Milestone M3 requirement) should be enabled on `AppDbContext`.

---

## 4. Conclusion

The technical specification and architecture for Milestone M1 (Outbox Queue & Sync Engine) is fully defined and ready for implementation. Concrete implementation details, file structures, schemas, C# code structures, and JavaScript code structures are documented in `f:\Raw\kasher\kasher\.agents\explorer_m1_2\analysis.md`.

Key deliverables designed:
1. `sync_outbox` schema & indexes for SQLite / EF Core and Dexie.js (Version 9).
2. `SyncOutbox` entity, `OutboxStatus` & `OutboxOperation` enums, `ISyncableEntity` interface, and `SyncOutboxDtos`.
3. `ISyncOutboxRepository` interface & `SyncOutboxRepository` implementation.
4. `OutboxSaveChangesInterceptor` for EF Core change tracking & atomic transaction enqueueing.
5. `SyncOutboxProcessor` background service (C#) & `SyncEngine` processor (JS) with network probing, batching, exponential backoff retries, LWW conflict resolution, and daily housekeeping purging.

---

## 5. Verification Method

To verify the implementation once completed:
1. **Compilation & Build**: Run `dotnet build` in `src/SmartPOS.Infrastructure` and `src/SmartPOS.WPF`.
2. **EF Core Migration**: Execute `dotnet ef migrations add AddSyncOutboxTable --project src/SmartPOS.Infrastructure` and verify generated migration SQL matches section 1.2 in `analysis.md`.
3. **Unit / Integration Testing**:
   - Create a test `Product` entity via `AppDbContext`, call `SaveChangesAsync()`, and assert that a corresponding `SyncOutbox` row with `Status == Pending` and matching `PayloadJson` exists in `sync_outbox`.
   - Run `SyncOutboxProcessor` processing loop against a mock HTTP endpoint, verify status transitions to `SYNCED` and `SyncedAt` is populated.
4. **Dexie.js Web PWA**: Load `smart-inventory-pro`, verify Dexie database opens with version 9, edit a product, and check DevTools IndexedDB `sync_outbox` table for new pending item.
