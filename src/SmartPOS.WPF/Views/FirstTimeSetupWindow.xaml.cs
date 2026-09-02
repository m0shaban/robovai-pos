using System;
using System.Collections.Generic;
using System.Drawing.Printing;
using System.Linq;
using System.Text.Json;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using Microsoft.Win32;
using SmartPOS.Core.Entities;
using SmartPOS.Core.Interfaces;

namespace SmartPOS.WPF.Views;

public partial class FirstTimeSetupWindow : Window
{
    private readonly ISettingsService _settingsService;
    private int _currentStep = 1;
    private string _selectedCountry = "SA";

    public FirstTimeSetupWindow(ISettingsService settingsService)
    {
        InitializeComponent();
        _settingsService = settingsService;

        _ = LoadPrintersAsync();
        ApplyCountryDefaults("SA");
        UpdateStepVisibility();
    }

    private async System.Threading.Tasks.Task LoadPrintersAsync()
    {
        try
        {
            var printers = await System.Threading.Tasks.Task.Run(() =>
            {
                var list = new List<string>();
                foreach (string printer in PrinterSettings.InstalledPrinters)
                {
                    list.Add(printer);
                }
                return list;
            });

            CmbPrinters.ItemsSource = printers;
            if (printers.Count > 0)
            {
                CmbPrinters.SelectedIndex = 0;
            }
        }
        catch { /* ignore */ }
    }

    private void Country_Checked(object sender, RoutedEventArgs e)
    {
        if (sender is RadioButton rb && rb.Tag is string tag)
        {
            _selectedCountry = tag;
            ApplyCountryDefaults(tag);
        }
    }

    private void ApplyCountryDefaults(string country)
    {
        if (TxtCurrencySymbol == null || TxtCurrencyName == null) return;

        // Reset all specific checkboxes first
        if (ChkMada != null) ChkMada.IsChecked = false;
        if (ChkStcPay != null) ChkStcPay.IsChecked = false;
        if (ChkUrpay != null) ChkUrpay.IsChecked = false;
        if (ChkAlRajhi != null) ChkAlRajhi.IsChecked = false;
        if (ChkTamara != null) ChkTamara.IsChecked = false;
        if (ChkTabby != null) ChkTabby.IsChecked = false;
        if (ChkSamsungPay != null) ChkSamsungPay.IsChecked = false;
        if (ChkPayBy != null) ChkPayBy.IsChecked = false;
        if (ChkKnet != null) ChkKnet.IsChecked = false;
        if (ChkBoubyan != null) ChkBoubyan.IsChecked = false;
        if (ChkNaps != null) ChkNaps.IsChecked = false;
        if (ChkBenefitPay != null) ChkBenefitPay.IsChecked = false;
        if (ChkOmanNet != null) ChkOmanNet.IsChecked = false;
        if (ChkThawani != null) ChkThawani.IsChecked = false;
        if (ChkInstaPay != null) ChkInstaPay.IsChecked = false;
        if (ChkVodafone != null) ChkVodafone.IsChecked = false;
        if (ChkMeeza != null) ChkMeeza.IsChecked = false;

        switch (country)
        {
            case "SA":
                TxtCurrencySymbol.Text = "ر.س";
                TxtCurrencyName.Text = "ريال سعودي";
                TxtTaxPercentage.Text = "15";
                if (ChkMada != null) ChkMada.IsChecked = true;
                if (ChkStcPay != null) ChkStcPay.IsChecked = true;
                if (ChkApplePay != null) ChkApplePay.IsChecked = true;
                break;

            case "EG":
                TxtCurrencySymbol.Text = "ج.م";
                TxtCurrencyName.Text = "جنيه مصري";
                TxtTaxPercentage.Text = "14";
                if (ChkInstaPay != null) ChkInstaPay.IsChecked = true;
                if (ChkVodafone != null) ChkVodafone.IsChecked = true;
                if (ChkMeeza != null) ChkMeeza.IsChecked = true;
                if (ChkApplePay != null) ChkApplePay.IsChecked = false;
                break;

            case "AE":
                TxtCurrencySymbol.Text = "د.إ";
                TxtCurrencyName.Text = "درهم إماراتي";
                TxtTaxPercentage.Text = "5";
                if (ChkApplePay != null) ChkApplePay.IsChecked = true;
                if (ChkSamsungPay != null) ChkSamsungPay.IsChecked = true;
                if (ChkPayBy != null) ChkPayBy.IsChecked = true;
                break;

            case "KW":
                TxtCurrencySymbol.Text = "د.ك";
                TxtCurrencyName.Text = "دينار كويتي";
                TxtTaxPercentage.Text = "0";
                if (ChkKnet != null) ChkKnet.IsChecked = true;
                if (ChkBoubyan != null) ChkBoubyan.IsChecked = true;
                if (ChkApplePay != null) ChkApplePay.IsChecked = true;
                break;

            case "QA":
                TxtCurrencySymbol.Text = "ر.ق";
                TxtCurrencyName.Text = "ريال قطري";
                TxtTaxPercentage.Text = "0";
                if (ChkNaps != null) ChkNaps.IsChecked = true;
                if (ChkApplePay != null) ChkApplePay.IsChecked = true;
                break;

            case "BH":
                TxtCurrencySymbol.Text = "د.ب";
                TxtCurrencyName.Text = "دينار بحريني";
                TxtTaxPercentage.Text = "10";
                if (ChkBenefitPay != null) ChkBenefitPay.IsChecked = true;
                if (ChkApplePay != null) ChkApplePay.IsChecked = true;
                break;

            case "OM":
                TxtCurrencySymbol.Text = "ر.ع";
                TxtCurrencyName.Text = "ريال عماني";
                TxtTaxPercentage.Text = "5";
                if (ChkOmanNet != null) ChkOmanNet.IsChecked = true;
                if (ChkThawani != null) ChkThawani.IsChecked = true;
                if (ChkApplePay != null) ChkApplePay.IsChecked = true;
                break;

            default:
                TxtCurrencySymbol.Text = "$";
                TxtCurrencyName.Text = "دولار";
                TxtTaxPercentage.Text = "0";
                if (ChkApplePay != null) ChkApplePay.IsChecked = true;
                break;
        }
    }

    private void SelectLogo_Click(object sender, RoutedEventArgs e)
    {
        var dialog = new OpenFileDialog
        {
            Title = "اختر شعار المتجر",
            Filter = "ملفات الصور|*.png;*.jpg;*.jpeg;*.bmp|جميع الملفات|*.*"
        };
        if (dialog.ShowDialog() == true)
        {
            TxtStoreLogoPath.Text = dialog.FileName;
        }
    }

    private void Next_Click(object sender, RoutedEventArgs e)
    {
        if (_currentStep < 5)
        {
            _currentStep++;
            UpdateStepVisibility();
        }
    }

    private void Prev_Click(object sender, RoutedEventArgs e)
    {
        if (_currentStep > 1)
        {
            _currentStep--;
            UpdateStepVisibility();
        }
    }

    private void UpdateStepVisibility()
    {
        Step1Panel.Visibility = _currentStep == 1 ? Visibility.Visible : Visibility.Collapsed;
        Step2Panel.Visibility = _currentStep == 2 ? Visibility.Visible : Visibility.Collapsed;
        Step3Panel.Visibility = _currentStep == 3 ? Visibility.Visible : Visibility.Collapsed;
        Step4Panel.Visibility = _currentStep == 4 ? Visibility.Visible : Visibility.Collapsed;
        Step5Panel.Visibility = _currentStep == 5 ? Visibility.Visible : Visibility.Collapsed;

        BtnPrev.Visibility = _currentStep > 1 ? Visibility.Visible : Visibility.Collapsed;
        BtnNext.Visibility = _currentStep < 5 ? Visibility.Visible : Visibility.Collapsed;
        BtnFinish.Visibility = _currentStep == 5 ? Visibility.Visible : Visibility.Collapsed;

        var activeBg = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#0E2C44"));
        var inactiveBg = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#081422"));
        var activeBorder = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#00D4FF"));
        var inactiveBorder = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#1A2C40"));
        var activeText = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#FFFFFF"));
        var inactiveText = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#94A3B8"));

        Step1Badge.Background = _currentStep == 1 ? activeBg : inactiveBg;
        Step1Badge.BorderBrush = _currentStep == 1 ? activeBorder : inactiveBorder;

        Step2Badge.Background = _currentStep == 2 ? activeBg : inactiveBg;
        Step2Badge.BorderBrush = _currentStep == 2 ? activeBorder : inactiveBorder;

        Step3Badge.Background = _currentStep == 3 ? activeBg : inactiveBg;
        Step3Badge.BorderBrush = _currentStep == 3 ? activeBorder : inactiveBorder;

        Step4Badge.Background = _currentStep == 4 ? activeBg : inactiveBg;
        Step4Badge.BorderBrush = _currentStep == 4 ? activeBorder : inactiveBorder;

        Step5Badge.Background = _currentStep == 5 ? activeBg : inactiveBg;
        Step5Badge.BorderBrush = _currentStep == 5 ? activeBorder : inactiveBorder;

        if (Step1Text != null) Step1Text.Foreground = _currentStep == 1 ? activeText : inactiveText;
        if (Step2Text != null) Step2Text.Foreground = _currentStep == 2 ? activeText : inactiveText;
        if (Step3Text != null) Step3Text.Foreground = _currentStep == 3 ? activeText : inactiveText;
        if (Step4Text != null) Step4Text.Foreground = _currentStep == 4 ? activeText : inactiveText;
        if (Step5Text != null) Step5Text.Foreground = _currentStep == 5 ? activeText : inactiveText;
    }

    private void TestPrint_Click(object sender, RoutedEventArgs e)
    {
        if (CmbPrinters.SelectedItem is string pName && !string.IsNullOrWhiteSpace(pName))
        {
            try
            {
                var pd = new PrintDocument();
                pd.PrinterSettings.PrinterName = pName;
                pd.PrintPage += (s, ev) =>
                {
                    using var fontTitle = new System.Drawing.Font("Arial", 12, System.Drawing.FontStyle.Bold);
                    using var fontBody = new System.Drawing.Font("Arial", 9);
                    using var brush = new System.Drawing.SolidBrush(System.Drawing.Color.Black);

                    float y = 10;
                    ev.Graphics?.DrawString("=== RobovAI POS ===", fontTitle, brush, 20, y); y += 25;
                    ev.Graphics?.DrawString($"المتجر: {TxtStoreName.Text}", fontBody, brush, 10, y); y += 18;
                    ev.Graphics?.DrawString($"الفرع: {TxtBranchName.Text}", fontBody, brush, 10, y); y += 18;
                    ev.Graphics?.DrawString($"التاريخ: {DateTime.Now:yyyy-MM-dd HH:mm}", fontBody, brush, 10, y); y += 18;
                    ev.Graphics?.DrawString("تم اختبار الطابعة بنجاح ✅", fontBody, brush, 10, y);
                };
                pd.Print();
                MessageBox.Show("تم إرسال أمر الطباعة التجريبية إلى الطابعة بنجاح ✅", "نجاح الطباعة", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"تعذر اختبار الطباعة: {ex.Message}", "تنبيه", MessageBoxButton.OK, MessageBoxImage.Warning);
            }
        }
        else
        {
            MessageBox.Show("يرجى اختيار طابعة أولاً", "تنبيه", MessageBoxButton.OK, MessageBoxImage.Warning);
        }
    }

    private async void Finish_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            // 1. Save Country & Currency & Sector
            await _settingsService.SaveSettingAsync("CountryCode", _selectedCountry);
            await _settingsService.SaveSettingAsync("CurrencySymbol", TxtCurrencySymbol.Text.Trim());
            await _settingsService.SaveSettingAsync("CurrencyName", TxtCurrencyName.Text.Trim());
            var sector = (CmbBusinessSector.SelectedItem as ComboBoxItem)?.Content?.ToString() ?? "General";
            await _settingsService.SaveSettingAsync("BusinessSector", sector);

            // 2. Save Store Details & Logo
            await _settingsService.SaveSettingAsync("StoreName", TxtStoreName.Text.Trim());
            await _settingsService.SaveSettingAsync("StorePhone", TxtStorePhone.Text.Trim());
            await _settingsService.SaveSettingAsync("StoreAddress", TxtStoreAddress.Text.Trim());
            await _settingsService.SaveSettingAsync("StoreTaxNumber", TxtStoreTaxNumber.Text.Trim());
            await _settingsService.SaveSettingAsync("TaxPercentage", TxtTaxPercentage.Text.Trim());
            await _settingsService.SaveSettingAsync("FooterMessage", TxtFooterMessage.Text.Trim());
            if (!string.IsNullOrWhiteSpace(TxtStoreLogoPath.Text))
            {
                await _settingsService.SaveSettingAsync("AppLogoPath", TxtStoreLogoPath.Text.Trim());
            }

            // 3. Save Multi-Branch & Sync Architecture
            var role = RbRoleCentral.IsChecked == true ? "CentralServer" : (RbRoleSub.IsChecked == true ? "SubTerminal" : "Standalone");
            await _settingsService.SaveSettingAsync("BranchRole", role);
            await _settingsService.SaveSettingAsync("BranchName", TxtBranchName.Text.Trim());
            await _settingsService.SaveSettingAsync("BranchCode", TxtBranchCode.Text.Trim());
            var branchCountStr = (CmbBranchCount.SelectedIndex + 1).ToString();
            await _settingsService.SaveSettingAsync("BranchCount", branchCountStr);

            await _settingsService.SaveSettingAsync("EnableLanSync", ChkEnableLanSync.IsChecked == true ? "true" : "false");
            await _settingsService.SaveSettingAsync("EnableWmsBridge", ChkEnableWmsBridge.IsChecked == true ? "true" : "false");
            await _settingsService.SaveSettingAsync("AutoBackupEnabled", ChkEnableDailyBackup.IsChecked == true ? "true" : "false");
            await _settingsService.SaveSettingAsync("EnableSampleData", ChkEnableSampleData.IsChecked == true ? "true" : "false");

            // 4. Save Enabled Payment Methods
            var enabledMethods = new List<int>();
            if (ChkCash.IsChecked == true) enabledMethods.Add((int)PaymentMethod.Cash);
            if (ChkCard.IsChecked == true) enabledMethods.Add((int)PaymentMethod.Card);
            if (ChkDeferred.IsChecked == true) enabledMethods.Add((int)PaymentMethod.Deferred);
            if (ChkApplePay?.IsChecked == true) enabledMethods.Add((int)PaymentMethod.ApplePay);
            if (ChkMada?.IsChecked == true) enabledMethods.Add((int)PaymentMethod.Mada);
            if (ChkStcPay?.IsChecked == true) enabledMethods.Add((int)PaymentMethod.StcPay);
            if (ChkUrpay?.IsChecked == true) enabledMethods.Add((int)PaymentMethod.Urpay);
            if (ChkAlRajhi?.IsChecked == true) enabledMethods.Add((int)PaymentMethod.AlRajhiTransfer);
            if (ChkTamara?.IsChecked == true) enabledMethods.Add((int)PaymentMethod.Tamara);
            if (ChkTabby?.IsChecked == true) enabledMethods.Add((int)PaymentMethod.Tabby);
            if (ChkSamsungPay?.IsChecked == true) enabledMethods.Add((int)PaymentMethod.SamsungPay);
            if (ChkPayBy?.IsChecked == true) enabledMethods.Add((int)PaymentMethod.PayBy);
            if (ChkKnet?.IsChecked == true) enabledMethods.Add((int)PaymentMethod.Knet);
            if (ChkBoubyan?.IsChecked == true) enabledMethods.Add((int)PaymentMethod.BoubyanPay);
            if (ChkNaps?.IsChecked == true) enabledMethods.Add((int)PaymentMethod.Naps);
            if (ChkBenefitPay?.IsChecked == true) enabledMethods.Add((int)PaymentMethod.BenefitPay);
            if (ChkOmanNet?.IsChecked == true) enabledMethods.Add((int)PaymentMethod.OmanNet);
            if (ChkThawani?.IsChecked == true) enabledMethods.Add((int)PaymentMethod.Thawani);
            if (ChkInstaPay?.IsChecked == true) enabledMethods.Add((int)PaymentMethod.InstaPay);
            if (ChkVodafone?.IsChecked == true) enabledMethods.Add((int)PaymentMethod.VodafoneCash);
            if (ChkMeeza?.IsChecked == true) enabledMethods.Add((int)PaymentMethod.Meeza);
            if (ChkBankTransfer?.IsChecked == true) enabledMethods.Add((int)PaymentMethod.BankTransfer);

            await _settingsService.SaveSettingAsync("ActivePaymentMethodsJson", JsonSerializer.Serialize(enabledMethods));

            // 5. Save Printer
            if (CmbPrinters.SelectedItem is string pName)
            {
                await _settingsService.SaveSettingAsync("PrinterName", pName);
            }
            var width = CmbPaperWidth.SelectedIndex == 1 ? "58" : "80";
            await _settingsService.SaveSettingAsync("ReceiptWidth", width);

            // 6. Mark First-Run as Completed
            await _settingsService.SaveSettingAsync("IsFirstRunCompleted", "true");

            DialogResult = true;
        }
        catch (Exception ex)
        {
            MessageBox.Show($"خطأ أثناء حفظ التهيئات: {ex.Message}", "خطأ", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private void Close_Click(object sender, RoutedEventArgs e)
    {
        DialogResult = false;
    }

    private void StepBadge_Click(object sender, System.Windows.Input.MouseButtonEventArgs e)
    {
        if (sender is FrameworkElement fe && int.TryParse(fe.Tag?.ToString(), out int targetStep))
        {
            if (targetStep >= 1 && targetStep <= 5)
            {
                _currentStep = targetStep;
                UpdateStepVisibility();
            }
        }
    }

    private void TitleBar_MouseLeftButtonDown(object sender, System.Windows.Input.MouseButtonEventArgs e)
    {
        if (e.LeftButton == System.Windows.Input.MouseButtonState.Pressed)
        {
            DragMove();
        }
    }
}
