using attendance_monitoring.Classes;
using attendance_monitoring.Constants;
using attendance_monitoring.Data;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace attendance.testproject.Integration_Testing.Support;

internal static class AttendanceQrSeedData
{
    internal const string ValidAttendanceCreate = nameof(ValidAttendanceCreate);
    internal const string ExistingAttendanceDuplicate = nameof(ExistingAttendanceDuplicate);
    internal const string ValidQrScan = nameof(ValidQrScan);
    internal const string DuplicateQrScan = nameof(DuplicateQrScan);

    public static async Task<AttendanceQrScenarioContext> SeedScenarioAsync(
        ApplicationDbContext dbContext,
        string scenarioName,
        CancellationToken cancellationToken = default)
    {
        await dbContext.Database.EnsureDeletedAsync(cancellationToken);
        await dbContext.Database.EnsureCreatedAsync(cancellationToken);

        var now = DateTime.UtcNow;

        var adminUser = CreateIdentityUser("integration-admin", "integration-admin@example.test");
        var instructorUser = CreateIdentityUser("integration-instructor", "integration-instructor@example.test");
        var studentUser = CreateIdentityUser("integration-student", "integration-student@example.test");
        var outsiderUser = CreateIdentityUser("integration-outsider", "integration-outsider@example.test");

        dbContext.Users.AddRange(adminUser, instructorUser, studentUser, outsiderUser);

        var course = new Course
        {
            Name = "Integration Course",
            CreatedAt = now,
            UpdatedAt = now
        };

        var section = new Section
        {
            Name = "INT-SEC-A",
            Course = course,
            CreatedAt = now,
            UpdatedAt = now
        };

        var outsiderSection = new Section
        {
            Name = "INT-SEC-B",
            Course = course,
            CreatedAt = now,
            UpdatedAt = now
        };

        var subject = new Subject
        {
            Name = "Integration Testing",
            Code = "ITEST1",
            CreatedAt = now,
            UpdatedAt = now
        };

        var classroom = new Classroom
        {
            Name = "Integration Room 1",
            CreatedAt = now,
            UpdatedAt = now
        };

        var instructor = new Instructor
        {
            UserId = instructorUser.Id,
            Firstname = "Ivy",
            Lastname = "Instructor",
            CreatedAt = now,
            UpdatedAt = now
        };

        var student = new Student
        {
            UserId = studentUser.Id,
            Firstname = "Sam",
            Lastname = "Student",
            Section = section,
            IsRegular = true,
            Usn = "TEST-SAM-001",
            CreatedAt = now,
            UpdatedAt = now
        };

        var outsiderStudent = new Student
        {
            UserId = outsiderUser.Id,
            Firstname = "Olly",
            Lastname = "Outsider",
            Section = outsiderSection,
            IsRegular = true,
            Usn = "TEST-OLLY-001",
            CreatedAt = now,
            UpdatedAt = now
        };

        var schedule = new Schedules
        {
            Subject = subject,
            Section = section,
            Classroom = classroom,
            Instructor = instructor,
            DayOfWeek = now.DayOfWeek.ToString(),
            TimeIn = TimeOnly.FromDateTime(now.AddMinutes(-10)),
            TimeOut = TimeOnly.FromDateTime(now.AddHours(1)),
            CreatedAt = now,
            UpdatedAt = now
        };

        var session = new Session
        {
            Schedule = schedule,
            Status = SessionStatusConstants.Active,
            SessionDate = now.Date,
            ActualStartTime = now.AddMinutes(-10),
            AttendanceCutOff = now.AddMinutes(5),
            ActualRoom = classroom,
            RowVersion = [1, 2, 3, 4],
            CreatedAt = now,
            UpdatedAt = now
        };

        dbContext.AddRange(course, section, outsiderSection, subject, classroom, instructor, student, outsiderStudent, schedule, session);
        await dbContext.SaveChangesAsync(cancellationToken);

        var regularEnrollment = new StudentEnrollment
        {
            StudentId = student.Id,
            SectionId = section.Id,
            SubjectId = subject.Id,
            EnrollmentType = "Regular",
            IsActive = true,
            AcademicYear = "2026",
            Semester = "2nd",
            EnrolledAt = now,
            CreatedAt = now,
            UpdatedAt = now
        };

        var qrCode = new QrCode
        {
            SessionId = session.Id,
            QrHash = $"qr-{scenarioName.ToLowerInvariant()}",
            GeneratedAt = now,
            ExpiresAt = now.AddMinutes(20),
            MaxUsage = 10,
            IsActive = true,
            UsageCount = 0,
            CreatedAt = now,
            UpdatedAt = now
        };

        dbContext.StudentEnrollments.Add(regularEnrollment);
        dbContext.QrCodes.Add(qrCode);
        await dbContext.SaveChangesAsync(cancellationToken);

        AttendanceRecord? attendanceRecord = null;

        if (scenarioName is ExistingAttendanceDuplicate or DuplicateQrScan)
        {
            var isQrSeed = scenarioName == DuplicateQrScan;

            attendanceRecord = new AttendanceRecord
            {
                StudentId = student.Id,
                SessionId = session.Id,
                QrCodeId = isQrSeed ? qrCode.Id : null,
                CheckInTime = now.AddMinutes(-2),
                Status = "Present",
                Notes = isQrSeed ? "Seeded duplicate QR scan" : "Seeded duplicate attendance",
                IsManualEntry = !isQrSeed,
                EnteredBy = isQrSeed ? null : instructorUser.Id,
                CreatedAt = now,
                UpdatedAt = now
            };

            dbContext.AttendanceRecords.Add(attendanceRecord);

            if (isQrSeed)
            {
                qrCode.UsageCount = 1;
                dbContext.QrCodes.Update(qrCode);
            }

            await dbContext.SaveChangesAsync(cancellationToken);
        }

        return new AttendanceQrScenarioContext
        {
            ScenarioName = scenarioName,
            AdminUserId = adminUser.Id,
            InstructorUserId = instructorUser.Id,
            StudentUserId = studentUser.Id,
            OutsiderStudentUserId = outsiderUser.Id,
            StudentId = student.Id,
            OutsiderStudentId = outsiderStudent.Id,
            SessionId = session.Id,
            QrCodeId = qrCode.Id,
            QrHash = qrCode.QrHash,
            ExistingAttendanceRecordId = attendanceRecord?.Id
        };
    }

    private static IdentityUser CreateIdentityUser(string id, string email)
    {
        return new IdentityUser
        {
            Id = id,
            UserName = email,
            NormalizedUserName = email.ToUpperInvariant(),
            Email = email,
            NormalizedEmail = email.ToUpperInvariant(),
            SecurityStamp = Guid.NewGuid().ToString("N"),
            ConcurrencyStamp = Guid.NewGuid().ToString("N"),
            EmailConfirmed = true
        };
    }
}

internal sealed class AttendanceQrScenarioContext
{
    public required string ScenarioName { get; init; }
    public required string AdminUserId { get; init; }
    public required string InstructorUserId { get; init; }
    public required string StudentUserId { get; init; }
    public required string OutsiderStudentUserId { get; init; }
    public required Guid StudentId { get; init; }
    public required Guid OutsiderStudentId { get; init; }
    public required Guid SessionId { get; init; }
    public required Guid QrCodeId { get; init; }
    public required string QrHash { get; init; }
    public Guid? ExistingAttendanceRecordId { get; init; }
}
