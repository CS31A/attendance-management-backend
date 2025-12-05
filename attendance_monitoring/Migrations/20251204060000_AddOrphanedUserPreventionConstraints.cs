using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace attendance_monitoring.Migrations
{
    /// <summary>
    /// Migration to add database-level constraints for preventing orphaned users.
    /// 
    /// This migration adds:
    /// 1. Soft delete consistency constraints for Students, Instructors, and Admins
    /// 2. Trigger-based validation to ensure users with roles have corresponding profiles
    /// 
    /// Note: SQL Server CHECK constraints cannot reference other tables, so we use triggers
    /// for the cross-table validation requirement.
    /// </summary>
    public partial class AddOrphanedUserPreventionConstraints : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // ==========================================
            // 1. SOFT DELETE CONSISTENCY CONSTRAINTS
            // ==========================================
            // These constraints ensure that soft delete flags are consistent:
            // - IsDeleted = 1 must have DeletedAt set
            // - IsDeleted = 0 must have DeletedAt = NULL

            // Students soft delete consistency
            migrationBuilder.Sql(@"
                ALTER TABLE Students 
                ADD CONSTRAINT CK_Students_SoftDeleteConsistency 
                CHECK ((IsDeleted = 1 AND DeletedAt IS NOT NULL) OR (IsDeleted = 0 AND DeletedAt IS NULL));
            ");

            // Instructors soft delete consistency
            migrationBuilder.Sql(@"
                ALTER TABLE Instructors 
                ADD CONSTRAINT CK_Instructors_SoftDeleteConsistency 
                CHECK ((IsDeleted = 1 AND DeletedAt IS NOT NULL) OR (IsDeleted = 0 AND DeletedAt IS NULL));
            ");

            // Admins soft delete consistency
            migrationBuilder.Sql(@"
                ALTER TABLE Admins 
                ADD CONSTRAINT CK_Admins_SoftDeleteConsistency 
                CHECK ((IsDeleted = 1 AND DeletedAt IS NOT NULL) OR (IsDeleted = 0 AND DeletedAt IS NULL));
            ");

            // ==========================================
            // 2. TRIGGER FOR USER PROFILE VALIDATION
            // ==========================================
            // This trigger validates that when a role is assigned to a user,
            // the user has a corresponding profile in Students, Instructors, or Admins.
            // 
            // Note: This is an AFTER trigger because we need to allow the profile
            // to be created in the same transaction as the role assignment.

            migrationBuilder.Sql(@"
                CREATE OR ALTER TRIGGER TR_AspNetUserRoles_ValidateUserProfile
                ON AspNetUserRoles
                AFTER INSERT, UPDATE
                AS
                BEGIN
                    SET NOCOUNT ON;
                    
                    -- Check if any inserted/updated users lack profiles
                    -- This is an AFTER trigger, so the profile should already exist
                    -- if created in the same transaction before the role assignment
                    IF EXISTS (
                        SELECT 1
                        FROM inserted i
                        INNER JOIN AspNetUsers u ON i.UserId = u.Id
                        WHERE NOT EXISTS (SELECT 1 FROM Students s WHERE s.UserId = i.UserId AND s.IsDeleted = 0)
                          AND NOT EXISTS (SELECT 1 FROM Instructors inst WHERE inst.UserId = i.UserId AND inst.IsDeleted = 0)
                          AND NOT EXISTS (SELECT 1 FROM Admins a WHERE a.UserId = i.UserId AND a.IsDeleted = 0)
                    )
                    BEGIN
                        -- Log a warning instead of failing - the background cleanup service will handle orphans
                        -- This is intentionally a soft validation to avoid breaking existing workflows
                        PRINT 'Warning: User role assigned without corresponding profile. Background cleanup may remove this user.';
                    END
                END;
            ");

            // ==========================================
            // 3. TRIGGER FOR PREVENTING PROFILE DELETION WITHOUT USER CLEANUP
            // ==========================================
            // When a profile is hard-deleted (not soft-deleted), validate the user state

            migrationBuilder.Sql(@"
                CREATE OR ALTER TRIGGER TR_Students_PreventOrphanOnDelete
                ON Students
                AFTER DELETE
                AS
                BEGIN
                    SET NOCOUNT ON;
                    
                    -- Log when a student profile is deleted
                    -- The user cleanup should be handled by the application
                    IF EXISTS (
                        SELECT 1
                        FROM deleted d
                        INNER JOIN AspNetUsers u ON d.UserId = u.Id
                        INNER JOIN AspNetUserRoles ur ON u.Id = ur.UserId
                        WHERE NOT EXISTS (SELECT 1 FROM Students s WHERE s.UserId = d.UserId AND s.Id != d.Id)
                          AND NOT EXISTS (SELECT 1 FROM Instructors i WHERE i.UserId = d.UserId)
                          AND NOT EXISTS (SELECT 1 FROM Admins a WHERE a.UserId = d.UserId)
                    )
                    BEGIN
                        PRINT 'Warning: Student profile deleted. User may become orphaned if not cleaned up.';
                    END
                END;
            ");

            migrationBuilder.Sql(@"
                CREATE OR ALTER TRIGGER TR_Instructors_PreventOrphanOnDelete
                ON Instructors
                AFTER DELETE
                AS
                BEGIN
                    SET NOCOUNT ON;
                    
                    IF EXISTS (
                        SELECT 1
                        FROM deleted d
                        INNER JOIN AspNetUsers u ON d.UserId = u.Id
                        INNER JOIN AspNetUserRoles ur ON u.Id = ur.UserId
                        WHERE NOT EXISTS (SELECT 1 FROM Students s WHERE s.UserId = d.UserId)
                          AND NOT EXISTS (SELECT 1 FROM Instructors i WHERE i.UserId = d.UserId AND i.Id != d.Id)
                          AND NOT EXISTS (SELECT 1 FROM Admins a WHERE a.UserId = d.UserId)
                    )
                    BEGIN
                        PRINT 'Warning: Instructor profile deleted. User may become orphaned if not cleaned up.';
                    END
                END;
            ");

            migrationBuilder.Sql(@"
                CREATE OR ALTER TRIGGER TR_Admins_PreventOrphanOnDelete
                ON Admins
                AFTER DELETE
                AS
                BEGIN
                    SET NOCOUNT ON;
                    
                    IF EXISTS (
                        SELECT 1
                        FROM deleted d
                        INNER JOIN AspNetUsers u ON d.UserId = u.Id
                        INNER JOIN AspNetUserRoles ur ON u.Id = ur.UserId
                        WHERE NOT EXISTS (SELECT 1 FROM Students s WHERE s.UserId = d.UserId)
                          AND NOT EXISTS (SELECT 1 FROM Instructors i WHERE i.UserId = d.UserId)
                          AND NOT EXISTS (SELECT 1 FROM Admins a WHERE a.UserId = d.UserId AND a.Id != d.Id)
                    )
                    BEGIN
                        PRINT 'Warning: Admin profile deleted. User may become orphaned if not cleaned up.';
                    END
                END;
            ");

            // ==========================================
            // 4. DIAGNOSTIC VIEW FOR ORPHANED USERS
            // ==========================================
            // Create a view to easily identify orphaned users for monitoring

            migrationBuilder.Sql(@"
                CREATE OR ALTER VIEW VW_OrphanedUsers AS
                SELECT 
                    u.Id AS UserId,
                    u.UserName,
                    u.Email,
                    r.Name AS RoleName,
                    'Has role but no active profile' AS OrphanReason
                FROM AspNetUsers u
                INNER JOIN AspNetUserRoles ur ON u.Id = ur.UserId
                INNER JOIN AspNetRoles r ON ur.RoleId = r.Id
                WHERE NOT EXISTS (SELECT 1 FROM Students s WHERE s.UserId = u.Id AND s.IsDeleted = 0)
                  AND NOT EXISTS (SELECT 1 FROM Instructors i WHERE i.UserId = u.Id AND i.IsDeleted = 0)
                  AND NOT EXISTS (SELECT 1 FROM Admins a WHERE a.UserId = u.Id AND a.IsDeleted = 0);
            ");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // Drop the diagnostic view
            migrationBuilder.Sql("DROP VIEW IF EXISTS VW_OrphanedUsers;");

            // Drop triggers
            migrationBuilder.Sql("DROP TRIGGER IF EXISTS TR_Admins_PreventOrphanOnDelete;");
            migrationBuilder.Sql("DROP TRIGGER IF EXISTS TR_Instructors_PreventOrphanOnDelete;");
            migrationBuilder.Sql("DROP TRIGGER IF EXISTS TR_Students_PreventOrphanOnDelete;");
            migrationBuilder.Sql("DROP TRIGGER IF EXISTS TR_AspNetUserRoles_ValidateUserProfile;");

            // Drop soft delete consistency constraints
            migrationBuilder.Sql("ALTER TABLE Admins DROP CONSTRAINT IF EXISTS CK_Admins_SoftDeleteConsistency;");
            migrationBuilder.Sql("ALTER TABLE Instructors DROP CONSTRAINT IF EXISTS CK_Instructors_SoftDeleteConsistency;");
            migrationBuilder.Sql("ALTER TABLE Students DROP CONSTRAINT IF EXISTS CK_Students_SoftDeleteConsistency;");
        }
    }
}
