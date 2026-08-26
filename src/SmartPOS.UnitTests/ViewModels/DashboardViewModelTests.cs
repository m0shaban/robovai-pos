using System;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using SmartPOS.Application.ViewModels;
using SmartPOS.Core.Entities;
using Xunit;

namespace SmartPOS.UnitTests.ViewModels;

public class DashboardViewModelTests
{
    [Fact]
    public async Task LoadDashboardData_ShouldCalculateCurrentShiftSales()
    {
        // Arrange
        var (factory, context) = TestDbContextFactory.CreateInMemory();
        var user = new User { Id = 1, Role = UserRole.Cashier };
        var activeShift = new Shift
        {
            Id = 1,
            UserId = 1,
            Status = ShiftStatus.Open,
            StartTime = DateTime.Now.AddHours(-2)
        };

        // Sale INSIDE shift
        var sale1 = new Sale
        {
            Id = 1,
            UserId = 1,
            ShiftId = 1,
            TotalAmount = 200,
            Status = SaleStatus.Completed,
            SaleDate = DateTime.Now.AddHours(-1)
        };

        // Sale BEFORE shift (should be ignored)
        var sale2 = new Sale
        {
            Id = 2,
            UserId = 1,
            ShiftId = null,
            TotalAmount = 500,
            Status = SaleStatus.Completed,
            SaleDate = DateTime.Now.AddHours(-5)
        };

        context.Shifts.Add(activeShift);
        context.Sales.AddRange(sale1, sale2);
        await context.SaveChangesAsync();

        var viewModel = new DashboardViewModel(factory, user);

        // Act
        await viewModel.LoadDashboardDataCommand.ExecuteAsync(null);

        // Assert
        Assert.Equal(200, viewModel.CurrentShiftSales);
    }
}
