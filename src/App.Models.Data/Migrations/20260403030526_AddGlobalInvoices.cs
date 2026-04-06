using System;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace App.Models.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddGlobalInvoices : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "mx_global_invoices",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    Serie = table.Column<string>(type: "varchar(10)", unicode: false, maxLength: 10, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Folio = table.Column<long>(type: "bigint", nullable: false),
                    Uuid = table.Column<string>(type: "varchar(36)", unicode: false, maxLength: 36, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Periodicity = table.Column<int>(type: "int", nullable: false),
                    StartDate = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    EndDate = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    PeriodMonth = table.Column<string>(type: "varchar(2)", unicode: false, maxLength: 2, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    PeriodYear = table.Column<int>(type: "int", nullable: false),
                    PaymentForm = table.Column<string>(type: "varchar(5)", unicode: false, maxLength: 5, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    SaleCount = table.Column<int>(type: "int", nullable: false),
                    Subtotal = table.Column<decimal>(type: "decimal(18,4)", nullable: false),
                    DiscountAmount = table.Column<decimal>(type: "decimal(18,4)", nullable: false),
                    TaxAmount = table.Column<decimal>(type: "decimal(18,4)", nullable: false),
                    Total = table.Column<decimal>(type: "decimal(18,4)", nullable: false),
                    Status = table.Column<int>(type: "int", nullable: false),
                    XmlContent = table.Column<string>(type: "mediumtext", nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    StampDate = table.Column<DateTime>(type: "datetime(6)", nullable: true),
                    StampError = table.Column<string>(type: "varchar(1000)", maxLength: 1000, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    CancellationDate = table.Column<DateTime>(type: "datetime(6)", nullable: true),
                    CancellationReason = table.Column<string>(type: "varchar(2)", unicode: false, maxLength: 2, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    ReplacementUuid = table.Column<string>(type: "varchar(36)", maxLength: 36, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    CancellationStatus = table.Column<string>(type: "varchar(20)", maxLength: 20, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    CancellationAcuse = table.Column<string>(type: "mediumtext", nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    IssuerRfc = table.Column<string>(type: "varchar(20)", unicode: false, maxLength: 20, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    IssuerLegalName = table.Column<string>(type: "varchar(150)", maxLength: 150, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    IssuerFiscalRegime = table.Column<string>(type: "varchar(5)", unicode: false, maxLength: 5, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    IssuerPostalCode = table.Column<string>(type: "varchar(10)", unicode: false, maxLength: 10, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    NoCertificadoSat = table.Column<string>(type: "varchar(20)", unicode: false, maxLength: 20, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    NoCertificadoCfdi = table.Column<string>(type: "varchar(20)", unicode: false, maxLength: 20, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    SelloSat = table.Column<string>(type: "text", nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    SelloCfdi = table.Column<string>(type: "text", nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    CadenaOriginalSat = table.Column<string>(type: "text", nullable: true)
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
                    table.PrimaryKey("PK_mx_global_invoices", x => x.Id);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "mx_global_invoice_sales",
                columns: table => new
                {
                    GlobalInvoiceId = table.Column<long>(type: "bigint", nullable: false),
                    SaleId = table.Column<long>(type: "bigint", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_mx_global_invoice_sales", x => new { x.GlobalInvoiceId, x.SaleId });
                    table.ForeignKey(
                        name: "FK_mx_global_invoice_sales_mx_global_invoices_GlobalInvoiceId",
                        column: x => x.GlobalInvoiceId,
                        principalTable: "mx_global_invoices",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_mx_global_invoice_sales_sh_sales_SaleId",
                        column: x => x.SaleId,
                        principalTable: "sh_sales",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateIndex(
                name: "IX_mx_global_invoice_sales_SaleId",
                table: "mx_global_invoice_sales",
                column: "SaleId");

            migrationBuilder.CreateIndex(
                name: "IX_mx_global_invoices_Serie_Folio",
                table: "mx_global_invoices",
                columns: new[] { "Serie", "Folio" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_mx_global_invoices_StartDate_EndDate",
                table: "mx_global_invoices",
                columns: new[] { "StartDate", "EndDate" });

            migrationBuilder.CreateIndex(
                name: "IX_mx_global_invoices_Status",
                table: "mx_global_invoices",
                column: "Status");

            migrationBuilder.CreateIndex(
                name: "IX_mx_global_invoices_Uuid",
                table: "mx_global_invoices",
                column: "Uuid",
                unique: true,
                filter: "Uuid IS NOT NULL");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "mx_global_invoice_sales");

            migrationBuilder.DropTable(
                name: "mx_global_invoices");
        }
    }
}
