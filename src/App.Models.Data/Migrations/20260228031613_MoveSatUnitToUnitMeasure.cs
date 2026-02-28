using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace App.Models.Data.Migrations
{
    /// <inheritdoc />
    public partial class MoveSatUnitToUnitMeasure : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_sh_products_mx_sat_units_MexicoSatUnitId",
                table: "sh_products");

            migrationBuilder.DropIndex(
                name: "IX_sh_products_MexicoSatUnitId",
                table: "sh_products");

            migrationBuilder.DropColumn(
                name: "MexicoSatUnitId",
                table: "sh_products");

            migrationBuilder.AddColumn<int>(
                name: "MexicoSatUnitId",
                table: "sh_unit_measures",
                type: "int",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_sh_unit_measures_MexicoSatUnitId",
                table: "sh_unit_measures",
                column: "MexicoSatUnitId");

            migrationBuilder.AddForeignKey(
                name: "FK_sh_unit_measures_mx_sat_units_MexicoSatUnitId",
                table: "sh_unit_measures",
                column: "MexicoSatUnitId",
                principalTable: "mx_sat_units",
                principalColumn: "Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_sh_unit_measures_mx_sat_units_MexicoSatUnitId",
                table: "sh_unit_measures");

            migrationBuilder.DropIndex(
                name: "IX_sh_unit_measures_MexicoSatUnitId",
                table: "sh_unit_measures");

            migrationBuilder.DropColumn(
                name: "MexicoSatUnitId",
                table: "sh_unit_measures");

            migrationBuilder.AddColumn<int>(
                name: "MexicoSatUnitId",
                table: "sh_products",
                type: "int",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_sh_products_MexicoSatUnitId",
                table: "sh_products",
                column: "MexicoSatUnitId");

            migrationBuilder.AddForeignKey(
                name: "FK_sh_products_mx_sat_units_MexicoSatUnitId",
                table: "sh_products",
                column: "MexicoSatUnitId",
                principalTable: "mx_sat_units",
                principalColumn: "Id");
        }
    }
}
