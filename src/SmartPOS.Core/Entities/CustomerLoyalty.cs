namespace SmartPOS.Core.Entities;

/// <summary>
/// Customer loyalty points tracking
/// </summary>
public class CustomerLoyalty : BaseEntity
{
    public int Points { get; set; }
    public int TotalPointsEarned { get; set; }
    public int TotalPointsRedeemed { get; set; }
    public LoyaltyTier Tier { get; set; } = LoyaltyTier.Bronze;
    public DateTime? LastTierUpdate { get; set; }

    // Foreign Keys
    public int CustomerId { get; set; }

    // Navigation Properties
    public virtual Customer Customer { get; set; } = null!;
    public virtual ICollection<LoyaltyTransaction> Transactions { get; set; } = new List<LoyaltyTransaction>();
}

/// <summary>
/// Individual loyalty point transactions
/// </summary>
public class LoyaltyTransaction : BaseEntity
{
    public int Points { get; set; }
    public LoyaltyTransactionType Type { get; set; }
    public string? Description { get; set; }
    public decimal? RelatedAmount { get; set; }

    // Foreign Keys
    public int CustomerLoyaltyId { get; set; }
    public int? SaleId { get; set; }

    // Navigation Properties
    public virtual CustomerLoyalty CustomerLoyalty { get; set; } = null!;
    public virtual Sale? Sale { get; set; }
}

public enum LoyaltyTier
{
    Bronze = 1,      // 0-999 points
    Silver = 2,      // 1000-2999 points
    Gold = 3,        // 3000-4999 points
    Platinum = 4     // 5000+ points
}

public enum LoyaltyTransactionType
{
    Earned = 1,
    Redeemed = 2,
    Expired = 3,
    Adjusted = 4
}
