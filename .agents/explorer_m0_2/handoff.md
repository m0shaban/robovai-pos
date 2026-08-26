# Handoff Report: API & Data Model Contract Mapping (M0 E2E Testing Track)

**Agent ID**: explorer_m0_2  
**Role**: Teamwork Explorer (API & Schema Contract Explorer for M0)  
**Working Directory**: `f:\Raw\kasher\kasher\.agents\explorer_m0_2`  
**Date**: 2026-08-08  

---

## 1. Observation

1. **Project Baseline & Specs**:
   - `PROJECT.md` defines 18 features (R1-R5) spanning dynamic sync configuration, `sync_outbox` transactional queue, embedded Kestrel server on port 5050, Jetpack Compose Material 3 UI theme, Dexie.js v9 IndexedDB, SQLite WAL/timeout settings, `fast-pair-v2` QR protocol, NDJSON streaming, and multi-branch admin control.
   - Codebase analysis confirmed WPF EF Core context in `src/SmartPOS.Infrastructure/Data/AppDbContext.cs`, QR bridge in `src/SmartPOS.Application/ViewModels/WmsQrBridgeViewModel.cs`, and Web PWA IndexedDB schema in `smart-inventory-pro/js/db.js` and `smart-inventory-pro/js/qr-sync.js`.

2. **Interface Contracts Mapped**:
   - `POST /api/v1/pair/handshake`: Header `Authorization: Bearer <signed_token>`, Body `{ deviceId, deviceName, deviceType, appVersion }`, Returns `{ status, sessionToken, serverTime, expiresAt }`.
   - `GET /api/v1/sync/export-stream`: Query `entity`, `since`, Headers `Authorization: Bearer <sessionToken>`, Content-Type `application/x-ndjson`.
   - `POST /api/v1/sync/import-stream`: Content-Type `application/x-ndjson`, Returns `{ status, importedCount, failedCount, serverTime }`.
   - `POST /api/v1/devices/heartbeat`: Body `{ deviceId, deviceName, type, branchId, appVersion }`, Returns `{ acknowledged, serverTime, configVersion }`.

3. **Database Schemas Mapped**:
   - `sync_outbox`: `id` (GUID), `entity_type`, `entity_id`, `operation`, `payload_json`, `created_at`, `synced_at`, `status`, `retry_count`.
   - `Branch`, `BranchStock`, `StockTransfer`, `ConnectedDevice` SQLite tables & Dexie v9 IndexedDB stores (`history_logs`, `app_prefs`, `branches`, `sync_outbox`, `products`, `transactions`, etc.).

4. **UI Components & UX Specifications**:
   - Bento Grid Dashboard (4-card responsive layout, `#0061A4` / `#001C3B` / `#F8F9FF` Material 3 palette).
   - Bottom Nav Bar (4 fixed routes, >= 48px touch targets, smooth slide animations).
   - `fast-pair-v2` QR payload string (~180 bytes, HMAC-SHA256 signature).
   - `RobovaiAdDialog` (5s countdown timer, skip button enablement, daily cap tracking in `localStorage`).

---

## 2. Logic Chain

1. **From Requirements to HTTP Contracts**:
   - Fast QR pairing requires decoupling heavy data payloads from QR codes, placing payload streaming on embedded Kestrel HTTP server endpoints. Thus `/api/v1/pair/handshake` authenticates tokens, and `/api/v1/sync/*-stream` transports NDJSON streams.

2. **From Multi-Mode Sync to `sync_outbox` Schema**:
   - Offline operations must be transactional and durable across system crashes and network drops. Storing `INSERT`/`UPDATE`/`DELETE` operations in `sync_outbox` with GUID identifiers ensures reliable status transitions (`PENDING` -> `SYNCED`).

3. **From 18 Features to Programmatic Testing Strategy**:
   - Each feature has a distinct testable interface: HTTP API endpoints for Kestrel & streaming, DOM/CSS assertions for Compose theme and bottom nav, Node/Playwright scripts for Dexie v9 IndexedDB, and C# xUnit integration tests for SQLite WAL and DbContextFactory.

---

## 3. Caveats

1. **WPF UI Render Testing**:
   - Full visual rendering of WPF XAML components under high transaction load relies on Windows UI thread execution; programmatic testing for WPF memory leaks is best accomplished via ViewModel / DbContextFactory stress harnesses combined with process working set monitors.
2. **Port Binding Assumptions**:
   - Tests targeting port 5050 require local port availability or fallback port configuration during test suite execution.

---

## 4. Conclusion

All required interface contracts, database schemas, UI component specifications, and programmatic verification methods for all 18 features in `PROJECT.md` have been fully documented in `analysis.md`. The design is complete and actionable for the sub-orchestrator M0 and implementer/testing subagents.

---

## 5. Verification Method

To independently verify the mapped contracts and programmatic test strategies:

1. **Inspect Analysis Report**:
   ```powershell
   Get-Content -Path "f:\Raw\kasher\kasher\.agents\explorer_m0_2\analysis.md"
   ```
2. **Verify Schema Alignment**:
   - Cross-check `sync_outbox`, `Branch`, `ConnectedDevice`, and `BranchStock` schemas against `AppDbContext.cs` and `db.js`.
3. **Verify HTTP API Specifications**:
   - Test endpoints `/api/v1/pair/handshake`, `/api/v1/sync/export-stream`, `/api/v1/sync/import-stream`, `/api/v1/devices/heartbeat` using HTTP mock servers or curl/Node test scripts.
