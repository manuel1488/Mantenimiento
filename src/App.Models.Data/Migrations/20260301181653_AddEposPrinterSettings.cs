using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace App.Models.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddEposPrinterSettings : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "DirectPrintEnabled",
                table: "stg_ticket_configuration",
                type: "tinyint(1)",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<string>(
                name: "EposPrintDeviceId",
                table: "stg_ticket_configuration",
                type: "varchar(50)",
                maxLength: 50,
                nullable: false,
                defaultValue: "")
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AddColumn<string>(
                name: "EposPrintHost",
                table: "stg_ticket_configuration",
                type: "varchar(100)",
                maxLength: 100,
                nullable: false,
                defaultValue: "")
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AddColumn<int>(
                name: "EposPrintPort",
                table: "stg_ticket_configuration",
                type: "int",
                nullable: false,
                defaultValue: 0);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "DirectPrintEnabled",
                table: "stg_ticket_configuration");

            migrationBuilder.DropColumn(
                name: "EposPrintDeviceId",
                table: "stg_ticket_configuration");

            migrationBuilder.DropColumn(
                name: "EposPrintHost",
                table: "stg_ticket_configuration");

            migrationBuilder.DropColumn(
                name: "EposPrintPort",
                table: "stg_ticket_configuration");
        }
    }
}
