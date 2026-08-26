namespace SmartPOS.Core.Entities;

/// <summary>
/// Purchase order for supplier orders
/// </summary>
public class PurchaseOrder : BaseEntity
{
    public string OrderNumber { get; set; } = string.Empty;
    public DateTime OrderDate { get; set; } = DateTime.Now;
    public DateTime? ReceivedDate { get; set; }
    public decimal TotalAmount { get; set; }
    public decimal PaidAmount { get; set; }
    public PurchaseOrderStatus Status { get; set; } = PurchaseOrderStatus.Pending;
    public string? Notes { get; set; }

    // Foreign Keys
    public int SupplierId { get; set; }

    // Navigation Properties
    public virtual Supplier Supplier { get; set; } = null!;
    public virtual ICollection<PurchaseOrderDetail> OrderDetails { get; set; } = new List<PurchaseOrderDetail>();

    // Computed Properties
    public decimal RemainingAmount => TotalAmount - PaidAmount;
}

public enum PurchaseOrderStatus
{
    Pending = 1,
    Received = 2,
    Cancelled = 3,
    PartiallyReceived = 4
}
