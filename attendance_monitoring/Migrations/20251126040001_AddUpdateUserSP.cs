using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace attendance_monitoring.Migrations
{
    /// <inheritdoc />
    public partial class AddUpdateUserSP : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            var sp = @"CREATE PROCEDURE sp_UpdateUser
                @UserId NVARCHAR(450),
                @Email NVARCHAR(256) = NULL,
                @Firstname NVARCHAR(100) = NULL,
                @Lastname NVARCHAR(100) = NULL,
                @SectionId INT = NULL,
                @IsRegular BIT = NULL
            AS
            BEGIN
                SET NOCOUNT ON;
                
                DECLARE @RoleName NVARCHAR(256);
                DECLARE @Success BIT = 0;
                DECLARE @Message NVARCHAR(500) = 'Update failed';
                
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
                    SELECT @Success AS Success, NULL AS UserId, NULL AS Username, NULL AS Email, 
                           NULL AS Role, NULL AS ProfileId, NULL AS Firstname, NULL AS Lastname, 
                           NULL AS CreatedAt, NULL AS UpdatedAt, @Message AS Message;
                    RETURN;
                END
                
                -- Update email in AspNetUsers if provided
                IF @Email IS NOT NULL
                BEGIN
                    -- Check for duplicate email (excluding current user)
                    IF EXISTS (SELECT 1 FROM AspNetUsers WHERE Email = @Email AND Id != @UserId)
                    BEGIN
                        SET @Message = 'Email address already in use';
                        SELECT @Success AS Success, NULL AS UserId, NULL AS Username, NULL AS Email, 
                               NULL AS Role, NULL AS ProfileId, NULL AS Firstname, NULL AS Lastname, 
                               NULL AS CreatedAt, NULL AS UpdatedAt, @Message AS Message;
                        RETURN;
                    END
                    
                    UPDATE AspNetUsers
                    SET Email = @Email, NormalizedEmail = UPPER(@Email)
                    WHERE Id = @UserId;
                END
                
                -- Update role-specific profile
                IF @RoleName = 'Student'
                BEGIN
                    -- Validate section exists if provided
                    IF @SectionId IS NOT NULL AND NOT EXISTS (SELECT 1 FROM Sections WHERE Id = @SectionId)
                    BEGIN
                        SET @Message = 'Invalid section ID';
                        SELECT @Success AS Success, NULL AS UserId, NULL AS Username, NULL AS Email, 
                               NULL AS Role, NULL AS ProfileId, NULL AS Firstname, NULL AS Lastname, 
                               NULL AS CreatedAt, NULL AS UpdatedAt, @Message AS Message;
                        RETURN;
                    END
                    
                    UPDATE Students
                    SET 
                        Firstname = COALESCE(@Firstname, Firstname),
                        Lastname = COALESCE(@Lastname, Lastname),
                        SectionId = COALESCE(@SectionId, SectionId),
                        IsRegular = COALESCE(@IsRegular, IsRegular),
                        UpdatedAt = GETUTCDATE()
                    WHERE UserId = @UserId AND IsDeleted = 0;
                    
                    IF @@ROWCOUNT > 0
                    BEGIN
                        SET @Success = 1;
                        SET @Message = 'Student profile updated successfully';
                    END
                END
                ELSE IF @RoleName = 'Teacher' OR @RoleName = 'Instructor'
                BEGIN
                    UPDATE Instructors
                    SET 
                        Firstname = COALESCE(@Firstname, Firstname),
                        Lastname = COALESCE(@Lastname, Lastname),
                        UpdatedAt = GETUTCDATE()
                    WHERE UserId = @UserId AND IsDeleted = 0;
                    
                    IF @@ROWCOUNT > 0
                    BEGIN
                        SET @Success = 1;
                        SET @Message = 'Instructor profile updated successfully';
                    END
                END
                ELSE IF @RoleName = 'Admin'
                BEGIN
                    UPDATE Admins
                    SET 
                        Firstname = COALESCE(@Firstname, Firstname),
                        Lastname = COALESCE(@Lastname, Lastname),
                        UpdatedAt = GETUTCDATE()
                    WHERE UserId = @UserId;
                    
                    IF @@ROWCOUNT > 0
                    BEGIN
                        SET @Success = 1;
                        SET @Message = 'Admin profile updated successfully';
                    END
                END
                
                -- Return updated user information
                SELECT
                    @Success AS Success,
                    u.Id AS UserId,
                    u.UserName AS Username,
                    u.Email,
                    ISNULL(r.Name, 'Unknown') AS Role,
                    COALESCE(s.Id, i.Id, a.Id) AS ProfileId,
                    COALESCE(s.Firstname, i.Firstname, a.Firstname) AS Firstname,
                    COALESCE(s.Lastname, i.Lastname, a.Lastname) AS Lastname,
                    COALESCE(s.CreatedAt, i.CreatedAt, a.CreatedAt, GETUTCDATE()) AS CreatedAt,
                    COALESCE(s.UpdatedAt, i.UpdatedAt, a.UpdatedAt, GETUTCDATE()) AS UpdatedAt,
                    @Message AS Message
                FROM AspNetUsers u
                LEFT JOIN AspNetUserRoles ur ON u.Id = ur.UserId
                LEFT JOIN AspNetRoles r ON ur.RoleId = r.Id
                LEFT JOIN Students s ON u.Id = s.UserId AND s.IsDeleted = 0
                LEFT JOIN Instructors i ON u.Id = i.UserId AND i.IsDeleted = 0
                LEFT JOIN Admins a ON u.Id = a.UserId
                WHERE u.Id = @UserId;
            END";

            migrationBuilder.Sql(sp);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("DROP PROCEDURE IF EXISTS sp_UpdateUser");
        }
    }
}
