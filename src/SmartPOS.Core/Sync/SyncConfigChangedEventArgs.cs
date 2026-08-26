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
