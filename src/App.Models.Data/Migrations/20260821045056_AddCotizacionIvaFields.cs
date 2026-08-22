using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace App.Models.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddCotizacionIvaFields : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<decimal>(
                name: "IvaTasaPorDefecto",
                table: "stg_settings",
                type: "decimal(5,2)",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<bool>(
                name: "IncluirIva",
                table: "cot_cotizaciones",
                type: "tinyint(1)",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<decimal>(
                name: "IvaMonto",
                table: "cot_cotizaciones",
                type: "decimal(18,2)",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<decimal>(
                name: "IvaTasa",
                table: "cot_cotizaciones",
                type: "decimal(5,2)",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<decimal>(
                name: "Subtotal",
                table: "cot_cotizaciones",
                type: "decimal(18,2)",
                nullable: false,
                defaultValue: 0m);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "IvaTasaPorDefecto",
                table: "stg_settings");

            migrationBuilder.DropColumn(
                name: "IncluirIva",
                table: "cot_cotizaciones");

            migrationBuilder.DropColumn(
                name: "IvaMonto",
                table: "cot_cotizaciones");

            migrationBuilder.DropColumn(
                name: "IvaTasa",
                table: "cot_cotizaciones");

            migrationBuilder.DropColumn(
                name: "Subtotal",
                table: "cot_cotizaciones");
        }
    }
}
