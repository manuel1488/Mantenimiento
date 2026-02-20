using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace App.Models.Data.Migrations
{
    /// <inheritdoc />
    public partial class RenameLogoPathToLogoBase64InLocationTicketSettings : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "LogoPath",
                table: "sh_location_ticket_settings");

            migrationBuilder.AddColumn<string>(
                name: "LogoBase64",
                table: "sh_location_ticket_settings",
                type: "LONGTEXT",
                nullable: true)
                .Annotation("MySql:CharSet", "utf8mb4");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "LogoBase64",
                table: "sh_location_ticket_settings");

            migrationBuilder.AddColumn<string>(
                name: "LogoPath",
                table: "sh_location_ticket_settings",
                type: "varchar(500)",
                maxLength: 500,
                nullable: true)
                .Annotation("MySql:CharSet", "utf8mb4");
        }
    }
}
