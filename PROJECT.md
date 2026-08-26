# Project: RobovAI PRO POS & WMS Ecosystem Engineering Upgrade

## Architecture
- Dual-track architecture: Implementation Track (WPF Desktop C# .NET 8 / EF Core + Web PWA React/Vite/Dexie.js) and E2E Testing Track.
- Embedded Kestrel HTTP server in WPF (`http://0.0.0.0:5050`) for P2P local LAN hosting and streaming endpoints.
- Cloud REST/GraphQL integration options (Online mode).
- Outbox sync queue (`sync_outbox`) and configurable multi-mode sync engine (R1: Offline, Online, Hybrid).
- PWA Modernization refactoring `smart-inventory-pro` to adopt Jetpack Compose UX from `C:\Users\shaban\Downloads\robovai-wms` with Dexie.js v9 offline IndexedDB (R2).
- WPF Performance & Concurrency Hardening: DbContextFactory, SQLite WAL mode, BusyTimeout 30s, LiveCharts paint reuse, camera handle cleanup, GC compaction (R3).
- Fast QR Pairing (`fast-pair-v2`) + Chunked HTTP NDJSON P2P Streaming Engine (R4).
- Central Multi-Branch & Device Admin Control Panel with device heartbeat ping and unified RBAC (R5).

## Feature Inventory
| # | Feature | Description | Milestone | Source |
|---|---------|-------------|-----------|--------|
| 1 | Multi-Mode Sync Config Engine | Configuration schema (Offline/Online/Hybrid) & dynamic runtime switching | M1 | survey_3 |
| 2 | Outbox Queue & Sync Engine | Outbox queue (`sync_outbox`) for transactional offline change tracking & cloud push/pull | M1 | survey_3 |
| 3 | Embedded Kestrel HTTP Server | Embedded ASP.NET Core listener on port 5050 for local LAN hosting & API endpoints | M1 | survey_3 |
| 4 | Compose UX Styling & Colors | Material 3 blue tokens (#0061A4, #001C3B, #F8F9FF), Bento Grid Dashboard | M2 | survey_1 |
| 5 | Mobile Bottom Nav & Touch UI | 4-tab bottom navigation bar, touch dialogs, smooth transitions for iOS/Android | M2 | survey_1 |
| 6 | Dexie.js v9 & Offline PWA | Dexie v9 IndexedDB schema (`history_logs`, `app_prefs`, `branches`) & PWA manifest/sw.js | M2 | survey_1 |
| 7 | Product, Stocktake & Dispatch UX | Category chips, unit selector dropdowns ("قطعة", "كرتونة", etc.), dedicated stock audit & dispatch | M2 | survey_1 |
| 8 | PIN Setup & RobovaiAdDialog | Auth setup flow & 5s interstitial promotion modal with daily cap limit | M2 | survey_1 |
| 9 | Scoped DbContext Factory | Re-architect EF Core `AppDbContext` to `IDbContextFactory` with `.AsNoTracking()` | M3 | survey_2 |
| 10 | SQLite WAL & Timeout Locks | Enable `PRAGMA journal_mode=WAL;`, `PRAGMA busy_timeout=30000;`, `PRAGMA synchronous=NORMAL;` | M3 | survey_2 |
| 11 | Unmanaged Leaks & Deadlocks | LiveCharts paint reuse, OpenCV video camera handle disposal, bitmap churn prevention | M3 | survey_2 |
| 12 | Scanner Lifecycle & GC | Restore barcode scanner messenger registration across tab navigation & automated GC compaction | M3 | survey_2 |
| 13 | Fast QR Pairing Protocol | `fast-pair-v2` signed QR token protocol (~180 bytes) replacing bulk optical QR data | M4 | survey_3 |
| 14 | LAN P2P HTTP NDJSON Streaming | High-speed chunked HTTP streaming endpoints for 10,000+ records transfer in < 1.5s | M4 | survey_3 |
| 15 | Multi-Branch Inventory Schema | EF Core & Dexie schemas for `Branch`, `BranchStock`, and `StockTransfer` | M5 | survey_3 |
| 16 | Device Management & Heartbeats | `ConnectedDevice` schema, `/api/v1/devices/heartbeat` ping endpoint & status dashboard | M5 | survey_3 |
| 17 | Unified Multi-Branch Admin | Central Admin Control Panel in WPF & Web for multi-location inventory & RBAC management | M5 | survey_3 |
| 18 | E2E Test Suite (Tiers 1-4) | Comprehensive opaque-box test suite for R1-R5 features | M0 | survey_all |

## Milestones
| # | Name | Scope | Dependencies | Status |
|---|------|-------|-------------|--------|
| M0 | E2E Testing Track | Requirement-driven opaque-box test suite (Tiers 1-4) & `TEST_READY.md` | none | IN_PROGRESS |
| M1 | R1: Hybrid Architecture & Config Engine | Embedded Kestrel HTTP server, SyncConfig engine, `sync_outbox` transactional queue | none | IN_PROGRESS |
| M2 | R2: Web PWA Modernization | Jetpack Compose UX, bottom nav, Bento grid, Dexie v9, PWA manifest/sw, Ad modal | M1 | PLANNED |
| M3 | R3: Desktop WPF Memory & DB Lock Resolution | DbContextFactory, SQLite WAL/timeout, LiveCharts/OpenCV leak fixes, GC compaction | none | IN_PROGRESS |
| M4 | R4: Fast QR Pairing & High-Capacity LAN Sync | `fast-pair-v2` signed QR tokens & chunked HTTP NDJSON streaming API | M1 | PLANNED |
| M5 | R5: Central Multi-Branch & Device Admin Panel | `Branch`, `ConnectedDevice`, `StockTransfer` schemas, heartbeat API & Admin UI | M1, M2, M3 | PLANNED |
| M6 | Final Integration & E2E Validation | Pass 100% E2E test suite (Tiers 1-4) & Tier 5 adversarial coverage hardening | M0, M1, M2, M3, M4, M5 | PLANNED |

## Interface Contracts
### WPF Embedded Kestrel API (Port 5050) ↔ Web PWA / Mobile Clients
- Endpoint `POST /api/v1/pair/handshake`: Header `Authorization: Bearer <signed_token>`, returns `SessionToken`.
- Endpoint `GET /api/v1/sync/export-stream?entity={products|transactions}&since={timestamp}`: Content-Type `application/x-ndjson`.
- Endpoint `POST /api/v1/sync/import-stream`: Content-Type `application/x-ndjson`, returns JSON `{ importedCount: N, status: "OK" }`.
- Endpoint `POST /api/v1/devices/heartbeat`: Body `{ deviceId, deviceName, type, branchId, appVersion }`, returns `{ acknowledged: true, serverTime }`.

### Sync Engine Outbox Schema (`sync_outbox`)
- Schema (SQLite & Dexie.js): `id` (GUID), `entity_type` (text), `entity_id` (text), `operation` (INSERT/UPDATE/DELETE), `payload_json` (text), `created_at` (ISO string), `synced_at` (nullable ISO string), `status` (PENDING/FAILED/SYNCED).

## Code Layout
### Desktop WPF Application (`src/`)
- `src/SmartPOS.Domain/Entities/`: `Branch.cs`, `BranchStock.cs`, `StockTransfer.cs`, `ConnectedDevice.cs`, `SyncOutbox.cs`
- `src/SmartPOS.Infrastructure/Data/`: `AppDbContext.cs`, `DbContextFactory.cs`, `DbInitializer.cs`, `DatabasePathHelper.cs`
- `src/SmartPOS.Infrastructure/Services/`: `KestrelEmbeddedServer.cs`, `SyncEngineService.cs`, `GcCompactionService.cs`
- `src/SmartPOS.Application/ViewModels/`: `MainPOSViewModel.cs`, `ReportsViewModel.cs`, `WmsQrBridgeViewModel.cs`, `MultiBranchAdminViewModel.cs`
- `src/SmartPOS.WPF/Views/`: `POSPage.xaml`, `ReportsPage.xaml`, `SettingsPage.xaml`, `MultiBranchAdminPage.xaml`

### Web PWA (`smart-inventory-pro/`)
- `smart-inventory-pro/js/db.js`: Dexie v9 schema upgrade
- `smart-inventory-pro/js/sync-engine.js`: Hybrid sync engine & NDJSON streaming client
- `smart-inventory-pro/js/fast-pair.js`: `fast-pair-v2` token generator & scanner
- `smart-inventory-pro/css/compose-theme.css`: Compose Material 3 styling & bottom nav CSS
- `smart-inventory-pro/js/components/`: `BentoGrid.js`, `BottomNav.js`, `CategoryChips.js`, `RobovaiAdDialog.js`, `MultiBranchAdmin.js`
