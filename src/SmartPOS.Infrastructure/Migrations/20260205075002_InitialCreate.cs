using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace SmartPOS.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Categories",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    Name = table.Column<string>(type: "TEXT", maxLength: 100, nullable: false),
                    Description = table.Column<string>(type: "TEXT", nullable: true),
                    IconName = table.Column<string>(type: "TEXT", nullable: true),
                    ColorCode = table.Column<string>(type: "TEXT", nullable: false),
                    IsActive = table.Column<bool>(type: "INTEGER", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "TEXT", nullable: true),
                    IsDeleted = table.Column<bool>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Categories", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Customers",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    Name = table.Column<string>(type: "TEXT", maxLength: 200, nullable: false),
                    Phone = table.Column<string>(type: "TEXT", nullable: true),
                    Email = table.Column<string>(type: "TEXT", nullable: true),
                    Address = table.Column<string>(type: "TEXT", nullable: true),
                    CreditLimit = table.Column<decimal>(type: "TEXT", precision: 18, scale: 2, nullable: false),
                    CurrentDebt = table.Column<decimal>(type: "TEXT", precision: 18, scale: 2, nullable: false),
                    IsActive = table.Column<bool>(type: "INTEGER", nullable: false),
                    Birthdate = table.Column<DateTime>(type: "TEXT", nullable: true),
                    Notes = table.Column<string>(type: "TEXT", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "TEXT", nullable: true),
                    IsDeleted = table.Column<bool>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Customers", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Suppliers",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    Name = table.Column<string>(type: "TEXT", maxLength: 200, nullable: false),
                    ContactPerson = table.Column<string>(type: "TEXT", nullable: true),
                    Phone = table.Column<string>(type: "TEXT", nullable: true),
                    Email = table.Column<string>(type: "TEXT", nullable: true),
                    Address = table.Column<string>(type: "TEXT", nullable: true),
                    DebtAmount = table.Column<decimal>(type: "TEXT", precision: 18, scale: 2, nullable: false),
                    IsActive = table.Column<bool>(type: "INTEGER", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "TEXT", nullable: true),
                    IsDeleted = table.Column<bool>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Suppliers", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Users",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    Username = table.Column<string>(type: "TEXT", maxLength: 50, nullable: false),
                    PasswordHash = table.Column<string>(type: "TEXT", nullable: false),
                    FullName = table.Column<string>(type: "TEXT", maxLength: 100, nullable: false),
                    Email = table.Column<string>(type: "TEXT", nullable: true),
                    Phone = table.Column<string>(type: "TEXT", nullable: true),
                    Role = table.Column<int>(type: "INTEGER", nullable: false),
                    IsActive = table.Column<bool>(type: "INTEGER", nullable: false),
                    LastLogin = table.Column<DateTime>(type: "TEXT", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "TEXT", nullable: true),
                    IsDeleted = table.Column<bool>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Users", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "CustomerLoyalties",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    Points = table.Column<int>(type: "INTEGER", nullable: false),
                    TotalPointsEarned = table.Column<int>(type: "INTEGER", nullable: false),
                    TotalPointsRedeemed = table.Column<int>(type: "INTEGER", nullable: false),
                    Tier = table.Column<int>(type: "INTEGER", nullable: false),
                    LastTierUpdate = table.Column<DateTime>(type: "TEXT", nullable: true),
                    CustomerId = table.Column<int>(type: "INTEGER", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "TEXT", nullable: true),
                    IsDeleted = table.Column<bool>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CustomerLoyalties", x => x.Id);
                    table.ForeignKey(
                        name: "FK_CustomerLoyalties_Customers_CustomerId",
                        column: x => x.CustomerId,
                        principalTable: "Customers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Products",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    Barcode = table.Column<string>(type: "TEXT", maxLength: 50, nullable: false),
                    Name = table.Column<string>(type: "TEXT", maxLength: 200, nullable: false),
                    Description = table.Column<string>(type: "TEXT", nullable: true),
                    PurchasePrice = table.Column<decimal>(type: "TEXT", precision: 18, scale: 2, nullable: false),
                    SellingPrice = table.Column<decimal>(type: "TEXT", precision: 18, scale: 2, nullable: false),
                    Stock = table.Column<int>(type: "INTEGER", nullable: false),
                    MinStockLevel = table.Column<int>(type: "INTEGER", nullable: false),
                    Unit = table.Column<int>(type: "INTEGER", nullable: false),
                    IsActive = table.Column<bool>(type: "INTEGER", nullable: false),
                    CategoryId = table.Column<int>(type: "INTEGER", nullable: false),
                    SupplierId = table.Column<int>(type: "INTEGER", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "TEXT", nullable: true),
                    IsDeleted = table.Column<bool>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Products", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Products_Categories_CategoryId",
                        column: x => x.CategoryId,
                        principalTable: "Categories",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Products_Suppliers_SupplierId",
                        column: x => x.SupplierId,
                        principalTable: "Suppliers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateTable(
                name: "PurchaseOrders",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    OrderNumber = table.Column<string>(type: "TEXT", maxLength: 50, nullable: false),
                    OrderDate = table.Column<DateTime>(type: "TEXT", nullable: false),
                    ReceivedDate = table.Column<DateTime>(type: "TEXT", nullable: true),
                    TotalAmount = table.Column<decimal>(type: "TEXT", precision: 18, scale: 2, nullable: false),
                    PaidAmount = table.Column<decimal>(type: "TEXT", precision: 18, scale: 2, nullable: false),
                    Status = table.Column<int>(type: "INTEGER", nullable: false),
                    Notes = table.Column<string>(type: "TEXT", nullable: true),
                    SupplierId = table.Column<int>(type: "INTEGER", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "TEXT", nullable: true),
                    IsDeleted = table.Column<bool>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PurchaseOrders", x => x.Id);
                    table.ForeignKey(
                        name: "FK_PurchaseOrders_Suppliers_SupplierId",
                        column: x => x.SupplierId,
                        principalTable: "Suppliers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "Expenses",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    Description = table.Column<string>(type: "TEXT", nullable: false),
                    Amount = table.Column<decimal>(type: "TEXT", precision: 18, scale: 2, nullable: false),
                    ExpenseDate = table.Column<DateTime>(type: "TEXT", nullable: false),
                    Category = table.Column<int>(type: "INTEGER", nullable: false),
                    Receipt = table.Column<string>(type: "TEXT", nullable: true),
                    Notes = table.Column<string>(type: "TEXT", nullable: true),
                    UserId = table.Column<int>(type: "INTEGER", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "TEXT", nullable: true),
                    IsDeleted = table.Column<bool>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Expenses", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Expenses_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "Shifts",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    StartTime = table.Column<DateTime>(type: "TEXT", nullable: false),
                    EndTime = table.Column<DateTime>(type: "TEXT", nullable: true),
                    OpeningBalance = table.Column<decimal>(type: "TEXT", precision: 18, scale: 2, nullable: false),
                    ClosingBalance = table.Column<decimal>(type: "TEXT", precision: 18, scale: 2, nullable: true),
                    ExpectedBalance = table.Column<decimal>(type: "TEXT", precision: 18, scale: 2, nullable: true),
                    Difference = table.Column<decimal>(type: "TEXT", precision: 18, scale: 2, nullable: true),
                    Status = table.Column<int>(type: "INTEGER", nullable: false),
                    Notes = table.Column<string>(type: "TEXT", nullable: true),
                    UserId = table.Column<int>(type: "INTEGER", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "TEXT", nullable: true),
                    IsDeleted = table.Column<bool>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Shifts", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Shifts_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "StockMovements",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    ProductId = table.Column<int>(type: "INTEGER", nullable: false),
                    Quantity = table.Column<int>(type: "INTEGER", nullable: false),
                    Type = table.Column<int>(type: "INTEGER", nullable: false),
                    Reference = table.Column<string>(type: "TEXT", nullable: true),
                    Notes = table.Column<string>(type: "TEXT", nullable: true),
                    MovementDate = table.Column<DateTime>(type: "TEXT", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "TEXT", nullable: true),
                    IsDeleted = table.Column<bool>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_StockMovements", x => x.Id);
                    table.ForeignKey(
                        name: "FK_StockMovements_Products_ProductId",
                        column: x => x.ProductId,
                        principalTable: "Products",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Sales",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    InvoiceNumber = table.Column<string>(type: "TEXT", maxLength: 50, nullable: false),
                    SaleDate = table.Column<DateTime>(type: "TEXT", nullable: false),
                    Subtotal = table.Column<decimal>(type: "TEXT", precision: 18, scale: 2, nullable: false),
                    DiscountAmount = table.Column<decimal>(type: "TEXT", precision: 18, scale: 2, nullable: false),
                    DiscountPercentage = table.Column<decimal>(type: "TEXT", nullable: false),
                    TaxAmount = table.Column<decimal>(type: "TEXT", precision: 18, scale: 2, nullable: false),
                    TotalAmount = table.Column<decimal>(type: "TEXT", precision: 18, scale: 2, nullable: false),
                    AmountPaid = table.Column<decimal>(type: "TEXT", precision: 18, scale: 2, nullable: false),
                    ChangeAmount = table.Column<decimal>(type: "TEXT", precision: 18, scale: 2, nullable: false),
                    PaymentMethod = table.Column<int>(type: "INTEGER", nullable: false),
                    Status = table.Column<int>(type: "INTEGER", nullable: false),
                    Notes = table.Column<string>(type: "TEXT", nullable: true),
                    LoyaltyPointsEarned = table.Column<int>(type: "INTEGER", nullable: true),
                    LoyaltyPointsRedeemed = table.Column<int>(type: "INTEGER", nullable: true),
                    QRCode = table.Column<string>(type: "TEXT", nullable: true),
                    IsPrinted = table.Column<bool>(type: "INTEGER", nullable: false),
                    UserId = table.Column<int>(type: "INTEGER", nullable: false),
                    CustomerId = table.Column<int>(type: "INTEGER", nullable: true),
                    ShiftId = table.Column<int>(type: "INTEGER", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "TEXT", nullable: true),
                    IsDeleted = table.Column<bool>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Sales", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Sales_Customers_CustomerId",
                        column: x => x.CustomerId,
                        principalTable: "Customers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_Sales_Shifts_ShiftId",
                        column: x => x.ShiftId,
                        principalTable: "Shifts",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Sales_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "LoyaltyTransactions",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    Points = table.Column<int>(type: "INTEGER", nullable: false),
                    Type = table.Column<int>(type: "INTEGER", nullable: false),
                    Description = table.Column<string>(type: "TEXT", nullable: true),
                    RelatedAmount = table.Column<decimal>(type: "TEXT", nullable: true),
                    CustomerLoyaltyId = table.Column<int>(type: "INTEGER", nullable: false),
                    SaleId = table.Column<int>(type: "INTEGER", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "TEXT", nullable: true),
                    IsDeleted = table.Column<bool>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_LoyaltyTransactions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_LoyaltyTransactions_CustomerLoyalties_CustomerLoyaltyId",
                        column: x => x.CustomerLoyaltyId,
                        principalTable: "CustomerLoyalties",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_LoyaltyTransactions_Sales_SaleId",
                        column: x => x.SaleId,
                        principalTable: "Sales",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateTable(
                name: "Returns",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    ReturnNumber = table.Column<string>(type: "TEXT", maxLength: 50, nullable: false),
                    ReturnDate = table.Column<DateTime>(type: "TEXT", nullable: false),
                    TotalAmount = table.Column<decimal>(type: "TEXT", precision: 18, scale: 2, nullable: false),
                    Reason = table.Column<int>(type: "INTEGER", nullable: false),
                    Notes = table.Column<string>(type: "TEXT", nullable: true),
                    Status = table.Column<int>(type: "INTEGER", nullable: false),
                    IsRefunded = table.Column<bool>(type: "INTEGER", nullable: false),
                    RefundDate = table.Column<DateTime>(type: "TEXT", nullable: true),
                    SaleId = table.Column<int>(type: "INTEGER", nullable: false),
                    CustomerId = table.Column<int>(type: "INTEGER", nullable: false),
                    ProcessedByUserId = table.Column<int>(type: "INTEGER", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "TEXT", nullable: true),
                    IsDeleted = table.Column<bool>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Returns", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Returns_Customers_CustomerId",
                        column: x => x.CustomerId,
                        principalTable: "Customers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Returns_Sales_SaleId",
                        column: x => x.SaleId,
                        principalTable: "Sales",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Returns_Users_ProcessedByUserId",
                        column: x => x.ProcessedByUserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "SaleDetails",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    Quantity = table.Column<int>(type: "INTEGER", nullable: false),
                    UnitPrice = table.Column<decimal>(type: "TEXT", precision: 18, scale: 2, nullable: false),
                    UnitCost = table.Column<decimal>(type: "TEXT", precision: 18, scale: 2, nullable: false),
                    DiscountPercentage = table.Column<decimal>(type: "TEXT", nullable: false),
                    DiscountAmount = table.Column<decimal>(type: "TEXT", precision: 18, scale: 2, nullable: false),
                    LineTotal = table.Column<decimal>(type: "TEXT", precision: 18, scale: 2, nullable: false),
                    SaleId = table.Column<int>(type: "INTEGER", nullable: false),
                    ProductId = table.Column<int>(type: "INTEGER", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "TEXT", nullable: true),
                    IsDeleted = table.Column<bool>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SaleDetails", x => x.Id);
                    table.ForeignKey(
                        name: "FK_SaleDetails_Products_ProductId",
                        column: x => x.ProductId,
                        principalTable: "Products",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_SaleDetails_Sales_SaleId",
                        column: x => x.SaleId,
                        principalTable: "Sales",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "ReturnDetails",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    Quantity = table.Column<int>(type: "INTEGER", nullable: false),
                    UnitPrice = table.Column<decimal>(type: "TEXT", precision: 18, scale: 2, nullable: false),
                    Subtotal = table.Column<decimal>(type: "TEXT", precision: 18, scale: 2, nullable: false),
                    Reason = table.Column<string>(type: "TEXT", nullable: true),
                    ReturnId = table.Column<int>(type: "INTEGER", nullable: false),
                    ProductId = table.Column<int>(type: "INTEGER", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "TEXT", nullable: true),
                    IsDeleted = table.Column<bool>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ReturnDetails", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ReturnDetails_Products_ProductId",
                        column: x => x.ProductId,
                        principalTable: "Products",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ReturnDetails_Returns_ReturnId",
                        column: x => x.ReturnId,
                        principalTable: "Returns",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.InsertData(
                table: "Categories",
                columns: new[] { "Id", "ColorCode", "CreatedAt", "Description", "IconName", "IsActive", "IsDeleted", "Name", "UpdatedAt" },
                values: new object[,]
                {
                    { 1, "#4CAF50", new DateTime(2026, 2, 5, 9, 50, 1, 328, DateTimeKind.Local).AddTicks(4953), "المواد الغذائية والمشروبات", null, true, false, "مواد غذائية", null },
                    { 2, "#2196F3", new DateTime(2026, 2, 5, 9, 50, 1, 328, DateTimeKind.Local).AddTicks(4953), "المشروبات الساخنة والباردة", null, true, false, "مشروبات", null },
                    { 3, "#FF9800", new DateTime(2026, 2, 5, 9, 50, 1, 328, DateTimeKind.Local).AddTicks(4953), "الحلويات والشوكولاتة", null, true, false, "حلويات", null },
                    { 4, "#9C27B0", new DateTime(2026, 2, 5, 9, 50, 1, 328, DateTimeKind.Local).AddTicks(4953), "منتجات الألبان والأجبان", null, true, false, "ألبان", null },
                    { 5, "#F44336", new DateTime(2026, 2, 5, 9, 50, 1, 328, DateTimeKind.Local).AddTicks(4953), "الأطعمة المعلبة", null, true, false, "معلبات", null },
                    { 6, "#795548", new DateTime(2026, 2, 5, 9, 50, 1, 328, DateTimeKind.Local).AddTicks(4953), "وجبات مطعم وكافيه", null, true, false, "وجبات", null }
                });

            migrationBuilder.InsertData(
                table: "Customers",
                columns: new[] { "Id", "Address", "Birthdate", "CreatedAt", "CreditLimit", "CurrentDebt", "Email", "IsActive", "IsDeleted", "Name", "Notes", "Phone", "UpdatedAt" },
                values: new object[,]
                {
                    { 1, "--", null, new DateTime(2026, 2, 5, 9, 50, 1, 328, DateTimeKind.Local).AddTicks(4953), 0m, 0m, null, true, false, "عميل نقدي", null, "0100000000", null },
                    { 2, "القاهرة", null, new DateTime(2026, 2, 5, 9, 50, 1, 328, DateTimeKind.Local).AddTicks(4953), 5000m, 0m, "sales@alhuda.com", true, false, "شركة الهدى", null, "0101234567", null }
                });

            migrationBuilder.InsertData(
                table: "Suppliers",
                columns: new[] { "Id", "Address", "ContactPerson", "CreatedAt", "DebtAmount", "Email", "IsActive", "IsDeleted", "Name", "Phone", "UpdatedAt" },
                values: new object[,]
                {
                    { 1, null, null, new DateTime(2026, 2, 5, 9, 50, 1, 328, DateTimeKind.Local).AddTicks(4953), 0m, "info@egyptfood.com", true, false, "شركة الأغذية المصرية", "0123456789", null },
                    { 2, null, null, new DateTime(2026, 2, 5, 9, 50, 1, 328, DateTimeKind.Local).AddTicks(4953), 0m, "sales@drinks.com", true, false, "مورد المشروبات الوطنية", "0111222333", null }
                });

            migrationBuilder.InsertData(
                table: "Users",
                columns: new[] { "Id", "CreatedAt", "Email", "FullName", "IsActive", "IsDeleted", "LastLogin", "PasswordHash", "Phone", "Role", "UpdatedAt", "Username" },
                values: new object[,]
                {
                    { 1, new DateTime(2026, 2, 5, 9, 50, 1, 328, DateTimeKind.Local).AddTicks(4953), null, "مدير النظام", true, false, null, "$2a$11$X7X8X9X0X1X2X3X4X5X6X7X8X9X0X1X2X3X4X5X6X7X8X9X0X1X2", null, 1, null, "admin" },
                    { 2, new DateTime(2026, 2, 5, 9, 50, 1, 328, DateTimeKind.Local).AddTicks(4953), null, "أحمد محمود - كاشير", true, false, null, "$2a$11$X7X8X9X0X1X2X3X4X5X6X7X8X9X0X1X2X3X4X5X6X7X8X9X0X1X2", null, 3, null, "cashier1" }
                });

            migrationBuilder.InsertData(
                table: "Expenses",
                columns: new[] { "Id", "Amount", "Category", "CreatedAt", "Description", "ExpenseDate", "IsDeleted", "Notes", "Receipt", "UpdatedAt", "UserId" },
                values: new object[,]
                {
                    { 1, 500.00m, 2, new DateTime(2026, 1, 31, 9, 50, 1, 328, DateTimeKind.Local).AddTicks(4953), "فاتورة كهرباء - يناير", new DateTime(2026, 1, 31, 9, 50, 1, 328, DateTimeKind.Local).AddTicks(4953), false, null, null, null, 1 },
                    { 2, 350.00m, 5, new DateTime(2026, 2, 2, 9, 50, 1, 328, DateTimeKind.Local).AddTicks(4953), "صيانة ثلاجة العرض", new DateTime(2026, 2, 2, 9, 50, 1, 328, DateTimeKind.Local).AddTicks(4953), false, null, null, null, 1 },
                    { 3, 1500.00m, 3, new DateTime(2026, 2, 3, 9, 50, 1, 328, DateTimeKind.Local).AddTicks(4953), "مرتب عامل نظافة", new DateTime(2026, 2, 3, 9, 50, 1, 328, DateTimeKind.Local).AddTicks(4953), false, null, null, null, 1 },
                    { 4, 150.00m, 4, new DateTime(2026, 2, 4, 9, 50, 1, 328, DateTimeKind.Local).AddTicks(4953), "أكياس بلاستيك", new DateTime(2026, 2, 4, 9, 50, 1, 328, DateTimeKind.Local).AddTicks(4953), false, null, null, null, 1 }
                });

            migrationBuilder.InsertData(
                table: "Products",
                columns: new[] { "Id", "Barcode", "CategoryId", "CreatedAt", "Description", "IsActive", "IsDeleted", "MinStockLevel", "Name", "PurchasePrice", "SellingPrice", "Stock", "SupplierId", "Unit", "UpdatedAt" },
                values: new object[,]
                {
                    { 1, "1001", 1, new DateTime(2026, 2, 5, 9, 50, 1, 328, DateTimeKind.Local).AddTicks(4953), null, true, false, 10, "أرز أبيض - كيس 1 كجم", 15.00m, 20.00m, 50, 1, 4, null },
                    { 2, "1002", 1, new DateTime(2026, 2, 5, 9, 50, 1, 328, DateTimeKind.Local).AddTicks(4953), null, true, false, 15, "سكر أبيض - كيس 1 كجم", 18.00m, 25.00m, 45, 1, 4, null },
                    { 3, "1003", 1, new DateTime(2026, 2, 5, 9, 50, 1, 328, DateTimeKind.Local).AddTicks(4953), null, true, false, 10, "زيت طعام - زجاجة 1 لتر", 35.00m, 45.00m, 30, 1, 5, null },
                    { 4, "1004", 1, new DateTime(2026, 2, 5, 9, 50, 1, 328, DateTimeKind.Local).AddTicks(4953), null, true, false, 20, "مكرونة إسباجتي - علبة 500جم", 8.00m, 12.00m, 60, 1, 2, null },
                    { 5, "1005", 1, new DateTime(2026, 2, 5, 9, 50, 1, 328, DateTimeKind.Local).AddTicks(4953), null, true, false, 10, "طحين أبيض - كيس 1 كجم", 10.00m, 15.00m, 8, 1, 4, null },
                    { 6, "2001", 2, new DateTime(2026, 2, 5, 9, 50, 1, 328, DateTimeKind.Local).AddTicks(4953), null, true, false, 30, "كوكاكولا - علبة 330 مل", 4.00m, 7.00m, 100, 2, 1, null },
                    { 7, "2002", 2, new DateTime(2026, 2, 5, 9, 50, 1, 328, DateTimeKind.Local).AddTicks(4953), null, true, false, 15, "عصير برتقال - عبوة 1 لتر", 12.00m, 18.00m, 40, 2, 5, null },
                    { 8, "2003", 2, new DateTime(2026, 2, 5, 9, 50, 1, 328, DateTimeKind.Local).AddTicks(4953), null, true, false, 25, "ماء معدني - زجاجة 1.5 لتر", 3.00m, 5.00m, 80, 2, 5, null },
                    { 9, "2004", 2, new DateTime(2026, 2, 5, 9, 50, 1, 328, DateTimeKind.Local).AddTicks(4953), null, true, false, 10, "شاي ليبتون - علبة 100 كيس", 25.00m, 35.00m, 5, 2, 2, null },
                    { 10, "2005", 2, new DateTime(2026, 2, 5, 9, 50, 1, 328, DateTimeKind.Local).AddTicks(4953), null, true, false, 8, "قهوة نسكافيه - برطمان 200 جم", 40.00m, 55.00m, 25, 2, 1, null },
                    { 11, "3001", 3, new DateTime(2026, 2, 5, 9, 50, 1, 328, DateTimeKind.Local).AddTicks(4953), null, true, false, 20, "شوكولاتة كيت كات", 5.00m, 8.00m, 70, 1, 1, null },
                    { 12, "3002", 3, new DateTime(2026, 2, 5, 9, 50, 1, 328, DateTimeKind.Local).AddTicks(4953), null, true, false, 15, "بسكويت أوريو", 6.00m, 10.00m, 55, 1, 2, null },
                    { 13, "3003", 3, new DateTime(2026, 2, 5, 9, 50, 1, 328, DateTimeKind.Local).AddTicks(4953), null, true, false, 30, "حلوى جيلي - كيس صغير", 2.00m, 4.00m, 90, 1, 1, null },
                    { 14, "4001", 4, new DateTime(2026, 2, 5, 9, 50, 1, 328, DateTimeKind.Local).AddTicks(4953), null, true, false, 15, "لبن كامل الدسم - كرتونة 1 لتر", 15.00m, 20.00m, 35, 1, 5, null },
                    { 15, "4002", 4, new DateTime(2026, 2, 5, 9, 50, 1, 328, DateTimeKind.Local).AddTicks(4953), null, true, false, 10, "جبنة بيضاء - علبة 500 جم", 30.00m, 40.00m, 20, 1, 2, null },
                    { 16, "4003", 4, new DateTime(2026, 2, 5, 9, 50, 1, 328, DateTimeKind.Local).AddTicks(4953), null, true, false, 20, "زبادي فواكه - علبة صغيرة", 4.00m, 6.00m, 45, 1, 1, null },
                    { 17, "5001", 5, new DateTime(2026, 2, 5, 9, 50, 1, 328, DateTimeKind.Local).AddTicks(4953), null, true, false, 15, "تونة - علبة متوسطة", 12.00m, 18.00m, 40, 1, 2, null },
                    { 18, "5002", 5, new DateTime(2026, 2, 5, 9, 50, 1, 328, DateTimeKind.Local).AddTicks(4953), null, true, false, 20, "فول مدمس - علبة 400 جم", 8.00m, 12.00m, 50, 1, 2, null },
                    { 19, "5003", 5, new DateTime(2026, 2, 5, 9, 50, 1, 328, DateTimeKind.Local).AddTicks(4953), null, true, false, 10, "طماطم معجون - علبة 400 جم", 7.00m, 10.00m, 3, 1, 2, null },
                    { 20, "5004", 5, new DateTime(2026, 2, 5, 9, 50, 1, 328, DateTimeKind.Local).AddTicks(4953), null, true, false, 10, "ذرة حلوة - علبة 340 جم", 9.00m, 13.00m, 6, 1, 2, null },
                    { 21, "6001", 6, new DateTime(2026, 2, 5, 9, 50, 1, 328, DateTimeKind.Local).AddTicks(4953), null, true, false, 10, "برجر كلاسيك", 25.00m, 40.00m, 30, 1, 1, null },
                    { 22, "6002", 6, new DateTime(2026, 2, 5, 9, 50, 1, 328, DateTimeKind.Local).AddTicks(4953), null, true, false, 8, "بيتزا مارجريتا", 35.00m, 55.00m, 20, 1, 1, null },
                    { 23, "6003", 6, new DateTime(2026, 2, 5, 9, 50, 1, 328, DateTimeKind.Local).AddTicks(4953), null, true, false, 15, "ساندوتش شاورما", 18.00m, 30.00m, 40, 1, 1, null },
                    { 24, "6004", 6, new DateTime(2026, 2, 5, 9, 50, 1, 328, DateTimeKind.Local).AddTicks(4953), null, true, false, 10, "وجبة فطار", 20.00m, 35.00m, 25, 1, 1, null }
                });

            migrationBuilder.InsertData(
                table: "Sales",
                columns: new[] { "Id", "AmountPaid", "ChangeAmount", "CreatedAt", "CustomerId", "DiscountAmount", "DiscountPercentage", "InvoiceNumber", "IsDeleted", "IsPrinted", "LoyaltyPointsEarned", "LoyaltyPointsRedeemed", "Notes", "PaymentMethod", "QRCode", "SaleDate", "ShiftId", "Status", "Subtotal", "TaxAmount", "TotalAmount", "UpdatedAt", "UserId" },
                values: new object[,]
                {
                    { 1, 70m, 0m, new DateTime(2026, 2, 5, 8, 50, 1, 328, DateTimeKind.Local).AddTicks(4953), 1, 0m, 0m, "INV-20260205-0001", false, false, null, null, null, 1, null, new DateTime(2026, 2, 5, 8, 50, 1, 328, DateTimeKind.Local).AddTicks(4953), null, 2, 70m, 0m, 70m, null, 2 },
                    { 2, 50m, 0m, new DateTime(2026, 2, 4, 7, 50, 1, 328, DateTimeKind.Local).AddTicks(4953), 1, 5m, 0m, "INV-20260204-0002", false, false, null, null, null, 1, null, new DateTime(2026, 2, 4, 7, 50, 1, 328, DateTimeKind.Local).AddTicks(4953), null, 2, 55m, 0m, 50m, null, 2 },
                    { 3, 68m, 0m, new DateTime(2026, 2, 3, 6, 50, 1, 328, DateTimeKind.Local).AddTicks(4953), 2, 0m, 0m, "INV-20260203-0003", false, false, null, null, null, 2, null, new DateTime(2026, 2, 3, 6, 50, 1, 328, DateTimeKind.Local).AddTicks(4953), null, 2, 68m, 0m, 68m, null, 2 }
                });

            migrationBuilder.InsertData(
                table: "SaleDetails",
                columns: new[] { "Id", "CreatedAt", "DiscountAmount", "DiscountPercentage", "IsDeleted", "LineTotal", "ProductId", "Quantity", "SaleId", "UnitCost", "UnitPrice", "UpdatedAt" },
                values: new object[,]
                {
                    { 1, new DateTime(2026, 2, 5, 8, 50, 1, 328, DateTimeKind.Local).AddTicks(4953), 0m, 0m, false, 40m, 1, 2, 1, 15m, 20m, null },
                    { 2, new DateTime(2026, 2, 5, 8, 50, 1, 328, DateTimeKind.Local).AddTicks(4953), 0m, 0m, false, 14m, 6, 2, 1, 4m, 7m, null },
                    { 3, new DateTime(2026, 2, 5, 8, 50, 1, 328, DateTimeKind.Local).AddTicks(4953), 0m, 0m, false, 16m, 11, 2, 1, 5m, 8m, null },
                    { 4, new DateTime(2026, 2, 4, 7, 50, 1, 328, DateTimeKind.Local).AddTicks(4953), 0m, 0m, false, 45m, 3, 1, 2, 35m, 45m, null },
                    { 5, new DateTime(2026, 2, 4, 7, 50, 1, 328, DateTimeKind.Local).AddTicks(4953), 0m, 0m, false, 10m, 8, 2, 2, 3m, 5m, null },
                    { 6, new DateTime(2026, 2, 3, 6, 50, 1, 328, DateTimeKind.Local).AddTicks(4953), 0m, 0m, false, 40m, 15, 1, 3, 30m, 40m, null },
                    { 7, new DateTime(2026, 2, 3, 6, 50, 1, 328, DateTimeKind.Local).AddTicks(4953), 0m, 0m, false, 10m, 12, 1, 3, 6m, 10m, null },
                    { 8, new DateTime(2026, 2, 3, 6, 50, 1, 328, DateTimeKind.Local).AddTicks(4953), 0m, 0m, false, 18m, 7, 1, 3, 12m, 18m, null }
                });

            migrationBuilder.CreateIndex(
                name: "IX_CustomerLoyalties_CustomerId",
                table: "CustomerLoyalties",
                column: "CustomerId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Expenses_ExpenseDate",
                table: "Expenses",
                column: "ExpenseDate");

            migrationBuilder.CreateIndex(
                name: "IX_Expenses_UserId",
                table: "Expenses",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_LoyaltyTransactions_CustomerLoyaltyId",
                table: "LoyaltyTransactions",
                column: "CustomerLoyaltyId");

            migrationBuilder.CreateIndex(
                name: "IX_LoyaltyTransactions_SaleId",
                table: "LoyaltyTransactions",
                column: "SaleId");

            migrationBuilder.CreateIndex(
                name: "IX_Products_Barcode",
                table: "Products",
                column: "Barcode");

            migrationBuilder.CreateIndex(
                name: "IX_Products_CategoryId",
                table: "Products",
                column: "CategoryId");

            migrationBuilder.CreateIndex(
                name: "IX_Products_Name",
                table: "Products",
                column: "Name");

            migrationBuilder.CreateIndex(
                name: "IX_Products_SupplierId",
                table: "Products",
                column: "SupplierId");

            migrationBuilder.CreateIndex(
                name: "IX_PurchaseOrders_OrderNumber",
                table: "PurchaseOrders",
                column: "OrderNumber",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_PurchaseOrders_SupplierId",
                table: "PurchaseOrders",
                column: "SupplierId");

            migrationBuilder.CreateIndex(
                name: "IX_ReturnDetails_ProductId",
                table: "ReturnDetails",
                column: "ProductId");

            migrationBuilder.CreateIndex(
                name: "IX_ReturnDetails_ReturnId",
                table: "ReturnDetails",
                column: "ReturnId");

            migrationBuilder.CreateIndex(
                name: "IX_Returns_CustomerId",
                table: "Returns",
                column: "CustomerId");

            migrationBuilder.CreateIndex(
                name: "IX_Returns_ProcessedByUserId",
                table: "Returns",
                column: "ProcessedByUserId");

            migrationBuilder.CreateIndex(
                name: "IX_Returns_SaleId",
                table: "Returns",
                column: "SaleId");

            migrationBuilder.CreateIndex(
                name: "IX_SaleDetails_ProductId",
                table: "SaleDetails",
                column: "ProductId");

            migrationBuilder.CreateIndex(
                name: "IX_SaleDetails_SaleId",
                table: "SaleDetails",
                column: "SaleId");

            migrationBuilder.CreateIndex(
                name: "IX_Sales_CustomerId",
                table: "Sales",
                column: "CustomerId");

            migrationBuilder.CreateIndex(
                name: "IX_Sales_InvoiceNumber",
                table: "Sales",
                column: "InvoiceNumber",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Sales_SaleDate",
                table: "Sales",
                column: "SaleDate");

            migrationBuilder.CreateIndex(
                name: "IX_Sales_ShiftId",
                table: "Sales",
                column: "ShiftId");

            migrationBuilder.CreateIndex(
                name: "IX_Sales_UserId",
                table: "Sales",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_Shifts_UserId",
                table: "Shifts",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_StockMovements_MovementDate",
                table: "StockMovements",
                column: "MovementDate");

            migrationBuilder.CreateIndex(
                name: "IX_StockMovements_ProductId",
                table: "StockMovements",
                column: "ProductId");

            migrationBuilder.CreateIndex(
                name: "IX_Users_Username",
                table: "Users",
                column: "Username",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Expenses");

            migrationBuilder.DropTable(
                name: "LoyaltyTransactions");

            migrationBuilder.DropTable(
                name: "PurchaseOrders");

            migrationBuilder.DropTable(
                name: "ReturnDetails");

            migrationBuilder.DropTable(
                name: "SaleDetails");

            migrationBuilder.DropTable(
                name: "StockMovements");

            migrationBuilder.DropTable(
                name: "CustomerLoyalties");

            migrationBuilder.DropTable(
                name: "Returns");

            migrationBuilder.DropTable(
                name: "Products");

            migrationBuilder.DropTable(
                name: "Sales");

            migrationBuilder.DropTable(
                name: "Categories");

            migrationBuilder.DropTable(
                name: "Suppliers");

            migrationBuilder.DropTable(
                name: "Customers");

            migrationBuilder.DropTable(
                name: "Shifts");

            migrationBuilder.DropTable(
                name: "Users");
        }
    }
}
