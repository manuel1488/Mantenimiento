using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace App.Models.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddIsLabelingAllowedToProduct : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "IsLabelingAllowed",
                table: "sh_products",
                type: "tinyint(1)",
                nullable: false,
                defaultValue: false);

            // Initialize IsLabelingAllowed with the same value as IsPartialSaleAllowed
            // so existing products that were used for labeling keep working
            migrationBuilder.Sql(
                "UPDATE sh_products SET IsLabelingAllowed = IsPartialSaleAllowed");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "IsLabelingAllowed",
                table: "sh_products");
        }
    }
}
