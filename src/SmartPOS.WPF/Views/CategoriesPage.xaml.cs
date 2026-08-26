using SmartPOS.Application.ViewModels;
using System.Windows.Controls;
using Microsoft.Extensions.DependencyInjection;

namespace SmartPOS.WPF.Views
{
    public partial class CategoriesPage : Page
    {
        public CategoriesPage()
        {
            InitializeComponent();
            var host = ((App)System.Windows.Application.Current).Host;
            DataContext = host.Services.GetRequiredService<CategoriesViewModel>();
        }
    }
}
