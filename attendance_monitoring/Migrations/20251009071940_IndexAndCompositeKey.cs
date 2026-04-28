using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace attendance_monitoring.Migrations
{
    /// <inheritdoc />
    public partial class IndexAndCompositeKey : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateIndex(
                name: "IX_Schedules_ClassroomId",
                table: "Schedules",
                column: "ClassroomId");

            migrationBuilder.CreateIndex(
                name: "IX_Schedules_DayOfWeek",
                table: "Schedules",
                column: "DayOfWeek");

            migrationBuilder.CreateIndex(
                name: "IX_Schedules_TimeIn",
                table: "Schedules",
                column: "TimeIn");

            migrationBuilder.CreateIndex(
                name: "IX_Schedules_TimeIn_TimeOut",
                table: "Schedules",
                columns: new[] { "TimeIn", "TimeOut" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Schedules_TimeOut",
                table: "Schedules",
                column: "TimeOut");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Schedules_ClassroomId",
                table: "Schedules");

            migrationBuilder.DropIndex(
                name: "IX_Schedules_DayOfWeek",
                table: "Schedules");

            migrationBuilder.DropIndex(
                name: "IX_Schedules_TimeIn",
                table: "Schedules");

            migrationBuilder.DropIndex(
                name: "IX_Schedules_TimeIn_TimeOut",
                table: "Schedules");

            migrationBuilder.DropIndex(
                name: "IX_Schedules_TimeOut",
                table: "Schedules");
        }
    }
}
