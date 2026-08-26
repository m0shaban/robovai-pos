# Repository Structure & Test Setup Analysis — Milestone M0

## Executive Summary
This document provides a comprehensive investigation of the **RobovAI PRO POS & WMS Ecosystem** repository structure (`f:\Raw\kasher\kasher`), existing test capabilities, available runtimes, dependencies, and automated test execution strategies for Milestone M0 (E2E Testing Track).

---

## 1. Directory Layout & Architecture

The repository contains a dual-track codebase combining a C# .NET 8 WPF Desktop system and a modern JavaScript/Vite Web PWA system:

```
f:\Raw\kasher\kasher\
├── .agents/                    # Agent work folders, plans, and briefs
│   ├── explorer_m0_1/          # Current Explorer workspace
│   ├── sub_orch_m0/            # Milestone M0 Scope & planning
│   ├── ORIGINAL_REQUEST.md     # System requirements (R1-R5)
│   └── ...
├── src/                        # WPF Desktop .NET 8 C# Source Code
│   ├── SmartPOS.Core/          # Core Domain Entities & Interfaces (.NET 8.0)
│   ├── SmartPOS.Infrastructure/# EF Core, SQLite DB, Kestrel Embedded Server (.NET 8.0-windows)
│   ├── SmartPOS.Application/   # MVVM ViewModels, Services, QR Generator (.NET 8.0-windows)
│   ├── SmartPOS.WPF/           # WPF XAML Desktop UI Application (.NET 8.0-windows10)
│   ├── SmartPOS.UnitTests/     # xUnit Test Project (.NET 8.0-windows)
│   └── TestDb/                 # Console DB Seed & Migration Tool (.NET 8.0-windows)
├── smart-inventory-pro/        # Cross-Platform Web PWA Application
│   ├── index.html              # Single Page Application entry point
│   ├── package.json            # Node dependencies (Vite, Dexie, Playwright, etc.)
│   ├── css/                    # Custom CSS styling (styles.css)
│   ├── js/                     # Client application logic (app.js, db.js, qr-sync.js, etc.)
│   ├── test-*.cjs              # Existing Playwright automation & test scripts
│   └── node_modules/           # Installed packages (Playwright v1.59.1, Vite v6.4.2)
├── scripts/                    # Utility scripts (.ps1, .py for licensing/cleanup)
├── tools/                      # Licensing & Branding tools (.NET C# projects)
├── PROJECT.md                  # Project Architecture & Feature Inventory
└── SmartPOS.sln                # Master Visual Studio Solution
```

### Detailed Component Inventory

| Component Path | Type / Framework | Purpose | Key Dependencies |
|---|---|---|---|
| `src/SmartPOS.Core` | Class Library (.NET 8.0) | Domain Entities & Interfaces | None |
| `src/SmartPOS.Infrastructure` | Class Library (.NET 8.0-windows) | Data Access, SQLite, Kestrel Server | `Microsoft.EntityFrameworkCore.Sqlite` v8.0.26, `QuestPDF` |
| `src/SmartPOS.Application` | Class Library (.NET 8.0-windows) | Business Logic, ViewModels | `CommunityToolkit.Mvvm`, `QRCoder`, `OpenCvSharp4` |
| `src/SmartPOS.WPF` | WinExe (.NET 8.0-windows10) | WPF Desktop Application UI | `MaterialDesignThemes`, `LiveChartsCore.SkiaSharpView.WPF` |
| `src/SmartPOS.UnitTests` | Test Project (.NET 8.0-windows) | C# Unit & Integration Tests | `xunit` v2.9.3, `Moq` v4.20.72, `Microsoft.NET.Test.Sdk` |
| `smart-inventory-pro` | Node.js PWA (Vite ES Module) | Web/Mobile PWA Application | `dexie` v4.2.1, `vite` v6.4.2, `playwright` v1.59.1 |

---

## 2. Test Runners & Harnesses Evaluation

Investigation confirmed that the system environment has all required runtimes installed and verified:

| Runtime / Tool | Verified Version | Execution Command | Suitability & Role |
|---|---|---|---|
| **.NET SDK** | `10.0.203` (Target `.NET 8.0`) | `dotnet test src/SmartPOS.UnitTests/SmartPOS.UnitTests.csproj` | Native C# unit & integration tests (xUnit v2.9.3). Direct verification of EF Core, DbContextFactory, SQLite WAL, Kestrel HTTP endpoints, and SyncOutbox. |
| **Node.js Test Runner** | `v24.14.1` | `node --test` | Lightweight, native JS unit testing with zero external dependencies. Tests core JS modules (sync engine, NDJSON streaming, QR token parser, Dexie schema logic). |
| **Playwright** | `v1.59.1` (Headless Chromium) | `npx playwright test` or `node <script>.cjs` | E2E browser automation for `smart-inventory-pro`. Simulates user interactions on PWA, tests IndexedDB persistence, touch navigation, and API handshakes. |
| **Vite Dev/Preview Server** | `v6.4.2` | `npx vite` / `npx vite preview` | Hosts `smart-inventory-pro` locally on port 3000 / 4173 for Playwright E2E testing. |
| **Master Test Harness Script** | Node.js / PowerShell | Custom script execution | Orchestrates multi-tier execution (starting servers, executing `dotnet test`, running Playwright & Node unit tests, aggregating results). |

### Key Test Infrastructure Verification Findings
1. `dotnet test` successfully compiled and executed 9 xUnit tests in `SmartPOS.UnitTests`.
2. `npm run build` in `smart-inventory-pro` succeeded cleanly in 20.8s.
3. Playwright (`v1.59.1`) runs Chromium in headless mode without missing browser dependency issues.
4. `node --test` runs natively in Node.js v24.14.1 with `node:test` and `node:assert` modules.

---

## 3. Automated Test Execution Methodology & Setup

To execute the test suite automatically in this environment:

### A. Environment Requirements
- **OS**: Windows x64
- **Node.js**: v24.14.1 (with npm 11.11.0)
- **.NET SDK**: 10.0.203 (Targeting net8.0-windows)
- **Port Allocation**:
  - `5050`: WPF Embedded Kestrel HTTP Server
  - `3000` / `4173`: Vite Web PWA Dev / Preview Server

### B. Standard Test Execution Commands
1. **Run .NET Backend Tests**:
   ```powershell
   dotnet test src/SmartPOS.UnitTests/SmartPOS.UnitTests.csproj
   ```
2. **Build Web PWA**:
   ```powershell
   cd smart-inventory-pro
   npm run build
   ```
3. **Start Local Web PWA Server**:
   ```powershell
   cd smart-inventory-pro
   npm run preview
   ```
4. **Run Node.js / Playwright E2E Tests**:
   ```powershell
   cd smart-inventory-pro
   node --test tests/**/*.test.js
   ```

---

## 4. Test Tier Mapping for Requirement Coverage (R1-R5)

The E2E test suite for M0 will cover all 18 feature items across 4 structured tiers:

- **Tier 1 (Feature Coverage)**: >=5 tests per feature (Target: 90+ tests across 18 features). Validates basic functionality of config engine, outbox queue, Kestrel endpoints, PWA navigation, Dexie IndexedDB, SQLite WAL, QR pairing, NDJSON streaming, and Admin panel.
- **Tier 2 (Boundary & Corner Cases)**: >=5 tests per feature (Target: 90+ tests). Validates edge cases such as malformed QR tokens, empty payloads, database timeouts, network disconnects/reconnects, high concurrency, and invalid auth headers.
- **Tier 3 (Cross-Feature Interactions)**: Pairwise integration tests (e.g., Kestrel HTTP API + Dexie Sync, Outbox Queue + Fast QR Pairing, WAL mode + Concurrent Scoped DbContext).
- **Tier 4 (Real-World Application Scenarios)**: Full E2E workflows simulating 24-hour transactions, multi-branch stock transfers, and P2P LAN data streaming.

---

## Conclusion
The codebase infrastructure fully supports a combined testing framework using **xUnit (`dotnet test`)** for backend .NET architecture and **Playwright + Node.js test runner (`node --test`)** for Web PWA end-to-end testing.
