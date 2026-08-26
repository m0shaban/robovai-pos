## 2026-08-08T06:16:55Z

You are the Worker agent for Milestone M1 (Hybrid Online/Offline Architecture & Configuration Engine).
Your working directory is: f:\Raw\kasher\kasher\.agents\worker_m1

MUST READ BEFORE STARTING WORK:
- f:\Raw\kasher\kasher\.agents\ORIGINAL_REQUEST.md
- f:\Raw\kasher\kasher\PROJECT.md
- f:\Raw\kasher\kasher\.agents\explorer_m1_1\handoff.md and f:\Raw\kasher\kasher\.agents\explorer_m1_1\analysis.md
- f:\Raw\kasher\kasher\.agents\explorer_m1_2\handoff.md and f:\Raw\kasher\kasher\.agents\explorer_m1_2\analysis.md
- f:\Raw\kasher\kasher\.agents\explorer_m1_3\handoff.md and f:\Raw\kasher\kasher\.agents\explorer_m1_3\analysis.md

MANDATORY INTEGRITY WARNING:
DO NOT CHEAT. All implementations must be genuine. DO NOT hardcode test results, create dummy/facade implementations, or circumvent the intended task. A teamwork_preview_auditor will independently verify your work. Integrity violations WILL be detected and your work WILL be rejected.

Scope & Tasks to Implement:
1. Multi-Mode Sync Config Engine:
   - `SyncMode` enum (Offline, Online, Hybrid), `SyncConfig` POCO, `ISyncConfigService`, and `SyncConfigService` storing JSON configuration in SQLite `AppSettings` table with dynamic runtime switching and thread-safe `SyncConfigChangedEventArgs` notifications.
   - Singleton & Factory DI registration in `src/SmartPOS.WPF/App.xaml.cs` and startup initialization.

2. Outbox Queue & Sync Engine:
   - `SyncOutbox` entity, `OutboxStatus` & `OutboxOperation` enums, `ISyncableEntity` interface, `SyncOutboxDtos`.
   - Update EF Core `AppDbContext` with `DbSet<SyncOutbox>` and `sync_outbox` model configuration with indexes.
   - `OutboxSaveChangesInterceptor` for EF Core change tracking & atomic transaction enqueueing into `sync_outbox`.
   - `ISyncOutboxRepository` interface & `SyncOutboxRepository` implementation.
   - `SyncOutboxProcessor` background service with polling, batching, exponential backoff retries, and LWW conflict resolution.
   - Dexie.js version 9 upgrade in `smart-inventory-pro/js/db.js` with `sync_outbox` store and Outbox transaction helper functions.

3. Embedded Kestrel HTTP Server:
   - Add `<FrameworkReference Include="Microsoft.AspNetCore.App" />` to `src/SmartPOS.WPF/SmartPOS.WPF.csproj`.
   - Add `KestrelServer` configuration to `src/SmartPOS.WPF/appsettings.json`.
   - Update `BuildHost()` in `src/SmartPOS.WPF/App.xaml.cs` with `.ConfigureWebHostDefaults(...)` listening on `http://0.0.0.0:5050`.
   - Add Controllers in `src/SmartPOS.WPF/Controllers/`: `PairingController.cs`, `SyncController.cs`, `PosOperationsController.cs`, `DeviceController.cs`.

4. Unit Tests & Verification:
   - Add comprehensive unit tests in `src/SmartPOS.UnitTests` covering `SyncConfigService`, `OutboxSaveChangesInterceptor`, `SyncOutboxProcessor`, and Controller endpoints.
   - Execute and verify build & tests using command line:
     - `dotnet build src/SmartPOS.Core/SmartPOS.Core.csproj`
     - `dotnet build src/SmartPOS.Infrastructure/SmartPOS.Infrastructure.csproj`
     - `dotnet build src/SmartPOS.WPF/SmartPOS.WPF.csproj`
     - `dotnet test src/SmartPOS.UnitTests/SmartPOS.UnitTests.csproj`

Write your handoff report to `f:\Raw\kasher\kasher\.agents\worker_m1\handoff.md` with complete details of changes made, build output, and test results.
Send a message to the caller when done.
