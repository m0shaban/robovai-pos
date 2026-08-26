using Microsoft.EntityFrameworkCore;
using SmartPOS.Core.Entities;
using SmartPOS.Infrastructure.Data;

namespace SmartPOS.Application.Services;

public class AIStockWarning
{
    public Product Product { get; set; } = null!;
    public string ProductName => Product.Name;
    public int CurrentStock => Product.Stock;
    public decimal DailyVelocity { get; set; }
    public int DaysUntilStockout { get; set; }
    public int SuggestedRestockQuantity { get; set; }
    public string AlertMessage { get; set; } = string.Empty;
}

public class AIPredictionService
{
    private readonly IDbContextFactory<AppDbContext> _contextFactory;

    public AIPredictionService(IDbContextFactory<AppDbContext> contextFactory)
    {
        _contextFactory = contextFactory;
    }

    /// <summary>
    /// Analyzes the last N days of sales to predict stockouts.
    /// </summary>
    public async Task<List<AIStockWarning>> GetInventoryPredictionsAsync(int daysToAnalyze = 30, int warningThresholdDays = 7)
    {
        var startDate = DateTime.Today.AddDays(-daysToAnalyze);
        
        using var context = _contextFactory.CreateDbContext();

        // 1. Get all completed sale details within the period
        var saleDetails = await context.SaleDetails
            .AsNoTracking()
            .Include(sd => sd.Sale)
            .Include(sd => sd.Product)
            .Where(sd => sd.Sale.SaleDate >= startDate 
                         && sd.Sale.Status == SaleStatus.Completed 
                         && !sd.Sale.IsDeleted
                         && !sd.Product.IsDeleted
                         && sd.Product.IsActive)
            .ToListAsync();

        // 2. Group by ProductId to calculate velocity
        var productSales = saleDetails
            .GroupBy(sd => sd.ProductId)
            .Select(g => new
            {
                Product = g.First().Product,
                TotalSold = g.Sum(sd => sd.Quantity),
                DailyVelocity = (decimal)g.Sum(sd => sd.Quantity) / daysToAnalyze
            })
            .Where(p => p.DailyVelocity > 0) // Only analyze products that actually sell
            .ToList();

        var warnings = new List<AIStockWarning>();

        foreach (var item in productSales)
        {
            int daysUntilStockout = item.Product.Stock <= 0 
                ? 0 
                : (int)Math.Floor(item.Product.Stock / item.DailyVelocity);

            if (daysUntilStockout <= warningThresholdDays)
            {
                // Suggest ordering enough to cover the next 30 days
                int suggestedRestock = (int)Math.Ceiling(item.DailyVelocity * 30) - item.Product.Stock;
                if (suggestedRestock < 0) suggestedRestock = 0;

                string alertMsg = daysUntilStockout == 0 
                    ? "نفد المخزون أو سينفد اليوم!" 
                    : $"سينفد خلال {daysUntilStockout} أيام.";

                warnings.Add(new AIStockWarning
                {
                    Product = item.Product,
                    DailyVelocity = Math.Round(item.DailyVelocity, 2),
                    DaysUntilStockout = daysUntilStockout,
                    SuggestedRestockQuantity = suggestedRestock > 0 ? suggestedRestock : 10,
                    AlertMessage = alertMsg
                });
            }
        }

        return warnings.OrderBy(w => w.DaysUntilStockout).ToList();
    }
}
