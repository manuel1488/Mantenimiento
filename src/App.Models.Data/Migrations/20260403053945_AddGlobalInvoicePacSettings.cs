using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace App.Models.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddGlobalInvoicePacSettings : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "GlobalInvoiceFolioLength",
                table: "mx_pac_settings",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<string>(
                name: "GlobalInvoiceSerie",
                table: "mx_pac_settings",
                type: "varchar(10)",
                maxLength: 10,
                nullable: true)
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AddColumn<long>(
                name: "GlobalInvoiceStartFolio",
                table: "mx_pac_settings",
                type: "bigint",
                nullable: false,
                defaultValue: 0L);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "GlobalInvoiceFolioLength",
                table: "mx_pac_settings");

            migrationBuilder.DropColumn(
                name: "GlobalInvoiceSerie",
                table: "mx_pac_settings");

            migrationBuilder.DropColumn(
                name: "GlobalInvoiceStartFolio",
                table: "mx_pac_settings");
        }
    }
}
