using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SmartPOS.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddProductImagePath : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "ImagePath",
                table: "Products",
                type: "TEXT",
                nullable: true);

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
                columns: new[] { "CreatedAt", "ImagePath" },
                values: new object[] { new DateTime(2026, 2, 5, 10, 22, 44, 685, DateTimeKind.Local).AddTicks(3448), null });

            migrationBuilder.UpdateData(
                table: "Products",
                keyColumn: "Id",
                keyValue: 2,
                columns: new[] { "CreatedAt", "ImagePath" },
                values: new object[] { new DateTime(2026, 2, 5, 10, 22, 44, 685, DateTimeKind.Local).AddTicks(3448), null });

            migrationBuilder.UpdateData(
                table: "Products",
                keyColumn: "Id",
                keyValue: 3,
                columns: new[] { "CreatedAt", "ImagePath" },
                values: new object[] { new DateTime(2026, 2, 5, 10, 22, 44, 685, DateTimeKind.Local).AddTicks(3448), null });

            migrationBuilder.UpdateData(
                table: "Products",
                keyColumn: "Id",
                keyValue: 4,
                columns: new[] { "CreatedAt", "ImagePath" },
                values: new object[] { new DateTime(2026, 2, 5, 10, 22, 44, 685, DateTimeKind.Local).AddTicks(3448), null });

            migrationBuilder.UpdateData(
                table: "Products",
                keyColumn: "Id",
                keyValue: 5,
                columns: new[] { "CreatedAt", "ImagePath" },
                values: new object[] { new DateTime(2026, 2, 5, 10, 22, 44, 685, DateTimeKind.Local).AddTicks(3448), null });

            migrationBuilder.UpdateData(
                table: "Products",
                keyColumn: "Id",
                keyValue: 6,
                columns: new[] { "CreatedAt", "ImagePath" },
                values: new object[] { new DateTime(2026, 2, 5, 10, 22, 44, 685, DateTimeKind.Local).AddTicks(3448), null });

            migrationBuilder.UpdateData(
                table: "Products",
                keyColumn: "Id",
                keyValue: 7,
                columns: new[] { "CreatedAt", "ImagePath" },
                values: new object[] { new DateTime(2026, 2, 5, 10, 22, 44, 685, DateTimeKind.Local).AddTicks(3448), null });

            migrationBuilder.UpdateData(
                table: "Products",
                keyColumn: "Id",
                keyValue: 8,
                columns: new[] { "CreatedAt", "ImagePath" },
                values: new object[] { new DateTime(2026, 2, 5, 10, 22, 44, 685, DateTimeKind.Local).AddTicks(3448), null });

            migrationBuilder.UpdateData(
                table: "Products",
                keyColumn: "Id",
                keyValue: 9,
                columns: new[] { "CreatedAt", "ImagePath" },
                values: new object[] { new DateTime(2026, 2, 5, 10, 22, 44, 685, DateTimeKind.Local).AddTicks(3448), null });

            migrationBuilder.UpdateData(
                table: "Products",
                keyColumn: "Id",
                keyValue: 10,
                columns: new[] { "CreatedAt", "ImagePath" },
                values: new object[] { new DateTime(2026, 2, 5, 10, 22, 44, 685, DateTimeKind.Local).AddTicks(3448), null });

            migrationBuilder.UpdateData(
                table: "Products",
                keyColumn: "Id",
                keyValue: 11,
                columns: new[] { "CreatedAt", "ImagePath" },
                values: new object[] { new DateTime(2026, 2, 5, 10, 22, 44, 685, DateTimeKind.Local).AddTicks(3448), null });

            migrationBuilder.UpdateData(
                table: "Products",
                keyColumn: "Id",
                keyValue: 12,
                columns: new[] { "CreatedAt", "ImagePath" },
                values: new object[] { new DateTime(2026, 2, 5, 10, 22, 44, 685, DateTimeKind.Local).AddTicks(3448), null });

            migrationBuilder.UpdateData(
                table: "Products",
                keyColumn: "Id",
                keyValue: 13,
                columns: new[] { "CreatedAt", "ImagePath" },
                values: new object[] { new DateTime(2026, 2, 5, 10, 22, 44, 685, DateTimeKind.Local).AddTicks(3448), null });

            migrationBuilder.UpdateData(
                table: "Products",
                keyColumn: "Id",
                keyValue: 14,
                columns: new[] { "CreatedAt", "ImagePath" },
                values: new object[] { new DateTime(2026, 2, 5, 10, 22, 44, 685, DateTimeKind.Local).AddTicks(3448), null });

            migrationBuilder.UpdateData(
                table: "Products",
                keyColumn: "Id",
                keyValue: 15,
                columns: new[] { "CreatedAt", "ImagePath" },
                values: new object[] { new DateTime(2026, 2, 5, 10, 22, 44, 685, DateTimeKind.Local).AddTicks(3448), null });

            migrationBuilder.UpdateData(
                table: "Products",
                keyColumn: "Id",
                keyValue: 16,
                columns: new[] { "CreatedAt", "ImagePath" },
                values: new object[] { new DateTime(2026, 2, 5, 10, 22, 44, 685, DateTimeKind.Local).AddTicks(3448), null });

            migrationBuilder.UpdateData(
                table: "Products",
                keyColumn: "Id",
                keyValue: 17,
                columns: new[] { "CreatedAt", "ImagePath" },
                values: new object[] { new DateTime(2026, 2, 5, 10, 22, 44, 685, DateTimeKind.Local).AddTicks(3448), null });

            migrationBuilder.UpdateData(
                table: "Products",
                keyColumn: "Id",
                keyValue: 18,
                columns: new[] { "CreatedAt", "ImagePath" },
                values: new object[] { new DateTime(2026, 2, 5, 10, 22, 44, 685, DateTimeKind.Local).AddTicks(3448), null });

            migrationBuilder.UpdateData(
                table: "Products",
                keyColumn: "Id",
                keyValue: 19,
                columns: new[] { "CreatedAt", "ImagePath" },
                values: new object[] { new DateTime(2026, 2, 5, 10, 22, 44, 685, DateTimeKind.Local).AddTicks(3448), null });

            migrationBuilder.UpdateData(
                table: "Products",
                keyColumn: "Id",
                keyValue: 20,
                columns: new[] { "CreatedAt", "ImagePath" },
                values: new object[] { new DateTime(2026, 2, 5, 10, 22, 44, 685, DateTimeKind.Local).AddTicks(3448), null });

            migrationBuilder.UpdateData(
                table: "Products",
                keyColumn: "Id",
                keyValue: 21,
                columns: new[] { "CreatedAt", "ImagePath" },
                values: new object[] { new DateTime(2026, 2, 5, 10, 22, 44, 685, DateTimeKind.Local).AddTicks(3448), null });

            migrationBuilder.UpdateData(
                table: "Products",
                keyColumn: "Id",
                keyValue: 22,
                columns: new[] { "CreatedAt", "ImagePath" },
                values: new object[] { new DateTime(2026, 2, 5, 10, 22, 44, 685, DateTimeKind.Local).AddTicks(3448), null });

            migrationBuilder.UpdateData(
                table: "Products",
                keyColumn: "Id",
                keyValue: 23,
                columns: new[] { "CreatedAt", "ImagePath" },
                values: new object[] { new DateTime(2026, 2, 5, 10, 22, 44, 685, DateTimeKind.Local).AddTicks(3448), null });

            migrationBuilder.UpdateData(
                table: "Products",
                keyColumn: "Id",
                keyValue: 24,
                columns: new[] { "CreatedAt", "ImagePath" },
                values: new object[] { new DateTime(2026, 2, 5, 10, 22, 44, 685, DateTimeKind.Local).AddTicks(3448), null });

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

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ImagePath",
                table: "Products");

            migrationBuilder.UpdateData(
                table: "Categories",
                keyColumn: "Id",
                keyValue: 1,
                column: "CreatedAt",
                value: new DateTime(2026, 2, 5, 9, 50, 1, 328, DateTimeKind.Local).AddTicks(4953));

            migrationBuilder.UpdateData(
                table: "Categories",
                keyColumn: "Id",
                keyValue: 2,
                column: "CreatedAt",
                value: new DateTime(2026, 2, 5, 9, 50, 1, 328, DateTimeKind.Local).AddTicks(4953));

            migrationBuilder.UpdateData(
                table: "Categories",
                keyColumn: "Id",
                keyValue: 3,
                column: "CreatedAt",
                value: new DateTime(2026, 2, 5, 9, 50, 1, 328, DateTimeKind.Local).AddTicks(4953));

            migrationBuilder.UpdateData(
                table: "Categories",
                keyColumn: "Id",
                keyValue: 4,
                column: "CreatedAt",
                value: new DateTime(2026, 2, 5, 9, 50, 1, 328, DateTimeKind.Local).AddTicks(4953));

            migrationBuilder.UpdateData(
                table: "Categories",
                keyColumn: "Id",
                keyValue: 5,
                column: "CreatedAt",
                value: new DateTime(2026, 2, 5, 9, 50, 1, 328, DateTimeKind.Local).AddTicks(4953));

            migrationBuilder.UpdateData(
                table: "Categories",
                keyColumn: "Id",
                keyValue: 6,
                column: "CreatedAt",
                value: new DateTime(2026, 2, 5, 9, 50, 1, 328, DateTimeKind.Local).AddTicks(4953));

            migrationBuilder.UpdateData(
                table: "Customers",
                keyColumn: "Id",
                keyValue: 1,
                column: "CreatedAt",
                value: new DateTime(2026, 2, 5, 9, 50, 1, 328, DateTimeKind.Local).AddTicks(4953));

            migrationBuilder.UpdateData(
                table: "Customers",
                keyColumn: "Id",
                keyValue: 2,
                column: "CreatedAt",
                value: new DateTime(2026, 2, 5, 9, 50, 1, 328, DateTimeKind.Local).AddTicks(4953));

            migrationBuilder.UpdateData(
                table: "Expenses",
                keyColumn: "Id",
                keyValue: 1,
                columns: new[] { "CreatedAt", "ExpenseDate" },
                values: new object[] { new DateTime(2026, 1, 31, 9, 50, 1, 328, DateTimeKind.Local).AddTicks(4953), new DateTime(2026, 1, 31, 9, 50, 1, 328, DateTimeKind.Local).AddTicks(4953) });

            migrationBuilder.UpdateData(
                table: "Expenses",
                keyColumn: "Id",
                keyValue: 2,
                columns: new[] { "CreatedAt", "ExpenseDate" },
                values: new object[] { new DateTime(2026, 2, 2, 9, 50, 1, 328, DateTimeKind.Local).AddTicks(4953), new DateTime(2026, 2, 2, 9, 50, 1, 328, DateTimeKind.Local).AddTicks(4953) });

            migrationBuilder.UpdateData(
                table: "Expenses",
                keyColumn: "Id",
                keyValue: 3,
                columns: new[] { "CreatedAt", "ExpenseDate" },
                values: new object[] { new DateTime(2026, 2, 3, 9, 50, 1, 328, DateTimeKind.Local).AddTicks(4953), new DateTime(2026, 2, 3, 9, 50, 1, 328, DateTimeKind.Local).AddTicks(4953) });

            migrationBuilder.UpdateData(
                table: "Expenses",
                keyColumn: "Id",
                keyValue: 4,
                columns: new[] { "CreatedAt", "ExpenseDate" },
                values: new object[] { new DateTime(2026, 2, 4, 9, 50, 1, 328, DateTimeKind.Local).AddTicks(4953), new DateTime(2026, 2, 4, 9, 50, 1, 328, DateTimeKind.Local).AddTicks(4953) });

            migrationBuilder.UpdateData(
                table: "Products",
                keyColumn: "Id",
                keyValue: 1,
                column: "CreatedAt",
                value: new DateTime(2026, 2, 5, 9, 50, 1, 328, DateTimeKind.Local).AddTicks(4953));

            migrationBuilder.UpdateData(
                table: "Products",
                keyColumn: "Id",
                keyValue: 2,
                column: "CreatedAt",
                value: new DateTime(2026, 2, 5, 9, 50, 1, 328, DateTimeKind.Local).AddTicks(4953));

            migrationBuilder.UpdateData(
                table: "Products",
                keyColumn: "Id",
                keyValue: 3,
                column: "CreatedAt",
                value: new DateTime(2026, 2, 5, 9, 50, 1, 328, DateTimeKind.Local).AddTicks(4953));

            migrationBuilder.UpdateData(
                table: "Products",
                keyColumn: "Id",
                keyValue: 4,
                column: "CreatedAt",
                value: new DateTime(2026, 2, 5, 9, 50, 1, 328, DateTimeKind.Local).AddTicks(4953));

            migrationBuilder.UpdateData(
                table: "Products",
                keyColumn: "Id",
                keyValue: 5,
                column: "CreatedAt",
                value: new DateTime(2026, 2, 5, 9, 50, 1, 328, DateTimeKind.Local).AddTicks(4953));

            migrationBuilder.UpdateData(
                table: "Products",
                keyColumn: "Id",
                keyValue: 6,
                column: "CreatedAt",
                value: new DateTime(2026, 2, 5, 9, 50, 1, 328, DateTimeKind.Local).AddTicks(4953));

            migrationBuilder.UpdateData(
                table: "Products",
                keyColumn: "Id",
                keyValue: 7,
                column: "CreatedAt",
                value: new DateTime(2026, 2, 5, 9, 50, 1, 328, DateTimeKind.Local).AddTicks(4953));

            migrationBuilder.UpdateData(
                table: "Products",
                keyColumn: "Id",
                keyValue: 8,
                column: "CreatedAt",
                value: new DateTime(2026, 2, 5, 9, 50, 1, 328, DateTimeKind.Local).AddTicks(4953));

            migrationBuilder.UpdateData(
                table: "Products",
                keyColumn: "Id",
                keyValue: 9,
                column: "CreatedAt",
                value: new DateTime(2026, 2, 5, 9, 50, 1, 328, DateTimeKind.Local).AddTicks(4953));

            migrationBuilder.UpdateData(
                table: "Products",
                keyColumn: "Id",
                keyValue: 10,
                column: "CreatedAt",
                value: new DateTime(2026, 2, 5, 9, 50, 1, 328, DateTimeKind.Local).AddTicks(4953));

            migrationBuilder.UpdateData(
                table: "Products",
                keyColumn: "Id",
                keyValue: 11,
                column: "CreatedAt",
                value: new DateTime(2026, 2, 5, 9, 50, 1, 328, DateTimeKind.Local).AddTicks(4953));

            migrationBuilder.UpdateData(
                table: "Products",
                keyColumn: "Id",
                keyValue: 12,
                column: "CreatedAt",
                value: new DateTime(2026, 2, 5, 9, 50, 1, 328, DateTimeKind.Local).AddTicks(4953));

            migrationBuilder.UpdateData(
                table: "Products",
                keyColumn: "Id",
                keyValue: 13,
                column: "CreatedAt",
                value: new DateTime(2026, 2, 5, 9, 50, 1, 328, DateTimeKind.Local).AddTicks(4953));

            migrationBuilder.UpdateData(
                table: "Products",
                keyColumn: "Id",
                keyValue: 14,
                column: "CreatedAt",
                value: new DateTime(2026, 2, 5, 9, 50, 1, 328, DateTimeKind.Local).AddTicks(4953));

            migrationBuilder.UpdateData(
                table: "Products",
                keyColumn: "Id",
                keyValue: 15,
                column: "CreatedAt",
                value: new DateTime(2026, 2, 5, 9, 50, 1, 328, DateTimeKind.Local).AddTicks(4953));

            migrationBuilder.UpdateData(
                table: "Products",
                keyColumn: "Id",
                keyValue: 16,
                column: "CreatedAt",
                value: new DateTime(2026, 2, 5, 9, 50, 1, 328, DateTimeKind.Local).AddTicks(4953));

            migrationBuilder.UpdateData(
                table: "Products",
                keyColumn: "Id",
                keyValue: 17,
                column: "CreatedAt",
                value: new DateTime(2026, 2, 5, 9, 50, 1, 328, DateTimeKind.Local).AddTicks(4953));

            migrationBuilder.UpdateData(
                table: "Products",
                keyColumn: "Id",
                keyValue: 18,
                column: "CreatedAt",
                value: new DateTime(2026, 2, 5, 9, 50, 1, 328, DateTimeKind.Local).AddTicks(4953));

            migrationBuilder.UpdateData(
                table: "Products",
                keyColumn: "Id",
                keyValue: 19,
                column: "CreatedAt",
                value: new DateTime(2026, 2, 5, 9, 50, 1, 328, DateTimeKind.Local).AddTicks(4953));

            migrationBuilder.UpdateData(
                table: "Products",
                keyColumn: "Id",
                keyValue: 20,
                column: "CreatedAt",
                value: new DateTime(2026, 2, 5, 9, 50, 1, 328, DateTimeKind.Local).AddTicks(4953));

            migrationBuilder.UpdateData(
                table: "Products",
                keyColumn: "Id",
                keyValue: 21,
                column: "CreatedAt",
                value: new DateTime(2026, 2, 5, 9, 50, 1, 328, DateTimeKind.Local).AddTicks(4953));

            migrationBuilder.UpdateData(
                table: "Products",
                keyColumn: "Id",
                keyValue: 22,
                column: "CreatedAt",
                value: new DateTime(2026, 2, 5, 9, 50, 1, 328, DateTimeKind.Local).AddTicks(4953));

            migrationBuilder.UpdateData(
                table: "Products",
                keyColumn: "Id",
                keyValue: 23,
                column: "CreatedAt",
                value: new DateTime(2026, 2, 5, 9, 50, 1, 328, DateTimeKind.Local).AddTicks(4953));

            migrationBuilder.UpdateData(
                table: "Products",
                keyColumn: "Id",
                keyValue: 24,
                column: "CreatedAt",
                value: new DateTime(2026, 2, 5, 9, 50, 1, 328, DateTimeKind.Local).AddTicks(4953));

            migrationBuilder.UpdateData(
                table: "SaleDetails",
                keyColumn: "Id",
                keyValue: 1,
                column: "CreatedAt",
                value: new DateTime(2026, 2, 5, 8, 50, 1, 328, DateTimeKind.Local).AddTicks(4953));

            migrationBuilder.UpdateData(
                table: "SaleDetails",
                keyColumn: "Id",
                keyValue: 2,
                column: "CreatedAt",
                value: new DateTime(2026, 2, 5, 8, 50, 1, 328, DateTimeKind.Local).AddTicks(4953));

            migrationBuilder.UpdateData(
                table: "SaleDetails",
                keyColumn: "Id",
                keyValue: 3,
                column: "CreatedAt",
                value: new DateTime(2026, 2, 5, 8, 50, 1, 328, DateTimeKind.Local).AddTicks(4953));

            migrationBuilder.UpdateData(
                table: "SaleDetails",
                keyColumn: "Id",
                keyValue: 4,
                column: "CreatedAt",
                value: new DateTime(2026, 2, 4, 7, 50, 1, 328, DateTimeKind.Local).AddTicks(4953));

            migrationBuilder.UpdateData(
                table: "SaleDetails",
                keyColumn: "Id",
                keyValue: 5,
                column: "CreatedAt",
                value: new DateTime(2026, 2, 4, 7, 50, 1, 328, DateTimeKind.Local).AddTicks(4953));

            migrationBuilder.UpdateData(
                table: "SaleDetails",
                keyColumn: "Id",
                keyValue: 6,
                column: "CreatedAt",
                value: new DateTime(2026, 2, 3, 6, 50, 1, 328, DateTimeKind.Local).AddTicks(4953));

            migrationBuilder.UpdateData(
                table: "SaleDetails",
                keyColumn: "Id",
                keyValue: 7,
                column: "CreatedAt",
                value: new DateTime(2026, 2, 3, 6, 50, 1, 328, DateTimeKind.Local).AddTicks(4953));

            migrationBuilder.UpdateData(
                table: "SaleDetails",
                keyColumn: "Id",
                keyValue: 8,
                column: "CreatedAt",
                value: new DateTime(2026, 2, 3, 6, 50, 1, 328, DateTimeKind.Local).AddTicks(4953));

            migrationBuilder.UpdateData(
                table: "Sales",
                keyColumn: "Id",
                keyValue: 1,
                columns: new[] { "CreatedAt", "SaleDate" },
                values: new object[] { new DateTime(2026, 2, 5, 8, 50, 1, 328, DateTimeKind.Local).AddTicks(4953), new DateTime(2026, 2, 5, 8, 50, 1, 328, DateTimeKind.Local).AddTicks(4953) });

            migrationBuilder.UpdateData(
                table: "Sales",
                keyColumn: "Id",
                keyValue: 2,
                columns: new[] { "CreatedAt", "SaleDate" },
                values: new object[] { new DateTime(2026, 2, 4, 7, 50, 1, 328, DateTimeKind.Local).AddTicks(4953), new DateTime(2026, 2, 4, 7, 50, 1, 328, DateTimeKind.Local).AddTicks(4953) });

            migrationBuilder.UpdateData(
                table: "Sales",
                keyColumn: "Id",
                keyValue: 3,
                columns: new[] { "CreatedAt", "SaleDate" },
                values: new object[] { new DateTime(2026, 2, 3, 6, 50, 1, 328, DateTimeKind.Local).AddTicks(4953), new DateTime(2026, 2, 3, 6, 50, 1, 328, DateTimeKind.Local).AddTicks(4953) });

            migrationBuilder.UpdateData(
                table: "Suppliers",
                keyColumn: "Id",
                keyValue: 1,
                column: "CreatedAt",
                value: new DateTime(2026, 2, 5, 9, 50, 1, 328, DateTimeKind.Local).AddTicks(4953));

            migrationBuilder.UpdateData(
                table: "Suppliers",
                keyColumn: "Id",
                keyValue: 2,
                column: "CreatedAt",
                value: new DateTime(2026, 2, 5, 9, 50, 1, 328, DateTimeKind.Local).AddTicks(4953));

            migrationBuilder.UpdateData(
                table: "Users",
                keyColumn: "Id",
                keyValue: 1,
                column: "CreatedAt",
                value: new DateTime(2026, 2, 5, 9, 50, 1, 328, DateTimeKind.Local).AddTicks(4953));

            migrationBuilder.UpdateData(
                table: "Users",
                keyColumn: "Id",
                keyValue: 2,
                column: "CreatedAt",
                value: new DateTime(2026, 2, 5, 9, 50, 1, 328, DateTimeKind.Local).AddTicks(4953));
        }
    }
}
