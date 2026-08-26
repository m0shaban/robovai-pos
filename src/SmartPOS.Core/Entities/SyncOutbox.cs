namespace SmartPOS.Core.Entities;

public enum OutboxStatus
{
    Pending = 1,
    Processing = 2,
    Synced = 3,
    Failed = 4,
    DeadLetter = 5
}

public enum OutboxOperation
{
    Insert = 1,
    Update = 2,
    Delete = 3
}

public class SyncOutbox
{
    public string Id { get; set; } = Guid.NewGuid().ToString();
    public string EntityType { get; set; } = string.Empty;
    public string EntityId { get; set; } = string.Empty;
    public string SyncId { get; set; } = Guid.NewGuid().ToString();
    public OutboxOperation Operation { get; set; } = OutboxOperation.Insert;
    public string PayloadJson { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? SyncedAt { get; set; }
    public OutboxStatus Status { get; set; } = OutboxStatus.Pending;
    public int RetryCount { get; set; } = 0;
    public string? LastError { get; set; }
    public string? TraceId { get; set; }
    public long Version { get; set; } = 1;
}
