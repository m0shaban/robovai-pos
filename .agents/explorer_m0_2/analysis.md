# Analysis Report: API Endpoints, Data Schemas & Programmatic E2E Testing Strategy (M0 Track)

**Agent ID**: explorer_m0_2  
**Working Directory**: `f:\Raw\kasher\kasher\.agents\explorer_m0_2`  
**Date**: 2026-08-08  

---

## 1. Executive Summary & Scope Mapping

This analysis provides a comprehensive mapping of interface contracts, HTTP API endpoints, database schemas (SQLite and Dexie.js v9 IndexedDB), UI/UX component specifications, and programmatic verification strategies for all **18 features** defined in `PROJECT.md` under **M0 (E2E Testing Track)** for the **RobovAI PRO POS & WMS Ecosystem**.

---

## 2. Interface Contracts & HTTP API Specifications

The dual-track architecture relies on an embedded Kestrel HTTP server in WPF Desktop listening on `http://0.0.0.0:5050` to service P2P LAN communication and Web PWA interactions.

### 2.1 Endpoint: `POST /api/v1/pair/handshake`
* **Purpose**: Fast QR pairing handshake protocol (`fast-pair-v2`). Exchanging signed QR token for an active session token.
* **Headers**:
  * `Authorization: Bearer <fast_pair_token>` (signed QR token payload)
  * `Content-Type: application/json`
* **Request Body**:
```json
{
  "deviceId": "DEV-MOBILE-9821",
  "deviceName": "Samsung Galaxy Tab POS",
  "deviceType": "PWA_MOBILE",
  "appVersion": "v2.0.0"
}
```
* **Response Body** (`200 OK`):
```json
{
  "status": "OK",
  "sessionToken": "sess_eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9...",
  "serverTime": "2026-08-08T09:15:00Z",
  "expiresAt": "2026-08-09T09:15:00Z"
}
```
* **Error Responses**:
  * `401 Unauthorized`: Invalid/tampered HMAC signature or expired QR timestamp (> 300s).
  * `400 Bad Request`: Missing mandatory fields (`deviceId`, `deviceName`).

---

### 2.2 Endpoint: `GET /api/v1/sync/export-stream`
* **Purpose**: LAN P2P chunked HTTP NDJSON data streaming for incremental or full data export.
* **Query Parameters**:
  * `entity`: `products` | `transactions` | `branches` | `stock_transfers` (required)
  * `since`: ISO 8601 UTC timestamp string (optional, for incremental sync)
  * `batchSize`: integer (optional, default 1000)
* **Headers**:
  * `Authorization: Bearer <sessionToken>`
  * `Accept: application/x-ndjson`
* **Response Content-Type**: `application/x-ndjson`
* **Stream Payload Example** (line-delimited JSON objects):
```ndjson
{"robovai_sync_id":"550e8400-e29b-41d4-a716-446655440000","barcode":"622123456789","name":"كوكاكولا 330مل","sellingPrice":15.00,"stock":150.0,"updatedAt":"2026-08-08T08:00:00Z"}
{"robovai_sync_id":"670e8400-e29b-41d4-a716-446655440001","barcode":"622987654321","name":"شيبس عائلي","sellingPrice":25.00,"stock":85.0,"updatedAt":"2026-08-08T08:05:00Z"}
```
* **Performance Benchmark Target**: 10,000 product records transferred in < 1.5 seconds over standard local Wi-Fi / Ethernet LAN.

---

### 2.3 Endpoint: `POST /api/v1/sync/import-stream`
* **Purpose**: High-speed HTTP payload streaming for importing NDJSON records into local SQLite database.
* **Headers**:
  * `Authorization: Bearer <sessionToken>`
  * `Content-Type: application/x-ndjson`
* **Request Body**: Stream of line-delimited JSON items.
* **Response Body** (`200 OK`):
```json
{
  "status": "OK",
  "importedCount": 10000,
  "failedCount": 0,
  "serverTime": "2026-08-08T09:15:02Z"
}
```

---

### 2.4 Endpoint: `POST /api/v1/devices/heartbeat`
* **Purpose**: Periodic device status ping and monitoring endpoint.
* **Headers**:
  * `Authorization: Bearer <sessionToken>`
  * `Content-Type: application/json`
* **Request Body**:
```json
{
  "deviceId": "DEV-MOBILE-9821",
  "deviceName": "Samsung Galaxy Tab POS",
  "type": "PWA_MOBILE",
  "branchId": "BR-MAIN-01",
  "appVersion": "v2.0.0",
  "batteryLevel": 88
}
```
* **Response Body** (`200 OK`):
```json
{
  "acknowledged": true,
  "serverTime": "2026-08-08T09:15:00Z",
  "configVersion": "1.0.4"
}
```

---

## 3. Database Schemas & Data Model Contracts

### 3.1 `sync_outbox` Schema (SQLite & Dexie.js)
Transactional queue tracking pending offline changes to guarantee consistency across multi-mode deployments.

| Field Name | Data Type | Nullable | Description / Constraints |
|------------|-----------|----------|---------------------------|
| `id` | GUID / String | No | Primary Key |
| `entity_type` | String(50) | No | Target entity (`Product`, `Sale`, `StockTransfer`, etc.) |
| `entity_id` | String(100) | No | Entity primary key / GUID |
| `operation` | String(20) | No | `INSERT` \| `UPDATE` \| `DELETE` |
| `payload_json` | Text | No | Serialized JSON payload of entity state |
| `created_at` | ISO String | No | UTC creation timestamp |
| `synced_at` | ISO String | Yes | UTC timestamp when push/pull succeeded |
| `status` | String(20) | No | `PENDING` \| `SYNCED` \| `FAILED` |
| `retry_count` | Integer | No | Count of failed synchronization attempts |

---

### 3.2 Multi-Branch Schemas

#### `Branch` Schema
```sql
CREATE TABLE Branches (
    Id TEXT PRIMARY KEY,
    Code VARCHAR(50) NOT NULL UNIQUE,
    Name VARCHAR(200) NOT NULL,
    Address VARCHAR(300),
    Phone VARCHAR(50),
    TaxNumber VARCHAR(50),
    IsActive INTEGER NOT NULL DEFAULT 1,
    CreatedAt VARCHAR(50) NOT NULL,
    IsDeleted INTEGER NOT NULL DEFAULT 0
);
```

#### `BranchStock` Schema
```sql
CREATE TABLE BranchStocks (
    Id TEXT PRIMARY KEY,
    BranchId TEXT NOT NULL FOREIGN KEY REFERENCES Branches(Id),
    ProductId INTEGER NOT NULL FOREIGN KEY REFERENCES Products(Id),
    StockQuantity REAL NOT NULL DEFAULT 0,
    MinStockLevel REAL NOT NULL DEFAULT 0,
    BranchSellingPrice REAL,
    LastAuditDate VARCHAR(50),
    IsDeleted INTEGER NOT NULL DEFAULT 0
);
```

#### `StockTransfer` Schema
```sql
CREATE TABLE StockTransfers (
    Id TEXT PRIMARY KEY,
    SourceBranchId TEXT NOT NULL,
    TargetBranchId TEXT NOT NULL,
    Status VARCHAR(30) NOT NULL, -- PENDING, IN_TRANSIT, COMPLETED, CANCELLED
    TransferDate VARCHAR(50) NOT NULL,
    ItemsJson TEXT NOT NULL,
    Notes TEXT,
    IsDeleted INTEGER NOT NULL DEFAULT 0
);
```

---

### 3.3 Device Management Schema

#### `ConnectedDevice` Schema
```sql
CREATE TABLE ConnectedDevices (
    Id TEXT PRIMARY KEY,
    DeviceId VARCHAR(100) NOT NULL UNIQUE,
    DeviceName VARCHAR(100) NOT NULL,
    DeviceType VARCHAR(50) NOT NULL, -- POS_WPF, PWA_MOBILE, SCANNER
    BranchId TEXT,
    AppVersion VARCHAR(30),
    LastHeartbeat VARCHAR(50) NOT NULL,
    Status VARCHAR(30) NOT NULL, -- ONLINE, OFFLINE, UNPAIRED
    IPAddress VARCHAR(50)
);
```

---

### 3.4 Dexie.js v9 Web PWA IndexedDB Schemas (`smart-inventory-pro/js/db.js`)
* `products`: `++id, barcode, name, category, sync_status, robovai_sync_id, location_code, batch_number, expiry_date`
* `transactions`: `++id, type, date, sync_status, robovai_sync_id`
* `destinations`: `++id, name`
* `users`: `++id, username, password_hash, role, cloud_uid`
* `suppliers`: `++id, name, phone`
* `branches`: `++id, code, name`
* `damages`: `++id, barcode, date`
* `audit_logs`: `++id, entity, entity_id, date`
* `kits`: `++id, barcode, name`
* `transfers`: `++id, date, status`
* `history_logs`: `++id, type, timestamp`
* `app_prefs`: `key, value`
* `sync_outbox`: `++id, entity_type, entity_id, operation, created_at, status`

---

## 4. UI Component Specifications & Design Tokens

### 4.1 Bento Grid Dashboard
* **Layout**: 4-column responsive grid container with fixed padding (16px) and gaps (12px).
* **Metrics Cards**:
  1. *Total Sales Today* (Primary accent card `#0061A4`)
  2. *Active Inventory Count* (Secondary container)
  3. *Sync Queue Status* (Badge: `SYNCED` / `PENDING`)
  4. *Connected Devices Counter* (Live heartbeat indicator)

### 4.2 Jetpack Compose Theme Design Tokens
* `Primary`: `#0061A4` (Deep Compose Blue)
* `OnPrimary / Navy Container`: `#001C3B`
* `Background / Surface Light`: `#F8F9FF` (Soft Light Blue Tint)
* `Surface Variant`: `#DFE2EB`
* `Secondary / Muted Text`: `#535F70`

### 4.3 Mobile Bottom Navigation Bar
* **Tabs (4 Fixed Routes)**:
  1. `Dashboard` (`/dashboard` - icon `layout-dashboard`)
  2. `Products` (`/products` - icon `package`)
  3. `Stock Control` (`/stock` - icon `arrow-down-up`)
  4. `Settings` (`/settings` - icon `settings`)
* **UX Rules**: Touch target height >= 48px, active icon indicator pill, smooth slide transition on view swap.

### 4.4 `fast-pair-v2` Fast QR Token
* **Payload String**: `fast-pair-v2|http://192.168.1.50:5050|DEV-DESK-01|ts:1788772500|sig:a4f8...` (~180 bytes)
* **HMAC Verification**: Signed using device master secret via HMAC-SHA256. Validated in < 2 seconds.

### 4.5 `RobovaiAdDialog` Interstitial Modal
* **Behavior**: Displays on app startup or post-transaction.
* **Rules**: 5-second mandatory countdown timer. Skip button enabled only after timer reaches 0. Daily cap (max 3 impressions per day) stored in `localStorage` key `robovai_ad_daily_count`.

---

## 5. 18 Features Programmatic E2E Testing Strategy Matrix

| # | Feature Name | Test Harness / Verification Strategy | Programmatic Assertions |
|---|--------------|--------------------------------------|------------------------|
| 1 | **Multi-Mode Sync Config Engine** | Node.js API test script + config override harness | Assert config load (`Offline`/`Online`/`Hybrid`), verify dynamic endpoint fallback when cloud connection fails |
| 2 | **Outbox Queue & Sync Engine** | SQLite DB harness + Dexie Playwright script | Query `sync_outbox` table -> verify record added on offline edit, verify `status` updates to `SYNCED` on push |
| 3 | **Embedded Kestrel HTTP Server** | `fetch`/`axios` HTTP test script against port 5050 | Send parallel GET/POST requests -> assert response `200 OK`, latency < 50ms, header `Server: Kestrel` |
| 4 | **Compose UX Styling & Colors** | DOM Computed Style Parser / Playwright style inspector | Assert CSS variables: `--primary-color: #0061A4`, `--bg-color: #F8F9FF`, container `#001C3B` |
| 5 | **Mobile Bottom Nav & Touch UI** | Playwright Touch Emulation (`Mobile Chrome`/`Mobile Safari`) | Click 4 bottom nav items -> assert route transition, 48px touch bounding box, active CSS class |
| 6 | **Dexie.js v9 & Offline PWA** | Playwright IndexedDB Inspector + Network Offline Mode | Assert Dexie version === 9, check presence of all 13 object stores, verify app load in offline mode (`sw.js`) |
| 7 | **Product, Stocktake & Dispatch UX** | Headless Browser DOM Interaction Script | Select category chips, test unit dropdown (`قطعة`, `كرتونة`), verify math calculations for stock adjustments |
| 8 | **PIN Setup & RobovaiAdDialog** | Playwright UI Automation Script | Set 4-digit PIN -> verify sha256 hash in storage; Trigger Ad modal -> verify 5s countdown & daily cap increment |
| 9 | **Scoped DbContext Factory** | .NET xUnit / Integration Test Harness | Invoke parallel threads creating `IDbContextFactory<AppDbContext>` -> assert clean disposal and `.AsNoTracking()` |
| 10 | **SQLite WAL & Timeout Locks** | `sqlite3` CLI & EF Core Concurrency Test Script | Run `PRAGMA journal_mode;` (assert `wal`), `PRAGMA busy_timeout;` (assert `30000`), execute 10 concurrent writes |
| 11 | **Unmanaged Leaks & Deadlocks** | Windows Process Performance Monitor / CLI Memory Profiler | Execute 1,000 UI chart renders & video handle cycles -> assert process Working Set growth < 25 MB |
| 12 | **Scanner Lifecycle & GC** | ViewModel Event Messenger Harness + GC Monitor Script | Trigger scanner message on route change -> verify listener clean-up; Invoke GC -> verify memory reduction |
| 13 | **Fast QR Pairing Protocol** | Node.js Crypto Test Script | Generate QR string -> assert size < 200 bytes; Tamper HMAC -> assert 401 error; Valid QR -> handshake in < 2s |
| 14 | **LAN P2P HTTP NDJSON Streaming** | Node.js Streaming HTTP Client (`http.request`) | Stream 10,000 NDJSON items via `GET /api/v1/sync/export-stream` -> assert total count = 10,000 & stream time < 1.5s |
| 15 | **Multi-Branch Inventory Schema** | EF Core & Dexie DB Integration Tests | Create branches A & B -> transfer stock -> assert stock in A decreases and stock in B increases atomically |
| 16 | **Device Management & Heartbeats** | HTTP Heartbeat Test Harness | POST `/api/v1/devices/heartbeat` -> verify `ConnectedDevice` record updated; simulate miss -> status becomes `OFFLINE` |
| 17 | **Unified Multi-Branch Admin** | Playwright Web & WPF UI Automated Inspector | Render Admin panel -> verify Branch stock overview table; Login as Worker -> assert Admin access denied (RBAC) |
| 18 | **E2E Test Suite (Tiers 1-4)** | Opaque-box Test Runner Harness (CLI / JUnit XML reporter) | Execute Tier 1 (Coverage), Tier 2 (Boundaries), Tier 3 (Interactions), Tier 4 (Scenarios) -> output 100% pass |

---

## 6. Recommendations for Test Runner Infrastructure

1. **API & Contract Harness**: Use Node.js `node:test` or `vitest` scripts targeting `http://127.0.0.1:5050` for HTTP API endpoints and NDJSON streaming tests.
2. **Database & Schema Verification**: Use `better-sqlite3` or `sqlite3` CLI for instant SQLite schema assertions and concurrency verification.
3. **Web PWA UX Verification**: Use `playwright` with mobile viewport configuration (`Pixel 7` / `iPhone 14`) for bottom nav, Bento grid, Ad modal, and Dexie v9 IndexedDB tests.
4. **C# WPF ViewModel & Memory Testing**: Use xUnit / NUnit test project targeting `.NET 8` for DbContextFactory, SQLite WAL, and GC compaction assertions.
