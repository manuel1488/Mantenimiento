using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace App.Models.Data.Migrations
{
    /// <inheritdoc />
    public partial class SeedUnidadMedidaHasData : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.InsertData(
                table: "srv_unidades_medida",
                columns: new[] { "Id", "ClaveUnidadSatId", "Codigo", "CreatedAt", "CreatedBy", "DeletedAt", "DeletedBy", "Descripcion", "IsDeleted", "ModifiedAt", "ModifiedBy", "Nombre" },
                values: new object[,]
                {
                    { 1, null, "PZA", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "System", null, null, null, 0u, null, null, "Pieza" },
                    { 2, null, "SRV", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "System", null, null, null, 0u, null, null, "Servicio" },
                    { 3, null, "KIT", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "System", null, null, null, 0u, null, null, "Kit" },
                    { 4, null, "M", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "System", null, null, null, 0u, null, null, "Metro" },
                    { 5, null, "M2", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "System", null, null, null, 0u, null, null, "Metro Cuadrado" },
                    { 6, null, "M3", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "System", null, null, null, 0u, null, null, "Metro Cúbico" },
                    { 7, null, "KM", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "System", null, null, null, 0u, null, null, "Kilómetro" },
                    { 8, null, "KG", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "System", null, null, null, 0u, null, null, "Kilogramo" },
                    { 9, null, "TON", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "System", null, null, null, 0u, null, null, "Tonelada" },
                    { 10, null, "L", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "System", null, null, null, 0u, null, null, "Litro" },
                    { 11, null, "HR", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "System", null, null, null, 0u, null, null, "Hora" },
                    { 12, null, "DIA", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "System", null, null, null, 0u, null, null, "Día" },
                    { 13, null, "MES", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "System", null, null, null, 0u, null, null, "Mes" },
                    { 14, null, "JGO", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "System", null, null, null, 0u, null, null, "Juego" },
                    { 15, null, "VIS", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "System", null, null, null, 0u, null, null, "Visita" }
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DeleteData(
                table: "srv_unidades_medida",
                keyColumn: "Id",
                keyValue: 1);

            migrationBuilder.DeleteData(
                table: "srv_unidades_medida",
                keyColumn: "Id",
                keyValue: 2);

            migrationBuilder.DeleteData(
                table: "srv_unidades_medida",
                keyColumn: "Id",
                keyValue: 3);

            migrationBuilder.DeleteData(
                table: "srv_unidades_medida",
                keyColumn: "Id",
                keyValue: 4);

            migrationBuilder.DeleteData(
                table: "srv_unidades_medida",
                keyColumn: "Id",
                keyValue: 5);

            migrationBuilder.DeleteData(
                table: "srv_unidades_medida",
                keyColumn: "Id",
                keyValue: 6);

            migrationBuilder.DeleteData(
                table: "srv_unidades_medida",
                keyColumn: "Id",
                keyValue: 7);

            migrationBuilder.DeleteData(
                table: "srv_unidades_medida",
                keyColumn: "Id",
                keyValue: 8);

            migrationBuilder.DeleteData(
                table: "srv_unidades_medida",
                keyColumn: "Id",
                keyValue: 9);

            migrationBuilder.DeleteData(
                table: "srv_unidades_medida",
                keyColumn: "Id",
                keyValue: 10);

            migrationBuilder.DeleteData(
                table: "srv_unidades_medida",
                keyColumn: "Id",
                keyValue: 11);

            migrationBuilder.DeleteData(
                table: "srv_unidades_medida",
                keyColumn: "Id",
                keyValue: 12);

            migrationBuilder.DeleteData(
                table: "srv_unidades_medida",
                keyColumn: "Id",
                keyValue: 13);

            migrationBuilder.DeleteData(
                table: "srv_unidades_medida",
                keyColumn: "Id",
                keyValue: 14);

            migrationBuilder.DeleteData(
                table: "srv_unidades_medida",
                keyColumn: "Id",
                keyValue: 15);
        }
    }
}
