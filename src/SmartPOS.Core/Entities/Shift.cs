namespace SmartPOS.Core.Entities;

/// <summary>
/// Shift entity for managing cashier work shifts
/// </summary>
public class Shift : BaseEntity
{
    public DateTime StartTime { get; set; }
    public DateTime? EndTime { get; set; }
    public decimal OpeningBalance { get; set; }
    public decimal? ClosingBalance { get; set; }
    public decimal? ExpectedBalance { get; set; }
    public decimal? Difference { get; set; }
    public ShiftStatus Status { get; set; } = ShiftStatus.Open;
    public string? Notes { get; set; }

    // Foreign Keys
    public int UserId { get; set; }

    // Navigation Properties
    public virtual User User { get; set; } = null!;
    public virtual ICollection<Sale> Sales { get; set; } = new List<Sale>();

    // Computed Properties
    [System.ComponentModel.DataAnnotations.Schema.NotMapped]
    public decimal TotalSales { get; set; }
    
    [System.ComponentModel.DataAnnotations.Schema.NotMapped]
    public decimal TotalCash { get; set; }
    
    [System.ComponentModel.DataAnnotations.Schema.NotMapped]
    public decimal TotalCard { get; set; }
    
    [System.ComponentModel.DataAnnotations.Schema.NotMapped]
    public decimal TotalTransfer { get; set; }
    
    [System.ComponentModel.DataAnnotations.Schema.NotMapped]
    public decimal TotalVodafoneCash { get; set; }
    
    [System.ComponentModel.DataAnnotations.Schema.NotMapped]
    public decimal TotalInstaPay { get; set; }
    
    [System.ComponentModel.DataAnnotations.Schema.NotMapped]
    public int TransactionCount { get; set; }
    
    public TimeSpan? Duration => EndTime.HasValue ? EndTime.Value - StartTime : DateTime.Now - StartTime;
}

public enum ShiftStatus
{
    Open = 1,
    Closed = 2,
    Reconciled = 3
}
