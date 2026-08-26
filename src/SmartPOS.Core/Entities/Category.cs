namespace SmartPOS.Core.Entities;

/// <summary>
/// Category for product classification
/// </summary>
public class Category : BaseEntity
{
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string? IconName { get; set; }
    public string ColorCode { get; set; } = "#3F51B5";
    public bool IsActive { get; set; } = true;
    
    // Navigation Properties
    public virtual ICollection<Product> Products { get; set; } = new List<Product>();
}
