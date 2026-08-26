using Microsoft.Extensions.DependencyInjection;
using SmartPOS.Application.ViewModels;
using System.Windows.Controls;

namespace SmartPOS.WPF.Views
{
    public partial class PurchaseOrdersPage : Page
    {
        private readonly PurchaseOrdersViewModel? _viewModel;

        public PurchaseOrdersPage()
        {
            InitializeComponent();

            // Resolve ViewModel
            if (System.Windows.Application.Current is App app)
            {
                _viewModel = app.Host.Services.GetRequiredService<PurchaseOrdersViewModel>();
                DataContext = _viewModel;
            }
        }
    }
}
