using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace expense_tracker.Migrations
{
    /// <inheritdoc />
    public partial class AddBudget : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "PaymentMode",
                table: "Transactions");

            migrationBuilder.AddColumn<int>(
                name: "Budget",
                table: "Categories",
                type: "int",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Budget",
                table: "Categories");

            migrationBuilder.AddColumn<string>(
                name: "PaymentMode",
                table: "Transactions",
                type: "nvarchar(20)",
                nullable: false,
                defaultValue: "");
        }
    }
}
