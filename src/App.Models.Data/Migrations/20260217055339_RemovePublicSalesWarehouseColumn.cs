using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace App.Models.Data.Migrations
{
    /// <inheritdoc />
    public partial class RemovePublicSalesWarehouseColumn : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_sh_warehouses_IsPublicSalesWarehouse_IsActive_IsDeleted",
                table: "sh_warehouses");

            migrationBuilder.DropColumn(
                name: "IsPublicSalesWarehouse",
                table: "sh_warehouses");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "IsPublicSalesWarehouse",
                table: "sh_warehouses",
                type: "tinyint(1)",
                nullable: false,
                defaultValue: false);

            migrationBuilder.CreateIndex(
                name: "IX_sh_warehouses_IsPublicSalesWarehouse_IsActive_IsDeleted",
                table: "sh_warehouses",
                columns: new[] { "IsPublicSalesWarehouse", "IsActive", "IsDeleted" },
                unique: true,
                filter: "IsPublicSalesWarehouse = 1 AND IsActive = 1 AND IsDeleted = 0");
        }
    }
}
