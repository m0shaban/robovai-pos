using System;

namespace SmartPOS.Core.Entities;

/// <summary>
/// Tracks sensitive actions performed by users in the system.
/// </summary>
public class AuditLog : BaseEntity
{
    public int UserId { get; set; }
    public virtual User User { get; set; } = null!;

    /// <summary>
    /// Describes the sensitive action (e.g., "VoidItem", "OpenDrawer", "HighDiscount")
    /// </summary>
    public string ActionType { get; set; } = string.Empty;

    /// <summary>
    /// Detailed description including what was voided, invoice number, etc.
    /// </summary>
    public string Details { get; set; } = string.Empty;

    /// <summary>
    /// If an Admin PIN was used to bypass a restriction, log the Admin's ID.
    /// </summary>
    public int? AuthorizedByAdminId { get; set; }
    public virtual User? AuthorizedByAdmin { get; set; }

    public DateTime Timestamp { get; set; } = DateTime.Now;
}
