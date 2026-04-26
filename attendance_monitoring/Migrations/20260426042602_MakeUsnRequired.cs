using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace attendance_monitoring.Migrations
{
    /// <inheritdoc />
    public partial class MakeUsnRequired : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Students_Usn",
                table: "Students");

            migrationBuilder.AlterColumn<string>(
                name: "Usn",
                table: "Students",
                type: "nvarchar(50)",
                maxLength: 50,
                nullable: false,
                defaultValue: "",
                oldClrType: typeof(string),
                oldType: "nvarchar(50)",
                oldMaxLength: 50,
                oldNullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Students_Usn",
                table: "Students",
                column: "Usn",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Students_Usn",
                table: "Students");

            migrationBuilder.AlterColumn<string>(
                name: "Usn",
                table: "Students",
                type: "nvarchar(50)",
                maxLength: 50,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(50)",
                oldMaxLength: 50);

            migrationBuilder.CreateIndex(
                name: "IX_Students_Usn",
                table: "Students",
                column: "Usn",
                unique: true,
                filter: "[Usn] IS NOT NULL");
        }
    }
}
