using System.Windows;
using System.Windows.Controls;
using Microsoft.Extensions.DependencyInjection;
using SmartPOS.Application.ViewModels;
using App = SmartPOS.WPF.App;

namespace SmartPOS.WPF.Views;

public partial class ShiftManagementPage : Page
{
    public ShiftManagementPage()
    {
        InitializeComponent();
    }

    private async void Page_Loaded(object sender, RoutedEventArgs e)
    {
        try
        {
            var host = ((App)System.Windows.Application.Current).Host;
            var viewModel = host.Services.GetRequiredService<ShiftManagementViewModel>();
            DataContext = viewModel;

            await viewModel.LoadShiftsCommand.ExecuteAsync(null);
        }
        catch (Exception ex)
        {
            MessageBox.Show($"خطأ في تحميل صفحة الورديات: {ex.Message}", "خطأ", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }
}
