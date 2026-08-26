using Microsoft.Extensions.DependencyInjection;
using SmartPOS.Application.ViewModels;
using System.Windows.Controls;

namespace SmartPOS.WPF.Views
{
    public partial class SuppliersPage : Page
    {
        private readonly SuppliersViewModel? _viewModel;

        public SuppliersPage()
        {
            InitializeComponent();

            // Resolve ViewModel
            if (System.Windows.Application.Current is App app)
            {
                _viewModel = app.Host.Services.GetRequiredService<SuppliersViewModel>();
                DataContext = _viewModel;
            }
        }
    }
}
