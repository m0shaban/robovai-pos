using System.Windows;
using System.Windows.Controls;
using Microsoft.Extensions.DependencyInjection;
using SmartPOS.Application.ViewModels;
using App = SmartPOS.WPF.App;

namespace SmartPOS.WPF.Views;

public partial class InvoicesPage : Page
{
    private InvoicesViewModel? _viewModel;

    public InvoicesPage()
    {
        InitializeComponent();
    }

    private async void Page_Loaded(object sender, RoutedEventArgs e)
    {
        try
        {
            var host = ((App)System.Windows.Application.Current).Host;
            _viewModel = host.Services.GetRequiredService<InvoicesViewModel>();
            DataContext = _viewModel;

            await _viewModel.LoadSalesCommand.ExecuteAsync(null);
        }
        catch (Exception ex)
        {
            MessageBox.Show($"خطأ في تحميل الفواتير: {ex.Message}", "خطأ", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private void Page_Unloaded(object sender, RoutedEventArgs e)
    {
        if (_viewModel is IDisposable disposable)
        {
            disposable.Dispose();
        }
    }
}