using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace App.Models.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddCotizacionContactSettings : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "CorreoElectronico",
                table: "stg_cotizacion_template_settings",
                type: "varchar(150)",
                maxLength: 150,
                nullable: true)
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AddColumn<string>(
                name: "Facebook",
                table: "stg_cotizacion_template_settings",
                type: "varchar(150)",
                maxLength: 150,
                nullable: true)
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AddColumn<string>(
                name: "Instagram",
                table: "stg_cotizacion_template_settings",
                type: "varchar(150)",
                maxLength: 150,
                nullable: true)
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AddColumn<bool>(
                name: "MostrarContacto",
                table: "stg_cotizacion_template_settings",
                type: "tinyint(1)",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<string>(
                name: "SitioWeb",
                table: "stg_cotizacion_template_settings",
                type: "varchar(200)",
                maxLength: 200,
                nullable: true)
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AddColumn<string>(
                name: "Telefono",
                table: "stg_cotizacion_template_settings",
                type: "varchar(30)",
                maxLength: 30,
                nullable: true)
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AddColumn<string>(
                name: "WhatsApp",
                table: "stg_cotizacion_template_settings",
                type: "varchar(30)",
                maxLength: 30,
                nullable: true)
                .Annotation("MySql:CharSet", "utf8mb4");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "CorreoElectronico",
                table: "stg_cotizacion_template_settings");

            migrationBuilder.DropColumn(
                name: "Facebook",
                table: "stg_cotizacion_template_settings");

            migrationBuilder.DropColumn(
                name: "Instagram",
                table: "stg_cotizacion_template_settings");

            migrationBuilder.DropColumn(
                name: "MostrarContacto",
                table: "stg_cotizacion_template_settings");

            migrationBuilder.DropColumn(
                name: "SitioWeb",
                table: "stg_cotizacion_template_settings");

            migrationBuilder.DropColumn(
                name: "Telefono",
                table: "stg_cotizacion_template_settings");

            migrationBuilder.DropColumn(
                name: "WhatsApp",
                table: "stg_cotizacion_template_settings");
        }
    }
}
