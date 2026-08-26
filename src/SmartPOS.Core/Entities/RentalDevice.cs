namespace SmartPOS.Core.Entities;

public enum DeviceType
{
    PlayStation,
    Billiard,
    PingPong,
    Tennis,
    Other
}

public class RentalDevice : BaseEntity
{
    public string Name { get; set; } = string.Empty;
    public DeviceType Type { get; set; } = DeviceType.PlayStation;
    
    /// <summary>
    /// Price per hour
    /// </summary>
    public decimal HourlyRate { get; set; }
    
    public bool IsActive { get; set; } = true;
}
