using System.Windows;
using System.Windows.Input;

namespace SmartPOS.WPF.Views.Dialogs;

public partial class AdminPinDialog : Window
{
    public string Pin { get; private set; } = string.Empty;

    public AdminPinDialog(string description)
    {
        InitializeComponent();
        DescriptionText.Text = description;
        Loaded += (s, e) => PinPasswordBox.Focus();
    }

    private void ConfirmButton_Click(object sender, RoutedEventArgs e)
    {
        Submit();
    }

    private void CancelButton_Click(object sender, RoutedEventArgs e)
    {
        DialogResult = false;
        Close();
    }

    private void PinPasswordBox_KeyDown(object sender, KeyEventArgs e)
    {
        if (e.Key == Key.Enter)
        {
            Submit();
        }
        else if (e.Key == Key.Escape)
        {
            DialogResult = false;
            Close();
        }
    }

    private void Submit()
    {
        Pin = PinPasswordBox.Password;
        if (string.IsNullOrWhiteSpace(Pin))
        {
            ErrorText.Visibility = Visibility.Visible;
            ErrorText.Text = "يرجى إدخال الرقم السري";
            return;
        }

        DialogResult = true;
        Close();
    }

    public void ShowError()
    {
        ErrorText.Visibility = Visibility.Visible;
        ErrorText.Text = "الرمز السري غير صحيح أو لا تملك صلاحية";
        PinPasswordBox.Clear();
        PinPasswordBox.Focus();
    }
}
