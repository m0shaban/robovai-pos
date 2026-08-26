using System.Windows;
using System.Windows.Controls;
using Microsoft.Extensions.DependencyInjection;
using SmartPOS.Application.ViewModels;
using SmartPOS.Core.Entities;
using App = SmartPOS.WPF.App;

namespace SmartPOS.WPF.Views;

public partial class ExpensesPage : Page
{
    private ExpensesViewModel? _viewModel;

    public ExpensesPage()
    {
        InitializeComponent();
    }

    private async void Page_Loaded(object sender, RoutedEventArgs e)
    {
        try
        {
            var host = ((App)System.Windows.Application.Current).Host;
            _viewModel = host.Services.GetRequiredService<ExpensesViewModel>();
            DataContext = _viewModel;

            if (_viewModel != null)
            {
                await _viewModel.LoadExpensesCommand.ExecuteAsync(null);
            }
        }
        catch (Exception ex)
        {
            MessageBox.Show($"خطأ في تحميل الصفحة: {ex.Message}", "خطأ", MessageBoxButton.OK, MessageBoxImage.Error);
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
