using Microsoft.EntityFrameworkCore;
using SmartPOS.Core.Entities;
using SmartPOS.Infrastructure.Data;
using System.IO;

// DB path - same as WPF app
string dbPath = @"f:\Raw\kasher\kasher\src\SmartPOS.WPF\bin\Release\net8.0-windows\win-x64\SmartPOS.db";
Console.WriteLine($"🗄️  Database: {dbPath}");

// Delete existing DB so we start from a clean migrated state
if (File.Exists(dbPath)) { File.Delete(dbPath); Console.WriteLine("  🗑️  Old DB deleted, creating fresh..."); }

var options = new DbContextOptionsBuilder<AppDbContext>()
    .UseSqlite($"Data Source={dbPath}")
    .Options;

using var db = new AppDbContext(options);
// Use MigrateAsync so __EFMigrationsHistory is populated correctly
await db.Database.MigrateAsync();

Console.WriteLine("🌱 Seeding database...");
SeedAll(db);
await db.SaveChangesAsync();
Console.WriteLine("\n✅ Done! Database seeded successfully.");

static void SeedAll(AppDbContext db)
{
    SeedCategories(db);
    SeedSuppliers(db);
    SeedUsers(db);
    SeedProducts(db);
    SeedCustomers(db);
    SeedRentalDevices(db);
    SeedPurchaseOrders(db);
    SeedShiftsAndSales(db);
    SeedExpenses(db);
    SeedReturns(db);
}

static void SeedCategories(AppDbContext db)
{
    if (db.Categories.IgnoreQueryFilters().Any()) { Console.WriteLine("  ⏭️  Categories exist"); return; }
    var cats = new[]
    {
        new Category { Name = "مشروبات ساخنة", Description = "قهوة وشاي ومشروبات ساخنة" },
        new Category { Name = "مشروبات باردة", Description = "عصائر ومشروبات غازية" },
        new Category { Name = "وجبات سريعة", Description = "ساندوتشات وبيتزا" },
        new Category { Name = "حلويات وكيك", Description = "كيك وحلويات متنوعة" },
        new Category { Name = "سجائر ومعسل", Description = "سجائر وتبغ" },
        new Category { Name = "إكسسوارات", Description = "إكسسوارات متنوعة" },
    };
    db.Categories.AddRange(cats);
    db.SaveChanges();
    Console.WriteLine($"  ✅ {cats.Length} Categories seeded");
}

static void SeedSuppliers(AppDbContext db)
{
    if (db.Suppliers.IgnoreQueryFilters().Any()) { Console.WriteLine("  ⏭️  Suppliers exist"); return; }
    var suppliers = new[]
    {
        new Supplier { Name = "شركة النيل للمشروبات", ContactPerson = "أحمد حسن", Phone = "01001234567", Email = "nile@drinks.com", DebtAmount = 2500 },
        new Supplier { Name = "مصنع الدلتا للأغذية", ContactPerson = "محمد علي", Phone = "01112345678", Email = "delta@food.com", DebtAmount = 0 },
        new Supplier { Name = "موزع فيليب موريس", ContactPerson = "كريم سامي", Phone = "01223456789", DebtAmount = 750 },
        new Supplier { Name = "شركة الجودة للتوزيع", ContactPerson = "سامي فوزي", Phone = "01334567890", DebtAmount = 0 },
    };
    db.Suppliers.AddRange(suppliers);
    db.SaveChanges();
    Console.WriteLine($"  ✅ {suppliers.Length} Suppliers seeded");
}

static void SeedUsers(AppDbContext db)
{
    if (db.Users.IgnoreQueryFilters().Any()) { Console.WriteLine("  ⏭️  Users exist"); return; }
    var users = new[]
    {
        new User { Username = "superadmin", PasswordHash = "super@2026", FullName = "المدير العام", Role = UserRole.SuperAdmin, IsActive = true, Email = "superadmin@pos.com", DailyMealLimit = 0 },
        new User { Username = "admin", PasswordHash = "admin@2026", FullName = "مدير النظام", Role = UserRole.Admin, IsActive = true, Email = "admin@pos.com", DailyMealLimit = 0 },
        new User { Username = "cashier1", PasswordHash = "cashier@2026", FullName = "أمين الصندوق الأول", Role = UserRole.Cashier, IsActive = true, Phone = "01001111111", DailyMealLimit = 50 },
        new User { Username = "cashier2", PasswordHash = "cashier@2026", FullName = "أمين الصندوق الثاني", Role = UserRole.Cashier, IsActive = true, Phone = "01002222222", DailyMealLimit = 50 },
        new User { Username = "manager", PasswordHash = "manager@2026", FullName = "مدير الفرع", Role = UserRole.Manager, IsActive = true, Email = "manager@pos.com", DailyMealLimit = 100 },
    };
    db.Users.AddRange(users);
    db.SaveChanges();
    Console.WriteLine($"  ✅ {users.Length} Users seeded");
}

static void SeedProducts(AppDbContext db)
{
    if (db.Products.IgnoreQueryFilters().Any()) { Console.WriteLine("  ⏭️  Products exist"); return; }
    var cats = db.Categories.IgnoreQueryFilters().ToList();
    var suppliers = db.Suppliers.IgnoreQueryFilters().ToList();
    int catDrinkHot = cats.First(c => c.Name.Contains("ساخنة")).Id;
    int catDrinkCold = cats.First(c => c.Name.Contains("باردة")).Id;
    int catFood = cats.First(c => c.Name.Contains("وجبات")).Id;
    int catSweet = cats.First(c => c.Name.Contains("حلويات")).Id;
    int catCig = cats.First(c => c.Name.Contains("سجائر")).Id;
    int supId1 = suppliers[0].Id;
    int supId2 = suppliers[1].Id;
    int supId3 = suppliers[2].Id;

    var products = new[]
    {
        // Hot Drinks
        new Product { Barcode="HOT001", Name="قهوة عربية", PurchasePrice=2, SellingPrice=8, Stock=200, MinStockLevel=20, CategoryId=catDrinkHot, SupplierId=supId1, Unit=UnitType.Piece },
        new Product { Barcode="HOT002", Name="إسبريسو", PurchasePrice=3, SellingPrice=12, Stock=150, MinStockLevel=15, CategoryId=catDrinkHot, SupplierId=supId1, Unit=UnitType.Piece },
        new Product { Barcode="HOT003", Name="لاتيه", PurchasePrice=5, SellingPrice=18, Stock=100, MinStockLevel=10, CategoryId=catDrinkHot, SupplierId=supId1, Unit=UnitType.Piece },
        new Product { Barcode="HOT004", Name="شاي أسود", PurchasePrice=1, SellingPrice=5, Stock=300, MinStockLevel=30, CategoryId=catDrinkHot, SupplierId=supId1, Unit=UnitType.Piece },
        new Product { Barcode="HOT005", Name="شاي بالنعناع", PurchasePrice=1.5m, SellingPrice=7, Stock=250, MinStockLevel=25, CategoryId=catDrinkHot, SupplierId=supId1, Unit=UnitType.Piece },
        new Product { Barcode="HOT006", Name="كابتشينو", PurchasePrice=5, SellingPrice=20, Stock=80, MinStockLevel=10, CategoryId=catDrinkHot, SupplierId=supId1, Unit=UnitType.Piece },
        // Cold Drinks
        new Product { Barcode="CLD001", Name="عصير برتقال طازج", PurchasePrice=5, SellingPrice=15, Stock=100, MinStockLevel=20, CategoryId=catDrinkCold, SupplierId=supId1, Unit=UnitType.Piece },
        new Product { Barcode="CLD002", Name="ليمون بالنعناع", PurchasePrice=4, SellingPrice=12, Stock=120, MinStockLevel=15, CategoryId=catDrinkCold, SupplierId=supId1, Unit=UnitType.Piece },
        new Product { Barcode="CLD003", Name="بيبسي 500مل", PurchasePrice=4, SellingPrice=8, Stock=200, MinStockLevel=30, CategoryId=catDrinkCold, SupplierId=supId1, Unit=UnitType.Piece },
        new Product { Barcode="CLD004", Name="ميه مقطرة 1.5L", PurchasePrice=3, SellingPrice=6, Stock=300, MinStockLevel=50, CategoryId=catDrinkCold, SupplierId=supId1, Unit=UnitType.Piece },
        new Product { Barcode="CLD005", Name="ريد بول", PurchasePrice=15, SellingPrice=28, Stock=60, MinStockLevel=10, CategoryId=catDrinkCold, SupplierId=supId1, Unit=UnitType.Piece },
        // Food
        new Product { Barcode="FD001", Name="ساندوتش فراخ", PurchasePrice=15, SellingPrice=35, Stock=50, MinStockLevel=5, CategoryId=catFood, SupplierId=supId2, Unit=UnitType.Piece },
        new Product { Barcode="FD002", Name="ساندوتش كفتة", PurchasePrice=12, SellingPrice=30, Stock=40, MinStockLevel=5, CategoryId=catFood, SupplierId=supId2, Unit=UnitType.Piece },
        new Product { Barcode="FD003", Name="بيتزا مارجريتا", PurchasePrice=20, SellingPrice=55, Stock=30, MinStockLevel=5, CategoryId=catFood, SupplierId=supId2, Unit=UnitType.Piece },
        new Product { Barcode="FD004", Name="بطاطس مقلية", PurchasePrice=5, SellingPrice=15, Stock=80, MinStockLevel=10, CategoryId=catFood, SupplierId=supId2, Unit=UnitType.Piece },
        // Sweets
        new Product { Barcode="SW001", Name="تشيز كيك", PurchasePrice=20, SellingPrice=45, Stock=25, MinStockLevel=5, CategoryId=catSweet, SupplierId=supId2, Unit=UnitType.Piece },
        new Product { Barcode="SW002", Name="كروسان بالجبن", PurchasePrice=8, SellingPrice=20, Stock=40, MinStockLevel=8, CategoryId=catSweet, SupplierId=supId2, Unit=UnitType.Piece },
        new Product { Barcode="SW003", Name="مافن شوكولاتة", PurchasePrice=6, SellingPrice=15, Stock=35, MinStockLevel=5, CategoryId=catSweet, SupplierId=supId2, Unit=UnitType.Piece },
        // Cigs
        new Product { Barcode="CIG001", Name="كليوباترا", PurchasePrice=12, SellingPrice=16, Stock=100, MinStockLevel=20, CategoryId=catCig, SupplierId=supId3, Unit=UnitType.Piece },
        new Product { Barcode="CIG002", Name="مارلبورو", PurchasePrice=28, SellingPrice=38, Stock=80, MinStockLevel=15, CategoryId=catCig, SupplierId=supId3, Unit=UnitType.Piece },
        new Product { Barcode="CIG003", Name="بوكس لاركي", PurchasePrice=18, SellingPrice=24, Stock=90, MinStockLevel=15, CategoryId=catCig, SupplierId=supId3, Unit=UnitType.Piece },
    };
    db.Products.AddRange(products);
    db.SaveChanges();
    Console.WriteLine($"  ✅ {products.Length} Products seeded");
}

static void SeedCustomers(AppDbContext db)
{
    if (db.Customers.IgnoreQueryFilters().Any()) { Console.WriteLine("  ⏭️  Customers exist"); return; }
    var customers = new Customer[]
    {
        new() { Name="أحمد محمد علي", Phone="01001234567", Email="ahmed@mail.com", CreditLimit=500, CurrentDebt=0, Birthdate=new DateTime(1990,3,15) },
        new() { Name="سارة إبراهيم", Phone="01112345678", Email="sara@mail.com", CreditLimit=1000, CurrentDebt=150, Birthdate=new DateTime(1985,7,22) },
        new() { Name="محمد عبدالله", Phone="01223456789", CreditLimit=500, CurrentDebt=0 },
        new() { Name="فاطمة حسن", Phone="01334567890", CreditLimit=800, CurrentDebt=300, Birthdate=new DateTime(1992,1,5) },
        new() { Name="عمر السيد", Phone="01445678901", CreditLimit=500, CurrentDebt=0 },
        new() { Name="منى خالد", Phone="01556789012", Email="mona@mail.com", CreditLimit=2000, CurrentDebt=0, Birthdate=new DateTime(1988,9,18) },
    };
    db.Customers.AddRange(customers);
    db.SaveChanges();
    // Add loyalty
    foreach (var c in customers)
        db.CustomerLoyalties.Add(new CustomerLoyalty { CustomerId = c.Id, Points = Random.Shared.Next(0, 500), TotalPointsEarned = Random.Shared.Next(100, 2000) });
    db.SaveChanges();
    Console.WriteLine($"  ✅ {customers.Length} Customers + Loyalty seeded");
}

static void SeedRentalDevices(AppDbContext db)
{
    if (db.RentalDevices.IgnoreQueryFilters().Any()) { Console.WriteLine("  ⏭️  Rentals exist"); return; }
    var devices = new[]
    {
        new RentalDevice { Name="PS5 - شاشة 1", Type=DeviceType.PlayStation, HourlyRate=40, IsActive=true },
        new RentalDevice { Name="PS5 - شاشة 2", Type=DeviceType.PlayStation, HourlyRate=40, IsActive=true },
        new RentalDevice { Name="PS4 - شاشة 3", Type=DeviceType.PlayStation, HourlyRate=25, IsActive=true },
        new RentalDevice { Name="طاولة بلياردو 1", Type=DeviceType.Billiard, HourlyRate=30, IsActive=true },
        new RentalDevice { Name="طاولة بلياردو 2", Type=DeviceType.Billiard, HourlyRate=30, IsActive=true },
        new RentalDevice { Name="طاولة تنس 1", Type=DeviceType.PingPong, HourlyRate=20, IsActive=true },
    };
    db.RentalDevices.AddRange(devices);
    db.SaveChanges();
    Console.WriteLine($"  ✅ {devices.Length} Rental Devices seeded");
}

static void SeedPurchaseOrders(AppDbContext db)
{
    if (db.PurchaseOrders.IgnoreQueryFilters().Any()) { Console.WriteLine("  ⏭️  PurchaseOrders exist"); return; }
    var suppliers = db.Suppliers.IgnoreQueryFilters().ToList();
    var products = db.Products.IgnoreQueryFilters().ToList();
    var orders = new List<PurchaseOrder>();
    var orderNum = 1;
    foreach (var sup in suppliers)
    {
        var supProds = products.Where(p => p.SupplierId == sup.Id).Take(3).ToList();
        if (!supProds.Any()) continue;
        decimal total = supProds.Sum(p => p.PurchasePrice * 50);
        var po = new PurchaseOrder
        {
            OrderNumber = $"PO-{DateTime.Now:yyyy}-{orderNum++:000}",
            SupplierId = sup.Id,
            OrderDate = DateTime.Now.AddDays(-Random.Shared.Next(5, 60)),
            ReceivedDate = DateTime.Now.AddDays(-Random.Shared.Next(1, 4)),
            TotalAmount = total,
            PaidAmount = total * 0.7m,
            Status = PurchaseOrderStatus.Received,
            OrderDetails = supProds.Select(p => new PurchaseOrderDetail
            {
                ProductId = p.Id,
                Quantity = 50,
                UnitCost = p.PurchasePrice,
                TotalCost = p.PurchasePrice * 50
            }).ToList()
        };
        orders.Add(po);
    }
    db.PurchaseOrders.AddRange(orders);
    db.SaveChanges();
    Console.WriteLine($"  ✅ {orders.Count} Purchase Orders seeded");
}

static void SeedShiftsAndSales(AppDbContext db)
{
    if (db.Shifts.IgnoreQueryFilters().Any()) { Console.WriteLine("  ⏭️  Shifts/Sales exist"); return; }
    var users = db.Users.IgnoreQueryFilters().Where(u => u.Role == UserRole.Cashier).ToList();
    var products = db.Products.IgnoreQueryFilters().ToList();
    var customers = db.Customers.IgnoreQueryFilters().ToList();
    var rng = Random.Shared;
    PaymentMethod[] methods = [PaymentMethod.Cash, PaymentMethod.Card, PaymentMethod.VodafoneCash, PaymentMethod.InstaPay];
    int invoiceNum = 1;
    int totalSales = 0;

    for (int day = 29; day >= 0; day--)
    {
        var date = DateTime.Now.Date.AddDays(-day);
        foreach (var user in users)
        {
            var shift = new Shift
            {
                UserId = user.Id,
                StartTime = date.AddHours(8),
                EndTime = date.AddHours(20),
                OpeningBalance = 500,
                ClosingBalance = 500 + rng.Next(500, 3000),
                Status = ShiftStatus.Closed,
                Notes = $"وردية {date:dd/MM/yyyy}"
            };
            db.Shifts.Add(shift);
            db.SaveChanges();

            int salesCount = rng.Next(5, 20);
            for (int s = 0; s < salesCount; s++)
            {
                var pickedProds = products.OrderBy(_ => rng.Next()).Take(rng.Next(1, 4)).ToList();
                var details = pickedProds.Select(p => new SaleDetail
                {
                    ProductId = p.Id,
                    Quantity = rng.Next(1, 4),
                    UnitPrice = p.SellingPrice,
                    UnitCost = p.PurchasePrice,
                    LineTotal = p.SellingPrice * rng.Next(1, 4)
                }).ToList();
                decimal sub = details.Sum(d => d.LineTotal);
                decimal disc = rng.Next(0, 3) == 0 ? Math.Round(sub * 0.05m, 2) : 0;
                decimal total = sub - disc;
                decimal paid = Math.Ceiling(total / 5) * 5;
                var customer = rng.Next(0, 3) == 0 ? customers[rng.Next(customers.Count)] : null;
                var sale = new Sale
                {
                    InvoiceNumber = $"INV-{date:yyyyMMdd}-{invoiceNum++:0000}",
                    SaleDate = date.AddHours(8 + rng.NextDouble() * 12),
                    Subtotal = sub,
                    DiscountAmount = disc,
                    TaxAmount = 0,
                    TotalAmount = total,
                    AmountPaid = paid,
                    ChangeAmount = paid - total,
                    PaymentMethod = methods[rng.Next(methods.Length)],
                    Status = SaleStatus.Completed,
                    UserId = user.Id,
                    ShiftId = shift.Id,
                    CustomerId = customer?.Id,
                    SaleDetails = details,
                    IsPrinted = true
                };
                db.Sales.Add(sale);
                totalSales++;
            }
            db.SaveChanges();
        }
    }
    Console.WriteLine($"  ✅ {db.Shifts.IgnoreQueryFilters().Count()} Shifts + {totalSales} Sales seeded");
}

static void SeedExpenses(AppDbContext db)
{
    if (db.Expenses.IgnoreQueryFilters().Any()) { Console.WriteLine("  ⏭️  Expenses exist"); return; }
    var adminId = db.Users.IgnoreQueryFilters().First(u => u.Role == UserRole.Admin).Id;
    var rng = Random.Shared;
    ExpenseCategory[] categories = [ExpenseCategory.Rent, ExpenseCategory.Utilities, ExpenseCategory.Salaries,
        ExpenseCategory.Supplies, ExpenseCategory.Maintenance, ExpenseCategory.Marketing];
    string[] descs = ["إيجار الشهر", "فاتورة كهرباء", "رواتب الموظفين", "مستلزمات مكتبية", "صيانة أجهزة", "إعلانات سوشيال ميديا"];
    var expenses = new List<Expense>();
    for (int i = 0; i < 20; i++)
    {
        int idx = rng.Next(categories.Length);
        expenses.Add(new Expense
        {
            Description = descs[idx],
            Amount = rng.Next(100, 5000),
            ExpenseDate = DateTime.Now.AddDays(-rng.Next(0, 30)),
            Category = categories[idx],
            UserId = adminId,
            Notes = $"مصروف دوري #{i + 1}"
        });
    }
    db.Expenses.AddRange(expenses);
    db.SaveChanges();
    Console.WriteLine($"  ✅ {expenses.Count} Expenses seeded");
}

static void SeedReturns(AppDbContext db)
{
    if (db.Returns.IgnoreQueryFilters().Any()) { Console.WriteLine("  ⏭️  Returns exist"); return; }
    var sales = db.Sales.IgnoreQueryFilters().Include(s => s.SaleDetails).Take(3).ToList();
    var adminId = db.Users.IgnoreQueryFilters().First(u => u.Role == UserRole.Admin).Id;
    var customers = db.Customers.IgnoreQueryFilters().ToList();
    int retNum = 1;
    foreach (var sale in sales)
    {
        var firstDetail = sale.SaleDetails.First();
        var ret = new Return
        {
            ReturnNumber = $"RET-{DateTime.Now:yyyyMMdd}-{retNum++:000}",
            SaleId = sale.Id,
            ReturnDate = sale.SaleDate.AddDays(1),
            TotalAmount = firstDetail.UnitPrice,
            Reason = ReturnReason.Defective,
            ProcessedByUserId = adminId,
            CustomerId = customers.First().Id,
            Status = ReturnStatus.Completed,
            ReturnDetails = new List<ReturnDetail>
            {
                new() { ProductId = firstDetail.ProductId, Quantity = 1, UnitPrice = firstDetail.UnitPrice, Subtotal = firstDetail.UnitPrice }
            }
        };
        db.Returns.Add(ret);
    }
    db.SaveChanges();
    Console.WriteLine($"  ✅ {sales.Count} Returns seeded");
}

