using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SmartPOS.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddSaleTableAndOrderType : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "OrderType",
                table: "Sales",
                type: "INTEGER",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "TableId",
                table: "Sales",
                type: "INTEGER",
                nullable: true);

            migrationBuilder.UpdateData(
                table: "Categories",
                keyColumn: "Id",
                keyValue: 1,
                column: "CreatedAt",
                value: new DateTime(2026, 2, 5, 10, 42, 45, 202, DateTimeKind.Local).AddTicks(621));

            migrationBuilder.UpdateData(
                table: "Categories",
                keyColumn: "Id",
                keyValue: 2,
                column: "CreatedAt",
                value: new DateTime(2026, 2, 5, 10, 42, 45, 202, DateTimeKind.Local).AddTicks(621));

            migrationBuilder.UpdateData(
                table: "Categories",
                keyColumn: "Id",
                keyValue: 3,
                column: "CreatedAt",
                value: new DateTime(2026, 2, 5, 10, 42, 45, 202, DateTimeKind.Local).AddTicks(621));

            migrationBuilder.UpdateData(
                table: "Categories",
                keyColumn: "Id",
                keyValue: 4,
                column: "CreatedAt",
                value: new DateTime(2026, 2, 5, 10, 42, 45, 202, DateTimeKind.Local).AddTicks(621));

            migrationBuilder.UpdateData(
                table: "Categories",
                keyColumn: "Id",
                keyValue: 5,
                column: "CreatedAt",
                value: new DateTime(2026, 2, 5, 10, 42, 45, 202, DateTimeKind.Local).AddTicks(621));

            migrationBuilder.UpdateData(
                table: "Categories",
                keyColumn: "Id",
                keyValue: 6,
                column: "CreatedAt",
                value: new DateTime(2026, 2, 5, 10, 42, 45, 202, DateTimeKind.Local).AddTicks(621));

            migrationBuilder.UpdateData(
                table: "Customers",
                keyColumn: "Id",
                keyValue: 1,
                column: "CreatedAt",
                value: new DateTime(2026, 2, 5, 10, 42, 45, 202, DateTimeKind.Local).AddTicks(621));

            migrationBuilder.UpdateData(
                table: "Customers",
                keyColumn: "Id",
                keyValue: 2,
                column: "CreatedAt",
                value: new DateTime(2026, 2, 5, 10, 42, 45, 202, DateTimeKind.Local).AddTicks(621));

            migrationBuilder.UpdateData(
                table: "Expenses",
                keyColumn: "Id",
                keyValue: 1,
                columns: new[] { "CreatedAt", "ExpenseDate" },
                values: new object[] { new DateTime(2026, 1, 31, 10, 42, 45, 202, DateTimeKind.Local).AddTicks(621), new DateTime(2026, 1, 31, 10, 42, 45, 202, DateTimeKind.Local).AddTicks(621) });

            migrationBuilder.UpdateData(
                table: "Expenses",
                keyColumn: "Id",
                keyValue: 2,
                columns: new[] { "CreatedAt", "ExpenseDate" },
                values: new object[] { new DateTime(2026, 2, 2, 10, 42, 45, 202, DateTimeKind.Local).AddTicks(621), new DateTime(2026, 2, 2, 10, 42, 45, 202, DateTimeKind.Local).AddTicks(621) });

            migrationBuilder.UpdateData(
                table: "Expenses",
                keyColumn: "Id",
                keyValue: 3,
                columns: new[] { "CreatedAt", "ExpenseDate" },
                values: new object[] { new DateTime(2026, 2, 3, 10, 42, 45, 202, DateTimeKind.Local).AddTicks(621), new DateTime(2026, 2, 3, 10, 42, 45, 202, DateTimeKind.Local).AddTicks(621) });

            migrationBuilder.UpdateData(
                table: "Expenses",
                keyColumn: "Id",
                keyValue: 4,
                columns: new[] { "CreatedAt", "ExpenseDate" },
                values: new object[] { new DateTime(2026, 2, 4, 10, 42, 45, 202, DateTimeKind.Local).AddTicks(621), new DateTime(2026, 2, 4, 10, 42, 45, 202, DateTimeKind.Local).AddTicks(621) });

            migrationBuilder.UpdateData(
                table: "Products",
                keyColumn: "Id",
                keyValue: 1,
                column: "CreatedAt",
                value: new DateTime(2026, 2, 5, 10, 42, 45, 202, DateTimeKind.Local).AddTicks(621));

            migrationBuilder.UpdateData(
                table: "Products",
                keyColumn: "Id",
                keyValue: 2,
                column: "CreatedAt",
                value: new DateTime(2026, 2, 5, 10, 42, 45, 202, DateTimeKind.Local).AddTicks(621));

            migrationBuilder.UpdateData(
                table: "Products",
                keyColumn: "Id",
                keyValue: 3,
                column: "CreatedAt",
                value: new DateTime(2026, 2, 5, 10, 42, 45, 202, DateTimeKind.Local).AddTicks(621));

            migrationBuilder.UpdateData(
                table: "Products",
                keyColumn: "Id",
                keyValue: 4,
                column: "CreatedAt",
                value: new DateTime(2026, 2, 5, 10, 42, 45, 202, DateTimeKind.Local).AddTicks(621));

            migrationBuilder.UpdateData(
                table: "Products",
                keyColumn: "Id",
                keyValue: 5,
                column: "CreatedAt",
                value: new DateTime(2026, 2, 5, 10, 42, 45, 202, DateTimeKind.Local).AddTicks(621));

            migrationBuilder.UpdateData(
                table: "Products",
                keyColumn: "Id",
                keyValue: 6,
                column: "CreatedAt",
                value: new DateTime(2026, 2, 5, 10, 42, 45, 202, DateTimeKind.Local).AddTicks(621));

            migrationBuilder.UpdateData(
                table: "Products",
                keyColumn: "Id",
                keyValue: 7,
                column: "CreatedAt",
                value: new DateTime(2026, 2, 5, 10, 42, 45, 202, DateTimeKind.Local).AddTicks(621));

            migrationBuilder.UpdateData(
                table: "Products",
                keyColumn: "Id",
                keyValue: 8,
                column: "CreatedAt",
                value: new DateTime(2026, 2, 5, 10, 42, 45, 202, DateTimeKind.Local).AddTicks(621));

            migrationBuilder.UpdateData(
                table: "Products",
                keyColumn: "Id",
                keyValue: 9,
                column: "CreatedAt",
                value: new DateTime(2026, 2, 5, 10, 42, 45, 202, DateTimeKind.Local).AddTicks(621));

            migrationBuilder.UpdateData(
                table: "Products",
                keyColumn: "Id",
                keyValue: 10,
                column: "CreatedAt",
                value: new DateTime(2026, 2, 5, 10, 42, 45, 202, DateTimeKind.Local).AddTicks(621));

            migrationBuilder.UpdateData(
                table: "Products",
                keyColumn: "Id",
                keyValue: 11,
                column: "CreatedAt",
                value: new DateTime(2026, 2, 5, 10, 42, 45, 202, DateTimeKind.Local).AddTicks(621));

            migrationBuilder.UpdateData(
                table: "Products",
                keyColumn: "Id",
                keyValue: 12,
                column: "CreatedAt",
                value: new DateTime(2026, 2, 5, 10, 42, 45, 202, DateTimeKind.Local).AddTicks(621));

            migrationBuilder.UpdateData(
                table: "Products",
                keyColumn: "Id",
                keyValue: 13,
                column: "CreatedAt",
                value: new DateTime(2026, 2, 5, 10, 42, 45, 202, DateTimeKind.Local).AddTicks(621));

            migrationBuilder.UpdateData(
                table: "Products",
                keyColumn: "Id",
                keyValue: 14,
                column: "CreatedAt",
                value: new DateTime(2026, 2, 5, 10, 42, 45, 202, DateTimeKind.Local).AddTicks(621));

            migrationBuilder.UpdateData(
                table: "Products",
                keyColumn: "Id",
                keyValue: 15,
                column: "CreatedAt",
                value: new DateTime(2026, 2, 5, 10, 42, 45, 202, DateTimeKind.Local).AddTicks(621));

            migrationBuilder.UpdateData(
                table: "Products",
                keyColumn: "Id",
                keyValue: 16,
                column: "CreatedAt",
                value: new DateTime(2026, 2, 5, 10, 42, 45, 202, DateTimeKind.Local).AddTicks(621));

            migrationBuilder.UpdateData(
                table: "Products",
                keyColumn: "Id",
                keyValue: 17,
                column: "CreatedAt",
                value: new DateTime(2026, 2, 5, 10, 42, 45, 202, DateTimeKind.Local).AddTicks(621));

            migrationBuilder.UpdateData(
                table: "Products",
                keyColumn: "Id",
                keyValue: 18,
                column: "CreatedAt",
                value: new DateTime(2026, 2, 5, 10, 42, 45, 202, DateTimeKind.Local).AddTicks(621));

            migrationBuilder.UpdateData(
                table: "Products",
                keyColumn: "Id",
                keyValue: 19,
                column: "CreatedAt",
                value: new DateTime(2026, 2, 5, 10, 42, 45, 202, DateTimeKind.Local).AddTicks(621));

            migrationBuilder.UpdateData(
                table: "Products",
                keyColumn: "Id",
                keyValue: 20,
                column: "CreatedAt",
                value: new DateTime(2026, 2, 5, 10, 42, 45, 202, DateTimeKind.Local).AddTicks(621));

            migrationBuilder.UpdateData(
                table: "Products",
                keyColumn: "Id",
                keyValue: 21,
                column: "CreatedAt",
                value: new DateTime(2026, 2, 5, 10, 42, 45, 202, DateTimeKind.Local).AddTicks(621));

            migrationBuilder.UpdateData(
                table: "Products",
                keyColumn: "Id",
                keyValue: 22,
                column: "CreatedAt",
                value: new DateTime(2026, 2, 5, 10, 42, 45, 202, DateTimeKind.Local).AddTicks(621));

            migrationBuilder.UpdateData(
                table: "Products",
                keyColumn: "Id",
                keyValue: 23,
                column: "CreatedAt",
                value: new DateTime(2026, 2, 5, 10, 42, 45, 202, DateTimeKind.Local).AddTicks(621));

            migrationBuilder.UpdateData(
                table: "Products",
                keyColumn: "Id",
                keyValue: 24,
                column: "CreatedAt",
                value: new DateTime(2026, 2, 5, 10, 42, 45, 202, DateTimeKind.Local).AddTicks(621));

            migrationBuilder.UpdateData(
                table: "SaleDetails",
                keyColumn: "Id",
                keyValue: 1,
                column: "CreatedAt",
                value: new DateTime(2026, 2, 5, 9, 42, 45, 202, DateTimeKind.Local).AddTicks(621));

            migrationBuilder.UpdateData(
                table: "SaleDetails",
                keyColumn: "Id",
                keyValue: 2,
                column: "CreatedAt",
                value: new DateTime(2026, 2, 5, 9, 42, 45, 202, DateTimeKind.Local).AddTicks(621));

            migrationBuilder.UpdateData(
                table: "SaleDetails",
                keyColumn: "Id",
                keyValue: 3,
                column: "CreatedAt",
                value: new DateTime(2026, 2, 5, 9, 42, 45, 202, DateTimeKind.Local).AddTicks(621));

            migrationBuilder.UpdateData(
                table: "SaleDetails",
                keyColumn: "Id",
                keyValue: 4,
                column: "CreatedAt",
                value: new DateTime(2026, 2, 4, 8, 42, 45, 202, DateTimeKind.Local).AddTicks(621));

            migrationBuilder.UpdateData(
                table: "SaleDetails",
                keyColumn: "Id",
                keyValue: 5,
                column: "CreatedAt",
                value: new DateTime(2026, 2, 4, 8, 42, 45, 202, DateTimeKind.Local).AddTicks(621));

            migrationBuilder.UpdateData(
                table: "SaleDetails",
                keyColumn: "Id",
                keyValue: 6,
                column: "CreatedAt",
                value: new DateTime(2026, 2, 3, 7, 42, 45, 202, DateTimeKind.Local).AddTicks(621));

            migrationBuilder.UpdateData(
                table: "SaleDetails",
                keyColumn: "Id",
                keyValue: 7,
                column: "CreatedAt",
                value: new DateTime(2026, 2, 3, 7, 42, 45, 202, DateTimeKind.Local).AddTicks(621));

            migrationBuilder.UpdateData(
                table: "SaleDetails",
                keyColumn: "Id",
                keyValue: 8,
                column: "CreatedAt",
                value: new DateTime(2026, 2, 3, 7, 42, 45, 202, DateTimeKind.Local).AddTicks(621));

            migrationBuilder.UpdateData(
                table: "Sales",
                keyColumn: "Id",
                keyValue: 1,
                columns: new[] { "CreatedAt", "OrderType", "SaleDate", "TableId" },
                values: new object[] { new DateTime(2026, 2, 5, 9, 42, 45, 202, DateTimeKind.Local).AddTicks(621), 1, new DateTime(2026, 2, 5, 9, 42, 45, 202, DateTimeKind.Local).AddTicks(621), null });

            migrationBuilder.UpdateData(
                table: "Sales",
                keyColumn: "Id",
                keyValue: 2,
                columns: new[] { "CreatedAt", "OrderType", "SaleDate", "TableId" },
                values: new object[] { new DateTime(2026, 2, 4, 8, 42, 45, 202, DateTimeKind.Local).AddTicks(621), 1, new DateTime(2026, 2, 4, 8, 42, 45, 202, DateTimeKind.Local).AddTicks(621), null });

            migrationBuilder.UpdateData(
                table: "Sales",
                keyColumn: "Id",
                keyValue: 3,
                columns: new[] { "CreatedAt", "OrderType", "SaleDate", "TableId" },
                values: new object[] { new DateTime(2026, 2, 3, 7, 42, 45, 202, DateTimeKind.Local).AddTicks(621), 1, new DateTime(2026, 2, 3, 7, 42, 45, 202, DateTimeKind.Local).AddTicks(621), null });

            migrationBuilder.UpdateData(
                table: "Suppliers",
                keyColumn: "Id",
                keyValue: 1,
                column: "CreatedAt",
                value: new DateTime(2026, 2, 5, 10, 42, 45, 202, DateTimeKind.Local).AddTicks(621));

            migrationBuilder.UpdateData(
                table: "Suppliers",
                keyColumn: "Id",
                keyValue: 2,
                column: "CreatedAt",
                value: new DateTime(2026, 2, 5, 10, 42, 45, 202, DateTimeKind.Local).AddTicks(621));

            migrationBuilder.UpdateData(
                table: "Users",
                keyColumn: "Id",
                keyValue: 1,
                column: "CreatedAt",
                value: new DateTime(2026, 2, 5, 10, 42, 45, 202, DateTimeKind.Local).AddTicks(621));

            migrationBuilder.UpdateData(
                table: "Users",
                keyColumn: "Id",
                keyValue: 2,
                column: "CreatedAt",
                value: new DateTime(2026, 2, 5, 10, 42, 45, 202, DateTimeKind.Local).AddTicks(621));

            migrationBuilder.CreateIndex(
                name: "IX_Sales_TableId",
                table: "Sales",
                column: "TableId");

            migrationBuilder.AddForeignKey(
                name: "FK_Sales_Tables_TableId",
                table: "Sales",
                column: "TableId",
                principalTable: "Tables",
                principalColumn: "Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Sales_Tables_TableId",
                table: "Sales");

            migrationBuilder.DropIndex(
                name: "IX_Sales_TableId",
                table: "Sales");

            migrationBuilder.DropColumn(
                name: "OrderType",
                table: "Sales");

            migrationBuilder.DropColumn(
                name: "TableId",
                table: "Sales");

            migrationBuilder.UpdateData(
                table: "Categories",
                keyColumn: "Id",
                keyValue: 1,
                column: "CreatedAt",
                value: new DateTime(2026, 2, 5, 10, 33, 54, 653, DateTimeKind.Local).AddTicks(6861));

            migrationBuilder.UpdateData(
                table: "Categories",
                keyColumn: "Id",
                keyValue: 2,
                column: "CreatedAt",
                value: new DateTime(2026, 2, 5, 10, 33, 54, 653, DateTimeKind.Local).AddTicks(6861));

            migrationBuilder.UpdateData(
                table: "Categories",
                keyColumn: "Id",
                keyValue: 3,
                column: "CreatedAt",
                value: new DateTime(2026, 2, 5, 10, 33, 54, 653, DateTimeKind.Local).AddTicks(6861));

            migrationBuilder.UpdateData(
                table: "Categories",
                keyColumn: "Id",
                keyValue: 4,
                column: "CreatedAt",
                value: new DateTime(2026, 2, 5, 10, 33, 54, 653, DateTimeKind.Local).AddTicks(6861));

            migrationBuilder.UpdateData(
                table: "Categories",
                keyColumn: "Id",
                keyValue: 5,
                column: "CreatedAt",
                value: new DateTime(2026, 2, 5, 10, 33, 54, 653, DateTimeKind.Local).AddTicks(6861));

            migrationBuilder.UpdateData(
                table: "Categories",
                keyColumn: "Id",
                keyValue: 6,
                column: "CreatedAt",
                value: new DateTime(2026, 2, 5, 10, 33, 54, 653, DateTimeKind.Local).AddTicks(6861));

            migrationBuilder.UpdateData(
                table: "Customers",
                keyColumn: "Id",
                keyValue: 1,
                column: "CreatedAt",
                value: new DateTime(2026, 2, 5, 10, 33, 54, 653, DateTimeKind.Local).AddTicks(6861));

            migrationBuilder.UpdateData(
                table: "Customers",
                keyColumn: "Id",
                keyValue: 2,
                column: "CreatedAt",
                value: new DateTime(2026, 2, 5, 10, 33, 54, 653, DateTimeKind.Local).AddTicks(6861));

            migrationBuilder.UpdateData(
                table: "Expenses",
                keyColumn: "Id",
                keyValue: 1,
                columns: new[] { "CreatedAt", "ExpenseDate" },
                values: new object[] { new DateTime(2026, 1, 31, 10, 33, 54, 653, DateTimeKind.Local).AddTicks(6861), new DateTime(2026, 1, 31, 10, 33, 54, 653, DateTimeKind.Local).AddTicks(6861) });

            migrationBuilder.UpdateData(
                table: "Expenses",
                keyColumn: "Id",
                keyValue: 2,
                columns: new[] { "CreatedAt", "ExpenseDate" },
                values: new object[] { new DateTime(2026, 2, 2, 10, 33, 54, 653, DateTimeKind.Local).AddTicks(6861), new DateTime(2026, 2, 2, 10, 33, 54, 653, DateTimeKind.Local).AddTicks(6861) });

            migrationBuilder.UpdateData(
                table: "Expenses",
                keyColumn: "Id",
                keyValue: 3,
                columns: new[] { "CreatedAt", "ExpenseDate" },
                values: new object[] { new DateTime(2026, 2, 3, 10, 33, 54, 653, DateTimeKind.Local).AddTicks(6861), new DateTime(2026, 2, 3, 10, 33, 54, 653, DateTimeKind.Local).AddTicks(6861) });

            migrationBuilder.UpdateData(
                table: "Expenses",
                keyColumn: "Id",
                keyValue: 4,
                columns: new[] { "CreatedAt", "ExpenseDate" },
                values: new object[] { new DateTime(2026, 2, 4, 10, 33, 54, 653, DateTimeKind.Local).AddTicks(6861), new DateTime(2026, 2, 4, 10, 33, 54, 653, DateTimeKind.Local).AddTicks(6861) });

            migrationBuilder.UpdateData(
                table: "Products",
                keyColumn: "Id",
                keyValue: 1,
                column: "CreatedAt",
                value: new DateTime(2026, 2, 5, 10, 33, 54, 653, DateTimeKind.Local).AddTicks(6861));

            migrationBuilder.UpdateData(
                table: "Products",
                keyColumn: "Id",
                keyValue: 2,
                column: "CreatedAt",
                value: new DateTime(2026, 2, 5, 10, 33, 54, 653, DateTimeKind.Local).AddTicks(6861));

            migrationBuilder.UpdateData(
                table: "Products",
                keyColumn: "Id",
                keyValue: 3,
                column: "CreatedAt",
                value: new DateTime(2026, 2, 5, 10, 33, 54, 653, DateTimeKind.Local).AddTicks(6861));

            migrationBuilder.UpdateData(
                table: "Products",
                keyColumn: "Id",
                keyValue: 4,
                column: "CreatedAt",
                value: new DateTime(2026, 2, 5, 10, 33, 54, 653, DateTimeKind.Local).AddTicks(6861));

            migrationBuilder.UpdateData(
                table: "Products",
                keyColumn: "Id",
                keyValue: 5,
                column: "CreatedAt",
                value: new DateTime(2026, 2, 5, 10, 33, 54, 653, DateTimeKind.Local).AddTicks(6861));

            migrationBuilder.UpdateData(
                table: "Products",
                keyColumn: "Id",
                keyValue: 6,
                column: "CreatedAt",
                value: new DateTime(2026, 2, 5, 10, 33, 54, 653, DateTimeKind.Local).AddTicks(6861));

            migrationBuilder.UpdateData(
                table: "Products",
                keyColumn: "Id",
                keyValue: 7,
                column: "CreatedAt",
                value: new DateTime(2026, 2, 5, 10, 33, 54, 653, DateTimeKind.Local).AddTicks(6861));

            migrationBuilder.UpdateData(
                table: "Products",
                keyColumn: "Id",
                keyValue: 8,
                column: "CreatedAt",
                value: new DateTime(2026, 2, 5, 10, 33, 54, 653, DateTimeKind.Local).AddTicks(6861));

            migrationBuilder.UpdateData(
                table: "Products",
                keyColumn: "Id",
                keyValue: 9,
                column: "CreatedAt",
                value: new DateTime(2026, 2, 5, 10, 33, 54, 653, DateTimeKind.Local).AddTicks(6861));

            migrationBuilder.UpdateData(
                table: "Products",
                keyColumn: "Id",
                keyValue: 10,
                column: "CreatedAt",
                value: new DateTime(2026, 2, 5, 10, 33, 54, 653, DateTimeKind.Local).AddTicks(6861));

            migrationBuilder.UpdateData(
                table: "Products",
                keyColumn: "Id",
                keyValue: 11,
                column: "CreatedAt",
                value: new DateTime(2026, 2, 5, 10, 33, 54, 653, DateTimeKind.Local).AddTicks(6861));

            migrationBuilder.UpdateData(
                table: "Products",
                keyColumn: "Id",
                keyValue: 12,
                column: "CreatedAt",
                value: new DateTime(2026, 2, 5, 10, 33, 54, 653, DateTimeKind.Local).AddTicks(6861));

            migrationBuilder.UpdateData(
                table: "Products",
                keyColumn: "Id",
                keyValue: 13,
                column: "CreatedAt",
                value: new DateTime(2026, 2, 5, 10, 33, 54, 653, DateTimeKind.Local).AddTicks(6861));

            migrationBuilder.UpdateData(
                table: "Products",
                keyColumn: "Id",
                keyValue: 14,
                column: "CreatedAt",
                value: new DateTime(2026, 2, 5, 10, 33, 54, 653, DateTimeKind.Local).AddTicks(6861));

            migrationBuilder.UpdateData(
                table: "Products",
                keyColumn: "Id",
                keyValue: 15,
                column: "CreatedAt",
                value: new DateTime(2026, 2, 5, 10, 33, 54, 653, DateTimeKind.Local).AddTicks(6861));

            migrationBuilder.UpdateData(
                table: "Products",
                keyColumn: "Id",
                keyValue: 16,
                column: "CreatedAt",
                value: new DateTime(2026, 2, 5, 10, 33, 54, 653, DateTimeKind.Local).AddTicks(6861));

            migrationBuilder.UpdateData(
                table: "Products",
                keyColumn: "Id",
                keyValue: 17,
                column: "CreatedAt",
                value: new DateTime(2026, 2, 5, 10, 33, 54, 653, DateTimeKind.Local).AddTicks(6861));

            migrationBuilder.UpdateData(
                table: "Products",
                keyColumn: "Id",
                keyValue: 18,
                column: "CreatedAt",
                value: new DateTime(2026, 2, 5, 10, 33, 54, 653, DateTimeKind.Local).AddTicks(6861));

            migrationBuilder.UpdateData(
                table: "Products",
                keyColumn: "Id",
                keyValue: 19,
                column: "CreatedAt",
                value: new DateTime(2026, 2, 5, 10, 33, 54, 653, DateTimeKind.Local).AddTicks(6861));

            migrationBuilder.UpdateData(
                table: "Products",
                keyColumn: "Id",
                keyValue: 20,
                column: "CreatedAt",
                value: new DateTime(2026, 2, 5, 10, 33, 54, 653, DateTimeKind.Local).AddTicks(6861));

            migrationBuilder.UpdateData(
                table: "Products",
                keyColumn: "Id",
                keyValue: 21,
                column: "CreatedAt",
                value: new DateTime(2026, 2, 5, 10, 33, 54, 653, DateTimeKind.Local).AddTicks(6861));

            migrationBuilder.UpdateData(
                table: "Products",
                keyColumn: "Id",
                keyValue: 22,
                column: "CreatedAt",
                value: new DateTime(2026, 2, 5, 10, 33, 54, 653, DateTimeKind.Local).AddTicks(6861));

            migrationBuilder.UpdateData(
                table: "Products",
                keyColumn: "Id",
                keyValue: 23,
                column: "CreatedAt",
                value: new DateTime(2026, 2, 5, 10, 33, 54, 653, DateTimeKind.Local).AddTicks(6861));

            migrationBuilder.UpdateData(
                table: "Products",
                keyColumn: "Id",
                keyValue: 24,
                column: "CreatedAt",
                value: new DateTime(2026, 2, 5, 10, 33, 54, 653, DateTimeKind.Local).AddTicks(6861));

            migrationBuilder.UpdateData(
                table: "SaleDetails",
                keyColumn: "Id",
                keyValue: 1,
                column: "CreatedAt",
                value: new DateTime(2026, 2, 5, 9, 33, 54, 653, DateTimeKind.Local).AddTicks(6861));

            migrationBuilder.UpdateData(
                table: "SaleDetails",
                keyColumn: "Id",
                keyValue: 2,
                column: "CreatedAt",
                value: new DateTime(2026, 2, 5, 9, 33, 54, 653, DateTimeKind.Local).AddTicks(6861));

            migrationBuilder.UpdateData(
                table: "SaleDetails",
                keyColumn: "Id",
                keyValue: 3,
                column: "CreatedAt",
                value: new DateTime(2026, 2, 5, 9, 33, 54, 653, DateTimeKind.Local).AddTicks(6861));

            migrationBuilder.UpdateData(
                table: "SaleDetails",
                keyColumn: "Id",
                keyValue: 4,
                column: "CreatedAt",
                value: new DateTime(2026, 2, 4, 8, 33, 54, 653, DateTimeKind.Local).AddTicks(6861));

            migrationBuilder.UpdateData(
                table: "SaleDetails",
                keyColumn: "Id",
                keyValue: 5,
                column: "CreatedAt",
                value: new DateTime(2026, 2, 4, 8, 33, 54, 653, DateTimeKind.Local).AddTicks(6861));

            migrationBuilder.UpdateData(
                table: "SaleDetails",
                keyColumn: "Id",
                keyValue: 6,
                column: "CreatedAt",
                value: new DateTime(2026, 2, 3, 7, 33, 54, 653, DateTimeKind.Local).AddTicks(6861));

            migrationBuilder.UpdateData(
                table: "SaleDetails",
                keyColumn: "Id",
                keyValue: 7,
                column: "CreatedAt",
                value: new DateTime(2026, 2, 3, 7, 33, 54, 653, DateTimeKind.Local).AddTicks(6861));

            migrationBuilder.UpdateData(
                table: "SaleDetails",
                keyColumn: "Id",
                keyValue: 8,
                column: "CreatedAt",
                value: new DateTime(2026, 2, 3, 7, 33, 54, 653, DateTimeKind.Local).AddTicks(6861));

            migrationBuilder.UpdateData(
                table: "Sales",
                keyColumn: "Id",
                keyValue: 1,
                columns: new[] { "CreatedAt", "SaleDate" },
                values: new object[] { new DateTime(2026, 2, 5, 9, 33, 54, 653, DateTimeKind.Local).AddTicks(6861), new DateTime(2026, 2, 5, 9, 33, 54, 653, DateTimeKind.Local).AddTicks(6861) });

            migrationBuilder.UpdateData(
                table: "Sales",
                keyColumn: "Id",
                keyValue: 2,
                columns: new[] { "CreatedAt", "SaleDate" },
                values: new object[] { new DateTime(2026, 2, 4, 8, 33, 54, 653, DateTimeKind.Local).AddTicks(6861), new DateTime(2026, 2, 4, 8, 33, 54, 653, DateTimeKind.Local).AddTicks(6861) });

            migrationBuilder.UpdateData(
                table: "Sales",
                keyColumn: "Id",
                keyValue: 3,
                columns: new[] { "CreatedAt", "SaleDate" },
                values: new object[] { new DateTime(2026, 2, 3, 7, 33, 54, 653, DateTimeKind.Local).AddTicks(6861), new DateTime(2026, 2, 3, 7, 33, 54, 653, DateTimeKind.Local).AddTicks(6861) });

            migrationBuilder.UpdateData(
                table: "Suppliers",
                keyColumn: "Id",
                keyValue: 1,
                column: "CreatedAt",
                value: new DateTime(2026, 2, 5, 10, 33, 54, 653, DateTimeKind.Local).AddTicks(6861));

            migrationBuilder.UpdateData(
                table: "Suppliers",
                keyColumn: "Id",
                keyValue: 2,
                column: "CreatedAt",
                value: new DateTime(2026, 2, 5, 10, 33, 54, 653, DateTimeKind.Local).AddTicks(6861));

            migrationBuilder.UpdateData(
                table: "Users",
                keyColumn: "Id",
                keyValue: 1,
                column: "CreatedAt",
                value: new DateTime(2026, 2, 5, 10, 33, 54, 653, DateTimeKind.Local).AddTicks(6861));

            migrationBuilder.UpdateData(
                table: "Users",
                keyColumn: "Id",
                keyValue: 2,
                column: "CreatedAt",
                value: new DateTime(2026, 2, 5, 10, 33, 54, 653, DateTimeKind.Local).AddTicks(6861));
        }
    }
}
