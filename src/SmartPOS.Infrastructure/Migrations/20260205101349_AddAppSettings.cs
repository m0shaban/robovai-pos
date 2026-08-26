using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SmartPOS.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddAppSettings : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "AppSettings",
                columns: table => new
                {
                    Key = table.Column<string>(type: "TEXT", nullable: false),
                    Value = table.Column<string>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AppSettings", x => x.Key);
                });

            migrationBuilder.UpdateData(
                table: "Categories",
                keyColumn: "Id",
                keyValue: 1,
                column: "CreatedAt",
                value: new DateTime(2026, 2, 5, 12, 13, 48, 173, DateTimeKind.Local).AddTicks(7694));

            migrationBuilder.UpdateData(
                table: "Categories",
                keyColumn: "Id",
                keyValue: 2,
                column: "CreatedAt",
                value: new DateTime(2026, 2, 5, 12, 13, 48, 173, DateTimeKind.Local).AddTicks(7694));

            migrationBuilder.UpdateData(
                table: "Categories",
                keyColumn: "Id",
                keyValue: 3,
                column: "CreatedAt",
                value: new DateTime(2026, 2, 5, 12, 13, 48, 173, DateTimeKind.Local).AddTicks(7694));

            migrationBuilder.UpdateData(
                table: "Categories",
                keyColumn: "Id",
                keyValue: 4,
                column: "CreatedAt",
                value: new DateTime(2026, 2, 5, 12, 13, 48, 173, DateTimeKind.Local).AddTicks(7694));

            migrationBuilder.UpdateData(
                table: "Categories",
                keyColumn: "Id",
                keyValue: 5,
                column: "CreatedAt",
                value: new DateTime(2026, 2, 5, 12, 13, 48, 173, DateTimeKind.Local).AddTicks(7694));

            migrationBuilder.UpdateData(
                table: "Categories",
                keyColumn: "Id",
                keyValue: 6,
                column: "CreatedAt",
                value: new DateTime(2026, 2, 5, 12, 13, 48, 173, DateTimeKind.Local).AddTicks(7694));

            migrationBuilder.UpdateData(
                table: "Customers",
                keyColumn: "Id",
                keyValue: 1,
                column: "CreatedAt",
                value: new DateTime(2026, 2, 5, 12, 13, 48, 173, DateTimeKind.Local).AddTicks(7694));

            migrationBuilder.UpdateData(
                table: "Customers",
                keyColumn: "Id",
                keyValue: 2,
                column: "CreatedAt",
                value: new DateTime(2026, 2, 5, 12, 13, 48, 173, DateTimeKind.Local).AddTicks(7694));

            migrationBuilder.UpdateData(
                table: "Expenses",
                keyColumn: "Id",
                keyValue: 1,
                columns: new[] { "CreatedAt", "ExpenseDate" },
                values: new object[] { new DateTime(2026, 1, 31, 12, 13, 48, 173, DateTimeKind.Local).AddTicks(7694), new DateTime(2026, 1, 31, 12, 13, 48, 173, DateTimeKind.Local).AddTicks(7694) });

            migrationBuilder.UpdateData(
                table: "Expenses",
                keyColumn: "Id",
                keyValue: 2,
                columns: new[] { "CreatedAt", "ExpenseDate" },
                values: new object[] { new DateTime(2026, 2, 2, 12, 13, 48, 173, DateTimeKind.Local).AddTicks(7694), new DateTime(2026, 2, 2, 12, 13, 48, 173, DateTimeKind.Local).AddTicks(7694) });

            migrationBuilder.UpdateData(
                table: "Expenses",
                keyColumn: "Id",
                keyValue: 3,
                columns: new[] { "CreatedAt", "ExpenseDate" },
                values: new object[] { new DateTime(2026, 2, 3, 12, 13, 48, 173, DateTimeKind.Local).AddTicks(7694), new DateTime(2026, 2, 3, 12, 13, 48, 173, DateTimeKind.Local).AddTicks(7694) });

            migrationBuilder.UpdateData(
                table: "Expenses",
                keyColumn: "Id",
                keyValue: 4,
                columns: new[] { "CreatedAt", "ExpenseDate" },
                values: new object[] { new DateTime(2026, 2, 4, 12, 13, 48, 173, DateTimeKind.Local).AddTicks(7694), new DateTime(2026, 2, 4, 12, 13, 48, 173, DateTimeKind.Local).AddTicks(7694) });

            migrationBuilder.UpdateData(
                table: "Products",
                keyColumn: "Id",
                keyValue: 1,
                column: "CreatedAt",
                value: new DateTime(2026, 2, 5, 12, 13, 48, 173, DateTimeKind.Local).AddTicks(7694));

            migrationBuilder.UpdateData(
                table: "Products",
                keyColumn: "Id",
                keyValue: 2,
                column: "CreatedAt",
                value: new DateTime(2026, 2, 5, 12, 13, 48, 173, DateTimeKind.Local).AddTicks(7694));

            migrationBuilder.UpdateData(
                table: "Products",
                keyColumn: "Id",
                keyValue: 3,
                column: "CreatedAt",
                value: new DateTime(2026, 2, 5, 12, 13, 48, 173, DateTimeKind.Local).AddTicks(7694));

            migrationBuilder.UpdateData(
                table: "Products",
                keyColumn: "Id",
                keyValue: 4,
                column: "CreatedAt",
                value: new DateTime(2026, 2, 5, 12, 13, 48, 173, DateTimeKind.Local).AddTicks(7694));

            migrationBuilder.UpdateData(
                table: "Products",
                keyColumn: "Id",
                keyValue: 5,
                column: "CreatedAt",
                value: new DateTime(2026, 2, 5, 12, 13, 48, 173, DateTimeKind.Local).AddTicks(7694));

            migrationBuilder.UpdateData(
                table: "Products",
                keyColumn: "Id",
                keyValue: 6,
                column: "CreatedAt",
                value: new DateTime(2026, 2, 5, 12, 13, 48, 173, DateTimeKind.Local).AddTicks(7694));

            migrationBuilder.UpdateData(
                table: "Products",
                keyColumn: "Id",
                keyValue: 7,
                column: "CreatedAt",
                value: new DateTime(2026, 2, 5, 12, 13, 48, 173, DateTimeKind.Local).AddTicks(7694));

            migrationBuilder.UpdateData(
                table: "Products",
                keyColumn: "Id",
                keyValue: 8,
                column: "CreatedAt",
                value: new DateTime(2026, 2, 5, 12, 13, 48, 173, DateTimeKind.Local).AddTicks(7694));

            migrationBuilder.UpdateData(
                table: "Products",
                keyColumn: "Id",
                keyValue: 9,
                column: "CreatedAt",
                value: new DateTime(2026, 2, 5, 12, 13, 48, 173, DateTimeKind.Local).AddTicks(7694));

            migrationBuilder.UpdateData(
                table: "Products",
                keyColumn: "Id",
                keyValue: 10,
                column: "CreatedAt",
                value: new DateTime(2026, 2, 5, 12, 13, 48, 173, DateTimeKind.Local).AddTicks(7694));

            migrationBuilder.UpdateData(
                table: "Products",
                keyColumn: "Id",
                keyValue: 11,
                column: "CreatedAt",
                value: new DateTime(2026, 2, 5, 12, 13, 48, 173, DateTimeKind.Local).AddTicks(7694));

            migrationBuilder.UpdateData(
                table: "Products",
                keyColumn: "Id",
                keyValue: 12,
                column: "CreatedAt",
                value: new DateTime(2026, 2, 5, 12, 13, 48, 173, DateTimeKind.Local).AddTicks(7694));

            migrationBuilder.UpdateData(
                table: "Products",
                keyColumn: "Id",
                keyValue: 13,
                column: "CreatedAt",
                value: new DateTime(2026, 2, 5, 12, 13, 48, 173, DateTimeKind.Local).AddTicks(7694));

            migrationBuilder.UpdateData(
                table: "Products",
                keyColumn: "Id",
                keyValue: 14,
                column: "CreatedAt",
                value: new DateTime(2026, 2, 5, 12, 13, 48, 173, DateTimeKind.Local).AddTicks(7694));

            migrationBuilder.UpdateData(
                table: "Products",
                keyColumn: "Id",
                keyValue: 15,
                column: "CreatedAt",
                value: new DateTime(2026, 2, 5, 12, 13, 48, 173, DateTimeKind.Local).AddTicks(7694));

            migrationBuilder.UpdateData(
                table: "Products",
                keyColumn: "Id",
                keyValue: 16,
                column: "CreatedAt",
                value: new DateTime(2026, 2, 5, 12, 13, 48, 173, DateTimeKind.Local).AddTicks(7694));

            migrationBuilder.UpdateData(
                table: "Products",
                keyColumn: "Id",
                keyValue: 17,
                column: "CreatedAt",
                value: new DateTime(2026, 2, 5, 12, 13, 48, 173, DateTimeKind.Local).AddTicks(7694));

            migrationBuilder.UpdateData(
                table: "Products",
                keyColumn: "Id",
                keyValue: 18,
                column: "CreatedAt",
                value: new DateTime(2026, 2, 5, 12, 13, 48, 173, DateTimeKind.Local).AddTicks(7694));

            migrationBuilder.UpdateData(
                table: "Products",
                keyColumn: "Id",
                keyValue: 19,
                column: "CreatedAt",
                value: new DateTime(2026, 2, 5, 12, 13, 48, 173, DateTimeKind.Local).AddTicks(7694));

            migrationBuilder.UpdateData(
                table: "Products",
                keyColumn: "Id",
                keyValue: 20,
                column: "CreatedAt",
                value: new DateTime(2026, 2, 5, 12, 13, 48, 173, DateTimeKind.Local).AddTicks(7694));

            migrationBuilder.UpdateData(
                table: "Products",
                keyColumn: "Id",
                keyValue: 21,
                column: "CreatedAt",
                value: new DateTime(2026, 2, 5, 12, 13, 48, 173, DateTimeKind.Local).AddTicks(7694));

            migrationBuilder.UpdateData(
                table: "Products",
                keyColumn: "Id",
                keyValue: 22,
                column: "CreatedAt",
                value: new DateTime(2026, 2, 5, 12, 13, 48, 173, DateTimeKind.Local).AddTicks(7694));

            migrationBuilder.UpdateData(
                table: "Products",
                keyColumn: "Id",
                keyValue: 23,
                column: "CreatedAt",
                value: new DateTime(2026, 2, 5, 12, 13, 48, 173, DateTimeKind.Local).AddTicks(7694));

            migrationBuilder.UpdateData(
                table: "Products",
                keyColumn: "Id",
                keyValue: 24,
                column: "CreatedAt",
                value: new DateTime(2026, 2, 5, 12, 13, 48, 173, DateTimeKind.Local).AddTicks(7694));

            migrationBuilder.UpdateData(
                table: "SaleDetails",
                keyColumn: "Id",
                keyValue: 1,
                column: "CreatedAt",
                value: new DateTime(2026, 2, 5, 11, 13, 48, 173, DateTimeKind.Local).AddTicks(7694));

            migrationBuilder.UpdateData(
                table: "SaleDetails",
                keyColumn: "Id",
                keyValue: 2,
                column: "CreatedAt",
                value: new DateTime(2026, 2, 5, 11, 13, 48, 173, DateTimeKind.Local).AddTicks(7694));

            migrationBuilder.UpdateData(
                table: "SaleDetails",
                keyColumn: "Id",
                keyValue: 3,
                column: "CreatedAt",
                value: new DateTime(2026, 2, 5, 11, 13, 48, 173, DateTimeKind.Local).AddTicks(7694));

            migrationBuilder.UpdateData(
                table: "SaleDetails",
                keyColumn: "Id",
                keyValue: 4,
                column: "CreatedAt",
                value: new DateTime(2026, 2, 4, 10, 13, 48, 173, DateTimeKind.Local).AddTicks(7694));

            migrationBuilder.UpdateData(
                table: "SaleDetails",
                keyColumn: "Id",
                keyValue: 5,
                column: "CreatedAt",
                value: new DateTime(2026, 2, 4, 10, 13, 48, 173, DateTimeKind.Local).AddTicks(7694));

            migrationBuilder.UpdateData(
                table: "SaleDetails",
                keyColumn: "Id",
                keyValue: 6,
                column: "CreatedAt",
                value: new DateTime(2026, 2, 3, 9, 13, 48, 173, DateTimeKind.Local).AddTicks(7694));

            migrationBuilder.UpdateData(
                table: "SaleDetails",
                keyColumn: "Id",
                keyValue: 7,
                column: "CreatedAt",
                value: new DateTime(2026, 2, 3, 9, 13, 48, 173, DateTimeKind.Local).AddTicks(7694));

            migrationBuilder.UpdateData(
                table: "SaleDetails",
                keyColumn: "Id",
                keyValue: 8,
                column: "CreatedAt",
                value: new DateTime(2026, 2, 3, 9, 13, 48, 173, DateTimeKind.Local).AddTicks(7694));

            migrationBuilder.UpdateData(
                table: "Sales",
                keyColumn: "Id",
                keyValue: 1,
                columns: new[] { "CreatedAt", "SaleDate" },
                values: new object[] { new DateTime(2026, 2, 5, 11, 13, 48, 173, DateTimeKind.Local).AddTicks(7694), new DateTime(2026, 2, 5, 11, 13, 48, 173, DateTimeKind.Local).AddTicks(7694) });

            migrationBuilder.UpdateData(
                table: "Sales",
                keyColumn: "Id",
                keyValue: 2,
                columns: new[] { "CreatedAt", "SaleDate" },
                values: new object[] { new DateTime(2026, 2, 4, 10, 13, 48, 173, DateTimeKind.Local).AddTicks(7694), new DateTime(2026, 2, 4, 10, 13, 48, 173, DateTimeKind.Local).AddTicks(7694) });

            migrationBuilder.UpdateData(
                table: "Sales",
                keyColumn: "Id",
                keyValue: 3,
                columns: new[] { "CreatedAt", "SaleDate" },
                values: new object[] { new DateTime(2026, 2, 3, 9, 13, 48, 173, DateTimeKind.Local).AddTicks(7694), new DateTime(2026, 2, 3, 9, 13, 48, 173, DateTimeKind.Local).AddTicks(7694) });

            migrationBuilder.UpdateData(
                table: "Suppliers",
                keyColumn: "Id",
                keyValue: 1,
                column: "CreatedAt",
                value: new DateTime(2026, 2, 5, 12, 13, 48, 173, DateTimeKind.Local).AddTicks(7694));

            migrationBuilder.UpdateData(
                table: "Suppliers",
                keyColumn: "Id",
                keyValue: 2,
                column: "CreatedAt",
                value: new DateTime(2026, 2, 5, 12, 13, 48, 173, DateTimeKind.Local).AddTicks(7694));

            migrationBuilder.UpdateData(
                table: "Users",
                keyColumn: "Id",
                keyValue: 1,
                column: "CreatedAt",
                value: new DateTime(2026, 2, 5, 12, 13, 48, 173, DateTimeKind.Local).AddTicks(7694));

            migrationBuilder.UpdateData(
                table: "Users",
                keyColumn: "Id",
                keyValue: 2,
                column: "CreatedAt",
                value: new DateTime(2026, 2, 5, 12, 13, 48, 173, DateTimeKind.Local).AddTicks(7694));
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "AppSettings");

            migrationBuilder.UpdateData(
                table: "Categories",
                keyColumn: "Id",
                keyValue: 1,
                column: "CreatedAt",
                value: new DateTime(2026, 2, 5, 12, 2, 24, 72, DateTimeKind.Local).AddTicks(2176));

            migrationBuilder.UpdateData(
                table: "Categories",
                keyColumn: "Id",
                keyValue: 2,
                column: "CreatedAt",
                value: new DateTime(2026, 2, 5, 12, 2, 24, 72, DateTimeKind.Local).AddTicks(2176));

            migrationBuilder.UpdateData(
                table: "Categories",
                keyColumn: "Id",
                keyValue: 3,
                column: "CreatedAt",
                value: new DateTime(2026, 2, 5, 12, 2, 24, 72, DateTimeKind.Local).AddTicks(2176));

            migrationBuilder.UpdateData(
                table: "Categories",
                keyColumn: "Id",
                keyValue: 4,
                column: "CreatedAt",
                value: new DateTime(2026, 2, 5, 12, 2, 24, 72, DateTimeKind.Local).AddTicks(2176));

            migrationBuilder.UpdateData(
                table: "Categories",
                keyColumn: "Id",
                keyValue: 5,
                column: "CreatedAt",
                value: new DateTime(2026, 2, 5, 12, 2, 24, 72, DateTimeKind.Local).AddTicks(2176));

            migrationBuilder.UpdateData(
                table: "Categories",
                keyColumn: "Id",
                keyValue: 6,
                column: "CreatedAt",
                value: new DateTime(2026, 2, 5, 12, 2, 24, 72, DateTimeKind.Local).AddTicks(2176));

            migrationBuilder.UpdateData(
                table: "Customers",
                keyColumn: "Id",
                keyValue: 1,
                column: "CreatedAt",
                value: new DateTime(2026, 2, 5, 12, 2, 24, 72, DateTimeKind.Local).AddTicks(2176));

            migrationBuilder.UpdateData(
                table: "Customers",
                keyColumn: "Id",
                keyValue: 2,
                column: "CreatedAt",
                value: new DateTime(2026, 2, 5, 12, 2, 24, 72, DateTimeKind.Local).AddTicks(2176));

            migrationBuilder.UpdateData(
                table: "Expenses",
                keyColumn: "Id",
                keyValue: 1,
                columns: new[] { "CreatedAt", "ExpenseDate" },
                values: new object[] { new DateTime(2026, 1, 31, 12, 2, 24, 72, DateTimeKind.Local).AddTicks(2176), new DateTime(2026, 1, 31, 12, 2, 24, 72, DateTimeKind.Local).AddTicks(2176) });

            migrationBuilder.UpdateData(
                table: "Expenses",
                keyColumn: "Id",
                keyValue: 2,
                columns: new[] { "CreatedAt", "ExpenseDate" },
                values: new object[] { new DateTime(2026, 2, 2, 12, 2, 24, 72, DateTimeKind.Local).AddTicks(2176), new DateTime(2026, 2, 2, 12, 2, 24, 72, DateTimeKind.Local).AddTicks(2176) });

            migrationBuilder.UpdateData(
                table: "Expenses",
                keyColumn: "Id",
                keyValue: 3,
                columns: new[] { "CreatedAt", "ExpenseDate" },
                values: new object[] { new DateTime(2026, 2, 3, 12, 2, 24, 72, DateTimeKind.Local).AddTicks(2176), new DateTime(2026, 2, 3, 12, 2, 24, 72, DateTimeKind.Local).AddTicks(2176) });

            migrationBuilder.UpdateData(
                table: "Expenses",
                keyColumn: "Id",
                keyValue: 4,
                columns: new[] { "CreatedAt", "ExpenseDate" },
                values: new object[] { new DateTime(2026, 2, 4, 12, 2, 24, 72, DateTimeKind.Local).AddTicks(2176), new DateTime(2026, 2, 4, 12, 2, 24, 72, DateTimeKind.Local).AddTicks(2176) });

            migrationBuilder.UpdateData(
                table: "Products",
                keyColumn: "Id",
                keyValue: 1,
                column: "CreatedAt",
                value: new DateTime(2026, 2, 5, 12, 2, 24, 72, DateTimeKind.Local).AddTicks(2176));

            migrationBuilder.UpdateData(
                table: "Products",
                keyColumn: "Id",
                keyValue: 2,
                column: "CreatedAt",
                value: new DateTime(2026, 2, 5, 12, 2, 24, 72, DateTimeKind.Local).AddTicks(2176));

            migrationBuilder.UpdateData(
                table: "Products",
                keyColumn: "Id",
                keyValue: 3,
                column: "CreatedAt",
                value: new DateTime(2026, 2, 5, 12, 2, 24, 72, DateTimeKind.Local).AddTicks(2176));

            migrationBuilder.UpdateData(
                table: "Products",
                keyColumn: "Id",
                keyValue: 4,
                column: "CreatedAt",
                value: new DateTime(2026, 2, 5, 12, 2, 24, 72, DateTimeKind.Local).AddTicks(2176));

            migrationBuilder.UpdateData(
                table: "Products",
                keyColumn: "Id",
                keyValue: 5,
                column: "CreatedAt",
                value: new DateTime(2026, 2, 5, 12, 2, 24, 72, DateTimeKind.Local).AddTicks(2176));

            migrationBuilder.UpdateData(
                table: "Products",
                keyColumn: "Id",
                keyValue: 6,
                column: "CreatedAt",
                value: new DateTime(2026, 2, 5, 12, 2, 24, 72, DateTimeKind.Local).AddTicks(2176));

            migrationBuilder.UpdateData(
                table: "Products",
                keyColumn: "Id",
                keyValue: 7,
                column: "CreatedAt",
                value: new DateTime(2026, 2, 5, 12, 2, 24, 72, DateTimeKind.Local).AddTicks(2176));

            migrationBuilder.UpdateData(
                table: "Products",
                keyColumn: "Id",
                keyValue: 8,
                column: "CreatedAt",
                value: new DateTime(2026, 2, 5, 12, 2, 24, 72, DateTimeKind.Local).AddTicks(2176));

            migrationBuilder.UpdateData(
                table: "Products",
                keyColumn: "Id",
                keyValue: 9,
                column: "CreatedAt",
                value: new DateTime(2026, 2, 5, 12, 2, 24, 72, DateTimeKind.Local).AddTicks(2176));

            migrationBuilder.UpdateData(
                table: "Products",
                keyColumn: "Id",
                keyValue: 10,
                column: "CreatedAt",
                value: new DateTime(2026, 2, 5, 12, 2, 24, 72, DateTimeKind.Local).AddTicks(2176));

            migrationBuilder.UpdateData(
                table: "Products",
                keyColumn: "Id",
                keyValue: 11,
                column: "CreatedAt",
                value: new DateTime(2026, 2, 5, 12, 2, 24, 72, DateTimeKind.Local).AddTicks(2176));

            migrationBuilder.UpdateData(
                table: "Products",
                keyColumn: "Id",
                keyValue: 12,
                column: "CreatedAt",
                value: new DateTime(2026, 2, 5, 12, 2, 24, 72, DateTimeKind.Local).AddTicks(2176));

            migrationBuilder.UpdateData(
                table: "Products",
                keyColumn: "Id",
                keyValue: 13,
                column: "CreatedAt",
                value: new DateTime(2026, 2, 5, 12, 2, 24, 72, DateTimeKind.Local).AddTicks(2176));

            migrationBuilder.UpdateData(
                table: "Products",
                keyColumn: "Id",
                keyValue: 14,
                column: "CreatedAt",
                value: new DateTime(2026, 2, 5, 12, 2, 24, 72, DateTimeKind.Local).AddTicks(2176));

            migrationBuilder.UpdateData(
                table: "Products",
                keyColumn: "Id",
                keyValue: 15,
                column: "CreatedAt",
                value: new DateTime(2026, 2, 5, 12, 2, 24, 72, DateTimeKind.Local).AddTicks(2176));

            migrationBuilder.UpdateData(
                table: "Products",
                keyColumn: "Id",
                keyValue: 16,
                column: "CreatedAt",
                value: new DateTime(2026, 2, 5, 12, 2, 24, 72, DateTimeKind.Local).AddTicks(2176));

            migrationBuilder.UpdateData(
                table: "Products",
                keyColumn: "Id",
                keyValue: 17,
                column: "CreatedAt",
                value: new DateTime(2026, 2, 5, 12, 2, 24, 72, DateTimeKind.Local).AddTicks(2176));

            migrationBuilder.UpdateData(
                table: "Products",
                keyColumn: "Id",
                keyValue: 18,
                column: "CreatedAt",
                value: new DateTime(2026, 2, 5, 12, 2, 24, 72, DateTimeKind.Local).AddTicks(2176));

            migrationBuilder.UpdateData(
                table: "Products",
                keyColumn: "Id",
                keyValue: 19,
                column: "CreatedAt",
                value: new DateTime(2026, 2, 5, 12, 2, 24, 72, DateTimeKind.Local).AddTicks(2176));

            migrationBuilder.UpdateData(
                table: "Products",
                keyColumn: "Id",
                keyValue: 20,
                column: "CreatedAt",
                value: new DateTime(2026, 2, 5, 12, 2, 24, 72, DateTimeKind.Local).AddTicks(2176));

            migrationBuilder.UpdateData(
                table: "Products",
                keyColumn: "Id",
                keyValue: 21,
                column: "CreatedAt",
                value: new DateTime(2026, 2, 5, 12, 2, 24, 72, DateTimeKind.Local).AddTicks(2176));

            migrationBuilder.UpdateData(
                table: "Products",
                keyColumn: "Id",
                keyValue: 22,
                column: "CreatedAt",
                value: new DateTime(2026, 2, 5, 12, 2, 24, 72, DateTimeKind.Local).AddTicks(2176));

            migrationBuilder.UpdateData(
                table: "Products",
                keyColumn: "Id",
                keyValue: 23,
                column: "CreatedAt",
                value: new DateTime(2026, 2, 5, 12, 2, 24, 72, DateTimeKind.Local).AddTicks(2176));

            migrationBuilder.UpdateData(
                table: "Products",
                keyColumn: "Id",
                keyValue: 24,
                column: "CreatedAt",
                value: new DateTime(2026, 2, 5, 12, 2, 24, 72, DateTimeKind.Local).AddTicks(2176));

            migrationBuilder.UpdateData(
                table: "SaleDetails",
                keyColumn: "Id",
                keyValue: 1,
                column: "CreatedAt",
                value: new DateTime(2026, 2, 5, 11, 2, 24, 72, DateTimeKind.Local).AddTicks(2176));

            migrationBuilder.UpdateData(
                table: "SaleDetails",
                keyColumn: "Id",
                keyValue: 2,
                column: "CreatedAt",
                value: new DateTime(2026, 2, 5, 11, 2, 24, 72, DateTimeKind.Local).AddTicks(2176));

            migrationBuilder.UpdateData(
                table: "SaleDetails",
                keyColumn: "Id",
                keyValue: 3,
                column: "CreatedAt",
                value: new DateTime(2026, 2, 5, 11, 2, 24, 72, DateTimeKind.Local).AddTicks(2176));

            migrationBuilder.UpdateData(
                table: "SaleDetails",
                keyColumn: "Id",
                keyValue: 4,
                column: "CreatedAt",
                value: new DateTime(2026, 2, 4, 10, 2, 24, 72, DateTimeKind.Local).AddTicks(2176));

            migrationBuilder.UpdateData(
                table: "SaleDetails",
                keyColumn: "Id",
                keyValue: 5,
                column: "CreatedAt",
                value: new DateTime(2026, 2, 4, 10, 2, 24, 72, DateTimeKind.Local).AddTicks(2176));

            migrationBuilder.UpdateData(
                table: "SaleDetails",
                keyColumn: "Id",
                keyValue: 6,
                column: "CreatedAt",
                value: new DateTime(2026, 2, 3, 9, 2, 24, 72, DateTimeKind.Local).AddTicks(2176));

            migrationBuilder.UpdateData(
                table: "SaleDetails",
                keyColumn: "Id",
                keyValue: 7,
                column: "CreatedAt",
                value: new DateTime(2026, 2, 3, 9, 2, 24, 72, DateTimeKind.Local).AddTicks(2176));

            migrationBuilder.UpdateData(
                table: "SaleDetails",
                keyColumn: "Id",
                keyValue: 8,
                column: "CreatedAt",
                value: new DateTime(2026, 2, 3, 9, 2, 24, 72, DateTimeKind.Local).AddTicks(2176));

            migrationBuilder.UpdateData(
                table: "Sales",
                keyColumn: "Id",
                keyValue: 1,
                columns: new[] { "CreatedAt", "SaleDate" },
                values: new object[] { new DateTime(2026, 2, 5, 11, 2, 24, 72, DateTimeKind.Local).AddTicks(2176), new DateTime(2026, 2, 5, 11, 2, 24, 72, DateTimeKind.Local).AddTicks(2176) });

            migrationBuilder.UpdateData(
                table: "Sales",
                keyColumn: "Id",
                keyValue: 2,
                columns: new[] { "CreatedAt", "SaleDate" },
                values: new object[] { new DateTime(2026, 2, 4, 10, 2, 24, 72, DateTimeKind.Local).AddTicks(2176), new DateTime(2026, 2, 4, 10, 2, 24, 72, DateTimeKind.Local).AddTicks(2176) });

            migrationBuilder.UpdateData(
                table: "Sales",
                keyColumn: "Id",
                keyValue: 3,
                columns: new[] { "CreatedAt", "SaleDate" },
                values: new object[] { new DateTime(2026, 2, 3, 9, 2, 24, 72, DateTimeKind.Local).AddTicks(2176), new DateTime(2026, 2, 3, 9, 2, 24, 72, DateTimeKind.Local).AddTicks(2176) });

            migrationBuilder.UpdateData(
                table: "Suppliers",
                keyColumn: "Id",
                keyValue: 1,
                column: "CreatedAt",
                value: new DateTime(2026, 2, 5, 12, 2, 24, 72, DateTimeKind.Local).AddTicks(2176));

            migrationBuilder.UpdateData(
                table: "Suppliers",
                keyColumn: "Id",
                keyValue: 2,
                column: "CreatedAt",
                value: new DateTime(2026, 2, 5, 12, 2, 24, 72, DateTimeKind.Local).AddTicks(2176));

            migrationBuilder.UpdateData(
                table: "Users",
                keyColumn: "Id",
                keyValue: 1,
                column: "CreatedAt",
                value: new DateTime(2026, 2, 5, 12, 2, 24, 72, DateTimeKind.Local).AddTicks(2176));

            migrationBuilder.UpdateData(
                table: "Users",
                keyColumn: "Id",
                keyValue: 2,
                column: "CreatedAt",
                value: new DateTime(2026, 2, 5, 12, 2, 24, 72, DateTimeKind.Local).AddTicks(2176));
        }
    }
}
