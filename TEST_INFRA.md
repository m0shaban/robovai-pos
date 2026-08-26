# E2E Test Infra: RobovAI PRO POS & WMS Ecosystem Engineering Upgrade

## Test Philosophy
- **Requirement-Driven & Opaque-Box**: Tests evaluate public behavior, API contracts, database persistence, and UI specifications against user requirements (R1–R5) without relying on private implementation details.
- **Methodology**: 4-Tier design using Category-Partition Testing (Tier 1), Boundary Value Analysis & Edge Cases (Tier 2), Pairwise Combinatorial Testing (Tier 3), and Real-World Application Workload Scenarios (Tier 4).

## Feature Inventory & Test Coverage Goals

| # | Feature | Source | Tier 1 (Feature) | Tier 2 (Boundary) | Tier 3 (Cross) | Tier 4 (Scenario) |
|---|---------|--------|:----------------:|:-----------------:|:--------------:|:-----------------:|
| 1 | Multi-Mode Sync Config Engine | R1 | 5 cases (`TC-T1-001`..`005`) | 5 cases (`TC-T2-001`..`005`) | ✓ | ✓ |
| 2 | Outbox Queue & Sync Engine | R1 | 5 cases (`TC-T1-006`..`010`) | 5 cases (`TC-T2-006`..`010`) | ✓ | ✓ |
| 3 | Embedded Kestrel HTTP Server | R1 | 5 cases (`TC-T1-011`..`015`) | 5 cases (`TC-T2-011`..`015`) | ✓ | ✓ |
| 4 | Compose UX Styling & Colors | R2 | 5 cases (`TC-T1-016`..`020`) | 5 cases (`TC-T2-016`..`020`) | ✓ | ✓ |
| 5 | Mobile Bottom Nav & Touch UI | R2 | 5 cases (`TC-T1-021`..`025`) | 5 cases (`TC-T2-021`..`025`) | ✓ | ✓ |
| 6 | Dexie.js v9 & Offline PWA | R2 | 5 cases (`TC-T1-026`..`030`) | 5 cases (`TC-T2-026`..`030`) | ✓ | ✓ |
| 7 | Product, Stocktake & Dispatch UX | R2 | 5 cases (`TC-T1-031`..`035`) | 5 cases (`TC-T2-031`..`035`) | ✓ | ✓ |
| 8 | PIN Setup & RobovaiAdDialog | R2 | 5 cases (`TC-T1-036`..`040`) | 5 cases (`TC-T2-036`..`040`) | ✓ | ✓ |
| 9 | Scoped DbContext Factory | R3 | 5 cases (`TC-T1-041`..`045`) | 5 cases (`TC-T2-041`..`045`) | ✓ | ✓ |
| 10 | SQLite WAL & Timeout Locks | R3 | 5 cases (`TC-T1-046`..`050`) | 5 cases (`TC-T2-046`..`050`) | ✓ | ✓ |
| 11 | Unmanaged Leaks & Deadlocks | R3 | 5 cases (`TC-T1-051`..`055`) | 5 cases (`TC-T2-051`..`055`) | ✓ | ✓ |
| 12 | Scanner Lifecycle & GC | R3 | 5 cases (`TC-T1-056`..`060`) | 5 cases (`TC-T2-056`..`060`) | ✓ | ✓ |
| 13 | Fast QR Pairing Protocol | R4 | 5 cases (`TC-T1-061`..`065`) | 5 cases (`TC-T2-061`..`065`) | ✓ | ✓ |
| 14 | LAN P2P HTTP NDJSON Streaming | R4 | 5 cases (`TC-T1-066`..`070`) | 5 cases (`TC-T2-066`..`070`) | ✓ | ✓ |
| 15 | Multi-Branch Inventory Schema | R5 | 5 cases (`TC-T1-071`..`075`) | 5 cases (`TC-T2-071`..`075`) | ✓ | ✓ |
| 16 | Device Management & Heartbeats | R5 | 5 cases (`TC-T1-076`..`080`) | 5 cases (`TC-T2-076`..`080`) | ✓ | ✓ |
| 17 | Unified Multi-Branch Admin | R5 | 5 cases (`TC-T1-081`..`085`) | 5 cases (`TC-T2-081`..`085`) | ✓ | ✓ |
| 18 | E2E Test Suite (Tiers 1-4) | R1-R5 | 5 cases (`TC-T1-086`..`090`) | 5 cases (`TC-T2-086`..`090`) | ✓ | ✓ |
| **Total** | **All 18 Features** | | **90 Cases** | **90 Cases** | **15 Scenarios** | **8 Scenarios** |

## Test Architecture & Execution Framework

The test suite uses a dual-harness execution model matching the dual-track project architecture:

### 1. C# xUnit Test Harness (.NET 8.0)
- **Target Location**: `src/SmartPOS.UnitTests/`
- **Execution Command**: `dotnet test src/SmartPOS.UnitTests/SmartPOS.UnitTests.csproj`
- **Responsibility**:
  - `DbContextFactory` scoping and change tracker cleanups.
  - SQLite `PRAGMA journal_mode=WAL;` and `busy_timeout=30000;` lock verification.
  - WPF ViewModel memory lifecycle, live chart paint recycling, and camera handle disposal.
  - Embedded Kestrel HTTP server listeners (port 5050).

### 2. Node.js & Playwright E2E Test Harness
- **Target Location**: `smart-inventory-pro/tests/`
- **Execution Command**: `node --test tests/**/*.test.js` or `npx playwright test`
- **Responsibility**:
  - Web PWA UI/UX components (Bento grid, Compose Material 3 palette, bottom nav touch targets).
  - Dexie.js v9 IndexedDB schema & offline data persistence.
  - `fast-pair-v2` signed QR token generation & parsing (~180 bytes).
  - HTTP NDJSON P2P payload streaming clients (`/api/v1/sync/export-stream` and `/import-stream`).
  - Device heartbeat endpoints (`/api/v1/devices/heartbeat`) and Multi-Branch Admin UI.

## Tier 4 Real-World Application Scenarios

| # | Scenario ID | Description & Workflow | Features Exercised | Complexity |
|---|-------------|------------------------|--------------------|------------|
| 1 | `TC-T4-001` | Multi-Branch Inventory Replenishment & Stock Transfers across local & central nodes | F1, F2, F15, F17 | High |
| 2 | `TC-T4-002` | Continuous 24-Hour Transaction Load & GC Compaction Stress Test | F9, F10, F11, F12 | High |
| 3 | `TC-T4-003` | Fast QR Pairing & High-Speed LAN P2P NDJSON Streaming (10,000+ records) | F3, F13, F14 | High |
| 4 | `TC-T4-004` | Offline Outbox Transactional Queue Persistence & Cloud Recovery Sync | F1, F2, F6, F7 | High |
| 5 | `TC-T4-005` | SQLite WAL Mode & Concurrency Lock Timeout Stress Under Simultaneous Read/Write | F9, F10 | Medium |
| 6 | `TC-T4-006` | Jetpack Compose UX, Responsive Bento Grid, & Mobile Bottom Navigation | F4, F5, F6, F7, F8 | Medium |
| 7 | `TC-T4-007` | Connected Device Management, Heartbeat Pings, & Central Admin RBAC Controls | F16, F17 | Medium |
| 8 | `TC-T4-008` | Disaster Recovery & Emergency Local Server Fallback During Cloud Outage | F1, F3, F9, F10 | High |

## Minimum Coverage Thresholds
- **Tier 1 (Feature Coverage)**: Exactly 90 test cases (5 per feature).
- **Tier 2 (Boundary & Corner Cases)**: Exactly 90 test cases (5 per feature).
- **Tier 3 (Cross-Feature Interactions)**: Exactly 15 multi-module interaction scenarios.
- **Tier 4 (Real-World Application Scenarios)**: Exactly 8 full end-to-end application scenarios.
- **Total Suite Count**: 203 automated test cases/scenarios across Tiers 1-4.
