# Original User Request

## Initial Request — 2026-08-08T05:48:38Z

Complete commercial-grade engineering upgrade for **RobovAI PRO POS & WMS Ecosystem** including Hybrid Online/Offline Architecture, Desktop Memory & Database Fixes, Cross-Platform Web PWA (iOS + Android), Fast QR Pairing + LAN Sync, and Multi-Branch Central Admin Control.

Working directory: f:\Raw\kasher\kasher
Integrity mode: development

## Requirements

### R1. Hybrid Online/Offline Architecture & Configuration Engine
Implement a configurable multi-mode sync engine supporting:
- **Offline Mode**: Local LAN Server hosting (WPF embedded HTTP API or local Node service) with SQLite / Dexie.js for pure intranet deployment.
- **Online Mode**: Cloud REST/GraphQL API hosted on Vercel/Render with PostgreSQL/Firebase for cloud-first multi-site deployment.
- **Hybrid Mode**: Local LAN for ultra-fast offline transactions + automatic background synchronization with Cloud when internet becomes available.

### R2. Web PWA Modernization & Cross-Platform iOS/Android Experience
Refactor `smart-inventory-pro` to incorporate all UI/UX strengths of the Android Compose app (`C:\Users\shaban\Downloads\robovai-wms`), ensuring:
- Native mobile feel on iOS (Safari PWA) and Android (Chrome PWA) with a responsive bottom navigation bar, touch-optimized dialogs, and smooth transitions.
- Offline-first IndexedDB persistence via Dexie.js.
- Feature parity: Products, Inbound/Outbound, Stocktake, Branches, Transactions, and Customer/Supplier management.

### R3. Desktop WPF Memory Leak & Database Lock Resolution
Fix performance degradation and freezes in `src/SmartPOS.WPF` after long uptime:
- Re-architect EF Core `DbContext` lifetime from long-lived to short-lived scoped/factory pattern.
- Enable SQLite Write-Ahead Logging (`PRAGMA journal_mode=WAL;`) and set `BusyTimeout = 30000ms` to prevent database locks.
- Eliminate WPF UI thread deadlocks from live charts, scanner threads, and video preview handles.
- Implement automated background GC compaction and change tracker cleanup.

### R4. High-Capacity LAN Sync & Fast QR Pairing Engine
Overhaul data transfer between devices:
- Replace bulk QR data encoding with **Fast QR Pairing**: QR contains only signed connection endpoint and auth handshake token.
- Implement high-speed P2P/LAN HTTP payload streaming for transferring unlimited products, invoices, and stock data across local network devices.

### R5. Central Multi-Branch & Device Admin Control Panel
Add a unified Multi-Branch Admin dashboard accessible from both Web and WPF Desktop:
- Manage multiple store branches, track stock levels across locations, configure branch-specific pricing/tax.
- Monitor active connected devices (terminals, scanners, handhelds), view sync status, and manage user roles/permissions centrally.

## Acceptance Criteria

### Performance & Uptime
- [ ] WPF Desktop app operates continuously for 24+ hours under simulated high transaction load without memory bloat, UI freezing, or SQLite database locks.
- [ ] Web PWA passes Lighthouse PWA audit and installs smoothly on iOS and Android devices with native-like UX.

### Multi-Mode Sync & Networking
- [ ] System seamlessly switches between Offline LAN, Online Cloud, and Hybrid modes based on config.
- [ ] Fast QR Pairing connects devices in < 2 seconds and transfers 10,000+ product records over LAN in seconds without data loss.

### Branch & Admin Operations
- [ ] Multi-Branch Admin dashboard successfully displays multi-location inventory, device health, and aggregated sales analytics.
