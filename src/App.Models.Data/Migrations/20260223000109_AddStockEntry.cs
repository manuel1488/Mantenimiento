using System;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace App.Models.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddStockEntry : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<long>(
                name: "StockEntryId",
                table: "sh_inventory_movements",
                type: "bigint",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "sh_stock_entries",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    MovementType = table.Column<string>(type: "varchar(20)", maxLength: 20, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    MovementSubType = table.Column<string>(type: "varchar(20)", maxLength: 20, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    LocationId = table.Column<int>(type: "int", nullable: false),
                    SupplierId = table.Column<long>(type: "bigint", nullable: true),
                    SupplierName = table.Column<string>(type: "varchar(100)", maxLength: 100, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    DocumentNumber = table.Column<string>(type: "varchar(100)", maxLength: 100, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Reference = table.Column<string>(type: "varchar(50)", maxLength: 50, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Reason = table.Column<string>(type: "varchar(500)", maxLength: 500, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    EntryDate = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    AttachmentFileName = table.Column<string>(type: "varchar(255)", maxLength: 255, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    AttachmentMimeType = table.Column<string>(type: "varchar(100)", maxLength: 100, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    AttachmentData = table.Column<byte[]>(type: "LONGBLOB", nullable: true),
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
                    table.PrimaryKey("PK_sh_stock_entries", x => x.Id);
                    table.ForeignKey(
                        name: "FK_sh_stock_entries_sh_locations_LocationId",
                        column: x => x.LocationId,
                        principalTable: "sh_locations",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_sh_stock_entries_sh_suppliers_SupplierId",
                        column: x => x.SupplierId,
                        principalTable: "sh_suppliers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "sh_stock_entry_items",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    StockEntryId = table.Column<long>(type: "bigint", nullable: false),
                    ProductId = table.Column<long>(type: "bigint", nullable: false),
                    Quantity = table.Column<decimal>(type: "decimal(15,6)", nullable: false),
                    UnitCost = table.Column<decimal>(type: "decimal(10,2)", nullable: true),
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
                    table.PrimaryKey("PK_sh_stock_entry_items", x => x.Id);
                    table.ForeignKey(
                        name: "FK_sh_stock_entry_items_sh_inventory_movements_InventoryMovemen~",
                        column: x => x.InventoryMovementId,
                        principalTable: "sh_inventory_movements",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_sh_stock_entry_items_sh_products_ProductId",
                        column: x => x.ProductId,
                        principalTable: "sh_products",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_sh_stock_entry_items_sh_stock_entries_StockEntryId",
                        column: x => x.StockEntryId,
                        principalTable: "sh_stock_entries",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateIndex(
                name: "IX_sh_inventory_movements_StockEntryId",
                table: "sh_inventory_movements",
                column: "StockEntryId");

            migrationBuilder.CreateIndex(
                name: "IX_sh_stock_entries_DocumentNumber",
                table: "sh_stock_entries",
                column: "DocumentNumber");

            migrationBuilder.CreateIndex(
                name: "IX_sh_stock_entries_LocationId_EntryDate",
                table: "sh_stock_entries",
                columns: new[] { "LocationId", "EntryDate" });

            migrationBuilder.CreateIndex(
                name: "IX_sh_stock_entries_SupplierId",
                table: "sh_stock_entries",
                column: "SupplierId");

            migrationBuilder.CreateIndex(
                name: "IX_sh_stock_entry_items_InventoryMovementId",
                table: "sh_stock_entry_items",
                column: "InventoryMovementId");

            migrationBuilder.CreateIndex(
                name: "IX_sh_stock_entry_items_ProductId",
                table: "sh_stock_entry_items",
                column: "ProductId");

            migrationBuilder.CreateIndex(
                name: "IX_sh_stock_entry_items_StockEntryId",
                table: "sh_stock_entry_items",
                column: "StockEntryId");

            migrationBuilder.AddForeignKey(
                name: "FK_sh_inventory_movements_sh_stock_entries_StockEntryId",
                table: "sh_inventory_movements",
                column: "StockEntryId",
                principalTable: "sh_stock_entries",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_sh_inventory_movements_sh_stock_entries_StockEntryId",
                table: "sh_inventory_movements");

            migrationBuilder.DropTable(
                name: "sh_stock_entry_items");

            migrationBuilder.DropTable(
                name: "sh_stock_entries");

            migrationBuilder.DropIndex(
                name: "IX_sh_inventory_movements_StockEntryId",
                table: "sh_inventory_movements");

            migrationBuilder.DropColumn(
                name: "StockEntryId",
                table: "sh_inventory_movements");
        }
    }
}
