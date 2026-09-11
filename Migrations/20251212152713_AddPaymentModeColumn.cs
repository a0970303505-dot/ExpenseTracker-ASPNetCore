using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace expense_tracker.Migrations
{
    /// <inheritdoc />
    public partial class AddPaymentModeColumn : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<string>(
                name: "Note",
                table: "Transactions",
                type: "nvarchar(75)",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(75)");

            migrationBuilder.AddColumn<string>(
                name: "PaymentMode",
                table: "Transactions",
                type: "nvarchar(20)",
                nullable: false,
                defaultValue: "");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "PaymentMode",
                table: "Transactions");

            migrationBuilder.AlterColumn<string>(
                name: "Note",
                table: "Transactions",
                type: "nvarchar(75)",
                nullable: false,
                defaultValue: "",
                oldClrType: typeof(string),
                oldType: "nvarchar(75)",
                oldNullable: true);
        }
    }
}
