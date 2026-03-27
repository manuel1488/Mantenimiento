using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace App.Models.Data.Migrations
{
    /// <inheritdoc />
    public partial class AllowMultipleInvoicesPerSale : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // MySQL requires dropping the FK before dropping the index it depends on
            migrationBuilder.DropForeignKey(
                name: "FK_mx_invoices_sh_sales_SaleId",
                table: "mx_invoices");

            migrationBuilder.DropIndex(
                name: "IX_mx_invoices_SaleId",
                table: "mx_invoices");

            migrationBuilder.CreateIndex(
                name: "IX_mx_invoices_SaleId",
                table: "mx_invoices",
                column: "SaleId");

            migrationBuilder.AddForeignKey(
                name: "FK_mx_invoices_sh_sales_SaleId",
                table: "mx_invoices",
                column: "SaleId",
                principalTable: "sh_sales",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_mx_invoices_sh_sales_SaleId",
                table: "mx_invoices");

            migrationBuilder.DropIndex(
                name: "IX_mx_invoices_SaleId",
                table: "mx_invoices");

            migrationBuilder.CreateIndex(
                name: "IX_mx_invoices_SaleId",
                table: "mx_invoices",
                column: "SaleId",
                unique: true);

            migrationBuilder.AddForeignKey(
                name: "FK_mx_invoices_sh_sales_SaleId",
                table: "mx_invoices",
                column: "SaleId",
                principalTable: "sh_sales",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }
    }
}
