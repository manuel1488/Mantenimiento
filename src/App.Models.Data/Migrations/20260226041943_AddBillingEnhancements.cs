using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace App.Models.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddBillingEnhancements : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "FiscalRegime",
                table: "mx_invoices");

            migrationBuilder.DropColumn(
                name: "PaymentType",
                table: "mx_invoices");

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

            migrationBuilder.AlterColumn<string>(
                name: "User",
                table: "mx_pac_settings",
                type: "varchar(100)",
                maxLength: 100,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "varchar(50)",
                oldMaxLength: 50)
                .Annotation("MySql:CharSet", "utf8mb4")
                .OldAnnotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AlterColumn<string>(
                name: "Password",
                table: "mx_pac_settings",
                type: "varchar(200)",
                maxLength: 200,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "varchar(100)",
                oldMaxLength: 100)
                .Annotation("MySql:CharSet", "utf8mb4")
                .OldAnnotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AddColumn<string>(
                name: "CsdCertificateBase64",
                table: "mx_pac_settings",
                type: "text",
                nullable: true)
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AddColumn<string>(
                name: "CsdPassword",
                table: "mx_pac_settings",
                type: "varchar(200)",
                maxLength: 200,
                nullable: true)
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AddColumn<string>(
                name: "CsdPrivateKeyBase64",
                table: "mx_pac_settings",
                type: "text",
                nullable: true)
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AddColumn<string>(
                name: "InvoiceSerie",
                table: "mx_pac_settings",
                type: "varchar(10)",
                unicode: false,
                maxLength: 10,
                nullable: true,
                defaultValue: "A")
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AddColumn<bool>(
                name: "IsProduction",
                table: "mx_pac_settings",
                type: "tinyint(1)",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<string>(
                name: "IssuerFiscalRegime",
                table: "mx_pac_settings",
                type: "varchar(5)",
                unicode: false,
                maxLength: 5,
                nullable: true)
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AddColumn<string>(
                name: "IssuerLegalName",
                table: "mx_pac_settings",
                type: "varchar(150)",
                maxLength: 150,
                nullable: true)
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AddColumn<string>(
                name: "IssuerPostalCode",
                table: "mx_pac_settings",
                type: "varchar(10)",
                unicode: false,
                maxLength: 10,
                nullable: true)
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AddColumn<string>(
                name: "IssuerRfc",
                table: "mx_pac_settings",
                type: "varchar(20)",
                unicode: false,
                maxLength: 20,
                nullable: true)
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AddColumn<string>(
                name: "Token",
                table: "mx_pac_settings",
                type: "varchar(500)",
                maxLength: 500,
                nullable: true)
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AlterColumn<string>(
                name: "Status",
                table: "mx_invoices",
                type: "varchar(20)",
                unicode: false,
                maxLength: 20,
                nullable: false,
                defaultValue: "Draft",
                oldClrType: typeof(string),
                oldType: "varchar(20)",
                oldMaxLength: 20)
                .Annotation("MySql:CharSet", "utf8mb4")
                .OldAnnotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AddColumn<string>(
                name: "CadenaOriginalSat",
                table: "mx_invoices",
                type: "text",
                nullable: true)
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AddColumn<string>(
                name: "CancellationStatus",
                table: "mx_invoices",
                type: "varchar(20)",
                maxLength: 20,
                nullable: true)
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AddColumn<string>(
                name: "Currency",
                table: "mx_invoices",
                type: "varchar(5)",
                unicode: false,
                maxLength: 5,
                nullable: false,
                defaultValue: "MXN")
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AddColumn<string>(
                name: "CustomerFiscalRegime",
                table: "mx_invoices",
                type: "varchar(5)",
                unicode: false,
                maxLength: 5,
                nullable: false,
                defaultValue: "")
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AddColumn<string>(
                name: "CustomerLegalName",
                table: "mx_invoices",
                type: "varchar(150)",
                maxLength: 150,
                nullable: false,
                defaultValue: "")
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AddColumn<string>(
                name: "CustomerPostalCode",
                table: "mx_invoices",
                type: "varchar(10)",
                unicode: false,
                maxLength: 10,
                nullable: false,
                defaultValue: "")
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AddColumn<string>(
                name: "CustomerRfc",
                table: "mx_invoices",
                type: "varchar(20)",
                unicode: false,
                maxLength: 20,
                nullable: false,
                defaultValue: "")
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AddColumn<decimal>(
                name: "ExchangeRate",
                table: "mx_invoices",
                type: "decimal(18,6)",
                nullable: false,
                defaultValue: 1m);

            migrationBuilder.AddColumn<long>(
                name: "Folio",
                table: "mx_invoices",
                type: "bigint",
                nullable: false,
                defaultValue: 0L);

            migrationBuilder.AddColumn<bool>(
                name: "IsStamped",
                table: "mx_invoices",
                type: "tinyint(1)",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<string>(
                name: "IssuerFiscalRegime",
                table: "mx_invoices",
                type: "varchar(5)",
                unicode: false,
                maxLength: 5,
                nullable: false,
                defaultValue: "")
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AddColumn<string>(
                name: "IssuerLegalName",
                table: "mx_invoices",
                type: "varchar(150)",
                maxLength: 150,
                nullable: false,
                defaultValue: "")
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AddColumn<string>(
                name: "IssuerPostalCode",
                table: "mx_invoices",
                type: "varchar(10)",
                unicode: false,
                maxLength: 10,
                nullable: false,
                defaultValue: "")
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AddColumn<string>(
                name: "IssuerRfc",
                table: "mx_invoices",
                type: "varchar(20)",
                unicode: false,
                maxLength: 20,
                nullable: false,
                defaultValue: "")
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AddColumn<string>(
                name: "NoCertificadoCfdi",
                table: "mx_invoices",
                type: "varchar(20)",
                unicode: false,
                maxLength: 20,
                nullable: true)
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AddColumn<string>(
                name: "NoCertificadoSat",
                table: "mx_invoices",
                type: "varchar(20)",
                unicode: false,
                maxLength: 20,
                nullable: true)
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AddColumn<string>(
                name: "PaymentForm",
                table: "mx_invoices",
                type: "varchar(5)",
                unicode: false,
                maxLength: 5,
                nullable: false,
                defaultValue: "")
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AddColumn<string>(
                name: "SelloCfdi",
                table: "mx_invoices",
                type: "text",
                nullable: true)
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AddColumn<string>(
                name: "SelloSat",
                table: "mx_invoices",
                type: "text",
                nullable: true)
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AddColumn<string>(
                name: "Serie",
                table: "mx_invoices",
                type: "varchar(10)",
                unicode: false,
                maxLength: 10,
                nullable: true)
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AddColumn<DateTime>(
                name: "StampDate",
                table: "mx_invoices",
                type: "datetime(6)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "StampError",
                table: "mx_invoices",
                type: "varchar(1000)",
                maxLength: 1000,
                nullable: true)
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AddColumn<decimal>(
                name: "Subtotal",
                table: "mx_invoices",
                type: "decimal(18,4)",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<decimal>(
                name: "TaxAmount",
                table: "mx_invoices",
                type: "decimal(18,4)",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<decimal>(
                name: "Total",
                table: "mx_invoices",
                type: "decimal(18,4)",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<string>(
                name: "Uuid",
                table: "mx_invoices",
                type: "varchar(36)",
                unicode: false,
                maxLength: 36,
                nullable: true)
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateIndex(
                name: "IX_mx_invoices_IsStamped",
                table: "mx_invoices",
                column: "IsStamped");

            migrationBuilder.CreateIndex(
                name: "IX_mx_invoices_Serie_Folio",
                table: "mx_invoices",
                columns: new[] { "Serie", "Folio" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_mx_invoices_Status",
                table: "mx_invoices",
                column: "Status");

            migrationBuilder.CreateIndex(
                name: "IX_mx_invoices_Uuid",
                table: "mx_invoices",
                column: "Uuid",
                unique: true,
                filter: "Uuid IS NOT NULL");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_mx_invoices_IsStamped",
                table: "mx_invoices");

            migrationBuilder.DropIndex(
                name: "IX_mx_invoices_Serie_Folio",
                table: "mx_invoices");

            migrationBuilder.DropIndex(
                name: "IX_mx_invoices_Status",
                table: "mx_invoices");

            migrationBuilder.DropIndex(
                name: "IX_mx_invoices_Uuid",
                table: "mx_invoices");

            migrationBuilder.DropColumn(
                name: "FiscalRegime",
                table: "shd_customers");

            migrationBuilder.DropColumn(
                name: "HasFiscalData",
                table: "shd_customers");

            migrationBuilder.DropColumn(
                name: "CsdCertificateBase64",
                table: "mx_pac_settings");

            migrationBuilder.DropColumn(
                name: "CsdPassword",
                table: "mx_pac_settings");

            migrationBuilder.DropColumn(
                name: "CsdPrivateKeyBase64",
                table: "mx_pac_settings");

            migrationBuilder.DropColumn(
                name: "InvoiceSerie",
                table: "mx_pac_settings");

            migrationBuilder.DropColumn(
                name: "IsProduction",
                table: "mx_pac_settings");

            migrationBuilder.DropColumn(
                name: "IssuerFiscalRegime",
                table: "mx_pac_settings");

            migrationBuilder.DropColumn(
                name: "IssuerLegalName",
                table: "mx_pac_settings");

            migrationBuilder.DropColumn(
                name: "IssuerPostalCode",
                table: "mx_pac_settings");

            migrationBuilder.DropColumn(
                name: "IssuerRfc",
                table: "mx_pac_settings");

            migrationBuilder.DropColumn(
                name: "Token",
                table: "mx_pac_settings");

            migrationBuilder.DropColumn(
                name: "CadenaOriginalSat",
                table: "mx_invoices");

            migrationBuilder.DropColumn(
                name: "CancellationStatus",
                table: "mx_invoices");

            migrationBuilder.DropColumn(
                name: "Currency",
                table: "mx_invoices");

            migrationBuilder.DropColumn(
                name: "CustomerFiscalRegime",
                table: "mx_invoices");

            migrationBuilder.DropColumn(
                name: "CustomerLegalName",
                table: "mx_invoices");

            migrationBuilder.DropColumn(
                name: "CustomerPostalCode",
                table: "mx_invoices");

            migrationBuilder.DropColumn(
                name: "CustomerRfc",
                table: "mx_invoices");

            migrationBuilder.DropColumn(
                name: "ExchangeRate",
                table: "mx_invoices");

            migrationBuilder.DropColumn(
                name: "Folio",
                table: "mx_invoices");

            migrationBuilder.DropColumn(
                name: "IsStamped",
                table: "mx_invoices");

            migrationBuilder.DropColumn(
                name: "IssuerFiscalRegime",
                table: "mx_invoices");

            migrationBuilder.DropColumn(
                name: "IssuerLegalName",
                table: "mx_invoices");

            migrationBuilder.DropColumn(
                name: "IssuerPostalCode",
                table: "mx_invoices");

            migrationBuilder.DropColumn(
                name: "IssuerRfc",
                table: "mx_invoices");

            migrationBuilder.DropColumn(
                name: "NoCertificadoCfdi",
                table: "mx_invoices");

            migrationBuilder.DropColumn(
                name: "NoCertificadoSat",
                table: "mx_invoices");

            migrationBuilder.DropColumn(
                name: "PaymentForm",
                table: "mx_invoices");

            migrationBuilder.DropColumn(
                name: "SelloCfdi",
                table: "mx_invoices");

            migrationBuilder.DropColumn(
                name: "SelloSat",
                table: "mx_invoices");

            migrationBuilder.DropColumn(
                name: "Serie",
                table: "mx_invoices");

            migrationBuilder.DropColumn(
                name: "StampDate",
                table: "mx_invoices");

            migrationBuilder.DropColumn(
                name: "StampError",
                table: "mx_invoices");

            migrationBuilder.DropColumn(
                name: "Subtotal",
                table: "mx_invoices");

            migrationBuilder.DropColumn(
                name: "TaxAmount",
                table: "mx_invoices");

            migrationBuilder.DropColumn(
                name: "Total",
                table: "mx_invoices");

            migrationBuilder.DropColumn(
                name: "Uuid",
                table: "mx_invoices");

            migrationBuilder.UpdateData(
                table: "mx_pac_settings",
                keyColumn: "User",
                keyValue: null,
                column: "User",
                value: "");

            migrationBuilder.AlterColumn<string>(
                name: "User",
                table: "mx_pac_settings",
                type: "varchar(50)",
                maxLength: 50,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "varchar(100)",
                oldMaxLength: 100,
                oldNullable: true)
                .Annotation("MySql:CharSet", "utf8mb4")
                .OldAnnotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.UpdateData(
                table: "mx_pac_settings",
                keyColumn: "Password",
                keyValue: null,
                column: "Password",
                value: "");

            migrationBuilder.AlterColumn<string>(
                name: "Password",
                table: "mx_pac_settings",
                type: "varchar(100)",
                maxLength: 100,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "varchar(200)",
                oldMaxLength: 200,
                oldNullable: true)
                .Annotation("MySql:CharSet", "utf8mb4")
                .OldAnnotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AlterColumn<string>(
                name: "Status",
                table: "mx_invoices",
                type: "varchar(20)",
                maxLength: 20,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "varchar(20)",
                oldUnicode: false,
                oldMaxLength: 20,
                oldDefaultValue: "Draft")
                .Annotation("MySql:CharSet", "utf8mb4")
                .OldAnnotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AddColumn<string>(
                name: "FiscalRegime",
                table: "mx_invoices",
                type: "varchar(5)",
                maxLength: 5,
                nullable: false,
                defaultValue: "")
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AddColumn<string>(
                name: "PaymentType",
                table: "mx_invoices",
                type: "varchar(5)",
                maxLength: 5,
                nullable: false,
                defaultValue: "")
                .Annotation("MySql:CharSet", "utf8mb4");
        }
    }
}
