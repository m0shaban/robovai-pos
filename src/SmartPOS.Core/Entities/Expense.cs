namespace SmartPOS.Core.Entities;

/// <summary>
/// Expense tracking entity
/// </summary>
public class Expense : BaseEntity
{
    public string Description { get; set; } = string.Empty;
    public decimal Amount { get; set; }
    public DateTime ExpenseDate { get; set; } = DateTime.Now;
    public ExpenseCategory Category { get; set; }
    public string? Receipt { get; set; }
    public string? Notes { get; set; }
    
    // Foreign Keys
    public int UserId { get; set; }
    
    // Navigation Properties
    public virtual User User { get; set; } = null!;
}

public enum ExpenseCategory
{
    Rent = 1,
    Utilities = 2,
    Salaries = 3,
    Supplies = 4,
    Maintenance = 5,
    Marketing = 6,
    Transportation = 7,
    Other = 8
}
