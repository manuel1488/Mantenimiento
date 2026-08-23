using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace App.Models.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddCotizacionFolio : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "FolioDigitos",
                table: "stg_cotizacion_template_settings",
                type: "int",
                nullable: false,
                defaultValue: 4);

            migrationBuilder.AddColumn<string>(
                name: "FolioPrefijo",
                table: "stg_cotizacion_template_settings",
                type: "varchar(20)",
                maxLength: 20,
                nullable: false,
                defaultValue: "COT")
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AddColumn<int>(
                name: "FolioAnio",
                table: "cot_cotizaciones",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "FolioNumero",
                table: "cot_cotizaciones",
                type: "int",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_cot_cotizaciones_FolioAnio_FolioNumero",
                table: "cot_cotizaciones",
                columns: new[] { "FolioAnio", "FolioNumero" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_cot_cotizaciones_FolioAnio_FolioNumero",
                table: "cot_cotizaciones");

            migrationBuilder.DropColumn(
                name: "FolioDigitos",
                table: "stg_cotizacion_template_settings");

            migrationBuilder.DropColumn(
                name: "FolioPrefijo",
                table: "stg_cotizacion_template_settings");

            migrationBuilder.DropColumn(
                name: "FolioAnio",
                table: "cot_cotizaciones");

            migrationBuilder.DropColumn(
                name: "FolioNumero",
                table: "cot_cotizaciones");
        }
    }
}
