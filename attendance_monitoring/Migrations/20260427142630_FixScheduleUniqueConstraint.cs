using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace attendance_monitoring.Migrations
{
    /// <inheritdoc />
    public partial class FixScheduleUniqueConstraint : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Schedules_TimeIn_TimeOut",
                table: "Schedules");

            migrationBuilder.CreateIndex(
                name: "IX_Schedules_ClassroomId_DayOfWeek_TimeIn_TimeOut",
                table: "Schedules",
                columns: new[] { "ClassroomId", "DayOfWeek", "TimeIn", "TimeOut" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Schedules_ClassroomId_DayOfWeek_TimeIn_TimeOut",
                table: "Schedules");

            migrationBuilder.CreateIndex(
                name: "IX_Schedules_TimeIn_TimeOut",
                table: "Schedules",
                columns: new[] { "TimeIn", "TimeOut" },
                unique: true);
        }
    }
}
