using System.Windows;
using System.Windows.Controls;
using System.Diagnostics;
using System.Windows.Navigation;
using Microsoft.Extensions.DependencyInjection;
using SmartPOS.Application.ViewModels;
using System.Text;
using SmartPOS.WPF.Views;

namespace SmartPOS.WPF.Views;

public partial class SettingsPage : Page
{
    public SettingsPage()
    {
        InitializeComponent();

        if (System.Windows.Application.Current is App app)
        {
            DataContext = app.Host.Services.GetRequiredService<SettingsViewModel>();
        }
    }

    private SettingsViewModel? Vm => DataContext as SettingsViewModel;

    private async Task ShowActivationWindowAndRefreshAsync()
    {
        if (System.Windows.Application.Current is not App app)
        {
            return;
        }

        var activationWindow = app.Host.Services.GetRequiredService<ActivationWindow>();
        activationWindow.Owner = Window.GetWindow(this);
        activationWindow.ShowDialog();

        if (Vm != null)
        {
            await Vm.RefreshActivationStatusAsync();
        }
    }

    private void CopyDeviceId_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            var deviceId = Vm?.DeviceId;
            if (!string.IsNullOrWhiteSpace(deviceId))
            {
                Clipboard.SetText(deviceId);
                MessageBox.Show("تم نسخ رقم الجهاز.", "التفعيل", MessageBoxButton.OK, MessageBoxImage.Information);
            }
        }
        catch
        {
            // ignore
        }
    }

    private void SendWhatsApp_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            var storeName = Vm?.StoreName ?? "";
            var storePhone = Vm?.StorePhone ?? "";
            var deviceId = Vm?.DeviceId ?? "";

            var sb = new StringBuilder();
            sb.AppendLine("مرحباً، أريد تفعيل RoboVAI POS");
            if (!string.IsNullOrWhiteSpace(storeName)) sb.AppendLine($"اسم المتجر: {storeName}");
            if (!string.IsNullOrWhiteSpace(storePhone)) sb.AppendLine($"رقم الهاتف: {storePhone}");
            if (!string.IsNullOrWhiteSpace(deviceId)) sb.AppendLine($"Device ID: {deviceId}");

            var text = Uri.EscapeDataString(sb.ToString());
            var url = $"https://wa.me/201121891913?text={text}";
            Process.Start(new ProcessStartInfo(url) { UseShellExecute = true });
        }
        catch
        {
            // ignore
        }
    }

    private async void EnterActivationCode_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            await ShowActivationWindowAndRefreshAsync();
        }
        catch
        {
            // ignore
        }
    }

    private async void OpenActivationWindow_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            await ShowActivationWindowAndRefreshAsync();
        }
        catch
        {
            // ignore
        }
    }

    private void OnRequestNavigate(object sender, RequestNavigateEventArgs e)
    {
        try
        {
            Process.Start(new ProcessStartInfo(e.Uri.AbsoluteUri) { UseShellExecute = true });
            e.Handled = true;
        }
        catch
        {
            // ignore
        }
    }

    private void OpenWmsBrowser_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            var targetUrl = !string.IsNullOrWhiteSpace(Vm?.WmsWebUrl) ? Vm.WmsWebUrl : "http://localhost:7890/wms/";
            Process.Start(new ProcessStartInfo(targetUrl) { UseShellExecute = true });
        }
        catch (Exception ex)
        {
            MessageBox.Show($"عذراً، تعذر فتح منصة WMS:\n{ex.Message}", "تنبيه", MessageBoxButton.OK, MessageBoxImage.Warning);
        }
    }

    private void OpenDashboardBrowser_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            var targetUrl = !string.IsNullOrWhiteSpace(Vm?.LanServerUrl) ? Vm.LanServerUrl : "http://localhost:7890/";
            Process.Start(new ProcessStartInfo(targetUrl) { UseShellExecute = true });
        }
        catch (Exception ex)
        {
            MessageBox.Show($"عذراً، تعذر فتح لوحة التحكم:\n{ex.Message}", "تنبيه", MessageBoxButton.OK, MessageBoxImage.Warning);
        }
    }

    private void OpenUserGuide_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            var localGuidePath = System.IO.Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "wms", "user-guide.html");
            if (System.IO.File.Exists(localGuidePath))
            {
                Process.Start(new ProcessStartInfo(localGuidePath) { UseShellExecute = true });
            }
            else
            {
                var targetUrl = !string.IsNullOrWhiteSpace(Vm?.LanServerUrl) ? $"{Vm.LanServerUrl}/wms/user-guide.html" : "http://localhost:7890/wms/user-guide.html";
                Process.Start(new ProcessStartInfo(targetUrl) { UseShellExecute = true });
            }
        }
        catch (Exception ex)
        {
            MessageBox.Show($"عذراً، تعذر فتح دليل الاستخدام:\n{ex.Message}", "تنبيه", MessageBoxButton.OK, MessageBoxImage.Warning);
        }
    }

    private void CopyWmsUrl_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            var url = !string.IsNullOrWhiteSpace(Vm?.WmsWebUrl) ? Vm.WmsWebUrl : "http://localhost:7890/wms/";
            Clipboard.SetText(url);
            MessageBox.Show($"تم نسخ رابط منصة المخازن بنجاح:\n{url}", "تم النسخ", MessageBoxButton.OK, MessageBoxImage.Information);
        }
        catch (Exception ex)
        {
            MessageBox.Show($"تعذر نسخ الرابط:\n{ex.Message}", "خطأ", MessageBoxButton.OK, MessageBoxImage.Warning);
        }
    }

    private void CopyDashboardUrl_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            var url = !string.IsNullOrWhiteSpace(Vm?.LanServerUrl) ? Vm.LanServerUrl : "http://localhost:7890/";
            Clipboard.SetText(url);
            MessageBox.Show($"تم نسخ رابط لوحة التحكم بنجاح:\n{url}", "تم النسخ", MessageBoxButton.OK, MessageBoxImage.Information);
        }
        catch (Exception ex)
        {
            MessageBox.Show($"تعذر نسخ الرابط:\n{ex.Message}", "خطأ", MessageBoxButton.OK, MessageBoxImage.Warning);
        }
    }

    private void Page_Unloaded(object sender, RoutedEventArgs e)
    {
        if (Vm is IDisposable disposable)
        {
            disposable.Dispose();
        }
    }
}
