using Moq;
using SmartPOS.Application.ViewModels;
using SmartPOS.Core.Entities;
using SmartPOS.Core.Interfaces;
using Xunit;

namespace SmartPOS.UnitTests.ViewModels;

public class ExpensesViewModelTests
{
    [Fact]
    public void OpenAddDialog_ShouldOpenAndResetFormFields()
    {
        var (factory, _) = TestDbContextFactory.CreateInMemory();
        var user = new User { Id = 1, Role = UserRole.Admin };
        var authService = new Mock<IAuthorizationService>();
        var viewModel = new ExpensesViewModel(factory, user, authService.Object)
        {
            NewExpenseDescription = "قديم",
            NewExpenseAmount = 99,
            SelectedCategoryIndex = 2,
            IsAddExpenseDialogOpen = false
        };

        viewModel.OpenAddDialogCommand.Execute(null);

        Assert.True(viewModel.IsAddExpenseDialogOpen);
        Assert.Equal(string.Empty, viewModel.NewExpenseDescription);
        Assert.Equal(0, viewModel.NewExpenseAmount);
        Assert.Equal(0, viewModel.SelectedCategoryIndex);
    }

    [Fact]
    public void CloseAddDialog_ShouldCloseDialog()
    {
        var (factory, _) = TestDbContextFactory.CreateInMemory();
        var user = new User { Id = 2, Role = UserRole.Cashier };
        var authService = new Mock<IAuthorizationService>();
        var viewModel = new ExpensesViewModel(factory, user, authService.Object)
        {
            IsAddExpenseDialogOpen = true
        };

        viewModel.CloseAddDialogCommand.Execute(null);

        Assert.False(viewModel.IsAddExpenseDialogOpen);
    }
}
