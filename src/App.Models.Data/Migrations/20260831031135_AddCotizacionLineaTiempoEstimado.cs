using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace App.Models.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddCotizacionLineaTiempoEstimado : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<decimal>(
                name: "RendimientoDiasPorUnidad",
                table: "cot_cotizacion_lineas",
                type: "decimal(10,2)",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<decimal>(
                name: "TiempoEstimadoDias",
                table: "cot_cotizacion_lineas",
                type: "decimal(10,2)",
                nullable: false,
                defaultValue: 0m);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "RendimientoDiasPorUnidad",
                table: "cot_cotizacion_lineas");

            migrationBuilder.DropColumn(
                name: "TiempoEstimadoDias",
                table: "cot_cotizacion_lineas");
        }
    }
}
