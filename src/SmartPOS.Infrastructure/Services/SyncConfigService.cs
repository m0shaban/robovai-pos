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
            var modeStr = section["Mode"];
            if (Enum.TryParse<SyncMode>(modeStr, true, out var mode)) config.Mode = mode;
            if (int.TryParse(section["BranchId"], out var branchId)) config.BranchId = branchId;
            if (!string.IsNullOrEmpty(section["BranchCode"])) config.BranchCode = section["BranchCode"]!;
            if (!string.IsNullOrEmpty(section["DeviceId"])) config.DeviceId = section["DeviceId"]!;
            if (!string.IsNullOrEmpty(section["DeviceName"])) config.DeviceName = section["DeviceName"]!;

            var lan = section.GetSection("LanServer");
            if (lan.Exists())
            {
                if (bool.TryParse(lan["Enabled"], out var e)) config.LanServer.Enabled = e;
                if (int.TryParse(lan["Port"], out var p)) config.LanServer.Port = p;
                if (!string.IsNullOrEmpty(lan["BindAddress"])) config.LanServer.BindAddress = lan["BindAddress"]!;
            }

            var cloud = section.GetSection("CloudServer");
            if (cloud.Exists())
            {
                if (bool.TryParse(cloud["Enabled"], out var e)) config.CloudServer.Enabled = e;
                if (!string.IsNullOrEmpty(cloud["BaseUrl"])) config.CloudServer.BaseUrl = cloud["BaseUrl"]!;
                if (!string.IsNullOrEmpty(cloud["Provider"])) config.CloudServer.Provider = cloud["Provider"]!;
            }

            var engine = section.GetSection("EngineParams");
            if (engine.Exists())
            {
                if (int.TryParse(engine["SyncIntervalSeconds"], out var sec)) config.EngineParams.SyncIntervalSeconds = sec;
                if (int.TryParse(engine["BatchSize"], out var bs)) config.EngineParams.BatchSize = bs;
            }
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
