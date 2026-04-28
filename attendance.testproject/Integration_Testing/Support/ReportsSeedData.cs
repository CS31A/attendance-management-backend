using attendance_monitoring.Classes;
using attendance_monitoring.Constants;
using attendance_monitoring.Data;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace attendance.testproject.Integration_Testing.Support;

/// <summary>
/// Context data for reports authorization integration tests.
/// </summary>
internal sealed class ReportsScenarioContext
{
    public required string AdminUserId { get; init; }
    public required string InstructorUserId { get; init; }
    public required string OtherInstructorUserId { get; init; }
    public required string StudentUserId { get; init; }
    public required string OutsiderStudentUserId { get; init; }
    public required Guid InstructorId { get; init; }
    public required Guid InstructorUuid { get; init; }
    public required Guid OtherInstructorId { get; init; }
    public required Guid OtherInstructorUuid { get; init; }
    public required Guid StudentId { get; init; }
    public required Guid StudentUuid { get; init; }
    public required Guid OutsiderStudentId { get; init; }
    public required Guid OutsiderStudentUuid { get; init; }
    public required Guid SectionId { get; init; }
    public required Guid SectionUuid { get; init; }
    public required Guid SessionId { get; init; }
    public required Guid SessionUuid { get; init; }
    public required Guid ScheduleId { get; init; }
    public required Guid ScheduleUuid { get; init; }
}

/// <summary>
/// Seed data for reports authorization integration tests.
/// </summary>
internal static class ReportsSeedData
{
    public static async Task<ReportsScenarioContext> SeedScenarioAsync(
        ApplicationDbContext dbContext,
        CancellationToken cancellationToken = default)
    {
        await dbContext.Database.EnsureDeletedAsync(cancellationToken);
        await dbContext.Database.EnsureCreatedAsync(cancellationToken);

        var now = DateTime.UtcNow;

        // Create Identity users
        var adminUser = CreateIdentityUser("integration-admin", "integration-admin@example.test");
        var instructorUser = CreateIdentityUser("integration-instructor", "integration-instructor@example.test");
        var otherInstructorUser = CreateIdentityUser("other-instructor", "other-instructor@example.test");
        var studentUser = CreateIdentityUser("integration-student", "integration-student@example.test");
        var outsiderUser = CreateIdentityUser("integration-outsider", "integration-outsider@example.test");

        dbContext.Users.AddRange(adminUser, instructorUser, otherInstructorUser, studentUser, outsiderUser);

        // Create course and sections
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

        // Create subject and classroom
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

        // Create instructor
        var instructor = new Instructor
        {
            UserId = instructorUser.Id,
            Firstname = "Ivy",
            Lastname = "Instructor",
            CreatedAt = now,
            UpdatedAt = now
        };

        var otherInstructor = new Instructor
        {
            UserId = otherInstructorUser.Id,
            Firstname = "Other",
            Lastname = "Instructor",
            CreatedAt = now,
            UpdatedAt = now
        };

        // Create students
        var student = new Student
        {
            UserId = studentUser.Id,
            Firstname = "Sam",
            Lastname = "Student",
            Section = section,
            IsRegular = true,
            Usn = "TEST-SAM-REPORT-001",
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
            Usn = "TEST-OLLY-REPORT-001",
            CreatedAt = now,
            UpdatedAt = now
        };

        // Create schedule
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

        // Create session
        var session = new Session
        {
            Schedule = schedule,
            Status = SessionStatusConstants.Ended,
            SessionDate = now.Date,
            ActualStartTime = now.AddMinutes(-10),
            AttendanceCutOff = now.AddMinutes(5),
            ActualRoom = classroom,
            CreatedAt = now,
            UpdatedAt = now
        };

        dbContext.AddRange(course, section, outsiderSection, subject, classroom, instructor, otherInstructor, student, outsiderStudent, schedule, session);
        await dbContext.SaveChangesAsync(cancellationToken);

        // Create enrollment
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

        dbContext.StudentEnrollments.Add(regularEnrollment);
        await dbContext.SaveChangesAsync(cancellationToken);

        return new ReportsScenarioContext
        {
            AdminUserId = adminUser.Id,
            InstructorUserId = instructorUser.Id,
            OtherInstructorUserId = otherInstructorUser.Id,
            StudentUserId = studentUser.Id,
            OutsiderStudentUserId = outsiderUser.Id,
            InstructorId = instructor.Id,
            InstructorUuid = instructor.Id,
            OtherInstructorId = otherInstructor.Id,
            OtherInstructorUuid = otherInstructor.Id,
            StudentId = student.Id,
            StudentUuid = student.Id,
            OutsiderStudentId = outsiderStudent.Id,
            OutsiderStudentUuid = outsiderStudent.Id,
            SectionId = section.Id,
            SectionUuid = section.Id,
            SessionId = session.Id,
            SessionUuid = session.Id,
            ScheduleId = schedule.Id,
            ScheduleUuid = schedule.Id
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
