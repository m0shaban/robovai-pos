using System.Windows.Controls;
using Microsoft.Extensions.DependencyInjection;
using SmartPOS.Application.ViewModels;

namespace SmartPOS.WPF.Views;

public partial class RentalsPage : Page
{
    public RentalsPage()
    {
        InitializeComponent();
        var host = ((App)System.Windows.Application.Current).Host;
        DataContext = host.Services.GetRequiredService<RentalsViewModel>();
    }
}
