using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace App.Models.Data.Migrations
{
    /// <inheritdoc />
    public partial class DecoupleCotizacionFromObra : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_cot_cotizacion_lineas_obr_actividades_ActividadId",
                table: "cot_cotizacion_lineas");

            migrationBuilder.DropForeignKey(
                name: "FK_cot_cotizaciones_obr_obras_ObraId",
                table: "cot_cotizaciones");

            migrationBuilder.DropIndex(
                name: "IX_cot_cotizaciones_ObraId_Version",
                table: "cot_cotizaciones");

            migrationBuilder.DropColumn(
                name: "ObraId",
                table: "cot_cotizaciones");

            migrationBuilder.RenameColumn(
                name: "Version",
                table: "cot_cotizaciones",
                newName: "ClienteId");

            migrationBuilder.RenameColumn(
                name: "ActividadId",
                table: "cot_cotizacion_lineas",
                newName: "ServicioId");

            migrationBuilder.RenameIndex(
                name: "IX_cot_cotizacion_lineas_ActividadId",
                table: "cot_cotizacion_lineas",
                newName: "IX_cot_cotizacion_lineas_ServicioId");

            migrationBuilder.CreateIndex(
                name: "IX_cot_cotizaciones_ClienteId",
                table: "cot_cotizaciones",
                column: "ClienteId");

            migrationBuilder.AddForeignKey(
                name: "FK_cot_cotizacion_lineas_srv_servicios_ServicioId",
                table: "cot_cotizacion_lineas",
                column: "ServicioId",
                principalTable: "srv_servicios",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_cot_cotizaciones_cli_clientes_ClienteId",
                table: "cot_cotizaciones",
                column: "ClienteId",
                principalTable: "cli_clientes",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_cot_cotizacion_lineas_srv_servicios_ServicioId",
                table: "cot_cotizacion_lineas");

            migrationBuilder.DropForeignKey(
                name: "FK_cot_cotizaciones_cli_clientes_ClienteId",
                table: "cot_cotizaciones");

            migrationBuilder.DropIndex(
                name: "IX_cot_cotizaciones_ClienteId",
                table: "cot_cotizaciones");

            migrationBuilder.RenameColumn(
                name: "ClienteId",
                table: "cot_cotizaciones",
                newName: "Version");

            migrationBuilder.RenameColumn(
                name: "ServicioId",
                table: "cot_cotizacion_lineas",
                newName: "ActividadId");

            migrationBuilder.RenameIndex(
                name: "IX_cot_cotizacion_lineas_ServicioId",
                table: "cot_cotizacion_lineas",
                newName: "IX_cot_cotizacion_lineas_ActividadId");

            migrationBuilder.AddColumn<int>(
                name: "ObraId",
                table: "cot_cotizaciones",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.CreateIndex(
                name: "IX_cot_cotizaciones_ObraId_Version",
                table: "cot_cotizaciones",
                columns: new[] { "ObraId", "Version" },
                unique: true);

            migrationBuilder.AddForeignKey(
                name: "FK_cot_cotizacion_lineas_obr_actividades_ActividadId",
                table: "cot_cotizacion_lineas",
                column: "ActividadId",
                principalTable: "obr_actividades",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_cot_cotizaciones_obr_obras_ObraId",
                table: "cot_cotizaciones",
                column: "ObraId",
                principalTable: "obr_obras",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
