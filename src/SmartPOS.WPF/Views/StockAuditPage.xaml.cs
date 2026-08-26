using System.Windows.Controls;
using SmartPOS.Application.ViewModels;

namespace SmartPOS.WPF.Views;

public partial class StockAuditPage : Page
{
    public StockAuditPage(StockAuditViewModel viewModel)
    {
        InitializeComponent();
        DataContext = viewModel;
    }
}
