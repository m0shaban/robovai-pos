namespace SmartPOS.Core.Interfaces;

/// <summary>
/// Interface for domain entities that participate in Outbox change tracking
/// </summary>
public interface ISyncableEntity
{
    string SyncId { get; set; }
    DateTime CreatedAt { get; set; }
    DateTime? UpdatedAt { get; set; }
    bool IsDeleted { get; set; }
}
