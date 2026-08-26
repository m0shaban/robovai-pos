using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SmartPOS.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddTables : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Tables",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    Name = table.Column<string>(type: "TEXT", nullable: false),
                    Capacity = table.Column<int>(type: "INTEGER", nullable: false),
                    Section = table.Column<string>(type: "TEXT", nullable: false),
                    Status = table.Column<int>(type: "INTEGER", nullable: false),
                    IsActive = table.Column<bool>(type: "INTEGER", nullable: false),
                    CurrentOrderId = table.Column<int>(type: "INTEGER", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "TEXT", nullable: true),
                    IsDeleted = table.Column<bool>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Tables", x => x.Id);
                });

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

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Tables");

            migrationBuilder.UpdateData(
                table: "Categories",
                keyColumn: "Id",
                keyValue: 1,
                column: "CreatedAt",
                value: new DateTime(2026, 2, 5, 10, 22, 44, 685, DateTimeKind.Local).AddTicks(3448));

            migrationBuilder.UpdateData(
                table: "Categories",
                keyColumn: "Id",
                keyValue: 2,
                column: "CreatedAt",
                value: new DateTime(2026, 2, 5, 10, 22, 44, 685, DateTimeKind.Local).AddTicks(3448));

            migrationBuilder.UpdateData(
                table: "Categories",
                keyColumn: "Id",
                keyValue: 3,
                column: "CreatedAt",
                value: new DateTime(2026, 2, 5, 10, 22, 44, 685, DateTimeKind.Local).AddTicks(3448));

            migrationBuilder.UpdateData(
                table: "Categories",
                keyColumn: "Id",
                keyValue: 4,
                column: "CreatedAt",
                value: new DateTime(2026, 2, 5, 10, 22, 44, 685, DateTimeKind.Local).AddTicks(3448));

            migrationBuilder.UpdateData(
                table: "Categories",
                keyColumn: "Id",
                keyValue: 5,
                column: "CreatedAt",
                value: new DateTime(2026, 2, 5, 10, 22, 44, 685, DateTimeKind.Local).AddTicks(3448));

            migrationBuilder.UpdateData(
                table: "Categories",
                keyColumn: "Id",
                keyValue: 6,
                column: "CreatedAt",
                value: new DateTime(2026, 2, 5, 10, 22, 44, 685, DateTimeKind.Local).AddTicks(3448));

            migrationBuilder.UpdateData(
                table: "Customers",
                keyColumn: "Id",
                keyValue: 1,
                column: "CreatedAt",
                value: new DateTime(2026, 2, 5, 10, 22, 44, 685, DateTimeKind.Local).AddTicks(3448));

            migrationBuilder.UpdateData(
                table: "Customers",
                keyColumn: "Id",
                keyValue: 2,
                column: "CreatedAt",
                value: new DateTime(2026, 2, 5, 10, 22, 44, 685, DateTimeKind.Local).AddTicks(3448));

            migrationBuilder.UpdateData(
                table: "Expenses",
                keyColumn: "Id",
                keyValue: 1,
                columns: new[] { "CreatedAt", "ExpenseDate" },
                values: new object[] { new DateTime(2026, 1, 31, 10, 22, 44, 685, DateTimeKind.Local).AddTicks(3448), new DateTime(2026, 1, 31, 10, 22, 44, 685, DateTimeKind.Local).AddTicks(3448) });

            migrationBuilder.UpdateData(
                table: "Expenses",
                keyColumn: "Id",
                keyValue: 2,
                columns: new[] { "CreatedAt", "ExpenseDate" },
                values: new object[] { new DateTime(2026, 2, 2, 10, 22, 44, 685, DateTimeKind.Local).AddTicks(3448), new DateTime(2026, 2, 2, 10, 22, 44, 685, DateTimeKind.Local).AddTicks(3448) });

            migrationBuilder.UpdateData(
                table: "Expenses",
                keyColumn: "Id",
                keyValue: 3,
                columns: new[] { "CreatedAt", "ExpenseDate" },
                values: new object[] { new DateTime(2026, 2, 3, 10, 22, 44, 685, DateTimeKind.Local).AddTicks(3448), new DateTime(2026, 2, 3, 10, 22, 44, 685, DateTimeKind.Local).AddTicks(3448) });

            migrationBuilder.UpdateData(
                table: "Expenses",
                keyColumn: "Id",
                keyValue: 4,
                columns: new[] { "CreatedAt", "ExpenseDate" },
                values: new object[] { new DateTime(2026, 2, 4, 10, 22, 44, 685, DateTimeKind.Local).AddTicks(3448), new DateTime(2026, 2, 4, 10, 22, 44, 685, DateTimeKind.Local).AddTicks(3448) });

            migrationBuilder.UpdateData(
                table: "Products",
                keyColumn: "Id",
                keyValue: 1,
                column: "CreatedAt",
                value: new DateTime(2026, 2, 5, 10, 22, 44, 685, DateTimeKind.Local).AddTicks(3448));

            migrationBuilder.UpdateData(
                table: "Products",
                keyColumn: "Id",
                keyValue: 2,
                column: "CreatedAt",
                value: new DateTime(2026, 2, 5, 10, 22, 44, 685, DateTimeKind.Local).AddTicks(3448));

            migrationBuilder.UpdateData(
                table: "Products",
                keyColumn: "Id",
                keyValue: 3,
                column: "CreatedAt",
                value: new DateTime(2026, 2, 5, 10, 22, 44, 685, DateTimeKind.Local).AddTicks(3448));

            migrationBuilder.UpdateData(
                table: "Products",
                keyColumn: "Id",
                keyValue: 4,
                column: "CreatedAt",
                value: new DateTime(2026, 2, 5, 10, 22, 44, 685, DateTimeKind.Local).AddTicks(3448));

            migrationBuilder.UpdateData(
                table: "Products",
                keyColumn: "Id",
                keyValue: 5,
                column: "CreatedAt",
                value: new DateTime(2026, 2, 5, 10, 22, 44, 685, DateTimeKind.Local).AddTicks(3448));

            migrationBuilder.UpdateData(
                table: "Products",
                keyColumn: "Id",
                keyValue: 6,
                column: "CreatedAt",
                value: new DateTime(2026, 2, 5, 10, 22, 44, 685, DateTimeKind.Local).AddTicks(3448));

            migrationBuilder.UpdateData(
                table: "Products",
                keyColumn: "Id",
                keyValue: 7,
                column: "CreatedAt",
                value: new DateTime(2026, 2, 5, 10, 22, 44, 685, DateTimeKind.Local).AddTicks(3448));

            migrationBuilder.UpdateData(
                table: "Products",
                keyColumn: "Id",
                keyValue: 8,
                column: "CreatedAt",
                value: new DateTime(2026, 2, 5, 10, 22, 44, 685, DateTimeKind.Local).AddTicks(3448));

            migrationBuilder.UpdateData(
                table: "Products",
                keyColumn: "Id",
                keyValue: 9,
                column: "CreatedAt",
                value: new DateTime(2026, 2, 5, 10, 22, 44, 685, DateTimeKind.Local).AddTicks(3448));

            migrationBuilder.UpdateData(
                table: "Products",
                keyColumn: "Id",
                keyValue: 10,
                column: "CreatedAt",
                value: new DateTime(2026, 2, 5, 10, 22, 44, 685, DateTimeKind.Local).AddTicks(3448));

            migrationBuilder.UpdateData(
                table: "Products",
                keyColumn: "Id",
                keyValue: 11,
                column: "CreatedAt",
                value: new DateTime(2026, 2, 5, 10, 22, 44, 685, DateTimeKind.Local).AddTicks(3448));

            migrationBuilder.UpdateData(
                table: "Products",
                keyColumn: "Id",
                keyValue: 12,
                column: "CreatedAt",
                value: new DateTime(2026, 2, 5, 10, 22, 44, 685, DateTimeKind.Local).AddTicks(3448));

            migrationBuilder.UpdateData(
                table: "Products",
                keyColumn: "Id",
                keyValue: 13,
                column: "CreatedAt",
                value: new DateTime(2026, 2, 5, 10, 22, 44, 685, DateTimeKind.Local).AddTicks(3448));

            migrationBuilder.UpdateData(
                table: "Products",
                keyColumn: "Id",
                keyValue: 14,
                column: "CreatedAt",
                value: new DateTime(2026, 2, 5, 10, 22, 44, 685, DateTimeKind.Local).AddTicks(3448));

            migrationBuilder.UpdateData(
                table: "Products",
                keyColumn: "Id",
                keyValue: 15,
                column: "CreatedAt",
                value: new DateTime(2026, 2, 5, 10, 22, 44, 685, DateTimeKind.Local).AddTicks(3448));

            migrationBuilder.UpdateData(
                table: "Products",
                keyColumn: "Id",
                keyValue: 16,
                column: "CreatedAt",
                value: new DateTime(2026, 2, 5, 10, 22, 44, 685, DateTimeKind.Local).AddTicks(3448));

            migrationBuilder.UpdateData(
                table: "Products",
                keyColumn: "Id",
                keyValue: 17,
                column: "CreatedAt",
                value: new DateTime(2026, 2, 5, 10, 22, 44, 685, DateTimeKind.Local).AddTicks(3448));

            migrationBuilder.UpdateData(
                table: "Products",
                keyColumn: "Id",
                keyValue: 18,
                column: "CreatedAt",
                value: new DateTime(2026, 2, 5, 10, 22, 44, 685, DateTimeKind.Local).AddTicks(3448));

            migrationBuilder.UpdateData(
                table: "Products",
                keyColumn: "Id",
                keyValue: 19,
                column: "CreatedAt",
                value: new DateTime(2026, 2, 5, 10, 22, 44, 685, DateTimeKind.Local).AddTicks(3448));

            migrationBuilder.UpdateData(
                table: "Products",
                keyColumn: "Id",
                keyValue: 20,
                column: "CreatedAt",
                value: new DateTime(2026, 2, 5, 10, 22, 44, 685, DateTimeKind.Local).AddTicks(3448));

            migrationBuilder.UpdateData(
                table: "Products",
                keyColumn: "Id",
                keyValue: 21,
                column: "CreatedAt",
                value: new DateTime(2026, 2, 5, 10, 22, 44, 685, DateTimeKind.Local).AddTicks(3448));

            migrationBuilder.UpdateData(
                table: "Products",
                keyColumn: "Id",
                keyValue: 22,
                column: "CreatedAt",
                value: new DateTime(2026, 2, 5, 10, 22, 44, 685, DateTimeKind.Local).AddTicks(3448));

            migrationBuilder.UpdateData(
                table: "Products",
                keyColumn: "Id",
                keyValue: 23,
                column: "CreatedAt",
                value: new DateTime(2026, 2, 5, 10, 22, 44, 685, DateTimeKind.Local).AddTicks(3448));

            migrationBuilder.UpdateData(
                table: "Products",
                keyColumn: "Id",
                keyValue: 24,
                column: "CreatedAt",
                value: new DateTime(2026, 2, 5, 10, 22, 44, 685, DateTimeKind.Local).AddTicks(3448));

            migrationBuilder.UpdateData(
                table: "SaleDetails",
                keyColumn: "Id",
                keyValue: 1,
                column: "CreatedAt",
                value: new DateTime(2026, 2, 5, 9, 22, 44, 685, DateTimeKind.Local).AddTicks(3448));

            migrationBuilder.UpdateData(
                table: "SaleDetails",
                keyColumn: "Id",
                keyValue: 2,
                column: "CreatedAt",
                value: new DateTime(2026, 2, 5, 9, 22, 44, 685, DateTimeKind.Local).AddTicks(3448));

            migrationBuilder.UpdateData(
                table: "SaleDetails",
                keyColumn: "Id",
                keyValue: 3,
                column: "CreatedAt",
                value: new DateTime(2026, 2, 5, 9, 22, 44, 685, DateTimeKind.Local).AddTicks(3448));

            migrationBuilder.UpdateData(
                table: "SaleDetails",
                keyColumn: "Id",
                keyValue: 4,
                column: "CreatedAt",
                value: new DateTime(2026, 2, 4, 8, 22, 44, 685, DateTimeKind.Local).AddTicks(3448));

            migrationBuilder.UpdateData(
                table: "SaleDetails",
                keyColumn: "Id",
                keyValue: 5,
                column: "CreatedAt",
                value: new DateTime(2026, 2, 4, 8, 22, 44, 685, DateTimeKind.Local).AddTicks(3448));

            migrationBuilder.UpdateData(
                table: "SaleDetails",
                keyColumn: "Id",
                keyValue: 6,
                column: "CreatedAt",
                value: new DateTime(2026, 2, 3, 7, 22, 44, 685, DateTimeKind.Local).AddTicks(3448));

            migrationBuilder.UpdateData(
                table: "SaleDetails",
                keyColumn: "Id",
                keyValue: 7,
                column: "CreatedAt",
                value: new DateTime(2026, 2, 3, 7, 22, 44, 685, DateTimeKind.Local).AddTicks(3448));

            migrationBuilder.UpdateData(
                table: "SaleDetails",
                keyColumn: "Id",
                keyValue: 8,
                column: "CreatedAt",
                value: new DateTime(2026, 2, 3, 7, 22, 44, 685, DateTimeKind.Local).AddTicks(3448));

            migrationBuilder.UpdateData(
                table: "Sales",
                keyColumn: "Id",
                keyValue: 1,
                columns: new[] { "CreatedAt", "SaleDate" },
                values: new object[] { new DateTime(2026, 2, 5, 9, 22, 44, 685, DateTimeKind.Local).AddTicks(3448), new DateTime(2026, 2, 5, 9, 22, 44, 685, DateTimeKind.Local).AddTicks(3448) });

            migrationBuilder.UpdateData(
                table: "Sales",
                keyColumn: "Id",
                keyValue: 2,
                columns: new[] { "CreatedAt", "SaleDate" },
                values: new object[] { new DateTime(2026, 2, 4, 8, 22, 44, 685, DateTimeKind.Local).AddTicks(3448), new DateTime(2026, 2, 4, 8, 22, 44, 685, DateTimeKind.Local).AddTicks(3448) });

            migrationBuilder.UpdateData(
                table: "Sales",
                keyColumn: "Id",
                keyValue: 3,
                columns: new[] { "CreatedAt", "SaleDate" },
                values: new object[] { new DateTime(2026, 2, 3, 7, 22, 44, 685, DateTimeKind.Local).AddTicks(3448), new DateTime(2026, 2, 3, 7, 22, 44, 685, DateTimeKind.Local).AddTicks(3448) });

            migrationBuilder.UpdateData(
                table: "Suppliers",
                keyColumn: "Id",
                keyValue: 1,
                column: "CreatedAt",
                value: new DateTime(2026, 2, 5, 10, 22, 44, 685, DateTimeKind.Local).AddTicks(3448));

            migrationBuilder.UpdateData(
                table: "Suppliers",
                keyColumn: "Id",
                keyValue: 2,
                column: "CreatedAt",
                value: new DateTime(2026, 2, 5, 10, 22, 44, 685, DateTimeKind.Local).AddTicks(3448));

            migrationBuilder.UpdateData(
                table: "Users",
                keyColumn: "Id",
                keyValue: 1,
                column: "CreatedAt",
                value: new DateTime(2026, 2, 5, 10, 22, 44, 685, DateTimeKind.Local).AddTicks(3448));

            migrationBuilder.UpdateData(
                table: "Users",
                keyColumn: "Id",
                keyValue: 2,
                column: "CreatedAt",
                value: new DateTime(2026, 2, 5, 10, 22, 44, 685, DateTimeKind.Local).AddTicks(3448));
        }
    }
}
