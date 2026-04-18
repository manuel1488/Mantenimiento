using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace App.Models.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddCustomerContactNameAndFiscalEmail : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "ContactName",
                table: "shd_customers",
                type: "varchar(100)",
                maxLength: 100,
                nullable: true)
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AddColumn<string>(
                name: "FiscalEmail",
                table: "shd_customer_fiscal_profiles",
                type: "varchar(100)",
                maxLength: 100,
                nullable: true)
                .Annotation("MySql:CharSet", "utf8mb4");

            // Copy existing commercial email → fiscal email for customers that already have a fiscal profile
            migrationBuilder.Sql(@"
                UPDATE shd_customer_fiscal_profiles fp
                JOIN shd_customers c ON c.Id = fp.CustomerId
                SET fp.FiscalEmail = c.Email
                WHERE c.Email IS NOT NULL
                  AND c.Email != ''
                  AND fp.FiscalEmail IS NULL;
            ");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ContactName",
                table: "shd_customers");

            migrationBuilder.DropColumn(
                name: "FiscalEmail",
                table: "shd_customer_fiscal_profiles");
        }
    }
}
