using System;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace App.Models.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddInventorySettingsAndPhysicalCounts : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "sh_physical_inventory_counts",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    BatchNumber = table.Column<string>(type: "varchar(20)", maxLength: 20, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    BatchId = table.Column<Guid>(type: "char(36)", nullable: false, collation: "ascii_general_ci"),
                    LocationId = table.Column<int>(type: "int", nullable: false),
                    Reference = table.Column<string>(type: "varchar(50)", maxLength: 50, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Reason = table.Column<string>(type: "varchar(500)", maxLength: 500, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    CountDate = table.Column<DateTime>(type: "datetime(6)", nullable: false),
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
                    table.PrimaryKey("PK_sh_physical_inventory_counts", x => x.Id);
                    table.ForeignKey(
                        name: "FK_sh_physical_inventory_counts_sh_locations_LocationId",
                        column: x => x.LocationId,
                        principalTable: "sh_locations",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "stg_inventory_settings",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    ShowStockDuringPhysicalCount = table.Column<bool>(type: "tinyint(1)", nullable: false),
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
                    table.PrimaryKey("PK_stg_inventory_settings", x => x.Id);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "sh_physical_inventory_count_lines",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    PhysicalInventoryCountId = table.Column<long>(type: "bigint", nullable: false),
                    ProductId = table.Column<long>(type: "bigint", nullable: false),
                    SystemQuantity = table.Column<decimal>(type: "decimal(15,6)", nullable: false),
                    CountedQuantity = table.Column<decimal>(type: "decimal(15,6)", nullable: false),
                    Difference = table.Column<decimal>(type: "decimal(15,6)", nullable: false),
                    InventoryMovementId = table.Column<long>(type: "bigint", nullable: true),
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
                    table.PrimaryKey("PK_sh_physical_inventory_count_lines", x => x.Id);
                    table.ForeignKey(
                        name: "FK_sh_physical_inventory_count_lines_sh_inventory_movements_Inv~",
                        column: x => x.InventoryMovementId,
                        principalTable: "sh_inventory_movements",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_sh_physical_inventory_count_lines_sh_physical_inventory_coun~",
                        column: x => x.PhysicalInventoryCountId,
                        principalTable: "sh_physical_inventory_counts",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_sh_physical_inventory_count_lines_sh_products_ProductId",
                        column: x => x.ProductId,
                        principalTable: "sh_products",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateIndex(
                name: "IX_sh_physical_inventory_count_lines_InventoryMovementId",
                table: "sh_physical_inventory_count_lines",
                column: "InventoryMovementId");

            migrationBuilder.CreateIndex(
                name: "IX_sh_physical_inventory_count_lines_PhysicalInventoryCountId",
                table: "sh_physical_inventory_count_lines",
                column: "PhysicalInventoryCountId");

            migrationBuilder.CreateIndex(
                name: "IX_sh_physical_inventory_count_lines_ProductId",
                table: "sh_physical_inventory_count_lines",
                column: "ProductId");

            migrationBuilder.CreateIndex(
                name: "IX_sh_physical_inventory_counts_BatchId",
                table: "sh_physical_inventory_counts",
                column: "BatchId");

            migrationBuilder.CreateIndex(
                name: "IX_sh_physical_inventory_counts_LocationId_CountDate",
                table: "sh_physical_inventory_counts",
                columns: new[] { "LocationId", "CountDate" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "sh_physical_inventory_count_lines");

            migrationBuilder.DropTable(
                name: "stg_inventory_settings");

            migrationBuilder.DropTable(
                name: "sh_physical_inventory_counts");
        }
    }
}
