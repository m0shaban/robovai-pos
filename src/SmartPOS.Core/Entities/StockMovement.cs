namespace SmartPOS.Core.Entities;

/// <summary>
/// Stock movement tracking for inventory audit
/// </summary>
public class StockMovement : BaseEntity
{
    public int ProductId { get; set; }
    public int Quantity { get; set; }
    public MovementType Type { get; set; }
    public string? Reference { get; set; }
    public string? Notes { get; set; }
    public DateTime MovementDate { get; set; } = DateTime.Now;
    
    // Navigation Properties
    public virtual Product Product { get; set; } = null!;
}

public enum MovementType
{
    Purchase = 1,
    Sale = 2,
    Adjustment = 3,
    Return = 4,
    Damage = 5
}
