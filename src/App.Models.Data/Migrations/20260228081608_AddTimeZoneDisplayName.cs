using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace App.Models.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddTimeZoneDisplayName : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<string>(
                name: "TimeZoneId",
                table: "stg_settings",
                type: "varchar(100)",
                unicode: false,
                maxLength: 100,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "varchar(50)",
                oldUnicode: false,
                oldMaxLength: 50)
                .Annotation("MySql:CharSet", "utf8mb4")
                .OldAnnotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AddColumn<string>(
                name: "TimeZoneDisplayName",
                table: "stg_settings",
                type: "varchar(200)",
                maxLength: 200,
                nullable: true)
                .Annotation("MySql:CharSet", "utf8mb4");

            // Normalise to the IANA ID used across the stack (Linux/Docker compatible).
            // Also populate the new display-name column.
            migrationBuilder.Sql(
                "UPDATE stg_settings " +
                "SET TimeZoneId = 'America/Mexico_City', " +
                "    TimeZoneDisplayName = '(UTC-06:00) Guadalajara, Ciudad de Mexico, Monterrey' " +
                "WHERE TimeZoneId NOT IN ('America/Mexico_City','America/Cancun','America/Tijuana'," +
                "                         'America/Hermosillo','America/Chihuahua','America/Monterrey'," +
                "                         'America/Merida','America/Mazatlan');");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "TimeZoneDisplayName",
                table: "stg_settings");

            migrationBuilder.AlterColumn<string>(
                name: "TimeZoneId",
                table: "stg_settings",
                type: "varchar(50)",
                unicode: false,
                maxLength: 50,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "varchar(100)",
                oldUnicode: false,
                oldMaxLength: 100)
                .Annotation("MySql:CharSet", "utf8mb4")
                .OldAnnotation("MySql:CharSet", "utf8mb4");
        }
    }
}
