# Milestone M1 Handoff Report: Embedded Kestrel HTTP Server & API Infrastructure

**Author**: Explorer Agent (`explorer_m1_3`)  
**Working Directory**: `f:\Raw\kasher\kasher\.agents\explorer_m1_3`  
**Date**: 2026-08-08  
**Milestone**: M1 (Embedded Kestrel HTTP Server)  

---

## 1. Observation

1. **Project SDK & Target Framework**: `src/SmartPOS.WPF/SmartPOS.WPF.csproj:1-12` targets `<TargetFramework>net8.0-windows10.0.19041</TargetFramework>` using SDK `Microsoft.NET.Sdk`. It references `Microsoft.Extensions.Hosting` (v8.0.1) and `Microsoft.Extensions.DependencyInjection` (v8.0.1).
2. **WPF Host Configuration**: `src/SmartPOS.WPF/App.xaml.cs:203-261` constructs an `IHost` via `Host.CreateDefaultBuilder()`. It registers transient/singleton services and builds the generic host. `App.xaml.cs:95` calls `await _host.StartAsync();` on startup and `App.xaml.cs:196` calls `await _host.StopAsync();` on exit.
3. **Database Context Lifetime**: `src/SmartPOS.WPF/App.xaml.cs:214-218` registers `AppDbContext` with `ServiceLifetime.Transient`.
4. **Build Verification**: Command `dotnet build src/SmartPOS.WPF/SmartPOS.WPF.csproj` completed with exit code 0 (`Build succeeded, 0 Warning(s), 0 Error(s)`).
5. **Controller Absence**: Directory search in `src/` revealed 0 existing API Controller classes (`Found 0 results` for `*Controller*`).

---

## 2. Logic Chain

1. **Observation 1 & 2** show that `SmartPOS.WPF` already runs a `Microsoft.Extensions.Hosting.IHost` pipeline inside `App.xaml.cs`. Therefore, adding `<FrameworkReference Include="Microsoft.AspNetCore.App" />` to `SmartPOS.WPF.csproj` enables full ASP.NET Core Kestrel and MVC Controller support without introducing third-party NuGet package bloat or version conflicts.
2. **Observation 2** shows that `_host.StartAsync()` and `_host.StopAsync()` are already invoked in WPF `OnStartup` and `OnExit`. Therefore, attaching `.ConfigureWebHostDefaults(webBuilder => ...)` inside `BuildHost()` will automatically manage the lifetime of the embedded Kestrel HTTP server on port `5050` during application launch and exit.
3. **Observation 3** shows `AppDbContext` is registered as `Transient`. Therefore, when Kestrel processes incoming HTTP requests on ThreadPool threads, each API controller invocation receives a separate, short-lived `AppDbContext` instance, avoiding DbContext thread-safety issues between WPF UI thread and HTTP threads.
4. **Observation 5** shows no existing Controllers. Therefore, adding a new `Controllers/` directory under `SmartPOS.WPF` containing `PairingController.cs`, `SyncController.cs`, `PosOperationsController.cs`, and `DeviceController.cs` cleanly modularizes the API endpoints for local POS operations, QR pairing, and NDJSON streaming without cluttering existing WPF view models.

---

## 3. Caveats

1. **Windows Firewall Prompt**: When Kestrel binds to `0.0.0.0:5050` for the first time, Windows Defender Firewall may display a permission prompt asking to allow incoming traffic on port 5050 across local networks. In production or automated installations, an InnoSetup / PowerShell script rule should add a firewall exception rule for port 5050.
2. **Authentication / Security Scope**: The basic pairing handshake and JWT session validation in `PairingController` uses HMAC-SHA256 tokens. Full role-based user authentication (RBAC) claims validation will be expanded in Milestone M5.

---

## 4. Conclusion

The architecture design for Milestone M1 (Embedded Kestrel HTTP Server) is complete, fully specified, and ready for immediate implementation.

### Implementation Checklist for Implementer:
1. Update `src/SmartPOS.WPF/SmartPOS.WPF.csproj` with `<FrameworkReference Include="Microsoft.AspNetCore.App" />`.
2. Add `KestrelServer` configuration block to `src/SmartPOS.WPF/appsettings.json`.
3. Update `BuildHost()` in `src/SmartPOS.WPF/App.xaml.cs` with `.ConfigureWebHostDefaults(...)` listening on `http://0.0.0.0:5050`.
4. Create controllers in `src/SmartPOS.WPF/Controllers/`: `PairingController.cs`, `SyncController.cs`, `PosOperationsController.cs`, `DeviceController.cs`.

---

## 5. Verification Method

To verify the implementation once executed:

1. **Build Verification**:
   ```powershell
   dotnet build src/SmartPOS.WPF/SmartPOS.WPF.csproj
   ```
   Expectation: Build succeeds with 0 errors.

2. **Server Listening & Handshake Verification**:
   Run `SmartPOS.WPF.exe` and test endpoints using `curl` or `Invoke-RestMethod`:
   ```powershell
   # 1. Pairing Status Check
   curl http://localhost:5050/api/v1/pair/status

   # 2. Devices Heartbeat Ping
   curl -X POST http://localhost:5050/api/v1/devices/heartbeat `
        -H "Content-Type: application/json" `
        -d '{"deviceId":"TEST-DEV-01","deviceName":"Test Device","deviceType":"MobileWms"}'

   # 3. Product Catalog Query
   curl http://localhost:5050/api/v1/pos/products

   # 4. NDJSON Payload Export Stream
   curl http://localhost:5050/api/v1/sync/export-stream?entity=products
   ```
   Expectation: Port 5050 responds with HTTP 200 OK and proper JSON / NDJSON content.
