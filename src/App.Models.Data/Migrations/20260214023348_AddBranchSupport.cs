using System;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace App.Models.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddBranchSupport : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "BranchId",
                table: "sh_warehouses",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "BranchId",
                table: "sh_sales",
                type: "int",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "sh_branches",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    Name = table.Column<string>(type: "varchar(100)", maxLength: 100, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Description = table.Column<string>(type: "varchar(500)", maxLength: 500, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Street = table.Column<string>(type: "varchar(200)", maxLength: 200, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    City = table.Column<string>(type: "varchar(100)", maxLength: 100, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    State = table.Column<string>(type: "varchar(100)", maxLength: 100, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    ZipCode = table.Column<string>(type: "varchar(20)", maxLength: 20, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Country = table.Column<string>(type: "varchar(3)", maxLength: 3, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Phone = table.Column<string>(type: "varchar(20)", maxLength: 20, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Email = table.Column<string>(type: "varchar(100)", maxLength: 100, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    IsActive = table.Column<bool>(type: "tinyint(1)", nullable: false, defaultValue: true),
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
                    table.PrimaryKey("PK_sh_branches", x => x.Id);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "id_user_branches",
                columns: table => new
                {
                    UserId = table.Column<string>(type: "varchar(255)", nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    BranchId = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_id_user_branches", x => new { x.UserId, x.BranchId });
                    table.ForeignKey(
                        name: "FK_id_user_branches_AspNetUsers_UserId",
                        column: x => x.UserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_id_user_branches_sh_branches_BranchId",
                        column: x => x.BranchId,
                        principalTable: "sh_branches",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateIndex(
                name: "IX_sh_warehouses_BranchId",
                table: "sh_warehouses",
                column: "BranchId");

            migrationBuilder.CreateIndex(
                name: "IX_sh_sales_BranchId",
                table: "sh_sales",
                column: "BranchId");

            migrationBuilder.CreateIndex(
                name: "IX_id_user_branches_BranchId",
                table: "id_user_branches",
                column: "BranchId");

            migrationBuilder.CreateIndex(
                name: "IX_sh_branches_Name",
                table: "sh_branches",
                column: "Name");

            migrationBuilder.CreateIndex(
                name: "IX_sh_branches_Name_IsDeleted",
                table: "sh_branches",
                columns: new[] { "Name", "IsDeleted" },
                unique: true,
                filter: "IsDeleted = 0");

            migrationBuilder.AddForeignKey(
                name: "FK_sh_sales_sh_branches_BranchId",
                table: "sh_sales",
                column: "BranchId",
                principalTable: "sh_branches",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_sh_warehouses_sh_branches_BranchId",
                table: "sh_warehouses",
                column: "BranchId",
                principalTable: "sh_branches",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_sh_sales_sh_branches_BranchId",
                table: "sh_sales");

            migrationBuilder.DropForeignKey(
                name: "FK_sh_warehouses_sh_branches_BranchId",
                table: "sh_warehouses");

            migrationBuilder.DropTable(
                name: "id_user_branches");

            migrationBuilder.DropTable(
                name: "sh_branches");

            migrationBuilder.DropIndex(
                name: "IX_sh_warehouses_BranchId",
                table: "sh_warehouses");

            migrationBuilder.DropIndex(
                name: "IX_sh_sales_BranchId",
                table: "sh_sales");

            migrationBuilder.DropColumn(
                name: "BranchId",
                table: "sh_warehouses");

            migrationBuilder.DropColumn(
                name: "BranchId",
                table: "sh_sales");
        }
    }
}
