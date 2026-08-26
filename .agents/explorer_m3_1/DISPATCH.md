## Dispatch for Explorer M3-1

**Working Directory**: `f:\Raw\kasher\kasher\.agents\explorer_m3_1`
**Role**: Read-only exploration agent (`teamwork_preview_explorer`)

### Required Context Files to Read:
1. `f:\Raw\kasher\kasher\.agents\ORIGINAL_REQUEST.md`
2. `f:\Raw\kasher\kasher\PROJECT.md`
3. `f:\Raw\kasher\kasher\.agents\explorer_2\analysis.md`

### Task Description:
Formulate an exact, step-by-step implementation plan for:
1. Registering `IDbContextFactory<AppDbContext>` in `App.xaml.cs` with `SqliteConnectionStringBuilder`.
2. Refactoring ViewModels (`DashboardViewModel.cs`, `ProductsViewModel.cs`, `ReportsViewModel.cs`, `InvoicesViewModel.cs`, `ExpensesViewModel.cs`, `MainPOSViewModel.cs`, etc.) from holding long-lived `AppDbContext` to creating short-lived contexts via `IDbContextFactory<AppDbContext>`.
3. Ensuring all read-only listing/query methods use `.AsNoTracking()`.

### Output Requirement:


## 2026-08-08T06:17:24Z
From: parent (40230514-75f7-4b32-9ba0-31d6e6dfc3d0)
Content: Please report status on your investigation for EF Core IDbContextFactory<AppDbContext> refactoring and .AsNoTracking() placement for Milestone M3-1.

