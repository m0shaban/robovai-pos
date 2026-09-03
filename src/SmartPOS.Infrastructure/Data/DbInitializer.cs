using Microsoft.EntityFrameworkCore;
using SmartPOS.Core.Entities;
using System;
using System.IO;
using System.Threading.Tasks;

namespace SmartPOS.Infrastructure.Data;

/// <summary>
/// Handles runtime data seeding after database schema is created.
/// Separated from OnModelCreating to avoid migration dependency issues.
/// Called once at application startup.
/// </summary>
public static class DbInitializer
{
    public static async Task InitializeAsync(AppDbContext context)
    {
        // 1. Configure SQLite PRAGMA settings first for concurrency and timeout
        try
        {
            await context.Database.ExecuteSqlRawAsync("PRAGMA journal_mode=WAL;");
            await context.Database.ExecuteSqlRawAsync("PRAGMA busy_timeout=30000;");
            await context.Database.ExecuteSqlRawAsync("PRAGMA synchronous=NORMAL;");
            await context.Database.ExecuteSqlRawAsync("PRAGMA temp_store=MEMORY;");
            await context.Database.ExecuteSqlRawAsync("PRAGMA foreign_keys=ON;");
        }
        catch { /* ignore pragma errors on memory/closed conns */ }

        // 2. Try standard EF Core Migrations
        try
        {
            await context.Database.MigrateAsync();
        }
        catch
        {
            // Migrations might fail on legacy or partially created databases.
            // Do not wipe user data! Proceed to self-healing repair below.
        }

        // 3. Guaranteed Self-Healing DDL: Ensure EVERY table and missing column exists
        await EnsureSchemaSelfHealedAsync(context);

        // 4. Seed required system data
        var now = DateTime.Now;
        await SeedUsersAsync(context, now);
        
        if (!await context.Customers.AnyAsync())
        {
            context.Customers.Add(new Customer { Name = "عميل نقدي", Phone = "--", Address = "--", IsActive = true, CreatedAt = now });
            await context.SaveChangesAsync();
        }
    }

    /// <summary>
    /// Bulletproof self-healing schema engine.
    /// Creates any missing tables and missing columns on older databases without data loss.
    /// </summary>
    public static async Task EnsureSchemaSelfHealedAsync(AppDbContext context)
    {
        var tables = new[]
        {
            @"CREATE TABLE IF NOT EXISTS ""AppSettings"" (
                ""Key"" TEXT NOT NULL CONSTRAINT ""PK_AppSettings"" PRIMARY KEY,
                ""Value"" TEXT NOT NULL
            );",

            @"CREATE TABLE IF NOT EXISTS ""Categories"" (
                ""Id"" INTEGER NOT NULL CONSTRAINT ""PK_Categories"" PRIMARY KEY AUTOINCREMENT,
                ""Name"" TEXT NOT NULL,
                ""Description"" TEXT NULL,
                ""IconName"" TEXT NULL,
                ""ColorCode"" TEXT NULL,
                ""IsActive"" INTEGER NOT NULL DEFAULT 1,
                ""CreatedAt"" TEXT NOT NULL,
                ""UpdatedAt"" TEXT NULL,
                ""DeletedAt"" TEXT NULL,
                ""IsDeleted"" INTEGER NOT NULL DEFAULT 0
            );",

            @"CREATE TABLE IF NOT EXISTS ""Suppliers"" (
                ""Id"" INTEGER NOT NULL CONSTRAINT ""PK_Suppliers"" PRIMARY KEY AUTOINCREMENT,
                ""Name"" TEXT NOT NULL,
                ""Phone"" TEXT NULL,
                ""Email"" TEXT NULL,
                ""Address"" TEXT NULL,
                ""DebtAmount"" TEXT NOT NULL DEFAULT '0',
                ""IsActive"" INTEGER NOT NULL DEFAULT 1,
                ""CreatedAt"" TEXT NOT NULL,
                ""UpdatedAt"" TEXT NULL,
                ""DeletedAt"" TEXT NULL,
                ""IsDeleted"" INTEGER NOT NULL DEFAULT 0
            );",

            @"CREATE TABLE IF NOT EXISTS ""Customers"" (
                ""Id"" INTEGER NOT NULL CONSTRAINT ""PK_Customers"" PRIMARY KEY AUTOINCREMENT,
                ""Name"" TEXT NOT NULL,
                ""Phone"" TEXT NULL,
                ""Email"" TEXT NULL,
                ""Address"" TEXT NULL,
                ""CreditLimit"" TEXT NOT NULL DEFAULT '0',
                ""CurrentDebt"" TEXT NOT NULL DEFAULT '0',
                ""Birthdate"" TEXT NULL,
                ""Notes"" TEXT NULL,
                ""IsActive"" INTEGER NOT NULL DEFAULT 1,
                ""CreatedAt"" TEXT NOT NULL,
                ""UpdatedAt"" TEXT NULL,
                ""DeletedAt"" TEXT NULL,
                ""IsDeleted"" INTEGER NOT NULL DEFAULT 0
            );",

            @"CREATE TABLE IF NOT EXISTS ""Users"" (
                ""Id"" INTEGER NOT NULL CONSTRAINT ""PK_Users"" PRIMARY KEY AUTOINCREMENT,
                ""Username"" TEXT NOT NULL,
                ""PasswordHash"" TEXT NOT NULL,
                ""FullName"" TEXT NOT NULL,
                ""Role"" INTEGER NOT NULL,
                ""Permissions"" INTEGER NOT NULL DEFAULT 0,
                ""AdminPin"" TEXT NULL,
                ""Phone"" TEXT NULL,
                ""Email"" TEXT NULL,
                ""LastLogin"" TEXT NULL,
                ""DailyMealLimit"" TEXT NOT NULL DEFAULT '0',
                ""IsActive"" INTEGER NOT NULL DEFAULT 1,
                ""CreatedAt"" TEXT NOT NULL,
                ""UpdatedAt"" TEXT NULL,
                ""DeletedAt"" TEXT NULL,
                ""IsDeleted"" INTEGER NOT NULL DEFAULT 0
            );",

            @"CREATE TABLE IF NOT EXISTS ""Products"" (
                ""Id"" INTEGER NOT NULL CONSTRAINT ""PK_Products"" PRIMARY KEY AUTOINCREMENT,
                ""Barcode"" TEXT NOT NULL,
                ""Name"" TEXT NOT NULL,
                ""Description"" TEXT NULL,
                ""CategoryId"" INTEGER NOT NULL,
                ""SupplierId"" INTEGER NULL,
                ""PurchasePrice"" TEXT NOT NULL DEFAULT '0',
                ""SellingPrice"" TEXT NOT NULL DEFAULT '0',
                ""Stock"" INTEGER NOT NULL DEFAULT 0,
                ""MinStockLevel"" INTEGER NOT NULL DEFAULT 0,
                ""Unit"" INTEGER NOT NULL DEFAULT 0,
                ""ImagePath"" TEXT NULL,
                ""ExpiryDate"" TEXT NULL,
                ""StaffMealEligible"" INTEGER NOT NULL DEFAULT 0,
                ""IsActive"" INTEGER NOT NULL DEFAULT 1,
                ""CreatedAt"" TEXT NOT NULL,
                ""UpdatedAt"" TEXT NULL,
                ""DeletedAt"" TEXT NULL,
                ""IsDeleted"" INTEGER NOT NULL DEFAULT 0,
                CONSTRAINT ""FK_Products_Categories_CategoryId"" FOREIGN KEY (""CategoryId"") REFERENCES ""Categories"" (""Id"") ON DELETE RESTRICT,
                CONSTRAINT ""FK_Products_Suppliers_SupplierId"" FOREIGN KEY (""SupplierId"") REFERENCES ""Suppliers"" (""Id"") ON DELETE SET NULL
            );",

            @"CREATE TABLE IF NOT EXISTS ""Sales"" (
                ""Id"" INTEGER NOT NULL CONSTRAINT ""PK_Sales"" PRIMARY KEY AUTOINCREMENT,
                ""InvoiceNumber"" TEXT NOT NULL,
                ""CustomerId"" INTEGER NULL,
                ""UserId"" INTEGER NOT NULL,
                ""ShiftId"" INTEGER NULL,
                ""TotalAmount"" TEXT NOT NULL DEFAULT '0',
                ""Subtotal"" TEXT NOT NULL DEFAULT '0',
                ""DiscountAmount"" TEXT NOT NULL DEFAULT '0',
                ""DiscountPercentage"" TEXT NOT NULL DEFAULT '0',
                ""TaxAmount"" TEXT NOT NULL DEFAULT '0',
                ""FinalAmount"" TEXT NOT NULL DEFAULT '0',
                ""AmountPaid"" TEXT NOT NULL DEFAULT '0',
                ""ChangeAmount"" TEXT NOT NULL DEFAULT '0',
                ""PaymentMethod"" INTEGER NOT NULL DEFAULT 0,
                ""Status"" INTEGER NOT NULL DEFAULT 0,
                ""Notes"" TEXT NULL,
                ""TableNumber"" TEXT NULL,
                ""TableId"" INTEGER NULL,
                ""ConsumedByUserId"" INTEGER NULL,
                ""LoyaltyPointsEarned"" INTEGER NULL,
                ""LoyaltyPointsRedeemed"" INTEGER NULL,
                ""QRCode"" TEXT NULL,
                ""IsPrinted"" INTEGER NOT NULL DEFAULT 0,
                ""OrderType"" INTEGER NOT NULL DEFAULT 0,
                ""SaleDate"" TEXT NOT NULL,
                ""CreatedAt"" TEXT NOT NULL,
                ""UpdatedAt"" TEXT NULL,
                ""DeletedAt"" TEXT NULL,
                ""IsDeleted"" INTEGER NOT NULL DEFAULT 0,
                CONSTRAINT ""FK_Sales_Customers_CustomerId"" FOREIGN KEY (""CustomerId"") REFERENCES ""Customers"" (""Id"") ON DELETE SET NULL,
                CONSTRAINT ""FK_Sales_Users_UserId"" FOREIGN KEY (""UserId"") REFERENCES ""Users"" (""Id"") ON DELETE RESTRICT
            );",

            @"CREATE TABLE IF NOT EXISTS ""SaleDetails"" (
                ""Id"" INTEGER NOT NULL CONSTRAINT ""PK_SaleDetails"" PRIMARY KEY AUTOINCREMENT,
                ""SaleId"" INTEGER NOT NULL,
                ""ProductId"" INTEGER NOT NULL,
                ""Quantity"" INTEGER NOT NULL DEFAULT 1,
                ""UnitPrice"" TEXT NOT NULL DEFAULT '0',
                ""UnitCost"" TEXT NOT NULL DEFAULT '0',
                ""DiscountPercentage"" TEXT NOT NULL DEFAULT '0',
                ""DiscountAmount"" TEXT NOT NULL DEFAULT '0',
                ""LineTotal"" TEXT NOT NULL DEFAULT '0',
                ""CreatedAt"" TEXT NOT NULL,
                ""UpdatedAt"" TEXT NULL,
                ""DeletedAt"" TEXT NULL,
                ""IsDeleted"" INTEGER NOT NULL DEFAULT 0,
                CONSTRAINT ""FK_SaleDetails_Products_ProductId"" FOREIGN KEY (""ProductId"") REFERENCES ""Products"" (""Id"") ON DELETE RESTRICT,
                CONSTRAINT ""FK_SaleDetails_Sales_SaleId"" FOREIGN KEY (""SaleId"") REFERENCES ""Sales"" (""Id"") ON DELETE CASCADE
            );",

            @"CREATE TABLE IF NOT EXISTS ""Expenses"" (
                ""Id"" INTEGER NOT NULL CONSTRAINT ""PK_Expenses"" PRIMARY KEY AUTOINCREMENT,
                ""Description"" TEXT NOT NULL,
                ""Amount"" TEXT NOT NULL DEFAULT '0',
                ""Category"" INTEGER NOT NULL DEFAULT 0,
                ""ExpenseDate"" TEXT NOT NULL,
                ""UserId"" INTEGER NOT NULL,
                ""Receipt"" TEXT NULL,
                ""Notes"" TEXT NULL,
                ""CreatedAt"" TEXT NOT NULL,
                ""UpdatedAt"" TEXT NULL,
                ""DeletedAt"" TEXT NULL,
                ""IsDeleted"" INTEGER NOT NULL DEFAULT 0,
                CONSTRAINT ""FK_Expenses_Users_UserId"" FOREIGN KEY (""UserId"") REFERENCES ""Users"" (""Id"") ON DELETE RESTRICT
            );",

            @"CREATE TABLE IF NOT EXISTS ""PurchaseOrders"" (
                ""Id"" INTEGER NOT NULL CONSTRAINT ""PK_PurchaseOrders"" PRIMARY KEY AUTOINCREMENT,
                ""OrderNumber"" TEXT NOT NULL,
                ""SupplierId"" INTEGER NOT NULL,
                ""UserId"" INTEGER NOT NULL,
                ""TotalAmount"" TEXT NOT NULL DEFAULT '0',
                ""PaidAmount"" TEXT NOT NULL DEFAULT '0',
                ""RemainingAmount"" TEXT NOT NULL DEFAULT '0',
                ""Status"" INTEGER NOT NULL DEFAULT 0,
                ""Notes"" TEXT NULL,
                ""OrderDate"" TEXT NOT NULL,
                ""DeliveryDate"" TEXT NULL,
                ""CreatedAt"" TEXT NOT NULL,
                ""UpdatedAt"" TEXT NULL,
                ""DeletedAt"" TEXT NULL,
                ""IsDeleted"" INTEGER NOT NULL DEFAULT 0,
                CONSTRAINT ""FK_PurchaseOrders_Suppliers_SupplierId"" FOREIGN KEY (""SupplierId"") REFERENCES ""Suppliers"" (""Id"") ON DELETE RESTRICT,
                CONSTRAINT ""FK_PurchaseOrders_Users_UserId"" FOREIGN KEY (""UserId"") REFERENCES ""Users"" (""Id"") ON DELETE RESTRICT
            );",

            @"CREATE TABLE IF NOT EXISTS ""PurchaseOrderDetails"" (
                ""Id"" INTEGER NOT NULL CONSTRAINT ""PK_PurchaseOrderDetails"" PRIMARY KEY AUTOINCREMENT,
                ""PurchaseOrderId"" INTEGER NOT NULL,
                ""ProductId"" INTEGER NOT NULL,
                ""Quantity"" INTEGER NOT NULL DEFAULT 1,
                ""UnitPrice"" TEXT NOT NULL DEFAULT '0',
                ""Total"" TEXT NOT NULL DEFAULT '0',
                ""CreatedAt"" TEXT NOT NULL,
                ""UpdatedAt"" TEXT NULL,
                ""DeletedAt"" TEXT NULL,
                ""IsDeleted"" INTEGER NOT NULL DEFAULT 0,
                CONSTRAINT ""FK_PurchaseOrderDetails_Products_ProductId"" FOREIGN KEY (""ProductId"") REFERENCES ""Products"" (""Id"") ON DELETE RESTRICT,
                CONSTRAINT ""FK_PurchaseOrderDetails_PurchaseOrders_PurchaseOrderId"" FOREIGN KEY (""PurchaseOrderId"") REFERENCES ""PurchaseOrders"" (""Id"") ON DELETE CASCADE
            );",

            @"CREATE TABLE IF NOT EXISTS ""SupplierPayments"" (
                ""Id"" INTEGER NOT NULL CONSTRAINT ""PK_SupplierPayments"" PRIMARY KEY AUTOINCREMENT,
                ""SupplierId"" INTEGER NOT NULL,
                ""PurchaseOrderId"" INTEGER NULL,
                ""Amount"" TEXT NOT NULL DEFAULT '0',
                ""PaymentDate"" TEXT NOT NULL,
                ""PaymentMethod"" INTEGER NOT NULL DEFAULT 0,
                ""Notes"" TEXT NULL,
                ""CreatedAt"" TEXT NOT NULL,
                ""UpdatedAt"" TEXT NULL,
                ""DeletedAt"" TEXT NULL,
                ""IsDeleted"" INTEGER NOT NULL DEFAULT 0,
                CONSTRAINT ""FK_SupplierPayments_PurchaseOrders_PurchaseOrderId"" FOREIGN KEY (""PurchaseOrderId"") REFERENCES ""PurchaseOrders"" (""Id"") ON DELETE SET NULL,
                CONSTRAINT ""FK_SupplierPayments_Suppliers_SupplierId"" FOREIGN KEY (""SupplierId"") REFERENCES ""Suppliers"" (""Id"") ON DELETE RESTRICT
            );",

            @"CREATE TABLE IF NOT EXISTS ""StockMovements"" (
                ""Id"" INTEGER NOT NULL CONSTRAINT ""PK_StockMovements"" PRIMARY KEY AUTOINCREMENT,
                ""ProductId"" INTEGER NOT NULL,
                ""Quantity"" INTEGER NOT NULL DEFAULT 0,
                ""Type"" INTEGER NOT NULL DEFAULT 0,
                ""Notes"" TEXT NULL,
                ""MovementDate"" TEXT NOT NULL,
                ""CreatedAt"" TEXT NOT NULL,
                ""UpdatedAt"" TEXT NULL,
                ""DeletedAt"" TEXT NULL,
                ""IsDeleted"" INTEGER NOT NULL DEFAULT 0,
                CONSTRAINT ""FK_StockMovements_Products_ProductId"" FOREIGN KEY (""ProductId"") REFERENCES ""Products"" (""Id"") ON DELETE CASCADE
            );",

            @"CREATE TABLE IF NOT EXISTS ""Shifts"" (
                ""Id"" INTEGER NOT NULL CONSTRAINT ""PK_Shifts"" PRIMARY KEY AUTOINCREMENT,
                ""UserId"" INTEGER NOT NULL,
                ""CashierName"" TEXT NOT NULL,
                ""StartTime"" TEXT NOT NULL,
                ""EndTime"" TEXT NULL,
                ""StartingCash"" TEXT NOT NULL DEFAULT '0',
                ""EndingCash"" TEXT NULL,
                ""ActualCash"" TEXT NULL,
                ""Difference"" TEXT NULL,
                ""TotalSales"" TEXT NOT NULL DEFAULT '0',
                ""TotalExpenses"" TEXT NOT NULL DEFAULT '0',
                ""Status"" INTEGER NOT NULL DEFAULT 0,
                ""Notes"" TEXT NULL,
                ""CreatedAt"" TEXT NOT NULL,
                ""UpdatedAt"" TEXT NULL,
                ""DeletedAt"" TEXT NULL,
                ""IsDeleted"" INTEGER NOT NULL DEFAULT 0
            );",

            @"CREATE TABLE IF NOT EXISTS ""CustomerLoyalties"" (
                ""Id"" INTEGER NOT NULL CONSTRAINT ""PK_CustomerLoyalties"" PRIMARY KEY AUTOINCREMENT,
                ""CustomerId"" INTEGER NOT NULL,
                ""PointsBalance"" INTEGER NOT NULL DEFAULT 0,
                ""TotalPointsEarned"" INTEGER NOT NULL DEFAULT 0,
                ""TotalPointsRedeemed"" INTEGER NOT NULL DEFAULT 0,
                ""Tier"" INTEGER NOT NULL DEFAULT 0,
                ""TierDiscountPercentage"" TEXT NOT NULL DEFAULT '0',
                ""CreatedAt"" TEXT NOT NULL,
                ""UpdatedAt"" TEXT NULL,
                ""DeletedAt"" TEXT NULL,
                ""IsDeleted"" INTEGER NOT NULL DEFAULT 0,
                CONSTRAINT ""FK_CustomerLoyalties_Customers_CustomerId"" FOREIGN KEY (""CustomerId"") REFERENCES ""Customers"" (""Id"") ON DELETE CASCADE
            );",

            @"CREATE TABLE IF NOT EXISTS ""LoyaltyTransactions"" (
                ""Id"" INTEGER NOT NULL CONSTRAINT ""PK_LoyaltyTransactions"" PRIMARY KEY AUTOINCREMENT,
                ""CustomerLoyaltyId"" INTEGER NOT NULL,
                ""SaleId"" INTEGER NULL,
                ""Type"" INTEGER NOT NULL DEFAULT 0,
                ""Points"" INTEGER NOT NULL DEFAULT 0,
                ""PointsBalanceAfter"" INTEGER NOT NULL DEFAULT 0,
                ""Notes"" TEXT NULL,
                ""TransactionDate"" TEXT NOT NULL,
                ""CreatedAt"" TEXT NOT NULL,
                ""UpdatedAt"" TEXT NULL,
                ""DeletedAt"" TEXT NULL,
                ""IsDeleted"" INTEGER NOT NULL DEFAULT 0,
                CONSTRAINT ""FK_LoyaltyTransactions_CustomerLoyalties_CustomerLoyaltyId"" FOREIGN KEY (""CustomerLoyaltyId"") REFERENCES ""CustomerLoyalties"" (""Id"") ON DELETE CASCADE
            );",

            @"CREATE TABLE IF NOT EXISTS ""Returns"" (
                ""Id"" INTEGER NOT NULL CONSTRAINT ""PK_Returns"" PRIMARY KEY AUTOINCREMENT,
                ""ReturnNumber"" TEXT NOT NULL,
                ""SaleId"" INTEGER NULL,
                ""InvoiceNumber"" TEXT NOT NULL,
                ""UserId"" INTEGER NOT NULL,
                ""ShiftId"" INTEGER NULL,
                ""CustomerId"" INTEGER NULL,
                ""SubTotal"" TEXT NOT NULL DEFAULT '0',
                ""TaxRefund"" TEXT NOT NULL DEFAULT '0',
                ""TotalRefund"" TEXT NOT NULL DEFAULT '0',
                ""PaymentMethod"" INTEGER NOT NULL DEFAULT 0,
                ""Status"" INTEGER NOT NULL DEFAULT 0,
                ""Reason"" TEXT NULL,
                ""AdminNotes"" TEXT NULL,
                ""ApprovedByUserId"" INTEGER NULL,
                ""ApprovedAt"" TEXT NULL,
                ""ReturnDate"" TEXT NOT NULL,
                ""CreatedAt"" TEXT NOT NULL,
                ""UpdatedAt"" TEXT NULL,
                ""DeletedAt"" TEXT NULL,
                ""IsDeleted"" INTEGER NOT NULL DEFAULT 0
            );",

            @"CREATE TABLE IF NOT EXISTS ""ReturnDetails"" (
                ""Id"" INTEGER NOT NULL CONSTRAINT ""PK_ReturnDetails"" PRIMARY KEY AUTOINCREMENT,
                ""ReturnId"" INTEGER NOT NULL,
                ""ProductId"" INTEGER NOT NULL,
                ""ProductName"" TEXT NOT NULL,
                ""Barcode"" TEXT NOT NULL,
                ""OriginalQuantity"" INTEGER NOT NULL DEFAULT 1,
                ""ReturnQuantity"" INTEGER NOT NULL DEFAULT 1,
                ""UnitPrice"" TEXT NOT NULL DEFAULT '0',
                ""TotalRefund"" TEXT NOT NULL DEFAULT '0',
                ""Condition"" INTEGER NOT NULL DEFAULT 0,
                ""RestockAction"" INTEGER NOT NULL DEFAULT 0,
                ""CreatedAt"" TEXT NOT NULL,
                ""UpdatedAt"" TEXT NULL,
                ""DeletedAt"" TEXT NULL,
                ""IsDeleted"" INTEGER NOT NULL DEFAULT 0,
                CONSTRAINT ""FK_ReturnDetails_Returns_ReturnId"" FOREIGN KEY (""ReturnId"") REFERENCES ""Returns"" (""Id"") ON DELETE CASCADE
            );",

            @"CREATE TABLE IF NOT EXISTS ""AuditLogs"" (
                ""Id"" INTEGER NOT NULL CONSTRAINT ""PK_AuditLogs"" PRIMARY KEY AUTOINCREMENT,
                ""UserId"" INTEGER NOT NULL,
                ""Username"" TEXT NOT NULL,
                ""Action"" TEXT NOT NULL,
                ""EntityType"" TEXT NOT NULL,
                ""EntityId"" TEXT NULL,
                ""OldValues"" TEXT NULL,
                ""NewValues"" TEXT NULL,
                ""IpAddress"" TEXT NULL,
                ""Severity"" INTEGER NOT NULL DEFAULT 0,
                ""Timestamp"" TEXT NOT NULL,
                ""CreatedAt"" TEXT NOT NULL,
                ""UpdatedAt"" TEXT NULL,
                ""DeletedAt"" TEXT NULL,
                ""IsDeleted"" INTEGER NOT NULL DEFAULT 0
            );",

            @"CREATE TABLE IF NOT EXISTS ""RentalDevices"" (
                ""Id"" INTEGER NOT NULL CONSTRAINT ""PK_RentalDevices"" PRIMARY KEY AUTOINCREMENT,
                ""DeviceName"" TEXT NOT NULL,
                ""DeviceType"" TEXT NULL,
                ""DeviceNumber"" INTEGER NOT NULL DEFAULT 0,
                ""HourlyRate"" TEXT NOT NULL DEFAULT '0',
                ""Status"" INTEGER NOT NULL DEFAULT 0,
                ""CurrentSessionId"" INTEGER NULL,
                ""TotalSessionsCount"" INTEGER NOT NULL DEFAULT 0,
                ""TotalRevenue"" TEXT NOT NULL DEFAULT '0',
                ""Notes"" TEXT NULL,
                ""CreatedAt"" TEXT NOT NULL,
                ""UpdatedAt"" TEXT NULL,
                ""DeletedAt"" TEXT NULL,
                ""IsDeleted"" INTEGER NOT NULL DEFAULT 0
            );",

            @"CREATE TABLE IF NOT EXISTS ""RentalSessions"" (
                ""Id"" INTEGER NOT NULL CONSTRAINT ""PK_RentalSessions"" PRIMARY KEY AUTOINCREMENT,
                ""RentalDeviceId"" INTEGER NOT NULL,
                ""CustomerName"" TEXT NULL,
                ""CustomerPhone"" TEXT NULL,
                ""StartTime"" TEXT NOT NULL,
                ""EndTime"" TEXT NULL,
                ""DurationMinutes"" INTEGER NOT NULL DEFAULT 0,
                ""HourlyRate"" TEXT NOT NULL DEFAULT '0',
                ""TotalAmount"" TEXT NOT NULL DEFAULT '0',
                ""PaidAmount"" TEXT NOT NULL DEFAULT '0',
                ""PaymentMethod"" INTEGER NOT NULL DEFAULT 0,
                ""Status"" INTEGER NOT NULL DEFAULT 0,
                ""Notes"" TEXT NULL,
                ""CreatedAt"" TEXT NOT NULL,
                ""UpdatedAt"" TEXT NULL,
                ""DeletedAt"" TEXT NULL,
                ""IsDeleted"" INTEGER NOT NULL DEFAULT 0,
                CONSTRAINT ""FK_RentalSessions_RentalDevices_RentalDeviceId"" FOREIGN KEY (""RentalDeviceId"") REFERENCES ""RentalDevices"" (""Id"") ON DELETE RESTRICT
            );",

            @"CREATE TABLE IF NOT EXISTS ""SyncOutboxes"" (
                ""Id"" INTEGER NOT NULL CONSTRAINT ""PK_SyncOutboxes"" PRIMARY KEY AUTOINCREMENT,
                ""EntityType"" TEXT NOT NULL,
                ""EntityId"" TEXT NOT NULL,
                ""Operation"" TEXT NOT NULL,
                ""Payload"" TEXT NOT NULL,
                ""Status"" INTEGER NOT NULL DEFAULT 0,
                ""Attempts"" INTEGER NOT NULL DEFAULT 0,
                ""LastError"" TEXT NULL,
                ""CreatedAt"" TEXT NOT NULL,
                ""ProcessedAt"" TEXT NULL
            );"
        };

        foreach (var sql in tables)
        {
            try { await context.Database.ExecuteSqlRawAsync(sql); } catch { /* ignore if already exists */ }
        }

        // Auto-heal missing columns across older database schemas
        var columnPatches = new[]
        {
            ("Users", "Permissions", "INTEGER NOT NULL DEFAULT 0"),
            ("Users", "AdminPin", "TEXT NULL"),
            ("Users", "Phone", "TEXT NULL"),
            ("Users", "Email", "TEXT NULL"),
            ("Users", "LastLogin", "TEXT NULL"),
            ("Users", "DailyMealLimit", "TEXT NOT NULL DEFAULT '0'"),
            ("Products", "ImagePath", "TEXT NULL"),
            ("Products", "Description", "TEXT NULL"),
            ("Products", "ExpiryDate", "TEXT NULL"),
            ("Products", "StaffMealEligible", "INTEGER NOT NULL DEFAULT 0"),
            ("Expenses", "Receipt", "TEXT NULL"),
            ("Expenses", "Notes", "TEXT NULL"),
            ("Sales", "TableNumber", "TEXT NULL"),
            ("Sales", "OrderType", "INTEGER NOT NULL DEFAULT 0"),
            ("Categories", "ColorCode", "TEXT NULL"),
            ("Categories", "IconName", "TEXT NULL"),
            ("Categories", "Description", "TEXT NULL"),
            ("Suppliers", "DebtAmount", "TEXT NOT NULL DEFAULT '0'"),
            ("Customers", "CreditLimit", "TEXT NOT NULL DEFAULT '0'"),
            ("Customers", "CurrentDebt", "TEXT NOT NULL DEFAULT '0'"),
            ("Customers", "Birthdate", "TEXT NULL"),
            ("Customers", "Notes", "TEXT NULL"),
            ("Sales", "Subtotal", "TEXT NOT NULL DEFAULT '0'"),
            ("Sales", "DiscountPercentage", "TEXT NOT NULL DEFAULT '0'"),
            ("Sales", "AmountPaid", "TEXT NOT NULL DEFAULT '0'"),
            ("Sales", "ChangeAmount", "TEXT NOT NULL DEFAULT '0'"),
            ("Sales", "TableId", "INTEGER NULL"),
            ("Sales", "ConsumedByUserId", "INTEGER NULL"),
            ("Sales", "LoyaltyPointsEarned", "INTEGER NULL"),
            ("Sales", "LoyaltyPointsRedeemed", "INTEGER NULL"),
            ("Sales", "QRCode", "TEXT NULL"),
            ("Sales", "IsPrinted", "INTEGER NOT NULL DEFAULT 0"),
            ("Returns", "ShiftId", "INTEGER NULL"),
            ("Returns", "CustomerId", "INTEGER NULL")
        };

        foreach (var (table, column, def) in columnPatches)
        {
            try
            {
                await context.Database.ExecuteSqlRawAsync($"ALTER TABLE \"{table}\" ADD COLUMN \"{column}\" {def};");
            }
            catch
            {
                // Column already exists or table not ready, safely continue
            }
        }

        // Apply high-performance composite and covering indexes for enterprise workload
        await EnsureHighPerformanceIndexesAsync(context);
    }

    /// <summary>
    /// Ensures all high-performance composite and covering indexes exist for massive workloads.
    /// Eliminates table scans on heavy inventory and sales queries.
    /// </summary>
    public static async Task EnsureHighPerformanceIndexesAsync(AppDbContext context)
    {
        var indexes = new[]
        {
            // Sales & POS Covering Indexes
            @"CREATE INDEX IF NOT EXISTS ""IX_Sales_Covering"" ON ""Sales"" (""SaleDate"", ""Status"", ""IsDeleted"", ""TotalAmount"");",
            @"CREATE INDEX IF NOT EXISTS ""IX_Sales_Shift_Covering"" ON ""Sales"" (""ShiftId"", ""Status"", ""IsDeleted"", ""TotalAmount"");",
            @"CREATE INDEX IF NOT EXISTS ""IX_Sales_Customer_Date"" ON ""Sales"" (""CustomerId"", ""SaleDate"", ""IsDeleted"");",
            @"CREATE INDEX IF NOT EXISTS ""IX_Sales_InvoiceNumber"" ON ""Sales"" (""InvoiceNumber"");",

            // Sale Details lookup
            @"CREATE INDEX IF NOT EXISTS ""IX_SaleDetails_Sale_Product"" ON ""SaleDetails"" (""SaleId"", ""ProductId"");",

            // Product Inventory & Fast Scanning
            @"CREATE INDEX IF NOT EXISTS ""IX_Products_LowStock"" ON ""Products"" (""IsActive"", ""IsDeleted"", ""Stock"", ""MinStockLevel"");",
            @"CREATE INDEX IF NOT EXISTS ""IX_Products_Category_Active"" ON ""Products"" (""CategoryId"", ""IsActive"", ""IsDeleted"");",
            @"CREATE INDEX IF NOT EXISTS ""IX_Products_Barcode_Active"" ON ""Products"" (""Barcode"", ""IsActive"", ""IsDeleted"");",

            // Customers fast lookup
            @"CREATE INDEX IF NOT EXISTS ""IX_Customers_Phone_Deleted"" ON ""Customers"" (""Phone"", ""IsDeleted"");",

            // Expenses & Shifts
            @"CREATE INDEX IF NOT EXISTS ""IX_Expenses_Date_User_Deleted"" ON ""Expenses"" (""ExpenseDate"", ""UserId"", ""IsDeleted"");",
            @"CREATE INDEX IF NOT EXISTS ""IX_Shifts_Status_UserId"" ON ""Shifts"" (""Status"", ""UserId"");",
            @"CREATE INDEX IF NOT EXISTS ""IX_StockMovements_Lookup"" ON ""StockMovements"" (""ProductId"", ""MovementDate"");"
        };

        foreach (var sql in indexes)
        {
            try
            {
                await context.Database.ExecuteSqlRawAsync(sql);
            }
            catch
            {
                // Index creation is idempotent, safely continue
            }
        }
    }

    // ──────────────────────────── Users ────────────────────────────
    private static async Task SeedUsersAsync(AppDbContext context, DateTime now)
    {
        var superAdmin = await context.Users.FirstOrDefaultAsync(u => u.Username == "superadmin");
        if (superAdmin == null)
        {
            context.Users.Add(new User
            {
                FullName = "مطور النظام - RoboVAI",
                Username = "superadmin",
                PasswordHash = "super@2026",
                Role = UserRole.SuperAdmin,
                Permissions = Permissions.All,
                AdminPin = "0000",
                IsActive = true,
                CreatedAt = now
            });
        }

        var admin = await context.Users.FirstOrDefaultAsync(u => u.Username == "admin");
        if (admin == null)
        {
            context.Users.Add(new User
            {
                FullName = "مدير المحل",
                Username = "admin",
                PasswordHash = "admin@2026",
                Role = UserRole.Admin,
                Permissions = Permissions.ViewDashboard | Permissions.AccessPOS | Permissions.ManageProducts
                    | Permissions.ManageCategories | Permissions.ManageSuppliers | Permissions.ManageCustomers
                    | Permissions.ViewReports | Permissions.ManageUsers | Permissions.ManageExpenses
                    | Permissions.ManagePurchases | Permissions.ManageReturns | Permissions.ManageShifts
                    | Permissions.OpenCashDrawer | Permissions.ApplyDiscount | Permissions.ApplyHighDiscount
                    | Permissions.VoidItem | Permissions.HoldSale | Permissions.IssueRefund
                    | Permissions.ProvideAdminPin,
                AdminPin = "1234",
                IsActive = true,
                CreatedAt = now
            });
        }
        else
        {
            // Update legacy admin to have the new necessary fields
            bool changed = false;
            if (string.IsNullOrEmpty(admin.AdminPin)) { admin.AdminPin = "1234"; changed = true; }
            if (admin.Permissions == 0) { admin.Permissions = Permissions.ViewDashboard | Permissions.AccessPOS | Permissions.ManageProducts | Permissions.ManageCategories | Permissions.ManageSuppliers | Permissions.ManageCustomers | Permissions.ViewReports | Permissions.ManageUsers | Permissions.ManageExpenses | Permissions.ManagePurchases | Permissions.ManageReturns | Permissions.ManageShifts | Permissions.OpenCashDrawer | Permissions.ApplyDiscount | Permissions.ApplyHighDiscount | Permissions.VoidItem | Permissions.HoldSale | Permissions.IssueRefund | Permissions.ProvideAdminPin; changed = true; }
            if (admin.PasswordHash != null && admin.PasswordHash.StartsWith("$2a$11$X")) { admin.PasswordHash = "admin@2026"; changed = true; }
            if (changed) context.Users.Update(admin);
        }

        var cashier = await context.Users.FirstOrDefaultAsync(u => u.Username == "cashier" || u.Username == "cashier1");
        if (cashier == null)
        {
            context.Users.Add(new User
            {
                FullName = "موظف الكاشير",
                Username = "cashier",
                PasswordHash = "cashier@2026",
                Role = UserRole.Cashier,
                Permissions = Permissions.AccessPOS | Permissions.ManageShifts | Permissions.ApplyDiscount,
                IsActive = true,
                CreatedAt = now
            });
        }
        else
        {
            bool changed = false;
            if (cashier.Permissions == 0) { cashier.Permissions = Permissions.AccessPOS | Permissions.ManageShifts | Permissions.ApplyDiscount; changed = true; }
            if (cashier.PasswordHash != null && cashier.PasswordHash.StartsWith("$2a$11$X")) { cashier.PasswordHash = "cashier@2026"; changed = true; }
            if (changed) context.Users.Update(cashier);
        }

        await context.SaveChangesAsync();
    }

    // ──────────────────────────── Categories ────────────────────────────
    private static async Task SeedCategoriesAsync(AppDbContext context, DateTime now)
    {
        if (await context.Categories.AnyAsync()) return;

        context.Categories.AddRange(
            new Category { Name = "مواد غذائية", Description = "المواد الغذائية والمشروبات", ColorCode = "#4CAF50", CreatedAt = now },
            new Category { Name = "مشروبات", Description = "المشروبات الساخنة والباردة", ColorCode = "#2196F3", CreatedAt = now },
            new Category { Name = "حلويات", Description = "الحلويات والشوكولاتة", ColorCode = "#FF9800", CreatedAt = now },
            new Category { Name = "ألبان", Description = "منتجات الألبان والأجبان", ColorCode = "#9C27B0", CreatedAt = now },
            new Category { Name = "معلبات", Description = "الأطعمة المعلبة", ColorCode = "#F44336", CreatedAt = now },
            new Category { Name = "وجبات", Description = "وجبات مطعم وكافيه", ColorCode = "#795548", CreatedAt = now }
        );
        await context.SaveChangesAsync();
    }

    // ──────────────────────────── Suppliers ────────────────────────────
    private static async Task SeedSuppliersAsync(AppDbContext context, DateTime now)
    {
        if (await context.Suppliers.AnyAsync()) return;

        context.Suppliers.AddRange(
            new Supplier { Name = "شركة الأغذية المصرية", Phone = "0123456789", Email = "info@egyptfood.com", DebtAmount = 0, CreatedAt = now },
            new Supplier { Name = "مورد المشروبات الوطنية", Phone = "0111222333", Email = "sales@drinks.com", DebtAmount = 0, CreatedAt = now }
        );
        await context.SaveChangesAsync();
    }

    // ──────────────────────────── Customers ────────────────────────────
    private static async Task SeedCustomersAsync(AppDbContext context, DateTime now)
    {
        if (await context.Customers.AnyAsync()) return;

        context.Customers.AddRange(
            new Customer { Name = "عميل نقدي", Phone = "0100000000", Address = "--", CreditLimit = 0m, CurrentDebt = 0m, IsActive = true, CreatedAt = now },
            new Customer { Name = "شركة الهدى", Phone = "0101234567", Email = "sales@alhuda.com", Address = "القاهرة", CreditLimit = 5000m, CurrentDebt = 0m, IsActive = true, CreatedAt = now }
        );
        await context.SaveChangesAsync();
    }

    // ──────────────────────────── Products ────────────────────────────
    private static async Task SeedProductsAsync(AppDbContext context, DateTime now)
    {
        if (await context.Products.AnyAsync()) return;

        // Resolve category & supplier IDs dynamically (no hardcoded IDs)
        var cats = await context.Categories.ToListAsync();
        var sups = await context.Suppliers.ToListAsync();

        var catFood  = cats.First(c => c.Name == "مواد غذائية").Id;
        var catDrink = cats.First(c => c.Name == "مشروبات").Id;
        var catSweet = cats.First(c => c.Name == "حلويات").Id;
        var catDairy = cats.First(c => c.Name == "ألبان").Id;
        var catCan   = cats.First(c => c.Name == "معلبات").Id;
        var catMeal  = cats.First(c => c.Name == "وجبات").Id;
        var sup1 = sups[0].Id;
        var sup2 = sups.Count > 1 ? sups[1].Id : sup1;

        context.Products.AddRange(
            // مواد غذائية
            new Product { Barcode = "1001", Name = "أرز أبيض - كيس 1 كجم", CategoryId = catFood, SupplierId = sup1, PurchasePrice = 15m, SellingPrice = 20m, Stock = 50, MinStockLevel = 10, Unit = UnitType.Kilogram, IsActive = true, CreatedAt = now },
            new Product { Barcode = "1002", Name = "سكر أبيض - كيس 1 كجم", CategoryId = catFood, SupplierId = sup1, PurchasePrice = 18m, SellingPrice = 25m, Stock = 45, MinStockLevel = 15, Unit = UnitType.Kilogram, IsActive = true, CreatedAt = now },
            new Product { Barcode = "1003", Name = "زيت طعام - زجاجة 1 لتر", CategoryId = catFood, SupplierId = sup1, PurchasePrice = 35m, SellingPrice = 45m, Stock = 30, MinStockLevel = 10, Unit = UnitType.Liter, IsActive = true, CreatedAt = now },
            new Product { Barcode = "1004", Name = "مكرونة إسباجتي - علبة 500جم", CategoryId = catFood, SupplierId = sup1, PurchasePrice = 8m, SellingPrice = 12m, Stock = 60, MinStockLevel = 20, Unit = UnitType.Box, IsActive = true, CreatedAt = now },
            new Product { Barcode = "1005", Name = "طحين أبيض - كيس 1 كجم", CategoryId = catFood, SupplierId = sup1, PurchasePrice = 10m, SellingPrice = 15m, Stock = 8, MinStockLevel = 10, Unit = UnitType.Kilogram, IsActive = true, CreatedAt = now },

            // مشروبات
            new Product { Barcode = "2001", Name = "كوكاكولا - علبة 330 مل", CategoryId = catDrink, SupplierId = sup2, PurchasePrice = 4m, SellingPrice = 7m, Stock = 100, MinStockLevel = 30, Unit = UnitType.Piece, IsActive = true, CreatedAt = now },
            new Product { Barcode = "2002", Name = "عصير برتقال - عبوة 1 لتر", CategoryId = catDrink, SupplierId = sup2, PurchasePrice = 12m, SellingPrice = 18m, Stock = 40, MinStockLevel = 15, Unit = UnitType.Liter, IsActive = true, CreatedAt = now },
            new Product { Barcode = "2003", Name = "ماء معدني - زجاجة 1.5 لتر", CategoryId = catDrink, SupplierId = sup2, PurchasePrice = 3m, SellingPrice = 5m, Stock = 80, MinStockLevel = 25, Unit = UnitType.Liter, IsActive = true, CreatedAt = now },
            new Product { Barcode = "2004", Name = "شاي ليبتون - علبة 100 كيس", CategoryId = catDrink, SupplierId = sup2, PurchasePrice = 25m, SellingPrice = 35m, Stock = 5, MinStockLevel = 10, Unit = UnitType.Box, IsActive = true, CreatedAt = now },
            new Product { Barcode = "2005", Name = "قهوة نسكافيه - برطمان 200 جم", CategoryId = catDrink, SupplierId = sup2, PurchasePrice = 40m, SellingPrice = 55m, Stock = 25, MinStockLevel = 8, Unit = UnitType.Piece, IsActive = true, CreatedAt = now },

            // حلويات
            new Product { Barcode = "3001", Name = "شوكولاتة كيت كات", CategoryId = catSweet, SupplierId = sup1, PurchasePrice = 5m, SellingPrice = 8m, Stock = 70, MinStockLevel = 20, Unit = UnitType.Piece, IsActive = true, CreatedAt = now },
            new Product { Barcode = "3002", Name = "بسكويت أوريو", CategoryId = catSweet, SupplierId = sup1, PurchasePrice = 6m, SellingPrice = 10m, Stock = 55, MinStockLevel = 15, Unit = UnitType.Box, IsActive = true, CreatedAt = now },
            new Product { Barcode = "3003", Name = "حلوى جيلي - كيس صغير", CategoryId = catSweet, SupplierId = sup1, PurchasePrice = 2m, SellingPrice = 4m, Stock = 90, MinStockLevel = 30, Unit = UnitType.Piece, IsActive = true, CreatedAt = now },

            // ألبان
            new Product { Barcode = "4001", Name = "لبن كامل الدسم - كرتونة 1 لتر", CategoryId = catDairy, SupplierId = sup1, PurchasePrice = 15m, SellingPrice = 20m, Stock = 35, MinStockLevel = 15, Unit = UnitType.Liter, IsActive = true, CreatedAt = now },
            new Product { Barcode = "4002", Name = "جبنة بيضاء - علبة 500 جم", CategoryId = catDairy, SupplierId = sup1, PurchasePrice = 30m, SellingPrice = 40m, Stock = 20, MinStockLevel = 10, Unit = UnitType.Box, IsActive = true, CreatedAt = now },
            new Product { Barcode = "4003", Name = "زبادي فواكه - علبة صغيرة", CategoryId = catDairy, SupplierId = sup1, PurchasePrice = 4m, SellingPrice = 6m, Stock = 45, MinStockLevel = 20, Unit = UnitType.Piece, IsActive = true, CreatedAt = now },

            // معلبات
            new Product { Barcode = "5001", Name = "تونة - علبة متوسطة", CategoryId = catCan, SupplierId = sup1, PurchasePrice = 12m, SellingPrice = 18m, Stock = 40, MinStockLevel = 15, Unit = UnitType.Box, IsActive = true, CreatedAt = now },
            new Product { Barcode = "5002", Name = "فول مدمس - علبة 400 جم", CategoryId = catCan, SupplierId = sup1, PurchasePrice = 8m, SellingPrice = 12m, Stock = 50, MinStockLevel = 20, Unit = UnitType.Box, IsActive = true, CreatedAt = now },
            new Product { Barcode = "5003", Name = "طماطم معجون - علبة 400 جم", CategoryId = catCan, SupplierId = sup1, PurchasePrice = 7m, SellingPrice = 10m, Stock = 3, MinStockLevel = 10, Unit = UnitType.Box, IsActive = true, CreatedAt = now },
            new Product { Barcode = "5004", Name = "ذرة حلوة - علبة 340 جم", CategoryId = catCan, SupplierId = sup1, PurchasePrice = 9m, SellingPrice = 13m, Stock = 6, MinStockLevel = 10, Unit = UnitType.Box, IsActive = true, CreatedAt = now },

            // وجبات
            new Product { Barcode = "6001", Name = "برجر كلاسيك", CategoryId = catMeal, SupplierId = sup1, PurchasePrice = 25m, SellingPrice = 40m, Stock = 30, MinStockLevel = 10, Unit = UnitType.Piece, IsActive = true, CreatedAt = now },
            new Product { Barcode = "6002", Name = "بيتزا مارجريتا", CategoryId = catMeal, SupplierId = sup1, PurchasePrice = 35m, SellingPrice = 55m, Stock = 20, MinStockLevel = 8, Unit = UnitType.Piece, IsActive = true, CreatedAt = now },
            new Product { Barcode = "6003", Name = "ساندوتش شاورما", CategoryId = catMeal, SupplierId = sup1, PurchasePrice = 18m, SellingPrice = 30m, Stock = 40, MinStockLevel = 15, Unit = UnitType.Piece, IsActive = true, CreatedAt = now },
            new Product { Barcode = "6004", Name = "وجبة فطار", CategoryId = catMeal, SupplierId = sup1, PurchasePrice = 20m, SellingPrice = 35m, Stock = 25, MinStockLevel = 10, Unit = UnitType.Piece, IsActive = true, CreatedAt = now }
        );
        await context.SaveChangesAsync();
    }

    // ──────────────────────────── Expenses ────────────────────────────
    private static async Task SeedExpensesAsync(AppDbContext context, DateTime now)
    {
        if (await context.Expenses.AnyAsync()) return;

        var adminUser = await context.Users.FirstAsync(u => u.Username == "admin");

        context.Expenses.AddRange(
            new Expense { Description = "فاتورة كهرباء - يناير", Amount = 500m, Category = ExpenseCategory.Utilities, ExpenseDate = now.AddDays(-5), UserId = adminUser.Id, CreatedAt = now.AddDays(-5) },
            new Expense { Description = "صيانة ثلاجة العرض", Amount = 350m, Category = ExpenseCategory.Maintenance, ExpenseDate = now.AddDays(-3), UserId = adminUser.Id, CreatedAt = now.AddDays(-3) },
            new Expense { Description = "مرتب عامل نظافة", Amount = 1500m, Category = ExpenseCategory.Salaries, ExpenseDate = now.AddDays(-2), UserId = adminUser.Id, CreatedAt = now.AddDays(-2) },
            new Expense { Description = "أكياس بلاستيك", Amount = 150m, Category = ExpenseCategory.Supplies, ExpenseDate = now.AddDays(-1), UserId = adminUser.Id, CreatedAt = now.AddDays(-1) }
        );
        await context.SaveChangesAsync();
    }
}
