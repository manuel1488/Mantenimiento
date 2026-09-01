using System;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace App.Models.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddObraFolio : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "FolioAnio",
                table: "obr_obras",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "FolioNumero",
                table: "obr_obras",
                type: "int",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "stg_obra_folio_settings",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    FolioPrefijo = table.Column<string>(type: "varchar(20)", maxLength: 20, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    FolioDigitos = table.Column<int>(type: "int", nullable: false),
                    CreatedBy = table.Column<string>(type: "longtext", nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    CreatedAt = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    ModifiedBy = table.Column<string>(type: "longtext", nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    ModifiedAt = table.Column<DateTime>(type: "datetime(6)", nullable: true),
                    IsDeleted = table.Column<uint>(type: "int unsigned", nullable: false),
                    DeletedBy = table.Column<string>(type: "longtext", nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    DeletedAt = table.Column<DateTime>(type: "datetime(6)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_stg_obra_folio_settings", x => x.Id);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateIndex(
                name: "IX_obr_obras_FolioAnio_FolioNumero",
                table: "obr_obras",
                columns: new[] { "FolioAnio", "FolioNumero" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_stg_obra_folio_settings_Id",
                table: "stg_obra_folio_settings",
                column: "Id",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "stg_obra_folio_settings");

            migrationBuilder.DropIndex(
                name: "IX_obr_obras_FolioAnio_FolioNumero",
                table: "obr_obras");

            migrationBuilder.DropColumn(
                name: "FolioAnio",
                table: "obr_obras");

            migrationBuilder.DropColumn(
                name: "FolioNumero",
                table: "obr_obras");
        }
    }
}
