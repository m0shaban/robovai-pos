namespace SmartPOS.Core.Entities;

/// <summary>
/// Product entity representing inventory items
/// </summary>
public class Product : BaseEntity
{
    public string Barcode { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string? ImagePath { get; set; }
    public decimal PurchasePrice { get; set; }
    public decimal SellingPrice { get; set; }
    public int Stock { get; set; }
    public int MinStockLevel { get; set; } = 10;
    public UnitType Unit { get; set; } = UnitType.Piece;
    public bool IsActive { get; set; } = true;
    public DateTime? ExpiryDate { get; set; }

    // Foreign Keys
    public int CategoryId { get; set; }
    public int? SupplierId { get; set; }

    // Navigation Properties
    public virtual Category Category { get; set; } = null!;
    public virtual Supplier? Supplier { get; set; }
    public virtual ICollection<SaleDetail> SaleDetails { get; set; } = new List<SaleDetail>();
    public virtual ICollection<StockMovement> StockMovements { get; set; } = new List<StockMovement>();

    // Computed Properties
    public bool IsLowStock => Stock <= MinStockLevel;
    public bool IsExpired => ExpiryDate.HasValue && ExpiryDate.Value.Date < DateTime.Today;
    public bool IsExpiringSoon => ExpiryDate.HasValue && !IsExpired && ExpiryDate.Value.Date <= DateTime.Today.AddDays(30);
    public decimal ProfitMargin => SellingPrice - PurchasePrice;
    public decimal ProfitMarginPercentage => PurchasePrice > 0 ? (ProfitMargin / PurchasePrice) * 100 : 0;
}

public enum UnitType
{
    Piece = 1,
    Box = 2,
    Carton = 3,
    Kilogram = 4,
    Liter = 5
}
