namespace SmartPOS.Core.Entities;

/// <summary>
/// Individual items in a sale transaction
/// </summary>
public class SaleDetail : BaseEntity
{
    public int Quantity { get; set; }
    public decimal UnitPrice { get; set; }
    public decimal UnitCost { get; set; }
    public decimal DiscountPercentage { get; set; }
    public decimal DiscountAmount { get; set; }
    public decimal LineTotal { get; set; }
    
    // Foreign Keys
    public int SaleId { get; set; }
    public int ProductId { get; set; }
    
    // Navigation Properties
    public virtual Sale Sale { get; set; } = null!;
    public virtual Product Product { get; set; } = null!;
    
    // Computed Properties
    public decimal Subtotal => Quantity * UnitPrice;
    public decimal LineProfit => (UnitPrice - UnitCost) * Quantity - DiscountAmount;
}
