using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace App.Models.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddAutoInvoiceSettings : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "AutoInvoice",
                table: "shd_customers",
                type: "tinyint(1)",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "AllowEditFiscalDataInPrompt",
                table: "mx_pac_settings",
                type: "tinyint(1)",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "AutoInvoicePromptEnabled",
                table: "mx_pac_settings",
                type: "tinyint(1)",
                nullable: false,
                defaultValue: false);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "AutoInvoice",
                table: "shd_customers");

            migrationBuilder.DropColumn(
                name: "AllowEditFiscalDataInPrompt",
                table: "mx_pac_settings");

            migrationBuilder.DropColumn(
                name: "AutoInvoicePromptEnabled",
                table: "mx_pac_settings");
        }
    }
}
