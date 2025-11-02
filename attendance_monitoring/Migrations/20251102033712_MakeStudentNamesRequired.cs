using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace attendance_monitoring.Migrations
{
    /// <inheritdoc />
    public partial class MakeStudentNamesRequired : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Step 1: Delete students with NULL or empty Firstname/Lastname
            // This is necessary before applying NOT NULL constraint
            migrationBuilder.Sql(@"
                DELETE FROM Students 
                WHERE Firstname IS NULL 
                   OR Firstname = '' 
                   OR Lastname IS NULL 
                   OR Lastname = '';
            ");

            // Step 2: Alter Firstname column to NOT NULL with maxLength 100
            migrationBuilder.AlterColumn<string>(
                name: "Firstname",
                table: "Students",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: false,
                defaultValue: "",
                oldClrType: typeof(string),
                oldType: "nvarchar(max)",
                oldNullable: true);

            // Step 3: Alter Lastname column to NOT NULL with maxLength 100
            migrationBuilder.AlterColumn<string>(
                name: "Lastname",
                table: "Students",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: false,
                defaultValue: "",
                oldClrType: typeof(string),
                oldType: "nvarchar(max)",
                oldNullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // Rollback: Make Firstname and Lastname nullable again
            migrationBuilder.AlterColumn<string>(
                name: "Lastname",
                table: "Students",
                type: "nvarchar(max)",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(100)",
                oldMaxLength: 100);

            migrationBuilder.AlterColumn<string>(
                name: "Firstname",
                table: "Students",
                type: "nvarchar(max)",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(100)",
                oldMaxLength: 100);
        }
    }
}
