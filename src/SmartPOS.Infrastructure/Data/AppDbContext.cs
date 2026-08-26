using Microsoft.EntityFrameworkCore;
using SmartPOS.Core.Entities;

namespace SmartPOS.Infrastructure.Data;

/// <summary>
/// Main database context for Smart POS application
/// </summary>
public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options)
    {
    }

    // DbSets
    public DbSet<Product> Products => Set<Product>();
    public DbSet<Category> Categories => Set<Category>();
    public DbSet<Sale> Sales => Set<Sale>();
    public DbSet<SaleDetail> SaleDetails => Set<SaleDetail>();
    public DbSet<User> Users => Set<User>();
    public DbSet<Expense> Expenses => Set<Expense>();
    public DbSet<Supplier> Suppliers => Set<Supplier>();
    public DbSet<Customer> Customers => Set<Customer>();
    public DbSet<PurchaseOrder> PurchaseOrders => Set<PurchaseOrder>();
    public DbSet<PurchaseOrderDetail> PurchaseOrderDetails => Set<PurchaseOrderDetail>();
    public DbSet<SupplierPayment> SupplierPayments => Set<SupplierPayment>();
    public DbSet<StockMovement> StockMovements => Set<StockMovement>();
    public DbSet<AppSetting> AppSettings => Set<AppSetting>();

    // New DbSets - Space Edition
    public DbSet<Shift> Shifts => Set<Shift>();
    public DbSet<CustomerLoyalty> CustomerLoyalties => Set<CustomerLoyalty>();
    public DbSet<LoyaltyTransaction> LoyaltyTransactions => Set<LoyaltyTransaction>();
    public DbSet<Return> Returns => Set<Return>();
    public DbSet<ReturnDetail> ReturnDetails => Set<ReturnDetail>();
    public DbSet<AuditLog> AuditLogs => Set<AuditLog>();

    // Rentals
    public DbSet<RentalDevice> RentalDevices => Set<RentalDevice>();
    public DbSet<RentalSession> RentalSessions => Set<RentalSession>();

    // Sync Engine Outbox (Milestone M1)
    public DbSet<SyncOutbox> SyncOutboxes => Set<SyncOutbox>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // Product Configuration
        modelBuilder.Entity<Product>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Barcode).HasMaxLength(50).IsRequired();
            entity.Property(e => e.Name).HasMaxLength(200).IsRequired();
            entity.Property(e => e.PurchasePrice).HasPrecision(18, 2);
            entity.Property(e => e.SellingPrice).HasPrecision(18, 2);
            entity.HasIndex(e => e.Barcode).IsUnique();
            entity.HasIndex(e => e.Name);

            entity.HasOne(e => e.Category)
                .WithMany(c => c.Products)
                .HasForeignKey(e => e.CategoryId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne(e => e.Supplier)
                .WithMany(s => s.Products)
                .HasForeignKey(e => e.SupplierId)
                .OnDelete(DeleteBehavior.SetNull);
        });

        // Category Configuration
        modelBuilder.Entity<Category>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Name).HasMaxLength(100).IsRequired();
        });

        // Sale Configuration
        modelBuilder.Entity<Sale>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.InvoiceNumber).HasMaxLength(50).IsRequired();
            entity.Property(e => e.Subtotal).HasPrecision(18, 2);
            entity.Property(e => e.DiscountAmount).HasPrecision(18, 2);
            entity.Property(e => e.TaxAmount).HasPrecision(18, 2);
            entity.Property(e => e.TotalAmount).HasPrecision(18, 2);
            entity.Property(e => e.AmountPaid).HasPrecision(18, 2);
            entity.Property(e => e.ChangeAmount).HasPrecision(18, 2);
            entity.HasIndex(e => e.InvoiceNumber).IsUnique();
            entity.HasIndex(e => e.SaleDate);

            entity.HasOne(e => e.User)
                .WithMany(u => u.Sales)
                .HasForeignKey(e => e.UserId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne(e => e.ConsumedByUser)
                .WithMany()
                .HasForeignKey(e => e.ConsumedByUserId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne(e => e.Customer)
                .WithMany(c => c.Sales)
                .HasForeignKey(e => e.CustomerId)
                .OnDelete(DeleteBehavior.SetNull);
        });

        // SaleDetail Configuration
        modelBuilder.Entity<SaleDetail>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.UnitPrice).HasPrecision(18, 2);
            entity.Property(e => e.UnitCost).HasPrecision(18, 2);
            entity.Property(e => e.DiscountAmount).HasPrecision(18, 2);
            entity.Property(e => e.LineTotal).HasPrecision(18, 2);

            entity.HasOne(e => e.Sale)
                .WithMany(s => s.SaleDetails)
                .HasForeignKey(e => e.SaleId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(e => e.Product)
                .WithMany(p => p.SaleDetails)
                .HasForeignKey(e => e.ProductId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        // User Configuration
        modelBuilder.Entity<User>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Username).HasMaxLength(50).IsRequired();
            entity.Property(e => e.FullName).HasMaxLength(100).IsRequired();
            entity.HasIndex(e => e.Username).IsUnique();
        });

        // Expense Configuration
        modelBuilder.Entity<Expense>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Amount).HasPrecision(18, 2);
            entity.HasIndex(e => e.ExpenseDate);

            entity.HasOne(e => e.User)
                .WithMany(u => u.Expenses)
                .HasForeignKey(e => e.UserId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        // Supplier Configuration
        modelBuilder.Entity<Supplier>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Name).HasMaxLength(200).IsRequired();
            entity.Property(e => e.DebtAmount).HasPrecision(18, 2);
        });

        // SupplierPayment Configuration
        modelBuilder.Entity<SupplierPayment>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Amount).HasPrecision(18, 2);

            entity.HasOne(e => e.Supplier)
                .WithMany(s => s.Payments)
                .HasForeignKey(e => e.SupplierId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        // Customer Configuration
        modelBuilder.Entity<Customer>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Name).HasMaxLength(200).IsRequired();
            entity.Property(e => e.CreditLimit).HasPrecision(18, 2);
            entity.Property(e => e.CurrentDebt).HasPrecision(18, 2);
        });

        // PurchaseOrder Configuration
        modelBuilder.Entity<PurchaseOrder>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.OrderNumber).HasMaxLength(50).IsRequired();
            entity.Property(e => e.TotalAmount).HasPrecision(18, 2);
            entity.Property(e => e.PaidAmount).HasPrecision(18, 2);
            entity.HasIndex(e => e.OrderNumber).IsUnique();

            entity.HasOne(e => e.Supplier)
                .WithMany(s => s.PurchaseOrders)
                .HasForeignKey(e => e.SupplierId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        // PurchaseOrderDetail Configuration
        modelBuilder.Entity<PurchaseOrderDetail>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.UnitCost).HasPrecision(18, 2);
            entity.Property(e => e.TotalCost).HasPrecision(18, 2);

            entity.HasOne(e => e.PurchaseOrder)
                .WithMany(o => o.OrderDetails)
                .HasForeignKey(e => e.PurchaseOrderId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(e => e.Product)
                .WithMany()
                .HasForeignKey(e => e.ProductId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        // StockMovement Configuration
        modelBuilder.Entity<StockMovement>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.HasIndex(e => e.MovementDate);

            entity.HasOne(e => e.Product)
                .WithMany(p => p.StockMovements)
                .HasForeignKey(e => e.ProductId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        // NOTE: Data seeding is handled by DbInitializer at runtime.
        // OnModelCreating is kept clean for schema and relationships only.

        // ====== Space Edition Configurations ======

        // CustomerLoyalty Configuration (One-to-One with Customer)
        modelBuilder.Entity<CustomerLoyalty>(entity =>
        {
            entity.HasKey(e => e.Id);

            entity.HasOne(e => e.Customer)
                .WithOne(c => c.Loyalty)
                .HasForeignKey<CustomerLoyalty>(e => e.CustomerId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        // LoyaltyTransaction Configuration
        modelBuilder.Entity<LoyaltyTransaction>(entity =>
        {
            entity.HasKey(e => e.Id);

            entity.HasOne(e => e.CustomerLoyalty)
                .WithMany(cl => cl.Transactions)
                .HasForeignKey(e => e.CustomerLoyaltyId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(e => e.Sale)
                .WithMany(s => s.LoyaltyTransactions)
                .HasForeignKey(e => e.SaleId)
                .OnDelete(DeleteBehavior.SetNull);
        });

        // Shift Configuration
        modelBuilder.Entity<Shift>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.OpeningBalance).HasPrecision(18, 2);
            entity.Property(e => e.ClosingBalance).HasPrecision(18, 2);
            entity.Property(e => e.ExpectedBalance).HasPrecision(18, 2);
            entity.Property(e => e.Difference).HasPrecision(18, 2);

            entity.HasIndex(e => new { e.UserId, e.Status });
            entity.HasIndex(e => e.StartTime);

            entity.HasOne(e => e.User)
                .WithMany()
                .HasForeignKey(e => e.UserId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        // Sale-Shift Relationship
        modelBuilder.Entity<Sale>()
            .HasOne(s => s.Shift)
            .WithMany(sh => sh.Sales)
            .HasForeignKey(s => s.ShiftId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<Sale>()
            .HasIndex(s => s.ShiftId);

        modelBuilder.Entity<Sale>()
            .HasIndex(s => new { s.ShiftId, s.SaleDate });

        // Return Configuration
        modelBuilder.Entity<Return>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.ReturnNumber).HasMaxLength(50).IsRequired();
            entity.Property(e => e.TotalAmount).HasPrecision(18, 2);

            entity.HasOne(e => e.Sale)
                .WithMany(s => s.Returns)
                .HasForeignKey(e => e.SaleId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne(e => e.Customer)
                .WithMany(c => c.Returns)
                .HasForeignKey(e => e.CustomerId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne(e => e.ProcessedBy)
                .WithMany()
                .HasForeignKey(e => e.ProcessedByUserId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        // ReturnDetail Configuration
        modelBuilder.Entity<ReturnDetail>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.UnitPrice).HasPrecision(18, 2);
            entity.Property(e => e.Subtotal).HasPrecision(18, 2);

            entity.HasOne(e => e.Return)
                .WithMany(r => r.ReturnDetails)
                .HasForeignKey(e => e.ReturnId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(e => e.Product)
                .WithMany()
                .HasForeignKey(e => e.ProductId)
                .OnDelete(DeleteBehavior.Restrict);
        });
        // ====== Global Query Filters (Soft Delete) ======
        modelBuilder.Entity<Product>().HasQueryFilter(e => !e.IsDeleted);
        modelBuilder.Entity<Category>().HasQueryFilter(e => !e.IsDeleted);
        modelBuilder.Entity<Sale>().HasQueryFilter(e => !e.IsDeleted);
        modelBuilder.Entity<SaleDetail>().HasQueryFilter(e => !e.IsDeleted);
        modelBuilder.Entity<User>().HasQueryFilter(e => !e.IsDeleted);
        modelBuilder.Entity<Expense>().HasQueryFilter(e => !e.IsDeleted);
        modelBuilder.Entity<Supplier>().HasQueryFilter(e => !e.IsDeleted);
        modelBuilder.Entity<Customer>().HasQueryFilter(e => !e.IsDeleted);
        modelBuilder.Entity<PurchaseOrder>().HasQueryFilter(e => !e.IsDeleted);
        modelBuilder.Entity<PurchaseOrderDetail>().HasQueryFilter(e => !e.IsDeleted);
        modelBuilder.Entity<StockMovement>().HasQueryFilter(e => !e.IsDeleted);
        modelBuilder.Entity<Shift>().HasQueryFilter(e => !e.IsDeleted);
        modelBuilder.Entity<CustomerLoyalty>().HasQueryFilter(e => !e.IsDeleted);
        modelBuilder.Entity<LoyaltyTransaction>().HasQueryFilter(e => !e.IsDeleted);
        modelBuilder.Entity<Return>().HasQueryFilter(e => !e.IsDeleted);
        modelBuilder.Entity<ReturnDetail>().HasQueryFilter(e => !e.IsDeleted);
        modelBuilder.Entity<RentalDevice>().HasQueryFilter(e => !e.IsDeleted);
        modelBuilder.Entity<RentalSession>().HasQueryFilter(e => !e.IsDeleted);

        // Rental Configuration
        modelBuilder.Entity<RentalDevice>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Name).HasMaxLength(100).IsRequired();
            entity.Property(e => e.HourlyRate).HasPrecision(18, 2);
        });

        modelBuilder.Entity<RentalSession>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.HourlyRateApplied).HasPrecision(18, 2);
            entity.Property(e => e.TotalAmount).HasPrecision(18, 2);
            entity.Property(e => e.CustomerName).HasMaxLength(100);
            entity.Property(e => e.Notes).HasMaxLength(500);

            entity.HasOne(e => e.Device)
                .WithMany()
                .HasForeignKey(e => e.RentalDeviceId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(e => e.Sale)
                .WithMany()
                .HasForeignKey(e => e.SaleId)
                .OnDelete(DeleteBehavior.SetNull);
        });

        // SyncOutbox Configuration (Milestone M1)
        modelBuilder.Entity<SyncOutbox>(entity =>
        {
            entity.ToTable("sync_outbox");
            entity.HasKey(e => e.Id);

            entity.Property(e => e.Id)
                .HasMaxLength(36)
                .ValueGeneratedNever();

            entity.Property(e => e.EntityType)
                .HasMaxLength(100)
                .IsRequired();

            entity.Property(e => e.EntityId)
                .HasMaxLength(100)
                .IsRequired();

            entity.Property(e => e.SyncId)
                .HasMaxLength(36)
                .IsRequired();

            entity.Property(e => e.Operation)
                .HasConversion<string>()
                .HasMaxLength(20)
                .IsRequired();

            entity.Property(e => e.PayloadJson)
                .IsRequired();

            entity.Property(e => e.CreatedAt)
                .HasConversion(
                    v => v.ToUniversalTime().ToString("o"),
                    v => DateTime.Parse(v, null, System.Globalization.DateTimeStyles.AdjustToUniversal))
                .IsRequired();

            entity.Property(e => e.SyncedAt)
                .HasConversion(
                    v => v.HasValue ? v.Value.ToUniversalTime().ToString("o") : null,
                    v => v != null ? DateTime.Parse(v, null, System.Globalization.DateTimeStyles.AdjustToUniversal) : (DateTime?)null);

            entity.Property(e => e.Status)
                .HasConversion<string>()
                .HasMaxLength(20)
                .HasDefaultValue(OutboxStatus.Pending)
                .IsRequired();

            entity.Property(e => e.RetryCount)
                .HasDefaultValue(0);

            entity.Property(e => e.Version)
                .HasDefaultValue(1L);

            entity.HasIndex(e => new { e.Status, e.CreatedAt })
                .HasDatabaseName("IX_sync_outbox_status_created_at");

            entity.HasIndex(e => e.SyncId)
                .HasDatabaseName("IX_sync_outbox_sync_id");

            entity.HasIndex(e => new { e.EntityType, e.EntityId })
                .HasDatabaseName("IX_sync_outbox_entity");
        });
    }
}
