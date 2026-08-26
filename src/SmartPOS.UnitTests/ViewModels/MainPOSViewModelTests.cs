using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Moq;
using SmartPOS.Application.DTOs;
using SmartPOS.Application.ViewModels;
using SmartPOS.Core.Entities;
using SmartPOS.Core.Interfaces;
using SmartPOS.Infrastructure.Data;
using Xunit;

namespace SmartPOS.UnitTests.ViewModels;

public class MainPOSViewModelTests : IDisposable
{
    private readonly SqliteConnection _connection;
    private readonly DbContextOptions<AppDbContext> _options;

    public MainPOSViewModelTests()
    {
        _connection = new SqliteConnection("DataSource=:memory:");
        _connection.Open();
        _options = new DbContextOptionsBuilder<AppDbContext>()
            .UseSqlite(_connection)
            .Options;

        using var ctx = new AppDbContext(_options);
        ctx.Database.EnsureCreated();
    }

    public void Dispose()
    {
        _connection.Close();
        _connection.Dispose();
    }

    private class SqliteTestDbContextFactory : IDbContextFactory<AppDbContext>
    {
        private readonly DbContextOptions<AppDbContext> _options;

        public SqliteTestDbContextFactory(DbContextOptions<AppDbContext> options)
        {
            _options = options;
        }

        public AppDbContext CreateDbContext() => new AppDbContext(_options);

        public Task<AppDbContext> CreateDbContextAsync(System.Threading.CancellationToken cancellationToken = default)
            => Task.FromResult(new AppDbContext(_options));
    }

    private (MainPOSViewModel vm, AppDbContext ctx, User user, Mock<INotificationService> notifMock) CreateViewModel()
    {
        var factory = new SqliteTestDbContextFactory(_options);
        var context = new AppDbContext(_options);

        var user = new User { Id = 1, Username = "cashier", FullName = "Cashier", Role = UserRole.Cashier, PasswordHash = "hash" };
        context.Users.Add(user);

        var activeShift = new Shift { Id = 10, UserId = 1, Status = ShiftStatus.Open, StartTime = DateTime.Now };
        context.Shifts.Add(activeShift);

        var category = new Category { Id = 1, Name = "General" };
        var product = new Product
        {
            Id = 1,
            Name = "Test Product",
            SellingPrice = 100,
            PurchasePrice = 60,
            Stock = 10,
            Barcode = "123456",
            CategoryId = 1,
            IsActive = true
        };

        context.Categories.Add(category);
        context.Products.Add(product);
        context.SaveChanges();

        var shiftRepository = new Mock<IShiftRepository>();
        shiftRepository.Setup(r => r.HasActiveShiftAsync(user.Id)).ReturnsAsync(true);
        shiftRepository.Setup(r => r.GetActiveShiftByUserIdAsync(user.Id)).ReturnsAsync(activeShift);

        var printingService = new Mock<IPrintingService>();
        printingService.Setup(p => p.GetAvailablePrinters()).Returns(new List<string> { "Printer1" });

        var barcodeService = new Mock<IBarcodeService>();
        var settingsService = new Mock<ISettingsService>();
        settingsService.Setup(s => s.LoadSettingsAsync()).Returns(Task.CompletedTask);
        settingsService.SetupGet(s => s.TaxPercentage).Returns(0);
        settingsService.SetupGet(s => s.AutoPrintReceipt).Returns(false);
        settingsService.SetupGet(s => s.AutoOpenDrawer).Returns(false);
        settingsService.SetupGet(s => s.KitchenPrinterEnabled).Returns(false);
        settingsService.SetupGet(s => s.CashDrawerPrinter).Returns(string.Empty);
        settingsService.SetupGet(s => s.PrinterName).Returns("Printer1");
        settingsService.SetupGet(s => s.DrawerPin).Returns("1");

        var notificationService = new Mock<INotificationService>();
        var authorizationService = new Mock<IAuthorizationService>();
        authorizationService.Setup(a => a.HasPermission(It.IsAny<Permissions>())).Returns(true);
        authorizationService.Setup(a => a.RequestAdminOverrideAsync(It.IsAny<string>())).ReturnsAsync(true);
        authorizationService.Setup(a => a.LogAuditAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<int?>())).Returns(Task.CompletedTask);

        var vm = new MainPOSViewModel(
            factory,
            shiftRepository.Object,
            printingService.Object,
            barcodeService.Object,
            settingsService.Object,
            notificationService.Object,
            authorizationService.Object,
            user);

        return (vm, context, user, notificationService);
    }

    [Fact]
    public async Task SubmitOrder_ShouldLinkSaleToActiveShift_AndCompleteSale()
    {
        var (viewModel, context, user, notificationService) = CreateViewModel();

        viewModel.CartItems.Add(new CartItem
        {
            ProductId = 1,
            Name = "Test Product",
            Barcode = "123456",
            UnitPrice = 100,
            UnitCost = 60,
            Quantity = 1,
            AvailableStock = 10
        });

        viewModel.Subtotal = 100;
        viewModel.TotalAmount = 100;
        viewModel.AmountPaid = 100;

        await viewModel.SubmitOrderCommand.ExecuteAsync(null);

        var sale = await context.Sales.FirstOrDefaultAsync();
        Assert.NotNull(sale);
        Assert.Equal(10, sale.ShiftId);
        Assert.Equal(SaleStatus.Completed, sale.Status);

        notificationService.Verify(n => n.ShowSuccess(It.IsAny<string>(), It.IsAny<string>()), Times.Once);
    }

    [Fact]
    public void QuickTender_And_ChangeDue_ShouldCalculateCorrectly()
    {
        var (viewModel, _, _, _) = CreateViewModel();

        viewModel.CartItems.Add(new CartItem
        {
            ProductId = 1,
            Name = "Test Product",
            UnitPrice = 120,
            Quantity = 1,
            AvailableStock = 10
        });

        viewModel.IncreaseQuantityCommand.Execute(viewModel.CartItems.First()); // Total = 240
        Assert.Equal(240, viewModel.TotalAmount);

        // Verify quick tender amounts generated
        Assert.Contains(240m, viewModel.QuickTenderAmounts);
        Assert.Contains(500m, viewModel.QuickTenderAmounts);

        // Select 500 EGP
        viewModel.SelectQuickTenderCommand.Execute(500m);
        Assert.Equal(500, viewModel.AmountPaid);
        Assert.Equal(260, viewModel.ChangeDue);
    }

    [Fact]
    public void ParkAndRecallOrder_ShouldPreserveItems_AndClearActiveCart()
    {
        var (viewModel, _, _, _) = CreateViewModel();

        viewModel.CartItems.Add(new CartItem
        {
            ProductId = 1,
            Name = "Item 1",
            UnitPrice = 50,
            Quantity = 2,
            AvailableStock = 10
        });

        viewModel.IncreaseQuantityCommand.Execute(viewModel.CartItems.First());
        var initialCount = viewModel.CartItems.Count;
        Assert.True(initialCount > 0);

        // 1. Park Order
        viewModel.ParkCurrentOrderCommand.Execute(null);
        Assert.Equal(1, viewModel.ParkedOrdersCount);
        Assert.Empty(viewModel.CartItems);

        // 2. Recall Order
        var parked = viewModel.ParkedOrders.First();
        viewModel.RecallParkedOrderCommand.Execute(parked);
        Assert.Equal(0, viewModel.ParkedOrdersCount);
        Assert.Single(viewModel.CartItems);
        Assert.Equal("Item 1", viewModel.CartItems.First().Name);
    }

    [Fact]
    public void QuickDiscounts_ShouldRecalculateTotalsAccurately()
    {
        var (viewModel, _, _, _) = CreateViewModel();

        viewModel.CartItems.Add(new CartItem
        {
            ProductId = 1,
            Name = "Item 1",
            UnitPrice = 200,
            Quantity = 1,
            AvailableStock = 10
        });

        viewModel.IncreaseQuantityCommand.Execute(viewModel.CartItems.First()); // Total = 400
        Assert.Equal(400, viewModel.Subtotal);

        // Apply 10% discount
        viewModel.ApplyQuickPercentDiscountCommand.Execute("10");
        Assert.Equal(10, viewModel.DiscountPercentage);
        Assert.Equal(40, viewModel.DiscountAmount);
        Assert.Equal(360, viewModel.TotalAmount);

        // Clear discount
        viewModel.ClearDiscountCommand.Execute(null);
        Assert.Equal(0, viewModel.DiscountPercentage);
        Assert.Equal(0, viewModel.DiscountAmount);
        Assert.Equal(400, viewModel.TotalAmount);
    }

    [Fact]
    public void TouchNumpad_ShouldSetQuantityAndPriceWithinStockLimits()
    {
        var (viewModel, _, _, _) = CreateViewModel();

        var item = new CartItem
        {
            ProductId = 1,
            Name = "Item 1",
            UnitPrice = 100,
            Quantity = 1,
            AvailableStock = 8
        };
        viewModel.CartItems.Add(item);
        viewModel.SelectedCartItem = item;

        // Open Numpad and type '5'
        viewModel.ToggleTouchNumpadCommand.Execute(null);
        Assert.True(viewModel.IsTouchNumpadVisible);

        viewModel.NumpadClearCommand.Execute(null);
        viewModel.NumpadDigitCommand.Execute("5");
        viewModel.NumpadApplyQtyCommand.Execute(null);

        Assert.Equal(5, item.Quantity);
        Assert.False(viewModel.IsTouchNumpadVisible);
    }

    [Fact]
    public void ToggleCompactListView_ShouldSwitchViewMode()
    {
        var (viewModel, _, _, _) = CreateViewModel();

        Assert.False(viewModel.IsCompactListView);
        viewModel.ToggleCompactListViewCommand.Execute(null);
        Assert.True(viewModel.IsCompactListView);
        viewModel.ToggleCompactListViewCommand.Execute(null);
        Assert.False(viewModel.IsCompactListView);
    }
}
