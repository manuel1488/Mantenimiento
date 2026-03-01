using System;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace App.Models.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddAdjustmentEntries : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<long>(
                name: "AdjustmentEntryId",
                table: "sh_inventory_movements",
                type: "bigint",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "sh_adjustment_entries",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    AdjustmentType = table.Column<string>(type: "varchar(20)", maxLength: 20, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    LocationId = table.Column<int>(type: "int", nullable: false),
                    Reference = table.Column<string>(type: "varchar(50)", maxLength: 50, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Reason = table.Column<string>(type: "varchar(500)", maxLength: 500, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    AdjustmentDate = table.Column<DateTime>(type: "datetime(6)", nullable: false),
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
                    table.PrimaryKey("PK_sh_adjustment_entries", x => x.Id);
                    table.ForeignKey(
                        name: "FK_sh_adjustment_entries_sh_locations_LocationId",
                        column: x => x.LocationId,
                        principalTable: "sh_locations",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "sh_adjustment_entry_items",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    AdjustmentEntryId = table.Column<long>(type: "bigint", nullable: false),
                    ProductId = table.Column<long>(type: "bigint", nullable: false),
                    NewQuantity = table.Column<decimal>(type: "decimal(15,6)", nullable: false),
                    PreviousQuantity = table.Column<decimal>(type: "decimal(15,6)", nullable: false),
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
                    table.PrimaryKey("PK_sh_adjustment_entry_items", x => x.Id);
                    table.ForeignKey(
                        name: "FK_sh_adjustment_entry_items_sh_adjustment_entries_AdjustmentEn~",
                        column: x => x.AdjustmentEntryId,
                        principalTable: "sh_adjustment_entries",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_sh_adjustment_entry_items_sh_inventory_movements_InventoryMo~",
                        column: x => x.InventoryMovementId,
                        principalTable: "sh_inventory_movements",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_sh_adjustment_entry_items_sh_products_ProductId",
                        column: x => x.ProductId,
                        principalTable: "sh_products",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateIndex(
                name: "IX_sh_inventory_movements_AdjustmentEntryId",
                table: "sh_inventory_movements",
                column: "AdjustmentEntryId");

            migrationBuilder.CreateIndex(
                name: "IX_sh_adjustment_entries_LocationId_AdjustmentDate",
                table: "sh_adjustment_entries",
                columns: new[] { "LocationId", "AdjustmentDate" });

            migrationBuilder.CreateIndex(
                name: "IX_sh_adjustment_entry_items_AdjustmentEntryId",
                table: "sh_adjustment_entry_items",
                column: "AdjustmentEntryId");

            migrationBuilder.CreateIndex(
                name: "IX_sh_adjustment_entry_items_InventoryMovementId",
                table: "sh_adjustment_entry_items",
                column: "InventoryMovementId");

            migrationBuilder.CreateIndex(
                name: "IX_sh_adjustment_entry_items_ProductId",
                table: "sh_adjustment_entry_items",
                column: "ProductId");

            migrationBuilder.AddForeignKey(
                name: "FK_sh_inventory_movements_sh_adjustment_entries_AdjustmentEntry~",
                table: "sh_inventory_movements",
                column: "AdjustmentEntryId",
                principalTable: "sh_adjustment_entries",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_sh_inventory_movements_sh_adjustment_entries_AdjustmentEntry~",
                table: "sh_inventory_movements");

            migrationBuilder.DropTable(
                name: "sh_adjustment_entry_items");

            migrationBuilder.DropTable(
                name: "sh_adjustment_entries");

            migrationBuilder.DropIndex(
                name: "IX_sh_inventory_movements_AdjustmentEntryId",
                table: "sh_inventory_movements");

            migrationBuilder.DropColumn(
                name: "AdjustmentEntryId",
                table: "sh_inventory_movements");
        }
    }
}
