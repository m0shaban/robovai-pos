using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.Messaging;
using System.Windows;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.EntityFrameworkCore;
using SmartPOS.Application.Extensions;
using SmartPOS.Core.Entities;
using SmartPOS.Infrastructure.Data;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;
using System.IO;
using Microsoft.Win32;

namespace SmartPOS.Application.ViewModels;

/// <summary>
/// ViewModel for expenses.
/// Updated for Factory pattern fix (v5.1)
/// </summary>
public partial class ExpensesViewModel : BaseViewModel, IDisposable, CommunityToolkit.Mvvm.Messaging.IRecipient<SmartPOS.Application.Messages.BarcodeScannedMessage>
{
    private readonly IDbContextFactory<AppDbContext> _contextFactory;
    private readonly User _currentUser;
    private readonly SmartPOS.Core.Interfaces.IAuthorizationService _authService;

    // --- Collections ---
    private List<Expense> _allExpenses = new();

    [ObservableProperty]
    private ObservableCollection<Expense> _filteredExpenses = new();

    public ObservableCollection<ExpenseCategory> Categories { get; } = new(Enum.GetValues<ExpenseCategory>());

    // --- State & Filters ---
    [ObservableProperty]
    private DateTime _filterStartDate = DateTime.Today.AddDays(-30);

    [ObservableProperty]
    private DateTime _filterEndDate = DateTime.Today;

    [ObservableProperty]
    private string _searchText = string.Empty;

    [ObservableProperty]
    private ExpenseCategory? _selectedCategoryFilter;

    // --- Statistics ---
    [ObservableProperty]
    private decimal _todayExpenses;

    [ObservableProperty]
    private decimal _monthExpenses;

    [ObservableProperty]
    private decimal _totalExpenses;

    // --- Add Form Dialog ---
    [ObservableProperty]
    private bool _isAddExpenseDialogOpen;

    [ObservableProperty]
    private string _newExpenseDescription = string.Empty;

    [ObservableProperty]
    private decimal _newExpenseAmount;

    [ObservableProperty]
    private int _selectedCategoryIndex;

    public ExpensesViewModel(IDbContextFactory<AppDbContext> contextFactory, User currentUser, SmartPOS.Core.Interfaces.IAuthorizationService authService)
    {
        _contextFactory = contextFactory;
        _currentUser = currentUser;
        _authService = authService;

        CommunityToolkit.Mvvm.Messaging.WeakReferenceMessenger.Default.RegisterAll(this);

        _ = InitializeAsync();
    }

    public void Dispose()
    {
        CommunityToolkit.Mvvm.Messaging.WeakReferenceMessenger.Default.UnregisterAll(this);
    }

    public void Receive(SmartPOS.Application.Messages.BarcodeScannedMessage message)
    {
        System.Windows.Application.Current.Dispatcher.InvokeAsync(() =>
        {
            if (IsAddExpenseDialogOpen)
            {
                if (string.IsNullOrWhiteSpace(NewExpenseDescription))
                    NewExpenseDescription = message.Value;
                else
                    NewExpenseDescription += $" - {message.Value}";
            }
            else
            {
                SearchText = message.Value;
            }
        });
    }

    private async Task InitializeAsync()
    {
        await ExecuteBusyAsync(LoadExpensesCoreAsync, "⏳ جاري تحميل المصروفات...", $"✅ تم تحميل {FilteredExpenses.Count} مصروف");
    }

    private async Task LoadExpensesCoreAsync()
    {
        var start = FilterStartDate.Date;
        var end = FilterEndDate.Date.AddDays(1); // include the whole end day

        await using var ctx = await _contextFactory.CreateDbContextAsync();
        _allExpenses = await ctx.Expenses
            .AsNoTracking()
            .Include(e => e.User)
            .Where(e => e.ExpenseDate >= start && e.ExpenseDate < end && !e.IsDeleted)
            .OrderByDescending(e => e.ExpenseDate)
            .ToListAsync();

        FilterExpensesList();
    }

    // --- Commands ---
    [RelayCommand]
    private async Task LoadExpensesAsync() => await ExecuteBusyAsync(LoadExpensesCoreAsync, "جاري تحديث القائمة...");

    [RelayCommand]
    private void OpenAddDialog()
    {
        NewExpenseDescription = string.Empty;
        NewExpenseAmount = 0;
        SelectedCategoryIndex = 0;
        IsAddExpenseDialogOpen = true;
    }

    [RelayCommand]
    private void CloseAddDialog()
    {
        IsAddExpenseDialogOpen = false;
    }

    [RelayCommand]
    private void ClearFilter()
    {
        SearchText = string.Empty;
        SelectedCategoryFilter = null;
    }

    [RelayCommand]
    private async Task AddExpenseAsync()
    {
        if (NewExpenseAmount <= 0)
        {
            MessageBox.Show("أدخل المبلغ", "تنبيه", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        await ExecuteBusyAsync(async () =>
        {
            await using var ctx = await _contextFactory.CreateDbContextAsync();
            var expense = new Expense
            {
                Category = Categories[SelectedCategoryIndex],
                Amount = NewExpenseAmount,
                Description = NewExpenseDescription,
                ExpenseDate = DateTime.Now,
                UserId = _currentUser.Id,
                CreatedAt = DateTime.Now
            };
            ctx.Expenses.Add(expense);
            await ctx.SaveChangesAsync();
            await LoadExpensesCoreAsync();
            CloseAddDialog();

        }, "جاري الحفظ...", "✅ تم الحفظ بنجاح");
    }

    [RelayCommand]
    private async Task DeleteExpenseAsync(Expense? expense)
    {
        if (expense == null) return;

        bool authorized = await _authService.RequestAdminOverrideAsync("حذف مصروف من النظام");
        if (!authorized) return;

        var result = MessageBox.Show($"هل تريد حذف المصروف بقمية {expense.Amount} ج.م؟", "تأكيد الحذف", MessageBoxButton.YesNo, MessageBoxImage.Warning);
        if (result == MessageBoxResult.Yes)
        {
            await ExecuteBusyAsync(async () =>
            {
                await using var ctx = await _contextFactory.CreateDbContextAsync();
                var expenseToDelete = await ctx.Expenses.FindAsync(expense.Id);
                if (expenseToDelete != null)
                {
                    expenseToDelete.IsDeleted = true;
                    await ctx.SaveChangesAsync();
                    await LoadExpensesCoreAsync();
                }
            }, "جاري الحذف...", "✅ تم الحذف بنجاح");
        }
    }

    // --- Property Changed Handlers ---
    partial void OnFilterStartDateChanged(DateTime value) => _ = LoadExpensesAsync();
    partial void OnFilterEndDateChanged(DateTime value) => _ = LoadExpensesAsync();
    partial void OnSelectedCategoryFilterChanged(ExpenseCategory? value) => FilterExpensesList();
    partial void OnSearchTextChanged(string value) => FilterExpensesList();

    // --- Helpers ---
    private void FilterExpensesList()
    {
        var filtered = _allExpenses.AsEnumerable();

        if (!string.IsNullOrWhiteSpace(SearchText))
        {
            filtered = filtered.Where(e => e.Description != null && e.Description.Contains(SearchText, StringComparison.OrdinalIgnoreCase));
        }

        if (SelectedCategoryFilter.HasValue)
        {
            filtered = filtered.Where(e => e.Category == SelectedCategoryFilter.Value);
        }

        FilteredExpenses.SyncWith(filtered.OrderByDescending(e => e.ExpenseDate).ToList());

        UpdateStatistics();
    }

    [RelayCommand]
    private async Task ExportPdfAsync()
    {
        if (FilteredExpenses.Count == 0)
        {
            MessageBox.Show("لا يوجد بيانات لتصديرها", "تنبيه", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        try
        {
            QuestPDF.Settings.License = LicenseType.Community;

            var pdfBytes = Document.Create(container =>
            {
                container.Page(page =>
                {
                    page.Size(PageSizes.A4);
                    page.Margin(1.5f, Unit.Centimetre);
                    page.PageColor(Colors.White);
                    page.DefaultTextStyle(x => x.FontFamily("Segoe UI").FontSize(10).DirectionFromRightToLeft());

                    page.Header().Column(col =>
                    {
                        col.Item().Background("#1E3A5F").Padding(14).Row(row =>
                        {
                            row.RelativeItem().Column(c =>
                            {
                                c.Item().Text("تقرير المصروفات").Bold().FontSize(22).FontColor(Colors.White);
                                c.Item().Text($"من {FilterStartDate:dd/MM/yyyy} إلى {FilterEndDate:dd/MM/yyyy}").FontSize(10).FontColor("#94A3B8");
                            });
                            row.AutoItem().AlignRight().AlignMiddle().Column(c =>
                            {
                                c.Item().Text("SmartPOS").Bold().FontSize(14).FontColor("#94A3B8");
                                c.Item().Text($"تاريخ الإصدار: {DateTime.Now:dd/MM/yyyy HH:mm}").FontSize(10).FontColor("#94A3B8");
                            });
                        });
                        col.Item().LineHorizontal(2).LineColor("#1E3A5F");
                    });

                    page.Content().PaddingVertical(12).Column(col =>
                    {
                        col.Spacing(10);
                        
                        // Summary Stats
                        col.Item().Row(r =>
                        {
                            r.RelativeItem().Text($"إجمالي المصروفات المعروضة: {FilteredExpenses.Sum(e => e.Amount):N2} ج.م").Bold().FontSize(12);
                            r.RelativeItem().AlignRight().Text($"عدد الحركات: {FilteredExpenses.Count}").FontSize(11);
                        });

                        col.Item().Table(table =>
                        {
                            table.ColumnsDefinition(c =>
                            {
                                c.ConstantColumn(40);
                                c.RelativeColumn(1.5f);
                                c.RelativeColumn(3);
                                c.RelativeColumn(1.5f);
                                c.RelativeColumn(2);
                            });
                            table.Header(h =>
                            {
                                foreach (var t in new[] { "#", "التاريخ", "الوصف", "المبلغ", "المستخدم" })
                                    h.Cell().Background("#1E3A5F").Padding(6).Text(t).Bold().FontSize(10).FontColor(Colors.White).AlignCenter();
                            });

                            int idx = 1;
                            foreach (var d in FilteredExpenses)
                            {
                                var bg = idx % 2 == 0 ? "#F8FAFC" : "#FFFFFF";
                                table.Cell().Background(bg).Padding(5).Text(idx++.ToString()).AlignCenter().FontSize(10);
                                table.Cell().Background(bg).Padding(5).Text(d.ExpenseDate.ToString("dd/MM/yyyy HH:mm")).AlignCenter().FontSize(10);
                                table.Cell().Background(bg).Padding(5).Text(d.Description ?? "").FontSize(10);
                                table.Cell().Background(bg).Padding(5).Text($"{d.Amount:N2}").AlignCenter().FontSize(10).Bold();
                                table.Cell().Background(bg).Padding(5).Text(d.User?.FullName ?? "").AlignCenter().FontSize(10);
                            }
                        });
                    });

                    page.Footer().AlignCenter().Text($"طُبع بواسطة {(_currentUser?.FullName ?? "النظام")} - صفحة ").FontSize(8).FontColor("#9CA3AF");
                });
            }).GeneratePdf();

            var dlg = new SaveFileDialog
            {
                Title = "حفظ تقرير المصروفات",
                FileName = $"تقرير-مصروفات-{DateTime.Now:yyyyMMdd}.pdf",
                DefaultExt = ".pdf",
                Filter = "PDF Files|*.pdf"
            };

            if (dlg.ShowDialog() == true)
            {
                await File.WriteAllBytesAsync(dlg.FileName, pdfBytes);
                MessageBox.Show($"✅ تم تصدير التقرير بنجاح:\n{dlg.FileName}", "تم التصدير", MessageBoxButton.OK, MessageBoxImage.Information);
            }
        }
        catch (Exception ex)
        {
            MessageBox.Show($"خطأ أثناء تصدير PDF:\n{ex.Message}", "خطأ", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private void UpdateStatistics()
    {
        var today = DateTime.Today;
        var startOfMonth = new DateTime(today.Year, today.Month, 1);

        TodayExpenses = _allExpenses.Where(e => e.ExpenseDate.Date == today).Sum(e => e.Amount);
        MonthExpenses = _allExpenses.Where(e => e.ExpenseDate.Date >= startOfMonth).Sum(e => e.Amount);
        TotalExpenses = _allExpenses.Sum(e => e.Amount);
    }
}
