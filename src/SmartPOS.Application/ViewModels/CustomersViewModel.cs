using System.Collections.ObjectModel;
using System.Windows;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.EntityFrameworkCore;
using SmartPOS.Application.Extensions;
using SmartPOS.Core.Entities;
using SmartPOS.Core.Interfaces;
using SmartPOS.Infrastructure.Data;

namespace SmartPOS.Application.ViewModels;

/// <summary>
/// CustomersViewModel handles customer operations.
/// Updated to use IDbContextFactory pattern (v5.1).
/// </summary>
public partial class CustomersViewModel : BaseViewModel
{
    private readonly IRepository<Customer> _customerRepository;
    private readonly IDbContextFactory<AppDbContext> _contextFactory;
    private readonly User _currentUser;
    private readonly IAuthorizationService _authService;

    private List<Customer> _allCustomers = new();

    [ObservableProperty]
    private ObservableCollection<Customer> _customers = new();

    [ObservableProperty]
    private string _searchText = string.Empty;

    [ObservableProperty]
    private Customer? _selectedCustomer;

    public bool IsAdmin =>
        _currentUser.Role == UserRole.SuperAdmin ||
        _currentUser.Role == UserRole.Admin ||
        _currentUser.Role == UserRole.Manager;

    [ObservableProperty] private string _name = string.Empty;
    [ObservableProperty] private string? _phone;
    [ObservableProperty] private string? _email;
    [ObservableProperty] private string? _address;
    [ObservableProperty] private decimal _creditLimit;
    [ObservableProperty] private string? _notes;
    [ObservableProperty] private bool _formIsActive = true;

    [ObservableProperty] private decimal _debtPaymentAmount;

    public CustomersViewModel(IRepository<Customer> customerRepository, IDbContextFactory<AppDbContext> contextFactory, User currentUser, IAuthorizationService authService)
    {
        _customerRepository = customerRepository;
        _contextFactory = contextFactory;
        _currentUser = currentUser;
        _authService = authService;

        _ = InitializeAsync();
    }

    private async Task InitializeAsync()
    {
        await ExecuteBusyAsync(LoadCustomersCoreAsync, "جاري تحميل العملاء...", $"تم تحميل {Customers.Count} عميل");
    }

    private async Task LoadCustomersCoreAsync()
    {
        var customers = await _customerRepository.GetAllAsync();
        _allCustomers = customers.OrderBy(x => x.Name).ToList();
        FilterCustomers();
    }

    [RelayCommand]
    private async Task LoadCustomersAsync() => await ExecuteBusyAsync(LoadCustomersCoreAsync, "جاري تحديث قائمة العملاء...");

    [RelayCommand]
    private void EditCustomer(Customer customer)
    {
        SelectedCustomer = customer;
    }

    [RelayCommand]
    private void ClearForm()
    {
        SelectedCustomer = null;
        Name = string.Empty;
        Phone = null;
        Email = null;
        Address = null;
        CreditLimit = 0;
        Notes = null;
        FormIsActive = true;
    }

    [RelayCommand]
    private async Task SaveCustomerAsync()
    {
        if (string.IsNullOrWhiteSpace(Name))
        {
            var reqMsg = SmartPOS.Core.Localization.Loc.Tr("Loc_Cust_NameRequired", "أدخل اسم العميل");
            var alertTitle = SmartPOS.Core.Localization.Loc.Tr("Loc_Alert", "تنبيه");
            MessageBox.Show(reqMsg, alertTitle, MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        await ExecuteBusyAsync(async () =>
        {
            if (SelectedCustomer == null)
            {
                var customer = new Customer
                {
                    Name = Name.Trim(),
                    Phone = Phone,
                    Email = Email,
                    Address = Address,
                    CreditLimit = CreditLimit,
                    Notes = Notes,
                    IsActive = FormIsActive,
                    CreatedAt = DateTime.Now
                };
                await _customerRepository.AddAsync(customer);
            }
            else
            {
                SelectedCustomer.Name = Name.Trim();
                SelectedCustomer.Phone = Phone;
                SelectedCustomer.Email = Email;
                SelectedCustomer.Address = Address;
                SelectedCustomer.CreditLimit = CreditLimit;
                SelectedCustomer.Notes = Notes;
                SelectedCustomer.IsActive = FormIsActive;
                await _customerRepository.UpdateAsync(SelectedCustomer);
            }

            await LoadCustomersCoreAsync();
            ClearForm();
        }, SmartPOS.Core.Localization.Loc.Tr("Loc_Cust_Saving", "جاري حفظ بيانات العميل..."), SmartPOS.Core.Localization.Loc.Tr("Loc_Cust_SavedSuccess", "تم حفظ بيانات العميل بنجاح"));
    }

    [RelayCommand]
    private async Task DeleteCustomerAsync(Customer? customer)
    {
        if (customer == null) return;

        bool authorized = await _authService.RequestAdminOverrideAsync("حذف عميل من النظام");
        if (!authorized) return;

        var confirmMsg = string.Format(SmartPOS.Core.Localization.Loc.Tr("Loc_Cust_DeleteConfirm", "هل أنت متأكد من حذف العميل \"{0}\"؟"), customer.Name);
        var confirmTitle = SmartPOS.Core.Localization.Loc.Tr("Loc_Confirm", "تأكيد");
        if (MessageBox.Show(confirmMsg, confirmTitle, MessageBoxButton.YesNo, MessageBoxImage.Warning) == MessageBoxResult.Yes)
        {
            await ExecuteBusyAsync(async () =>
            {
                await _customerRepository.DeleteAsync(customer.Id);
                await LoadCustomersCoreAsync();
            }, SmartPOS.Core.Localization.Loc.Tr("Loc_Cust_Deleting", "جاري حذف العميل..."), SmartPOS.Core.Localization.Loc.Tr("Loc_Cust_DeletedSuccess", "تم حذف العميل بنجاح"));
        }
    }

    partial void OnSelectedCustomerChanged(Customer? value)
    {
        if (value == null) return;

        Name = value.Name;
        Phone = value.Phone;
        Email = value.Email;
        Address = value.Address;
        CreditLimit = value.CreditLimit;
        Notes = value.Notes;
        FormIsActive = value.IsActive;
    }

    partial void OnSearchTextChanged(string value) => FilterCustomers();

    private void FilterCustomers()
    {
        var filtered = string.IsNullOrWhiteSpace(SearchText)
            ? _allCustomers
            : _allCustomers.Where(c => c.Name.Contains(SearchText, StringComparison.OrdinalIgnoreCase) ||
                                       (c.Phone != null && c.Phone.Contains(SearchText)) ||
                                       (c.Email != null && c.Email.Contains(SearchText, StringComparison.OrdinalIgnoreCase)));

        Customers.SyncWith(filtered);
    }

    [RelayCommand]
    private async Task CollectDebtAsync(Customer? customer)
    {
        if (customer == null) return;
        if (customer.CurrentDebt <= 0)
        {
            MessageBox.Show("لا يوجد دين مستحق على هذا العميل.", "تنبيه", MessageBoxButton.OK, MessageBoxImage.Information);
            return;
        }

        if (DebtPaymentAmount <= 0)
            DebtPaymentAmount = customer.CurrentDebt;

        if (DebtPaymentAmount > customer.CurrentDebt)
        {
            MessageBox.Show(
                $"المبلغ المدخل ({DebtPaymentAmount:N2}) أكبر من الدين المستحق ({customer.CurrentDebt:N2}).",
                "تنبيه",
                MessageBoxButton.OK,
                MessageBoxImage.Warning);
            return;
        }

        var confirm = MessageBox.Show(
            $"تأكيد تحصيل دين العميل\n\nالعميل: {customer.Name}\nالمبلغ المحصل: {DebtPaymentAmount:N2} ج.م\nالدين المتبقي: {customer.CurrentDebt - DebtPaymentAmount:N2} ج.م",
            "تأكيد التحصيل",
            MessageBoxButton.YesNo,
            MessageBoxImage.Question);
        if (confirm != MessageBoxResult.Yes) return;

        await ExecuteBusyAsync(async () =>
        {
            await using var ctx = await _contextFactory.CreateDbContextAsync();
            var cust = await ctx.Customers.FindAsync(customer.Id);
            if (cust == null) return;

            cust.CurrentDebt -= DebtPaymentAmount;

            var activeShift = await ctx.Shifts
                .FirstOrDefaultAsync(s => s.UserId == _currentUser.Id && s.Status == ShiftStatus.Open && !s.IsDeleted);

            var paymentSale = new Sale
            {
                InvoiceNumber = $"DEF-PAY-{DateTime.Now:yyyyMMddHHmmss}",
                SaleDate = DateTime.Now,
                Subtotal = DebtPaymentAmount,
                TotalAmount = DebtPaymentAmount,
                AmountPaid = DebtPaymentAmount,
                ChangeAmount = 0,
                PaymentMethod = PaymentMethod.Cash,
                Status = SaleStatus.Completed,
                UserId = _currentUser.Id,
                CustomerId = cust.Id,
                ShiftId = activeShift?.Id,
                Notes = $"تحصيل دين عميل: {cust.Name}"
            };
            ctx.Sales.Add(paymentSale);

            await ctx.SaveChangesAsync();

            var paidAmt = DebtPaymentAmount;
            DebtPaymentAmount = 0;
            await LoadCustomersCoreAsync();
            MessageBox.Show(
                $"تم تحصيل {paidAmt:N2} ج.م من العميل {cust.Name}.\nالدين المتبقي: {cust.CurrentDebt:N2} ج.م",
                "تم التحصيل",
                MessageBoxButton.OK,
                MessageBoxImage.Information);
        }, "جاري تسجيل التحصيل...");
    }
}
