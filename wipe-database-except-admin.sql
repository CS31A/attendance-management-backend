-- SQL Script to wipe all database data except the admin account
-- Preserves: admin@attendance.com or the user with "System Administrator" name
-- Database: attendance_management

BEGIN TRANSACTION;

-- Preserve admin user ID
DECLARE @AdminUserId NVARCHAR(450);
DECLARE @AdminId UNIQUEIDENTIFIER;

-- Get admin user by email or name
SELECT @AdminUserId = Id FROM AspNetUsers WHERE Email = 'admin@attendance.com';

IF @AdminUserId IS NULL
BEGIN
    -- Try to find by admin profile name
    SELECT @AdminUserId = a.UserId 
    FROM Admins a 
    WHERE a.Firstname = 'System' AND a.Lastname = 'Administrator';
END

IF @AdminUserId IS NULL
BEGIN
    ROLLBACK;
    PRINT 'ERROR: Admin account not found. Cannot proceed with data wipe.';
    RETURN;
END

PRINT 'Preserving admin user ID: ' + ISNULL(@AdminUserId, 'NULL');

-- Delete in order of dependency (most dependent first)

-- 1. Attendance records (depends on Student, Session, QrCode)
DELETE FROM AttendanceRecords;

-- 2. QR codes (depends on Session)
DELETE FROM QrCodes;

-- 3. Sessions (depends on Schedule, ActualRoom, InstructorWhoStarted, InstructorWhoEnded)
DELETE FROM Sessions;

-- 4. Fingerprint scan events (depends on Device, MatchedStudent, Session, AttendanceRecord)
DELETE FROM FingerprintScanEvents;

-- 5. Fingerprint enrollment sessions (depends on Device, Student)
DELETE FROM FingerprintEnrollmentSessions;

-- 6. Fingerprints (except admin's fingerprints, if any)
DELETE FROM Fingerprints WHERE UserId <> @AdminUserId;

-- 7. Student enrollments (depends on Student, Section, Subject)
DELETE FROM StudentEnrollments;

-- 8. Schedules (depends on Subject, Section, Classroom, Instructor)
DELETE FROM Schedules;

-- 9. Refresh tokens (except admin's)
DELETE FROM RefreshTokens WHERE UserId <> @AdminUserId;

-- 10. Students (depends on Section, User)
DELETE FROM Students;

-- 11. Instructors (depends on User)
DELETE FROM Instructors;

-- 12. Admins (except the preserved admin)
DELETE FROM Admins WHERE UserId <> @AdminUserId;

-- 13. Sections
DELETE FROM Sections;

-- 14. Subjects
DELETE FROM Subjects;

-- 15. Classrooms
DELETE FROM Classrooms;

-- 16. Courses
DELETE FROM Courses;

-- 17. Fingerprint devices
DELETE FROM FingerprintDevices;

-- 18. Blacklisted tokens (no foreign keys, safe to delete)
DELETE FROM BlacklistedTokens;

-- 19. ASP.NET Identity data (except admin)
DELETE FROM AspNetUserRoles WHERE UserId <> @AdminUserId;
DELETE FROM AspNetUserLogins WHERE UserId <> @AdminUserId;
DELETE FROM AspNetUserClaims WHERE UserId <> @AdminUserId;
DELETE FROM AspNetUserTokens WHERE UserId <> @AdminUserId;
DELETE FROM AspNetUsers WHERE Id <> @AdminUserId;

-- Note: We keep AspNetRoles as they are configuration, not user data

COMMIT TRANSACTION;

PRINT 'Database wipe completed successfully.';
PRINT 'Preserved admin user ID: ' + ISNULL(@AdminUserId, 'NULL');
