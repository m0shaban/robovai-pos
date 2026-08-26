## 2026-08-08T09:04:22Z

You are the Sub-Orchestrator for Milestone M3 (R3: Desktop WPF Memory Leak & Database Lock Resolution).
Working directory: f:\Raw\kasher\kasher\.agents\sub_orch_m3
Project document: f:\Raw\kasher\kasher\PROJECT.md
Original request path: f:\Raw\kasher\kasher\.agents\ORIGINAL_REQUEST.md
Parent conversation ID: 6703759f-0ac0-49ba-8d30-4a7c00cd8907

Scope for Milestone M3:
1. Re-architect EF Core `AppDbContext` lifetime to short-lived `IDbContextFactory<AppDbContext>` in ViewModels with `.AsNoTracking()` on read queries.
2. Enable SQLite WAL mode (`PRAGMA journal_mode=WAL;`), set `Busy Timeout = 30000ms`, `synchronous=NORMAL`, and update `DatabasePathHelper.cs` / `DbInitializer.cs`.
3. Eliminate unmanaged LiveCharts & SkiaSharp paint leaks, fix OpenCV `VideoCapture(0)` camera handle leaks on page unload, and eliminate bitmap churn.
4. Restore `MainPOSViewModel` barcode scanner messenger lifecycle on page load/unload, and implement automated background GC compaction service (`GcCompactionService.cs`).
