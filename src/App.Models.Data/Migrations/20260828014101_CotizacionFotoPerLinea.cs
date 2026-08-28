using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace App.Models.Data.Migrations
{
    /// <inheritdoc />
    public partial class CotizacionFotoPerLinea : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_cot_cotizacion_fotos_cot_cotizaciones_CotizacionId",
                table: "cot_cotizacion_fotos");

            migrationBuilder.RenameColumn(
                name: "CotizacionId",
                table: "cot_cotizacion_fotos",
                newName: "CotizacionLineaId");

            migrationBuilder.RenameIndex(
                name: "IX_cot_cotizacion_fotos_CotizacionId",
                table: "cot_cotizacion_fotos",
                newName: "IX_cot_cotizacion_fotos_CotizacionLineaId");

            // At this point CotizacionLineaId still holds the old Cotizacion.Id values (renaming a
            // column doesn't touch data). Remap each foto to the first línea of the Cotizacion it
            // used to point to — a Cotizacion always has at least one línea by the time it can have
            // fotos, so every row should find a match; any that don't (pre-existing data anomaly)
            // are removed rather than left violating the new FK.
            migrationBuilder.Sql(@"
                UPDATE cot_cotizacion_fotos f
                INNER JOIN (
                    SELECT CotizacionId, MIN(Id) AS PrimeraLineaId
                    FROM cot_cotizacion_lineas
                    GROUP BY CotizacionId
                ) m ON f.CotizacionLineaId = m.CotizacionId
                SET f.CotizacionLineaId = m.PrimeraLineaId;");

            migrationBuilder.Sql(@"
                DELETE FROM cot_cotizacion_fotos
                WHERE CotizacionLineaId NOT IN (SELECT Id FROM cot_cotizacion_lineas);");

            migrationBuilder.AddForeignKey(
                name: "FK_cot_cotizacion_fotos_cot_cotizacion_lineas_CotizacionLineaId",
                table: "cot_cotizacion_fotos",
                column: "CotizacionLineaId",
                principalTable: "cot_cotizacion_lineas",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_cot_cotizacion_fotos_cot_cotizacion_lineas_CotizacionLineaId",
                table: "cot_cotizacion_fotos");

            // Best-effort reverse remap: point each foto back at the Cotizacion that owns the línea
            // it's currently attached to. This collapses fotos of different líneas from the same
            // Cotizacion back onto one CotizacionId, same as before this migration ran — the
            // per-línea assignment made by Up() is not recoverable.
            migrationBuilder.Sql(@"
                UPDATE cot_cotizacion_fotos f
                INNER JOIN cot_cotizacion_lineas l ON f.CotizacionLineaId = l.Id
                SET f.CotizacionLineaId = l.CotizacionId;");

            migrationBuilder.RenameColumn(
                name: "CotizacionLineaId",
                table: "cot_cotizacion_fotos",
                newName: "CotizacionId");

            migrationBuilder.RenameIndex(
                name: "IX_cot_cotizacion_fotos_CotizacionLineaId",
                table: "cot_cotizacion_fotos",
                newName: "IX_cot_cotizacion_fotos_CotizacionId");

            migrationBuilder.AddForeignKey(
                name: "FK_cot_cotizacion_fotos_cot_cotizaciones_CotizacionId",
                table: "cot_cotizacion_fotos",
                column: "CotizacionId",
                principalTable: "cot_cotizaciones",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
