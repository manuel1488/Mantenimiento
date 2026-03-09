using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace App.Models.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddInvoiceCancellationFields : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Clear legacy free-text cancellation reasons before reducing column size
            migrationBuilder.Sql("UPDATE `mx_invoices` SET `CancellationReason` = NULL WHERE `CancellationReason` IS NOT NULL AND LENGTH(`CancellationReason`) > 2;");

            migrationBuilder.AlterColumn<string>(
                name: "CancellationReason",
                table: "mx_invoices",
                type: "varchar(2)",
                maxLength: 2,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "varchar(100)",
                oldMaxLength: 100,
                oldNullable: true)
                .Annotation("MySql:CharSet", "utf8mb4")
                .OldAnnotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AddColumn<string>(
                name: "CancellationAcuse",
                table: "mx_invoices",
                type: "mediumtext",
                nullable: true)
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AddColumn<string>(
                name: "CancellationIsCancelable",
                table: "mx_invoices",
                type: "varchar(100)",
                maxLength: 100,
                nullable: true)
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AddColumn<string>(
                name: "CancellationStatusSat",
                table: "mx_invoices",
                type: "varchar(50)",
                maxLength: 50,
                nullable: true)
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AddColumn<string>(
                name: "CancellationUuidStatusCode",
                table: "mx_invoices",
                type: "varchar(10)",
                maxLength: 10,
                nullable: true)
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AddColumn<string>(
                name: "ReplacementUuid",
                table: "mx_invoices",
                type: "varchar(36)",
                maxLength: 36,
                nullable: true)
                .Annotation("MySql:CharSet", "utf8mb4");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "CancellationAcuse",
                table: "mx_invoices");

            migrationBuilder.DropColumn(
                name: "CancellationIsCancelable",
                table: "mx_invoices");

            migrationBuilder.DropColumn(
                name: "CancellationStatusSat",
                table: "mx_invoices");

            migrationBuilder.DropColumn(
                name: "CancellationUuidStatusCode",
                table: "mx_invoices");

            migrationBuilder.DropColumn(
                name: "ReplacementUuid",
                table: "mx_invoices");

            migrationBuilder.AlterColumn<string>(
                name: "CancellationReason",
                table: "mx_invoices",
                type: "varchar(100)",
                maxLength: 100,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "varchar(2)",
                oldMaxLength: 2,
                oldNullable: true)
                .Annotation("MySql:CharSet", "utf8mb4")
                .OldAnnotation("MySql:CharSet", "utf8mb4");
        }
    }
}
