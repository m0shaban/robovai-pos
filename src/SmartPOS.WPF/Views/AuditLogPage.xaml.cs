using Microsoft.Extensions.DependencyInjection;
using SmartPOS.Application.ViewModels;
using System.Windows.Controls;

namespace SmartPOS.WPF.Views
{
    public partial class AuditLogPage : Page
    {
        public AuditLogPage()
        {
            InitializeComponent();
            if (System.Windows.Application.Current is App app)
                DataContext = app.Host.Services.GetRequiredService<AuditLogViewModel>();
        }
    }
}
