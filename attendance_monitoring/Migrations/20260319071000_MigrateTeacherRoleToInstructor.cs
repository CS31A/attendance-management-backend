using attendance_monitoring.Data;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace attendance_monitoring.Migrations
{
    [DbContext(typeof(ApplicationDbContext))]
    [Migration("20260319071000_MigrateTeacherRoleToInstructor")]
    public partial class MigrateTeacherRoleToInstructor : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            var sql = @"
IF EXISTS (SELECT 1 FROM AspNetRoles WHERE NormalizedName = 'TEACHER')
BEGIN
    DECLARE @TeacherRoleId NVARCHAR(450);
    DECLARE @InstructorRoleId NVARCHAR(450);

    SELECT TOP 1 @TeacherRoleId = Id
    FROM AspNetRoles
    WHERE NormalizedName = 'TEACHER';

    SELECT TOP 1 @InstructorRoleId = Id
    FROM AspNetRoles
    WHERE NormalizedName = 'INSTRUCTOR';

    IF @InstructorRoleId IS NULL
    BEGIN
        SET @InstructorRoleId = CONVERT(NVARCHAR(450), NEWID());

        INSERT INTO AspNetRoles (Id, Name, NormalizedName, ConcurrencyStamp)
        VALUES (@InstructorRoleId, 'Instructor', 'INSTRUCTOR', CONVERT(NVARCHAR(36), NEWID()));
    END

    INSERT INTO AspNetRoleClaims (RoleId, ClaimType, ClaimValue)
    SELECT @InstructorRoleId, teacherClaims.ClaimType, teacherClaims.ClaimValue
    FROM (
        SELECT DISTINCT ClaimType, ClaimValue
        FROM AspNetRoleClaims
        WHERE RoleId = @TeacherRoleId
    ) AS teacherClaims
    WHERE NOT EXISTS (
          SELECT 1
          FROM AspNetRoleClaims AS instructorClaims
          WHERE instructorClaims.RoleId = @InstructorRoleId
            AND ((instructorClaims.ClaimType = teacherClaims.ClaimType)
                 OR (instructorClaims.ClaimType IS NULL AND teacherClaims.ClaimType IS NULL))
            AND ((instructorClaims.ClaimValue = teacherClaims.ClaimValue)
                 OR (instructorClaims.ClaimValue IS NULL AND teacherClaims.ClaimValue IS NULL))
      );

    INSERT INTO AspNetUserRoles (UserId, RoleId)
    SELECT teacherRoles.UserId, @InstructorRoleId
    FROM AspNetUserRoles AS teacherRoles
    WHERE teacherRoles.RoleId = @TeacherRoleId
      AND NOT EXISTS (
          SELECT 1
          FROM AspNetUserRoles AS instructorRoles
          WHERE instructorRoles.UserId = teacherRoles.UserId
            AND instructorRoles.RoleId = @InstructorRoleId
      );

    DELETE FROM AspNetRoleClaims
    WHERE RoleId = @TeacherRoleId;

    DELETE FROM AspNetUserRoles
    WHERE RoleId = @TeacherRoleId;

    DELETE FROM AspNetRoles
    WHERE Id = @TeacherRoleId;
END";

            migrationBuilder.Sql(sql);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
        }
    }
}
