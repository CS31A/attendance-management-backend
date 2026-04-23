using attendance_monitoring.Classes;
using attendance_monitoring.Data;
using Microsoft.AspNetCore.Identity;

namespace attendance.testproject.Integration_Testing.Support;

/// <summary>
/// Seed data for instructor sections integration tests.
/// </summary>
internal static class InstructorSeedData
{
    public static async Task<InstructorScenarioContext> SeedScenarioAsync(
        ApplicationDbContext dbContext,
        CancellationToken cancellationToken = default)
    {
        await dbContext.Database.EnsureDeletedAsync(cancellationToken);
        await dbContext.Database.EnsureCreatedAsync(cancellationToken);

        var now = DateTime.UtcNow;

        // Create Identity users
        var instructorUser = CreateIdentityUser("instructor-user-1", "instructor1@example.test");
        var instructorWithNoSchedulesUser = CreateIdentityUser("instructor-user-2", "instructor2@example.test");

        dbContext.Users.AddRange(instructorUser, instructorWithNoSchedulesUser);

        // Create course
        var course = new Course
        {
            Name = "Bachelor of Science in Computer Science",
            CreatedAt = now,
            UpdatedAt = now
        };

        // Create sections
        var section1 = new Section
        {
            Name = "BSCS 3A",
            Course = course,
            CreatedAt = now,
            UpdatedAt = now
        };

        var section2 = new Section
        {
            Name = "BSCS 3B",
            Course = course,
            CreatedAt = now,
            UpdatedAt = now
        };

        // Create subjects
        var subject1 = new Subject
        {
            Name = "Data Structures",
            Code = "CS301",
            CreatedAt = now,
            UpdatedAt = now
        };

        var subject2 = new Subject
        {
            Name = "Algorithms",
            Code = "CS302",
            CreatedAt = now,
            UpdatedAt = now
        };

        // Create classrooms
        var classroom1 = new Classroom
        {
            Name = "Room 101",
            CreatedAt = now,
            UpdatedAt = now
        };

        var classroom2 = new Classroom
        {
            Name = "Room 102",
            CreatedAt = now,
            UpdatedAt = now
        };

        // Create instructors
        var instructor = new Instructor
        {
            UserId = instructorUser.Id,
            Firstname = "John",
            Lastname = "Doe",
            CreatedAt = now,
            UpdatedAt = now
        };

        var instructorWithNoSchedules = new Instructor
        {
            UserId = instructorWithNoSchedulesUser.Id,
            Firstname = "Jane",
            Lastname = "Smith",
            CreatedAt = now,
            UpdatedAt = now
        };

        // Create students
        var regularStudent1 = new Student
        {
            UserId = "student-user-1",
            Firstname = "Alice",
            Lastname = "Johnson",
            Section = section1,
            IsRegular = true,
            CreatedAt = now,
            UpdatedAt = now
        };

        var regularStudent2 = new Student
        {
            UserId = "student-user-2",
            Firstname = "Bob",
            Lastname = "Williams",
            Section = section1,
            IsRegular = true,
            CreatedAt = now,
            UpdatedAt = now
        };

        var irregularStudent = new Student
        {
            UserId = "student-user-3",
            Firstname = "Charlie",
            Lastname = "Brown",
            Section = section2,
            IsRegular = false,
            CreatedAt = now,
            UpdatedAt = now
        };

        // Create schedules for instructor with sections
        var schedule1 = new Schedules
        {
            Subject = subject1,
            Section = section1,
            Classroom = classroom1,
            Instructor = instructor,
            DayOfWeek = "Monday",
            TimeIn = new TimeOnly(8, 0),
            TimeOut = new TimeOnly(10, 0),
            CreatedAt = now,
            UpdatedAt = now
        };

        var schedule2 = new Schedules
        {
            Subject = subject2,
            Section = section1,
            Classroom = classroom2,
            Instructor = instructor,
            DayOfWeek = "Wednesday",
            TimeIn = new TimeOnly(10, 0),
            TimeOut = new TimeOnly(12, 0),
            CreatedAt = now,
            UpdatedAt = now
        };

        dbContext.AddRange(
            course,
            section1,
            section2,
            subject1,
            subject2,
            classroom1,
            classroom2,
            instructor,
            instructorWithNoSchedules,
            regularStudent1,
            regularStudent2,
            irregularStudent,
            schedule1,
            schedule2);

        await dbContext.SaveChangesAsync(cancellationToken);

        // Create student enrollment for irregular student
        var irregularEnrollment = new StudentEnrollment
        {
            StudentId = irregularStudent.Id,
            SectionId = section1.Id,
            SubjectId = subject1.Id,
            EnrollmentType = "Irregular",
            IsActive = true,
            AcademicYear = "2026",
            Semester = "2nd",
            EnrolledAt = now,
            CreatedAt = now,
            UpdatedAt = now
        };

        dbContext.StudentEnrollments.Add(irregularEnrollment);
        await dbContext.SaveChangesAsync(cancellationToken);

        return new InstructorScenarioContext
        {
            InstructorId = instructor.Id,
            InstructorUserId = instructorUser.Id,
            InstructorUsername = instructorUser.UserName!,
            InstructorFirstname = instructor.Firstname,
            InstructorLastname = instructor.Lastname,
            InstructorWithNoSchedulesId = instructorWithNoSchedules.Id,
            InstructorWithNoSchedulesUserId = instructorWithNoSchedulesUser.Id,
            InstructorWithNoSchedulesUsername = instructorWithNoSchedulesUser.UserName!
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
