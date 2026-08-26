﻿using System.Linq;
using System.Windows;
using SmartPOS.Application.Utilities;
using SmartPOS.Core.Entities;
using SmartPOS.Infrastructure.Data;

namespace SmartPOS.WPF.Views
{
    public partial class CustomerInvoicesWindow : Window
    {
        private readonly AppDbContext _context;
        private readonly Customer _customer;

        private sealed class CustomerInvoiceRow
        {
            public string InvoiceNumber { get; init; } = string.Empty;
            public DateTime SaleDate { get; init; }
            public decimal TotalAmount { get; init; }
            public decimal AmountPaid { get; init; }
            public decimal RemainingAmount { get; init; }
            public string PaymentMethodText { get; init; } = string.Empty;
        }

        public CustomerInvoicesWindow(AppDbContext context, Customer customer)
        {
            InitializeComponent();
            _context = context;
            _customer = customer;
            LoadInvoices();
        }

        private void LoadInvoices()
        {
            txtCustomerName.Text = _customer.Name;
            txtCreditLimit.Text = "الحد الائتماني: " + _customer.CreditLimit.ToString("N2") + " ج.م";

            var invoices = _context.Sales
                .Where(s => s.CustomerId == _customer.Id
                         && !s.IsDeleted
                         && !s.InvoiceNumber.StartsWith(SaleRecordKinds.DebtPaymentPrefix))
                .OrderByDescending(s => s.SaleDate)
                .Select(s => new CustomerInvoiceRow
                {
                    InvoiceNumber = s.InvoiceNumber,
                    SaleDate = s.SaleDate,
                    TotalAmount = s.TotalAmount,
                    AmountPaid = s.AmountPaid,
                    RemainingAmount = s.TotalAmount - s.AmountPaid,
                    PaymentMethodText = s.PaymentMethod == PaymentMethod.Deferred ? "آجل" : s.PaymentMethod.ToString()
                })
                .ToList();

            dgInvoices.ItemsSource = invoices;

            var totalDebt = invoices.Sum(i => i.RemainingAmount);
            txtTotalDebt.Text = "إجمالي الرصيد المستحق: " + totalDebt.ToString("N2") + " ج.م";
        }

        private void Close_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }
    }
}
