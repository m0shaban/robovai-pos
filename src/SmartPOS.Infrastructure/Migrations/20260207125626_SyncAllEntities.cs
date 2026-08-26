using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SmartPOS.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class SyncAllEntities : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Shifts_UserId",
                table: "Shifts");

            migrationBuilder.UpdateData(
                table: "Categories",
                keyColumn: "Id",
                keyValue: 1,
                column: "CreatedAt",
                value: new DateTime(2026, 2, 7, 14, 56, 25, 397, DateTimeKind.Local).AddTicks(8076));

            migrationBuilder.UpdateData(
                table: "Categories",
                keyColumn: "Id",
                keyValue: 2,
                column: "CreatedAt",
                value: new DateTime(2026, 2, 7, 14, 56, 25, 397, DateTimeKind.Local).AddTicks(8076));

            migrationBuilder.UpdateData(
                table: "Categories",
                keyColumn: "Id",
                keyValue: 3,
                column: "CreatedAt",
                value: new DateTime(2026, 2, 7, 14, 56, 25, 397, DateTimeKind.Local).AddTicks(8076));

            migrationBuilder.UpdateData(
                table: "Categories",
                keyColumn: "Id",
                keyValue: 4,
                column: "CreatedAt",
                value: new DateTime(2026, 2, 7, 14, 56, 25, 397, DateTimeKind.Local).AddTicks(8076));

            migrationBuilder.UpdateData(
                table: "Categories",
                keyColumn: "Id",
                keyValue: 5,
                column: "CreatedAt",
                value: new DateTime(2026, 2, 7, 14, 56, 25, 397, DateTimeKind.Local).AddTicks(8076));

            migrationBuilder.UpdateData(
                table: "Categories",
                keyColumn: "Id",
                keyValue: 6,
                column: "CreatedAt",
                value: new DateTime(2026, 2, 7, 14, 56, 25, 397, DateTimeKind.Local).AddTicks(8076));

            migrationBuilder.UpdateData(
                table: "Customers",
                keyColumn: "Id",
                keyValue: 1,
                column: "CreatedAt",
                value: new DateTime(2026, 2, 7, 14, 56, 25, 397, DateTimeKind.Local).AddTicks(8076));

            migrationBuilder.UpdateData(
                table: "Customers",
                keyColumn: "Id",
                keyValue: 2,
                column: "CreatedAt",
                value: new DateTime(2026, 2, 7, 14, 56, 25, 397, DateTimeKind.Local).AddTicks(8076));

            migrationBuilder.UpdateData(
                table: "Expenses",
                keyColumn: "Id",
                keyValue: 1,
                columns: new[] { "CreatedAt", "ExpenseDate" },
                values: new object[] { new DateTime(2026, 2, 2, 14, 56, 25, 397, DateTimeKind.Local).AddTicks(8076), new DateTime(2026, 2, 2, 14, 56, 25, 397, DateTimeKind.Local).AddTicks(8076) });

            migrationBuilder.UpdateData(
                table: "Expenses",
                keyColumn: "Id",
                keyValue: 2,
                columns: new[] { "CreatedAt", "ExpenseDate" },
                values: new object[] { new DateTime(2026, 2, 4, 14, 56, 25, 397, DateTimeKind.Local).AddTicks(8076), new DateTime(2026, 2, 4, 14, 56, 25, 397, DateTimeKind.Local).AddTicks(8076) });

            migrationBuilder.UpdateData(
                table: "Expenses",
                keyColumn: "Id",
                keyValue: 3,
                columns: new[] { "CreatedAt", "ExpenseDate" },
                values: new object[] { new DateTime(2026, 2, 5, 14, 56, 25, 397, DateTimeKind.Local).AddTicks(8076), new DateTime(2026, 2, 5, 14, 56, 25, 397, DateTimeKind.Local).AddTicks(8076) });

            migrationBuilder.UpdateData(
                table: "Expenses",
                keyColumn: "Id",
                keyValue: 4,
                columns: new[] { "CreatedAt", "ExpenseDate" },
                values: new object[] { new DateTime(2026, 2, 6, 14, 56, 25, 397, DateTimeKind.Local).AddTicks(8076), new DateTime(2026, 2, 6, 14, 56, 25, 397, DateTimeKind.Local).AddTicks(8076) });

            migrationBuilder.UpdateData(
                table: "Products",
                keyColumn: "Id",
                keyValue: 1,
                column: "CreatedAt",
                value: new DateTime(2026, 2, 7, 14, 56, 25, 397, DateTimeKind.Local).AddTicks(8076));

            migrationBuilder.UpdateData(
                table: "Products",
                keyColumn: "Id",
                keyValue: 2,
                column: "CreatedAt",
                value: new DateTime(2026, 2, 7, 14, 56, 25, 397, DateTimeKind.Local).AddTicks(8076));

            migrationBuilder.UpdateData(
                table: "Products",
                keyColumn: "Id",
                keyValue: 3,
                column: "CreatedAt",
                value: new DateTime(2026, 2, 7, 14, 56, 25, 397, DateTimeKind.Local).AddTicks(8076));

            migrationBuilder.UpdateData(
                table: "Products",
                keyColumn: "Id",
                keyValue: 4,
                column: "CreatedAt",
                value: new DateTime(2026, 2, 7, 14, 56, 25, 397, DateTimeKind.Local).AddTicks(8076));

            migrationBuilder.UpdateData(
                table: "Products",
                keyColumn: "Id",
                keyValue: 5,
                column: "CreatedAt",
                value: new DateTime(2026, 2, 7, 14, 56, 25, 397, DateTimeKind.Local).AddTicks(8076));

            migrationBuilder.UpdateData(
                table: "Products",
                keyColumn: "Id",
                keyValue: 6,
                column: "CreatedAt",
                value: new DateTime(2026, 2, 7, 14, 56, 25, 397, DateTimeKind.Local).AddTicks(8076));

            migrationBuilder.UpdateData(
                table: "Products",
                keyColumn: "Id",
                keyValue: 7,
                column: "CreatedAt",
                value: new DateTime(2026, 2, 7, 14, 56, 25, 397, DateTimeKind.Local).AddTicks(8076));

            migrationBuilder.UpdateData(
                table: "Products",
                keyColumn: "Id",
                keyValue: 8,
                column: "CreatedAt",
                value: new DateTime(2026, 2, 7, 14, 56, 25, 397, DateTimeKind.Local).AddTicks(8076));

            migrationBuilder.UpdateData(
                table: "Products",
                keyColumn: "Id",
                keyValue: 9,
                column: "CreatedAt",
                value: new DateTime(2026, 2, 7, 14, 56, 25, 397, DateTimeKind.Local).AddTicks(8076));

            migrationBuilder.UpdateData(
                table: "Products",
                keyColumn: "Id",
                keyValue: 10,
                column: "CreatedAt",
                value: new DateTime(2026, 2, 7, 14, 56, 25, 397, DateTimeKind.Local).AddTicks(8076));

            migrationBuilder.UpdateData(
                table: "Products",
                keyColumn: "Id",
                keyValue: 11,
                column: "CreatedAt",
                value: new DateTime(2026, 2, 7, 14, 56, 25, 397, DateTimeKind.Local).AddTicks(8076));

            migrationBuilder.UpdateData(
                table: "Products",
                keyColumn: "Id",
                keyValue: 12,
                column: "CreatedAt",
                value: new DateTime(2026, 2, 7, 14, 56, 25, 397, DateTimeKind.Local).AddTicks(8076));

            migrationBuilder.UpdateData(
                table: "Products",
                keyColumn: "Id",
                keyValue: 13,
                column: "CreatedAt",
                value: new DateTime(2026, 2, 7, 14, 56, 25, 397, DateTimeKind.Local).AddTicks(8076));

            migrationBuilder.UpdateData(
                table: "Products",
                keyColumn: "Id",
                keyValue: 14,
                column: "CreatedAt",
                value: new DateTime(2026, 2, 7, 14, 56, 25, 397, DateTimeKind.Local).AddTicks(8076));

            migrationBuilder.UpdateData(
                table: "Products",
                keyColumn: "Id",
                keyValue: 15,
                column: "CreatedAt",
                value: new DateTime(2026, 2, 7, 14, 56, 25, 397, DateTimeKind.Local).AddTicks(8076));

            migrationBuilder.UpdateData(
                table: "Products",
                keyColumn: "Id",
                keyValue: 16,
                column: "CreatedAt",
                value: new DateTime(2026, 2, 7, 14, 56, 25, 397, DateTimeKind.Local).AddTicks(8076));

            migrationBuilder.UpdateData(
                table: "Products",
                keyColumn: "Id",
                keyValue: 17,
                column: "CreatedAt",
                value: new DateTime(2026, 2, 7, 14, 56, 25, 397, DateTimeKind.Local).AddTicks(8076));

            migrationBuilder.UpdateData(
                table: "Products",
                keyColumn: "Id",
                keyValue: 18,
                column: "CreatedAt",
                value: new DateTime(2026, 2, 7, 14, 56, 25, 397, DateTimeKind.Local).AddTicks(8076));

            migrationBuilder.UpdateData(
                table: "Products",
                keyColumn: "Id",
                keyValue: 19,
                column: "CreatedAt",
                value: new DateTime(2026, 2, 7, 14, 56, 25, 397, DateTimeKind.Local).AddTicks(8076));

            migrationBuilder.UpdateData(
                table: "Products",
                keyColumn: "Id",
                keyValue: 20,
                column: "CreatedAt",
                value: new DateTime(2026, 2, 7, 14, 56, 25, 397, DateTimeKind.Local).AddTicks(8076));

            migrationBuilder.UpdateData(
                table: "Products",
                keyColumn: "Id",
                keyValue: 21,
                column: "CreatedAt",
                value: new DateTime(2026, 2, 7, 14, 56, 25, 397, DateTimeKind.Local).AddTicks(8076));

            migrationBuilder.UpdateData(
                table: "Products",
                keyColumn: "Id",
                keyValue: 22,
                column: "CreatedAt",
                value: new DateTime(2026, 2, 7, 14, 56, 25, 397, DateTimeKind.Local).AddTicks(8076));

            migrationBuilder.UpdateData(
                table: "Products",
                keyColumn: "Id",
                keyValue: 23,
                column: "CreatedAt",
                value: new DateTime(2026, 2, 7, 14, 56, 25, 397, DateTimeKind.Local).AddTicks(8076));

            migrationBuilder.UpdateData(
                table: "Products",
                keyColumn: "Id",
                keyValue: 24,
                column: "CreatedAt",
                value: new DateTime(2026, 2, 7, 14, 56, 25, 397, DateTimeKind.Local).AddTicks(8076));

            migrationBuilder.UpdateData(
                table: "SaleDetails",
                keyColumn: "Id",
                keyValue: 1,
                column: "CreatedAt",
                value: new DateTime(2026, 2, 7, 13, 56, 25, 397, DateTimeKind.Local).AddTicks(8076));

            migrationBuilder.UpdateData(
                table: "SaleDetails",
                keyColumn: "Id",
                keyValue: 2,
                column: "CreatedAt",
                value: new DateTime(2026, 2, 7, 13, 56, 25, 397, DateTimeKind.Local).AddTicks(8076));

            migrationBuilder.UpdateData(
                table: "SaleDetails",
                keyColumn: "Id",
                keyValue: 3,
                column: "CreatedAt",
                value: new DateTime(2026, 2, 7, 13, 56, 25, 397, DateTimeKind.Local).AddTicks(8076));

            migrationBuilder.UpdateData(
                table: "SaleDetails",
                keyColumn: "Id",
                keyValue: 4,
                column: "CreatedAt",
                value: new DateTime(2026, 2, 6, 12, 56, 25, 397, DateTimeKind.Local).AddTicks(8076));

            migrationBuilder.UpdateData(
                table: "SaleDetails",
                keyColumn: "Id",
                keyValue: 5,
                column: "CreatedAt",
                value: new DateTime(2026, 2, 6, 12, 56, 25, 397, DateTimeKind.Local).AddTicks(8076));

            migrationBuilder.UpdateData(
                table: "SaleDetails",
                keyColumn: "Id",
                keyValue: 6,
                column: "CreatedAt",
                value: new DateTime(2026, 2, 5, 11, 56, 25, 397, DateTimeKind.Local).AddTicks(8076));

            migrationBuilder.UpdateData(
                table: "SaleDetails",
                keyColumn: "Id",
                keyValue: 7,
                column: "CreatedAt",
                value: new DateTime(2026, 2, 5, 11, 56, 25, 397, DateTimeKind.Local).AddTicks(8076));

            migrationBuilder.UpdateData(
                table: "SaleDetails",
                keyColumn: "Id",
                keyValue: 8,
                column: "CreatedAt",
                value: new DateTime(2026, 2, 5, 11, 56, 25, 397, DateTimeKind.Local).AddTicks(8076));

            migrationBuilder.UpdateData(
                table: "Sales",
                keyColumn: "Id",
                keyValue: 1,
                columns: new[] { "CreatedAt", "SaleDate" },
                values: new object[] { new DateTime(2026, 2, 7, 13, 56, 25, 397, DateTimeKind.Local).AddTicks(8076), new DateTime(2026, 2, 7, 13, 56, 25, 397, DateTimeKind.Local).AddTicks(8076) });

            migrationBuilder.UpdateData(
                table: "Sales",
                keyColumn: "Id",
                keyValue: 2,
                columns: new[] { "CreatedAt", "SaleDate" },
                values: new object[] { new DateTime(2026, 2, 6, 12, 56, 25, 397, DateTimeKind.Local).AddTicks(8076), new DateTime(2026, 2, 6, 12, 56, 25, 397, DateTimeKind.Local).AddTicks(8076) });

            migrationBuilder.UpdateData(
                table: "Sales",
                keyColumn: "Id",
                keyValue: 3,
                columns: new[] { "CreatedAt", "SaleDate" },
                values: new object[] { new DateTime(2026, 2, 5, 11, 56, 25, 397, DateTimeKind.Local).AddTicks(8076), new DateTime(2026, 2, 5, 11, 56, 25, 397, DateTimeKind.Local).AddTicks(8076) });

            migrationBuilder.UpdateData(
                table: "Suppliers",
                keyColumn: "Id",
                keyValue: 1,
                column: "CreatedAt",
                value: new DateTime(2026, 2, 7, 14, 56, 25, 397, DateTimeKind.Local).AddTicks(8076));

            migrationBuilder.UpdateData(
                table: "Suppliers",
                keyColumn: "Id",
                keyValue: 2,
                column: "CreatedAt",
                value: new DateTime(2026, 2, 7, 14, 56, 25, 397, DateTimeKind.Local).AddTicks(8076));

            migrationBuilder.UpdateData(
                table: "Users",
                keyColumn: "Id",
                keyValue: 1,
                column: "CreatedAt",
                value: new DateTime(2026, 2, 7, 14, 56, 25, 397, DateTimeKind.Local).AddTicks(8076));

            migrationBuilder.UpdateData(
                table: "Users",
                keyColumn: "Id",
                keyValue: 2,
                column: "CreatedAt",
                value: new DateTime(2026, 2, 7, 14, 56, 25, 397, DateTimeKind.Local).AddTicks(8076));

            migrationBuilder.CreateIndex(
                name: "IX_Shifts_StartTime",
                table: "Shifts",
                column: "StartTime");

            migrationBuilder.CreateIndex(
                name: "IX_Shifts_UserId_Status",
                table: "Shifts",
                columns: new[] { "UserId", "Status" });

            migrationBuilder.CreateIndex(
                name: "IX_Sales_ShiftId_SaleDate",
                table: "Sales",
                columns: new[] { "ShiftId", "SaleDate" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Shifts_StartTime",
                table: "Shifts");

            migrationBuilder.DropIndex(
                name: "IX_Shifts_UserId_Status",
                table: "Shifts");

            migrationBuilder.DropIndex(
                name: "IX_Sales_ShiftId_SaleDate",
                table: "Sales");

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

            migrationBuilder.CreateIndex(
                name: "IX_Shifts_UserId",
                table: "Shifts",
                column: "UserId");
        }
    }
}
