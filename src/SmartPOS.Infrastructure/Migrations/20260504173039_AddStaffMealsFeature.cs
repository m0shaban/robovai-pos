using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SmartPOS.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddStaffMealsFeature : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<decimal>(
                name: "DailyMealLimit",
                table: "Users",
                type: "TEXT",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<int>(
                name: "ConsumedByUserId",
                table: "Sales",
                type: "INTEGER",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Sales_ConsumedByUserId",
                table: "Sales",
                column: "ConsumedByUserId");

            migrationBuilder.AddForeignKey(
                name: "FK_Sales_Users_ConsumedByUserId",
                table: "Sales",
                column: "ConsumedByUserId",
                principalTable: "Users",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Sales_Users_ConsumedByUserId",
                table: "Sales");

            migrationBuilder.DropIndex(
                name: "IX_Sales_ConsumedByUserId",
                table: "Sales");

            migrationBuilder.DropColumn(
                name: "DailyMealLimit",
                table: "Users");

            migrationBuilder.DropColumn(
                name: "ConsumedByUserId",
                table: "Sales");
        }
    }
}
