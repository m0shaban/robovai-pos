using CommunityToolkit.Mvvm.ComponentModel;

namespace SmartPOS.Application.DTOs;

/// <summary>
/// Cart item for POS checkout
/// </summary>
public partial class CartItem : ObservableObject
{
    public int ProductId { get; set; }
    public string Barcode { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public decimal UnitPrice { get; set; }
    public decimal UnitCost { get; set; }

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(Subtotal))]
    [NotifyPropertyChangedFor(nameof(Total))]
    [NotifyPropertyChangedFor(nameof(LineProfit))]
    [NotifyPropertyChangedFor(nameof(CanIncreaseQuantity))]
    private int _quantity;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(Total))]
    private decimal _discountPercentage;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(Total))]
    [NotifyPropertyChangedFor(nameof(LineProfit))]
    private decimal _discountAmount;

    public int AvailableStock { get; set; }
    
    // Computed properties
    public decimal Subtotal => Quantity * UnitPrice;
    public decimal Total => Subtotal - DiscountAmount;
    public decimal LineProfit => (UnitPrice - UnitCost) * Quantity - DiscountAmount;
    public bool CanIncreaseQuantity => Quantity < AvailableStock;
}
