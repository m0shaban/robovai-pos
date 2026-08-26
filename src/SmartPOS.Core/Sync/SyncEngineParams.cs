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
