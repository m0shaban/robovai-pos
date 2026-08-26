using Microsoft.EntityFrameworkCore;
using SmartPOS.Core.Entities;
using SmartPOS.Infrastructure.Data;

namespace SmartPOS.UnitTests.Infrastructure;

public class AppDbContextQueryFilterTests
{
    private static AppDbContext GetInMemoryDbContext()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;
        return new AppDbContext(options);
    }

    [Fact]
    public async Task GlobalSoftDeleteFilter_HidesDeletedRowsByDefault()
    {
        using var context = GetInMemoryDbContext();
        context.Categories.AddRange(
            new Category { Id = 1, Name = "Visible", IsDeleted = false },
            new Category { Id = 2, Name = "Hidden", IsDeleted = true });
        await context.SaveChangesAsync();

        var visible = await context.Categories.ToListAsync();
        var allRows = await context.Categories.IgnoreQueryFilters().ToListAsync();

        Assert.Single(visible);
        Assert.Equal(2, allRows.Count);
        Assert.Equal("Visible", visible[0].Name);
    }
}
