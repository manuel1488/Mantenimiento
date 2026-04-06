using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace App.Models.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddInvoiceBackdateSupport : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "MxMaxBackdateHours",
                table: "stg_tax_settings",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "RequestedInvoiceDate",
                table: "mx_invoices",
                type: "datetime(6)",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "MxMaxBackdateHours",
                table: "stg_tax_settings");

            migrationBuilder.DropColumn(
                name: "RequestedInvoiceDate",
                table: "mx_invoices");
        }
    }
}
