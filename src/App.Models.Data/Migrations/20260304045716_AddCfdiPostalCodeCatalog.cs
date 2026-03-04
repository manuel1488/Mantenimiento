using System;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace App.Models.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddCfdiPostalCodeCatalog : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "PostalCodeIanaTimeZoneId",
                table: "stg_tax_settings",
                type: "varchar(100)",
                maxLength: 100,
                nullable: true)
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AddColumn<int>(
                name: "PostalCodeOffsetSummer",
                table: "stg_tax_settings",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "PostalCodeOffsetWinter",
                table: "stg_tax_settings",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "PostalCodeTimeZoneName",
                table: "stg_tax_settings",
                type: "varchar(100)",
                maxLength: 100,
                nullable: true)
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "cat_cfdi_postal_codes",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    Code = table.Column<string>(type: "varchar(10)", maxLength: 10, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    StateId = table.Column<string>(type: "varchar(3)", maxLength: 3, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    MunicipalityId = table.Column<string>(type: "varchar(50)", maxLength: 50, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    LocalityId = table.Column<string>(type: "varchar(10)", maxLength: 10, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    IsBorderZone = table.Column<bool>(type: "tinyint(1)", nullable: false),
                    TimeZoneName = table.Column<string>(type: "varchar(100)", maxLength: 100, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    IanaTimeZoneId = table.Column<string>(type: "varchar(100)", maxLength: 100, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    OffsetWinter = table.Column<int>(type: "int", nullable: false),
                    OffsetSummer = table.Column<int>(type: "int", nullable: false),
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
                    table.PrimaryKey("PK_cat_cfdi_postal_codes", x => x.Id);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateIndex(
                name: "IX_cat_cfdi_postal_codes_Code",
                table: "cat_cfdi_postal_codes",
                column: "Code");

            migrationBuilder.CreateIndex(
                name: "IX_cat_cfdi_postal_codes_StateId",
                table: "cat_cfdi_postal_codes",
                column: "StateId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "cat_cfdi_postal_codes");

            migrationBuilder.DropColumn(
                name: "PostalCodeIanaTimeZoneId",
                table: "stg_tax_settings");

            migrationBuilder.DropColumn(
                name: "PostalCodeOffsetSummer",
                table: "stg_tax_settings");

            migrationBuilder.DropColumn(
                name: "PostalCodeOffsetWinter",
                table: "stg_tax_settings");

            migrationBuilder.DropColumn(
                name: "PostalCodeTimeZoneName",
                table: "stg_tax_settings");
        }
    }
}
