# BRIEFING — 2026-08-08T05:58:34Z

## Mission
Detailed technical investigation of Desktop WPF app (`src/SmartPOS.WPF`) memory leaks, UI thread deadlocks, SQLite database locks, EF Core DbContext lifetime, LiveCharts, camera/scanner handling, and background GC/ChangeTracker cleanup.

## 🔒 My Identity
- Archetype: Teamwork explorer (explorer_2)
- Roles: Read-only investigation, root-cause analysis, fix strategy proposal
- Working directory: f:\Raw\kasher\kasher\.agents\explorer_2
- Original parent: 6703759f-0ac0-49ba-8d30-4a7c00cd8907
- Milestone: Desktop WPF Memory & Database Lock Resolution (R3)

## 🔒 Key Constraints
- Read-only investigation — do NOT modify application source code directly
- Focus on `src/SmartPOS.WPF` and related database/infrastructure projects
- Output analysis report to `analysis.md` and handoff report to `handoff.md`

## Current Parent
- Conversation ID: 6703759f-0ac0-49ba-8d30-4a7c00cd8907
- Updated: 2026-08-08T05:58:34Z

## Investigation State
- **Explored paths**: `src/SmartPOS.WPF` (App.xaml.cs, MainWindow, POSPage, ReportsPage, SettingsPage), `src/SmartPOS.Infrastructure` (AppDbContext, DbInitializer, DatabasePathHelper, UnitOfWork, BarcodeService, BackupService), `src/SmartPOS.Application` (DashboardViewModel, MainPOSViewModel, ReportsViewModel, WmsQrBridgeViewModel, SettingsViewModel).
- **Key findings**:
  1. Long-lived `AppDbContext` instances held inside ViewModels cached in `MainWindow._pageCache`, causing massive EF Core `ChangeTracker` entity accumulation.
  2. SQLite default connection lacks `Busy Timeout=30000` and `PRAGMA journal_mode=WAL;` / `PRAGMA synchronous=NORMAL;`, causing `SQLite Error 5: 'database is locked'` during concurrent writes/reads.
  3. LiveCharts SkiaSharp paints (`SolidColorPaint`, `LinearGradientPaint`) allocated on every chart render without disposal; orphaned ViewModels on `ReportsPage` remain rooted in memory.
  4. Video Camera preview (`WmsQrBridgeViewModel`) allocates new `WriteableBitmap` every 80ms without buffer reuse and leaves unmanaged `VideoCapture` handles running if user navigates away.
  5. Barcode scanner (`POSPage`) unregisters permanently from `WeakReferenceMessenger` on `Page_Unloaded`, breaking barcode scanning when navigating back to POS. `BarcodeService` missing `IDisposable` for `SerialPort`.
- **Unexplored areas**: None. Technical investigation complete.

## Key Decisions Made
- Recommending `IDbContextFactory<AppDbContext>` short-lived DbContext scoping across all ViewModels.
- Recommending explicit SQLite connection string parameters (`Busy Timeout=30000;Pooling=True`) and startup PRAGMAs (`WAL`, `NORMAL`, `MEMORY`).
- Recommending static/reusable SkiaSharp paints and explicit cleanup on Page/ViewModel unload.
- Recommending periodic background GC Compaction hosted service (`GC.Collect` with LOH compaction).

## Artifact Index
- `f:\Raw\kasher\kasher\.agents\explorer_2\DISPATCH.md` — Dispatch history log
- `f:\Raw\kasher\kasher\.agents\explorer_2\BRIEFING.md` — Persistent briefing
- `f:\Raw\kasher\kasher\.agents\explorer_2\progress.md` — Liveness heartbeat & progress log
- `f:\Raw\kasher\kasher\.agents\explorer_2\analysis.md` — Technical analysis report
- `f:\Raw\kasher\kasher\.agents\explorer_2\handoff.md` — 5-Component handoff report
