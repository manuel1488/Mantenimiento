using System;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace App.Models.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddMexicoSatUnits : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "MexicoSatUnitId",
                table: "sh_products",
                type: "int",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "mx_sat_units",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    Code = table.Column<string>(type: "varchar(10)", maxLength: 10, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Name = table.Column<string>(type: "varchar(200)", maxLength: 200, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Symbol = table.Column<string>(type: "varchar(10)", maxLength: 10, nullable: true)
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
                    table.PrimaryKey("PK_mx_sat_units", x => x.Id);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateIndex(
                name: "IX_sh_products_MexicoSatUnitId",
                table: "sh_products",
                column: "MexicoSatUnitId");

            migrationBuilder.CreateIndex(
                name: "IX_mx_sat_units_Code",
                table: "mx_sat_units",
                column: "Code",
                unique: true);

            migrationBuilder.AddForeignKey(
                name: "FK_sh_products_mx_sat_units_MexicoSatUnitId",
                table: "sh_products",
                column: "MexicoSatUnitId",
                principalTable: "mx_sat_units",
                principalColumn: "Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_sh_products_mx_sat_units_MexicoSatUnitId",
                table: "sh_products");

            migrationBuilder.DropTable(
                name: "mx_sat_units");

            migrationBuilder.DropIndex(
                name: "IX_sh_products_MexicoSatUnitId",
                table: "sh_products");

            migrationBuilder.DropColumn(
                name: "MexicoSatUnitId",
                table: "sh_products");
        }
    }
}
