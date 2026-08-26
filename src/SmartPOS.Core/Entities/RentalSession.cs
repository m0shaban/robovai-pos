using System;

namespace SmartPOS.Core.Entities;

public enum RentalSessionStatus
{
    Running,
    Completed,
    Cancelled
}

public class RentalSession : BaseEntity
{
    public int RentalDeviceId { get; set; }
    public virtual RentalDevice Device { get; set; } = null!;

    public DateTime StartTime { get; set; }
    
    /// <summary>
    /// If null, it's an open-ended session (stopwatch mode).
    /// If set, it's a fixed duration (countdown mode).
    /// </summary>
    public int? DurationMinutes { get; set; }
    
    public DateTime? ExpectedEndTime { get; set; }
    public DateTime? ActualEndTime { get; set; }

    public RentalSessionStatus Status { get; set; } = RentalSessionStatus.Running;

    public decimal HourlyRateApplied { get; set; }
    public decimal TotalAmount { get; set; }

    public string CustomerName { get; set; } = string.Empty;
    public string Notes { get; set; } = string.Empty;

    // Foreign Key to the Sale (Invoice) generated for this session
    public int? SaleId { get; set; }
    public virtual Sale? Sale { get; set; }
}
