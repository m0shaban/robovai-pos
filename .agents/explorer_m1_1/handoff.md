# Handoff Report: Milestone M1 Multi-Mode Sync Config Engine

**Agent**: Explorer Agent (`explorer_m1_1`)  
**Working Directory**: `f:\Raw\kasher\kasher\.agents\explorer_m1_1`  
**Target Milestone**: M1 (Multi-Mode Sync Config Engine - Requirement R1)  
**Date**: 2026-08-08  

---

## 1. Observation

1. **Existing Application Setup**:
   - `src/SmartPOS.WPF/App.xaml.cs:203-261` configures `Microsoft.Extensions.Hosting` host and DI container via `ConfigureServices`.
   - `src/SmartPOS.Infrastructure/Services/SettingsService.cs:1-106` uses `IServiceScopeFactory` to load/save settings to SQLite `AppSettings` table via `AppDbContext`.
   - `src/SmartPOS.UnitTests/ViewModels/DashboardViewModelTests.cs:1-71` uses xUnit and `EF Core InMemory` database for unit tests.
2. **Absence of Sync Config Files**:
   - No `SyncConfig.cs`, `SyncMode.cs`, or `ISyncConfigService.cs` files currently exist in `src/`.
3. **Requirements & Blueprints**:
   - `ORIGINAL_REQUEST.md:12-17` (R1) specifies Offline, Online, and Hybrid sync modes.
   - `PROJECT.md:13-34` identifies Feature 1 (Multi-Mode Sync Config Engine) under Milestone M1.
   - `explorer_3/analysis.md:141-218` outlines the JSON configuration schema and Outbox state machine requirements.

---

## 2. Logic Chain

1. **Domain Model Placement**:
   - `SyncMode`, `SyncConfig`, sub-configurations (`LanServerConfig`, `CloudServerConfig`, `SyncEngineParams`), `ConflictResolutionStrategy`, and `SyncConfigChangedEventArgs` must reside in `src/SmartPOS.Core/Sync/` under namespace `SmartPOS.Core.Sync`.
   - This ensures all layers (`Infrastructure`, `Application`, `WPF`, `UnitTests`) can consume sync domain types without circular dependencies.
2. **Interface & Implementation Separation**:
   - `ISyncConfigService` contract lives in `src/SmartPOS.Core/Interfaces/ISyncConfigService.cs`.
   - Infrastructure implementation `SyncConfigService` lives in `src/SmartPOS.Infrastructure/Services/SyncConfigService.cs`.
3. **Dynamic Mode Switching & Thread-Safe Event Notification**:
   - `SyncConfigService` holds a private lock-protected `_currentConfig` instance and exposes `Current` property returning a deep clone snapshot.
   - Updates via `UpdateConfigAsync` or `UpdateModeAsync` save serialized JSON to SQLite `AppSettings` table (`Key = "SyncEngine_ConfigJson"`) and trigger `ConfigChanged` event (`SyncConfigChangedEventArgs`).
   - Subscriber exceptions are safely caught per handler to ensure failure in one UI subscriber never aborts configuration updates.
4. **DI Lifecycle & Initialization**:
   - Registered as **Singleton** (`ISyncConfigService`, `SyncConfigService`) in `App.xaml.cs`.
   - Transient `SyncConfig` registered via factory delegate `sp => sp.GetRequiredService<ISyncConfigService>().Current`.
   - `InitializeAsync()` called during `App.OnStartup()` after DB initialization (`DbInitializer.InitializeAsync`).

---

## 3. Caveats

- **Database Concurrency**: `SyncConfigService` uses `IServiceScopeFactory` to create scoped `AppDbContext` instances for DB reads/writes to prevent holding long-lived DbContext connections.
- **Dynamic Port Re-binding**: Changing `LanServer.Port` at runtime requires the Kestrel Embedded Server component (M1 partner feature) to handle server restart gracefully on port changes.

---

## 4. Conclusion

The Multi-Mode Sync Config Engine design is completely specified, concrete C# code blueprints are provided in `f:\Raw\kasher\kasher\.agents\explorer_m1_1\analysis.md`, and implementation details are fully mapped out. An implementer agent can proceed directly to file creation and integration.

---

## 5. Verification Method

1. **Build Verification**:
   ```powershell
   dotnet build f:\Raw\kasher\kasher\src\SmartPOS.Core\SmartPOS.Core.csproj
   dotnet build f:\Raw\kasher\kasher\src\SmartPOS.Infrastructure\SmartPOS.Infrastructure.csproj
   dotnet build f:\Raw\kasher\kasher\src\SmartPOS.WPF\SmartPOS.WPF.csproj
   ```
2. **Unit Test Verification**:
   ```powershell
   dotnet test f:\Raw\kasher\kasher\src\SmartPOS.UnitTests\SmartPOS.UnitTests.csproj
   ```
3. **Inspection of Artifact Files**:
   - `f:\Raw\kasher\kasher\.agents\explorer_m1_1\analysis.md`
   - `f:\Raw\kasher\kasher\.agents\explorer_m1_1\handoff.md`

---
