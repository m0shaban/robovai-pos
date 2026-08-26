using System.Windows;
using System.Windows.Controls;
using Microsoft.Extensions.DependencyInjection;
using SmartPOS.Application.ViewModels;
using App = SmartPOS.WPF.App;

namespace SmartPOS.WPF.Views;

public partial class ReportsPage : Page
{
    private ReportsViewModel? _viewModel;

    public ReportsPage()
    {
        InitializeComponent();
    }

    private async void Page_Loaded(object sender, RoutedEventArgs e)
    {
        try
        {
            var host = ((App)System.Windows.Application.Current).Host;
            _viewModel = host.Services.GetRequiredService<ReportsViewModel>();
            DataContext = _viewModel;

            await LoadData();
        }
        catch (Exception ex)
        {
            MessageBox.Show($"خطأ في تحميل الصفحة: {ex.Message}", "خطأ", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private async Task LoadData()
    {
        if (_viewModel != null)
        {
            await _viewModel.LoadReportsCommand.ExecuteAsync(null);

            // Update UI
            TodaySalesText.Text = $"{_viewModel.TodaySales:F2} ج.م";
            TodayTransactionsText.Text = $"{_viewModel.TodayTransactions} عملية";
            TodayProfitText.Text = $"{_viewModel.TodayProfit:F2} ج.م";
            MonthSalesText.Text = $"{_viewModel.MonthSales:F2} ج.م";
            TotalExpensesText.Text = $"{_viewModel.TotalExpenses:F2} ج.م";

            RecentSalesGrid.ItemsSource = _viewModel.RecentSales;
            TopProductsGrid.ItemsSource = _viewModel.TopProducts;

            // Set default dates
            StartDatePicker.SelectedDate = DateTime.Now.Date;
            EndDatePicker.SelectedDate = DateTime.Now.Date;
        }
    }

    private async void FilterButton_Click(object sender, RoutedEventArgs e)
    {
        if (_viewModel != null && StartDatePicker.SelectedDate.HasValue && EndDatePicker.SelectedDate.HasValue)
        {
            _viewModel.SelectedStartDate = StartDatePicker.SelectedDate.Value;
            _viewModel.SelectedEndDate = EndDatePicker.SelectedDate.Value.AddDays(1).AddSeconds(-1);

            await _viewModel.FilterByDateCommand.ExecuteAsync(null);

            RecentSalesGrid.ItemsSource = _viewModel.RecentSales;
        }
    }
}
