using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace attendance_monitoring.Migrations
{
    /// <inheritdoc />
    public partial class ForceFixScheduleIndex : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Manually drop the old index if it exists
            migrationBuilder.Sql(
                @"IF EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_Schedules_TimeIn_TimeOut' AND object_id = OBJECT_ID('dbo.Schedules'))
                  BEGIN
                      DROP INDEX [IX_Schedules_TimeIn_TimeOut] ON [dbo].[Schedules];
                  END");

            // Manually create the new composite unique index if it doesn't exist
            migrationBuilder.Sql(
                @"IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_Schedules_ClassroomId_DayOfWeek_TimeIn_TimeOut' AND object_id = OBJECT_ID('dbo.Schedules'))
                  BEGIN
                      CREATE UNIQUE INDEX [IX_Schedules_ClassroomId_DayOfWeek_TimeIn_TimeOut] 
                      ON [dbo].[Schedules] ([ClassroomId], [DayOfWeek], [TimeIn], [TimeOut]);
                  END");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // Reverse the changes
            migrationBuilder.Sql(
                @"IF EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_Schedules_ClassroomId_DayOfWeek_TimeIn_TimeOut' AND object_id = OBJECT_ID('dbo.Schedules'))
                  BEGIN
                      DROP INDEX [IX_Schedules_ClassroomId_DayOfWeek_TimeIn_TimeOut] ON [dbo].[Schedules];
                  END");

            migrationBuilder.Sql(
                @"IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_Schedules_TimeIn_TimeOut' AND object_id = OBJECT_ID('dbo.Schedules'))
                  BEGIN
                      CREATE UNIQUE INDEX [IX_Schedules_TimeIn_TimeOut] 
                      ON [dbo].[Schedules] ([TimeIn], [TimeOut]);
                  END");
        }
    }
}
