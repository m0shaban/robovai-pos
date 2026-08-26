using SmartPOS.Core.Licensing;

namespace SmartPOS.Core.Interfaces;

public interface ILicenseService
{
    string GetDeviceId();
    Task<LicenseStatus> GetStatusAsync(CancellationToken cancellationToken = default);
    Task<LicenseStatus> ActivateAsync(string activationCode, CancellationToken cancellationToken = default);
    Task<LicenseStatus> StartTrialAsync(CancellationToken cancellationToken = default);
}
