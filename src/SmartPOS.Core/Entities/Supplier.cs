namespace SmartPOS.Core.Entities;

/// <summary>
/// Supplier entity for inventory management
/// </summary>
public class Supplier : BaseEntity
{
    public string Name { get; set; } = string.Empty;
    public string? ContactPerson { get; set; }
    public string? Phone { get; set; }
    public string? Email { get; set; }
    public string? Address { get; set; }
    public decimal DebtAmount { get; set; }
    public bool IsActive { get; set; } = true;
    
    // Navigation Properties
    public virtual ICollection<Product> Products { get; set; } = new List<Product>();
    public virtual ICollection<PurchaseOrder> PurchaseOrders { get; set; } = new List<PurchaseOrder>();
    public virtual ICollection<SupplierPayment> Payments { get; set; } = new List<SupplierPayment>();
}
