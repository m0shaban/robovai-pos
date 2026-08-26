# Kasher PRO POS & WMS Ecosystem — Specification Mining & E2E Test Suite Design

- **Document Version**: 1.0.0
- **Target Component**: M0 (E2E Testing Track)
- **Author**: Specification Miner Agent (`spec_miner_m0_1`)
- **Status**: Completed Specification & Requirements Extraction
- **Scope**: Features 1–18 (Requirements R1–R5), Test Tiers 1–4

---

## Executive Summary & Master Feature Inventory

The **RobovAI PRO POS & WMS Ecosystem** (Kasher POS) undergoes a commercial-grade engineering upgrade across 5 primary requirement pillars (R1–R5). This document establishes the authoritative requirement specifications and test suite design across 4 opaque-box testing tiers to satisfy Milestone M0.

### Master Feature Mapping Matrix

| # | Category | Feature Name | Description | Req Ref | Milestone | Source |
|---|----------|--------------|-------------|---------|-----------|--------|
| 1 | Architecture & Sync | Multi-Mode Sync Config Engine | Configuration schema (Offline/Online/Hybrid) & dynamic runtime switching | R1 | M1 | survey_3 |
| 2 | Data Layer & Queue | Outbox Queue & Sync Engine | `sync_outbox` queue for transactional offline change tracking & cloud push/pull | R1 | M1 | survey_3 |
| 3 | Embedded Services | Embedded Kestrel HTTP Server | Embedded ASP.NET Core listener on port 5050 for local LAN hosting & API endpoints | R1 | M1 | survey_3 |
| 4 | Web PWA UX | Compose UX Styling & Colors | Material 3 blue tokens (#0061A4, #001C3B, #F8F9FF), Bento Grid Dashboard | R2 | M2 | survey_1 |
| 5 | Web PWA UX | Mobile Bottom Nav & Touch UI | 4-tab bottom navigation bar, touch dialogs, smooth transitions for iOS/Android | R2 | M2 | survey_1 |
| 6 | Offline Storage | Dexie.js v9 & Offline PWA | Dexie v9 IndexedDB schema (`history_logs`, `app_prefs`, `branches`) & PWA manifest/sw.js | R2 | M2 | survey_1 |
| 7 | Web PWA Operations | Product, Stocktake & Dispatch UX | Category chips, unit selector dropdowns ("قطعة", "كرتونة", etc.), dedicated stock audit & dispatch | R2 | M2 | survey_1 |
| 8 | Auth & Promotion | PIN Setup & RobovaiAdDialog | Auth setup flow & 5s interstitial promotion modal with daily cap limit | R2 | M2 | survey_1 |
| 9 | WPF Core & Data | Scoped DbContext Factory | Re-architect EF Core `AppDbContext` to `IDbContextFactory` with `.AsNoTracking()` | R3 | M3 | survey_2 |
| 10 | WPF Core & Data | SQLite WAL & Timeout Locks | Enable `PRAGMA journal_mode=WAL;`, `PRAGMA busy_timeout=30000;`, `PRAGMA synchronous=NORMAL;` | R3 | M3 | survey_2 |
| 11 | Performance & Graphics | Unmanaged Leaks & Deadlocks | LiveCharts paint reuse, OpenCV video camera handle disposal, bitmap churn prevention | R3 | M3 | survey_2 |
| 12 | Hardware & System | Scanner Lifecycle & GC | Restore barcode scanner messenger registration across tab navigation & automated GC compaction | R3 | M3 | survey_2 |
| 13 | Network & P2P | Fast QR Pairing Protocol | `fast-pair-v2` signed QR token protocol (~180 bytes) replacing bulk optical QR data | R4 | M4 | survey_3 |
| 14 | Network & P2P | LAN P2P HTTP NDJSON Streaming | High-speed chunked HTTP streaming endpoints for 10,000+ records transfer in < 1.5s | R4 | M4 | survey_3 |
| 15 | Multi-Branch Data | Multi-Branch Inventory Schema | EF Core & Dexie schemas for `Branch`, `BranchStock`, and `StockTransfer` | R5 | M5 | survey_3 |
| 16 | Admin & Infrastructure | Device Management & Heartbeats | `ConnectedDevice` schema, `/api/v1/devices/heartbeat` ping endpoint & status dashboard | R5 | M5 | survey_3 |
| 17 | Central Management | Unified Multi-Branch Admin | Central Admin Control Panel in WPF & Web for multi-location inventory & RBAC management | R5 | M5 | survey_3 |
| 18 | Quality & Test Infra | E2E Test Suite (Tiers 1-4) | Comprehensive opaque-box test suite for R1-R5 features | M0 | M0 | survey_all |

---

## Section 1: Detailed Feature Specifications & Interface Contracts

### Feature 1: Multi-Mode Sync Config Engine (R1 / M1)
- **Description**: Runtime configuration engine enabling dynamic switching between `Offline` (local LAN hosting only), `Online` (cloud REST/GraphQL API on Render/Vercel with PostgreSQL/Firebase), and `Hybrid` (local LAN for instant transactions + background auto-sync to cloud when online).
- **Interface Contract**:
  - Config Schema: JSON configuration file `appsettings.json` / Dexie `app_prefs` with fields: `syncMode` ("Offline" | "Online" | "Hybrid"), `cloudEndpoint` (string URL), `localPort` (int, default 5050), `syncIntervalSeconds` (int, e.g. 15), `retryBackoffMaxSeconds` (int, e.g. 300).
  - Runtime Switch API: `ISyncConfigEngine.SwitchMode(SyncMode newMode)` returns `SyncConfigResult`.
- **Inputs**: Mode enumeration string, API keys/endpoints, network ping status.
- **Outputs**: Active mode state, operational status flags (`IsCloudReachable`, `IsLanActive`, `ActiveQueueLength`).
- **Error Handling**: Graceful fallback to `Offline` if `Online`/`Hybrid` endpoint is unreachable; emit `SyncModeFallbackEvent`.

### Feature 2: Outbox Queue & Sync Engine (`sync_outbox`) (R1 / M1)
- **Description**: Transactional outbox pattern implementation for local data mutations while offline, with reliable cloud/peer background synchronization.
- **Interface Contract**:
  - Table Schema (`sync_outbox` in SQLite & Dexie.js):
    - `id` (GUID / string, Primary Key)
    - `entity_type` (text, e.g. "Product", "Sale", "StockTransfer")
    - `entity_id` (text)
    - `operation` (text: "INSERT" | "UPDATE" | "DELETE")
    - `payload_json` (text, serialized entity state)
    - `created_at` (text ISO-8601 string)
    - `synced_at` (nullable text ISO-8601 string)
    - `status` (text: "PENDING" | "SYNCED" | "FAILED")
    - `retry_count` (integer, default 0)
    - `last_error` (nullable text)
- **Inputs**: Entity mutation calls from WPF or Web PWA services.
- **Outputs**: Batch push payloads to cloud `/api/v1/sync/push` or LAN peer streaming.
- **Error Handling**: Idempotency check using `id`, exponential backoff retries on network failures, dead-letter state after max retries (5).

### Feature 3: Embedded Kestrel HTTP Server (R1 / M1)
- **Description**: Lightweight Kestrel HTTP server embedded in WPF application running on port `5050` (`http://0.0.0.0:5050`) serving P2P sync, QR pairing, and local mobile device REST endpoints.
- **Interface Contract**:
  - Base URL: `http://localhost:5050` or `http://<LAN_IP>:5050`
  - Routes:
    - `GET /api/v1/health` -> `{ status: "Healthy", uptimeSeconds: N, version: "2.0.0" }`
    - `POST /api/v1/pair/handshake` -> `{ sessionToken: string, expiresAt: string }`
    - `GET /api/v1/sync/export-stream` -> `application/x-ndjson`
    - `POST /api/v1/sync/import-stream` -> `{ importedCount: N, status: "OK" }`
    - `POST /api/v1/devices/heartbeat` -> `{ acknowledged: true, serverTime: string }`
- **Inputs**: HTTP requests over local intranet / Wi-Fi network.
- **Outputs**: JSON responses, NDJSON streams, HTTP status codes (200, 400, 401, 500).
- **Error Handling**: Port conflict detection (fallback to available port or log clear exception), rate limiting per IP, request payload size caps (max 50MB per stream batch).

### Feature 4: Compose UX Styling & Colors (R2 / M2)
- **Description**: Refactoring `smart-inventory-pro` CSS/UI styling to mirror Google Material Design 3 Jetpack Compose design tokens from Android WMS app.
- **Interface Contract**:
  - Color Palette: Primary `#0061A4`, Dark Container `#001C3B`, Background `#F8F9FF`, Surface `#FFFFFF`, Outline `#72777F`.
  - Layout Structure: Bento Grid dashboard with modular cards (Sales Summary, Inventory Alert, Device Status, Quick Actions).
- **Inputs**: Viewport width, theme settings (light/dark).
- **Outputs**: Responsive CSS layout with smooth grid transformations.
- **Error Handling**: Graceful degradation on unsupported CSS grid viewports, high-contrast fallback mode.

### Feature 5: Mobile Bottom Nav & Touch UI (R2 / M2)
- **Description**: Responsive 4-tab mobile bottom navigation bar designed for touch ergonomics on iOS (Safari PWA) and Android (Chrome PWA).
- **Interface Contract**:
  - Tabs: `[Home, Products/Stock, Transactions/Invoices, Settings/Admin]`
  - Dimensions: Fixed bottom height `64px`, touch targets `>= 48px x 48px`.
  - Transitions: Slide & fade page transitions (`150ms cubic-bezier`).
- **Inputs**: Touch tap events, swipe gestures, orientation change events.
- **Outputs**: Active view switching without full page reload.
- **Error Handling**: View state preservation during rapid tab switching, back-button history management.

### Feature 6: Dexie.js v9 & Offline PWA (R2 / M2)
- **Description**: Client-side storage modernizer using Dexie v9 IndexedDB and Service Worker caching strategy for offline operational capability.
- **Interface Contract**:
  - Dexie Schema v9: `db.version(9).stores({ products: 'id, barcode, category, name', transactions: 'id, date, status', history_logs: '++id, timestamp, action', app_prefs: 'key', branches: 'id, code', sync_outbox: 'id, status, created_at' })`
  - Service Worker (`sw.js`): Caching static assets (`index.html`, `compose-theme.css`, `bundle.js`) with Cache-First strategy.
- **Inputs**: Browser IndexedDB operations, Service Worker registration.
- **Outputs**: Persistent offline storage, fast app startup (< 1.2s offline).
- **Error Handling**: QuotaExceededError handling, automated db upgrade migration callbacks, offline fallback banner.

### Feature 7: Product, Stocktake & Dispatch UX (R2 / M2)
- **Description**: Touch-friendly web components for category chip filtering, unit selector dropdowns, stock counting/audit, and stock dispatching.
- **Interface Contract**:
  - Category Chips: Dynamic horizontally scrollable chip list.
  - Multi-Unit Dropdown: Unit options (`"قطعة"`, `"كرتونة"`, `"علبة"`, `"كيلو"`, `"جرام"`).
  - Stocktake UI: Physical count entry, expected count variance calculation (`variance = physicalCount - expectedCount`), discrepancy flag.
  - Dispatch UI: Outbound stock transfer creation with destination branch selector and item quantity inputs.
- **Inputs**: Barcode scans, category selections, quantity entries.
- **Outputs**: Updated inventory counts, stock audit records, outbox transfer events.
- **Error Handling**: Negative quantity validation, invalid barcode lookup notice, unsaved change warnings.

### Feature 8: PIN Setup & RobovaiAdDialog (R2 / M2)
- **Description**: Quick 4/6-digit security PIN setup/authentication and a 5-second non-skippable interstitial promotion dialog (`RobovaiAdDialog`).
- **Interface Contract**:
  - PIN Auth: Hash saved in `app_prefs` key `user_pin_hash` (PBKDF2/SHA256).
  - Interstitial Modal (`RobovaiAdDialog`): Appears on key actions (e.g. login or shift start). Countdown timer 5s before enable close button.
  - Daily Impression Cap: Max 3 impressions per 24-hour cycle stored in `app_prefs` `ad_impressions_today`.
- **Inputs**: User keypad PIN entries, ad modal trigger events.
- **Outputs**: Authenticated session flag, ad view counter increment.
- **Error Handling**: Account lockout after 5 invalid PIN attempts (30-second lockout timer), ad fallback display if image asset fails to load.

### Feature 9: Scoped DbContext Factory (R3 / M3)
- **Description**: Refactoring EF Core in WPF from long-lived singleton `AppDbContext` to `IDbContextFactory<AppDbContext>` short-lived scoped contexts to eliminate memory accumulation.
- **Interface Contract**:
  - Registration: `services.AddDbContextFactory<AppDbContext>(options => options.UseSqlite(connectionString))`
  - Usage Pattern: `using (var context = _dbContextFactory.CreateDbContext()) { ... }`
  - Read Queries: Always append `.AsNoTracking()` for read-only queries.
- **Inputs**: Data access service calls across ViewModels.
- **Outputs**: Instant memory release upon scope disposal.
- **Error Handling**: Automatic disposal via `using` blocks, prevention of ObjectDisposedException by passing DTOs/ViewModels instead of attached entity tracking instances.

### Feature 10: SQLite WAL & Timeout Locks (R3 / M3)
- **Description**: SQLite concurrency hardening to allow parallel multi-threaded read/write operations without database lock freezes.
- **Interface Contract**:
  - Connection Setup Pragmas:
    - `PRAGMA journal_mode = WAL;` (Write-Ahead Logging)
    - `PRAGMA busy_timeout = 30000;` (30-second lock wait threshold)
    - `PRAGMA synchronous = NORMAL;` (Safe performance trade-off)
- **Inputs**: High-concurrency read/write operations (e.g. POS cashier checkout + background sync daemon + scanner service).
- **Outputs**: Zero `SQLite Error 5 (database locked)` occurrences under heavy load.
- **Error Handling**: Graceful lock wait up to 30 seconds before throwing TimeoutException, write retry policy.

### Feature 11: Unmanaged Leaks & Deadlocks (R3 / M3)
- **Description**: Resolving WPF memory leaks and UI thread deadlocks stemming from LiveCharts paint objects, OpenCV video camera capture handles, and unmanaged bitmaps.
- **Interface Contract**:
  - LiveCharts Cleanup: Reuse `SolidColorPaint` instances; call `.Dispose()` on paint handles when chart views unload.
  - Camera Capture Cleanup: Explicit `_videoCapture?.Release(); _videoCapture?.Dispose();` in camera page `Unloaded` event.
  - Threading: Dispatcher operations wrapped in `await Application.Current.Dispatcher.InvokeAsync(...)` without blocking `.Wait()` or `.Result`.
- **Inputs**: Tab navigation, live chart updates, barcode scanning via video camera.
- **Outputs**: Steady memory consumption profile (< 250 MB working set over 24 hours).
- **Error Handling**: Camera device loss recovery, chart render failure fallback.

### Feature 12: Scanner Lifecycle & GC (R3 / M3)
- **Description**: Barcode scanner event messenger lifecycle management and scheduled background GC compaction.
- **Interface Contract**:
  - Messenger Registration: `WeakReferenceMessenger.Default.Register<BarcodeScannedMessage>(this, ...)` on view activation; `WeakReferenceMessenger.Default.Unregister<BarcodeScannedMessage>(this)` on deactivation.
  - GC Compaction Worker: Periodic background timer (every 15 minutes) executing `GC.Collect(2, GCCollectionMode.Aggressive, true, true); GC.WaitForPendingFinalizers();`.
- **Inputs**: Barcode scanner hardware signals, periodic compaction timer ticks.
- **Outputs**: Duplicate scan prevention, garbage collection log entries, memory footprint reduction.
- **Error Handling**: Unhandled hardware disconnections, safe GC collection during active POS checkout.

### Feature 13: Fast QR Pairing Protocol (R4 / M4)
- **Description**: `fast-pair-v2` lightweight QR code protocol replacing optical data transfer with tiny (~180 byte) signed JSON payload containing local server endpoint and encrypted authorization token.
- **Interface Contract**:
  - QR Code Payload Structure:
    ```json
    {
      "v": 2,
      "ep": "http://192.168.1.100:5050",
      "t": "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9...",
      "exp": 1770547200,
      "bid": "BR-HEAD-01"
    }
    ```
  - Pair Handshake Route: `POST /api/v1/pair/handshake` with header `Authorization: Bearer <t>`. Response: `{ "sessionToken": "SESS-998811", "expiresAt": "..." }`.
- **Inputs**: Camera scan of QR code, handshake HTTP request.
- **Outputs**: Authenticated P2P session token, pair confirmation UI feedback.
- **Error Handling**: Token expiration check (reject if `exp < currentTime`), invalid signature error (401 Unauthorized), connection timeout (< 2s pairing target).

### Feature 14: LAN P2P HTTP NDJSON Streaming (R4 / M4)
- **Description**: High-capacity P2P HTTP data streaming engine delivering 10,000+ products/transactions in under 1.5 seconds over local Wi-Fi/LAN.
- **Interface Contract**:
  - Stream Format: `application/x-ndjson` (Newline Delimited JSON, 1 record per line).
  - Export Route: `GET /api/v1/sync/export-stream?entity=products&since=0`
  - Import Route: `POST /api/v1/sync/import-stream`
- **Inputs**: Entity streams, byte chunk buffers.
- **Outputs**: Streaming HTTP response chunks, imported item count report.
- **Error Handling**: Stream truncation detection (line validator), chunk checksum verification, transaction rollback on chunk parse error.

### Feature 15: Multi-Branch Inventory Schema (R5 / M5)
- **Description**: Database schema and data management layer supporting multi-location branches, per-branch stock levels, and inter-branch stock transfers.
- **Interface Contract**:
  - `Branch` Entity: `id` (string), `name` (string), `code` (string), `address` (string), `is_headquarters` (bool).
  - `BranchStock` Entity: `id` (string), `branch_id` (string), `product_id` (string), `quantity` (decimal), `min_level` (decimal), `reorder_point` (decimal).
  - `StockTransfer` Entity: `id` (string), `source_branch_id` (string), `dest_branch_id` (string), `status` ("DRAFT" | "PENDING" | "SHIPPED" | "RECEIVED" | "CANCELLED"), `items_json` (string), `created_at` (string).
- **Inputs**: Stock allocation adjustments, transfer requests.
- **Outputs**: Branch-filtered stock views, stock audit logs.
- **Error Handling**: Prevent transfer if source branch quantity is insufficient, unique constraint on `(branch_id, product_id)`.

### Feature 16: Device Management & Heartbeats (R5 / M5)
- **Description**: Device registry and active ping monitor tracking connected POS terminals, mobile handhelds, and barcode scanners.
- **Interface Contract**:
  - `ConnectedDevice` Schema: `id`, `device_id`, `device_name`, `device_type` ("WPF_DESKTOP" | "WEB_PWA" | "MOBILE_HANDHELD"), `branch_id`, `ip_address`, `app_version`, `last_seen`, `status` ("ONLINE" | "IDLE" | "OFFLINE").
  - Heartbeat Route: `POST /api/v1/devices/heartbeat`
  - Payload: `{ "deviceId": "DEV-POS-01", "deviceName": "Main Checkout", "type": "WPF_DESKTOP", "branchId": "BR-01", "appVersion": "2.0.0" }`
  - Response: `{ "acknowledged": true, "serverTime": "2026-08-08T06:10:00Z" }`
- **Inputs**: Periodic heartbeat pings (every 30 seconds).
- **Outputs**: Live status indicators in Admin Control Panel.
- **Error Handling**: Automatically flag device as `OFFLINE` if no heartbeat received for > 90 seconds.

### Feature 17: Unified Multi-Branch Admin (R5 / M5)
- **Description**: Centralized administration dashboard accessible from WPF Desktop and Web PWA for managing multi-location inventory, transfer approvals, device health, and RBAC permissions.
- **Interface Contract**:
  - RBAC Roles: `Admin` (full access), `BranchManager` (branch-scoped write), `Cashier` (sales only), `InventoryClerk` (stock counting & transfers).
  - Controls: Multi-branch stock matrix table, transfer approval workflow button, user role assignment dropdown, sales aggregation chart by branch.
- **Inputs**: User credentials, admin commands, branch selector inputs.
- **Outputs**: Multi-branch sales reports, transfer state transitions, user permission grants.
- **Error Handling**: 403 Forbidden on unauthorized action attempt, audit logging of administrative overrides.

### Feature 18: E2E Test Suite (Tiers 1-4) (M0 / M0)
- **Description**: Opaque-box test automation suite, runner harness, and verification framework covering Features 1–17 across all 4 testing tiers.
- **Interface Contract**:
  - Harness Executable / Test Runner: CLI command runner executing integration and end-to-end assertions.
  - Output Report: JUnit XML / JSON summary output detailing test count, pass/fail state, execution duration, and failure stack traces.
  - Setup/Teardown: Isolated test database instances (`smartpos_test.db`, clean Dexie test context), mock Kestrel server handles.
- **Inputs**: Test configuration flags, target environment URLs/connection strings.
- **Outputs**: Detailed test execution results, pass percentage (> 99.5% threshold), `TEST_READY.md` verification report.
- **Error Handling**: Automatic test environment cleanup on exit, timeout protection per test case (30s max).

---

## Section 2: Tier 1 — Feature Coverage Test Specifications (TC-T1-001 to TC-T1-090)

This tier provides >= 5 concrete opaque-box test cases for each of the 18 features (90 total cases).

### Feature 1: Multi-Mode Sync Config Engine (R1 / M1)

#### TC-T1-001: Default Sync Engine Startup in Offline Mode
- **Objective**: Verify system defaults to `Offline` mode when initialized without prior configuration.
- **Setup**: Clean environment, no existing `appsettings.json` or `app_prefs`.
- **Steps**:
  1. Launch POS application service.
  2. Query `ISyncConfigEngine.GetCurrentMode()`.
  3. Inspect active config state.
- **Expected Results**: Active mode is `Offline`. Local Kestrel listener initialized; no remote network pings initiated.
- **Assertion**: `Assert.AreEqual(SyncMode.Offline, activeMode); Assert.IsFalse(syncEngine.IsCloudConnectionAttempted);`

#### TC-T1-002: Dynamic Mode Switch from Offline to Hybrid
- **Objective**: Verify runtime switching from `Offline` to `Hybrid` mode enables background sync daemon without restart.
- **Setup**: POS app running in `Offline` mode.
- **Steps**:
  1. Invoke `ISyncConfigEngine.SwitchMode(SyncMode.Hybrid)`.
  2. Monitor sync background worker status.
  3. Inspect `sync_outbox` processing thread.
- **Expected Results**: Sync engine updates active mode to `Hybrid`, starts outbox polling timer, retains local port 5050.
- **Assertion**: `Assert.AreEqual(SyncMode.Hybrid, activeMode); Assert.IsTrue(syncDaemon.IsRunning);`

#### TC-T1-003: Dynamic Mode Switch from Hybrid to Online
- **Objective**: Verify runtime switching to `Online` mode routes transaction calls directly to cloud endpoint.
- **Setup**: POS app running in `Hybrid` mode with valid cloud endpoint configured.
- **Steps**:
  1. Invoke `ISyncConfigEngine.SwitchMode(SyncMode.Online)`.
  2. Execute a test transaction API call.
- **Expected Results**: Mode switches to `Online`. Transaction payload is sent synchronously to cloud REST API.
- **Assertion**: `Assert.AreEqual(SyncMode.Online, activeMode); Assert.IsTrue(lastTransactionResponse.IsCloudAck);`

#### TC-T1-004: Sync Configuration Persistence Across System Restart
- **Objective**: Verify mode selection persists in configuration storage upon process restart.
- **Setup**: Set mode to `Hybrid`, `cloudEndpoint="https://api.kasher.com"`, save config.
- **Steps**:
  1. Terminate POS process.
  2. Restart POS application process.
  3. Query `ISyncConfigEngine.GetCurrentMode()`.
- **Expected Results**: Application reads saved configuration and resumes in `Hybrid` mode with configured endpoint.
- **Assertion**: `Assert.AreEqual(SyncMode.Hybrid, loadedMode); Assert.AreEqual("https://api.kasher.com", loadedEndpoint);`

#### TC-T1-005: Runtime Update of Cloud Endpoint & Interval Parameters
- **Objective**: Verify parameter updates (endpoint URL, poll interval) apply immediately without changing active mode.
- **Setup**: POS running in `Hybrid` mode with `syncIntervalSeconds=30`.
- **Steps**:
  1. Update `syncIntervalSeconds` to `15` and `cloudEndpoint` to `"https://backup-api.kasher.com"`.
  2. Invoke `ISyncConfigEngine.UpdateParameters(...)`.
- **Expected Results**: Sync daemon timer interval updates to 15 seconds; next outbox push targets backup endpoint.
- **Assertion**: `Assert.AreEqual(15, syncDaemon.IntervalSeconds); Assert.AreEqual("https://backup-api.kasher.com", syncDaemon.CurrentEndpoint);`

---

### Feature 2: Outbox Queue & Sync Engine (`sync_outbox`) (R1 / M1)

#### TC-T1-006: Transactional Outbox Enqueue on Offline Sale Creation
- **Objective**: Verify creating a sale in offline mode inserts a structured record into `sync_outbox`.
- **Setup**: System in `Offline` mode, `sync_outbox` table empty.
- **Steps**:
  1. Create a POS sale transaction with 2 items total $150.00.
  2. Save transaction to local database.
  3. Query `sync_outbox` table.
- **Expected Results**: 1 new row inserted in `sync_outbox` with `entity_type="Sale"`, `operation="INSERT"`, `status="PENDING"`, valid `payload_json`.
- **Assertion**: `Assert.AreEqual(1, outboxRows.Count); Assert.AreEqual("PENDING", outboxRows[0].Status);`

#### TC-T1-007: Outbox Queue Drainer Cloud Push Execution
- **Objective**: Verify pending outbox items are pushed to cloud API when cloud becomes reachable in Hybrid mode.
- **Setup**: 3 rows in `sync_outbox` with `status="PENDING"`, mock cloud API returning 200 OK.
- **Steps**:
  1. Trigger outbox sync worker `SyncEngineService.ProcessOutboxQueueAsync()`.
  2. Verify HTTP POST calls to cloud sync endpoint.
  3. Re-query `sync_outbox` table.
- **Expected Results**: Cloud API receives 3 items. `sync_outbox` rows update `status="SYNCED"` and `synced_at` set to current timestamp.
- **Assertion**: `Assert.IsTrue(outboxRows.All(r => r.Status == "SYNCED" && r.SyncedAt != null));`

#### TC-T1-008: Outbox Retry Counter & Backoff Increment on Cloud Error
- **Objective**: Verify outbox item stays `PENDING` and increments `retry_count` when cloud API returns 503 HTTP error.
- **Setup**: 1 item in `sync_outbox` (`retry_count=0`), mock cloud API returning 503 Service Unavailable.
- **Steps**:
  1. Trigger outbox sync worker.
  2. Inspect item state in `sync_outbox`.
- **Expected Results**: Item `status` remains `"PENDING"`, `retry_count` increments to `1`, `last_error` populated with HTTP 503 detail.
- **Assertion**: `Assert.AreEqual("PENDING", item.Status); Assert.AreEqual(1, item.RetryCount); Assert.IsNotNull(item.LastError);`

#### TC-T1-009: Dead-Letter Flagging After Maximum Retries
- **Objective**: Verify outbox item transitions to `FAILED` after reaching maximum retry limit (5).
- **Setup**: 1 item in `sync_outbox` with `retry_count=4`, mock cloud API failing permanently.
- **Steps**:
  1. Trigger outbox sync worker.
  2. Inspect item state.
- **Expected Results**: `retry_count` becomes 5, `status` updates to `"FAILED"`. Sync engine stops re-attempting this item.
- **Assertion**: `Assert.AreEqual("FAILED", item.Status); Assert.AreEqual(5, item.RetryCount);`

#### TC-T1-010: Idempotent Handling of Duplicate Sync Payloads
- **Objective**: Verify receiving identical entity GUID push does not duplicate records in target database.
- **Setup**: Entity with GUID `"SALE-10020"` already synced.
- **Steps**:
  1. Submit sync push payload containing `"SALE-10020"` again.
  2. Inspect target database `Sales` table.
- **Expected Results**: Target database detects existing entity ID, updates or ignores duplicate, returns success response without error.
- **Assertion**: `Assert.AreEqual(1, db.Sales.Count(s => s.Id == "SALE-10020"));`

---

### Feature 3: Embedded Kestrel HTTP Server (R1 / M1)

#### TC-T1-011: Kestrel Server Listener Binding on Port 5050
- **Objective**: Verify embedded Kestrel HTTP server starts listening on `http://0.0.0.0:5050` when application starts.
- **Setup**: WPF desktop app startup.
- **Steps**:
  1. Start WPF app instance.
  2. Send HTTP request `GET http://localhost:5050/api/v1/health`.
- **Expected Results**: Server responds with HTTP status 200 OK.
- **Assertion**: `Assert.AreEqual(HttpStatusCode.OK, response.StatusCode);`

#### TC-T1-012: Health Check Endpoint Payload Structure
- **Objective**: Verify `GET /api/v1/health` returns complete JSON diagnostic metadata.
- **Setup**: Kestrel server running on port 5050.
- **Steps**:
  1. Execute `GET http://localhost:5050/api/v1/health`.
  2. Parse JSON response body.
- **Expected Results**: JSON contains `status="Healthy"`, `uptimeSeconds` (integer > 0), `version="2.0.0"`.
- **Assertion**: `Assert.AreEqual("Healthy", json["status"].ToString()); Assert.IsTrue((int)json["uptimeSeconds"] >= 0);`

#### TC-T1-013: CORS Header Configuration for PWA Web Clients
- **Objective**: Verify Kestrel includes required Access-Control headers for PWA cross-origin requests.
- **Setup**: Send HTTP OPTIONS request to `http://localhost:5050/api/v1/sync/export-stream`.
- **Steps**:
  1. Inspect response HTTP headers.
- **Expected Results**: Headers contain `Access-Control-Allow-Origin: *` (or target origin) and `Access-Control-Allow-Methods: GET, POST, OPTIONS`.
- **Assertion**: `Assert.IsTrue(response.Headers.Contains("Access-Control-Allow-Origin"));`

#### TC-T1-014: High Concurrency HTTP Request Handling
- **Objective**: Verify server handles 50 concurrent GET requests to `/api/v1/health` without socket exhaustion.
- **Setup**: Server running on port 5050.
- **Steps**:
  1. Dispatch 50 parallel HTTP GET tasks using `HttpClient`.
  2. Await all tasks.
- **Expected Results**: All 50 requests return 200 OK within 500ms total elapsed time.
- **Assertion**: `Assert.IsTrue(responses.All(r => r.StatusCode == HttpStatusCode.OK));`

#### TC-T1-015: Graceful Embedded Kestrel Server Shutdown
- **Objective**: Verify closing WPF application releases port 5050 cleanly without lingering process or socket lock.
- **Setup**: WPF app running with active Kestrel listener.
- **Steps**:
  1. Trigger app shutdown / `KestrelEmbeddedServer.StopAsync()`.
  2. Attempt new HTTP GET to port 5050.
- **Expected Results**: Port 5050 is freed immediately; new connections are refused cleanly.
- **Assertion**: `Assert.ThrowsAsync<HttpRequestException>(async () => await client.GetAsync("http://localhost:5050/api/v1/health"));`

---

### Feature 4: Compose UX Styling & Colors (R2 / M2)

#### TC-T1-016: Material 3 Color Token Verification
- **Objective**: Verify CSS styling in `smart-inventory-pro` applies correct M3 color variables.
- **Setup**: Load Web PWA in headless Chrome / browser context.
- **Steps**:
  1. Inspect computed CSS root styles.
  2. Check values for `--md-sys-color-primary`, `--md-sys-color-on-primary-container`, `--md-sys-color-background`.
- **Expected Results**: `--md-sys-color-primary` is `#0061A4`, container is `#001C3B`, background is `#F8F9FF`.
- **Assertion**: `Assert.AreEqual("#0061a4", primaryColor.ToLower()); Assert.AreEqual("#001c3b", containerColor.ToLower());`

#### TC-T1-017: Bento Grid Dashboard Component Rendering
- **Objective**: Verify Bento Grid renders 4 primary card modules with responsive CSS grid layout.
- **Setup**: Open Web PWA Dashboard view (`#dashboard`).
- **Steps**:
  1. Inspect `.bento-grid` DOM element.
  2. Count child `.bento-card` elements.
- **Expected Results**: 4 bento cards present (Sales Summary, Inventory Alerts, Device Status, Quick Actions).
- **Assertion**: `Assert.AreEqual(4, bentoCards.Length); Assert.IsTrue(bentoGrid.HasClass("bento-grid"));`

#### TC-T1-018: Dark Theme & Light Theme Palette Switcher
- **Objective**: Verify toggling theme changes `--md-sys-color-background` dynamically.
- **Setup**: Web PWA open in light mode.
- **Steps**:
  1. Click theme toggle button.
  2. Inspect computed background color.
- **Expected Results**: Background color switches to dark token (e.g. `#1A1C1E`); `body` receives `dark-theme` CSS class.
- **Assertion**: `Assert.IsTrue(document.body.classList.contains("dark-theme"));`

#### TC-T1-019: Card Elevation & Border Radius Tokens
- **Objective**: Verify M3 card components have 16px border-radius and subtle elevation shadow.
- **Setup**: Web PWA card elements loaded.
- **Steps**:
  1. Inspect CSS properties of `.bento-card`.
- **Expected Results**: `border-radius` equals `16px` (or `1rem`), `box-shadow` matches M3 level 1 elevation.
- **Assertion**: `Assert.AreEqual("16px", cardStyle.borderRadius);`

#### TC-T1-020: Typography Hierarchy & Font Scaling
- **Objective**: Verify headings and body text adhere to Roboto/M3 typography scale tokens.
- **Setup**: Web PWA rendered view.
- **Steps**:
  1. Measure font size of `.bento-title` and `.bento-stat-value`.
- **Expected Results**: Title is 20px/headline size, stat value is 28px/display size.
- **Assertion**: `Assert.AreEqual("20px", titleFontSize); Assert.AreEqual("28px", statFontSize);`

---

### Feature 5: Mobile Bottom Nav & Touch UI (R2 / M2)

#### TC-T1-021: Mobile 4-Tab Bottom Navigation Bar Display
- **Objective**: Verify bottom navigation bar renders 4 tab items on mobile viewport (< 768px).
- **Setup**: Resize browser viewport to 390x844 (mobile portrait).
- **Steps**:
  1. Check visibility of `.bottom-nav`.
  2. Count `.bottom-nav-item` elements.
- **Expected Results**: Navigation bar pinned to viewport bottom, displaying 4 tabs (Home, Products, Invoices, Settings).
- **Assertion**: `Assert.IsTrue(bottomNav.IsVisible); Assert.AreEqual(4, navItems.Length);`

#### TC-T1-022: Tab Navigation View Switching
- **Objective**: Verify tapping "Products" tab navigates to Products view and updates active state.
- **Setup**: Mobile viewport, active tab = "Home".
- **Steps**:
  1. Click/tap "Products" bottom nav item.
  2. Inspect visible page section and active nav icon.
- **Expected Results**: Products view `#view-products` becomes active; Home view hides; "Products" icon gains `.active` class.
- **Assertion**: `Assert.IsTrue(productsView.IsVisible); Assert.IsTrue(productsNavItem.HasClass("active"));`

#### TC-T1-023: Touch Target Size Compliance (>= 48px x 48px)
- **Objective**: Verify all touch interactive buttons and nav items meet 48x48dp minimum touch size.
- **Setup**: Mobile viewport view inspection.
- **Steps**:
  1. Query bounding box dimensions of bottom nav buttons, category chips, and POS action buttons.
- **Expected Results**: Width and height for all interactive touch elements are >= 48px.
- **Assertion**: `Assert.IsTrue(buttonRect.width >= 48 && buttonRect.height >= 48);`

#### TC-T1-024: Touch Dialog Modal Open & Close Lifecycle
- **Objective**: Verify touch modal dialog (e.g. Add Product Dialog) opens modally with background backdrop and closes cleanly.
- **Setup**: Web PWA Products view.
- **Steps**:
  1. Tap "+ Add Product" button.
  2. Verify backdrop display.
  3. Tap backdrop or Close button.
- **Expected Results**: Modal overlay appears over page; tapping close dismisses dialog without leaving orphaned DOM nodes.
- **Assertion**: `Assert.IsTrue(modal.IsVisible); Assert.IsFalse(modalAfterClose.IsVisible);`

#### TC-T1-025: Page Transition Animation Smoothness
- **Objective**: Verify view transitions execute with smooth CSS transitions without layout shift.
- **Setup**: Mobile viewport navigation between tabs.
- **Steps**:
  1. Trigger tab transition from Home to Settings.
  2. Measure frame performance / CSS transition duration.
- **Expected Results**: Transition completes in <= 200ms with zero horizontal overflow scroll.
- **Assertion**: `Assert.IsTrue(transitionTimeMs <= 200); Assert.AreEqual(0, document.documentElement.scrollLeft);`

---

### Feature 6: Dexie.js v9 & Offline PWA (R2 / M2)

#### TC-T1-026: Dexie v9 IndexedDB Database Schema Upgrade
- **Objective**: Verify Dexie initialization creates v9 database with required stores.
- **Setup**: Clean browser IndexedDB state.
- **Steps**:
  1. Initialize `db = new Dexie('SmartInventoryDB')`.
  2. Check `db.verno` and object store names.
- **Expected Results**: `db.verno` is `9`. Stores `products`, `transactions`, `history_logs`, `app_prefs`, `branches`, `sync_outbox` exist.
- **Assertion**: `Assert.AreEqual(9, db.verno); Assert.IsTrue(db.tables.map(t=>t.name).includes("history_logs"));`

#### TC-T1-027: Service Worker Registration & Activation
- **Objective**: Verify `sw.js` registers and enters `activated` state on PWA launch.
- **Setup**: PWA served over HTTP/HTTPS.
- **Steps**:
  1. Execute `navigator.serviceWorker.register('/sw.js')`.
  2. Await registration ready state.
- **Expected Results**: Service Worker registers successfully with active state `activated`.
- **Assertion**: `Assert.IsNotNull(registration.active); Assert.AreEqual("activated", registration.active.state);`

#### TC-T1-028: Offline Asset Cache Verification
- **Objective**: Verify Service Worker caches core static files in `CacheStorage`.
- **Setup**: Service Worker active.
- **Steps**:
  1. Inspect `caches.keys()`.
  2. Open core cache repository and check keys (`/index.html`, `/css/styles.css`, `/js/app.js`).
- **Expected Results**: Cache contains all required static shell assets.
- **Assertion**: `Assert.IsTrue(cachedUrls.includes("/index.html")); Assert.IsTrue(cachedUrls.includes("/css/styles.css"));`

#### TC-T1-029: Complete Offline App Launch & Dexie Query Execution
- **Objective**: Verify application launches and fetches products from Dexie IndexedDB when network is completely offline.
- **Setup**: Pre-load 5 products into Dexie; set browser offline (`navigator.onLine = false`).
- **Steps**:
  1. Reload application URL while offline.
  2. Query `db.products.toArray()`.
- **Expected Results**: App UI loads from Service Worker cache; 5 products are retrieved from Dexie and rendered.
- **Assertion**: `Assert.AreEqual(5, products.length); Assert.IsFalse(navigator.onLine);`

#### TC-T1-030: PWA Web App Manifest Standalone Configuration
- **Objective**: Verify `manifest.json` specifies standalone display, name, and icons.
- **Setup**: Fetch `manifest.json`.
- **Steps**:
  1. Parse JSON contents of manifest.
- **Expected Results**: `display` is `"standalone"`, `start_url` is `"index.html"`, icons array contains 192x192 and 512x512 PNG entries.
- **Assertion**: `Assert.AreEqual("standalone", manifest.display); Assert.IsTrue(manifest.icons.some(i => i.sizes == "192x192"));`

---

### Feature 7: Product, Stocktake & Dispatch UX (R2 / M2)

#### TC-T1-031: Category Chip Filter Functionality
- **Objective**: Verify clicking a category chip filters the product display list.
- **Setup**: Products loaded across categories ("المشروبات", "المأكولات").
- **Steps**:
  1. Click "المشروبات" category chip.
  2. Count displayed product items.
- **Expected Results**: Product list shows only items with `category == "المشروبات"`.
- **Assertion**: `Assert.IsTrue(displayedProducts.All(p => p.Category == "المشروبات"));`

#### TC-T1-032: Multi-Unit Selection Dropdown State Handling
- **Objective**: Verify selecting a alternate unit (e.g. "كرتونة") updates product price and barcode mapping.
- **Setup**: Open product entry with unit conversions (`1 كرتونة = 24 قطعة`).
- **Steps**:
  1. Select unit dropdown option `"كرتونة"`.
  2. Inspect calculated unit price display.
- **Expected Results**: Unit price scales by factor 24; barcode field updates to carton barcode.
- **Assertion**: `Assert.AreEqual(basePrice * 24, calculatedUnitPrice);`

#### TC-T1-033: Stocktake Physical Count Entry & Audit Log Creation
- **Objective**: Verify submitting a stock count generates an audit log record with count variance.
- **Setup**: Product "Coffee 250g" expected stock = 50.
- **Steps**:
  1. Open Stocktake view.
  2. Enter physical count = 45.
  3. Click "Submit Stock Audit".
- **Expected Results**: System records physical count 45, variance -5, creates `history_logs` record with action `"STOCKTAKE_AUDIT"`.
- **Assertion**: `Assert.AreEqual(-5, auditRecord.Variance); Assert.AreEqual("STOCKTAKE_AUDIT", auditRecord.Action);`

#### TC-T1-034: Discrepancy Flag Threshold on Stock Audit
- **Objective**: Verify variance magnitude > 10% flags item as "HIGH_DISCREPANCY".
- **Setup**: Expected stock = 100, physical entry = 80 (variance -20%).
- **Steps**:
  1. Submit stock audit entry.
- **Expected Results**: Audit record flagged with `isHighDiscrepancy = true` for manager review.
- **Assertion**: `Assert.IsTrue(auditRecord.IsHighDiscrepancy);`

#### TC-T1-035: Outbound Stock Dispatch Transfer Creation
- **Objective**: Verify creating outbound dispatch constructs valid `StockTransfer` draft object.
- **Setup**: Branch "BR-MAIN".
- **Steps**:
  1. Open Dispatch view.
  2. Select Destination Branch "BR-NORTH", select 3 products with quantities.
  3. Click "Create Dispatch".
- **Expected Results**: New record inserted in `StockTransfer` table with `status="DRAFT"`, `source_branch_id="BR-MAIN"`, `dest_branch_id="BR-NORTH"`.
- **Assertion**: `Assert.AreEqual("DRAFT", transfer.Status); Assert.AreEqual("BR-NORTH", transfer.DestBranchId);`

---

### Feature 8: PIN Setup & RobovaiAdDialog (R2 / M2)

#### TC-T1-036: PIN Auth Setup & Hashing Verification
- **Objective**: Verify initial 4-digit PIN setup saves hashed value in `app_prefs`.
- **Setup**: First application launch, no PIN set.
- **Steps**:
  1. Enter PIN `"1234"`, confirm PIN `"1234"`.
  2. Click "Save PIN".
  3. Inspect `app_prefs` record `user_pin_hash`.
- **Expected Results**: PIN is hashed using PBKDF2/SHA256 (not plain text) and stored under key `user_pin_hash`.
- **Assertion**: `Assert.AreNotEqual("1234", storedHash); Assert.IsTrue(storedHash.Length >= 32);`

#### TC-T1-037: PIN Authentication Lock Screen Verification
- **Objective**: Verify entering correct PIN unlocks application; incorrect PIN rejects.
- **Setup**: PIN set to `"5678"`.
- **Steps**:
  1. Enter `"1111"` -> click Unlock.
  2. Enter `"5678"` -> click Unlock.
- **Expected Results**: Attempt 1 fails with message "رمز PIN غير صحيح". Attempt 2 succeeds and navigates to Dashboard.
- **Assertion**: `Assert.IsTrue(attempt1Failed); Assert.IsTrue(attempt2Unlocked);`

#### TC-T1-038: RobovaiAdDialog 5-Second Interstitial Countdown
- **Objective**: Verify advertisement modal displays a strict 5-second non-skippable timer.
- **Setup**: Trigger `RobovaiAdDialog`.
- **Steps**:
  1. Observe close button state at t = 0s, 2s, 5s.
- **Expected Results**: Close button disabled at t=0s and t=2s with text "إغلاق (5)", "إغلاق (3)". Enabled at t=5s with text "إغلاق".
- **Assertion**: `Assert.IsTrue(closeBtn.Disabled); /* wait 5s */ Assert.IsFalse(closeBtn.Disabled);`

#### TC-T1-039: Ad Impression Counter Increment
- **Objective**: Verify completing ad display increments daily impression counter in `app_prefs`.
- **Setup**: Initial `ad_impressions_today = 0`.
- **Steps**:
  1. Trigger and close `RobovaiAdDialog`.
  2. Query `app_prefs` value `ad_impressions_today`.
- **Expected Results**: Counter value becomes 1.
- **Assertion**: `Assert.AreEqual(1, impressionsToday);`

#### TC-T1-040: RobovaiAdDialog Daily Cap Enforcement (Max 3)
- **Objective**: Verify ad dialog does NOT display once daily impression cap of 3 is reached.
- **Setup**: Set `ad_impressions_today = 3` in `app_prefs`.
- **Steps**:
  1. Invoke `RobovaiAdDialog.ShowIfAllowed()`.
- **Expected Results**: Method returns `false`; ad dialog is bypassed.
- **Assertion**: `Assert.IsFalse(adShown); Assert.IsFalse(dialogElement.IsVisible);`

---

### Feature 9: Scoped DbContext Factory (R3 / M3)

#### TC-T1-041: IDbContextFactory Instance Creation
- **Objective**: Verify `IDbContextFactory<AppDbContext>.CreateDbContext()` yields fresh DbContext instances.
- **Setup**: Service provider initialized with factory registration.
- **Steps**:
  1. Create `context1` using factory.
  2. Create `context2` using factory.
- **Expected Results**: `context1` and `context2` are separate instance references (`context1 != context2`).
- **Assertion**: `Assert.AreNotSame(context1, context2);`

#### TC-T1-042: Short-Lived DbContext Scope Disposal
- **Objective**: Verify DbContext connection releases immediately when exiting `using` block.
- **Setup**: Factory initialized.
- **Steps**:
  1. Execute `using (var context = factory.CreateDbContext()) { ... }`.
  2. Check underlying SQLite connection state post-using block.
- **Expected Results**: Connection is closed/returned to pool; memory allocated by context is eligible for GC.
- **Assertion**: `Assert.Throws<ObjectDisposedException>(() => context.Products.Count());`

#### TC-T1-043: Read-Only AsNoTracking Performance Query
- **Objective**: Verify read operations specifying `.AsNoTracking()` do not attach entities to change tracker.
- **Setup**: Database contains 500 product records.
- **Steps**:
  1. Execute `using var ctx = factory.CreateDbContext(); var list = ctx.Products.AsNoTracking().ToList();`.
  2. Inspect `ctx.ChangeTracker.Entries()`.
- **Expected Results**: List returned with 500 items; `ChangeTracker.Entries().Count()` is `0`.
- **Assertion**: `Assert.AreEqual(500, list.Count); Assert.AreEqual(0, ctx.ChangeTracker.Entries().Count());`

#### TC-T1-044: Parallel Multi-Threaded Scoped DbContext Execution
- **Objective**: Verify 10 concurrent threads executing queries via `IDbContextFactory` run without concurrency exception.
- **Setup**: Factory initialized.
- **Steps**:
  1. Launch 10 parallel `Task.Run` worker tasks, each executing a scoped DbContext read query.
  2. Await `Task.WhenAll`.
- **Expected Results**: All 10 tasks complete successfully with zero `InvalidOperationException`.
- **Assertion**: `Assert.IsTrue(tasks.All(t => t.IsCompletedSuccessfully));`

#### TC-T1-045: Memory Heap Stability After Repeated Queries
- **Objective**: Verify executing 1,000 scoped read queries in a loop does not increase heap memory footprint.
- **Setup**: Record initial heap memory baseline.
- **Steps**:
  1. Loop 1,000 times: `using var ctx = factory.CreateDbContext(); var p = ctx.Products.AsNoTracking().FirstOrDefault();`.
  2. Trigger GC and check heap delta.
- **Expected Results**: Heap delta post-GC is < 2 MB.
- **Assertion**: `Assert.IsTrue(memoryDeltaMb < 2.0);`

---

### Feature 10: SQLite WAL & Timeout Locks (R3 / M3)

#### TC-T1-046: SQLite WAL Mode Pragma Verification
- **Objective**: Verify database connection initializes with `PRAGMA journal_mode=WAL;`.
- **Setup**: Connect to SQLite database using `AppDbContext`.
- **Steps**:
  1. Execute raw SQL query `PRAGMA journal_mode;`.
- **Expected Results**: Query returns single string result `"wal"`.
- **Assertion**: `Assert.AreEqual("wal", journalMode.ToLower());`

#### TC-T1-047: SQLite BusyTimeout 30,000ms Configuration
- **Objective**: Verify connection string or connection pragma sets `busy_timeout` to 30,000ms.
- **Setup**: Open database connection.
- **Steps**:
  1. Execute raw SQL `PRAGMA busy_timeout;`.
- **Expected Results**: Query returns `30000`.
- **Assertion**: `Assert.AreEqual(30000, busyTimeout);`

#### TC-T1-048: SQLite Synchronous Normal Mode Verification
- **Objective**: Verify connection pragma sets `PRAGMA synchronous = NORMAL;`.
- **Setup**: Open database connection.
- **Steps**:
  1. Execute raw SQL `PRAGMA synchronous;`.
- **Expected Results**: Query returns `1` (NORMAL).
- **Assertion**: `Assert.AreEqual(1, synchronousMode);`

#### TC-T1-049: Concurrent Read Operations During Active Write Transaction
- **Objective**: Verify readers are not blocked while a write transaction is executing under WAL mode.
- **Setup**: Database open in WAL mode.
- **Steps**:
  1. Start explicit write transaction on Thread A (delay 500ms before commit).
  2. Immediately execute read query on Thread B.
- **Expected Results**: Thread B read query completes immediately (< 50ms) without waiting for Thread A commit.
- **Assertion**: `Assert.IsTrue(readDurationMs < 100);`

#### TC-T1-050: Write Transaction Wait within BusyTimeout Window
- **Objective**: Verify a second write transaction waits up to busy_timeout rather than throwing database locked error instantly.
- **Setup**: WAL mode, busy_timeout = 30,000ms.
- **Steps**:
  1. Thread A acquires write lock for 1,000ms.
  2. Thread B attempts write transaction at t = 100ms.
- **Expected Results**: Thread B waits ~900ms and succeeds post Thread A commit; zero `SQLite Error 5` thrown.
- **Assertion**: `Assert.IsTrue(threadBCompleted); Assert.IsFalse(threadBThrewLockError);`

---

### Feature 11: Unmanaged Leaks & Deadlocks (R3 / M3)

#### TC-T1-051: LiveCharts Paint Object Reuse Verification
- **Objective**: Verify LiveCharts chart control reuses `SolidColorPaint` instances across redraw cycles.
- **Setup**: Load Reports View containing sales line chart.
- **Steps**:
  1. Trigger 20 chart data updates.
  2. Inspect instantiated `SolidColorPaint` object count.
- **Expected Results**: Static/cached paint instances are reused; paint allocations remain constant.
- **Assertion**: `Assert.AreEqual(initialPaintCount, finalPaintCount);`

#### TC-T1-052: LiveCharts Control Unload Paint Handle Disposal
- **Objective**: Verify navigating away from Reports View calls `.Dispose()` on paint handles.
- **Setup**: Open Reports View.
- **Steps**:
  1. Navigate to POS View (unloading Reports View).
  2. Check disposal flags on view paint resources.
- **Expected Results**: All view-scoped paint handles marked disposed.
- **Assertion**: `Assert.IsTrue(paintHandle.IsDisposed);`

#### TC-T1-053: OpenCV Camera Capture Resource Release on Tab Navigate
- **Objective**: Verify navigating away from Barcode Scanner camera view releases video capture handles.
- **Setup**: Camera scanner view active with live video preview.
- **Steps**:
  1. Navigate to Settings page.
  2. Inspect camera capture service handle status (`_videoCapture`).
- **Expected Results**: `_videoCapture.IsOpened()` is `false`; native camera resource handle freed.
- **Assertion**: `Assert.IsFalse(cameraService.IsCapturing); Assert.IsNull(cameraService.NativeHandle);`

#### TC-T1-054: WPF Image Bitmap Cache Churn Prevention
- **Objective**: Verify loading product image grid reuses bitmap caches without retaining unmanaged memory handles.
- **Setup**: Product grid displaying 100 item thumbnail images.
- **Steps**:
  1. Scroll product grid rapidly up and down 10 times.
  2. Check process working set memory.
- **Expected Results**: Memory footprint remains stable; unmanaged GDI/DirectX handles do not leak.
- **Assertion**: `Assert.IsTrue(workingSetMb < 300);`

#### TC-T1-055: Dispatcher Async UI Update Deadlock Immunity
- **Objective**: Verify background thread UI updates using `Application.Current.Dispatcher.InvokeAsync` do not cause thread lock.
- **Setup**: WPF UI running.
- **Steps**:
  1. Dispatch 100 async UI updates from background tasks without calling `.Wait()` or `.Result`.
- **Expected Results**: UI thread remains responsive (frame rate >= 50 FPS); zero UI thread freeze.
- **Assertion**: `Assert.IsTrue(uiIsResponsive);`

---

### Feature 12: Scanner Lifecycle & GC (R3 / M3)

#### TC-T1-056: Barcode Scanner Messenger Registration on View Load
- **Objective**: Verify View Models register for `BarcodeScannedMessage` upon activation.
- **Setup**: POS View inactive.
- **Steps**:
  1. Activate POS View.
  2. Query `WeakReferenceMessenger.Default.IsRegistered<BarcodeScannedMessage>(posViewModel)`.
- **Expected Results**: Registration is active (`true`).
- **Assertion**: `Assert.IsTrue(isRegistered);`

#### TC-T1-057: Barcode Scanner Messenger Unregistration on View Unload
- **Objective**: Verify View Models unregister for `BarcodeScannedMessage` upon deactivation.
- **Setup**: POS View active.
- **Steps**:
  1. Deactivate / Navigate away from POS View.
  2. Query messenger registration state.
- **Expected Results**: Registration is cleared (`false`), preventing double-scan events in background views.
- **Assertion**: `Assert.IsFalse(isRegistered);`

#### TC-T1-058: Barcode Scan Single-Processing Guarantee
- **Objective**: Verify sending 1 `BarcodeScannedMessage` results in exactly 1 product cart addition.
- **Setup**: POS View active, cart empty.
- **Steps**:
  1. Send `BarcodeScannedMessage("6221000111223")`.
  2. Check cart item count.
- **Expected Results**: Cart contains exactly 1 item with quantity = 1.
- **Assertion**: `Assert.AreEqual(1, cart.Count); Assert.AreEqual(1, cart[0].Quantity);`

#### TC-T1-059: Background GC Compaction Service Triggering
- **Objective**: Verify `GcCompactionService` executes periodic LOH/L2 garbage collection.
- **Setup**: `GcCompactionService` timer interval set to 1 second (test mode).
- **Steps**:
  1. Allocate temporary 50 MB byte arrays in memory.
  2. Wait for GC compaction timer tick.
- **Expected Results**: Compaction service fires `GC.Collect(2, GCCollectionMode.Aggressive, true, true)`; memory reclaimed.
- **Assertion**: `Assert.IsTrue(service.LastCollectionTimestamp > startTime); Assert.IsTrue(reclaimedMb > 40);`

#### TC-T1-060: 24-Hour Continuous Working Set Stability Simulation
- **Objective**: Verify working set memory remains under 250 MB after 10,000 simulated continuous checkout transactions.
- **Setup**: Continuous transaction runner loop.
- **Steps**:
  1. Execute 10,000 simulated sales transactions with scanner messages, database reads/writes, and view updates.
  2. Record working set memory at completion.
- **Expected Results**: Process working set memory is <= 250 MB.
- **Assertion**: `Assert.IsTrue(process.WorkingSet64 / (1024 * 1024) <= 250);`

---

### Feature 13: Fast QR Pairing Protocol (R4 / M4)

#### TC-T1-061: `fast-pair-v2` QR Payload Token Size & Schema Validation
- **Objective**: Verify generated QR payload adheres to `fast-pair-v2` schema and payload size is <= 200 bytes.
- **Setup**: Invoke `FastPairService.GenerateQrPayload(endpoint, branchId, secret)`.
- **Steps**:
  1. Measure string byte length of serialized JSON.
  2. Validate fields `v`, `ep`, `t`, `exp`, `bid`.
- **Expected Results**: `v == 2`, payload size is ~180 bytes (<= 200 bytes max).
- **Assertion**: `Assert.IsTrue(payloadBytes.Length <= 200); Assert.AreEqual(2, json["v"].ToInt32());`

#### TC-T1-062: Fast QR Code Scanning & Endpoint Parsing
- **Objective**: Verify client scanner extracts connection endpoint and token from scanned QR JSON string.
- **Setup**: Scanner receives QR string `{"v":2,"ep":"http://192.168.1.150:5050","t":"TOKEN_123","exp":1770547200,"bid":"BR-01"}`.
- **Steps**:
  1. Parse payload using `FastPairScanner.Parse(qrString)`.
- **Expected Results**: Endpoint resolves to `"http://192.168.1.150:5050"`, token resolves to `"TOKEN_123"`.
- **Assertion**: `Assert.AreEqual("http://192.168.1.150:5050", config.Endpoint); Assert.AreEqual("TOKEN_123", config.Token);`

#### TC-T1-063: Handshake Endpoint Authentication (`POST /api/v1/pair/handshake`)
- **Objective**: Verify valid signed token produces 200 OK and `SessionToken`.
- **Setup**: Server running on port 5050, valid pairing token generated.
- **Steps**:
  1. Send `POST http://localhost:5050/api/v1/pair/handshake` with header `Authorization: Bearer <valid_token>`.
- **Expected Results**: Status 200 OK, body contains `{ "sessionToken": "SESS-...", "expiresAt": "..." }`.
- **Assertion**: `Assert.AreEqual(HttpStatusCode.OK, response.StatusCode); Assert.IsTrue(json["sessionToken"].ToString().StartsWith("SESS-"));`

#### TC-T1-064: Handshake Token Expiration Rejection
- **Objective**: Verify expired pairing token (`exp < currentTimestamp`) is rejected with 401 Unauthorized.
- **Setup**: Pairing token created with `exp` timestamp in the past.
- **Steps**:
  1. Send handshake request with expired token.
- **Expected Results**: Server returns HTTP 401 Unauthorized with message `"Token expired"`.
- **Assertion**: `Assert.AreEqual(HttpStatusCode.Unauthorized, response.StatusCode);`

#### TC-T1-065: End-to-End Pairing Execution Time (< 2.0 Seconds)
- **Objective**: Verify complete QR scan to authenticated session token completion takes < 2.0 seconds.
- **Setup**: Client and Server on local LAN.
- **Steps**:
  1. Start stopwatch. Scan QR -> Parse token -> Execute Handshake request -> Receive SessionToken.
  2. Stop stopwatch.
- **Expected Results**: Total elapsed time is < 2000ms (typically ~150ms over LAN).
- **Assertion**: `Assert.IsTrue(stopwatch.ElapsedMilliseconds < 2000);`

---

### Feature 14: LAN P2P HTTP NDJSON Streaming (R4 / M4)

#### TC-T1-066: Export Stream Content-Type Header Verification
- **Objective**: Verify `GET /api/v1/sync/export-stream` returns `Content-Type: application/x-ndjson`.
- **Setup**: 10 products in server database.
- **Steps**:
  1. Request `GET http://localhost:5050/api/v1/sync/export-stream?entity=products`.
- **Expected Results**: Response Content-Type header is `application/x-ndjson`.
- **Assertion**: `Assert.AreEqual("application/x-ndjson", response.Content.Headers.ContentType.MediaType);`

#### TC-T1-067: NDJSON Line-by-Line Chunk Formatting
- **Objective**: Verify stream payload consists of valid JSON objects separated strictly by newline characters (`\n`).
- **Setup**: Export stream response stream.
- **Steps**:
  1. Read stream line by line using `StreamReader`.
  2. Parse each line independently as JSON.
- **Expected Results**: Each non-empty line parses successfully as a valid entity JSON object.
- **Assertion**: `Assert.IsTrue(lines.All(l => IsValidJson(l)));`

#### TC-T1-068: Import Stream Processing (`POST /api/v1/sync/import-stream`)
- **Objective**: Verify uploading NDJSON stream inserts records into client database and returns imported count.
- **Setup**: Prepare NDJSON stream with 50 product records.
- **Steps**:
  1. Send `POST /api/v1/sync/import-stream` with body containing 50 NDJSON lines.
- **Expected Results**: Status 200 OK, response JSON `{ "importedCount": 50, "status": "OK" }`.
- **Assertion**: `Assert.AreEqual(50, (int)responseJson["importedCount"]);`

#### TC-T1-069: High-Volume 10,000 Record Streaming Performance (< 1.5s)
- **Objective**: Verify transferring 10,000 product records over HTTP streaming completes in under 1.5 seconds.
- **Setup**: Server populated with 10,000 product entities.
- **Steps**:
  1. Execute streaming transfer from Server to Client.
  2. Measure execution duration from request dispatch to database write completion.
- **Expected Results**: All 10,000 records imported in elapsed time < 1500ms.
- **Assertion**: `Assert.IsTrue(elapsedMs < 1500); Assert.AreEqual(10000, clientDb.Products.Count());`

#### TC-T1-070: Data Integrity & Field Checksum Verification Post-Sync
- **Objective**: Verify all entity attributes (barcodes, prices, Arabic names) match exactly after NDJSON streaming.
- **Setup**: Source product `"زيت زيتون 1 لتر"`, barcode `"6225544332211"`, price `125.50`.
- **Steps**:
  1. Perform LAN NDJSON stream sync.
  2. Fetch synced product from target database.
- **Expected Results**: All attributes equal source attributes with zero character corruption.
- **Assertion**: `Assert.AreEqual("زيت زيتون 1 لتر", targetProduct.Name); Assert.AreEqual(125.50m, targetProduct.Price);`

---

### Feature 15: Multi-Branch Inventory Schema (R5 / M5)

#### TC-T1-071: Branch Entity Database Creation & Query
- **Objective**: Verify `Branch` entity can be created, persisted, and retrieved in EF Core and Dexie.
- **Setup**: Clean database.
- **Steps**:
  1. Insert `Branch { Id = "BR-01", Name = "فرع القاهرة", Code = "CAI-01", IsHeadquarters = true }`.
  2. Query database for `Id == "BR-01"`.
- **Expected Results**: Record retrieved matching name, code, and HQ flag.
- **Assertion**: `Assert.AreEqual("فرع القاهرة", branch.Name); Assert.IsTrue(branch.IsHeadquarters);`

#### TC-T1-072: BranchStock Per-Location Tracking Integrity
- **Objective**: Verify distinct stock levels are maintained for same product across different branches.
- **Setup**: Branches "BR-01" and "BR-02", Product "P-100".
- **Steps**:
  1. Set `BranchStock` quantity for (BR-01, P-100) = 50.
  2. Set `BranchStock` quantity for (BR-02, P-100) = 12.
  3. Query stocks by branch ID.
- **Expected Results**: BR-01 returns 50; BR-02 returns 12.
- **Assertion**: `Assert.AreEqual(50, stockBr1.Quantity); Assert.AreEqual(12, stockBr2.Quantity);`

#### TC-T1-073: StockTransfer Entity Lifecycle (DRAFT State)
- **Objective**: Verify initializing a stock transfer creates record in `DRAFT` status.
- **Setup**: Source "BR-01", Dest "BR-02".
- **Steps**:
  1. Create `StockTransfer` with 2 items.
  2. Save to database.
- **Expected Results**: Transfer status is `"DRAFT"`, timestamp set, transfer ID assigned.
- **Assertion**: `Assert.AreEqual("DRAFT", transfer.Status); Assert.IsNotNull(transfer.CreatedAt);`

#### TC-T1-074: StockTransfer State Transition Chain
- **Objective**: Verify transfer state transitions sequentially: `DRAFT` -> `PENDING` -> `SHIPPED` -> `RECEIVED`.
- **Setup**: Existing draft transfer.
- **Steps**:
  1. Update status to `PENDING` -> save.
  2. Update status to `SHIPPED` -> save.
  3. Update status to `RECEIVED` -> save.
- **Expected Results**: All state transitions succeed and produce corresponding audit history entries.
- **Assertion**: `Assert.AreEqual("RECEIVED", transfer.Status); Assert.AreEqual(4, auditLogs.Count);`

#### TC-T1-075: Inventory Deduction & Addition on Transfer Reception
- **Objective**: Verify completing transfer deducts quantity from source branch and adds to destination branch.
- **Setup**: Source BR-01 stock = 100, Dest BR-02 stock = 10; transfer quantity = 20.
- **Steps**:
  1. Execute `StockTransferService.CompleteTransfer(transferId)`.
  2. Re-query branch stocks.
- **Expected Results**: BR-01 stock becomes 80; BR-02 stock becomes 30.
- **Assertion**: `Assert.AreEqual(80, stockBr1.Quantity); Assert.AreEqual(30, stockBr2.Quantity);`

---

### Feature 16: Device Management & Heartbeats (R5 / M5)

#### TC-T1-076: Device Registration Entity Creation
- **Objective**: Verify connecting new POS device registers record in `ConnectedDevice` table.
- **Setup**: Clean device registry.
- **Steps**:
  1. Register device `DeviceId="DEV-POS-05"`, `DeviceName="Cashier Terminal 5"`, `Type="WPF_DESKTOP"`.
- **Expected Results**: Device record created with `Status="ONLINE"`, `LastSeen` populated.
- **Assertion**: `Assert.AreEqual("DEV-POS-05", device.DeviceId); Assert.AreEqual("ONLINE", device.Status);`

#### TC-T1-077: Heartbeat API Endpoint Request (`POST /api/v1/devices/heartbeat`)
- **Objective**: Verify sending heartbeat ping returns 200 OK with server acknowledgment.
- **Setup**: Registered device `"DEV-POS-05"`.
- **Steps**:
  1. Send `POST /api/v1/devices/heartbeat` with JSON payload `{ "deviceId": "DEV-POS-05", "branchId": "BR-01", "appVersion": "2.0.0" }`.
- **Expected Results**: Response HTTP status 200 OK, JSON `{ "acknowledged": true, "serverTime": "..." }`.
- **Assertion**: `Assert.AreEqual(HttpStatusCode.OK, response.StatusCode); Assert.IsTrue((bool)json["acknowledged"]);`

#### TC-T1-078: LastSeen Timestamp Update on Heartbeat Ping
- **Objective**: Verify heartbeat update refreshes `LastSeen` timestamp in database.
- **Setup**: Device `LastSeen` = 10 minutes ago.
- **Steps**:
  1. Dispatch heartbeat request.
  2. Query `ConnectedDevice` table.
- **Expected Results**: `LastSeen` timestamp updated to match server time (within 2 seconds).
- **Assertion**: `Assert.IsTrue((DateTime.UtcNow - device.LastSeen).TotalSeconds < 5);`

#### TC-T1-079: Automatic Offline Status Flagging on Missed Heartbeats
- **Objective**: Verify device status updates to `OFFLINE` if no heartbeat is received within timeout threshold (90s).
- **Setup**: Device `LastSeen` = 120 seconds ago, `Status="ONLINE"`.
- **Steps**:
  1. Run `DeviceMonitoringService.CheckDeviceHealth()`.
  2. Inspect device status.
- **Expected Results**: Device status transitions to `"OFFLINE"`.
- **Assertion**: `Assert.AreEqual("OFFLINE", device.Status);`

#### TC-T1-080: Active Devices Count Aggregation by Branch
- **Objective**: Verify admin endpoint returns count of active devices per branch.
- **Setup**: Branch BR-01 has 3 ONLINE devices, 1 OFFLINE device.
- **Steps**:
  1. Call `DeviceMonitoringService.GetActiveDeviceCount("BR-01")`.
- **Expected Results**: Method returns `3`.
- **Assertion**: `Assert.AreEqual(3, activeCount);`

---

### Feature 17: Unified Multi-Branch Admin (R5 / M5)

#### TC-T1-081: Central Admin Control Panel Initialization
- **Objective**: Verify Multi-Branch Admin view loads with aggregated system dashboard cards.
- **Setup**: User logged in as `Admin`.
- **Steps**:
  1. Open Multi-Branch Admin Page.
  2. Verify UI components (Branch Selector, Inventory Matrix, Transfer Approvals, Device Health).
- **Expected Results**: Admin view renders completely without error.
- **Assertion**: `Assert.IsTrue(adminPage.IsLoaded); Assert.IsNotNull(adminPage.InventoryMatrixGrid);`

#### TC-T1-082: Consolidated Multi-Branch Inventory Matrix Rendering
- **Objective**: Verify inventory grid displays stock columns grouped by branch location.
- **Setup**: 3 branches registered, 10 products.
- **Steps**:
  1. Load Multi-Branch Inventory Matrix.
  2. Inspect table headers and cell values.
- **Expected Results**: Table displays product names with dedicated quantity columns for Branch 1, Branch 2, Branch 3, and Total.
- **Assertion**: `Assert.AreEqual(4, matrixColumns.Count); // 3 branches + total`

#### TC-T1-083: Inter-Branch Stock Transfer Manager Approval Workflow
- **Objective**: Verify Admin user can approve a pending stock transfer from Admin Panel.
- **Setup**: Transfer `TR-9901` in `PENDING` status.
- **Steps**:
  1. Click "Approve Transfer" button for `TR-9901`.
- **Expected Results**: Transfer status updates to `SHIPPED`; notification sent to destination branch.
- **Assertion**: `Assert.AreEqual("SHIPPED", transfer.Status);`

#### TC-T1-084: Aggregated Sales Analytics Calculation Across Branches
- **Objective**: Verify Admin dashboard sums sales totals across all branches accurately.
- **Setup**: Branch 1 sales = $5,000; Branch 2 sales = $3,500.
- **Steps**:
  1. Fetch aggregated sales report.
- **Expected Results**: Total aggregated sales equals $8,500.00.
- **Assertion**: `Assert.AreEqual(8500.00m, report.TotalSales);`

#### TC-T1-085: Role-Based Access Control (RBAC) Permission Enforcement
- **Objective**: Verify user with role `Cashier` is blocked from accessing Admin Control Panel.
- **Setup**: User logged in as `Role = Cashier`.
- **Steps**:
  1. Attempt to navigate to `MultiBranchAdminPage`.
- **Expected Results**: Access denied dialog shown ("غير مصرح بالوصول"); view navigation blocked.
- **Assertion**: `Assert.IsFalse(navResult.Success); Assert.AreEqual("AccessDenied", navResult.ErrorCode);`

---

### Feature 18: E2E Test Suite (Tiers 1-4) (M0 / M0)

#### TC-T1-086: E2E Test Suite CLI Runner Execution
- **Objective**: Verify test suite runner launches and executes test suite via command line interface.
- **Setup**: Test runner environment configured.
- **Steps**:
  1. Execute CLI command `dotnet test --filter "Category=E2E"`.
- **Expected Results**: Test runner starts, executes targeted tests, and exits with code 0.
- **Assertion**: `Assert.AreEqual(0, process.ExitCode);`

#### TC-T1-087: Automated Test Environment Isolation & Setup
- **Objective**: Verify test harness creates clean isolated test database before test execution.
- **Setup**: Existing production/dev database files present.
- **Steps**:
  1. Initialize test runner harness.
- **Expected Results**: Test runner creates temporary database `smartpos_test_tmp.db`; existing data untouched.
- **Assertion**: `Assert.IsTrue(File.Exists("smartpos_test_tmp.db")); Assert.AreEqual(0, testDb.Products.Count());`

#### TC-T1-088: Real-Time Test Execution Assertion Reporting
- **Objective**: Verify test runner captures and outputs assertion failures with line numbers and stack traces.
- **Setup**: Force intentional assertion failure in test case.
- **Steps**:
  1. Run failing test case.
  2. Inspect test output log.
- **Expected Results**: Log records failure detail, expected vs actual values, and source file line number.
- **Assertion**: `Assert.IsTrue(logOutput.Contains("Expected: 5, Actual: 4"));`

#### TC-T1-089: Automated Test Harness Teardown & Cleanup
- **Objective**: Verify test harness deletes temporary test databases and stops embedded server handles on completion.
- **Setup**: Completed test run.
- **Steps**:
  1. Execute test suite teardown routine.
- **Expected Results**: Temporary test DB files deleted; Kestrel port 5050 closed.
- **Assertion**: `Assert.IsFalse(File.Exists("smartpos_test_tmp.db"));`

#### TC-T1-090: JUnit XML & JSON Test Summary Report Generation
- **Objective**: Verify test suite produces standard JUnit XML report detailing total tests, pass rate, and execution times.
- **Setup**: Execute full test run.
- **Steps**:
  1. Check output directory for `test_results.xml`.
  2. Parse XML summary attributes.
- **Expected Results**: XML report generated with `<testsuite tests="180" failures="0" ...>`. Pass rate equals 100%.
- **Assertion**: `Assert.Assertion(File.Exists("test_results.xml")); Assert.AreEqual("0", xmlRoot.Attribute("failures").Value);`

## Section 3: Tier 2 — Boundary & Corner Cases Test Specifications (TC-T2-001 to TC-T2-090)

This tier details >= 5 edge, boundary, and extreme corner case test scenarios for each of the 18 features (90 total cases).

### Feature 1: Multi-Mode Sync Config Engine (R1 / M1)

#### TC-T2-001: Mode Switch to Online with Unreachable Remote Host
- **Boundary Condition**: Network cable disconnected / DNS resolution failure during mode switch.
- **Input**: `ISyncConfigEngine.SwitchMode(SyncMode.Online)` when cloud endpoint is dead (HTTP timeout / host unreachable).
- **Observed Behavior**: System attempts connection for 3s, emits `SyncConnectionFailedEvent`, gracefully reverts mode to `Offline`, logs warning.
- **Assertion**: `Assert.AreEqual(SyncMode.Offline, activeMode); Assert.IsTrue(eventLogger.Contains("Cloud unreachable, reverted to Offline"));`

#### TC-T2-002: Malformed Endpoint URL Configuration
- **Boundary Condition**: Invalid URL format string in configuration file (e.g. `cloudEndpoint = "ht!p://invalid_url:99999"`).
- **Input**: Save corrupt URL to `appsettings.json` and initialize engine.
- **Observed Behavior**: URL parser catches `UriFormatException`, logs error, falls back to local port 5050 in `Offline` mode.
- **Assertion**: `Assert.AreEqual(SyncMode.Offline, activeMode); Assert.IsFalse(syncEngine.IsRunning);`

#### TC-T2-003: Rapid Multithreaded Mode Toggling Race Condition
- **Boundary Condition**: Switching modes 20 times in 100ms across 4 parallel threads.
- **Input**: Concurrent invocation of `SwitchMode` (Offline -> Hybrid -> Online -> Offline).
- **Observed Behavior**: Thread lock synchronization guarantees atomic mode transitions; zero deadlocks or corrupt intermediate states.
- **Assertion**: `Assert.IsTrue(Enum.IsDefined(typeof(SyncMode), activeMode)); Assert.IsFalse(syncEngine.IsCorrupted);`

#### TC-T2-004: Zero & Negative Sync Interval Boundary Clamping
- **Boundary Condition**: Configuration parameter `syncIntervalSeconds = 0` or `-15`.
- **Input**: Update interval config with non-positive values.
- **Observed Behavior**: Config engine clamps value to safe minimum boundary (`1` second) and logs parameter adjustment notice.
- **Assertion**: `Assert.AreEqual(1, syncDaemon.IntervalSeconds);`

#### TC-T2-005: Corrupt Handshake Header Response Handling
- **Boundary Condition**: Remote server returns invalid HTTP headers or HTML error page instead of JSON during sync initialization.
- **Input**: Server returns HTTP 200 with body `<html>502 Bad Gateway</html>`.
- **Observed Behavior**: Sync engine catches JSON deserialization exception, treats as sync failure, increments retry backoff timer.
- **Assertion**: `Assert.AreEqual(SyncStatus.Failed, syncResult.Status); Assert.IsTrue(syncResult.ErrorMessage.Contains("Deserialization"));`

---

### Feature 2: Outbox Queue & Sync Engine (`sync_outbox`) (R2 / M1)

#### TC-T2-006: Bulk Queue Pressure with 50,000 Pending Outbox Items
- **Boundary Condition**: Heavy offline operations generating 50,000 pending mutations in `sync_outbox`.
- **Input**: 50,000 row batch insert into `sync_outbox`.
- **Observed Behavior**: Queue drainer streams items in paginated batches of 500 items; process memory remains < 150 MB.
- **Assertion**: `Assert.AreEqual(500, firstBatch.Count); Assert.IsTrue(memoryUsageMb < 150);`

#### TC-T2-007: Ultra-Large Single Outbox Payload (10 MB Payload JSON)
- **Boundary Condition**: Single outbox mutation containing 10 MB payload string (e.g., base64 embedded product image).
- **Input**: Insert outbox record with `payload_json.Length == 10,485,760`.
- **Observed Behavior**: SQLite/Dexie stores payload without truncation; streaming sync transmitter uploads chunked HTTP stream successfully.
- **Assertion**: `Assert.AreEqual(10485760, syncedRecord.PayloadJson.Length);`

#### TC-T2-008: Intermittent Network Flap During Outbox Batch Push
- **Boundary Condition**: TCP connection drops after item 25 of 50 in batch push.
- **Input**: Abort socket connection mid-batch HTTP POST request.
- **Observed Behavior**: First 24 confirmed synced items marked `SYNCED`; unacknowledged items remain `PENDING` for next cycle.
- **Assertion**: `Assert.AreEqual(24, db.Outbox.Count(o => o.Status == "SYNCED")); Assert.AreEqual(26, db.Outbox.Count(o => o.Status == "PENDING"));`

#### TC-T2-009: Corrupt JSON Payload in Outbox Row
- **Boundary Condition**: Outbox row `payload_json` corrupted due to disk sector fault (invalid JSON string).
- **Input**: `payload_json = "{ id: 100, name: TRUNCATED..."`.
- **Observed Behavior**: Queue worker catches JSON parse exception, flags row `status = "FAILED"`, logs error, continues to next item.
- **Assertion**: `Assert.AreEqual("FAILED", corruptRow.Status); Assert.AreEqual("PENDING", nextRow.Status);`

#### TC-T2-010: Concurrent Process Queue Drain Lock Race
- **Boundary Condition**: WPF Desktop app and background service attempt to drain `sync_outbox` simultaneously.
- **Input**: Concurrent invocation of `ProcessOutboxQueueAsync()` on 2 threads.
- **Observed Behavior**: SQLite WAL row locking / `BEGIN IMMEDIATE` transaction ensures only 1 worker claims pending items; zero duplicate pushes.
- **Assertion**: `Assert.AreEqual(totalExpectedPushes, actualCloudPushes);`

---

### Feature 3: Embedded Kestrel HTTP Server (R1 / M1)

#### TC-T2-011: Port 5050 Occupation Conflict Handling
- **Boundary Condition**: Port 5050 is already occupied by another local service on startup.
- **Input**: Start Kestrel when port 5050 has an active socket listener.
- **Observed Behavior**: Server catches `IOException` / `AddressAlreadyInUse`, emits `PortConflictException`, logs clear diagnostic message.
- **Assertion**: `Assert.IsTrue(exception.Message.Contains("5050"));`

#### TC-T2-012: Inbound Payload Size Boundary (50 MB Limit)
- **Boundary Condition**: HTTP POST request payload size = 51 MB (exceeds max request body size).
- **Input**: `POST /api/v1/sync/import-stream` with 51 MB body.
- **Observed Behavior**: Server returns `HTTP 413 Payload Too Large` immediately without buffering full payload in RAM.
- **Assertion**: `Assert.AreEqual(HttpStatusCode.RequestEntityTooLarge, response.StatusCode);`

#### TC-T2-013: Malformed NDJSON Stream Ingestion Recovery
- **Boundary Condition**: NDJSON stream contains invalid JSON syntax on line 4,000 of 10,000.
- **Input**: Upload NDJSON stream with syntax error on line 4,000.
- **Observed Behavior**: Server halts stream processing at line 4,000, rolls back database transaction, returns `HTTP 400 Bad Request`.
- **Assertion**: `Assert.AreEqual(HttpStatusCode.BadRequest, response.StatusCode); Assert.AreEqual(0, db.Products.Count());`

#### TC-T2-014: High Rate Connection Flooding Throttling
- **Boundary Condition**: 500 inbound TCP connections per second from local network scanning tool.
- **Input**: Rapid TCP connection flood to `http://localhost:5050`.
- **Observed Behavior**: Server limits concurrent connections to configured cap (100), drops overflow gracefully; process does not crash.
- **Assertion**: `Assert.IsTrue(kestrelServer.IsHealthy);`

#### TC-T2-015: Unexpected Client TCP RST During Large Stream Export
- **Boundary Condition**: Client closes socket / sends TCP RST halfway through `GET /api/v1/sync/export-stream`.
- **Input**: Terminate client connection while server is streaming line 2,000.
- **Observed Behavior**: Server catches `OperationCanceledException` / `SocketException`, cancels stream task, releases response buffer cleanly.
- **Assertion**: `Assert.IsTrue(kestrelServer.ActiveStreamsCount == 0);`

---

### Feature 4: Compose UX Styling & Colors (R2 / M2)

#### TC-T2-016: Ultra-Narrow Viewport Rendering (280px Screen)
- **Boundary Condition**: Screen width set to 280px (extreme foldable / watch view).
- **Input**: Resize viewport to 280x650.
- **Observed Behavior**: Bento grid collapses to single column layout; no horizontal scrollbar or text truncation occurs.
- **Assertion**: `Assert.AreEqual(0, document.body.scrollWidth - document.body.clientWidth);`

#### TC-T2-017: High-DPI Desktop Scaling (300% DPI Scaling)
- **Boundary Condition**: OS display scaling set to 300% (4K display high DPI).
- **Input**: Render Web PWA under 300% device pixel ratio (`window.devicePixelRatio = 3.0`).
- **Observed Behavior**: CSS pixels scale proportionally; border radii and card elevation shadows render sharp without pixelation blur.
- **Assertion**: `Assert.AreEqual(3.0, window.devicePixelRatio);`

#### TC-T2-018: High Contrast Accessibility Theme Override
- **Boundary Condition**: OS high contrast mode enabled (`@media (forced-colors: active)`).
- **Input**: Activate high contrast OS theme.
- **Observed Behavior**: Material 3 colors yield to OS system contrast borders; text contrast ratio stays >= 7:1.
- **Assertion**: `Assert.IsTrue(computedContrastRatio >= 7.0);`

#### TC-T2-019: Bento Grid Empty Dashboard Initial State
- **Objective**: Verify Bento Grid handles 0 sales transactions and 0 products gracefully.
- **Input**: Load dashboard with empty IndexedDB database.
- **Observed Behavior**: Cards display zero states ("0.00 ج.م", "لا توجد تنبيهات") without rendering `undefined` or `NaN`.
- **Assertion**: `Assert.IsFalse(cardHtml.Contains("NaN")); Assert.IsTrue(cardHtml.Contains("0.00"));`

#### TC-T2-020: Extreme Category Tag List Rendering (200 Dynamic Tags)
- **Boundary Condition**: Product database contains 200 distinct category tags.
- **Input**: Render category chip filter bar with 200 chip elements.
- **Observed Behavior**: Chip container renders horizontal scrollbar with smooth touch scrolling; zero DOM freeze.
- **Assertion**: `Assert.AreEqual(200, categoryChips.Length);`

---

### Feature 5: Mobile Bottom Nav & Touch UI (R2 / M2)

#### TC-T2-021: Rapid Debounced Tab Tapping (10 Taps in 1 Second)
- **Boundary Condition**: User taps between bottom nav tabs 10 times in 1000ms.
- **Input**: Dispatch 10 rapid click events across tab icons.
- **Observed Behavior**: Debounce handler processes last selected tab; view state remains synchronized with active tab indicator.
- **Assertion**: `Assert.AreEqual(targetTabId, activeViewId);`

#### TC-T2-022: Screen Orientation Flip During Active Touch Modal
- **Boundary Condition**: Device rotates from Portrait to Landscape while Add Product modal is open.
- **Input**: Trigger `window.orientationchange` event.
- **Observed Behavior**: Modal dynamically resizes scrollable container height to fit landscape height; action buttons remain visible.
- **Assertion**: `Assert.IsTrue(modalSaveBtn.IsVisible);`

#### TC-T2-023: On-Screen Virtual Keyboard Viewport Compression
- **Boundary Condition**: Mobile virtual keyboard opens, reducing visible viewport height by 50%.
- **Input**: Focus text input field inside touch dialog.
- **Observed Behavior**: Dialog scrolls focused input into view; bottom nav hides or shifts behind keyboard overlay without obscuring input.
- **Assertion**: `Assert.IsTrue(inputRect.top >= 0 && inputRect.bottom <= window.innerHeight);`

#### TC-T2-024: Long-Press Touch Gesture Prevention on Action Buttons
- **Boundary Condition**: User performs 2-second long-press on POS checkout button.
- **Input**: Dispatch `touchstart` -> hold 2000ms -> `touchend`.
- **Observed Behavior**: Browser context menu and text selection callout are suppressed (`user-select: none`, `touch-action: manipulation`); button fires single click event.
- **Assertion**: `Assert.AreEqual(1, checkoutClickCount);`

#### TC-T2-025: Unsaved Changes Navigation Confirmation Modal
- **Boundary Condition**: User attempts bottom nav tab switch while editing product details with dirty form state.
- **Input**: Modify input field -> tap "Invoices" tab.
- **Observed Behavior**: Confirmation dialog appears ("تغييرات غير محفوظة"); view switch paused until user confirms or cancels.
- **Assertion**: `Assert.IsTrue(confirmDialog.IsVisible); Assert.AreEqual(currentViewId, activeViewId);`

---

### Feature 6: Dexie.js v9 & Offline PWA (R2 / M2)

#### TC-T2-026: IndexedDB Storage Quota Exhaustion (`QuotaExceededError`)
- **Boundary Condition**: Disk space full; browser triggers `QuotaExceededError` during Dexie `add()`.
- **Input**: Attempt database insertion when storage quota is reached.
- **Observed Behavior**: Error caught, oldest `history_logs` purged, user alerted via UI banner ("مساحة التخزين التلقائي امتلات").
- **Assertion**: `Assert.IsTrue(userNotified); Assert.IsTrue(oldLogsPurged);`

#### TC-T2-027: Dexie Database Schema Upgrade Migration (v8 -> v9)
- **Boundary Condition**: Upgrading existing client from v8 schema (missing `branches` table) to v9.
- **Input**: Launch app with existing v8 IndexedDB instance.
- **Observed Behavior**: Dexie upgrade callback executes, creates `branches` store, preserves all existing `products` and `transactions` data.
- **Assertion**: `Assert.AreEqual(9, db.verno); Assert.AreEqual(existingProductCount, db.products.count());`

#### TC-T2-028: Corrupt IndexedDB Transaction Recovery
- **Boundary Condition**: Browser abruptly closed mid-IndexedDB write transaction resulting in locked/corrupt state.
- **Input**: Open application with corrupted IndexedDB database.
- **Observed Behavior**: Application detects open failure, invokes `Dexie.delete()`, re-initializes clean schema, triggers full sync pull from local LAN server.
- **Assertion**: `Assert.IsTrue(db.isOpen());`

#### TC-T2-029: Service Worker Update Lifecycle & Stale Assets
- **Boundary Condition**: New Service Worker deployed while app is active in browser tab.
- **Input**: Deploy updated `sw.js`.
- **Observed Behavior**: Service Worker downloads in background, prompts user ("تحديث جديد متوفر"), updates cache upon user refresh confirmation.
- **Assertion**: `Assert.IsTrue(updateBannerDisplayed);`

#### TC-T2-030: Hard Refresh Offline App Reload
- **Boundary Condition**: User executes `Ctrl+F5` / hard reload while network is offline.
- **Input**: Hard reload page with `navigator.onLine == false`.
- **Observed Behavior**: Service Worker intercepts fetch event, serves cached `index.html` shell and static bundles.
- **Assertion**: `Assert.IsTrue(appLoadedSuccessfully);`

---

### Feature 7: Product, Stocktake & Dispatch UX (R2 / M2)

#### TC-T2-031: Maximum Integer Quantity Boundary (`999,999,999`)
- **Boundary Condition**: User enters maximum allowed quantity `999,999,999` in stock audit.
- **Input**: Set physical count = `999999999`.
- **Observed Behavior**: System accepts value without numeric overflow, formats text display with digit separators (`999,999,999`).
- **Assertion**: `Assert.AreEqual(999999999m, auditItem.Quantity);`

#### TC-T2-032: Special Character & Diacritics Product Search
- **Boundary Condition**: Product search query containing Arabic diacritics, quotes, and symbols (`"زَيْت"`, `%`, `'OR '1'='1`).
- **Input**: Type `"زَيْت"` into search bar.
- **Observed Behavior**: Search normalizes Arabic text (removes tashkeel), safely escapes SQL/IndexedDB queries, returns matching item.
- **Assertion**: `Assert.IsTrue(searchResults.Any(p => p.Name.Contains("زيت")));`

#### TC-T2-033: Non-Integer Unit Conversion Rounding (1 Carton = 7 Items)
- **Boundary Condition**: Fractional unit price division (Carton price $10.00 / 7 items = $1.428571...).
- **Input**: Select single item unit from carton bundle of 7.
- **Observed Behavior**: Unit price rounded to 2 decimal places ($1.43); total transaction sum matches exact currency rounding rules.
- **Assertion**: `Assert.AreEqual(1.43m, itemUnitPrice);`

#### TC-T2-034: Mass Stocktake Audit Submission (5,000 Line Items)
- **Boundary Condition**: Submitting stock audit for entire store inventory of 5,000 items in single action.
- **Input**: Submit 5,000 line item audit grid.
- **Observed Behavior**: Batch processing executes in chunks of 500 items; progress bar updates, completes within 2.5s.
- **Assertion**: `Assert.AreEqual(5000, auditLogsInsertedCount);`

#### TC-T2-035: Invalid Inter-Branch Self-Dispatch Validation
- **Boundary Condition**: User selects Destination Branch identical to Source Branch ("BR-MAIN" -> "BR-MAIN").
- **Input**: Click Create Dispatch with source == destination.
- **Observed Behavior**: Form validation prevents submission, displays error "لا يمكن التحويل لنفس الفرع".
- **Assertion**: `Assert.IsFalse(isSubmitted); Assert.IsTrue(errorMsgVisible);`

---

### Feature 8: PIN Setup & RobovaiAdDialog (R2 / M2)

#### TC-T2-036: Non-Numeric Characters in PIN Input
- **Boundary Condition**: User attempts pasting letters / special characters into PIN keypad (`"12ab"`).
- **Input**: Enter `"12ab"` into PIN setup field.
- **Observed Behavior**: Input field filters out non-digit characters immediately; only numeric digits `"12"` are accepted.
- **Assertion**: `Assert.AreEqual("12", pinInputField.Text);`

#### TC-T2-037: 5 Consecutive Invalid PIN Lockout Escalation
- **Boundary Condition**: User enters wrong PIN 5 times in succession.
- **Input**: 5 failed PIN entry attempts.
- **Observed Behavior**: Keypad is disabled for 30 seconds; countdown timer displays "يرجى الانتظار 30 ثانية".
- **Assertion**: `Assert.IsTrue(keypadDisabled); Assert.AreEqual(30, lockoutSecondsRemaining);`

#### TC-T2-038: System Clock Manipulation for Ad Cap Bypass
- **Boundary Condition**: User changes system date backwards by 1 day to reset `ad_impressions_today`.
- **Input**: System clock set back 24 hours while `ad_impressions_today == 3`.
- **Observed Behavior**: System compares timestamp against monotonically increasing server time or session token epoch; ad remains capped.
- **Assertion**: `Assert.IsFalse(adDialogShown);`

#### TC-T2-039: Ad Image Asset Network 404 Failure Fallback
- **Boundary Condition**: Image URL configured for `RobovaiAdDialog` returns HTTP 404 Not Found.
- **Input**: Trigger ad modal when image asset fails to load.
- **Observed Behavior**: Modal falls back to styled CSS text promotional card; 5-second timer operates normally without breaking.
- **Assertion**: `Assert.IsTrue(fallbackCardVisible); Assert.IsTrue(timerRunning);`

#### TC-T2-040: Application Exit During Active 5s Interstitial Ad
- **Boundary Condition**: App force closed / browser tab closed at t = 2s of 5s ad timer.
- **Input**: Terminate process at t = 2s.
- **Observed Behavior**: Daily impression counter is NOT incremented; ad will re-display on next app launch.
- **Assertion**: `Assert.AreEqual(0, savedAdImpressionsToday);`

---

### Feature 9: Scoped DbContext Factory (R3 / M3)

#### TC-T2-041: Access Disposed Entity Property Outside Scope
- **Boundary Condition**: ViewModel attempts lazy-loading navigation property after scoped DbContext is disposed.
- **Input**: Access `sale.Customer.Name` outside `using` scope when lazy loading is enabled.
- **Observed Behavior**: Explicit projections (`.Select()`) or eager loading (`.Include()`) used; lazy loading disabled to prevent `ObjectDisposedException`.
- **Assertion**: `Assert.IsNotNull(saleDto.CustomerName);`

#### TC-T2-042: Thread Pool Exhaustion Under 1,000 Concurrent DbContext Requests
- **Boundary Condition**: 1,000 simultaneous background tasks request DbContext from factory.
- **Input**: `Task.WhenAll(1000 tasks calling factory.CreateDbContext())`.
- **Observed Behavior**: SQLite connection pool queues requests cleanly; all 1,000 contexts execute and dispose without unhandled exception.
- **Assertion**: `Assert.IsTrue(allTasksCompleted);`

#### TC-T2-043: Long-Running Scoped DbContext (> 30 Minutes) Timeout Guard
- **Boundary Condition**: Buggy code holds scoped DbContext open for 30 minutes.
- **Input**: Keep DbContext scope open without disposing for 1,800 seconds.
- **Observed Behavior**: DbContext leak detector service logs warning alert with stack trace identifying un-disposed context.
- **Assertion**: `Assert.IsTrue(leakDetector.HasWarning);`

#### TC-T2-044: Exception Inside DbContext Scope Clean Disposal
- **Boundary Condition**: Unhandled division by zero exception occurs inside DbContext transaction block.
- **Input**: Throw exception inside `using (var ctx = factory.CreateDbContext())`.
- **Observed Behavior**: `using` block automatically invokes `.Dispose()`; underlying SQLite connection is returned to pool cleanly.
- **Assertion**: `Assert.AreEqual(0, activeConnectionsCount);`

#### TC-T2-045: Mixed Read/Write in Single Scope with AsNoTracking Conflict
- **Boundary Condition**: Querying entity with `.AsNoTracking()` and then attempting `dbContext.Update(entity)`.
- **Input**: Fetch untracked product -> modify price -> call `SaveChanges()`.
- **Observed Behavior**: DbContext attaches entity explicitly via `dbContext.Update()`; write succeeds cleanly.
- **Assertion**: `Assert.AreEqual(EntityState.Modified, ctx.Entry(product).State);`

---

### Feature 10: SQLite WAL & Timeout Locks (R3 / M3)

#### TC-T2-046: Database File Lock Exceeding BusyTimeout (30,000ms)
- **Boundary Condition**: External process holds exclusive lock on `smartpos.db` for 35 seconds.
- **Input**: Attempt write operation when database is locked > 30,000ms.
- **Observed Behavior**: System waits full 30,000ms busy_timeout, then throws `TimeoutException` with user-friendly retry prompt.
- **Assertion**: `Assert.IsTrue(elapsedMs >= 30000); Assert.IsTrue(exceptionThrown);`

#### TC-T2-047: Sudden Process Crash Mid WAL Write Transaction
- **Boundary Condition**: Process `kill -9` executed while writing 1,000 transactions to WAL file.
- **Input**: Terminate process during `db.SaveChangesAsync()`.
- **Observed Behavior**: Upon app restart, SQLite automatically recovers database integrity from `.db-wal` file; zero database corruption.
- **Assertion**: `Assert.IsTrue(dbCheck.IntegrityCheckPassed);`

#### TC-T2-048: Disk Full (0 Bytes Remaining) Mid WAL Checkpoint
- **Boundary Condition**: Disk space reaches 0 bytes while WAL log is checkpointing to main DB file.
- **Input**: Simulate disk full condition during `PRAGMA wal_checkpoint(FULL);`.
- **Observed Behavior**: Checkpoint fails safely, transaction rolls back, system switches to read-only alert mode.
- **Assertion**: `Assert.IsTrue(isReadOnlyModeActive);`

#### TC-T2-049: Extreme Multi-Threaded Write Contention (20 Threads)
- **Boundary Condition**: 20 threads attempting simultaneous write transactions to `sync_outbox`.
- **Input**: Execute 20 concurrent write loops.
- **Observed Behavior**: SQLite WAL serializes writes via busy_timeout queuing; all 20 writes complete successfully without lock error.
- **Assertion**: `Assert.AreEqual(20, completedWritesCount);`

#### TC-T2-050: Non-ASCII & Arabic Directory File Path Connection
- **Boundary Condition**: Database stored in path with spaces and Arabic characters (`F:\برنامج المبيعات\data\smartpos.db`).
- **Input**: Initialize SQLite connection string with Arabic file path.
- **Observed Behavior**: SQLite connection opens cleanly; WAL and SHM journal files created in Arabic directory path.
- **Assertion**: `Assert.IsTrue(File.Exists(@"F:\برنامج المبيعات\data\smartpos.db-wal"));`

---

### Feature 11: Unmanaged Leaks & Deadlocks (R3 / M3)

#### TC-T2-051: LiveCharts High-Frequency Redraw Stress (1,000 Redraws)
- **Boundary Condition**: Rapid real-time sales stream triggering 1,000 chart updates in 10 seconds.
- **Input**: Push 1,000 data point updates to LiveCharts series.
- **Observed Behavior**: Chart update throttled to 30 FPS render window; memory footprint delta remains < 5 MB.
- **Assertion**: `Assert.IsTrue(memoryDeltaMb < 5.0);`

#### TC-T2-052: Rapid Page Navigation Loop (100 Navigations in 10s)
- **Boundary Condition**: Switching rapidly between Reports View and Camera Scanner View 100 times.
- **Input**: Execute automated navigation loop.
- **Observed Behavior**: All view resources, camera handles, and chart paint objects are disposed on each cycle; zero handle leak.
- **Assertion**: `Assert.IsTrue(processHandleCountDelta < 10);`

#### TC-T2-053: Camera USB Unplugged During Live Video Capture Preview
- **Boundary Condition**: Barcode scanner webcam USB cable physically disconnected during active video stream.
- **Input**: Simulate hardware device disconnection event.
- **Observed Behavior**: OpenCV capture loop catches device read failure, releases native handle safely, displays UI camera offline icon.
- **Assertion**: `Assert.IsFalse(cameraService.IsCapturing); Assert.IsTrue(cameraOfflineUiVisible);`

#### TC-T2-054: WPF Dispatcher Synchronous Wait Deadlock Prevention
- **Boundary Condition**: Calling `.Result` or `.Wait()` on async DbContext factory call inside WPF UI thread event handler.
- **Input**: Invoke `factory.CreateDbContextAsync().Result` on main thread.
- **Observed Behavior**: Architecture enforces pure `async`/`await` pattern; code review rule & test analyzer flag blocking `.Result` calls.
- **Assertion**: `Assert.IsFalse(codebaseAnalyzer.HasSyncOverAsyncCalls);`

#### TC-T2-055: Low System RAM Pressure Operation (2 GB System)
- **Boundary Condition**: POS application running on low-spec hardware terminal with only 2 GB total physical RAM.
- **Input**: Limit available process working set memory to 200 MB max.
- **Observed Behavior**: App operates stably, triggers aggressive LOH GC compaction when working set reaches 180 MB.
- **Assertion**: `Assert.IsTrue(processWorkingSetMb <= 200);`

---

### Feature 12: Scanner Lifecycle & GC (R3 / M3)

#### TC-T2-056: High-Frequency Scanner Burst (50 Scans in 1 Second)
- **Boundary Condition**: Industrial barcode scanner firing 50 scans per second into input buffer.
- **Input**: Dispatch 50 `BarcodeScannedMessage` events in 1000ms.
- **Observed Behavior**: Messenger queues messages, POS process adds items sequentially without losing scan events or crashing UI.
- **Assertion**: `Assert.AreEqual(50, totalScannedItemsInCart);`

#### TC-T2-057: Scanner Event Dispatched with No Active ViewModel
- **Boundary Condition**: Hardware barcode scanner fires scan event while user is on modal loading screen (no active view registered).
- **Input**: Send `BarcodeScannedMessage` when active view is null.
- **Observed Behavior**: WeakReferenceMessenger ignores event safely; zero null reference exception.
- **Assertion**: `Assert.IsTrue(noExceptionThrown);`

#### TC-T2-058: Barcode Containing Binary & Control Characters
- **Boundary Condition**: Scanned barcode contains unprintable ASCII control characters (`\x00\x1B\x0762211`).
- **Input**: Dispatch scanner message with raw control characters.
- **Observed Behavior**: Input sanitizer strips control characters, extracts valid barcode string `"62211"`.
- **Assertion**: `Assert.AreEqual("62211", sanitizedBarcode);`

#### TC-T2-059: Manual GC Compaction Interlocking During Write Transaction
- **Boundary Condition**: Background GC compaction timer fires while heavy database write transaction is committing.
- **Input**: Invoke `GC.Collect()` mid `SaveChangesAsync()`.
- **Observed Behavior**: Write transaction completes cleanly; GC compaction runs post-commit without locking database thread.
- **Assertion**: `Assert.IsTrue(transactionSucceeded); Assert.IsTrue(gcExecuted);`

#### TC-T2-060: Scanner USB Device Re-enumeration Loop
- **Boundary Condition**: Scanner hardware disconnects and reconnects 5 times in 10 seconds (faulty USB cable).
- **Input**: Trigger device connect/disconnect events repeatedly.
- **Observed Behavior**: Scanner lifecycle manager re-binds listener on each reconnection without creating duplicate event handlers.
- **Assertion**: `Assert.AreEqual(1, scannerService.ActiveListenerCount);`

---

### Feature 13: Fast QR Pairing Protocol (R4 / M4)

#### TC-T2-061: Optical Corruption of Scanned QR String
- **Boundary Condition**: Scanned QR string has corrupted base64 or missing JSON closing brace (`{"v":2,"ep":"http://192...`).
- **Input**: Pass malformed QR payload string to scanner parser.
- **Observed Behavior**: Parser catches format exception, returns clear error result "رمز QR غير صالح", prompts user to re-scan.
- **Assertion**: `Assert.IsFalse(parseResult.Success); Assert.IsTrue(parseResult.ErrorMessage.Contains("QR"));`

#### TC-T2-062: Pairing Request from Subnet Outside Local LAN
- **Boundary Condition**: Inbound handshake request originates from public WAN IP (`203.0.113.50`) instead of local subnet (`192.168.x.x`).
- **Input**: Send `POST /api/v1/pair/handshake` with WAN source IP.
- **Observed Behavior**: Server rejects request with `HTTP 403 Forbidden`, logging untrusted pairing attempt.
- **Assertion**: `Assert.AreEqual(HttpStatusCode.Forbidden, response.StatusCode);`

#### TC-T2-063: Signature Verification Failure on Tampered Token
- **Boundary Condition**: Attacker modifies `ep` field inside QR payload without valid secret signature.
- **Input**: Submit tampered pairing token to handshake endpoint.
- **Observed Behavior**: HMAC signature check fails; server returns `HTTP 401 Unauthorized`.
- **Assertion**: `Assert.AreEqual(HttpStatusCode.Unauthorized, response.StatusCode);`

#### TC-T2-064: Extreme Clock Skew Between Pairing Devices
- **Boundary Condition**: Client device clock is set 15 minutes ahead of Server device clock.
- **Input**: Perform pairing when client `currentTime` > server `exp` timestamp.
- **Observed Behavior**: Server validates token against server's clock, accepts skew within 5-minute grace window or rejects with clock drift alert.
- **Assertion**: `Assert.IsTrue(responseHandledCorrectly);`

#### TC-T2-065: 20 Simultaneous QR Pairing Requests
- **Boundary Condition**: 20 handheld devices scan server QR code simultaneously.
- **Input**: 20 parallel HTTP POST requests to `/api/v1/pair/handshake`.
- **Observed Behavior**: Server issues 20 distinct valid session tokens; all 20 devices complete pairing in < 1.0s total.
- **Assertion**: `Assert.AreEqual(20, sessionTokens.Distinct().Count());`

---

### Feature 14: LAN P2P HTTP NDJSON Streaming (R4 / M4)

#### TC-T2-066: Export Stream Query with `since` Timestamp in Future
- **Boundary Condition**: Client requests export stream with `since = 2524608000` (year 2050).
- **Input**: `GET /api/v1/sync/export-stream?entity=products&since=2524608000`.
- **Observed Behavior**: Server returns 200 OK with empty NDJSON body (0 records); zero server exception.
- **Assertion**: `Assert.AreEqual(0, returnedLines.Length);`

#### TC-T2-067: Cable Disconnection Mid 10,000 Record Export Stream
- **Boundary Condition**: Network connection severs at line 5,000 during 10,000 record streaming export.
- **Input**: Abort socket connection mid-stream.
- **Observed Behavior**: Server catches socket write exception, terminates stream worker task, reclaims memory buffers.
- **Assertion**: `Assert.IsFalse(serverStreamWorker.IsAlive);`

#### TC-T2-068: Import Stream Containing Duplicate Primary Key Payloads
- **Boundary Condition**: Incoming NDJSON stream contains product ID `"P-500"` which already exists in local database.
- **Input**: Stream NDJSON payload with existing primary key `"P-500"`.
- **Observed Behavior**: Import engine applies upsert policy (updates existing record `"P-500"`), preserves primary key integrity.
- **Assertion**: `Assert.AreEqual(1, db.Products.Count(p => p.Id == "P-500"));`

#### TC-T2-069: SQL Injection & Script Payloads inside NDJSON Stream
- **Boundary Condition**: NDJSON stream fields contain SQL injection strings (`' DROP TABLE Products; --`) and XSS tags (`<script>alert(1)</script>`).
- **Input**: Stream NDJSON with malicious payload strings.
- **Observed Behavior**: EF Core parameterized queries store text literally; Web UI escapes HTML tags safely; zero code execution.
- **Assertion**: `Assert.IsNotNull(db.Products.FirstOrDefault(p => p.Name.Contains("DROP TABLE")));`

#### TC-T2-070: Stream Import Processing Under 1 MB Free Disk Space
- **Boundary Condition**: Target device has only 1 MB remaining disk space before import starts.
- **Input**: Import 10,000 record stream on low-disk device.
- **Observed Behavior**: Import engine checks available disk space before transaction commit; aborts import safely if space < required buffer.
- **Assertion**: `Assert.IsTrue(importAbortedSafely);`

---

### Feature 15: Multi-Branch Inventory Schema (R5 / M5)

#### TC-T2-071: Transfer Quantity Exceeding Source Branch Stock
- **Boundary Condition**: Transfer request specifies quantity = 100 for product with source branch stock = 15.
- **Input**: Create stock transfer with quantity 100 from BR-01 (stock 15).
- **Observed Behavior**: Validation error thrown "الكمية المطلوبة غير متوفرة في الفرع المصدر", transfer creation rejected.
- **Assertion**: `Assert.IsFalse(transferCreated);`

#### TC-T2-072: Foreign Key Constraint Violation on Branch Deletion
- **Boundary Condition**: Attempting to hard-delete `Branch` record with existing linked sales and inventory.
- **Input**: `dbContext.Branches.Remove(activeBranch); dbContext.SaveChanges();`.
- **Observed Behavior**: System prevents hard deletion (FK constraint), enforces soft-delete (`is_active = false`).
- **Assertion**: `Assert.IsTrue(activeBranch.IsDisabled);`

#### TC-T2-073: Concurrent Status Update on Same Transfer Record
- **Boundary Condition**: Branch Manager A approves transfer (`SHIPPED`) at same instant Cashier B cancels transfer (`CANCELLED`).
- **Input**: Two concurrent HTTP requests updating status of `StockTransfer` ID `"TR-100"`.
- **Observed Behavior**: Optimistic concurrency control (row version / `concurrency_token`) allows first update, rejects second with concurrency conflict.
- **Assertion**: `Assert.IsTrue(conflictDetected);`

#### TC-T2-074: Extreme Transfer Payload (1,000 Line Items in 1 Transfer)
- **Boundary Condition**: Single stock transfer containing 1,000 distinct product items.
- **Input**: Create `StockTransfer` with 1,000 items in `items_json`.
- **Observed Behavior**: Schema handles JSON payload, updates branch inventory for all 1,000 items in single atomic transaction.
- **Assertion**: `Assert.AreEqual(1000, transferItemsCount);`

#### TC-T2-075: Offline Transaction Driving Branch Stock Below Zero
- **Boundary Condition**: Offline POS checkout sells 5 units of product with local recorded stock = 2.
- **Input**: Process offline sale of 5 units (stock becomes -3).
- **Observed Behavior**: System allows transaction offline (negative stock allowed for uninterrupted checkout), flags `is_negative_stock = true` for audit reconciliation.
- **Assertion**: `Assert.AreEqual(-3, branchStock.Quantity); Assert.IsTrue(branchStock.IsNegativeStockFlagged);`

---

### Feature 16: Device Management & Heartbeats (R5 / M5)

#### TC-T2-076: Duplicate Device ID Conflict from Cloned System Image
- **Boundary Condition**: Two physical terminals cloned from same disk image send heartbeats with identical `deviceId = "DEV-CLONE-01"` but different IP addresses.
- **Input**: Heartbeat ping from IP 192.168.1.10, followed by IP 192.168.1.11 with same device ID.
- **Observed Behavior**: Device registry flags conflict, appends IP suffix (`"DEV-CLONE-01_192.168.1.11"`), alerts admin of duplicate device ID.
- **Assertion**: `Assert.IsTrue(deviceRegistry.HasDuplicateAlert);`

#### TC-T2-077: Malicious Heartbeat Flood Attack (1,000 Pings / Sec)
- **Boundary Condition**: Compromised IoT device floods `/api/v1/devices/heartbeat` with 1,000 pings per second.
- **Input**: Send 1,000 heartbeat HTTP POST requests in 1 second.
- **Observed Behavior**: Rate limiting middleware throttles device pings to max 1 ping per 5 seconds per device ID; HTTP 429 returned for flooded requests.
- **Assertion**: `Assert.AreEqual(HttpStatusCode.TooManyRequests, floodResponse.StatusCode);`

#### TC-T2-078: Client System Clock Ahead of Server Time
- **Boundary Condition**: Mobile handheld clock is set 2 hours ahead (`serverTime + 2 hours`).
- **Input**: Send heartbeat with client timestamp in future.
- **Observed Behavior**: Server overrides client timestamp with authoritative server UTC time; database records server time.
- **Assertion**: `Assert.IsTrue((DateTime.UtcNow - device.LastSeen).TotalSeconds < 5);`

#### TC-T2-079: Rapid Device Network Flapping (Online <-> Offline every 2s)
- **Boundary Condition**: Wi-Fi signal dropping every 2 seconds causing constant state toggling.
- **Input**: Send heartbeat -> miss heartbeat -> send heartbeat repeatedly.
- **Observed Behavior**: Hysteresis buffer requires 3 consecutive missed heartbeats before marking device `OFFLINE`, preventing notification flap.
- **Assertion**: `Assert.AreEqual("ONLINE", device.Status); // maintained during brief flap`

#### TC-T2-080: Large Scale Device Registry (500 Active Terminals)
- **Boundary Condition**: Admin monitoring 500 active terminals across 20 store branches.
- **Input**: Process heartbeats from 500 devices simultaneously.
- **Observed Behavior**: Heartbeat processor updates in-memory cache and batches DB writes; response time remains < 25ms per ping.
- **Assertion**: `Assert.IsTrue(pingDurationMs < 25);`

---

### Feature 17: Unified Multi-Branch Admin (R5 / M5)

#### TC-T2-081: Rendering Multi-Branch Matrix with 50 Branches & 10,000 Products
- **Boundary Condition**: Enterprise database with 50 store branches and 10,000 inventory product SKUs.
- **Input**: Open Multi-Branch Admin Inventory Matrix.
- **Observed Behavior**: View utilizes virtualized data grid scrolling; initial load completes in < 1.2s; memory consumption < 180 MB.
- **Assertion**: `Assert.IsTrue(loadTimeMs < 1200);`

#### TC-T2-082: Concurrent Multi-Manager Transfer Approval Race
- **Boundary Condition**: Manager A approves transfer on WPF Desktop while Manager B approves same transfer on Web PWA at exact same second.
- **Input**: Simultaneous approval submission for transfer `TR-888`.
- **Observed Behavior**: First approval succeeds; second approval receives notice "تم اعتماد الطلب بالفعل" without duplicating stock adjustments.
- **Assertion**: `Assert.AreEqual(1, stockAdjustmentEventsCount);`

#### TC-T2-083: Real-Time User Permission Revocation Mid-Session
- **Boundary Condition**: Admin revokes `Manager` role while user is currently viewing Multi-Branch Admin Panel.
- **Input**: Update user role to `Cashier` in database while user session is active.
- **Observed Behavior**: Next admin API call or heartbeat check detects role change, displays session expired modal, redirects user to POS sales view.
- **Assertion**: `Assert.IsTrue(redirectedToPos);`

#### TC-T2-084: Zero Sales Aggregate Calculations Across All Branches
- **Boundary Condition**: New fiscal day start with 0 transactions recorded across all branches.
- **Input**: Generate aggregated sales analytics report for today.
- **Observed Behavior**: Report outputs total sales $0.00, average transaction $0.00, transaction count 0 without divide-by-zero exception.
- **Assertion**: `Assert.AreEqual(0.00m, report.AverageTransactionValue);`

#### TC-T2-085: Multi-Branch Admin Dashboard Reload Under 100% CPU Load
- **Boundary Condition**: Host machine running background video export causing 100% CPU utilization.
- **Input**: Refresh Multi-Branch Admin Dashboard under CPU stress.
- **Observed Behavior**: Async loading tasks complete without timing out; UI renders progressive skeleton loader until data binds.
- **Assertion**: `Assert.IsTrue(adminPage.IsFullyBound);`

---

### Feature 18: E2E Test Suite (Tiers 1-4) (M0 / M0)

#### TC-T2-086: Executing Test Suite on Minimal Single-Core CPU VM
- **Boundary Condition**: Running E2E test suite inside a restricted CI container with 1 vCPU and 2 GB RAM.
- **Input**: Execute `dotnet test` in single-core environment.
- **Observed Behavior**: Test runner adjusts concurrency parallelism to 1, completes all test cases successfully without timeout.
- **Assertion**: `Assert.AreEqual(180, totalPassedTests);`

#### TC-T2-087: Firewall Blocking Kestrel Port 5050 During E2E Run
- **Boundary Condition**: Local OS firewall blocks inbound TCP traffic on port 5050.
- **Input**: Run network integration tests when port 5050 is firewalled.
- **Observed Behavior**: Test harness detects network socket block, outputs clear diagnostic "Port 5050 firewalled", fails test gracefully with remediation instructions.
- **Assertion**: `Assert.IsTrue(testOutput.Contains("firewall"));`

#### TC-T2-088: Test Case Timeout Kill (Test Executing > 30 Seconds)
- **Boundary Condition**: Individual test case enters infinite loop during execution.
- **Input**: Run test case configured with `[Timeout(30000)]` containing an infinite loop.
- **Observed Behavior**: Test runner terminates hung test case after exactly 30,000ms, records `TimeoutException`, continues executing remaining test suite.
- **Assertion**: `Assert.AreEqual(1, failedTestsCount); Assert.IsTrue(remainingTestsExecuted);`

#### TC-T2-089: Parallel Execution of 2 E2E Test Suite Instances
- **Boundary Condition**: Two CI pipelines execute test suite simultaneously on same host machine.
- **Input**: Launch two concurrent instances of `dotnet test`.
- **Observed Behavior**: Each test instance generates unique isolated temp DB names (`smartpos_test_GUID1.db`, `smartpos_test_GUID2.db`) and dynamic server ports.
- **Assertion**: `Assert.AreEqual(0, testCollisionsCount);`

#### TC-T2-090: Write-Protected Test Output Report Directory
- **Boundary Condition**: Output directory for `test_results.xml` is set to read-only.
- **Input**: Complete test run when output folder is read-only.
- **Observed Behavior**: Test runner catches `UnauthorizedAccessException`, falls back to writing report to `%TEMP%/test_results.xml`, logs fallback location.
- **Assertion**: `Assert.IsTrue(File.Exists(fallbackXmlPath));`

## Section 4: Tier 3 — Cross-Feature Interaction Test Specifications (TC-T3-001 to TC-T3-015)

This tier defines pairwise and multi-feature interaction specifications across Requirements R1 to R5.

### TC-T3-001: Multi-Mode Sync Config (F1) + Outbox Queue (F2)
- **Objective**: Verify switching mode from `Offline` to `Hybrid` immediately activates Outbox Queue drainer and flushes pending mutations.
- **Participating Modules**: `ISyncConfigEngine`, `SyncEngineService`, `sync_outbox` table.
- **Execution Steps**:
  1. Operating in `Offline` mode, generate 10 offline POS sales (10 rows in `sync_outbox` with `status="PENDING"`).
  2. Invoke `ISyncConfigEngine.SwitchMode(SyncMode.Hybrid)`.
  3. Monitor outbox worker activity.
- **Expected Results**: Sync worker wakes up immediately upon mode change event, pushes all 10 outbox items to cloud API, updates status to `SYNCED`.
- **Assertion**: `Assert.AreEqual(0, db.Outbox.Count(o => o.Status == "PENDING")); Assert.AreEqual(10, db.Outbox.Count(o => o.Status == "SYNCED"));`

### TC-T3-002: Embedded Kestrel HTTP (F3) + Fast QR Pairing (F13)
- **Objective**: Verify Fast QR pairing handshake request (`POST /api/v1/pair/handshake`) is processed and authenticated by Kestrel port 5050 server.
- **Participating Modules**: `KestrelEmbeddedServer`, `FastPairService`, Auth Middleware.
- **Execution Steps**:
  1. Start Embedded Kestrel server on port 5050.
  2. Generate signed pairing token payload.
  3. Send HTTP POST to `http://localhost:5050/api/v1/pair/handshake` with `Authorization: Bearer <token>`.
- **Expected Results**: Kestrel routes request to pairing controller, validates signature, returns HTTP 200 with `SessionToken`.
- **Assertion**: `Assert.AreEqual(HttpStatusCode.OK, response.StatusCode); Assert.IsNotNull(sessionToken);`

### TC-T3-003: Web PWA Dexie v9 (F6) + Product/Stocktake UX (F7)
- **Objective**: Verify completing a stock audit in Web PWA writes record to Dexie IndexedDB `history_logs` and queues item in Dexie `sync_outbox`.
- **Participating Modules**: Web PWA Stocktake Component, Dexie v9 (`db.history_logs`, `db.sync_outbox`).
- **Execution Steps**:
  1. Open Stocktake view in Web PWA.
  2. Enter physical count variance for product "P-101".
  3. Click Submit Stock Audit.
- **Expected Results**: Dexie inserts record into `history_logs` and enqueues audit mutation into `sync_outbox` for background sync.
- **Assertion**: `Assert.AreEqual(1, await db.history_logs.count()); Assert.AreEqual(1, await db.sync_outbox.count());`

### TC-T3-004: Scoped DbContext Factory (F9) + SQLite WAL Mode (F10)
- **Objective**: Verify multiple scoped DbContexts executing parallel read/write transactions under SQLite WAL mode do not throw database locked exceptions.
- **Participating Modules**: `IDbContextFactory<AppDbContext>`, SQLite WAL Engine.
- **Execution Steps**:
  1. Spin up 5 parallel tasks: Task 1 writes POS sale, Task 2 queries products `.AsNoTracking()`, Task 3 writes device heartbeat, Task 4 updates stock, Task 5 reads audit logs.
  2. Await `Task.WhenAll`.
- **Expected Results**: All 5 operations complete within 200ms with zero `SQLite Error 5 (database locked)` errors.
- **Assertion**: `Assert.IsTrue(tasks.All(t => t.IsCompletedSuccessfully));`

### TC-T3-005: Scanner Lifecycle (F12) + WPF UI Unmanaged Leaks (F11)
- **Objective**: Verify barcode scanner events received while navigating between camera view and reports view release OpenCV handles cleanly.
- **Participating Modules**: `WeakReferenceMessenger`, OpenCV Camera Service, WPF View Models.
- **Execution Steps**:
  1. Open Camera Scanner View -> trigger barcode scan message.
  2. Navigate to Reports View (chart view).
  3. Trigger another barcode scan message.
- **Expected Results**: Camera handle released on navigation; scan message handled by active view without leaking native camera or chart handles.
- **Assertion**: `Assert.IsFalse(cameraService.IsCapturing); Assert.AreEqual(0, leakedHandleCount);`

### TC-T3-006: Fast QR Pairing (F13) + P2P NDJSON Streaming (F14)
- **Objective**: Verify handheld device uses session token from Fast QR pairing to authenticate subsequent NDJSON P2P stream import request.
- **Participating Modules**: `FastPairScanner`, `KestrelEmbeddedServer`, NDJSON Stream Engine.
- **Execution Steps**:
  1. Pair client device using Fast QR payload -> receive `SessionToken`.
  2. Attach `Bearer <SessionToken>` to HTTP request `GET /api/v1/sync/export-stream?entity=products`.
- **Expected Results**: Server validates session token and streams 10,000 product NDJSON records over socket.
- **Assertion**: `Assert.AreEqual(HttpStatusCode.OK, streamResponse.StatusCode); Assert.AreEqual("application/x-ndjson", contentType);`

### TC-T3-007: Multi-Branch Inventory Schema (F15) + Device Management & Heartbeats (F16)
- **Objective**: Verify device heartbeats carrying `branchId` update active device count per branch in Multi-Branch schema.
- **Participating Modules**: `ConnectedDevice` Schema, Heartbeat Endpoint, `Branch` Entity.
- **Execution Steps**:
  1. Dispatch heartbeat from device `"DEV-01"` with `branchId = "BR-NORTH"`.
  2. Query `Branch` device statistics for `"BR-NORTH"`.
- **Expected Results**: Active device count for `"BR-NORTH"` increments to reflect online terminal.
- **Assertion**: `Assert.AreEqual(1, branchStats.ActiveDeviceCount);`

### TC-T3-008: Unified Multi-Branch Admin (F17) + Multi-Branch Inventory Schema (F15)
- **Objective**: Verify Admin user approving a stock transfer in Multi-Branch Admin Panel updates source and destination `BranchStock` records atomically.
- **Participating Modules**: MultiBranchAdminViewModel, `StockTransferService`, `BranchStock` Table.
- **Execution Steps**:
  1. Admin opens Transfer Approvals tab in Admin Panel.
  2. Click "Approve Transfer" for transfer `TR-555` (quantity 50 from BR-01 to BR-02).
- **Expected Results**: Transfer status transitions to `SHIPPED`; BR-01 stock decreases by 50; BR-02 stock increases by 50 in single transaction.
- **Assertion**: `Assert.AreEqual("SHIPPED", transfer.Status); Assert.AreEqual(150, br1Stock); Assert.AreEqual(100, br2Stock);`

### TC-T3-009: PIN Setup (F8) + Compose UX Styling (F4)
- **Objective**: Verify PIN authentication screen is styled according to Material 3 Compose blue color tokens (#0061A4, #001C3B).
- **Participating Modules**: PIN Keypad View Component, Compose CSS Theme (`compose-theme.css`).
- **Execution Steps**:
  1. Load PIN authentication screen in Web PWA.
  2. Inspect computed styling of PIN keypad buttons and background backdrop.
- **Expected Results**: Keypad buttons styled with `#0061A4` primary background; backdrop matches `#001C3B` dark surface token.
- **Assertion**: `Assert.AreEqual("#0061a4", keypadBtnColor); Assert.AreEqual("#001c3b", backdropColor);`

### TC-T3-010: RobovaiAdDialog (F8) + E2E Test Suite (F18)
- **Objective**: Verify automated E2E test runner handles `RobovaiAdDialog` non-skippable 5s countdown without failing UI interaction flow.
- **Participating Modules**: `RobovaiAdDialog`, E2E Test Harness UI Driver.
- **Execution Steps**:
  1. E2E test runner triggers user login action causing `RobovaiAdDialog` to display.
  2. Test harness awaits ad countdown completion (5,000ms) before clicking close button.
- **Expected Results**: Test runner detects ad modal, waits 5s for close button activation, dismisses ad, and proceeds with checkout test steps.
- **Assertion**: `Assert.IsTrue(testCompletedSuccessfully); Assert.IsFalse(adModalVisible);`

### TC-T3-011: Mobile Bottom Nav (F5) + Scanner Lifecycle (F12)
- **Objective**: Verify switching tabs via mobile bottom nav automatically unregisters barcode scanner messenger listeners.
- **Participating Modules**: Mobile BottomNav Component, Barcode Scanner Messenger (`WeakReferenceMessenger`).
- **Execution Steps**:
  1. Open POS View (scanner registered).
  2. Tap "Settings" bottom nav item.
  3. Dispatch `BarcodeScannedMessage`.
- **Expected Results**: Navigation unregisters scanner listener; Settings view ignores barcode scan message; zero unintended processing.
- **Assertion**: `Assert.IsFalse(isMessengerRegistered); Assert.AreEqual(0, settingsHandledScansCount);`

### TC-T3-012: SQLite WAL (F10) + Outbox Queue (F2)
- **Objective**: Verify background outbox queue drainer writing `status="SYNCED"` does not block POS cashier thread writing new sale to SQLite DB under WAL mode.
- **Participating Modules**: `SyncEngineService`, `MainPOSViewModel`, SQLite WAL Engine.
- **Execution Steps**:
  1. Background sync worker starts bulk update of 100 outbox records to `SYNCED`.
  2. Cashier clicks "Complete Sale" on POS checkout thread at same instant.
- **Expected Results**: POS sale transaction completes in < 30ms without waiting for outbox bulk update lock.
- **Assertion**: `Assert.IsTrue(saleDurationMs < 50); Assert.IsTrue(posSaleCompleted);`

### TC-T3-013: Fast QR Pairing (F13) + Multi-Branch Admin (F17)
- **Objective**: Verify newly paired mobile device via Fast QR code automatically appears in Admin Control Panel device list.
- **Participating Modules**: `FastPairService`, `ConnectedDevice` Registry, MultiBranchAdminViewModel.
- **Execution Steps**:
  1. Mobile handheld completes Fast QR pairing handshake with Kestrel server.
  2. Admin user refreshes Connected Devices grid in Multi-Branch Admin Panel.
- **Expected Results**: Newly paired device appears in device registry table with `Status="ONLINE"` and correct `BranchId`.
- **Assertion**: `Assert.IsTrue(adminDeviceGrid.Any(d => d.DeviceId == pairedDeviceId));`

### TC-T3-014: Dexie.js v9 (F6) + Outbox Queue (F2)
- **Objective**: Verify Web PWA offline mutation written to Dexie `sync_outbox` triggers Service Worker background sync tag when online.
- **Participating Modules**: Dexie v9, `sw.js` Sync Manager, Web PWA Sync Client.
- **Execution Steps**:
  1. Web PWA creates product offline (inserted into Dexie `sync_outbox`).
  2. Network connection restored (`navigator.onLine = true`).
- **Expected Results**: Service Worker `sync` event fires, reads pending Dexie outbox items, streams push payload to server.
- **Assertion**: `Assert.AreEqual(0, await db.sync_outbox.where("status").equals("PENDING").count());`

### TC-T3-015: Scoped DbContext Factory (F9) + Device Heartbeats (F16)
- **Objective**: Verify device heartbeat background service creating scoped DbContexts every 30s does not cause memory leak over 1,000 heartbeat cycles.
- **Participating Modules**: `DeviceMonitoringService`, `IDbContextFactory<AppDbContext>`.
- **Execution Steps**:
  1. Execute 1,000 simulated device heartbeat pings through `DeviceMonitoringService`.
  2. Force GC collection and measure working set memory.
- **Expected Results**: All 1,000 scoped DbContexts disposed cleanly; working set memory growth < 1 MB.
- **Assertion**: `Assert.IsTrue(memoryDeltaMb < 1.0);`

---

## Section 5: Tier 4 — Real-World Application E2E Scenarios (TC-T4-001 to TC-T4-008)

This tier defines end-to-end multi-branch, offline/online transition, and disaster recovery workflow test specifications.

### TC-T4-001: End-to-End Multi-Branch Stock Replenishment Workflow
- **Scenario Description**: Complete lifecycle of inter-branch inventory replenishment across 2 branch locations.
- **Preconditions**: Headquarters "BR-HEAD" has 500 units of Product "P-COFFEE"; Branch "BR-ALEX" has 10 units.
- **Execution Workflow**:
  1. **Step 1 (Draft Creation)**: Inventory Clerk at BR-ALEX opens Stock Dispatch view and creates a transfer request for 100 units from BR-HEAD (`status="DRAFT"`).
  2. **Step 2 (Submission)**: Clerk submits request (`status="PENDING"`).
  3. **Step 3 (Admin Approval)**: Admin at BR-HEAD opens Central Admin Control Panel, reviews pending transfer `TR-9001`, clicks "Approve Transfer" (`status="SHIPPED"`).
  4. **Step 4 (LAN Sync / Transmission)**: Kestrel embedded server streams transfer payload over LAN/Cloud to BR-ALEX node.
  5. **Step 5 (Reception & Stock Adjust)**: Store Manager at BR-ALEX receives shipment, verifies count, clicks "Confirm Receive" (`status="RECEIVED"`).
- **Expected Results**:
  - `StockTransfer` state transitions: `DRAFT` -> `PENDING` -> `SHIPPED` -> `RECEIVED`.
  - BR-HEAD stock for "P-COFFEE" becomes 400 (500 - 100).
  - BR-ALEX stock for "P-COFFEE" becomes 110 (10 + 100).
  - Detailed audit log entries recorded at both branches with timestamps and user IDs.
- **Assertion**: `Assert.AreEqual(400, brHeadStock); Assert.AreEqual(110, brAlexStock); Assert.AreEqual("RECEIVED", finalTransferStatus);`

### TC-T4-002: Offline Store Operation & Automated Cloud Re-synchronization
- **Scenario Description**: Full day offline retail store operation followed by internet connectivity restoration and background cloud sync.
- **Preconditions**: POS terminal configured in `Hybrid` mode. Internet connection physically disconnected at 08:00 AM.
- **Execution Workflow**:
  1. **Step 1 (Offline Sales)**: Cashiers execute 250 sales transactions, 15 customer creations, and 5 returns offline.
  2. **Step 2 (Local Storage Integrity)**: All transactions stored locally in SQLite / Dexie IndexedDB; 270 mutation items enqueued in `sync_outbox` with `status="PENDING"`.
  3. **Step 3 (Internet Restoration)**: At 05:00 PM, network connectivity is restored (`IsCloudReachable = true`).
  4. **Step 4 (Automated Background Sync)**: `SyncEngineService` detects network event, triggers outbox queue drainer worker.
  5. **Step 5 (Cloud Reconciliation)**: Outbox engine pushes 270 items in chunked HTTP batches to Cloud REST API.
- **Expected Results**:
  - All 270 outbox records transition from `PENDING` to `SYNCED`.
  - Cloud database record counts match local terminal counts exactly.
  - Zero duplicate sales or double-counted stock deductions.
  - Terminal operational performance remained uninterrupted throughout offline period.
- **Assertion**: `Assert.AreEqual(0, outboxPendingCount); Assert.AreEqual(250, cloudSalesCount); Assert.AreEqual(15, cloudCustomersCount);`

### TC-T4-003: Multi-Device Instant QR Pairing & High-Capacity Inventory Streaming
- **Scenario Description**: Setting up a new mobile PWA handheld terminal by scanning WPF desktop QR code and streaming 10,000 product records over local Wi-Fi.
- **Preconditions**: WPF Desktop host running Kestrel server on port 5050 with 10,000 product SKUs; mobile device un-paired.
- **Execution Workflow**:
  1. **Step 1 (QR Generation)**: WPF Desktop displays `fast-pair-v2` QR code containing endpoint `http://192.168.1.120:5050` and signed token (~180 bytes).
  2. **Step 2 (QR Scan & Handshake)**: Mobile PWA camera scans QR code (< 1s), extracts token, sends `POST /api/v1/pair/handshake` request (< 200ms).
  3. **Step 3 (Authentication)**: Server verifies HMAC signature, returns `SessionToken` `"SESS-998811"`.
  4. **Step 4 (NDJSON P2P Stream Request)**: Mobile PWA requests `GET /api/v1/sync/export-stream?entity=products` with Bearer header.
  5. **Step 5 (Stream Ingestion)**: Kestrel streams 10,000 product lines via chunked NDJSON stream to mobile Dexie IndexedDB.
- **Expected Results**:
  - Pairing handshake completes in < 1.0 second total.
  - 10,000 product records stream and insert into mobile Dexie.js database in < 1.5 seconds.
  - Mobile PWA shows "Pairing Complete - 10,000 Products Synced".
- **Assertion**: `Assert.IsTrue(pairingTimeMs < 1000); Assert.IsTrue(streamTimeMs < 1500); Assert.AreEqual(10000, mobileDexieProductsCount);`

### TC-T4-004: Continuous 24-Hour POS Terminal Uptime & Concurrency Stress
- **Scenario Description**: Simulating continuous 24-hour retail terminal operations under heavy transaction load to verify memory stability and lock immunity.
- **Preconditions**: WPF Desktop app running in simulated high-load test mode.
- **Execution Workflow**:
  1. **Step 1 (Transaction Loop)**: Execute 10,000 checkout sales transactions continuously.
  2. **Step 2 (Hardware Messages)**: Dispatch 10,000 `BarcodeScannedMessage` events through messenger pipeline.
  3. **Step 3 (UI Graphics Updates)**: LiveCharts sales graph redraws every 5 seconds.
  4. **Step 4 (Database Access)**: Background threads perform concurrent `IDbContextFactory` reads/writes under SQLite WAL mode.
  5. **Step 5 (GC Compaction Worker)**: `GcCompactionService` executes background compaction every 15 minutes.
- **Expected Results**:
  - Zero application crashes, UI freezes, or unhandled exceptions.
  - Process working set memory remains stable at <= 250 MB (no memory bloat).
  - Zero `SQLite Error 5 (database locked)` lock errors recorded.
  - Chart paint handles and camera handles show zero unmanaged handle leaks.
- **Assertion**: `Assert.IsTrue(workingSetMb <= 250); Assert.AreEqual(0, dbLockErrorCount); Assert.AreEqual(10000, salesCompletedCount);`

### TC-T4-005: Central Multi-Branch Monitoring & Real-Time Security RBAC Override
- **Scenario Description**: Central Administrator monitoring branch operations and executing real-time role revocation on a compromised terminal.
- **Preconditions**: Admin logged in on WPF Central Admin Control Panel; Cashier logged in on Branch 2 Web PWA terminal.
- **Execution Workflow**:
  1. **Step 1 (Dashboard Monitoring)**: Admin views aggregated sales ($45,000 across 4 branches) and active connected devices (8 terminals `ONLINE`).
  2. **Step 2 (Anomaly Detection)**: Admin notices unusual transaction frequency on terminal `"DEV-BR2-03"`.
  3. **Step 3 (Permission Revocation)**: Admin selects user `"cashier_br2"` in Admin Panel and changes role to `Disabled`.
  4. **Step 4 (Real-Time Push)**: Admin server broadcasts permission update signal to branch devices.
  5. **Step 5 (Terminal Lockout)**: Branch 2 Web PWA receives security update, immediately terminates active cashier session, locks screen with message "تم إلغاء صلاحية الحساب".
- **Expected Results**:
  - Admin Panel reflects device status and sales metrics in real-time.
  - Permission modification takes effect immediately across network.
  - Unauthorized checkout attempts on revoked terminal are blocked and audited.
- **Assertion**: `Assert.IsTrue(revokedSessionTerminated); Assert.AreEqual("Disabled", targetUser.Role);`

### TC-T4-006: PWA Cross-Platform Mobile Experience (iOS Safari / Android Chrome)
- **Scenario Description**: End-to-end verification of Web PWA modern Jetpack Compose UX, bottom navigation, and offline Dexie storage on mobile devices.
- **Preconditions**: `smart-inventory-pro` hosted on PWA web server; mobile viewport simulated (iOS Safari & Android Chrome).
- **Execution Workflow**:
  1. **Step 1 (Launch & Manifest)**: Launch PWA URL in standalone display mode; Service Worker `sw.js` activates and caches static shell.
  2. **Step 2 (Bento Dashboard)**: Render Bento Grid dashboard using Material 3 blue tokens (#0061A4, #001C3B, #F8F9FF).
  3. **Step 3 (Touch Navigation)**: Tap 4-tab bottom navigation bar (`Home` -> `Products` -> `Invoices` -> `Settings`) with touch targets >= 48x48dp.
  4. **Step 4 (Offline Checkout)**: Disconnect device Wi-Fi/cellular connection; create sales transaction in offline PWA mode.
  5. **Step 5 (Dexie v9 Persistence)**: Transaction written to Dexie v9 IndexedDB (`products`, `transactions`, `sync_outbox`).
- **Expected Results**:
  - PWA installs and launches standalone with native mobile app feel.
  - Touch dialogs and page transitions execute smoothly at 60 FPS without layout shift.
  - Offline transactions persist reliably in Dexie IndexedDB and sync automatically when reconnected.
- **Assertion**: `Assert.IsTrue(pwaStandaloneActive); Assert.IsTrue(offlineTransactionSavedInDexie);`

### TC-T4-007: Sudden Power Loss & SQLite WAL Disaster Recovery
- **Scenario Description**: Recovering database integrity and transaction state after sudden hard power outage during active checkout write.
- **Preconditions**: POS app executing 500 simultaneous database writes under SQLite WAL mode.
- **Execution Workflow**:
  1. **Step 1 (Power Outage Simulation)**: Hard kill POS process (`kill -9`) at step 250 of 500 write operations.
  2. **Step 2 (State Inspection)**: `smartpos.db` main file and `.db-wal` Write-Ahead Log file remain on disk in un-checkpointed state.
  3. **Step 3 (System Reboot)**: Restart POS application service.
  4. **Step 4 (WAL Recovery)**: EF Core / SQLite engine opens database connection, detects uncommitted WAL transactions, executes automatic WAL recovery.
  5. **Step 5 (Integrity Check)**: Execute `PRAGMA integrity_check;` and verify sales record count.
- **Expected Results**:
  - SQLite WAL recovery completes cleanly in < 100ms upon restart.
  - `PRAGMA integrity_check` returns `"ok"` (zero database corruption).
  - All fully committed sales (first 249) are intact; incomplete write (250) rolled back cleanly.
- **Assertion**: `Assert.AreEqual("ok", integrityCheckResult); Assert.IsTrue(committedSalesCount == 249);`

### TC-T4-008: Heterogeneous Fleet Pairing & Network Partition Recovery
- **Scenario Description**: Multi-device fleet (1 WPF Desktop Host, 3 Web PWAs, 2 Mobile Handhelds) operating through network partition and merge.
- **Preconditions**: WPF Host acting as LAN Server on port 5050; 5 mobile terminals paired via Fast QR.
- **Execution Workflow**:
  1. **Step 1 (Partition Setup)**: Split network into 2 subnets (Subnet A: Host + 2 PWAs; Subnet B: 3 Mobile Handhelds offline).
  2. **Step 2 (Split-Brain Operations)**: Terminals on Subnet A sync live over LAN; terminals on Subnet B process sales offline in Dexie outbox.
  3. **Step 3 (Conflict Generation)**: Terminal on Subnet A and Terminal on Subnet B edit same product stock simultaneously.
  4. **Step 4 (Network Merge)**: Reconnect Subnet B to LAN; outbox engines trigger automatic synchronization.
  5. **Step 5 (CRDT / Timestamp Conflict Resolution)**: Sync engine evaluates entity timestamps/version GUIDs, applies deterministic last-write-wins resolution.
- **Expected Results**:
  - All 5 mobile terminals synchronize outbox queues post-merge without data loss.
  - Conflicting stock updates resolved deterministically with audit log entry detailing resolution.
  - Final inventory totals across all terminals match calculated net movements.
- **Assertion**: `Assert.AreEqual(0, allTerminalsPendingOutboxCount); Assert.IsTrue(inventoryTotalsInSync);`

---

## Section 6: `TEST_INFRA.md` Integration Snippet

Below is the formatted specification summary ready for direct inclusion into `TEST_INFRA.md`.

```markdown
# Kasher PRO POS & WMS Ecosystem — E2E Test Suite Specification (Milestone M0)

## Test Suite Architecture & Coverage Matrix

The E2E Test Suite provides 100% requirement-driven opaque-box coverage across all 18 features (Requirements R1–R5) spanning 4 execution tiers:

| Tier | Focus Area | Test Count Target | Scope & Methodology |
|------|------------|-------------------|---------------------|
| Tier 1 | Feature Coverage | >= 90 cases (5 per feature) | Verifies base functional requirements, interface contracts, and expected API/UI outputs for Features 1–18. |
| Tier 2 | Boundary & Corner Cases | >= 90 cases (5 per feature) | Validates edge conditions, unexpected inputs, race conditions, memory pressure, network drops, and database locks. |
| Tier 3 | Cross-Feature Interactions | 15 Integration Scenarios | Pairwise and multi-module integration testing across sync, storage, network streaming, UI, and admin control layers. |
| Tier 4 | Real-World E2E Workflows | 8 End-to-End Scenarios | Real-world multi-branch replenishment, 24-hour uptime stress, fast QR sync, PWA mobile offline experience, and disaster recovery. |

## Feature Mapping Reference Index

1. **Multi-Mode Sync Config Engine**: TC-T1-001..005, TC-T2-001..005, TC-T3-001, TC-T4-002
2. **Outbox Queue & Sync Engine**: TC-T1-006..010, TC-T2-006..010, TC-T3-001, TC-T3-012, TC-T3-014, TC-T4-002
3. **Embedded Kestrel HTTP Server**: TC-T1-011..015, TC-T2-011..015, TC-T3-002, TC-T3-006, TC-T4-003
4. **Compose UX Styling & Colors**: TC-T1-016..020, TC-T2-016..020, TC-T3-009, TC-T4-006
5. **Mobile Bottom Nav & Touch UI**: TC-T1-021..025, TC-T2-021..025, TC-T3-011, TC-T4-006
6. **Dexie.js v9 & Offline PWA**: TC-T1-026..030, TC-T2-026..030, TC-T3-003, TC-T3-014, TC-T4-006
7. **Product, Stocktake & Dispatch UX**: TC-T1-031..035, TC-T2-031..035, TC-T3-003, TC-T4-001
8. **PIN Setup & RobovaiAdDialog**: TC-T1-036..040, TC-T2-036..040, TC-T3-009, TC-T3-010
9. **Scoped DbContext Factory**: TC-T1-041..045, TC-T2-041..045, TC-T3-004, TC-T3-015, TC-T4-004
10. **SQLite WAL & Timeout Locks**: TC-T1-046..050, TC-T2-046..050, TC-T3-004, TC-T3-012, TC-T4-004, TC-T4-007
11. **Unmanaged Leaks & Deadlocks**: TC-T1-051..055, TC-T2-051..055, TC-T3-005, TC-T4-004
12. **Scanner Lifecycle & GC**: TC-T1-056..060, TC-T2-056..060, TC-T3-005, TC-T3-011, TC-T4-004
13. **Fast QR Pairing Protocol**: TC-T1-061..065, TC-T2-061..065, TC-T3-002, TC-T3-006, TC-T3-013, TC-T4-003
14. **LAN P2P HTTP NDJSON Streaming**: TC-T1-066..070, TC-T2-066..070, TC-T3-006, TC-T4-001, TC-T4-003
15. **Multi-Branch Inventory Schema**: TC-T1-071..075, TC-T2-071..075, TC-T3-007, TC-T3-008, TC-T4-001
16. **Device Management & Heartbeats**: TC-T1-076..080, TC-T2-076..080, TC-T3-007, TC-T3-015, TC-T4-005
17. **Unified Multi-Branch Admin**: TC-T1-081..085, TC-T2-081..085, TC-T3-008, TC-T3-013, TC-T4-001, TC-T4-005
18. **E2E Test Suite (Tiers 1-4)**: TC-T1-086..090, TC-T2-086..090, TC-T3-010, TC-T4-004, TC-T4-008

## Quality Gates & Verification Thresholds
- **Tier 1 & 2 Execution Pass Rate**: 100% required.
- **Tier 3 & 4 Workflow Pass Rate**: 100% required.
- **Maximum Execution Time**: Entire suite execution duration <= 180 seconds.
- **Memory Footprint Ceiling**: Maximum working set RAM during test execution <= 250 MB.
- **Concurrency Locks**: Zero `SQLite Error 5` occurrences.
```

---



