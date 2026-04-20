using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace attendance_monitoring.Migrations
{
    /// <inheritdoc />
    public partial class MakeSessionRowVersionNonNullable : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Drop the existing nullable RowVersion column
            migrationBuilder.DropColumn(
                name: "RowVersion",
                table: "Sessions");

            // Add it back as non-nullable (SQL Server will auto-populate)
            migrationBuilder.AddColumn<byte[]>(
                name: "RowVersion",
                table: "Sessions",
                type: "rowversion",
                rowVersion: true,
                nullable: false);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // Drop the non-nullable column
            migrationBuilder.DropColumn(
                name: "RowVersion",
                table: "Sessions");

            // Add it back as nullable
            migrationBuilder.AddColumn<byte[]>(
                name: "RowVersion",
                table: "Sessions",
                type: "rowversion",
                rowVersion: true,
                nullable: true);
        }
    }
}
