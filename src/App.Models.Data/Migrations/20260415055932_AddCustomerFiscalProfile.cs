using System;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace App.Models.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddCustomerFiscalProfile : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Step 1: Create the fiscal profiles table first
            migrationBuilder.CreateTable(
                name: "shd_customer_fiscal_profiles",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    CustomerId = table.Column<long>(type: "bigint", nullable: false),
                    TaxId = table.Column<string>(type: "varchar(20)", unicode: false, maxLength: 20, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    LegalName = table.Column<string>(type: "varchar(150)", maxLength: 150, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Street = table.Column<string>(type: "varchar(100)", maxLength: 100, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    ExteriorNumber = table.Column<string>(type: "varchar(20)", maxLength: 20, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    InteriorNumber = table.Column<string>(type: "varchar(20)", maxLength: 20, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Neighborhood = table.Column<string>(type: "varchar(100)", maxLength: 100, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    City = table.Column<string>(type: "varchar(100)", maxLength: 100, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    State = table.Column<string>(type: "varchar(100)", maxLength: 100, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    PostalCode = table.Column<string>(type: "varchar(10)", unicode: false, maxLength: 10, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    FiscalRegime = table.Column<string>(type: "varchar(5)", unicode: false, maxLength: 5, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    DefaultCfdiUse = table.Column<string>(type: "varchar(10)", unicode: false, maxLength: 10, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    AutoInvoice = table.Column<bool>(type: "tinyint(1)", nullable: false, defaultValue: false),
                    SendInvoiceEmail = table.Column<bool>(type: "tinyint(1)", nullable: false, defaultValue: false),
                    CaGstNumber = table.Column<string>(type: "varchar(20)", unicode: false, maxLength: 20, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    CaPstNumber = table.Column<string>(type: "varchar(20)", unicode: false, maxLength: 20, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    CaHstNumber = table.Column<string>(type: "varchar(20)", unicode: false, maxLength: 20, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    CaQstNumber = table.Column<string>(type: "varchar(20)", unicode: false, maxLength: 20, nullable: true)
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
                    table.PrimaryKey("PK_shd_customer_fiscal_profiles", x => x.Id);
                    table.ForeignKey(
                        name: "FK_shd_customer_fiscal_profiles_shd_customers_CustomerId",
                        column: x => x.CustomerId,
                        principalTable: "shd_customers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            // Step 2: Migrate existing fiscal data from customers to the new table.
            // - Only customers with TaxId are migrated (excludes Público General which has no TaxId).
            // - The commercial address is copied to fiscal address as a safe default.
            // - LegalName falls back to Name if not set.
            migrationBuilder.Sql(@"
                INSERT INTO shd_customer_fiscal_profiles (
                    CustomerId, TaxId, LegalName,
                    Street, ExteriorNumber, InteriorNumber,
                    Neighborhood, City, State, PostalCode,
                    FiscalRegime, DefaultCfdiUse,
                    AutoInvoice, SendInvoiceEmail,
                    CaGstNumber, CaPstNumber, CaHstNumber, CaQstNumber,
                    CreatedBy, CreatedAt, IsDeleted
                )
                SELECT
                    Id,
                    TaxId,
                    COALESCE(NULLIF(TRIM(LegalName), ''), Name),
                    Street, ExteriorNumber, InteriorNumber,
                    Neighborhood, City, State, PostalCode,
                    FiscalRegime, DefaultCfdiUse,
                    AutoInvoice, SendInvoiceEmail,
                    CaGstNumber, CaPstNumber, CaHstNumber, CaQstNumber,
                    CreatedBy, CreatedAt, 0
                FROM shd_customers
                WHERE TaxId IS NOT NULL
                  AND TRIM(TaxId) != '';
            ");

            // Step 3: Drop old indexes and fiscal columns from shd_customers
            migrationBuilder.DropIndex(
                name: "IX_shd_customers_CountryCode_TaxId",
                table: "shd_customers");

            migrationBuilder.DropIndex(
                name: "IX_shd_customers_LegalName",
                table: "shd_customers");

            migrationBuilder.DropIndex(
                name: "IX_shd_customers_TaxId",
                table: "shd_customers");

            migrationBuilder.DropColumn(
                name: "AutoInvoice",
                table: "shd_customers");

            migrationBuilder.DropColumn(
                name: "CaGstNumber",
                table: "shd_customers");

            migrationBuilder.DropColumn(
                name: "CaHstNumber",
                table: "shd_customers");

            migrationBuilder.DropColumn(
                name: "CaPstNumber",
                table: "shd_customers");

            migrationBuilder.DropColumn(
                name: "CaQstNumber",
                table: "shd_customers");

            migrationBuilder.DropColumn(
                name: "DefaultCfdiUse",
                table: "shd_customers");

            migrationBuilder.DropColumn(
                name: "FiscalRegime",
                table: "shd_customers");

            migrationBuilder.DropColumn(
                name: "HasFiscalData",
                table: "shd_customers");

            migrationBuilder.DropColumn(
                name: "LegalName",
                table: "shd_customers");

            migrationBuilder.DropColumn(
                name: "SendInvoiceEmail",
                table: "shd_customers");

            migrationBuilder.DropColumn(
                name: "TaxId",
                table: "shd_customers");

            // Step 4: Add indexes on the new table
            migrationBuilder.CreateIndex(
                name: "IX_shd_customer_fiscal_profiles_CustomerId",
                table: "shd_customer_fiscal_profiles",
                column: "CustomerId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_shd_customer_fiscal_profiles_TaxId",
                table: "shd_customer_fiscal_profiles",
                column: "TaxId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "shd_customer_fiscal_profiles");

            migrationBuilder.AddColumn<bool>(
                name: "AutoInvoice",
                table: "shd_customers",
                type: "tinyint(1)",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<string>(
                name: "CaGstNumber",
                table: "shd_customers",
                type: "varchar(20)",
                unicode: false,
                maxLength: 20,
                nullable: true)
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AddColumn<string>(
                name: "CaHstNumber",
                table: "shd_customers",
                type: "varchar(20)",
                unicode: false,
                maxLength: 20,
                nullable: true)
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AddColumn<string>(
                name: "CaPstNumber",
                table: "shd_customers",
                type: "varchar(20)",
                unicode: false,
                maxLength: 20,
                nullable: true)
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AddColumn<string>(
                name: "CaQstNumber",
                table: "shd_customers",
                type: "varchar(20)",
                unicode: false,
                maxLength: 20,
                nullable: true)
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AddColumn<string>(
                name: "DefaultCfdiUse",
                table: "shd_customers",
                type: "varchar(10)",
                maxLength: 10,
                nullable: true)
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AddColumn<string>(
                name: "FiscalRegime",
                table: "shd_customers",
                type: "varchar(5)",
                unicode: false,
                maxLength: 5,
                nullable: true)
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AddColumn<bool>(
                name: "HasFiscalData",
                table: "shd_customers",
                type: "tinyint(1)",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<string>(
                name: "LegalName",
                table: "shd_customers",
                type: "varchar(150)",
                maxLength: 150,
                nullable: true)
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AddColumn<bool>(
                name: "SendInvoiceEmail",
                table: "shd_customers",
                type: "tinyint(1)",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<string>(
                name: "TaxId",
                table: "shd_customers",
                type: "varchar(20)",
                unicode: false,
                maxLength: 20,
                nullable: true)
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateIndex(
                name: "IX_shd_customers_CountryCode_TaxId",
                table: "shd_customers",
                columns: new[] { "CountryCode", "TaxId" },
                unique: true,
                filter: "TaxId IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_shd_customers_LegalName",
                table: "shd_customers",
                column: "LegalName");

            migrationBuilder.CreateIndex(
                name: "IX_shd_customers_TaxId",
                table: "shd_customers",
                column: "TaxId");
        }
    }
}
