# Plan — RobovAI PRO POS & WMS Ecosystem Engineering Upgrade

## Overview
This plan governs the commercial-grade engineering upgrade for RobovAI PRO POS & WMS Ecosystem according to ORIGINAL_REQUEST.md.

## Phases & Strategy

### Phase 0: Survey & Project Mapping
- Dispatch 3 parallel Explorers:
  - Explorer 1: Map existing web app (`smart-inventory-pro`) structure, dependencies, state management, and Dexie/IndexedDB usage, plus analyze Android UX reference (`C:\Users\shaban\Downloads\robovai-wms`).
  - Explorer 2: Map WPF app (`src/SmartPOS.WPF`), EF Core DbContext lifetime, SQLite usage, threading, scanner/video preview handles, live charts.
  - Explorer 3: Survey network/sync mechanisms, LAN/Cloud sync requirements, QR pairing architecture, and multi-branch admin control requirements across WPF and Web.
- Aggregate findings into `PROJECT.md` at `f:\Raw\kasher\kasher\PROJECT.md`.

### Phase 1: Decomposition & Track Setup
- Define interface contracts, code layout, feature inventory, and milestone milestones:
  - M0: E2E Testing Track & Test Infrastructure (`TEST_INFRA.md`)
  - M1: R1 - Hybrid Online/Offline Architecture & Configuration Engine
  - M2: R2 - Web PWA Modernization & Cross-Platform iOS/Android Experience
  - M3: R3 - Desktop WPF Memory Leak & Database Lock Resolution
  - M4: R4 - High-Capacity LAN Sync & Fast QR Pairing Engine
  - M5: R5 - Central Multi-Branch & Device Admin Control Panel
  - M6: Final Integration & E2E Validation

### Phase 2: Parallel Track Execution
- Spawn E2E Testing Track Orchestrator.
- Spawn Sub-Orchestrators for independent milestones.

### Phase 3: Final E2E Test Suite Validation & Verification
- Verify 100% pass rate on E2E test suite.
- Audit for code integrity & completeness.
