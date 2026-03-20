using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace App.Models.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddQuotationPdfStorage : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<byte[]>(
                name: "PdfData",
                table: "sh_quotations",
                type: "longblob",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "PdfGeneratedAt",
                table: "sh_quotations",
                type: "datetime(6)",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "PdfData",
                table: "sh_quotations");

            migrationBuilder.DropColumn(
                name: "PdfGeneratedAt",
                table: "sh_quotations");
        }
    }
}
