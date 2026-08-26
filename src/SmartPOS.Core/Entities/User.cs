namespace SmartPOS.Core.Entities;

/// <summary>
/// System user entity
/// </summary>
public class User : BaseEntity
{
    public string Username { get; set; } = string.Empty;
    public string PasswordHash { get; set; } = string.Empty;
    public string FullName { get; set; } = string.Empty;
    public string? Email { get; set; }
    public string? Phone { get; set; }
    public UserRole Role { get; set; } = UserRole.Cashier;
    public Permissions Permissions { get; set; } = Permissions.None;
    public string? AdminPin { get; set; }
    public bool IsActive { get; set; } = true;
    public DateTime? LastLogin { get; set; }
    public decimal DailyMealLimit { get; set; } = 0; // الحد المسموح به للمسحوبات اليومية (للموظفين)

    // Navigation Properties
    public virtual ICollection<Sale> Sales { get; set; } = new List<Sale>();
    public virtual ICollection<Expense> Expenses { get; set; } = new List<Expense>();
}

public enum UserRole
{
    SuperAdmin = 0,
    Admin = 1,
    Manager = 2,
    Cashier = 3,
    Inventory = 4
}
