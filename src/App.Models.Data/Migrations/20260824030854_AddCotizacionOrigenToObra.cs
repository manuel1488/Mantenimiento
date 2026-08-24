using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace App.Models.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddCotizacionOrigenToObra : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "CotizacionOrigenId",
                table: "obr_obras",
                type: "int",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_obr_obras_CotizacionOrigenId",
                table: "obr_obras",
                column: "CotizacionOrigenId",
                unique: true);

            migrationBuilder.AddForeignKey(
                name: "FK_obr_obras_cot_cotizaciones_CotizacionOrigenId",
                table: "obr_obras",
                column: "CotizacionOrigenId",
                principalTable: "cot_cotizaciones",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_obr_obras_cot_cotizaciones_CotizacionOrigenId",
                table: "obr_obras");

            migrationBuilder.DropIndex(
                name: "IX_obr_obras_CotizacionOrigenId",
                table: "obr_obras");

            migrationBuilder.DropColumn(
                name: "CotizacionOrigenId",
                table: "obr_obras");
        }
    }
}
