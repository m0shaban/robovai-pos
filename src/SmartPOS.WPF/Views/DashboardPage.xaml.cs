using Microsoft.Extensions.DependencyInjection;
using SmartPOS.Application.ViewModels;
using System.Windows.Controls;

namespace SmartPOS.WPF.Views;

public partial class DashboardPage : Page
{
    private readonly DashboardViewModel _viewModel;

    public DashboardPage()
    {
        InitializeComponent();

        // Get ViewModel from DI
        _viewModel = ((App)System.Windows.Application.Current)
            .Host.Services.GetRequiredService<DashboardViewModel>();

        DataContext = _viewModel;
    }

    private async void Page_Loaded(object sender, System.Windows.RoutedEventArgs e)
    {
        // Load data every time page is loaded
        await _viewModel.LoadDashboardDataCommand.ExecuteAsync(null);
    }
}

// Extension to access IHost from App
public static class AppExtensions
{
    public static Microsoft.Extensions.Hosting.IHost Host => ((App)System.Windows.Application.Current).GetHost();

    public static Microsoft.Extensions.Hosting.IHost GetHost(this App app)
    {
        return (Microsoft.Extensions.Hosting.IHost)app.GetType()
            .GetField("_host", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)!
            .GetValue(app)!;
    }
}
