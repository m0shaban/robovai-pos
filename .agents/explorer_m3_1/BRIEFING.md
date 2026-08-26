# BRIEFING — 2026-08-08T09:18:40Z

## Mission
Formulate an exact, detailed implementation strategy for EF Core IDbContextFactory<AppDbContext> refactoring and .AsNoTracking() placement across all WPF ViewModels, Services, and Repositories (Milestone M3-1).

## 🔒 My Identity
- Archetype: teamwork_preview_explorer
- Roles: Read-only investigation and implementation strategy formulation
- Working directory: f:\Raw\kasher\kasher\.agents\explorer_m3_1
- Original parent: 40230514-75f7-4b32-9ba0-31d6e6dfc3d0
- Milestone: M3-1 (EF Core DbContextFactory & AsNoTracking Hardening)

## 🔒 Key Constraints
- Read-only investigation — do NOT modify application source code in src/
- Deliver detailed analysis report to f:\Raw\kasher\kasher\.agents\explorer_m3_1\analysis.md
- Deliver handoff report via send_message to parent when complete

## Current Parent
- Conversation ID: 40230514-75f7-4b32-9ba0-31d6e6dfc3d0
- Updated: 2026-08-08T06:17:24Z

## Investigation State
- **Explored paths**: `src/SmartPOS.WPF/App.xaml.cs`, `src/SmartPOS.Infrastructure/Data/DatabasePathHelper.cs`, `src/SmartPOS.Infrastructure/Data/AppDbContextFactory.cs`, `src/SmartPOS.Application/ViewModels/*.cs`, `src/SmartPOS.Infrastructure/Services/*.cs`, `src/SmartPOS.Infrastructure/Repositories/*.cs`, `src/SmartPOS.UnitTests/**/*.cs`
- **Key findings**: Identified 18 ViewModels and 4 Services currently injecting long-lived `AppDbContext`, causing ChangeTracker accumulation and memory leaks. Identified test suite constructor patterns requiring `TestDbContextFactory`.
- **Unexplored areas**: Finalizing exact line numbers and code transformation snippets for `analysis.md` report.

## Key Decisions Made
- Use `SqliteConnectionStringBuilder` in `DatabasePathHelper.cs` to supply `Busy Timeout=30` and `Pooling=true`.
- Replace `services.AddDbContext<AppDbContext>(..., ServiceLifetime.Transient)` with `services.AddDbContextFactory<AppDbContext>(...)` in `App.xaml.cs`.
- Refactor ViewModel constructors to accept `IDbContextFactory<AppDbContext>` and encapsulate operations in `using var context = _contextFactory.CreateDbContext()`.
- Add `.AsNoTracking()` to all read-only listing/query methods.
- Provide `TestDbContextFactory` in Unit Tests so test suite compiles and runs cleanly.

## Artifact Index
- `f:\Raw\kasher\kasher\.agents\explorer_m3_1\DISPATCH.md` — Parent dispatch & status query messages
- `f:\Raw\kasher\kasher\.agents\explorer_m3_1\BRIEFING.md` — Explorer working memory index
- `f:\Raw\kasher\kasher\.agents\explorer_m3_1\progress.md` — Liveness heartbeat log
- `f:\Raw\kasher\kasher\.agents\explorer_m3_1\analysis.md` — Main implementation plan report
- `f:\Raw\kasher\kasher\.agents\explorer_m3_1\handoff.md` — 5-component handoff report
