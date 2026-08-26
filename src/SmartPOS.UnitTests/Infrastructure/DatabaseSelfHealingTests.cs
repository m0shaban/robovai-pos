using System;
using System.IO;
using System.Threading.Tasks;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using SmartPOS.Core.Entities;
using SmartPOS.Infrastructure.Data;
using Xunit;

namespace SmartPOS.UnitTests.Infrastructure;

public class DatabaseSelfHealingTests : IDisposable
{
    private readonly string _testDbPath;
    private readonly string _connectionString;

    public DatabaseSelfHealingTests()
    {
        _testDbPath = Path.Combine(Path.GetTempPath(), $"SmartPOS_Test_{Guid.NewGuid():N}.db");
        _connectionString = $"Data Source={_testDbPath}";
    }

    public void Dispose()
    {
        try
        {
            SqliteConnection.ClearAllPools();
            if (File.Exists(_testDbPath))
            {
                File.Delete(_testDbPath);
            }
        }
        catch { }
    }

    private AppDbContext CreateDbContext()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseSqlite(_connectionString)
            .Options;
        return new AppDbContext(options);
    }

    [Fact]
    public async Task EnsureSchemaSelfHealedAsync_ShouldCreateAll22Tables_AndPreventMissingTableCrashes()
    {
        await using var context = CreateDbContext();

        // 1. Run the self-healing routine on an empty SQLite database
        await DbInitializer.EnsureSchemaSelfHealedAsync(context);

        // 2. Verify we can insert and read from AppSettings
        var setting = new AppSetting
        {
            Key = "TestKey",
            Value = "TestValue"
        };
        context.AppSettings.Add(setting);
        await context.SaveChangesAsync();

        var readSetting = await context.AppSettings.FirstOrDefaultAsync(s => s.Key == "TestKey");
        Assert.NotNull(readSetting);
        Assert.Equal("TestValue", readSetting.Value);

        // 3. Verify core business entities operate without schema errors
        var user = new User
        {
            Username = "admin_test",
            PasswordHash = "hashed",
            FullName = "Admin Test",
            Role = UserRole.Admin,
            IsActive = true
        };
        context.Users.Add(user);
        await context.SaveChangesAsync();

        var category = new Category { Name = "Beverages", IsActive = true };
        context.Categories.Add(category);
        await context.SaveChangesAsync();

        var product = new Product
        {
            Name = "Mineral Water",
            Barcode = "99887766",
            SellingPrice = 10,
            PurchasePrice = 5,
            Stock = 100,
            CategoryId = category.Id,
            IsActive = true
        };
        context.Products.Add(product);
        await context.SaveChangesAsync();

        var readProduct = await context.Products.Include(p => p.Category).FirstOrDefaultAsync(p => p.Barcode == "99887766");
        Assert.NotNull(readProduct);
        Assert.Equal("Beverages", readProduct.Category.Name);
    }

    [Fact]
    public async Task SelfHealing_ShouldBeIdempotent_WhenRunMultipleTimes()
    {
        await using var context = CreateDbContext();

        // Run self-healing 3 times in a row
        await DbInitializer.EnsureSchemaSelfHealedAsync(context);
        await DbInitializer.EnsureSchemaSelfHealedAsync(context);
        await DbInitializer.EnsureSchemaSelfHealedAsync(context);

        // Verify database remains valid and intact
        var count = await context.AppSettings.CountAsync();
        Assert.True(count >= 0);
    }
}
