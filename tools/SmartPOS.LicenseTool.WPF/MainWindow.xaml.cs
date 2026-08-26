using Microsoft.Win32;
using SmartPOS.Core.Licensing;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Windows;

namespace SmartPOS.LicenseTool.WPF;

public partial class MainWindow : Window
{
    // As requested
    private const string AdminEmail = "adminpos";
    private const string AdminPassword = "adminpos123";

    public MainWindow()
    {
        InitializeComponent();
        EmailBox.Focus();
    }

    private void Login_Click(object sender, RoutedEventArgs e)
    {
        var email = (EmailBox.Text ?? string.Empty).Trim();
        var password = PasswordBox.Password ?? string.Empty;

        if (!string.Equals(email, AdminEmail, StringComparison.Ordinal) || !string.Equals(password, AdminPassword, StringComparison.Ordinal))
        {
            LoginError.Text = "بيانات الدخول غير صحيحة.";
            LoginError.Visibility = Visibility.Visible;
            return;
        }

        LoginError.Visibility = Visibility.Collapsed;
        LoginPanel.Visibility = Visibility.Collapsed;
        GeneratorPanel.Visibility = Visibility.Visible;
    }

    private void Logout_Click(object sender, RoutedEventArgs e)
    {
        GeneratorPanel.Visibility = Visibility.Collapsed;
        LoginPanel.Visibility = Visibility.Visible;
        PasswordBox.Password = string.Empty;
        EmailBox.Text = string.Empty;
        CodeBox.Text = string.Empty;
        PrivateKeyPathBox.Text = string.Empty;
        GenError.Visibility = Visibility.Collapsed;
        ExpiryInfo.Text = string.Empty;
        EmailBox.Focus();
    }

    private void BrowseKey_Click(object sender, RoutedEventArgs e)
    {
        var dlg = new OpenFileDialog
        {
            Title = "اختر ملف private key (PEM)",
            Filter = "PEM files (*.pem)|*.pem|All files (*.*)|*.*",
            CheckFileExists = true
        };

        if (dlg.ShowDialog(this) == true)
        {
            PrivateKeyPathBox.Text = dlg.FileName;
        }
    }

    private void PasteDeviceId_Click(object sender, RoutedEventArgs e)
    {
        if (Clipboard.ContainsText())
        {
            DeviceIdBox.Text = Clipboard.GetText().Trim();
        }
    }

    private void Generate_Click(object sender, RoutedEventArgs e)
    {
        GenError.Visibility = Visibility.Collapsed;
        ExpiryInfo.Text = string.Empty;

        var customer = (CustomerBox.Text ?? string.Empty).Trim();
        var deviceId = (DeviceIdBox.Text ?? string.Empty).Trim();
        var privPath = (PrivateKeyPathBox.Text ?? string.Empty).Trim();

        if (string.IsNullOrWhiteSpace(customer))
        {
            ShowGenError("أدخل اسم العميل.");
            return;
        }

        if (string.IsNullOrWhiteSpace(deviceId))
        {
            ShowGenError("أدخل Device ID.");
            return;
        }

        if (string.IsNullOrWhiteSpace(privPath) || !File.Exists(privPath))
        {
            ShowGenError("اختر ملف private key الصحيح.");
            return;
        }

        var issued = DateTimeOffset.UtcNow;
        DateTimeOffset expires;
        int planMonths;

        if (TrialRadio.IsChecked == true)
        {
            planMonths = 0;
            expires = issued.AddDays(14);
        }
        else if (CustomDaysRadio.IsChecked == true)
        {
            if (!int.TryParse((CustomDaysBox.Text ?? string.Empty).Trim(), out var days) || days <= 0)
            {
                ShowGenError("أدخل عدد أيام صحيح.");
                return;
            }

            planMonths = 0;
            expires = issued.AddDays(days);
        }
        else
        {
            planMonths = MonthlyRadio.IsChecked == true ? 1 : (SixRadio.IsChecked == true ? 6 : 12);
            expires = issued.AddMonths(planMonths);
        }

        var expiryStr = (SixRadio.IsChecked == false && MonthlyRadio.IsChecked == false) ? "LIFETIME" : expires.ToString("yyyy-MM-dd", System.Globalization.CultureInfo.InvariantCulture);

        var payload = new LicensePayload(
            MachineId: deviceId,
            PlanName: planMonths > 0 ? $"{planMonths}-Months" : "Lifetime",
            Expiry: expiryStr,
            GeneratedAt: DateTime.UtcNow.ToString("yyyy-MM-ddTHH:mm:ssZ"),
            OrderId: 1, OrderItemId: 1, Seat: 1, ProductId: "SmartPOS-PRO");

        var payloadJson = JsonSerializer.Serialize(payload, new JsonSerializerOptions { WriteIndented = false });
        var payloadBytes = Encoding.UTF8.GetBytes(payloadJson);

        try
        {
            var offlineSecret = "SmartPOS-Offline-Secret-Key-2026";
            var secretSalt = "store.license-key.v1";
            var secretBytes = Encoding.UTF8.GetBytes($"{offlineSecret}:{secretSalt}");
            var payloadB64 = Base64Url.Encode(payloadBytes);
            var sigBytes = HMACSHA256.HashData(secretBytes, Encoding.UTF8.GetBytes(payloadB64));
            var signatureB64 = Base64Url.Encode(sigBytes);

            var token = $"{payloadB64}.{signatureB64}";
            CodeBox.Text = token;

            ExpiryInfo.Text = expiryStr == "LIFETIME" ? "الترخيص: مدى الحياة (Lifetime)" : $"ينتهي في: {expires.ToLocalTime():yyyy/MM/dd}";
        }
        catch (Exception ex)
        {
            ShowGenError($"فشل توليد الكود: {ex.Message}");
        }
    }

    private void CopyCode_Click(object sender, RoutedEventArgs e)
    {
        var code = CodeBox.Text ?? string.Empty;
        if (string.IsNullOrWhiteSpace(code))
        {
            return;
        }

        Clipboard.SetText(code);
    }

    private void Clear_Click(object sender, RoutedEventArgs e)
    {
        CodeBox.Text = string.Empty;
        ExpiryInfo.Text = string.Empty;
        GenError.Visibility = Visibility.Collapsed;
    }

    private void ShowGenError(string message)
    {
        GenError.Text = message;
        GenError.Visibility = Visibility.Visible;
    }

    private static class Base64Url
    {
        public static string Encode(byte[] data)
        {
            return Convert.ToBase64String(data)
                .TrimEnd('=')
                .Replace('+', '-')
                .Replace('/', '_');
        }
    }
}
