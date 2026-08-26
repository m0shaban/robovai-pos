using System.Windows;
using System.Windows.Controls;
using Microsoft.Extensions.DependencyInjection;
using SmartPOS.Application.ViewModels;
using SmartPOS.Core.Entities;
using SmartPOS.Infrastructure.Data;
using App = SmartPOS.WPF.App;

namespace SmartPOS.WPF.Views;

public partial class CustomersPage : Page
{
    public CustomersPage()
    {
        InitializeComponent();
    }

    private async void Page_Loaded(object sender, RoutedEventArgs e)
    {
        try
        {
            var host = ((App)System.Windows.Application.Current).Host;
            var viewModel = host.Services.GetRequiredService<CustomersViewModel>();
            DataContext = viewModel;

            await viewModel.LoadCustomersCommand.ExecuteAsync(null);
        }
        catch (Exception ex)
        {
            MessageBox.Show($"خطأ في تحميل العملاء: {ex.Message}", "خطأ", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private void BtnViewInvoices_Click(object sender, RoutedEventArgs e)
    {
        if (sender is not Button { Tag: Customer customer })
        {
            return;
        }

        var host = ((App)System.Windows.Application.Current).Host;
        var context = host.Services.GetRequiredService<AppDbContext>();
        var entityCustomer = context.Customers.Find(customer.Id);

        if (entityCustomer == null)
        {
            MessageBox.Show("تعذر العثور على بيانات العميل المطلوبة.", "تنبيه", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        var window = new CustomerInvoicesWindow(context, entityCustomer);
        window.ShowDialog();
    }
}
