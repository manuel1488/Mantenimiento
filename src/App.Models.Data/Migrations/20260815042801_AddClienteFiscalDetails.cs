using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace App.Models.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddClienteFiscalDetails : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "CalleFiscal",
                table: "cli_clientes",
                type: "varchar(150)",
                maxLength: 150,
                nullable: true)
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AddColumn<string>(
                name: "CiudadFiscal",
                table: "cli_clientes",
                type: "varchar(100)",
                maxLength: 100,
                nullable: true)
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AddColumn<string>(
                name: "ColoniaFiscal",
                table: "cli_clientes",
                type: "varchar(100)",
                maxLength: 100,
                nullable: true)
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AddColumn<string>(
                name: "CorreoFiscal",
                table: "cli_clientes",
                type: "varchar(150)",
                maxLength: 150,
                nullable: true)
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AddColumn<string>(
                name: "EstadoFiscal",
                table: "cli_clientes",
                type: "varchar(100)",
                maxLength: 100,
                nullable: true)
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AddColumn<string>(
                name: "NumeroExteriorFiscal",
                table: "cli_clientes",
                type: "varchar(20)",
                maxLength: 20,
                nullable: true)
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AddColumn<string>(
                name: "NumeroInteriorFiscal",
                table: "cli_clientes",
                type: "varchar(20)",
                maxLength: 20,
                nullable: true)
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AddColumn<bool>(
                name: "TieneDatosFiscales",
                table: "cli_clientes",
                type: "tinyint(1)",
                nullable: false,
                defaultValue: false);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "CalleFiscal",
                table: "cli_clientes");

            migrationBuilder.DropColumn(
                name: "CiudadFiscal",
                table: "cli_clientes");

            migrationBuilder.DropColumn(
                name: "ColoniaFiscal",
                table: "cli_clientes");

            migrationBuilder.DropColumn(
                name: "CorreoFiscal",
                table: "cli_clientes");

            migrationBuilder.DropColumn(
                name: "EstadoFiscal",
                table: "cli_clientes");

            migrationBuilder.DropColumn(
                name: "NumeroExteriorFiscal",
                table: "cli_clientes");

            migrationBuilder.DropColumn(
                name: "NumeroInteriorFiscal",
                table: "cli_clientes");

            migrationBuilder.DropColumn(
                name: "TieneDatosFiscales",
                table: "cli_clientes");
        }
    }
}
