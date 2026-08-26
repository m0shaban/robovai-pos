# Scope: M0 — E2E Testing Track

## Architecture
- Requirement-driven opaque-box E2E test suite covering RobovAI PRO POS & WMS Ecosystem (R1-R5).
- Test Runner: Automated test scripts and harness capable of verifying embedded HTTP endpoints, SQLite/Dexie data structures, QR pairing token validation, multi-branch APIs, and PWA UX capabilities.
- Test Tiers:
  - Tier 1: Feature Coverage (>=5 tests per feature across R1-R5, target: 90+ tests for 18 features)
  - Tier 2: Boundary & Corner Cases (>=5 tests per feature across R1-R5, target: 90+ tests)
  - Tier 3: Cross-Feature Interactions (pairwise feature interactions)
  - Tier 4: Real-World Application Scenarios (multi-branch, P2P sync, offline/online transitions)

## Feature Inventory Mapping
1. Multi-Mode Sync Config Engine
2. Outbox Queue & Sync Engine
3. Embedded Kestrel HTTP Server
4. Compose UX Styling & Colors
5. Mobile Bottom Nav & Touch UI
6. Dexie.js v9 & Offline PWA
7. Product, Stocktake & Dispatch UX
8. PIN Setup & RobovaiAdDialog
9. Scoped DbContext Factory
10. SQLite WAL & Timeout Locks
11. Unmanaged Leaks & Deadlocks
12. Scanner Lifecycle & GC
13. Fast QR Pairing Protocol
14. LAN P2P HTTP NDJSON Streaming
15. Multi-Branch Inventory Schema
16. Device Management & Heartbeats
17. Unified Multi-Branch Admin
18. E2E Test Suite (Tiers 1-4)

## Milestones / Sub-tasks
| # | Name | Scope | Dependencies | Status |
|---|------|-------|-------------|--------|
| M0.1 | Discovery & Infra Mapping | Survey codebase, identify test framework, write TEST_INFRA.md | none | IN_PROGRESS |
| M0.2 | Tier 1 & 2 Test Suite | Implement automated feature coverage & boundary test cases | M0.1 | PLANNED |
| M0.3 | Tier 3 & 4 Test Suite | Implement cross-feature interaction & real-world scenario tests | M0.2 | PLANNED |
| M0.4 | Verification & TEST_READY.md | Execute full test suite, verify 100% pass, generate TEST_READY.md | M0.3 | PLANNED |
