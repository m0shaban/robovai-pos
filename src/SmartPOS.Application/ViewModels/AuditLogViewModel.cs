using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.EntityFrameworkCore;
using SmartPOS.Application.Extensions;
using SmartPOS.Core.Entities;
using SmartPOS.Infrastructure.Data;

namespace SmartPOS.Application.ViewModels;

/// <summary>
/// ViewModel for Audit Logs, refactored to use IDbContextFactory (v5.1 Factory Pattern)
/// </summary>
public partial class AuditLogViewModel : BaseViewModel
{
    private readonly IDbContextFactory<AppDbContext> _contextFactory;

    [ObservableProperty]
    private ObservableCollection<AuditLog> _auditLogs = new();

    [ObservableProperty]
    private ObservableCollection<User> _users = new();

    [ObservableProperty]
    private User? _filterUser;

    [ObservableProperty]
    private string _filterAction = string.Empty;

    [ObservableProperty]
    private DateTime _startDate = DateTime.Now.AddDays(-7);

    [ObservableProperty]
    private DateTime _endDate = DateTime.Now;

    [ObservableProperty]
    private string _searchText = string.Empty;

    public AuditLogViewModel(IDbContextFactory<AppDbContext> contextFactory)
    {
        _contextFactory = contextFactory;
        _ = InitializeAsync();
    }

    private async Task InitializeAsync()
        => await ExecuteBusyAsync(LoadAsync, "⏳ جاري تحميل سجل النشاط...", "✅ تم التحميل");

    private async Task LoadAsync()
    {
        await using var ctx = await _contextFactory.CreateDbContextAsync();
        var users = await ctx.Users.AsNoTracking()
            .Where(u => !u.IsDeleted)
            .OrderBy(u => u.FullName)
            .ToListAsync();
        Users.SyncWith(users);

        await ApplyFilterAsync();
    }

    private async Task ApplyFilterAsync()
    {
        await using var ctx = await _contextFactory.CreateDbContextAsync();
        var start = StartDate.Date;
        var end   = EndDate.Date.AddDays(1);

        var query = ctx.AuditLogs
            .AsNoTracking()
            .Include(a => a.User)
            .Include(a => a.AuthorizedByAdmin)
            .Where(a => a.Timestamp >= start && a.Timestamp < end);

        if (FilterUser != null)
            query = query.Where(a => a.UserId == FilterUser.Id);

        if (!string.IsNullOrWhiteSpace(FilterAction))
            query = query.Where(a => a.ActionType.Contains(FilterAction));

        if (!string.IsNullOrWhiteSpace(SearchText))
            query = query.Where(a => a.Details.Contains(SearchText));

        var logs = await query.OrderByDescending(a => a.Timestamp).Take(500).ToListAsync();
        AuditLogs.SyncWith(logs);
    }

    [RelayCommand]
    private async Task FilterAsync()
        => await ExecuteBusyAsync(ApplyFilterAsync, "جاري البحث...");

    [RelayCommand]
    private async Task RefreshAsync()
        => await ExecuteBusyAsync(LoadAsync, "جاري التحديث...");
}
