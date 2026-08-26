# Handoff Report: Network, Sync, Fast QR Pairing, and Multi-Branch Admin Survey

**Agent**: Explorer 3 (`teamwork_preview_explorer`)  
**Target Path**: `f:\Raw\kasher\kasher`  
**Date**: 2026-08-08  
**Detailed Survey File**: `f:\Raw\kasher\kasher\.agents\explorer_3\analysis.md`  

---

## 1. Observation

### Codebase Inspection & Direct Findings

1. **WPF Desktop Architecture & Data Models (`src/SmartPOS.Core/Entities/`)**:
   - SQLite EF Core context (`AppDbContext.cs:1-150`) manages entities: `Product`, `Category`, `Sale`, `SaleDetail`, `User`, `Permissions`, `Customer`, `Supplier`, `PurchaseOrder`, `Expense`, `AuditLog`, `StockMovement`, `Shift`, `RentalDevice`, `RentalSession`, `Table`, `AppSetting`.
   - **Absence of Branch & Device Entities**: Core entities currently lack `Branch`, `BranchStock`, and `ConnectedDevice` entities (only `RentalDevice` exists for gaming rooms).
   - **Absence of Embedded HTTP Server**: `App.xaml.cs:1-352` builds Microsoft Extensions Host with SQLite transient context, services, and ViewModels. No background ASP.NET Core Kestrel HTTP listener or web server daemon is registered.

2. **WMS Web PWA Architecture (`smart-inventory-pro/js/`)**:
   - IndexedDB via Dexie.js (`db.js:1-805`) manages tables: `products`, `transactions`, `destinations`, `users`, `suppliers`, `branches`, `damages`, `audit_logs`, `kits`, `transfers`.
   - `firebase.js:1-482` implements optional cloud persistence with Firestore and Firebase Auth (`fbSyncProducts`, `fbSyncTransactions`, `fbPullProducts`, `fbRegisterAccount`, `fbLoginByUsername`).
   - `qr-sync.js:1-1049` manages QR data exports/imports and device pairing.

3. **Current QR Bridge & Payload Bottleneck**:
   - `WmsQrBridgeViewModel.cs:49-99` generates `pos-pair-v1` Base64 deep link: `https://pos.robovai.tech/wms/?pair={b64}`.
   - `WmsQrBridgeViewModel.cs:188-193` & `qr-sync.js:66-74` serialize bulk data directly into optical QR images. Both modules truncate datasets to **max 10–12 items** if the JSON payload exceeds **2,700–2,900 characters**.

---

## 2. Logic Chain

1. **Premise 1**: Requirements **R1** and **R4** require high-capacity LAN data transfer for 10,000+ records and multi-mode (Offline/Online/Hybrid) operation.
2. **Observation**: Current data transfer relies entirely on camera-based optical QR scanning where full JSON data is encoded in the QR code.
3. **Inference 1**: Optical QR codes cannot store large datasets (physical QR size limits to ~2.9KB max). Truncating datasets to 12 records makes syncing 10,000 products impossible via QR images (requires ~830 manual camera scans).
4. **Observation**: WPF desktop app has no embedded HTTP web server listening for LAN requests, and no streaming endpoint exists.
5. **Inference 2**: To achieve high-speed P2P LAN sync (R4), an ASP.NET Core Kestrel HTTP server must be embedded directly inside `SmartPOS.WPF` listening on a designated port (e.g., `http://0.0.0.0:5050`), allowing mobile devices to pair via a small signed QR token (`fast-pair-v2`) and stream data over HTTP NDJSON.
6. **Observation**: WPF entities lack `Branch` and `ConnectedDevice` models, while WMS has a basic `branches` table without cross-branch stock or transfer flows.
7. **Inference 3**: Requirement **R5** requires extending both C# EF Core models and JS Dexie schemas with `Branch`, `BranchStock`, `StockTransfer`, and `ConnectedDevice`, accompanied by a central admin dashboard in WPF and WMS.

---

## 3. Caveats

1. **Firewall & Network Binding**:
   - Hosting an embedded Kestrel HTTP server on `0.0.0.0:5050` under Windows desktop requires Windows Defender Firewall rule authorization (prompting the user during first launch or requiring installer firewall rule creation).
2. **Firebase Auth Limitations**:
   - Soft-deleting sub-users in `firebase.js:310-317` sets `active: false` in Firestore because hard-deleting Firebase Auth users requires the Node.js Firebase Admin SDK.
3. **Scope Constraints**:
   - As an Explorer agent, all analysis and code blueprints have been documented in `analysis.md` without modifying project source code outside `.agents/explorer_3/`.

---

## 4. Conclusion

1. **Current System Assessment**:
   - The ecosystem features solid offline capabilities in WPF (SQLite) and WMS (Dexie.js), alongside a working QR auth card and basic deep-link device pairing. However, optical QR data payloads are bottlenecked to 10-12 records, and no background HTTP service exists in WPF for P2P network streaming.

2. **Actionable Implementation Plan**:
   - **R1**: Implement `SyncConfig` configuration engine and background outbox queue (`sync_outbox`) for seamless Offline, Online, and Hybrid sync.
   - **R4**: Embed ASP.NET Core Kestrel HTTP server in WPF on port 5050. Upgrade QR protocol to `fast-pair-v2` (ephemeral signed handshake token ~180 bytes) and build chunked HTTP NDJSON streaming endpoints (`/api/v1/sync/export-stream`), enabling 10,000+ records to sync in < 1.5 seconds over Wi-Fi.
   - **R5**: Extend WPF EF Core & WMS Dexie schemas with `Branch`, `BranchStock`, `StockTransfer`, and `ConnectedDevice`, providing real-time multi-location inventory, device health heartbeats, and unified RBAC enforcement.

---

## 5. Verification Method

### Independent Verification Steps for Future Implementer Agents:

1. **Verify Report Location & Completeness**:
   - View `f:\Raw\kasher\kasher\.agents\explorer_3\analysis.md` to confirm detailed schemas, API specifications, and code blueprints.

2. **Verify Code Locations Cited**:
   - Inspect `f:\Raw\kasher\kasher\src\SmartPOS.Application\ViewModels\WmsQrBridgeViewModel.cs` lines 49–230 to verify pairing payload logic and 2,700-character truncation rules.
   - Inspect `f:\Raw\kasher\kasher\smart-inventory-pro\js\qr-sync.js` lines 44–116 and lines 855–1048 to verify WMS pairing & optical QR handling.
   - Inspect `f:\Raw\kasher\kasher\smart-inventory-pro\js\db.js` schema versions (lines 27–105) and `firebase.js` (lines 346–415).

3. **Verify Implementation Feasibility**:
   - Open solution `SmartPOS.sln` using `dotnet build` to confirm compilation readiness for embedded Kestrel HTTP server package additions.
