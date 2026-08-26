namespace SmartPOS.Core.Entities;

/// <summary>
/// Return/Refund entity for managing product returns
/// </summary>
public class Return : BaseEntity
{
    public string ReturnNumber { get; set; } = string.Empty;
    public DateTime ReturnDate { get; set; } = DateTime.Now;
    public decimal TotalAmount { get; set; }
    public ReturnReason Reason { get; set; }
    public string? Notes { get; set; }
    public ReturnStatus Status { get; set; } = ReturnStatus.Pending;
    public bool IsRefunded { get; set; }
    public DateTime? RefundDate { get; set; }

    // Foreign Keys
    public int SaleId { get; set; }
    public int CustomerId { get; set; }
    public int ProcessedByUserId { get; set; }

    // Navigation Properties
    public virtual Sale Sale { get; set; } = null!;
    public virtual Customer Customer { get; set; } = null!;
    public virtual User ProcessedBy { get; set; } = null!;
    public virtual ICollection<ReturnDetail> ReturnDetails { get; set; } = new List<ReturnDetail>();
}

/// <summary>
/// Individual items in a return
/// </summary>
public class ReturnDetail : BaseEntity
{
    public int Quantity { get; set; }
    public decimal UnitPrice { get; set; }
    public decimal Subtotal { get; set; }
    public string? Reason { get; set; }

    // Foreign Keys
    public int ReturnId { get; set; }
    public int ProductId { get; set; }

    // Navigation Properties
    public virtual Return Return { get; set; } = null!;
    public virtual Product Product { get; set; } = null!;
}

public enum ReturnReason
{
    Defective = 1,
    WrongItem = 2,
    CustomerRequest = 3,
    Expired = 4,
    Damaged = 5,
    Other = 6
}

public enum ReturnStatus
{
    Pending = 1,
    Approved = 2,
    Rejected = 3,
    Completed = 4
}
