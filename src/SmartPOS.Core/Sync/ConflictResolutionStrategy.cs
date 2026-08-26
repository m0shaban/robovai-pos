namespace SmartPOS.Core.Sync;

public enum ConflictResolutionStrategy
{
    LastWriteWins = 0,
    ServerWins = 1,
    ClientWins = 2,
    ManualResolution = 3
}
