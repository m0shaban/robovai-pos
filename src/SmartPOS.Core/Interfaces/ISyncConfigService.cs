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
