namespace SmartPOS.Core.Entities;

/// <summary>
/// Customer entity
/// </summary>
public class Customer : BaseEntity
{
    public string Name { get; set; } = string.Empty;
    public string? Phone { get; set; }
    public string? Email { get; set; }
    public string? Address { get; set; }
    public decimal CreditLimit { get; set; }
    public decimal CurrentDebt { get; set; }
    public bool IsActive { get; set; } = true;
    public DateTime? Birthdate { get; set; }
    public string? Notes { get; set; }

    // Navigation Properties
    public virtual ICollection<Sale> Sales { get; set; } = new List<Sale>();
    public virtual ICollection<Return> Returns { get; set; } = new List<Return>();
    public virtual CustomerLoyalty? Loyalty { get; set; }

    // Computed Properties
    public decimal TotalPurchases => Sales.Where(s => s.Status == SaleStatus.Completed).Sum(s => s.TotalAmount);
    public int TotalTransactions => Sales.Count(s => s.Status == SaleStatus.Completed);
}
