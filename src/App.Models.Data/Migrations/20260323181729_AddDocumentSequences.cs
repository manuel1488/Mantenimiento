using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace App.Models.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddDocumentSequences : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "sh_document_sequences",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    DocumentType = table.Column<string>(type: "varchar(50)", maxLength: 50, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Year = table.Column<int>(type: "int", nullable: false),
                    CurrentValue = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_sh_document_sequences", x => x.Id);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateIndex(
                name: "IX_sh_document_sequences_DocumentType_Year",
                table: "sh_document_sequences",
                columns: new[] { "DocumentType", "Year" },
                unique: true);

            // Seed current max values from existing documents
            migrationBuilder.Sql(@"
                INSERT INTO sh_document_sequences (DocumentType, Year, CurrentValue)
                SELECT 'Quotation', YEAR(QuoteDate), MAX(CAST(SUBSTRING(QuotationNumber, LENGTH(CONCAT('COT-', YEAR(QuoteDate), '-')) + 1) AS UNSIGNED))
                FROM sh_quotations
                WHERE QuotationNumber IS NOT NULL
                GROUP BY YEAR(QuoteDate)
                HAVING MAX(CAST(SUBSTRING(QuotationNumber, LENGTH(CONCAT('COT-', YEAR(QuoteDate), '-')) + 1) AS UNSIGNED)) > 0
                ORDER BY YEAR(QuoteDate);
            ");

            migrationBuilder.Sql(@"
                INSERT INTO sh_document_sequences (DocumentType, Year, CurrentValue)
                SELECT 'Remission', YEAR(RemissionDate), MAX(CAST(SUBSTRING(RemissionNumber, LENGTH(CONCAT('REM-', YEAR(RemissionDate), '-')) + 1) AS UNSIGNED))
                FROM sh_remissions
                WHERE RemissionNumber IS NOT NULL
                GROUP BY YEAR(RemissionDate)
                HAVING MAX(CAST(SUBSTRING(RemissionNumber, LENGTH(CONCAT('REM-', YEAR(RemissionDate), '-')) + 1) AS UNSIGNED)) > 0
                ORDER BY YEAR(RemissionDate);
            ");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "sh_document_sequences");
        }
    }
}
