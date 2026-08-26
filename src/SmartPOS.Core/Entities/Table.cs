namespace SmartPOS.Core.Entities;

public enum TableStatus
{
    Available,
    Occupied,
    Reserved,
    Cleaning
}

public class Table : BaseEntity
{
    public string Name { get; set; } = string.Empty;
    public int Capacity { get; set; }
    public string Section { get; set; } = "Main Hall"; // Indoor, Outdoor, etc.
    public TableStatus Status { get; set; } = TableStatus.Available;
    public bool IsActive { get; set; } = true;

    // Optional: Link to current active order if occupied
    public int? CurrentOrderId { get; set; }
}
