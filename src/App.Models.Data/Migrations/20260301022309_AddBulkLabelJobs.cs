using System;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace App.Models.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddBulkLabelJobs : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "sh_bulk_label_jobs",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    ProductId = table.Column<long>(type: "bigint", nullable: false),
                    Quantity = table.Column<decimal>(type: "decimal(10,3)", nullable: false),
                    UnitMeasureCode = table.Column<string>(type: "varchar(10)", maxLength: 10, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    UnitPrice = table.Column<decimal>(type: "decimal(10,2)", nullable: false),
                    TotalPrice = table.Column<decimal>(type: "decimal(10,2)", nullable: false),
                    LabelCount = table.Column<int>(type: "int", nullable: false, defaultValue: 1),
                    BatchNumber = table.Column<string>(type: "varchar(50)", maxLength: 50, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Notes = table.Column<string>(type: "varchar(200)", maxLength: 200, nullable: true)
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
                    table.PrimaryKey("PK_sh_bulk_label_jobs", x => x.Id);
                    table.ForeignKey(
                        name: "FK_sh_bulk_label_jobs_sh_products_ProductId",
                        column: x => x.ProductId,
                        principalTable: "sh_products",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateIndex(
                name: "IX_sh_bulk_label_jobs_BatchNumber",
                table: "sh_bulk_label_jobs",
                column: "BatchNumber");

            migrationBuilder.CreateIndex(
                name: "IX_sh_bulk_label_jobs_CreatedAt",
                table: "sh_bulk_label_jobs",
                column: "CreatedAt");

            migrationBuilder.CreateIndex(
                name: "IX_sh_bulk_label_jobs_ProductId",
                table: "sh_bulk_label_jobs",
                column: "ProductId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "sh_bulk_label_jobs");
        }
    }
}
