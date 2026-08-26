using SmartPOS.LicenseTool.Gui.Services;
using System.Globalization;
using System.Windows;

namespace SmartPOS.LicenseTool.Gui.Views;

public partial class MainWindow : Window
{
    public MainWindow()
    {
        InitializeComponent();

        DurationBox.ItemsSource = new[]
        {
            "Trial (14 days)",
            "Monthly (1 month)",
            "6 months",
            "Yearly (12 months)",
            "Custom days"
        };
        DurationBox.SelectedIndex = 0;

        CustomDaysBox.Text = "14";

        var defaultPriv = LicenseGenerator.GetDefaultPrivateKeyPath();
        if (!string.IsNullOrWhiteSpace(defaultPriv))
        {
            PrivateKeyBox.Text = defaultPriv;
        }

        var defaultKeyOut = System.IO.Path.GetFullPath(System.IO.Path.Combine(AppContext.BaseDirectory, "..", "keys"));
        KeyOutDirBox.Text = defaultKeyOut;
    }

    private void OnDurationChanged(object sender, System.Windows.Controls.SelectionChangedEventArgs e)
    {
        var isCustom = DurationBox.SelectedIndex == 4;
        CustomDaysBox.IsEnabled = isCustom;
        Set14Button.IsEnabled = isCustom;
    }

    private void OnBrowsePrivateKey(object sender, RoutedEventArgs e)
    {
        var dlg = new Microsoft.Win32.OpenFileDialog
        {
            Filter = "PEM files (*.pem)|*.pem|All files (*.*)|*.*",
            CheckFileExists = true
        };

        if (dlg.ShowDialog(this) == true)
        {
            PrivateKeyBox.Text = dlg.FileName;
        }
    }

    private void OnBrowseKeyOutDir(object sender, RoutedEventArgs e)
    {
        using var dlg = new System.Windows.Forms.FolderBrowserDialog
        {
            Description = "Select output folder for keys",
            UseDescriptionForTitle = true,
            ShowNewFolderButton = true
        };

        var result = dlg.ShowDialog();
        if (result == System.Windows.Forms.DialogResult.OK && !string.IsNullOrWhiteSpace(dlg.SelectedPath))
        {
            KeyOutDirBox.Text = dlg.SelectedPath;
        }
    }

    private void OnGenerateKeys(object sender, RoutedEventArgs e)
    {
        StatusText.Foreground = System.Windows.Media.Brushes.DarkGreen;
        StatusText.Text = string.Empty;

        var outDir = (KeyOutDirBox.Text ?? string.Empty).Trim().Trim('"');
        var r = LicenseGenerator.GenerateKeys(outDir);
        if (!r.Success)
        {
            StatusText.Foreground = System.Windows.Media.Brushes.DarkRed;
            StatusText.Text = r.Error;
            return;
        }

        StatusText.Text = $"Keys generated:\nPrivate: {r.PrivateKeyPath}\nPublic: {r.PublicKeyPath}";
        PrivateKeyBox.Text = r.PrivateKeyPath;
    }

    private void OnPasteDeviceId(object sender, RoutedEventArgs e)
    {
        if (System.Windows.Clipboard.ContainsText())
        {
            DeviceIdBox.Text = System.Windows.Clipboard.GetText().Trim();
        }
    }

    private void OnSet14Days(object sender, RoutedEventArgs e)
    {
        CustomDaysBox.Text = "14";
    }

    private void OnGenerate(object sender, RoutedEventArgs e)
    {
        StatusText.Text = string.Empty;
        ExpiryText.Text = string.Empty;

        var duration = GetDurationFromUi();
        if (duration is null)
        {
            return;
        }

        var r = LicenseGenerator.Generate(
            privateKeyPemPath: (PrivateKeyBox.Text ?? string.Empty).Trim().Trim('"'),
            deviceId: (DeviceIdBox.Text ?? string.Empty).Trim(),
            customerName: (CustomerBox.Text ?? string.Empty).Trim(),
            duration: duration.Value);

        if (!r.Success)
        {
            System.Windows.MessageBox.Show(this, r.Error, "Generate failed", MessageBoxButton.OK, MessageBoxImage.Error);
            return;
        }

        TokenBox.Text = r.Token;
        ExpiryText.Text = $"Expires: {r.ExpiresAtUtc.ToLocalTime():yyyy-MM-dd HH:mm} (local)";
    }

    private LicenseDuration? GetDurationFromUi()
    {
        return DurationBox.SelectedIndex switch
        {
            0 => new LicenseDuration(LicenseDurationKind.Trial14Days),
            1 => new LicenseDuration(LicenseDurationKind.Monthly1),
            2 => new LicenseDuration(LicenseDurationKind.SixMonths6),
            3 => new LicenseDuration(LicenseDurationKind.Yearly12),
            4 => ParseCustomDays(),
            _ => null
        };
    }

    private LicenseDuration? ParseCustomDays()
    {
        var text = (CustomDaysBox.Text ?? string.Empty).Trim();
        if (!int.TryParse(text, NumberStyles.Integer, CultureInfo.InvariantCulture, out var days) || days <= 0)
        {
            System.Windows.MessageBox.Show(this, "Custom days must be a positive number.", "Invalid days", MessageBoxButton.OK, MessageBoxImage.Warning);
            return null;
        }

        return new LicenseDuration(LicenseDurationKind.CustomDays, days);
    }

    private void OnCopy(object sender, RoutedEventArgs e)
    {
        var text = (TokenBox.Text ?? string.Empty).Trim();
        if (string.IsNullOrWhiteSpace(text))
        {
            return;
        }

        System.Windows.Clipboard.SetText(text);
        ExpiryText.Text = string.IsNullOrWhiteSpace(ExpiryText.Text)
            ? "Copied to clipboard."
            : $"{ExpiryText.Text}  |  Copied";
    }

    private void OnClose(object sender, RoutedEventArgs e)
    {
        Close();
    }
}
