using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace App.Models.Data.Migrations
{
    /// <inheritdoc />
    public partial class FixQuotationAuditUserNames : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Replace stored user GUIDs with FullName in sh_quotations (CreatedBy / ModifiedBy / DeletedBy)
            migrationBuilder.Sql(@"
                UPDATE sh_quotations q
                JOIN AspNetUsers u ON u.Id = q.CreatedBy
                SET q.CreatedBy = u.FullName
                WHERE q.CreatedBy REGEXP '^[0-9a-fA-F]{8}-[0-9a-fA-F]{4}-[0-9a-fA-F]{4}-[0-9a-fA-F]{4}-[0-9a-fA-F]{12}$';
            ");

            migrationBuilder.Sql(@"
                UPDATE sh_quotations q
                JOIN AspNetUsers u ON u.Id = q.ModifiedBy
                SET q.ModifiedBy = u.FullName
                WHERE q.ModifiedBy REGEXP '^[0-9a-fA-F]{8}-[0-9a-fA-F]{4}-[0-9a-fA-F]{4}-[0-9a-fA-F]{4}-[0-9a-fA-F]{12}$';
            ");

            migrationBuilder.Sql(@"
                UPDATE sh_quotations q
                JOIN AspNetUsers u ON u.Id = q.DeletedBy
                SET q.DeletedBy = u.FullName
                WHERE q.DeletedBy REGEXP '^[0-9a-fA-F]{8}-[0-9a-fA-F]{4}-[0-9a-fA-F]{4}-[0-9a-fA-F]{4}-[0-9a-fA-F]{12}$';
            ");

            // Same for sh_quotation_details
            migrationBuilder.Sql(@"
                UPDATE sh_quotation_details d
                JOIN AspNetUsers u ON u.Id = d.CreatedBy
                SET d.CreatedBy = u.FullName
                WHERE d.CreatedBy REGEXP '^[0-9a-fA-F]{8}-[0-9a-fA-F]{4}-[0-9a-fA-F]{4}-[0-9a-fA-F]{4}-[0-9a-fA-F]{12}$';
            ");

            migrationBuilder.Sql(@"
                UPDATE sh_quotation_details d
                JOIN AspNetUsers u ON u.Id = d.ModifiedBy
                SET d.ModifiedBy = u.FullName
                WHERE d.ModifiedBy REGEXP '^[0-9a-fA-F]{8}-[0-9a-fA-F]{4}-[0-9a-fA-F]{4}-[0-9a-fA-F]{4}-[0-9a-fA-F]{12}$';
            ");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // Cannot reverse: original GUIDs are not stored after the update.
        }
    }
}
