using SmartPOS.Core.Interfaces;
using System.IO;
using System.ComponentModel;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Windows;
using System.Windows.Navigation;

namespace SmartPOS.WPF.Views;

public partial class ActivationWindow : Window, INotifyPropertyChanged
{
    private readonly ILicenseService _licenseService;
    private const string ActivationWebsiteUrl = "https://robovai.tech";

    public event PropertyChangedEventHandler? PropertyChanged;

    private string _deviceId = string.Empty;
    public string DeviceId
    {
        get => _deviceId;
        set { _deviceId = value; OnPropertyChanged(); }
    }

    private string _statusMessage = "";
    public string StatusMessage
    {
        get => _statusMessage;
        set { _statusMessage = value; OnPropertyChanged(); }
    }

    private string _expiryLine = "";
    public string ExpiryLine
    {
        get => _expiryLine;
        set { _expiryLine = value; OnPropertyChanged(); }
    }

    public ActivationWindow(ILicenseService licenseService)
    {
        InitializeComponent();
        _licenseService = licenseService;
        DataContext = this;

        DeviceId = _licenseService.GetDeviceId();
        ExpiryLine = "";
        StatusMessage = "أرسل رقم الجهاز لك للحصول على كود التفعيل.";
    }

    private async void ActivationWindow_Loaded(object sender, RoutedEventArgs e)
    {
        var status = await _licenseService.GetStatusAsync();
        UpdateExpiryLine(status);
        UpdateStatusMessage(status);
    }

    private void CopyDeviceId_Click(object sender, RoutedEventArgs e)
    {
        Clipboard.SetText(DeviceId);
        StatusMessage = "تم نسخ رقم الجهاز.";
    }

    private async void Activate_Click(object sender, RoutedEventArgs e)
    {
        var code = CodeBox.Text?.Trim();
        if (string.IsNullOrWhiteSpace(code))
        {
            StatusMessage = "أدخل كود التفعيل أولاً.";
            return;
        }

        StatusMessage = "جارٍ التحقق من الكود...";

        try
        {
            // Step 1: Try to activate
            var activateResult = await _licenseService.ActivateAsync(code);

            // Log for diagnostics
            var logDir = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "SmartPOS");
            Directory.CreateDirectory(logDir);
            var logPath = Path.Combine(logDir, "activation_debug.log");
            var logLine = $"[{DateTime.Now:yyyy-MM-dd HH:mm:ss}] ActivateAsync => Valid={activateResult.IsValid}, Grace={activateResult.IsInGrace}, Reason={activateResult.Reason}, Days={activateResult.DaysRemaining}\n";
            await File.AppendAllTextAsync(logPath, logLine);

            if (activateResult.IsValid || activateResult.IsInGrace)
            {
                StatusMessage = "تم التفعيل بنجاح! ✓";
                await Task.Delay(500);
                DialogResult = true;
                Close();
                return;
            }

            // Step 2: If direct result failed, try GetStatus (maybe it was saved but returned wrong status)
            var fallbackStatus = await _licenseService.GetStatusAsync();
            await File.AppendAllTextAsync(logPath, $"[{DateTime.Now:yyyy-MM-dd HH:mm:ss}] GetStatusAsync => Valid={fallbackStatus.IsValid}, Reason={fallbackStatus.Reason}\n");

            if (fallbackStatus.IsValid || fallbackStatus.IsInGrace)
            {
                StatusMessage = "تم التفعيل بنجاح! ✓";
                await Task.Delay(500);
                DialogResult = true;
                Close();
                return;
            }

            // Show failure with exact reason
            UpdateExpiryLine(activateResult);
            StatusMessage = $"فشل التفعيل: {activateResult.Reason}";
        }
        catch (Exception ex)
        {
            StatusMessage = $"خطأ أثناء التفعيل: {ex.Message}";
        }
    }

    private async void StartTrial_Click(object sender, RoutedEventArgs e)
    {
        var status = await _licenseService.StartTrialAsync();
        status = await _licenseService.GetStatusAsync();
        UpdateExpiryLine(status);
        UpdateStatusMessage(status);

        if (status.IsValid)
        {
            DialogResult = true;
            Close();
        }
    }

    private void Exit_Click(object sender, RoutedEventArgs e)
    {
        DialogResult = false;
        Close();
    }

    private void UpdateExpiryLine(SmartPOS.Core.Licensing.LicenseStatus status)
    {
        if (status.ExpiresAtUtc is null)
        {
            ExpiryLine = "";
            return;
        }

        var local = status.ExpiresAtUtc.Value.ToLocalTime();
        var remaining = status.DaysRemaining;
        ExpiryLine = $"ينتهي في: {local:yyyy/MM/dd HH:mm} — متبقي: {remaining} يوم";
    }

    private void UpdateStatusMessage(SmartPOS.Core.Licensing.LicenseStatus status)
    {
        if (status.IsValid && status.IsTrial)
        {
            StatusMessage = $"التجربة المجانية مفعلة. متبقي {Math.Max(0, status.DaysRemaining)} يوم.";
            return;
        }

        if (!status.IsValid && status.IsTrial)
        {
            StatusMessage = $"انتهت التجربة المجانية. اطلب كود التفعيل عبر {ActivationWebsiteUrl}.";
            return;
        }

        if (status.IsValid || status.IsInGrace)
        {
            StatusMessage = "تم التفعيل بنجاح.";
            return;
        }

        StatusMessage = "أرسل رقم الجهاز لك للحصول على كود التفعيل.";
    }

    private void ActivationLink_RequestNavigate(object sender, RequestNavigateEventArgs e)
    {
        try
        {
            Process.Start(new ProcessStartInfo(e.Uri.AbsoluteUri) { UseShellExecute = true });
        }
        catch
        {
            // ignore
        }

        e.Handled = true;
    }

    private void OnPropertyChanged([CallerMemberName] string? propertyName = null)
        => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
}
