using System;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Moq;
using SmartPOS.Application.ViewModels;
using SmartPOS.Core.Entities;
using SmartPOS.Core.Interfaces;
using SmartPOS.Infrastructure.Repositories;
using Xunit;

namespace SmartPOS.UnitTests.ViewModels;

public class ShiftManagementViewModelTests
{
    private static Mock<ISettingsService> CreateSettingsServiceMock()
    {
        var settings = new Mock<ISettingsService>();
        settings.SetupGet(s => s.PrintZReportOnClose).Returns(false);
        settings.SetupGet(s => s.SaveZReportPdfOnClose).Returns(false);
        settings.SetupGet(s => s.PrinterName).Returns(string.Empty);
        settings.SetupGet(s => s.ReceiptWidth).Returns(80);
        settings.SetupGet(s => s.ReceiptLanguage).Returns("Both");
        return settings;
    }

    [Fact]
    public async Task OpenShiftAsync_ShouldCreateNewShift_WhenNoShiftIsOpen()
    {
        var (factory, context) = TestDbContextFactory.CreateInMemory();
        var shiftRepository = new ShiftRepository(context);
        var user = new User { Id = 1, Role = UserRole.Admin, Username = "admin", FullName = "Admin" };
        var notificationService = new Mock<INotificationService>();
        var printingService = new Mock<IPrintingService>();
        var settingsService = CreateSettingsServiceMock();
        var authorizationService = new Mock<IAuthorizationService>();

        var viewModel = new ShiftManagementViewModel(
            factory,
            shiftRepository,
            printingService.Object,
            settingsService.Object,
            user,
            notificationService.Object,
            authorizationService.Object);

        viewModel.OpeningBalanceInput = 1000;

        await viewModel.OpenShiftCommand.ExecuteAsync(null);

        var shift = await context.Shifts.FirstOrDefaultAsync();
        Assert.NotNull(shift);
        Assert.Equal(ShiftStatus.Open, shift.Status);
        Assert.Equal(1000, shift.OpeningBalance);
        Assert.Equal(user.Id, shift.UserId);

        notificationService.Verify(n => n.ShowSuccess(It.IsAny<string>(), It.IsAny<string>()), Times.Once);
    }

    [Fact]
    public async Task CloseShiftAsync_ShouldCloseShiftAndCalculateBalance()
    {
        var (factory, context) = TestDbContextFactory.CreateInMemory();

        var dbUser = new User { Id = 1, Role = UserRole.Admin, Username = "admin", FullName = "Admin User" };
        context.Users.Add(dbUser);

        var shift = new Shift
        {
            Id = 1,
            UserId = 1,
            Status = ShiftStatus.Open,
            StartTime = DateTime.Now.AddHours(-8),
            OpeningBalance = 1000
        };

        var sale = new Sale
        {
            Id = 1,
            InvoiceNumber = "INV-1",
            ShiftId = 1,
            UserId = 1,
            TotalAmount = 500,
            PaymentMethod = PaymentMethod.Cash,
            Status = SaleStatus.Completed,
            SaleDate = DateTime.Now.AddHours(-1)
        };

        var expense = new Expense
        {
            Id = 1,
            UserId = 1,
            Amount = 100,
            ExpenseDate = DateTime.Now.AddHours(-1)
        };

        context.Shifts.Add(shift);
        context.Sales.Add(sale);
        context.Expenses.Add(expense);
        await context.SaveChangesAsync();

        var shiftRepository = new ShiftRepository(context);
        var notificationService = new Mock<INotificationService>();
        var printingService = new Mock<IPrintingService>();
        var settingsService = CreateSettingsServiceMock();
        var authorizationService = new Mock<IAuthorizationService>();
        var user = new User { Id = 1, Role = UserRole.Admin, FullName = "Admin User" };

        var viewModel = new ShiftManagementViewModel(
            factory,
            shiftRepository,
            printingService.Object,
            settingsService.Object,
            user,
            notificationService.Object,
            authorizationService.Object);

        await viewModel.LoadShiftsCommand.ExecuteAsync(null);

        Assert.NotNull(viewModel.CurrentShift);
        Assert.Equal(1, viewModel.CurrentShift.Id);

        viewModel.ClosingBalanceInput = 1400;

        await viewModel.CloseShiftCommand.ExecuteAsync(null);

        var closedShift = await context.Shifts.FindAsync(1);
        Assert.NotNull(closedShift);
        Assert.Equal(ShiftStatus.Closed, closedShift.Status);
        Assert.NotNull(closedShift.EndTime);
        Assert.Equal(1400, closedShift.ExpectedBalance);
        Assert.Equal(0, closedShift.Difference);

        notificationService.Verify(n => n.ShowSuccess(It.IsAny<string>(), It.IsAny<string>()), Times.Once);
        notificationService.Verify(n => n.ShowWarning(It.Is<string>(m => m.Contains("فرق الوردية")), It.IsAny<string>()), Times.Never);
    }
}
