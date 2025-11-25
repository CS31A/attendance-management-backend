using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace attendance_monitoring.Migrations
{
    /// <inheritdoc />
    public partial class AddGetAllUsersSP : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            var sp = @"CREATE PROCEDURE sp_GetAllUsers
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
                            LEFT JOIN Students s ON u.Id = s.UserId AND s.IsDeleted = 0
                            LEFT JOIN Instructors i ON u.Id = i.UserId AND i.IsDeleted = 0
                            LEFT JOIN Admins a ON u.Id = a.UserId;
                        END";

            migrationBuilder.Sql(sp);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("DROP PROCEDURE IF EXISTS sp_GetAllUsers");
        }
    }
}
