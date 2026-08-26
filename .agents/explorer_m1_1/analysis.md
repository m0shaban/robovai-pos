# Technical Implementation Specification: Multi-Mode Sync Config Engine (Milestone M1)

**Author**: Explorer Agent (`explorer_m1_1`)  
**Target Milestone**: M1 (Multi-Mode Sync Config Engine - R1)  
**Target Repository**: `f:\Raw\kasher\kasher`  
**Date**: 2026-08-08  

---

## Executive Summary

This document specifies the exact technical implementation design for **Milestone M1: Multi-Mode Sync Config Engine** for the **RobovAI PRO POS & WMS Ecosystem**. Requirement R1 specifies a configurable multi-mode sync engine supporting **Offline**, **Online**, and **Hybrid** modes with dynamic runtime mode switching, event notification dispatching, and seamless Dependency Injection (DI) integration.

---

## 1. Domain Architecture & File Blueprint

The sync configuration engine is designed as a core domain component residing in `SmartPOS.Core` to allow referencing across `SmartPOS.Infrastructure`, `SmartPOS.Application`, and `SmartPOS.WPF`.

### File Allocation Table

| Purpose | Namespace | Target File Path |
|---|---|---|
| Mode Enum | `SmartPOS.Core.Sync` | `src/SmartPOS.Core/Sync/SyncMode.cs` |
| Conflict Strategy Enum | `SmartPOS.Core.Sync` | `src/SmartPOS.Core/Sync/ConflictResolutionStrategy.cs` |
| Sub-Configuration POCOs | `SmartPOS.Core.Sync` | `src/SmartPOS.Core/Sync/LanServerConfig.cs`<br>`src/SmartPOS.Core/Sync/CloudServerConfig.cs`<br>`src/SmartPOS.Core/Sync/SyncEngineParams.cs` |
| Root Configuration POCO | `SmartPOS.Core.Sync` | `src/SmartPOS.Core/Sync/SyncConfig.cs` |
| Event Arguments | `SmartPOS.Core.Sync` | `src/SmartPOS.Core/Sync/SyncConfigChangedEventArgs.cs` |
| Service Interface | `SmartPOS.Core.Interfaces` | `src/SmartPOS.Core/Interfaces/ISyncConfigService.cs` |
| Infrastructure Service | `SmartPOS.Infrastructure.Services` | `src/SmartPOS.Infrastructure/Services/SyncConfigService.cs` |
| Unit Tests | `SmartPOS.UnitTests.Sync` | `src/SmartPOS.UnitTests/Sync/SyncConfigServiceTests.cs` |

---

## 2. Concrete Class & Enum Definitions

### 2.1 `SyncMode.cs`
```csharp
namespace SmartPOS.Core.Sync;

/// <summary>
/// Defines the operational mode for the RobovAI POS & WMS Sync Engine (Requirement R1).
/// Supports dynamic runtime switching without application restart.
/// </summary>
public enum SyncMode
{
    /// <summary>
    /// Pure Local Intranet Mode.
    /// WPF POS hosts local Kestrel HTTP server (port 5050). All data is persisted in local SQLite/Dexie.
    /// Cloud synchronization worker is paused/disabled.
    /// </summary>
    Offline = 0,

    /// <summary>
    /// Pure Cloud-First Mode.
    /// POS/WMS connect directly to Cloud API (Vercel/Render/Firebase/PostgreSQL).
    /// Local LAN HTTP server listener is disabled.
    /// </summary>
    Online = 1,

    /// <summary>
    /// Dual-Tier Hybrid Mode (Recommended Commercial Default).
    /// Local LAN handles ultra-fast checkout transactions (< 5ms).
    /// Asynchronous Outbox Worker pushes pending changes to Cloud when internet is available.
    /// Both LAN Server and Cloud Worker are active.
    /// </summary>
    Hybrid = 2
}
```

### 2.2 `ConflictResolutionStrategy.cs`
```csharp
namespace SmartPOS.Core.Sync;

public enum ConflictResolutionStrategy
{
    LastWriteWins = 0,
    ServerWins = 1,
    ClientWins = 2,
    ManualResolution = 3
}
```

### 2.3 Sub-Configuration Schemas

#### `LanServerConfig.cs`
```csharp
namespace SmartPOS.Core.Sync;

public class LanServerConfig
{
    public bool Enabled { get; set; } = true;
    public int Port { get; set; } = 5050;
    public string BindAddress { get; set; } = "0.0.0.0";
    public string JwtSecret { get; set; } = "RobovAI_LAN_Secret_Key_Change_In_Production_32Bytes!";
    public int ConnectionTimeoutMs { get; set; } = 30000;
    public string[] CorsAllowedOrigins { get; set; } = new[] { "*" };

    public LanServerConfig Clone() => new()
    {
        Enabled = Enabled,
        Port = Port,
        BindAddress = BindAddress,
        JwtSecret = JwtSecret,
        ConnectionTimeoutMs = ConnectionTimeoutMs,
        CorsAllowedOrigins = (string[])CorsAllowedOrigins.Clone()
    };
}
```

#### `CloudServerConfig.cs`
```csharp
namespace SmartPOS.Core.Sync;

public class CloudServerConfig
{
    public bool Enabled { get; set; } = true;
    public string Provider { get; set; } = "Firebase"; // "Firebase" | "REST" | "GraphQL"
    public string BaseUrl { get; set; } = "https://api.robovai.tech/v1";
    public string ApiKey { get; set; } = string.Empty;
    public string ProjectId { get; set; } = "robovai-pos-prod";
    public string TenantId { get; set; } = "tenant_default";
    public string AuthToken { get; set; } = string.Empty;

    public CloudServerConfig Clone() => new()
    {
        Enabled = Enabled,
        Provider = Provider,
        BaseUrl = BaseUrl,
        ApiKey = ApiKey,
        ProjectId = ProjectId,
        TenantId = TenantId,
        AuthToken = AuthToken
    };
}
```

#### `SyncEngineParams.cs`
```csharp
namespace SmartPOS.Core.Sync;

public class SyncEngineParams
{
    public int SyncIntervalSeconds { get; set; } = 30;
    public int BatchSize { get; set; } = 500;
    public int MaxRetryAttempts { get; set; } = 5;
    public ConflictResolutionStrategy ConflictResolution { get; set; } = ConflictResolutionStrategy.LastWriteWins;

    public SyncEngineParams Clone() => new()
    {
        SyncIntervalSeconds = SyncIntervalSeconds,
        BatchSize = BatchSize,
        MaxRetryAttempts = MaxRetryAttempts,
        ConflictResolution = ConflictResolution
    };
}
```

### 2.4 `SyncConfig.cs`
```csharp
namespace SmartPOS.Core.Sync;

public class SyncConfig
{
    public SyncMode Mode { get; set; } = SyncMode.Hybrid;
    public string DeviceId { get; set; } = Guid.NewGuid().ToString();
    public string DeviceName { get; set; } = Environment.MachineName;
    public string BranchCode { get; set; } = "BR-MAIN-01";
    public int BranchId { get; set; } = 1;

    public LanServerConfig LanServer { get; set; } = new();
    public CloudServerConfig CloudServer { get; set; } = new();
    public SyncEngineParams EngineParams { get; set; } = new();

    public SyncConfig Clone() => new()
    {
        Mode = Mode,
        DeviceId = DeviceId,
        DeviceName = DeviceName,
        BranchCode = BranchCode,
        BranchId = BranchId,
        LanServer = LanServer.Clone(),
        CloudServer = CloudServer.Clone(),
        EngineParams = EngineParams.Clone()
    };
}
```

---

## 3. Dynamic Configuration Updates & Event Infrastructure

### 3.1 `SyncConfigChangedEventArgs.cs`
```csharp
namespace SmartPOS.Core.Sync;

public class SyncConfigChangedEventArgs : EventArgs
{
    public SyncConfig PreviousConfig { get; }
    public SyncConfig NewConfig { get; }
    public SyncMode PreviousMode => PreviousConfig.Mode;
    public SyncMode NewMode => NewConfig.Mode;
    public bool ModeHasChanged => PreviousMode != NewMode;

    public SyncConfigChangedEventArgs(SyncConfig previousConfig, SyncConfig newConfig)
    {
        PreviousConfig = previousConfig ?? throw new ArgumentNullException(nameof(previousConfig));
        NewConfig = newConfig ?? throw new ArgumentNullException(nameof(newConfig));
    }
}
```

### 3.2 `ISyncConfigService.cs`
```csharp
namespace SmartPOS.Core.Interfaces;

using SmartPOS.Core.Sync;

public interface ISyncConfigService
{
    /// <summary>Gets a thread-safe snapshot of current sync configuration.</summary>
    SyncConfig Current { get; }

    /// <summary>Fired whenever sync configuration or mode changes dynamically.</summary>
    event EventHandler<SyncConfigChangedEventArgs>? ConfigChanged;

    /// <summary>Initializes configuration from database/JSON cache.</summary>
    Task InitializeAsync();

    /// <summary>Updates and persists configuration, notifying all subscribers.</summary>
    Task UpdateConfigAsync(SyncConfig newConfig);

    /// <summary>Updates sync mode dynamically and adjusts server flags.</summary>
    Task UpdateModeAsync(SyncMode newMode);

    /// <summary>Resets configuration to default settings.</summary>
    Task ResetToDefaultsAsync();
}
```

### 3.3 `SyncConfigService.cs` Implementation Blueprint
```csharp
namespace SmartPOS.Infrastructure.Services;

using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using SmartPOS.Core.Entities;
using SmartPOS.Core.Interfaces;
using SmartPOS.Core.Sync;
using SmartPOS.Infrastructure.Data;

public class SyncConfigService : ISyncConfigService
{
    private const string AppSettingKey = "SyncEngine_ConfigJson";
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly IConfiguration _configuration;
    private readonly ILogger<SyncConfigService>? _logger;
    private readonly object _lock = new();
    private SyncConfig _currentConfig;

    public event EventHandler<SyncConfigChangedEventArgs>? ConfigChanged;

    public SyncConfig Current
    {
        get
        {
            lock (_lock)
            {
                return _currentConfig.Clone();
            }
        }
    }

    public SyncConfigService(
        IServiceScopeFactory scopeFactory,
        IConfiguration configuration,
        ILogger<SyncConfigService>? logger = null)
    {
        _scopeFactory = scopeFactory ?? throw new ArgumentNullException(nameof(scopeFactory));
        _configuration = configuration ?? throw new ArgumentNullException(nameof(configuration));
        _logger = logger;
        _currentConfig = LoadFromConfigurationFallback();
    }

    public async Task InitializeAsync()
    {
        try
        {
            using var scope = _scopeFactory.CreateScope();
            var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();

            var setting = await dbContext.AppSettings.FirstOrDefaultAsync(s => s.Key == AppSettingKey);
            if (setting != null && !string.IsNullOrWhiteSpace(setting.Value))
            {
                var loaded = JsonSerializer.Deserialize<SyncConfig>(setting.Value);
                if (loaded != null)
                {
                    lock (_lock)
                    {
                        _currentConfig = loaded;
                    }
                    _logger?.LogInformation("SyncConfig initialized from AppSettings table. Mode: {Mode}", loaded.Mode);
                    return;
                }
            }

            await SaveConfigToDbAsync(_currentConfig);
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Failed to initialize SyncConfig from database; using fallback configuration.");
        }
    }

    public async Task UpdateConfigAsync(SyncConfig newConfig)
    {
        if (newConfig == null) throw new ArgumentNullException(nameof(newConfig));

        SyncConfig previous;
        SyncConfig snapshot;

        lock (_lock)
        {
            previous = _currentConfig.Clone();
            _currentConfig = newConfig.Clone();
            snapshot = _currentConfig.Clone();
        }

        await SaveConfigToDbAsync(snapshot);
        NotifySubscribers(previous, snapshot);
    }

    public async Task UpdateModeAsync(SyncMode newMode)
    {
        SyncConfig updated;
        lock (_lock)
        {
            updated = _currentConfig.Clone();
            updated.Mode = newMode;
            switch (newMode)
            {
                case SyncMode.Offline:
                    updated.LanServer.Enabled = true;
                    updated.CloudServer.Enabled = false;
                    break;
                case SyncMode.Online:
                    updated.LanServer.Enabled = false;
                    updated.CloudServer.Enabled = true;
                    break;
                case SyncMode.Hybrid:
                    updated.LanServer.Enabled = true;
                    updated.CloudServer.Enabled = true;
                    break;
            }
        }

        await UpdateConfigAsync(updated);
    }

    public async Task ResetToDefaultsAsync()
    {
        var defaultConfig = LoadFromConfigurationFallback();
        await UpdateConfigAsync(defaultConfig);
    }

    private SyncConfig LoadFromConfigurationFallback()
    {
        var config = new SyncConfig();
        var section = _configuration.GetSection("SyncEngine");
        if (section.Exists())
        {
            section.Bind(config);
        }
        return config;
    }

    private async Task SaveConfigToDbAsync(SyncConfig config)
    {
        using var scope = _scopeFactory.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        var json = JsonSerializer.Serialize(config, new JsonSerializerOptions { WriteIndented = false });
        var setting = await dbContext.AppSettings.FirstOrDefaultAsync(s => s.Key == AppSettingKey);
        if (setting == null)
        {
            dbContext.AppSettings.Add(new AppSetting { Key = AppSettingKey, Value = json });
        }
        else
        {
            setting.Value = json;
        }

        await dbContext.SaveChangesAsync();
    }

    private void NotifySubscribers(SyncConfig previous, SyncConfig current)
    {
        var handlers = ConfigChanged;
        if (handlers == null) return;

        var args = new SyncConfigChangedEventArgs(previous, current);
        foreach (EventHandler<SyncConfigChangedEventArgs> handler in handlers.GetInvocationList())
        {
            try
            {
                handler.Invoke(this, args);
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Error in SyncConfigChanged subscriber: {Method}", handler.Method.Name);
            }
        }
    }
}
```

---

## 4. Dependency Injection Registration & Integration

### 4.1 DI Container Registration (`SmartPOS.WPF/App.xaml.cs`)
```csharp
.ConfigureServices((context, services) =>
{
    // ... Existing service registrations ...

    // Sync Engine Configuration Services (Milestone M1)
    services.Configure<SyncConfig>(context.Configuration.GetSection("SyncEngine"));
    services.AddSingleton<ISyncConfigService, SyncConfigService>();
    services.AddTransient<SyncConfig>(sp => sp.GetRequiredService<ISyncConfigService>().Current);
})
```

### 4.2 Application Startup Initialization (`OnStartup`)
```csharp
// Initialize Sync Config Engine after DbInitializer
var syncConfigService = _host.Services.GetRequiredService<ISyncConfigService>();
await syncConfigService.InitializeAsync();
```

---

## 5. Verification & Testing Strategy

1. **Unit Test Suite**: Create `src/SmartPOS.UnitTests/Sync/SyncConfigServiceTests.cs` using xUnit and EF Core InMemory database.
2. **Key Test Cases**:
   - `InitializeAsync_LoadsDefaultsFromConfiguration_WhenDatabaseEmpty`
   - `UpdateModeAsync_TogglesServerFlags_AndFiresConfigChangedEvent`
   - `UpdateConfigAsync_PersistsJsonToAppSettingsTable`
   - `Current_ReturnsIndependentSnapshot_EnsuringThreadSafety`
3. **Verification Command**:
   `dotnet test src/SmartPOS.UnitTests/SmartPOS.UnitTests.csproj`

---
