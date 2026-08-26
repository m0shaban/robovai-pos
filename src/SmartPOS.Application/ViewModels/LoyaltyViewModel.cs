using System.Collections.ObjectModel;
using System.Windows;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.EntityFrameworkCore;
using SmartPOS.Application.Extensions;
using SmartPOS.Core.Entities;
using SmartPOS.Infrastructure.Data;

namespace SmartPOS.Application.ViewModels;

/// <summary>
/// ViewModel for Loyalty (v5.1 Factory Pattern)
/// </summary>
public partial class LoyaltyViewModel : BaseViewModel
{
    private readonly IDbContextFactory<AppDbContext> _contextFactory;

    [ObservableProperty]
    private ObservableCollection<Customer> _customers = new();

    private List<CustomerLoyalty> _allLoyalties = new();

    [ObservableProperty]
    private ObservableCollection<CustomerLoyalty> _loyalties = new();

    [ObservableProperty]
    private string _searchText = string.Empty;

    [ObservableProperty]
    private Customer? _selectedCustomer;

    [ObservableProperty]
    private int _pointsInput = 10;

    [ObservableProperty]
    private string? _notesInput;

    [ObservableProperty]
    private int _bronzeCount;

    [ObservableProperty]
    private int _silverCount;

    [ObservableProperty]
    private int _goldCount;

    [ObservableProperty]
    private int _platinumCount;

    public LoyaltyViewModel(IDbContextFactory<AppDbContext> contextFactory)
    {
        _contextFactory = contextFactory;
        _ = InitializeAsync();
    }

    private async Task InitializeAsync()
    {
        await ExecuteBusyAsync(LoadCoreAsync, "⏳ جاري تحميل الولاء...", "✅ تم تحميل بيانات الولاء");
    }

    private async Task LoadCoreAsync()
    {
        await using var ctx = await _contextFactory.CreateDbContextAsync();
        var customers = await ctx.Customers
            .AsNoTracking()
            .Where(c => !c.IsDeleted)
            .OrderBy(c => c.Name)
            .ToListAsync();
            
        Customers.SyncWith(customers);

        var loyalties = await ctx.CustomerLoyalties
            .AsNoTracking()
            .Include(l => l.Customer)
            .OrderByDescending(l => l.Points)
            .ToListAsync();
            
        _allLoyalties = loyalties;
        FilterLoyalties();

        CalculateTierCounts(loyalties);
    }

    partial void OnSearchTextChanged(string value) => FilterLoyalties();

    private void FilterLoyalties()
    {
        var query = _allLoyalties.AsEnumerable();

        if (!string.IsNullOrWhiteSpace(SearchText))
        {
            query = query.Where(l =>
                (l.Customer?.Name?.Contains(SearchText, StringComparison.OrdinalIgnoreCase) ?? false) ||
                (l.Customer?.Phone?.Contains(SearchText, StringComparison.OrdinalIgnoreCase) ?? false));
        }

        Loyalties.SyncWith(query);
    }

    [RelayCommand]
    private async Task LoadAsync()
    {
        await ExecuteBusyAsync(LoadCoreAsync, "جاري تحديث بيانات الولاء...");
    }

    [RelayCommand]
    private async Task AddPointsAsync()
    {
        if (SelectedCustomer == null)
        {
            MessageBox.Show("اختر العميل أولاً", "تنبيه", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        if (PointsInput <= 0)
        {
            MessageBox.Show("عدد النقاط غير صالح", "تنبيه", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        await ExecuteBusyAsync(async () =>
        {
            var loyalty = await EnsureLoyaltyAsync(SelectedCustomer.Id);

            await using var ctx = await _contextFactory.CreateDbContextAsync();
            ctx.CustomerLoyalties.Update(loyalty);

            loyalty.Points += PointsInput;
            loyalty.TotalPointsEarned += PointsInput;
            loyalty.Tier = CalculateTier(loyalty.Points);
            loyalty.LastTierUpdate = DateTime.Now;

            ctx.LoyaltyTransactions.Add(new LoyaltyTransaction
            {
                CustomerLoyaltyId = loyalty.Id,
                Points = PointsInput,
                Type = LoyaltyTransactionType.Earned,
                Description = NotesInput,
                CreatedAt = DateTime.Now
            });

            await ctx.SaveChangesAsync();
            
            PointsInput = 10;
            NotesInput = string.Empty;
            
            await LoadCoreAsync();
            MessageBox.Show("تم إضافة النقاط بنجاح", "نجح", MessageBoxButton.OK, MessageBoxImage.Information);
        }, "جاري إضافة النقاط...");
    }

    [RelayCommand]
    private async Task RedeemPointsAsync()
    {
        if (SelectedCustomer == null)
        {
            MessageBox.Show("اختر العميل أولاً", "تنبيه", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        if (PointsInput <= 0)
        {
            MessageBox.Show("عدد النقاط غير صالح", "تنبيه", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        await ExecuteBusyAsync(async () =>
        {
            var loyalty = await EnsureLoyaltyAsync(SelectedCustomer.Id);

            if (loyalty.Points < PointsInput)
            {
                MessageBox.Show("النقاط غير كافية", "تنبيه", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            await using var ctx = await _contextFactory.CreateDbContextAsync();
            ctx.CustomerLoyalties.Update(loyalty);

            loyalty.Points -= PointsInput;
            loyalty.TotalPointsRedeemed += PointsInput;
            loyalty.Tier = CalculateTier(loyalty.Points);
            loyalty.LastTierUpdate = DateTime.Now;

            ctx.LoyaltyTransactions.Add(new LoyaltyTransaction
            {
                CustomerLoyaltyId = loyalty.Id,
                Points = PointsInput,
                Type = LoyaltyTransactionType.Redeemed,
                Description = NotesInput,
                CreatedAt = DateTime.Now
            });

            await ctx.SaveChangesAsync();
            
            PointsInput = 10;
            NotesInput = string.Empty;
            
            await LoadCoreAsync();
            MessageBox.Show("تم استبدال النقاط بنجاح", "نجح", MessageBoxButton.OK, MessageBoxImage.Information);
        }, "جاري استبدال النقاط...");
    }

    private async Task<CustomerLoyalty> EnsureLoyaltyAsync(int customerId)
    {
        await using var ctx = await _contextFactory.CreateDbContextAsync();
        var loyalty = await ctx.CustomerLoyalties
            .FirstOrDefaultAsync(l => l.CustomerId == customerId);

        if (loyalty == null)
        {
            loyalty = new CustomerLoyalty
            {
                CustomerId = customerId,
                Points = 0,
                TotalPointsEarned = 0,
                TotalPointsRedeemed = 0,
                Tier = LoyaltyTier.Bronze,
                CreatedAt = DateTime.Now
            };
            ctx.CustomerLoyalties.Add(loyalty);
            await ctx.SaveChangesAsync();
        }

        return loyalty;
    }

    private LoyaltyTier CalculateTier(int points)
    {
        if (points >= 5000) return LoyaltyTier.Platinum;
        if (points >= 3000) return LoyaltyTier.Gold;
        if (points >= 1000) return LoyaltyTier.Silver;
        return LoyaltyTier.Bronze;
    }

    private void CalculateTierCounts(List<CustomerLoyalty> loyalties)
    {
        BronzeCount = loyalties.Count(l => l.Tier == LoyaltyTier.Bronze);
        SilverCount = loyalties.Count(l => l.Tier == LoyaltyTier.Silver);
        GoldCount = loyalties.Count(l => l.Tier == LoyaltyTier.Gold);
        PlatinumCount = loyalties.Count(l => l.Tier == LoyaltyTier.Platinum);
    }
}
