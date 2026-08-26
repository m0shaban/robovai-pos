using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace SmartPOS.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddEnterpriseRBACAndAudit : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Sales_Tables_TableId",
                table: "Sales");

            migrationBuilder.DropIndex(
                name: "IX_Products_Barcode",
                table: "Products");

            migrationBuilder.DropPrimaryKey(
                name: "PK_Tables",
                table: "Tables");







































































































            migrationBuilder.RenameTable(
                name: "Tables",
                newName: "Table");

            migrationBuilder.AddColumn<string>(
                name: "AdminPin",
                table: "Users",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<long>(
                name: "Permissions",
                table: "Users",
                type: "INTEGER",
                nullable: false,
                defaultValue: 0L);

            migrationBuilder.AddPrimaryKey(
                name: "PK_Table",
                table: "Table",
                column: "Id");

            migrationBuilder.CreateTable(
                name: "AuditLogs",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    UserId = table.Column<int>(type: "INTEGER", nullable: false),
                    ActionType = table.Column<string>(type: "TEXT", nullable: false),
                    Details = table.Column<string>(type: "TEXT", nullable: false),
                    AuthorizedByAdminId = table.Column<int>(type: "INTEGER", nullable: true),
                    Timestamp = table.Column<DateTime>(type: "TEXT", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "TEXT", nullable: true),
                    IsDeleted = table.Column<bool>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AuditLogs", x => x.Id);
                    table.ForeignKey(
                        name: "FK_AuditLogs_Users_AuthorizedByAdminId",
                        column: x => x.AuthorizedByAdminId,
                        principalTable: "Users",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_AuditLogs_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Products_Barcode",
                table: "Products",
                column: "Barcode",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_AuditLogs_AuthorizedByAdminId",
                table: "AuditLogs",
                column: "AuthorizedByAdminId");

            migrationBuilder.CreateIndex(
                name: "IX_AuditLogs_UserId",
                table: "AuditLogs",
                column: "UserId");

            migrationBuilder.AddForeignKey(
                name: "FK_Sales_Table_TableId",
                table: "Sales",
                column: "TableId",
                principalTable: "Table",
                principalColumn: "Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Sales_Table_TableId",
                table: "Sales");

            migrationBuilder.DropTable(
                name: "AuditLogs");

            migrationBuilder.DropIndex(
                name: "IX_Products_Barcode",
                table: "Products");

            migrationBuilder.DropPrimaryKey(
                name: "PK_Table",
                table: "Table");

            migrationBuilder.DropColumn(
                name: "AdminPin",
                table: "Users");

            migrationBuilder.DropColumn(
                name: "Permissions",
                table: "Users");

            migrationBuilder.RenameTable(
                name: "Table",
                newName: "Tables");

            migrationBuilder.AddPrimaryKey(
                name: "PK_Tables",
                table: "Tables",
                column: "Id");

            migrationBuilder.InsertData(
                table: "Categories",
                columns: new[] { "Id", "ColorCode", "CreatedAt", "Description", "IconName", "IsActive", "IsDeleted", "Name", "UpdatedAt" },
                values: new object[,]
                {
                    { 1, "#4CAF50", new DateTime(2026, 2, 7, 14, 56, 25, 397, DateTimeKind.Local).AddTicks(8076), "المواد الغذائية والمشروبات", null, true, false, "مواد غذائية", null },
                    { 2, "#2196F3", new DateTime(2026, 2, 7, 14, 56, 25, 397, DateTimeKind.Local).AddTicks(8076), "المشروبات الساخنة والباردة", null, true, false, "مشروبات", null },
                    { 3, "#FF9800", new DateTime(2026, 2, 7, 14, 56, 25, 397, DateTimeKind.Local).AddTicks(8076), "الحلويات والشوكولاتة", null, true, false, "حلويات", null },
                    { 4, "#9C27B0", new DateTime(2026, 2, 7, 14, 56, 25, 397, DateTimeKind.Local).AddTicks(8076), "منتجات الألبان والأجبان", null, true, false, "ألبان", null },
                    { 5, "#F44336", new DateTime(2026, 2, 7, 14, 56, 25, 397, DateTimeKind.Local).AddTicks(8076), "الأطعمة المعلبة", null, true, false, "معلبات", null },
                    { 6, "#795548", new DateTime(2026, 2, 7, 14, 56, 25, 397, DateTimeKind.Local).AddTicks(8076), "وجبات مطعم وكافيه", null, true, false, "وجبات", null }
                });

            migrationBuilder.InsertData(
                table: "Customers",
                columns: new[] { "Id", "Address", "Birthdate", "CreatedAt", "CreditLimit", "CurrentDebt", "Email", "IsActive", "IsDeleted", "Name", "Notes", "Phone", "UpdatedAt" },
                values: new object[,]
                {
                    { 1, "--", null, new DateTime(2026, 2, 7, 14, 56, 25, 397, DateTimeKind.Local).AddTicks(8076), 0m, 0m, null, true, false, "عميل نقدي", null, "0100000000", null },
                    { 2, "القاهرة", null, new DateTime(2026, 2, 7, 14, 56, 25, 397, DateTimeKind.Local).AddTicks(8076), 5000m, 0m, "sales@alhuda.com", true, false, "شركة الهدى", null, "0101234567", null }
                });

            migrationBuilder.InsertData(
                table: "Suppliers",
                columns: new[] { "Id", "Address", "ContactPerson", "CreatedAt", "DebtAmount", "Email", "IsActive", "IsDeleted", "Name", "Phone", "UpdatedAt" },
                values: new object[,]
                {
                    { 1, null, null, new DateTime(2026, 2, 7, 14, 56, 25, 397, DateTimeKind.Local).AddTicks(8076), 0m, "info@egyptfood.com", true, false, "شركة الأغذية المصرية", "0123456789", null },
                    { 2, null, null, new DateTime(2026, 2, 7, 14, 56, 25, 397, DateTimeKind.Local).AddTicks(8076), 0m, "sales@drinks.com", true, false, "مورد المشروبات الوطنية", "0111222333", null }
                });

            migrationBuilder.InsertData(
                table: "Users",
                columns: new[] { "Id", "CreatedAt", "Email", "FullName", "IsActive", "IsDeleted", "LastLogin", "PasswordHash", "Phone", "Role", "UpdatedAt", "Username" },
                values: new object[,]
                {
                    { 1, new DateTime(2026, 2, 7, 14, 56, 25, 397, DateTimeKind.Local).AddTicks(8076), null, "مدير النظام", true, false, null, "$2a$11$X7X8X9X0X1X2X3X4X5X6X7X8X9X0X1X2X3X4X5X6X7X8X9X0X1X2", null, 1, null, "admin" },
                    { 2, new DateTime(2026, 2, 7, 14, 56, 25, 397, DateTimeKind.Local).AddTicks(8076), null, "أحمد محمود - كاشير", true, false, null, "$2a$11$X7X8X9X0X1X2X3X4X5X6X7X8X9X0X1X2X3X4X5X6X7X8X9X0X1X2", null, 3, null, "cashier1" }
                });

            migrationBuilder.InsertData(
                table: "Expenses",
                columns: new[] { "Id", "Amount", "Category", "CreatedAt", "Description", "ExpenseDate", "IsDeleted", "Notes", "Receipt", "UpdatedAt", "UserId" },
                values: new object[,]
                {
                    { 1, 500.00m, 2, new DateTime(2026, 2, 2, 14, 56, 25, 397, DateTimeKind.Local).AddTicks(8076), "فاتورة كهرباء - يناير", new DateTime(2026, 2, 2, 14, 56, 25, 397, DateTimeKind.Local).AddTicks(8076), false, null, null, null, 1 },
                    { 2, 350.00m, 5, new DateTime(2026, 2, 4, 14, 56, 25, 397, DateTimeKind.Local).AddTicks(8076), "صيانة ثلاجة العرض", new DateTime(2026, 2, 4, 14, 56, 25, 397, DateTimeKind.Local).AddTicks(8076), false, null, null, null, 1 },
                    { 3, 1500.00m, 3, new DateTime(2026, 2, 5, 14, 56, 25, 397, DateTimeKind.Local).AddTicks(8076), "مرتب عامل نظافة", new DateTime(2026, 2, 5, 14, 56, 25, 397, DateTimeKind.Local).AddTicks(8076), false, null, null, null, 1 },
                    { 4, 150.00m, 4, new DateTime(2026, 2, 6, 14, 56, 25, 397, DateTimeKind.Local).AddTicks(8076), "أكياس بلاستيك", new DateTime(2026, 2, 6, 14, 56, 25, 397, DateTimeKind.Local).AddTicks(8076), false, null, null, null, 1 }
                });

            migrationBuilder.InsertData(
                table: "Products",
                columns: new[] { "Id", "Barcode", "CategoryId", "CreatedAt", "Description", "ImagePath", "IsActive", "IsDeleted", "MinStockLevel", "Name", "PurchasePrice", "SellingPrice", "Stock", "SupplierId", "Unit", "UpdatedAt" },
                values: new object[,]
                {
                    { 1, "1001", 1, new DateTime(2026, 2, 7, 14, 56, 25, 397, DateTimeKind.Local).AddTicks(8076), null, null, true, false, 10, "أرز أبيض - كيس 1 كجم", 15.00m, 20.00m, 50, 1, 4, null },
                    { 2, "1002", 1, new DateTime(2026, 2, 7, 14, 56, 25, 397, DateTimeKind.Local).AddTicks(8076), null, null, true, false, 15, "سكر أبيض - كيس 1 كجم", 18.00m, 25.00m, 45, 1, 4, null },
                    { 3, "1003", 1, new DateTime(2026, 2, 7, 14, 56, 25, 397, DateTimeKind.Local).AddTicks(8076), null, null, true, false, 10, "زيت طعام - زجاجة 1 لتر", 35.00m, 45.00m, 30, 1, 5, null },
                    { 4, "1004", 1, new DateTime(2026, 2, 7, 14, 56, 25, 397, DateTimeKind.Local).AddTicks(8076), null, null, true, false, 20, "مكرونة إسباجتي - علبة 500جم", 8.00m, 12.00m, 60, 1, 2, null },
                    { 5, "1005", 1, new DateTime(2026, 2, 7, 14, 56, 25, 397, DateTimeKind.Local).AddTicks(8076), null, null, true, false, 10, "طحين أبيض - كيس 1 كجم", 10.00m, 15.00m, 8, 1, 4, null },
                    { 6, "2001", 2, new DateTime(2026, 2, 7, 14, 56, 25, 397, DateTimeKind.Local).AddTicks(8076), null, null, true, false, 30, "كوكاكولا - علبة 330 مل", 4.00m, 7.00m, 100, 2, 1, null },
                    { 7, "2002", 2, new DateTime(2026, 2, 7, 14, 56, 25, 397, DateTimeKind.Local).AddTicks(8076), null, null, true, false, 15, "عصير برتقال - عبوة 1 لتر", 12.00m, 18.00m, 40, 2, 5, null },
                    { 8, "2003", 2, new DateTime(2026, 2, 7, 14, 56, 25, 397, DateTimeKind.Local).AddTicks(8076), null, null, true, false, 25, "ماء معدني - زجاجة 1.5 لتر", 3.00m, 5.00m, 80, 2, 5, null },
                    { 9, "2004", 2, new DateTime(2026, 2, 7, 14, 56, 25, 397, DateTimeKind.Local).AddTicks(8076), null, null, true, false, 10, "شاي ليبتون - علبة 100 كيس", 25.00m, 35.00m, 5, 2, 2, null },
                    { 10, "2005", 2, new DateTime(2026, 2, 7, 14, 56, 25, 397, DateTimeKind.Local).AddTicks(8076), null, null, true, false, 8, "قهوة نسكافيه - برطمان 200 جم", 40.00m, 55.00m, 25, 2, 1, null },
                    { 11, "3001", 3, new DateTime(2026, 2, 7, 14, 56, 25, 397, DateTimeKind.Local).AddTicks(8076), null, null, true, false, 20, "شوكولاتة كيت كات", 5.00m, 8.00m, 70, 1, 1, null },
                    { 12, "3002", 3, new DateTime(2026, 2, 7, 14, 56, 25, 397, DateTimeKind.Local).AddTicks(8076), null, null, true, false, 15, "بسكويت أوريو", 6.00m, 10.00m, 55, 1, 2, null },
                    { 13, "3003", 3, new DateTime(2026, 2, 7, 14, 56, 25, 397, DateTimeKind.Local).AddTicks(8076), null, null, true, false, 30, "حلوى جيلي - كيس صغير", 2.00m, 4.00m, 90, 1, 1, null },
                    { 14, "4001", 4, new DateTime(2026, 2, 7, 14, 56, 25, 397, DateTimeKind.Local).AddTicks(8076), null, null, true, false, 15, "لبن كامل الدسم - كرتونة 1 لتر", 15.00m, 20.00m, 35, 1, 5, null },
                    { 15, "4002", 4, new DateTime(2026, 2, 7, 14, 56, 25, 397, DateTimeKind.Local).AddTicks(8076), null, null, true, false, 10, "جبنة بيضاء - علبة 500 جم", 30.00m, 40.00m, 20, 1, 2, null },
                    { 16, "4003", 4, new DateTime(2026, 2, 7, 14, 56, 25, 397, DateTimeKind.Local).AddTicks(8076), null, null, true, false, 20, "زبادي فواكه - علبة صغيرة", 4.00m, 6.00m, 45, 1, 1, null },
                    { 17, "5001", 5, new DateTime(2026, 2, 7, 14, 56, 25, 397, DateTimeKind.Local).AddTicks(8076), null, null, true, false, 15, "تونة - علبة متوسطة", 12.00m, 18.00m, 40, 1, 2, null },
                    { 18, "5002", 5, new DateTime(2026, 2, 7, 14, 56, 25, 397, DateTimeKind.Local).AddTicks(8076), null, null, true, false, 20, "فول مدمس - علبة 400 جم", 8.00m, 12.00m, 50, 1, 2, null },
                    { 19, "5003", 5, new DateTime(2026, 2, 7, 14, 56, 25, 397, DateTimeKind.Local).AddTicks(8076), null, null, true, false, 10, "طماطم معجون - علبة 400 جم", 7.00m, 10.00m, 3, 1, 2, null },
                    { 20, "5004", 5, new DateTime(2026, 2, 7, 14, 56, 25, 397, DateTimeKind.Local).AddTicks(8076), null, null, true, false, 10, "ذرة حلوة - علبة 340 جم", 9.00m, 13.00m, 6, 1, 2, null },
                    { 21, "6001", 6, new DateTime(2026, 2, 7, 14, 56, 25, 397, DateTimeKind.Local).AddTicks(8076), null, null, true, false, 10, "برجر كلاسيك", 25.00m, 40.00m, 30, 1, 1, null },
                    { 22, "6002", 6, new DateTime(2026, 2, 7, 14, 56, 25, 397, DateTimeKind.Local).AddTicks(8076), null, null, true, false, 8, "بيتزا مارجريتا", 35.00m, 55.00m, 20, 1, 1, null },
                    { 23, "6003", 6, new DateTime(2026, 2, 7, 14, 56, 25, 397, DateTimeKind.Local).AddTicks(8076), null, null, true, false, 15, "ساندوتش شاورما", 18.00m, 30.00m, 40, 1, 1, null },
                    { 24, "6004", 6, new DateTime(2026, 2, 7, 14, 56, 25, 397, DateTimeKind.Local).AddTicks(8076), null, null, true, false, 10, "وجبة فطار", 20.00m, 35.00m, 25, 1, 1, null }
                });

            migrationBuilder.InsertData(
                table: "Sales",
                columns: new[] { "Id", "AmountPaid", "ChangeAmount", "CreatedAt", "CustomerId", "DiscountAmount", "DiscountPercentage", "InvoiceNumber", "IsDeleted", "IsPrinted", "LoyaltyPointsEarned", "LoyaltyPointsRedeemed", "Notes", "OrderType", "PaymentMethod", "QRCode", "SaleDate", "ShiftId", "Status", "Subtotal", "TableId", "TaxAmount", "TotalAmount", "UpdatedAt", "UserId" },
                values: new object[,]
                {
                    { 1, 70m, 0m, new DateTime(2026, 2, 7, 13, 56, 25, 397, DateTimeKind.Local).AddTicks(8076), 1, 0m, 0m, "INV-20260205-0001", false, false, null, null, null, 1, 1, null, new DateTime(2026, 2, 7, 13, 56, 25, 397, DateTimeKind.Local).AddTicks(8076), null, 2, 70m, null, 0m, 70m, null, 2 },
                    { 2, 50m, 0m, new DateTime(2026, 2, 6, 12, 56, 25, 397, DateTimeKind.Local).AddTicks(8076), 1, 5m, 0m, "INV-20260204-0002", false, false, null, null, null, 1, 1, null, new DateTime(2026, 2, 6, 12, 56, 25, 397, DateTimeKind.Local).AddTicks(8076), null, 2, 55m, null, 0m, 50m, null, 2 },
                    { 3, 68m, 0m, new DateTime(2026, 2, 5, 11, 56, 25, 397, DateTimeKind.Local).AddTicks(8076), 2, 0m, 0m, "INV-20260203-0003", false, false, null, null, null, 1, 2, null, new DateTime(2026, 2, 5, 11, 56, 25, 397, DateTimeKind.Local).AddTicks(8076), null, 2, 68m, null, 0m, 68m, null, 2 }
                });

            migrationBuilder.InsertData(
                table: "SaleDetails",
                columns: new[] { "Id", "CreatedAt", "DiscountAmount", "DiscountPercentage", "IsDeleted", "LineTotal", "ProductId", "Quantity", "SaleId", "UnitCost", "UnitPrice", "UpdatedAt" },
                values: new object[,]
                {
                    { 1, new DateTime(2026, 2, 7, 13, 56, 25, 397, DateTimeKind.Local).AddTicks(8076), 0m, 0m, false, 40m, 1, 2, 1, 15m, 20m, null },
                    { 2, new DateTime(2026, 2, 7, 13, 56, 25, 397, DateTimeKind.Local).AddTicks(8076), 0m, 0m, false, 14m, 6, 2, 1, 4m, 7m, null },
                    { 3, new DateTime(2026, 2, 7, 13, 56, 25, 397, DateTimeKind.Local).AddTicks(8076), 0m, 0m, false, 16m, 11, 2, 1, 5m, 8m, null },
                    { 4, new DateTime(2026, 2, 6, 12, 56, 25, 397, DateTimeKind.Local).AddTicks(8076), 0m, 0m, false, 45m, 3, 1, 2, 35m, 45m, null },
                    { 5, new DateTime(2026, 2, 6, 12, 56, 25, 397, DateTimeKind.Local).AddTicks(8076), 0m, 0m, false, 10m, 8, 2, 2, 3m, 5m, null },
                    { 6, new DateTime(2026, 2, 5, 11, 56, 25, 397, DateTimeKind.Local).AddTicks(8076), 0m, 0m, false, 40m, 15, 1, 3, 30m, 40m, null },
                    { 7, new DateTime(2026, 2, 5, 11, 56, 25, 397, DateTimeKind.Local).AddTicks(8076), 0m, 0m, false, 10m, 12, 1, 3, 6m, 10m, null },
                    { 8, new DateTime(2026, 2, 5, 11, 56, 25, 397, DateTimeKind.Local).AddTicks(8076), 0m, 0m, false, 18m, 7, 1, 3, 12m, 18m, null }
                });

            migrationBuilder.CreateIndex(
                name: "IX_Products_Barcode",
                table: "Products",
                column: "Barcode");

            migrationBuilder.AddForeignKey(
                name: "FK_Sales_Tables_TableId",
                table: "Sales",
                column: "TableId",
                principalTable: "Tables",
                principalColumn: "Id");
        }
    }
}

