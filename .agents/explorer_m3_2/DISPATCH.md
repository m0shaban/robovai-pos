## Dispatch for Explorer M3-2

**Working Directory**: `f:\Raw\kasher\kasher\.agents\explorer_m3_2`
**Role**: Read-only exploration agent (`teamwork_preview_explorer`)

### Required Context Files to Read:
1. `f:\Raw\kasher\kasher\.agents\ORIGINAL_REQUEST.md`
2. `f:\Raw\kasher\kasher\PROJECT.md`
3. `f:\Raw\kasher\kasher\.agents\explorer_2\analysis.md`

### Task Description:
Formulate an exact, step-by-step implementation plan for:
1. Updating `DatabasePathHelper.cs` and `App.xaml.cs` to build SQLite connection string with `Busy Timeout = 30000`, `Mode = ReadWriteCreate`, and `Pooling = true`.
2. Executing SQLite PRAGMA statements (`journal_mode=WAL;`, `busy_timeout=30000;`, `synchronous=NORMAL;`, `temp_store=MEMORY;`, `foreign_keys=ON;`) on DB initialization in `DbInitializer.cs`.
3. Creating the automated background GC compaction service (`GcCompactionService.cs`) in `SmartPOS.Infrastructure/Services/` that performs periodic LOH compaction (`GCSettings.LargeObjectHeapCompactionMode = GCLargeObjectHeapCompactionMode.CompactOnce; GC.Collect(...)`) every 15 minutes, and registering it in `App.xaml.cs`.

### Output Requirement:
Write a detailed report to `f:\Raw\kasher\kasher\.agents\explorer_m3_2\analysis.md` detailing exact file paths, line ranges, class signatures, and code modifications needed. Deliver handoff report via send_message to parent when complete.
