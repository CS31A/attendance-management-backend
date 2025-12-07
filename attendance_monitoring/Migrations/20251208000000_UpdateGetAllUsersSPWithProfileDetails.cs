using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace attendance_monitoring.Migrations
{
    /// <inheritdoc />
    public partial class UpdateGetAllUsersSPWithProfileDetails : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Drop existing stored procedure
            migrationBuilder.Sql("DROP PROCEDURE IF EXISTS sp_GetAllUsers");

            // Create updated stored procedure with full profile details
            migrationBuilder.Sql(@"
                CREATE PROCEDURE sp_GetAllUsers
                    @Status INT = 0  -- 0 = Active, 1 = Archived, 2 = All
                AS
                BEGIN
                    SET NOCOUNT ON;

                    SELECT 
                        u.Id AS UserId,
                        u.UserName AS Username,
                        u.Email,
                        ISNULL(r.Name, 'Unknown') AS Role,
                        -- Profile ID (based on role)
                        COALESCE(s.Id, i.Id, a.Id) AS ProfileId,
                        -- Common profile fields
                        COALESCE(s.Firstname, i.Firstname, a.Firstname) AS Firstname,
                        COALESCE(s.Lastname, i.Lastname, a.Lastname) AS Lastname,
                        COALESCE(s.CreatedAt, i.CreatedAt, a.CreatedAt, GETUTCDATE()) AS CreatedAt,
                        COALESCE(s.UpdatedAt, i.UpdatedAt, a.UpdatedAt, GETUTCDATE()) AS UpdatedAt,
                        -- IsDeleted and DeletedAt for all profile types
                        COALESCE(s.IsDeleted, i.IsDeleted, a.IsDeleted, 0) AS IsDeleted,
                        COALESCE(s.DeletedAt, i.DeletedAt, a.DeletedAt) AS DeletedAt,
                        -- Student-specific fields
                        s.SectionId AS SectionId,
                        sec.Name AS SectionName,
                        sec.CourseId AS CourseId,
                        c.Name AS CourseName,
                        s.IsRegular AS IsRegular
                    FROM AspNetUsers u
                    LEFT JOIN AspNetUserRoles ur ON u.Id = ur.UserId
                    LEFT JOIN AspNetRoles r ON ur.RoleId = r.Id
                    LEFT JOIN Students s ON u.Id = s.UserId 
                        AND (
                            @Status = 2 OR  -- All
                            (@Status = 0 AND s.IsDeleted = 0) OR  -- Active only
                            (@Status = 1 AND s.IsDeleted = 1)     -- Archived only
                        )
                    LEFT JOIN Sections sec ON s.SectionId = sec.Id
                    LEFT JOIN Courses c ON sec.CourseId = c.Id
                    LEFT JOIN Instructors i ON u.Id = i.UserId 
                        AND (
                            @Status = 2 OR  -- All
                            (@Status = 0 AND i.IsDeleted = 0) OR  -- Active only
                            (@Status = 1 AND i.IsDeleted = 1)     -- Archived only
                        )
                    LEFT JOIN Admins a ON u.Id = a.UserId 
                        AND (
                            @Status = 2 OR  -- All
                            (@Status = 0 AND (a.IsDeleted = 0 OR a.IsDeleted IS NULL)) OR  -- Active only
                            (@Status = 1 AND a.IsDeleted = 1)     -- Archived only
                        )
                    WHERE 
                        -- Only include users that have a profile matching the status filter
                        @Status = 2 OR  -- All users
                        (s.Id IS NOT NULL OR i.Id IS NOT NULL OR a.Id IS NOT NULL)
                    ORDER BY u.UserName;
                END
            ");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // Drop the updated stored procedure
            migrationBuilder.Sql("DROP PROCEDURE IF EXISTS sp_GetAllUsers");

            // Recreate previous version of stored procedure
            migrationBuilder.Sql(@"
                CREATE PROCEDURE sp_GetAllUsers
                    @Status INT = 0  -- 0 = Active, 1 = Archived, 2 = All
                AS
                BEGIN
                    SET NOCOUNT ON;

                    SELECT 
                        u.Id AS UserId,
                        u.UserName AS Username,
                        u.Email,
                        ISNULL(r.Name, 'Unknown') AS Role,
                        COALESCE(s.Id, i.Id, a.Id) AS ProfileId,
                        COALESCE(s.Firstname, i.Firstname, a.Firstname) AS Firstname,
                        COALESCE(s.Lastname, i.Lastname, a.Lastname) AS Lastname,
                        COALESCE(s.CreatedAt, i.CreatedAt, a.CreatedAt, GETUTCDATE()) AS CreatedAt,
                        COALESCE(s.UpdatedAt, i.UpdatedAt, a.UpdatedAt, GETUTCDATE()) AS UpdatedAt
                    FROM AspNetUsers u
                    LEFT JOIN AspNetUserRoles ur ON u.Id = ur.UserId
                    LEFT JOIN AspNetRoles r ON ur.RoleId = r.Id
                    LEFT JOIN Students s ON u.Id = s.UserId 
                        AND (
                            @Status = 2 OR  -- All
                            (@Status = 0 AND s.IsDeleted = 0) OR  -- Active only
                            (@Status = 1 AND s.IsDeleted = 1)     -- Archived only
                        )
                    LEFT JOIN Instructors i ON u.Id = i.UserId 
                        AND (
                            @Status = 2 OR  -- All
                            (@Status = 0 AND i.IsDeleted = 0) OR  -- Active only
                            (@Status = 1 AND i.IsDeleted = 1)     -- Archived only
                        )
                    LEFT JOIN Admins a ON u.Id = a.UserId 
                        AND (
                            @Status = 2 OR  -- All
                            (@Status = 0 AND (a.IsDeleted = 0 OR a.IsDeleted IS NULL)) OR  -- Active only
                            (@Status = 1 AND a.IsDeleted = 1)     -- Archived only
                        )
                    WHERE 
                        -- Only include users that have a profile matching the status filter
                        @Status = 2 OR  -- All users
                        (s.Id IS NOT NULL OR i.Id IS NOT NULL OR a.Id IS NOT NULL)
                    ORDER BY u.UserName;
                END
            ");
        }
    }
}
