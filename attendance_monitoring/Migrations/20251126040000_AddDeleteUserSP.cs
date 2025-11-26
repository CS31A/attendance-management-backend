using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace attendance_monitoring.Migrations
{
    /// <inheritdoc />
    public partial class AddDeleteUserSP : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            var sp = @"CREATE PROCEDURE sp_DeleteUser
                @UserId NVARCHAR(450)
            AS
            BEGIN
                SET NOCOUNT ON;
                
                DECLARE @RoleName NVARCHAR(256);
                DECLARE @Success BIT = 0;
                DECLARE @Message NVARCHAR(500) = 'User not found or deletion failed';
                
                -- Get user's role
                SELECT TOP 1 @RoleName = r.Name
                FROM AspNetUsers u
                LEFT JOIN AspNetUserRoles ur ON u.Id = ur.UserId
                LEFT JOIN AspNetRoles r ON ur.RoleId = r.Id
                WHERE u.Id = @UserId;
                
                -- Check if user exists
                IF @RoleName IS NULL
                BEGIN
                    SET @Message = 'User not found';
                    SELECT @Success AS Success, @Message AS Message;
                    RETURN;
                END
                
                -- Soft delete based on role
                IF @RoleName = 'Student'
                BEGIN
                    UPDATE Students
                    SET IsDeleted = 1, DeletedAt = GETUTCDATE()
                    WHERE UserId = @UserId AND IsDeleted = 0;
                    
                    IF @@ROWCOUNT > 0
                    BEGIN
                        SET @Success = 1;
                        SET @Message = 'Student profile deleted successfully';
                    END
                    ELSE
                    BEGIN
                        SET @Message = 'Student profile not found or already deleted';
                    END
                END
                ELSE IF @RoleName = 'Teacher' OR @RoleName = 'Instructor'
                BEGIN
                    UPDATE Instructors
                    SET IsDeleted = 1, DeletedAt = GETUTCDATE()
                    WHERE UserId = @UserId AND IsDeleted = 0;
                    
                    IF @@ROWCOUNT > 0
                    BEGIN
                        SET @Success = 1;
                        SET @Message = 'Instructor profile deleted successfully';
                    END
                    ELSE
                    BEGIN
                        SET @Message = 'Instructor profile not found or already deleted';
                    END
                END
                ELSE IF @RoleName = 'Admin'
                BEGIN
                    UPDATE Admins
                    SET IsDeleted = 1, DeletedAt = GETUTCDATE()
                    WHERE UserId = @UserId;
                    
                    IF @@ROWCOUNT > 0
                    BEGIN
                        SET @Success = 1;
                        SET @Message = 'Admin profile deleted successfully';
                    END
                    ELSE
                    BEGIN
                        SET @Message = 'Admin profile not found or already deleted';
                    END
                END
                
                SELECT @Success AS Success, @Message AS Message;
            END";

            migrationBuilder.Sql(sp);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("DROP PROCEDURE IF EXISTS sp_DeleteUser");
        }
    }
}
