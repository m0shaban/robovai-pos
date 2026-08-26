namespace SmartPOS.Core.Licensing;

public sealed record LicenseStatus(
    bool IsValid,
    bool IsInGrace,
    int DaysRemaining,
    DateTimeOffset? ExpiresAtUtc,
    string Reason,
    bool IsTrial = false
);
