using System;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ClosedXML.Excel;
using Microsoft.EntityFrameworkCore;
using Moq;
using SmartPOS.Application.ViewModels;
using SmartPOS.Core.Entities;
using SmartPOS.Core.Interfaces;
using SmartPOS.Infrastructure.Data;
using Xunit;

namespace SmartPOS.UnitTests.ViewModels;

public class ProductsImportExportTests
{
    private (IDbContextFactory<AppDbContext> factory, AppDbContext context) SetupDb()
    {
        return TestDbContextFactory.CreateInMemory();
    }

    [Fact]
    public async Task ImportFromCsv_WithScrambledArabicHeadersAndCurrencySymbols_Succeeds()
    {
        // Arrange
        var (factory, context) = SetupDb();
        var user = new User { Id = 1, Role = UserRole.Admin };
        var authService = new Mock<IAuthorizationService>();
        var settingsService = new Mock<ISettingsService>();
        settingsService.Setup(s => s.ImportAutoCreateCategories).Returns(true);
        settingsService.Setup(s => s.ImportDuplicateAction).Returns("UpdateStock");

        var vm = new ProductsViewModel(factory, user, authService.Object, settingsService.Object);

        // Header order is intentionally scrambled:
        // اسم الصنف, سعر البيع, الباركود, الكمية, تكلفة الشراء, القسم, الوحدة
        var csvContent = new StringBuilder();
        csvContent.AppendLine("اسم الصنف,سعر البيع,الباركود,الكمية,تكلفة الشراء,القسم,الوحدة");
        csvContent.AppendLine("\"مياه معدنية 1.5 لتر\",\"15.50 ج.م\",\"62210001\",\"50\",\"10.00 ج.م\",\"مشروبات\",\"علبة\"");
        csvContent.AppendLine("\"شيبسي سوبر\",\"12.00\",\"62210002\",\"100\",\"8.50\",\"أغذية\",\"قطعة\"");

        var tempCsvPath = Path.Combine(Path.GetTempPath(), $"import_test_{Guid.NewGuid():N}.csv");
        await File.WriteAllTextAsync(tempCsvPath, csvContent.ToString(), new UTF8Encoding(true));

        try
        {
            // Act
            var result = await vm.ImportFromFileAsync(tempCsvPath);

            // Assert
            Assert.Equal(2, result.added);
            Assert.Equal(0, result.skipped);

            await using var verifyCtx = await factory.CreateDbContextAsync();
            var p1 = await verifyCtx.Products.Include(p => p.Category).FirstOrDefaultAsync(p => p.Barcode == "62210001");
            Assert.NotNull(p1);
            Assert.Equal("مياه معدنية 1.5 لتر", p1.Name);
            Assert.Equal(15.50m, p1.SellingPrice);
            Assert.Equal(10.00m, p1.PurchasePrice);
            Assert.Equal(50, p1.Stock);
            Assert.Equal(UnitType.Box, p1.Unit);
            Assert.Equal("مشروبات", p1.Category.Name);

            var p2 = await verifyCtx.Products.Include(p => p.Category).FirstOrDefaultAsync(p => p.Barcode == "62210002");
            Assert.NotNull(p2);
            Assert.Equal("شيبسي سوبر", p2.Name);
            Assert.Equal(12.00m, p2.SellingPrice);
            Assert.Equal(8.50m, p2.PurchasePrice);
            Assert.Equal(100, p2.Stock);
            Assert.Equal(UnitType.Piece, p2.Unit);
            Assert.Equal("أغذية", p2.Category.Name);
        }
        finally
        {
            if (File.Exists(tempCsvPath)) File.Delete(tempCsvPath);
        }
    }

    [Fact]
    public async Task ImportFromExcel_WithEnglishHeadersAndMissingCategory_AutoCreatesCategory()
    {
        // Arrange
        var (factory, context) = SetupDb();
        var user = new User { Id = 1, Role = UserRole.Admin };
        var authService = new Mock<IAuthorizationService>();
        var settingsService = new Mock<ISettingsService>();
        settingsService.Setup(s => s.ImportAutoCreateCategories).Returns(true);
        settingsService.Setup(s => s.ImportDuplicateAction).Returns("UpdateStock");

        var vm = new ProductsViewModel(factory, user, authService.Object, settingsService.Object);

        var tempXlsxPath = Path.Combine(Path.GetTempPath(), $"import_test_{Guid.NewGuid():N}.xlsx");
        using (var wb = new XLWorkbook())
        {
            var ws = wb.AddWorksheet("Products");
            // English headers in varied order
            ws.Cell(1, 1).Value = "SKU Code";
            ws.Cell(1, 2).Value = "Item Name";
            ws.Cell(1, 3).Value = "Cost";
            ws.Cell(1, 4).Value = "Retail Price";
            ws.Cell(1, 5).Value = "Stock Qty";
            ws.Cell(1, 6).Value = "Category";

            ws.Cell(2, 1).Value = "ENG-001";
            ws.Cell(2, 2).Value = "Wireless Mouse";
            ws.Cell(2, 3).Value = "$25.00";
            ws.Cell(2, 4).Value = "$45.00";
            ws.Cell(2, 5).Value = "30";
            ws.Cell(2, 6).Value = "Electronics";

            wb.SaveAs(tempXlsxPath);
        }

        try
        {
            // Act
            var result = await vm.ImportFromFileAsync(tempXlsxPath);

            // Assert
            Assert.Equal(1, result.added);
            Assert.Equal(0, result.skipped);

            await using var verifyCtx = await factory.CreateDbContextAsync();
            var p = await verifyCtx.Products.Include(prod => prod.Category).FirstOrDefaultAsync(prod => prod.Barcode == "ENG-001");
            Assert.NotNull(p);
            Assert.Equal("Wireless Mouse", p.Name);
            Assert.Equal(25.00m, p.PurchasePrice);
            Assert.Equal(45.00m, p.SellingPrice);
            Assert.Equal(30, p.Stock);
            Assert.NotNull(p.Category);
            Assert.Equal("Electronics", p.Category.Name);
        }
        finally
        {
            if (File.Exists(tempXlsxPath)) File.Delete(tempXlsxPath);
        }
    }

    [Fact]
    public async Task ImportFromCsv_WhenDuplicateBarcode_UpdatesStockAccordingToSettings()
    {
        // Arrange
        var (factory, context) = SetupDb();
        var cat = new Category { Id = 10, Name = "General", Description = "Default", IsActive = true, CreatedAt = DateTime.Now };
        context.Categories.Add(cat);
        context.Products.Add(new Product
        {
            Barcode = "DUP-999",
            Name = "Original Product",
            PurchasePrice = 10,
            SellingPrice = 20,
            Stock = 15,
            CategoryId = 10,
            IsActive = true,
            CreatedAt = DateTime.Now
        });
        await context.SaveChangesAsync();

        var user = new User { Id = 1, Role = UserRole.Admin };
        var authService = new Mock<IAuthorizationService>();
        var settingsService = new Mock<ISettingsService>();
        settingsService.Setup(s => s.ImportAutoCreateCategories).Returns(true);
        settingsService.Setup(s => s.ImportDuplicateAction).Returns("UpdateStock");

        var vm = new ProductsViewModel(factory, user, authService.Object, settingsService.Object);

        var csvContent = new StringBuilder();
        csvContent.AppendLine("Barcode,Product,Buy,Sell,Stock");
        csvContent.AppendLine("DUP-999,Original Product,12,25,10"); // adding 10 stock, new prices

        var tempCsvPath = Path.Combine(Path.GetTempPath(), $"import_test_{Guid.NewGuid():N}.csv");
        await File.WriteAllTextAsync(tempCsvPath, csvContent.ToString(), Encoding.UTF8);

        try
        {
            // Act
            var result = await vm.ImportFromFileAsync(tempCsvPath);

            // Assert
            Assert.Equal(0, result.added);
            Assert.Equal(1, result.updated);

            await using var verifyCtx = await factory.CreateDbContextAsync();
            var p = await verifyCtx.Products.FirstOrDefaultAsync(prod => prod.Barcode == "DUP-999");
            Assert.NotNull(p);
            Assert.Equal(25, p.Stock); // 15 + 10 = 25
            Assert.Equal(25m, p.SellingPrice);
            Assert.Equal(12m, p.PurchasePrice);
        }
        finally
        {
            if (File.Exists(tempCsvPath)) File.Delete(tempCsvPath);
        }
    }
}
