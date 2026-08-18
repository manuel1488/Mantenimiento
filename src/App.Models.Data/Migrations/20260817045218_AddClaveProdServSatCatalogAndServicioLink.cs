using System;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace App.Models.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddClaveProdServSatCatalogAndServicioLink : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "ClaveProdServSatId",
                table: "srv_servicios",
                type: "int",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "cat_claves_prod_serv_sat",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    Codigo = table.Column<string>(type: "varchar(10)", maxLength: 10, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Descripcion = table.Column<string>(type: "varchar(250)", maxLength: 250, nullable: false)
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
                    table.PrimaryKey("PK_cat_claves_prod_serv_sat", x => x.Id);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateIndex(
                name: "IX_srv_servicios_ClaveProdServSatId",
                table: "srv_servicios",
                column: "ClaveProdServSatId");

            migrationBuilder.CreateIndex(
                name: "IX_cat_claves_prod_serv_sat_Codigo",
                table: "cat_claves_prod_serv_sat",
                column: "Codigo",
                unique: true);

            migrationBuilder.AddForeignKey(
                name: "FK_srv_servicios_cat_claves_prod_serv_sat_ClaveProdServSatId",
                table: "srv_servicios",
                column: "ClaveProdServSatId",
                principalTable: "cat_claves_prod_serv_sat",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_srv_servicios_cat_claves_prod_serv_sat_ClaveProdServSatId",
                table: "srv_servicios");

            migrationBuilder.DropTable(
                name: "cat_claves_prod_serv_sat");

            migrationBuilder.DropIndex(
                name: "IX_srv_servicios_ClaveProdServSatId",
                table: "srv_servicios");

            migrationBuilder.DropColumn(
                name: "ClaveProdServSatId",
                table: "srv_servicios");
        }
    }
}
