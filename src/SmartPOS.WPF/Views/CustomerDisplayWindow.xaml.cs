using SmartPOS.Application.DTOs;
using System.Collections.Generic;
using System.Windows;

namespace SmartPOS.WPF.Views
{
    public partial class CustomerDisplayWindow : Window
    {
        private static CustomerDisplayWindow? _instance;

        public static CustomerDisplayWindow GetOrCreate()
        {
            if (_instance == null || !_instance.IsLoaded)
                _instance = new CustomerDisplayWindow();
            return _instance;
        }

        public CustomerDisplayWindow()
        {
            InitializeComponent();
        }

        public void UpdateDisplay(string storeName, string customerName, IEnumerable<CartItem> items, decimal total, string welcomeMessage = "شكراً لتسوقكم معنا 😊")
        {
            Dispatcher.Invoke(() =>
            {
                StoreNameText.Text    = storeName;
                CustomerNameText.Text = string.IsNullOrWhiteSpace(customerName) ? "زبون كريم" : customerName;
                TotalText.Text        = $"{total:N2} ج.م";
                WelcomeText.Text      = welcomeMessage;
                ItemsList.ItemsSource = items;
            });
        }

        public void ClearDisplay()
        {
            Dispatcher.Invoke(() =>
            {
                CustomerNameText.Text = "زبون كريم";
                TotalText.Text        = "0.00 ج.م";
                ItemsList.ItemsSource = null;
            });
        }

        protected override void OnClosing(System.ComponentModel.CancelEventArgs e)
        {
            e.Cancel = true; // Prevent close — only hide
            Hide();
        }
    }
}
