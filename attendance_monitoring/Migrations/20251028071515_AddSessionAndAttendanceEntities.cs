using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace attendance_monitoring.Migrations
{
    /// <inheritdoc />
    public partial class AddSessionAndAttendanceEntities : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // IMPORTANT: Clear existing QR codes since we're changing the schema significantly
            // QR codes are ephemeral and will be regenerated
            migrationBuilder.Sql("DELETE FROM [QrCodes]");

            migrationBuilder.DropForeignKey(
                name: "FK_QrCodes_Classrooms_ActualRoomId",
                table: "QrCodes");

            migrationBuilder.DropForeignKey(
                name: "FK_QrCodes_Schedules_ScheduleId",
                table: "QrCodes");

            migrationBuilder.DropForeignKey(
                name: "FK_QrCodes_Sections_SectionId",
                table: "QrCodes");

            migrationBuilder.DropIndex(
                name: "IX_QrCodes_ActualRoomId",
                table: "QrCodes");

            migrationBuilder.DropIndex(
                name: "IX_QrCodes_ScheduleId",
                table: "QrCodes");

            migrationBuilder.DropColumn(
                name: "ActualRoomId",
                table: "QrCodes");

            migrationBuilder.DropColumn(
                name: "ScheduleId",
                table: "QrCodes");

            migrationBuilder.RenameColumn(
                name: "SectionId",
                table: "QrCodes",
                newName: "SessionId");

            migrationBuilder.RenameIndex(
                name: "IX_QrCodes_SectionId",
                table: "QrCodes",
                newName: "IX_QrCodes_SessionId");

            migrationBuilder.CreateTable(
                name: "Sessions",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ScheduleId = table.Column<int>(type: "int", nullable: false),
                    Status = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    SessionDate = table.Column<DateTime>(type: "date", nullable: false),
                    ActualStartTime = table.Column<DateTime>(type: "datetime2", nullable: true),
                    ActualEndTime = table.Column<DateTime>(type: "datetime2", nullable: true),
                    AttendanceCutOff = table.Column<DateTime>(type: "datetime2", nullable: true),
                    Description = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    ActualRoomId = table.Column<int>(type: "int", nullable: true),
                    StartedBy = table.Column<int>(type: "int", nullable: true),
                    EndedBy = table.Column<int>(type: "int", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Sessions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Sessions_Classrooms_ActualRoomId",
                        column: x => x.ActualRoomId,
                        principalTable: "Classrooms",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Sessions_Instructors_EndedBy",
                        column: x => x.EndedBy,
                        principalTable: "Instructors",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Sessions_Instructors_StartedBy",
                        column: x => x.StartedBy,
                        principalTable: "Instructors",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Sessions_Schedules_ScheduleId",
                        column: x => x.ScheduleId,
                        principalTable: "Schedules",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "AttendanceRecords",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    StudentId = table.Column<int>(type: "int", nullable: false),
                    SessionId = table.Column<int>(type: "int", nullable: false),
                    QrCodeId = table.Column<int>(type: "int", nullable: true),
                    CheckInTime = table.Column<DateTime>(type: "datetime2", nullable: false),
                    Status = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    Notes = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    IsManualEntry = table.Column<bool>(type: "bit", nullable: false),
                    EnteredBy = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AttendanceRecords", x => x.Id);
                    table.ForeignKey(
                        name: "FK_AttendanceRecords_QrCodes_QrCodeId",
                        column: x => x.QrCodeId,
                        principalTable: "QrCodes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_AttendanceRecords_Sessions_SessionId",
                        column: x => x.SessionId,
                        principalTable: "Sessions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_AttendanceRecords_Students_StudentId",
                        column: x => x.StudentId,
                        principalTable: "Students",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            // Note: These indexes already exist from previous migrations, so we skip creating them
            // migrationBuilder.CreateIndex(
            //     name: "IX_QrCodes_ExpiresAt",
            //     table: "QrCodes",
            //     column: "ExpiresAt");

            // migrationBuilder.CreateIndex(
            //     name: "IX_QrCodes_IsActive",
            //     table: "QrCodes",
            //     column: "IsActive");

            // migrationBuilder.CreateIndex(
            //     name: "IX_QrCodes_QrHash",
            //     table: "QrCodes",
            //     column: "QrHash",
            //     unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_AttendanceRecords_CheckInTime",
                table: "AttendanceRecords",
                column: "CheckInTime");

            migrationBuilder.CreateIndex(
                name: "IX_AttendanceRecords_QrCodeId",
                table: "AttendanceRecords",
                column: "QrCodeId");

            migrationBuilder.CreateIndex(
                name: "IX_AttendanceRecords_SessionId",
                table: "AttendanceRecords",
                column: "SessionId");

            migrationBuilder.CreateIndex(
                name: "IX_AttendanceRecords_Status",
                table: "AttendanceRecords",
                column: "Status");

            migrationBuilder.CreateIndex(
                name: "IX_AttendanceRecords_StudentId",
                table: "AttendanceRecords",
                column: "StudentId");

            migrationBuilder.CreateIndex(
                name: "IX_AttendanceRecords_StudentId_SessionId",
                table: "AttendanceRecords",
                columns: new[] { "StudentId", "SessionId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Sessions_ActualRoomId",
                table: "Sessions",
                column: "ActualRoomId");

            migrationBuilder.CreateIndex(
                name: "IX_Sessions_EndedBy",
                table: "Sessions",
                column: "EndedBy");

            migrationBuilder.CreateIndex(
                name: "IX_Sessions_ScheduleId",
                table: "Sessions",
                column: "ScheduleId");

            migrationBuilder.CreateIndex(
                name: "IX_Sessions_ScheduleId_SessionDate",
                table: "Sessions",
                columns: new[] { "ScheduleId", "SessionDate" });

            migrationBuilder.CreateIndex(
                name: "IX_Sessions_SessionDate",
                table: "Sessions",
                column: "SessionDate");

            migrationBuilder.CreateIndex(
                name: "IX_Sessions_StartedBy",
                table: "Sessions",
                column: "StartedBy");

            migrationBuilder.CreateIndex(
                name: "IX_Sessions_Status",
                table: "Sessions",
                column: "Status");

            migrationBuilder.AddForeignKey(
                name: "FK_QrCodes_Sessions_SessionId",
                table: "QrCodes",
                column: "SessionId",
                principalTable: "Sessions",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_QrCodes_Sessions_SessionId",
                table: "QrCodes");

            migrationBuilder.DropTable(
                name: "AttendanceRecords");

            migrationBuilder.DropTable(
                name: "Sessions");

            // Note: These indexes were not created by this migration, so we don't drop them
            // migrationBuilder.DropIndex(
            //     name: "IX_QrCodes_ExpiresAt",
            //     table: "QrCodes");

            // migrationBuilder.DropIndex(
            //     name: "IX_QrCodes_IsActive",
            //     table: "QrCodes");

            // migrationBuilder.DropIndex(
            //     name: "IX_QrCodes_QrHash",
            //     table: "QrCodes");

            migrationBuilder.RenameColumn(
                name: "SessionId",
                table: "QrCodes",
                newName: "SectionId");

            migrationBuilder.RenameIndex(
                name: "IX_QrCodes_SessionId",
                table: "QrCodes",
                newName: "IX_QrCodes_SectionId");

            migrationBuilder.AddColumn<int>(
                name: "ActualRoomId",
                table: "QrCodes",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "ScheduleId",
                table: "QrCodes",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.CreateIndex(
                name: "IX_QrCodes_ActualRoomId",
                table: "QrCodes",
                column: "ActualRoomId");

            migrationBuilder.CreateIndex(
                name: "IX_QrCodes_ScheduleId",
                table: "QrCodes",
                column: "ScheduleId");

            migrationBuilder.AddForeignKey(
                name: "FK_QrCodes_Classrooms_ActualRoomId",
                table: "QrCodes",
                column: "ActualRoomId",
                principalTable: "Classrooms",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_QrCodes_Schedules_ScheduleId",
                table: "QrCodes",
                column: "ScheduleId",
                principalTable: "Schedules",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_QrCodes_Sections_SectionId",
                table: "QrCodes",
                column: "SectionId",
                principalTable: "Sections",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
