using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace App.Models.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddQuotationLinkToSalesAndRemissions : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<long>(
                name: "QuotationId",
                table: "sh_sales",
                type: "bigint",
                nullable: true);

            migrationBuilder.AddColumn<long>(
                name: "QuotationId",
                table: "sh_remissions",
                type: "bigint",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_sh_sales_QuotationId",
                table: "sh_sales",
                column: "QuotationId");

            migrationBuilder.CreateIndex(
                name: "IX_sh_remissions_QuotationId",
                table: "sh_remissions",
                column: "QuotationId");

            migrationBuilder.AddForeignKey(
                name: "FK_sh_remissions_sh_quotations_QuotationId",
                table: "sh_remissions",
                column: "QuotationId",
                principalTable: "sh_quotations",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_sh_sales_sh_quotations_QuotationId",
                table: "sh_sales",
                column: "QuotationId",
                principalTable: "sh_quotations",
                principalColumn: "Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_sh_remissions_sh_quotations_QuotationId",
                table: "sh_remissions");

            migrationBuilder.DropForeignKey(
                name: "FK_sh_sales_sh_quotations_QuotationId",
                table: "sh_sales");

            migrationBuilder.DropIndex(
                name: "IX_sh_sales_QuotationId",
                table: "sh_sales");

            migrationBuilder.DropIndex(
                name: "IX_sh_remissions_QuotationId",
                table: "sh_remissions");

            migrationBuilder.DropColumn(
                name: "QuotationId",
                table: "sh_sales");

            migrationBuilder.DropColumn(
                name: "QuotationId",
                table: "sh_remissions");
        }
    }
}
