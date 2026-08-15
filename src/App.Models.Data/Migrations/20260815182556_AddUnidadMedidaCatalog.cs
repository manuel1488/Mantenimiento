using System;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace App.Models.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddUnidadMedidaCatalog : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "UnidadMedida",
                table: "srv_servicios");

            migrationBuilder.AddColumn<int>(
                name: "UnidadMedidaId",
                table: "srv_servicios",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.CreateTable(
                name: "cat_claves_unidad_sat",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    Codigo = table.Column<string>(type: "varchar(10)", maxLength: 10, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Nombre = table.Column<string>(type: "varchar(200)", maxLength: 200, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Simbolo = table.Column<string>(type: "varchar(50)", maxLength: 50, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
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
                    table.PrimaryKey("PK_cat_claves_unidad_sat", x => x.Id);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "srv_unidades_medida",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    Codigo = table.Column<string>(type: "varchar(20)", maxLength: 20, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Nombre = table.Column<string>(type: "varchar(100)", maxLength: 100, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Descripcion = table.Column<string>(type: "varchar(200)", maxLength: 200, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    ClaveUnidadSatId = table.Column<int>(type: "int", nullable: true),
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
                    table.PrimaryKey("PK_srv_unidades_medida", x => x.Id);
                    table.ForeignKey(
                        name: "FK_srv_unidades_medida_cat_claves_unidad_sat_ClaveUnidadSatId",
                        column: x => x.ClaveUnidadSatId,
                        principalTable: "cat_claves_unidad_sat",
                        principalColumn: "Id");
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateIndex(
                name: "IX_srv_servicios_UnidadMedidaId",
                table: "srv_servicios",
                column: "UnidadMedidaId");

            migrationBuilder.CreateIndex(
                name: "IX_cat_claves_unidad_sat_Codigo",
                table: "cat_claves_unidad_sat",
                column: "Codigo",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_srv_unidades_medida_ClaveUnidadSatId",
                table: "srv_unidades_medida",
                column: "ClaveUnidadSatId");

            migrationBuilder.CreateIndex(
                name: "IX_srv_unidades_medida_Codigo",
                table: "srv_unidades_medida",
                column: "Codigo",
                unique: true);

            migrationBuilder.AddForeignKey(
                name: "FK_srv_servicios_srv_unidades_medida_UnidadMedidaId",
                table: "srv_servicios",
                column: "UnidadMedidaId",
                principalTable: "srv_unidades_medida",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_srv_servicios_srv_unidades_medida_UnidadMedidaId",
                table: "srv_servicios");

            migrationBuilder.DropTable(
                name: "srv_unidades_medida");

            migrationBuilder.DropTable(
                name: "cat_claves_unidad_sat");

            migrationBuilder.DropIndex(
                name: "IX_srv_servicios_UnidadMedidaId",
                table: "srv_servicios");

            migrationBuilder.DropColumn(
                name: "UnidadMedidaId",
                table: "srv_servicios");

            migrationBuilder.AddColumn<string>(
                name: "UnidadMedida",
                table: "srv_servicios",
                type: "varchar(20)",
                unicode: false,
                maxLength: 20,
                nullable: false,
                defaultValue: "")
                .Annotation("MySql:CharSet", "utf8mb4");
        }
    }
}
