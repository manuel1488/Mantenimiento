using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace App.Models.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddCotizacionPdfDesignSettings : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "Direccion",
                table: "stg_settings",
                type: "varchar(300)",
                maxLength: 300,
                nullable: true)
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AddColumn<string>(
                name: "BancoBeneficiario",
                table: "stg_cotizacion_template_settings",
                type: "varchar(150)",
                maxLength: 150,
                nullable: true)
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AddColumn<string>(
                name: "BancoClabe",
                table: "stg_cotizacion_template_settings",
                type: "varchar(18)",
                maxLength: 18,
                nullable: true)
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AddColumn<string>(
                name: "BancoNombre",
                table: "stg_cotizacion_template_settings",
                type: "varchar(100)",
                maxLength: 100,
                nullable: true)
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AddColumn<string>(
                name: "BancoNumeroCuenta",
                table: "stg_cotizacion_template_settings",
                type: "varchar(50)",
                maxLength: 50,
                nullable: true)
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AddColumn<string>(
                name: "BancoRfc",
                table: "stg_cotizacion_template_settings",
                type: "varchar(13)",
                maxLength: 13,
                nullable: true)
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AddColumn<string>(
                name: "BancoSwift",
                table: "stg_cotizacion_template_settings",
                type: "varchar(20)",
                maxLength: 20,
                nullable: true)
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AddColumn<bool>(
                name: "MostrarDatosBancarios",
                table: "stg_cotizacion_template_settings",
                type: "tinyint(1)",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "MostrarDireccionEnCotizacion",
                table: "stg_cotizacion_template_settings",
                type: "tinyint(1)",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<string>(
                name: "PaymentTermsText",
                table: "stg_cotizacion_template_settings",
                type: "varchar(2000)",
                maxLength: 2000,
                nullable: true)
                .Annotation("MySql:CharSet", "utf8mb4");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Direccion",
                table: "stg_settings");

            migrationBuilder.DropColumn(
                name: "BancoBeneficiario",
                table: "stg_cotizacion_template_settings");

            migrationBuilder.DropColumn(
                name: "BancoClabe",
                table: "stg_cotizacion_template_settings");

            migrationBuilder.DropColumn(
                name: "BancoNombre",
                table: "stg_cotizacion_template_settings");

            migrationBuilder.DropColumn(
                name: "BancoNumeroCuenta",
                table: "stg_cotizacion_template_settings");

            migrationBuilder.DropColumn(
                name: "BancoRfc",
                table: "stg_cotizacion_template_settings");

            migrationBuilder.DropColumn(
                name: "BancoSwift",
                table: "stg_cotizacion_template_settings");

            migrationBuilder.DropColumn(
                name: "MostrarDatosBancarios",
                table: "stg_cotizacion_template_settings");

            migrationBuilder.DropColumn(
                name: "MostrarDireccionEnCotizacion",
                table: "stg_cotizacion_template_settings");

            migrationBuilder.DropColumn(
                name: "PaymentTermsText",
                table: "stg_cotizacion_template_settings");
        }
    }
}
