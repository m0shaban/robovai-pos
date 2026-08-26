using System.Threading.Tasks;
using SmartPOS.Core.Entities;

namespace SmartPOS.Core.Interfaces;

public interface IAuthorizationService
{
    /// <summary>
    /// Checks if the current user has the required permission.
    /// </summary>
    bool HasPermission(Permissions permission);

    /// <summary>
    /// Checks if a user has a specific permission.
    /// </summary>
    bool HasPermission(User user, Permissions permission);

    /// <summary>
    /// Prompts for an Admin PIN to bypass a restricted action.
    /// Returns true if the correct PIN is provided by an authorized Admin.
    /// </summary>
    Task<bool> RequestAdminOverrideAsync(string actionDescription);

    /// <summary>
    /// Logs a sensitive action to the AuditLog.
    /// </summary>
    Task LogAuditAsync(string actionType, string details, int? authorizedByAdminId = null);
}
