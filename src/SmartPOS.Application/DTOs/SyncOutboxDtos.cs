namespace SmartPOS.Application.DTOs;

public class OutboxItemDto
{
    public string Id { get; set; } = string.Empty;
    public string EntityType { get; set; } = string.Empty;
    public string EntityId { get; set; } = string.Empty;
    public string SyncId { get; set; } = string.Empty;
    public string Operation { get; set; } = string.Empty;
    public string PayloadJson { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
    public long Version { get; set; }
    public string? TraceId { get; set; }
}

public class OutboxBatchRequestDto
{
    public string SourceDeviceId { get; set; } = string.Empty;
    public string BranchCode { get; set; } = string.Empty;
    public DateTime SentAt { get; set; } = DateTime.UtcNow;
    public List<OutboxItemDto> Items { get; set; } = new();
}

public class OutboxAckItemDto
{
    public string Id { get; set; } = string.Empty;
    public string SyncId { get; set; } = string.Empty;
    public bool Success { get; set; }
    public string? ErrorMessage { get; set; }
}

public class OutboxBatchResponseDto
{
    public string Status { get; set; } = "OK";
    public int ProcessedCount { get; set; }
    public int SuccessCount { get; set; }
    public int FailureCount { get; set; }
    public List<OutboxAckItemDto> Acks { get; set; } = new();
    public DateTime ServerTime { get; set; } = DateTime.UtcNow;
}
