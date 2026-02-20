using System;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace App.Models.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddLocationAddressAndTicketSettings : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "City",
                table: "sh_locations",
                type: "varchar(100)",
                maxLength: 100,
                nullable: true)
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AddColumn<string>(
                name: "Country",
                table: "sh_locations",
                type: "varchar(2)",
                unicode: false,
                maxLength: 2,
                nullable: false,
                defaultValue: "MX")
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AddColumn<string>(
                name: "ExteriorNumber",
                table: "sh_locations",
                type: "varchar(20)",
                maxLength: 20,
                nullable: true)
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AddColumn<string>(
                name: "InteriorNumber",
                table: "sh_locations",
                type: "varchar(20)",
                maxLength: 20,
                nullable: true)
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AddColumn<decimal>(
                name: "Latitude",
                table: "sh_locations",
                type: "decimal(10,8)",
                precision: 10,
                scale: 8,
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "Longitude",
                table: "sh_locations",
                type: "decimal(11,8)",
                precision: 11,
                scale: 8,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Neighborhood",
                table: "sh_locations",
                type: "varchar(100)",
                maxLength: 100,
                nullable: true)
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AddColumn<string>(
                name: "PostalCode",
                table: "sh_locations",
                type: "varchar(10)",
                unicode: false,
                maxLength: 10,
                nullable: true)
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AddColumn<string>(
                name: "State",
                table: "sh_locations",
                type: "varchar(100)",
                maxLength: 100,
                nullable: true)
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AddColumn<string>(
                name: "Street",
                table: "sh_locations",
                type: "varchar(200)",
                maxLength: 200,
                nullable: true)
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "sh_location_ticket_settings",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    LocationId = table.Column<int>(type: "int", nullable: false),
                    PrinterName = table.Column<string>(type: "varchar(200)", unicode: false, maxLength: 200, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    PaperWidth = table.Column<int>(type: "int", nullable: false, defaultValue: 80),
                    AutoPrint = table.Column<bool>(type: "tinyint(1)", nullable: false, defaultValue: false),
                    Copies = table.Column<int>(type: "int", nullable: false, defaultValue: 1),
                    HeaderText = table.Column<string>(type: "varchar(500)", maxLength: 500, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    FooterText = table.Column<string>(type: "varchar(500)", maxLength: 500, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    LogoPath = table.Column<string>(type: "varchar(500)", maxLength: 500, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    ShowLogo = table.Column<bool>(type: "tinyint(1)", nullable: false, defaultValue: true),
                    TaxId = table.Column<string>(type: "varchar(20)", maxLength: 20, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    LegalName = table.Column<string>(type: "varchar(200)", maxLength: 200, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    ShowFullAddress = table.Column<bool>(type: "tinyint(1)", nullable: false, defaultValue: true),
                    ShowQrCode = table.Column<bool>(type: "tinyint(1)", nullable: false, defaultValue: false),
                    QrCodeContent = table.Column<string>(type: "varchar(500)", maxLength: 500, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    ShowPrices = table.Column<bool>(type: "tinyint(1)", nullable: false, defaultValue: true),
                    ShowTaxBreakdown = table.Column<bool>(type: "tinyint(1)", nullable: false, defaultValue: true),
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
                    table.PrimaryKey("PK_sh_location_ticket_settings", x => x.Id);
                    table.ForeignKey(
                        name: "FK_sh_location_ticket_settings_sh_locations_LocationId",
                        column: x => x.LocationId,
                        principalTable: "sh_locations",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateIndex(
                name: "IX_sh_locations_City_State",
                table: "sh_locations",
                columns: new[] { "City", "State" });

            migrationBuilder.CreateIndex(
                name: "IX_sh_locations_PostalCode",
                table: "sh_locations",
                column: "PostalCode");

            migrationBuilder.CreateIndex(
                name: "IX_sh_location_ticket_settings_LocationId",
                table: "sh_location_ticket_settings",
                column: "LocationId",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "sh_location_ticket_settings");

            migrationBuilder.DropIndex(
                name: "IX_sh_locations_City_State",
                table: "sh_locations");

            migrationBuilder.DropIndex(
                name: "IX_sh_locations_PostalCode",
                table: "sh_locations");

            migrationBuilder.DropColumn(
                name: "City",
                table: "sh_locations");

            migrationBuilder.DropColumn(
                name: "Country",
                table: "sh_locations");

            migrationBuilder.DropColumn(
                name: "ExteriorNumber",
                table: "sh_locations");

            migrationBuilder.DropColumn(
                name: "InteriorNumber",
                table: "sh_locations");

            migrationBuilder.DropColumn(
                name: "Latitude",
                table: "sh_locations");

            migrationBuilder.DropColumn(
                name: "Longitude",
                table: "sh_locations");

            migrationBuilder.DropColumn(
                name: "Neighborhood",
                table: "sh_locations");

            migrationBuilder.DropColumn(
                name: "PostalCode",
                table: "sh_locations");

            migrationBuilder.DropColumn(
                name: "State",
                table: "sh_locations");

            migrationBuilder.DropColumn(
                name: "Street",
                table: "sh_locations");
        }
    }
}
