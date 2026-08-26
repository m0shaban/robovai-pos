using SmartPOS.LicenseTool.Gui.Services;
using System.Windows;

namespace SmartPOS.LicenseTool.Gui.Views;

public partial class LoginWindow : Window
{
    public LoginWindow()
    {
        InitializeComponent();
        EmailBox.Focus();
    }

    private void OnLogin(object sender, RoutedEventArgs e)
    {
        ErrorText.Visibility = Visibility.Collapsed;

        var email = EmailBox.Text;
        var password = PasswordBox.Password;

        if (!AuthService.Validate(email, password))
        {
            ErrorText.Text = "Invalid credentials.";
            ErrorText.Visibility = Visibility.Visible;
            return;
        }

        var main = new MainWindow();
        main.Show();
        Close();
    }
}
