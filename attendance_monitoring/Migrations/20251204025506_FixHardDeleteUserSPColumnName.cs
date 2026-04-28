using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace attendance_monitoring.Migrations
{
    /// <inheritdoc />
    public partial class FixHardDeleteUserSPColumnName : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Drop and recreate the stored procedure with correct column name
            migrationBuilder.Sql("DROP PROCEDURE IF EXISTS sp_HardDeleteUser");

            var sp = @"CREATE PROCEDURE sp_HardDeleteUser
                @UserId NVARCHAR(450),
                @ConfirmDeletion BIT = 0,
                @Success BIT OUTPUT,
                @Message NVARCHAR(500) OUTPUT
            AS
            BEGIN
                SET NOCOUNT ON;

                DECLARE @RoleName NVARCHAR(256);
                DECLARE @HasActiveSessions BIT = 0;
                DECLARE @StudentsCount INT = 0;
                DECLARE @InstructorsCount INT = 0;
                DECLARE @AdminsCount INT = 0;

                -- Initialize output parameters
                SET @Success = 0;
                SET @Message = 'User not found or deletion failed';

                -- Check if deletion is explicitly confirmed
                IF @ConfirmDeletion = 0
                BEGIN
                    SET @Message = 'Hard deletion must be explicitly confirmed';
                    RETURN;
                END

                -- Check if user exists and get role
                SELECT TOP 1 @RoleName = r.Name
                FROM AspNetUsers u
                LEFT JOIN AspNetUserRoles ur ON u.Id = ur.UserId
                LEFT JOIN AspNetRoles r ON ur.RoleId = r.Id
                WHERE u.Id = @UserId;

                -- Check if user exists
                IF @RoleName IS NULL
                BEGIN
                    SET @Message = 'User not found';
                    RETURN;
                END

                -- Check if user has active sessions (prevent deletion of active users)
                -- FIXED: Changed ExpiryDate to ExpiresAt
                SELECT @HasActiveSessions = CASE WHEN COUNT(*) > 0 THEN 1 ELSE 0 END
                FROM RefreshTokens rt
                WHERE rt.UserId = @UserId
                AND rt.ExpiresAt > GETUTCDATE();

                IF @HasActiveSessions = 1
                BEGIN
                    SET @Message = 'Cannot delete user with active sessions';
                    RETURN;
                END

                -- Count related records for verification
                SELECT @StudentsCount = COUNT(*) FROM Students WHERE UserId = @UserId;
                SELECT @InstructorsCount = COUNT(*) FROM Instructors WHERE UserId = @UserId;
                SELECT @AdminsCount = COUNT(*) FROM Admins WHERE UserId = @UserId;

                -- Verify role matches profile records
                IF (@RoleName = 'Student' AND @StudentsCount = 0) OR
                   ((@RoleName = 'Teacher' OR @RoleName = 'Instructor') AND @InstructorsCount = 0) OR
                   (@RoleName = 'Admin' AND @AdminsCount = 0)
                BEGIN
                    SET @Message = 'User role does not match profile records';
                    RETURN;
                END

                BEGIN TRY
                    BEGIN TRANSACTION;

                    -- Hard delete based on role
                    IF @RoleName = 'Student'
                    BEGIN
                        -- Only attempt deletion if records exist
                        IF @StudentsCount > 0
                        BEGIN
                            -- Delete student's attendance records first (FK constraint: ON DELETE NO ACTION)
                            IF EXISTS (SELECT 1 FROM AttendanceRecords WHERE StudentId IN (SELECT Id FROM Students WHERE UserId = @UserId))
                                DELETE FROM AttendanceRecords
                                WHERE StudentId IN (SELECT Id FROM Students WHERE UserId = @UserId);

                            -- Delete student enrollments (handled by CASCADE, but explicit for clarity)
                            IF EXISTS (SELECT 1 FROM StudentEnrollments WHERE StudentId IN (SELECT Id FROM Students WHERE UserId = @UserId))
                                DELETE FROM StudentEnrollments
                                WHERE StudentId IN (SELECT Id FROM Students WHERE UserId = @UserId);

                            -- Delete student profile
                            DELETE FROM Students WHERE UserId = @UserId;
                            SET @Message = 'Student profile permanently deleted';
                        END
                        ELSE
                        BEGIN
                            SET @Message = 'No student profile found for user';
                        END
                    END
                    ELSE IF @RoleName = 'Teacher' OR @RoleName = 'Instructor'
                    BEGIN
                        -- Only attempt deletion if records exist
                        IF @InstructorsCount > 0
                        BEGIN
                            -- Nullify instructor references in Sessions (FK constraint: ON DELETE NO ACTION)
                            IF EXISTS (SELECT 1 FROM Sessions WHERE StartedBy IN (SELECT Id FROM Instructors WHERE UserId = @UserId))
                                UPDATE Sessions SET StartedBy = NULL WHERE StartedBy IN (SELECT Id FROM Instructors WHERE UserId = @UserId);

                            IF EXISTS (SELECT 1 FROM Sessions WHERE EndedBy IN (SELECT Id FROM Instructors WHERE UserId = @UserId))
                                UPDATE Sessions SET EndedBy = NULL WHERE EndedBy IN (SELECT Id FROM Instructors WHERE UserId = @UserId);

                            -- Nullify instructor references in Schedules (FK constraint: ON DELETE NO ACTION)
                            IF EXISTS (SELECT 1 FROM Schedules WHERE InstructorId IN (SELECT Id FROM Instructors WHERE UserId = @UserId))
                                UPDATE Schedules SET InstructorId = NULL WHERE InstructorId IN (SELECT Id FROM Instructors WHERE UserId = @UserId);

                            -- Delete instructor profile
                            DELETE FROM Instructors WHERE UserId = @UserId;
                            SET @Message = 'Instructor profile permanently deleted';
                        END
                        ELSE
                        BEGIN
                            SET @Message = 'No instructor profile found for user';
                        END
                    END
                    ELSE IF @RoleName = 'Admin'
                    BEGIN
                        -- Only attempt deletion if records exist
                        IF @AdminsCount > 0
                        BEGIN
                            -- Delete admin profile
                            DELETE FROM Admins WHERE UserId = @UserId;
                            SET @Message = 'Admin profile permanently deleted';
                        END
                        ELSE
                        BEGIN
                            SET @Message = 'No admin profile found for user';
                        END
                    END

                    -- Delete all Identity-related data (only if records exist)
                    IF EXISTS (SELECT 1 FROM AspNetUserRoles WHERE UserId = @UserId)
                        DELETE FROM AspNetUserRoles WHERE UserId = @UserId;

                    IF EXISTS (SELECT 1 FROM AspNetUserClaims WHERE UserId = @UserId)
                        DELETE FROM AspNetUserClaims WHERE UserId = @UserId;

                    IF EXISTS (SELECT 1 FROM AspNetUserLogins WHERE UserId = @UserId)
                        DELETE FROM AspNetUserLogins WHERE UserId = @UserId;

                    IF EXISTS (SELECT 1 FROM AspNetUserTokens WHERE UserId = @UserId)
                        DELETE FROM AspNetUserTokens WHERE UserId = @UserId;

                    IF EXISTS (SELECT 1 FROM RefreshTokens WHERE UserId = @UserId)
                        DELETE FROM RefreshTokens WHERE UserId = @UserId;

                    -- Finally delete the user
                    DELETE FROM AspNetUsers WHERE Id = @UserId;

                    COMMIT TRANSACTION;
                    SET @Success = 1;
                    SET @Message = @Message + ' and all associated data removed';
                END TRY
                BEGIN CATCH
                    IF @@TRANCOUNT > 0
                        ROLLBACK TRANSACTION;

                    SET @Success = 0;
                    SET @Message = 'Deletion failed: ' + ERROR_MESSAGE();
                END CATCH
            END";

            migrationBuilder.Sql(sp);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("DROP PROCEDURE IF EXISTS sp_HardDeleteUser");
        }
    }
}
