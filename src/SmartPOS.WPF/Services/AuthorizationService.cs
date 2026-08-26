using Microsoft.EntityFrameworkCore;
using SmartPOS.Core.Entities;
using SmartPOS.Core.Interfaces;
using SmartPOS.Infrastructure.Data;
using SmartPOS.WPF.Views.Dialogs;
using System;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;

namespace SmartPOS.WPF.Services;

public class AuthorizationService : IAuthorizationService
{
    private readonly IUserService _userService;
    private readonly IDbContextFactory<AppDbContext> _dbContextFactory;

    public AuthorizationService(IUserService userService, IDbContextFactory<AppDbContext> dbContextFactory)
    {
        _userService = userService;
        _dbContextFactory = dbContextFactory;
    }

    public bool HasPermission(Permissions permission)
    {
        return HasPermission(_userService.CurrentUser, permission);
    }

    public bool HasPermission(User? user, Permissions permission)
    {
        if (user == null) return false;
        
        // SuperAdmin has all permissions implicitly
        if (user.Role == UserRole.SuperAdmin || user.Role == UserRole.Admin || user.Permissions.HasFlag(Permissions.All))
            return true;

        // Fallback for Cashier if DB is corrupted or legacy user:
        if (user.Role == UserRole.Cashier && (permission == Permissions.AccessPOS || permission == Permissions.ManageShifts || permission == Permissions.ManageReturns))
            return true;

        return user.Permissions.HasFlag(permission);
    }

    public async Task<bool> RequestAdminOverrideAsync(string actionDescription)
    {
        // If current user is already an admin with override privileges, no PIN needed
        if (HasPermission(Permissions.ProvideAdminPin) || _userService.CurrentUser?.Role == UserRole.SuperAdmin)
        {
            return true;
        }

        bool authorized = false;

        await System.Windows.Application.Current.Dispatcher.InvokeAsync(async () =>
        {
            while (true)
            {
                var dialog = new AdminPinDialog(actionDescription);
                
                if (dialog.ShowDialog() != true)
                    break; // User cancelled
                
                var enteredPin = dialog.Pin;
                
                using var dbContext = _dbContextFactory.CreateDbContext();
                // Find any active user with ProvideAdminPin permission matching this PIN
                var authorizingAdmin = await dbContext.Users.AsNoTracking().FirstOrDefaultAsync(u => 
                    u.IsActive && 
                    u.AdminPin == enteredPin && 
                    (u.Role == UserRole.SuperAdmin || (u.Permissions & Permissions.ProvideAdminPin) == Permissions.ProvideAdminPin)
                );

                if (authorizingAdmin != null)
                {
                    authorized = true;
                    // Log the override
                    await LogAuditAsync($"AdminOverride: {actionDescription}", $"Authorized by {authorizingAdmin.Username}", authorizingAdmin.Id);
                    break;
                }
                else
                {
                    MessageBox.Show("الرمز السري غير صحيح أو لا تملك صلاحية المصادقة", "خطأ", MessageBoxButton.OK, MessageBoxImage.Warning);
                    // loop continues, new dialog will be created
                }
            }
        });

        return authorized;
    }

    public async Task LogAuditAsync(string actionType, string details, int? authorizedByAdminId = null)
    {
        if (_userService.CurrentUser == null) return;

        var log = new AuditLog
        {
            UserId = _userService.CurrentUser.Id,
            ActionType = actionType,
            Details = details,
            AuthorizedByAdminId = authorizedByAdminId,
            Timestamp = DateTime.Now
        };

        using var dbContext = _dbContextFactory.CreateDbContext();
        dbContext.AuditLogs.Add(log);
        await dbContext.SaveChangesAsync();
    }
}
