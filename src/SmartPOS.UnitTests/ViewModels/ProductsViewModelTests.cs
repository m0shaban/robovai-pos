using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Moq;
using SmartPOS.Application.ViewModels;
using SmartPOS.Core.Entities;
using SmartPOS.Core.Interfaces;
using SmartPOS.Infrastructure.Data;
using Xunit;

namespace SmartPOS.UnitTests.ViewModels;

public class ProductsViewModelTests
{
    [Fact]
    public async Task LoadProducts_WhenOnlySoftDeletedProducts_ReturnsEmptyVisibleList()
    {
        var (factory, context) = TestDbContextFactory.CreateInMemory();
        context.Categories.Add(new Category { Id = 1, Name = "Cat-1" });
        context.Products.Add(new Product
        {
            Id = 1,
            Name = "Deleted Product",
            Barcode = "P-001",
            CategoryId = 1,
            IsDeleted = true
        });
        await context.SaveChangesAsync();

        var user = new User { Id = 1, Role = UserRole.Admin };
        var authService = new Mock<IAuthorizationService>();
        var viewModel = new ProductsViewModel(factory, user, authService.Object);

        await viewModel.LoadProductsCommand.ExecuteAsync(null);

        Assert.Empty(viewModel.Products);
        Assert.Empty(viewModel.FilteredProducts);
    }
}
