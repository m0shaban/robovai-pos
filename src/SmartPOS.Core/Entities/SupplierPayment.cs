namespace SmartPOS.Core.Entities;

/// <summary>
/// Records a payment made to a supplier to reduce their debt balance.
/// </summary>
public class SupplierPayment : BaseEntity
{
    public int SupplierId { get; set; }
    public decimal Amount { get; set; }
    public DateTime PaymentDate { get; set; } = DateTime.Now;
    public string? Notes { get; set; }
    public string? Reference { get; set; }

    // Navigation Properties
    public virtual Supplier Supplier { get; set; } = null!;
}
