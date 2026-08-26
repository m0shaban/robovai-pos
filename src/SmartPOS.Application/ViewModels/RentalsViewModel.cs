using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Threading;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.EntityFrameworkCore;
using SmartPOS.Core.Entities;
using SmartPOS.Core.Interfaces;
using SmartPOS.Infrastructure.Data;

namespace SmartPOS.Application.ViewModels;

public partial class RentalDeviceCardViewModel : ObservableObject
{
    public RentalDevice Device { get; }

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IsOccupied))]
    [NotifyPropertyChangedFor(nameof(IsAvailable))]
    [NotifyPropertyChangedFor(nameof(CardBackgroundColor))]
    private RentalSession? _currentSession;

    public bool IsOccupied => CurrentSession != null && CurrentSession.Status == RentalSessionStatus.Running;
    public bool IsAvailable => !IsOccupied;

    public string CardBackgroundColor => IsOccupied ? (IsTimeUp ? "#7F1D1D" : "#064E3B") : "#16203A"; // Reddish if time up, greenish if running, dark if available

    [ObservableProperty]
    private string _timerText = "00:00:00";

    [ObservableProperty]
    private string _statusText = "متاح";

    [ObservableProperty]
    private decimal _currentAmount = 0m;

    [ObservableProperty]
    private bool _isTimeUp = false;

    private bool _hasAlertedTimeUp = false;

    // --- Inputs for Starting Session ---
    [ObservableProperty]
    private string _inputCustomerName = string.Empty;

    [ObservableProperty]
    private int _inputDurationMinutes = 60;

    [ObservableProperty]
    private bool _inputIsOpenDuration = false;

    public RentalDeviceCardViewModel(RentalDevice device, RentalSession? activeSession)
    {
        Device = device;
        CurrentSession = activeSession;
        UpdateTimer();
    }

    public void UpdateTimer()
    {
        if (!IsOccupied || CurrentSession == null)
        {
            TimerText = "00:00:00";
            StatusText = "متاح";
            CurrentAmount = 0;
            IsTimeUp = false;
            _hasAlertedTimeUp = false;
            return;
        }

        var now = DateTime.Now;
        var elapsed = now - CurrentSession.StartTime;

        if (CurrentSession.DurationMinutes.HasValue && CurrentSession.ExpectedEndTime.HasValue)
        {
            // Countdown Mode
            var remaining = CurrentSession.ExpectedEndTime.Value - now;
            if (remaining.TotalSeconds <= 0)
            {
                TimerText = "انتهى الوقت!";
                StatusText = "انتهى الوقت";
                IsTimeUp = true;

                if (!_hasAlertedTimeUp)
                {
                    _hasAlertedTimeUp = true;
                    System.Windows.Application.Current.Dispatcher.InvokeAsync(() =>
                    {
                        MessageBox.Show($"انتهى الوقت المخصص للجهاز: {Device.Name}\nالعميل: {CurrentSession.CustomerName ?? "بدون اسم"}\nالرجاء إنهاء الجلسة أو تمديدها.", "تنبيه انتهاء الوقت", MessageBoxButton.OK, MessageBoxImage.Information);
                    });
                }
            }
            else
            {
                TimerText = $"{(int)remaining.TotalHours:D2}:{remaining.Minutes:D2}:{remaining.Seconds:D2}";
                StatusText = "قيد التشغيل";
                IsTimeUp = false;
                _hasAlertedTimeUp = false;
            }

            // Calculate Amount (Fixed based on duration)
            CurrentAmount = CurrentSession.HourlyRateApplied * (CurrentSession.DurationMinutes.Value / 60m);
        }
        else
        {
            // Stopwatch Mode
            TimerText = $"{(int)elapsed.TotalHours:D2}:{elapsed.Minutes:D2}:{elapsed.Seconds:D2}";
            StatusText = "لعب مفتوح";
            IsTimeUp = false;

            // Calculate Amount (Dynamic based on elapsed time)
            var totalMinutes = (decimal)elapsed.TotalMinutes;
            CurrentAmount = CurrentSession.HourlyRateApplied * (totalMinutes / 60m);
        }
    }
}

/// <summary>
/// Refactored to use IDbContextFactory (v5.1)
/// </summary>
public partial class RentalsViewModel : BaseViewModel, IDisposable
{
    private readonly IDbContextFactory<AppDbContext> _contextFactory;
    private readonly DispatcherTimer _timer;

    [ObservableProperty]
    private ObservableCollection<RentalDeviceCardViewModel> _devices = new();

    [ObservableProperty]
    private RentalDeviceCardViewModel? _selectedDevice;

    // Device Management
    [ObservableProperty]
    private string _newDeviceName = string.Empty;

    [ObservableProperty]
    private decimal _newDeviceRate = 50m;

    [ObservableProperty]
    private DeviceType _newDeviceType = DeviceType.PlayStation;

    private readonly IUserService _userService;
    private readonly IShiftRepository _shiftRepository;
    private readonly IPrintingService _printingService;
    private readonly ISettingsService _settingsService;

    public RentalsViewModel(
        IDbContextFactory<AppDbContext> contextFactory,
        IUserService userService,
        IShiftRepository shiftRepository,
        IPrintingService printingService,
        ISettingsService settingsService)
    {
        _contextFactory = contextFactory;
        _userService = userService;
        _shiftRepository = shiftRepository;
        _printingService = printingService;
        _settingsService = settingsService;

        _timer = new DispatcherTimer
        {
            Interval = TimeSpan.FromSeconds(1)
        };
        _timer.Tick += Timer_Tick;

        _ = LoadDevicesAsync();
    }

    private void Timer_Tick(object? sender, EventArgs e)
    {
        foreach (var device in Devices)
        {
            if (device.IsOccupied)
            {
                device.UpdateTimer();
            }
        }
    }

    private async Task LoadDevicesAsync()
    {
        await ExecuteBusyAsync(async () =>
        {
            await using var ctx = await _contextFactory.CreateDbContextAsync();
            var dbDevices = await ctx.RentalDevices
                .Where(d => d.IsActive && !d.IsDeleted)
                .AsNoTracking()
                .ToListAsync();

            var activeSessions = await ctx.RentalSessions
                .Where(s => s.Status == RentalSessionStatus.Running && !s.IsDeleted)
                .AsNoTracking()
                .ToListAsync();

            var cards = dbDevices.Select(d =>
            {
                var session = activeSessions.FirstOrDefault(s => s.RentalDeviceId == d.Id);
                return new RentalDeviceCardViewModel(d, session);
            });

            Devices.Clear();
            foreach (var card in cards) Devices.Add(card);

            if (!_timer.IsEnabled) _timer.Start();

        }, "جاري تحميل الأجهزة...");
    }

    private async Task<Product> GetOrCreateRentalProductAsync()
    {
        await using var ctx = await _contextFactory.CreateDbContextAsync();
        var product = await ctx.Products.FirstOrDefaultAsync(p => p.Name == "إيجار ألعاب");
        if (product == null)
        {
            var category = await ctx.Categories.FirstOrDefaultAsync(c => c.Name == "إيجارات");
            if (category == null)
            {
                category = new Category { Name = "إيجارات", IsActive = true };
                ctx.Categories.Add(category);
                await ctx.SaveChangesAsync();
            }

            product = new Product
            {
                Name = "إيجار ألعاب",
                CategoryId = category.Id,
                SellingPrice = 0,
                PurchasePrice = 0,
                IsActive = true
            };
            ctx.Products.Add(product);
            await ctx.SaveChangesAsync();
        }
        return product;
    }

    private async Task PrintRentalTicketAsync(Sale sale, string deviceName, string? customerName, DateTime startTime, DateTime? endTime, string durationText)
    {
        var ticketData = new RentalTicketData
        {
            StoreName = _settingsService.StoreName ?? "Smart POS",
            StoreAddress = _settingsService.StoreAddress ?? "عنوان المحل",
            Phone = _settingsService.StorePhone ?? "",
            InvoiceNumber = sale.InvoiceNumber,
            DeviceName = deviceName,
            CustomerName = customerName ?? "عميل صالة",
            StartTime = startTime,
            EndTime = endTime,
            DurationText = durationText,
            TotalAmount = sale.TotalAmount,
            Footer = _settingsService.FooterMessage ?? "نتمنى لكم وقتاً ممتعاً!"
        };

        string printerName = _settingsService.PrinterName ?? "Microsoft Print to PDF";
        await _printingService.PrintRentalTicketAsync(printerName, ticketData, 80, "Arabic", "1", false);
    }

    [RelayCommand]
    private async Task StartSessionAsync(RentalDeviceCardViewModel card)
    {
        if (card.IsOccupied)
        {
            MessageBox.Show("الجهاز مشغول حالياً!", "تنبيه", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        if (!card.InputIsOpenDuration && card.InputDurationMinutes <= 0)
        {
            MessageBox.Show("الرجاء إدخال مدة صحيحة أو اختيار وقت مفتوح", "تنبيه", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        await ExecuteBusyAsync(async () =>
        {
            await using var ctx = await _contextFactory.CreateDbContextAsync();
            var session = new RentalSession
            {
                RentalDeviceId = card.Device.Id,
                StartTime = DateTime.Now,
                HourlyRateApplied = card.Device.HourlyRate,
                Status = RentalSessionStatus.Running,
                CustomerName = card.InputCustomerName
            };

            Sale? sale = null;

            if (!card.InputIsOpenDuration)
            {
                session.DurationMinutes = card.InputDurationMinutes;
                session.ExpectedEndTime = session.StartTime.AddMinutes(card.InputDurationMinutes);

                // Prepaid Sale
                var shift = await _shiftRepository.GetActiveShiftByUserIdAsync(_userService.CurrentUser?.Id ?? 0);
                var rentalProduct = await GetOrCreateRentalProductAsync();
                decimal expectedAmount = session.HourlyRateApplied * (card.InputDurationMinutes / 60m);

                sale = new Sale
                {
                    InvoiceNumber = $"RNT-{DateTime.Now:yyyyMMddHHmmss}",
                    SaleDate = DateTime.Now,
                    Subtotal = expectedAmount,
                    TotalAmount = expectedAmount,
                    AmountPaid = expectedAmount,
                    PaymentMethod = PaymentMethod.Cash,
                    Status = SaleStatus.Completed,
                    UserId = _userService.CurrentUser?.Id ?? 0,
                    ShiftId = shift?.Id,
                    Notes = $"الجهاز: {card.Device.Name} | {card.InputDurationMinutes} دقيقة"
                };

                sale.SaleDetails.Add(new SaleDetail
                {
                    ProductId = rentalProduct.Id,
                    Quantity = 1,
                    UnitPrice = expectedAmount,
                    UnitCost = 0,
                    LineTotal = expectedAmount
                });

                ctx.Sales.Add(sale);
                session.TotalAmount = expectedAmount;
            }

            ctx.RentalSessions.Add(session);
            await ctx.SaveChangesAsync();

            if (sale != null)
            {
                session.SaleId = sale.Id;
                await ctx.SaveChangesAsync();

                // Print Ticket
                await PrintRentalTicketAsync(sale, card.Device.Name, session.CustomerName, session.StartTime, session.ExpectedEndTime, $"{card.InputDurationMinutes} دقيقة");
            }

            card.CurrentSession = session;
            card.UpdateTimer();

            card.InputCustomerName = string.Empty;
            card.InputDurationMinutes = 60;
            card.InputIsOpenDuration = false;

        }, "جاري بدء الجلسة...");
    }

    [RelayCommand]
    private async Task EndSessionReservedAsync(RentalDeviceCardViewModel card)
    {
        if (!card.IsOccupied || card.CurrentSession == null) return;

        var confirm = MessageBox.Show(
            $"هل تريد إنهاء الجلسة للجهاز {card.Device.Name} على المدة المتفق عليها؟ (لن يتم احتساب وقت إضافي ولن يتم خصم وقت)",
            "إنهاء على المدة المتفق عليها", MessageBoxButton.YesNo, MessageBoxImage.Question);

        if (confirm != MessageBoxResult.Yes) return;

        await ExecuteBusyAsync(async () =>
        {
            await using var ctx = await _contextFactory.CreateDbContextAsync();
            var session = await ctx.RentalSessions.FindAsync(card.CurrentSession.Id);
            if (session == null) return;

            // Fix the actual end time to what was expected, so no extra charges or refunds occur
            session.ActualEndTime = session.ExpectedEndTime ?? DateTime.Now;
            session.Status = RentalSessionStatus.Completed;

            // For prepaid, total amount remains what was paid (no extra, no refund)
            if (session.SaleId != null)
            {
                var originalSale = await ctx.Sales.FirstOrDefaultAsync(s => s.Id == session.SaleId);
                if (originalSale != null)
                {
                    session.TotalAmount = originalSale.TotalAmount; // Exact paid
                    string dur = session.DurationMinutes.HasValue ? $"{session.DurationMinutes.Value} دقيقة" : "وقت مفتوح";
                    await PrintRentalTicketAsync(originalSale, card.Device.Name, session.CustomerName, session.StartTime, session.ActualEndTime, dur);
                }
            }
            else
            {
                // Open-time session - calculate till now since there is no 'expected' time
                var elapsed = session.ActualEndTime.Value - session.StartTime;
                var totalMinutes = (decimal)elapsed.TotalMinutes;
                session.TotalAmount = session.HourlyRateApplied * (totalMinutes / 60m);

                var shift = await _shiftRepository.GetActiveShiftByUserIdAsync(_userService.CurrentUser?.Id ?? 0);
                var rentalProduct = await GetOrCreateRentalProductAsync();
                var sale = new Sale
                {
                    InvoiceNumber = $"RNT-{DateTime.Now:yyyyMMddHHmmss}",
                    SaleDate = DateTime.Now,
                    Subtotal = session.TotalAmount,
                    TotalAmount = session.TotalAmount,
                    AmountPaid = session.TotalAmount,
                    PaymentMethod = PaymentMethod.Cash,
                    Status = SaleStatus.Completed,
                    UserId = _userService.CurrentUser?.Id ?? 0,
                    ShiftId = shift?.Id,
                    Notes = $"الجهاز: {card.Device.Name} | وقت مفتوح: {(int)totalMinutes} دقيقة"
                };

                sale.SaleDetails.Add(new SaleDetail
                {
                    ProductId = rentalProduct.Id,
                    Quantity = 1,
                    UnitPrice = session.TotalAmount,
                    UnitCost = 0,
                    LineTotal = session.TotalAmount
                });

                ctx.Sales.Add(sale);
                await ctx.SaveChangesAsync();

                session.SaleId = sale.Id;
                await PrintRentalTicketAsync(sale, card.Device.Name, session.CustomerName, session.StartTime, session.ActualEndTime, "وقت مفتوح");
            }

            await ctx.SaveChangesAsync();

            card.CurrentSession = null;
            card.UpdateTimer();

            MessageBox.Show($"تم إنهاء الجلسة حسب المدة المتفق عليها.\nالمبلغ الإجمالي: {session.TotalAmount:N2} ج.م", "تمت العملية", MessageBoxButton.OK, MessageBoxImage.Information);

        }, "جاري إنهاء الجلسة...");
    }

    [RelayCommand]
    private async Task EndSessionAsync(RentalDeviceCardViewModel card)
    {
        if (!card.IsOccupied || card.CurrentSession == null) return;

        var confirm = MessageBox.Show(
            $"هل تريد إنهاء الجلسة للجهاز {card.Device.Name}؟\nالحساب التقديري: {card.CurrentAmount:N2} ج.م",
            "إنهاء الجلسة", MessageBoxButton.YesNo, MessageBoxImage.Question);

        if (confirm != MessageBoxResult.Yes) return;

        await ExecuteBusyAsync(async () =>
        {
            await using var ctx = await _contextFactory.CreateDbContextAsync();
            var session = await ctx.RentalSessions.FindAsync(card.CurrentSession.Id);
            if (session == null) return;

            session.ActualEndTime = DateTime.Now;
            session.Status = RentalSessionStatus.Completed;

            // Recalculate exact amount
            var elapsed = session.ActualEndTime.Value - session.StartTime;
            var totalMinutes = (decimal)elapsed.TotalMinutes;
            session.TotalAmount = session.HourlyRateApplied * (totalMinutes / 60m);

            if (session.SaleId == null)
            {
                // Open-time session — create sale now for exact elapsed time
                var shift = await _shiftRepository.GetActiveShiftByUserIdAsync(_userService.CurrentUser?.Id ?? 0);
                var rentalProduct = await GetOrCreateRentalProductAsync();

                var sale = new Sale
                {
                    InvoiceNumber = $"RNT-{DateTime.Now:yyyyMMddHHmmss}",
                    SaleDate = DateTime.Now,
                    Subtotal = session.TotalAmount,
                    TotalAmount = session.TotalAmount,
                    AmountPaid = session.TotalAmount,
                    PaymentMethod = PaymentMethod.Cash,
                    Status = SaleStatus.Completed,
                    UserId = _userService.CurrentUser?.Id ?? 0,
                    ShiftId = shift?.Id,
                    Notes = $"الجهاز: {card.Device.Name} | وقت مفتوح: {(int)totalMinutes} دقيقة"
                };

                sale.SaleDetails.Add(new SaleDetail
                {
                    ProductId = rentalProduct.Id,
                    Quantity = 1,
                    UnitPrice = session.TotalAmount,
                    UnitCost = 0,
                    LineTotal = session.TotalAmount
                });

                ctx.Sales.Add(sale);
                await ctx.SaveChangesAsync();

                session.SaleId = sale.Id;
                await PrintRentalTicketAsync(sale, card.Device.Name, session.CustomerName, session.StartTime, session.ActualEndTime, "وقت مفتوح");
            }
            else
            {
                // Prepaid session — compare paid amount vs actual amount
                var originalSale = await ctx.Sales.FirstOrDefaultAsync(s => s.Id == session.SaleId);
                if (originalSale != null)
                {
                    var paidAmount = originalSale.TotalAmount; // what was paid upfront
                    var actualAmount = session.TotalAmount;       // what was actually used
                    var diff = actualAmount - paidAmount;          // positive = extra to charge, negative = refund

                    if (diff < -0.01m)
                    {
                        // Customer paid MORE than they used — refund the difference
                        var shift = await _shiftRepository.GetActiveShiftByUserIdAsync(_userService.CurrentUser?.Id ?? 0);
                        var refundSale = new Sale
                        {
                            InvoiceNumber = $"RFD-{DateTime.Now:yyyyMMddHHmmss}",
                            SaleDate = DateTime.Now,
                            Subtotal = diff,         // negative
                            TotalAmount = diff,      // negative
                            AmountPaid = diff,
                            ChangeAmount = 0,
                            PaymentMethod = PaymentMethod.Cash,
                            Status = SaleStatus.Refunded,
                            UserId = _userService.CurrentUser?.Id ?? 0,
                            ShiftId = shift?.Id,
                            Notes = $"رد فرق وقت إيجار - {card.Device.Name}"
                        };
                        ctx.Sales.Add(refundSale);
                        await ctx.SaveChangesAsync();
                        MessageBox.Show(
                            $"تم إنهاء الجلسة بنجاح.\nالمدفوع مقدماً: {paidAmount:N2} ج.م\nالمستخدم فعلياً: {actualAmount:N2} ج.م\nالمسترد للعميل: {Math.Abs(diff):N2} ج.م",
                            "تمت العملية", MessageBoxButton.OK, MessageBoxImage.Information);
                    }
                    else if (diff > 0.01m)
                    {
                        // Customer used MORE than they paid — charge extra
                        var shift = await _shiftRepository.GetActiveShiftByUserIdAsync(_userService.CurrentUser?.Id ?? 0);
                        var rentalProduct = await GetOrCreateRentalProductAsync();
                        var extraSale = new Sale
                        {
                            InvoiceNumber = $"RNT-{DateTime.Now:yyyyMMddHHmmss}",
                            SaleDate = DateTime.Now,
                            Subtotal = diff,
                            TotalAmount = diff,
                            AmountPaid = diff,
                            PaymentMethod = PaymentMethod.Cash,
                            Status = SaleStatus.Completed,
                            UserId = _userService.CurrentUser?.Id ?? 0,
                            ShiftId = shift?.Id,
                            Notes = $"فرق وقت إيجار إضافي - {card.Device.Name}"
                        };
                        extraSale.SaleDetails.Add(new SaleDetail { ProductId = rentalProduct.Id, Quantity = 1, UnitPrice = diff, UnitCost = 0, LineTotal = diff });
                        ctx.Sales.Add(extraSale);
                        await ctx.SaveChangesAsync();
                        MessageBox.Show(
                            $"تجاوز الوقت المدفوع مقدماً!\nمبلغ إضافي مستحق: {diff:N2} ج.م",
                            "مبلغ إضافي", MessageBoxButton.OK, MessageBoxImage.Warning);
                    }
                    else
                    {
                        // Exact — just print closing ticket
                        string dur = session.DurationMinutes.HasValue ? $"{session.DurationMinutes.Value} دقيقة" : "وقت مفتوح";
                        await PrintRentalTicketAsync(originalSale, card.Device.Name, session.CustomerName, session.StartTime, session.ActualEndTime, dur);
                        MessageBox.Show($"تم إنهاء الجلسة بنجاح.\nالمبلغ: {session.TotalAmount:N2} ج.م", "تمت العملية", MessageBoxButton.OK, MessageBoxImage.Information);
                    }
                }
            }

            await ctx.SaveChangesAsync();

            card.CurrentSession = null;
            card.UpdateTimer();

            MessageBox.Show($"تم إنهاء الجلسة بنجاح.\nالمبلغ الإجمالي: {session.TotalAmount:N2} ج.م", "تمت العملية", MessageBoxButton.OK, MessageBoxImage.Information);

        }, "جاري إنهاء الجلسة...");
    }

    [RelayCommand]
    private async Task AddExtraTimeAsync(RentalDeviceCardViewModel card)
    {
        if (!card.IsOccupied || card.CurrentSession == null || !card.CurrentSession.DurationMinutes.HasValue)
        {
            MessageBox.Show("الجهاز غير مشغول أو يعمل بنظام الوقت المفتوح.", "تنبيه", MessageBoxButton.OK, MessageBoxImage.Information);
            return;
        }

        await ExecuteBusyAsync(async () =>
        {
            await using var ctx = await _contextFactory.CreateDbContextAsync();
            var session = await ctx.RentalSessions.FindAsync(card.CurrentSession.Id);
            if (session == null) return;

            int extraMinutes = 30;
            session.DurationMinutes += extraMinutes;
            session.ExpectedEndTime = session.StartTime.AddMinutes(session.DurationMinutes.GetValueOrDefault(0));

            // Prepaid Sale for Extra Time
            var shift = await _shiftRepository.GetActiveShiftByUserIdAsync(_userService.CurrentUser?.Id ?? 0);
            var rentalProduct = await GetOrCreateRentalProductAsync();
            decimal extraAmount = session.HourlyRateApplied * (extraMinutes / 60m);

            var sale = new Sale
            {
                InvoiceNumber = $"RNT-{DateTime.Now:yyyyMMddHHmmss}",
                SaleDate = DateTime.Now,
                Subtotal = extraAmount,
                TotalAmount = extraAmount,
                AmountPaid = extraAmount,
                PaymentMethod = PaymentMethod.Cash,
                Status = SaleStatus.Completed,
                UserId = _userService.CurrentUser?.Id ?? 0,
                ShiftId = shift?.Id,
                Notes = $"تزويد وقت: {card.Device.Name} | {extraMinutes} دقيقة إضافية"
            };

            sale.SaleDetails.Add(new SaleDetail
            {
                ProductId = rentalProduct.Id,
                Quantity = 1,
                UnitPrice = extraAmount,
                UnitCost = 0,
                LineTotal = extraAmount
            });

            ctx.Sales.Add(sale);

            // Total amount for the session increases
            session.TotalAmount += extraAmount;

            await ctx.SaveChangesAsync();
            await PrintRentalTicketAsync(sale, card.Device.Name, session.CustomerName, DateTime.Now, null, $"+{extraMinutes} دقيقة إضافية");

            card.CurrentSession = session;
            card.UpdateTimer();
        });
    }

    [RelayCommand]
    private async Task AddDeviceAsync()
    {
        if (string.IsNullOrWhiteSpace(NewDeviceName))
        {
            MessageBox.Show("يرجى إدخال اسم الجهاز", "تنبيه", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        await ExecuteBusyAsync(async () =>
        {
            await using var ctx = await _contextFactory.CreateDbContextAsync();
            var device = new RentalDevice
            {
                Name = NewDeviceName.Trim(),
                Type = NewDeviceType,
                HourlyRate = NewDeviceRate,
                IsActive = true
            };

            ctx.RentalDevices.Add(device);
            await ctx.SaveChangesAsync();

            NewDeviceName = string.Empty;
        }, "جاري حفظ الجهاز...", "✅ تم إضافة الجهاز");

        // Reload OUTSIDE ExecuteBusyAsync to avoid nested busy lock
        await LoadDevicesAsync();
    }

    [RelayCommand]
    private async Task DeleteDeviceAsync(RentalDeviceCardViewModel card)
    {
        if (card.IsOccupied)
        {
            MessageBox.Show("لا يمكن حذف جهاز وهو مشغول. أنهِ الجلسة أولاً.", "تنبيه", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        var confirm = MessageBox.Show(
            $"هل أنت متأكد من حذف الجهاز \"{card.Device.Name}\"؟",
            "تأكيد الحذف", MessageBoxButton.YesNo, MessageBoxImage.Warning);

        if (confirm != MessageBoxResult.Yes) return;

        await ExecuteBusyAsync(async () =>
        {
            await using var ctx = await _contextFactory.CreateDbContextAsync();
            var device = await ctx.RentalDevices.FindAsync(card.Device.Id);
            if (device == null) return;
            device.IsDeleted = true;
            device.IsActive = false;
            await ctx.SaveChangesAsync();
        }, "جاري الحذف...", "✅ تم حذف الجهاز");

        await LoadDevicesAsync();
    }

    public void Dispose()
    {
        _timer.Stop();
        _timer.Tick -= Timer_Tick;
    }
}
