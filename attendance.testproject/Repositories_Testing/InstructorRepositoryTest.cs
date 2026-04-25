using attendance_monitoring.Classes;
using attendance_monitoring.Data;
using attendance_monitoring.Repositories;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace attendance.testproject.Repositories_Testing;

/// <summary>
/// Unit tests for InstructorRepository
/// Tests GetSchedulesWithRelatedDataByInstructorIdAsync method
/// </summary>
public class InstructorRepositoryTest : IDisposable
{
    private readonly ApplicationDbContext _context;
    private readonly InstructorRepository _repository;

    public InstructorRepositoryTest()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        _context = new ApplicationDbContext(options);
        _repository = new InstructorRepository(_context);
    }

    [Fact]
    public async Task GetSchedulesWithRelatedDataByInstructorIdAsync_ReturnsSchedulesWithRelatedEntities_WhenInstructorHasSchedules()
    {
        // Arrange
        var instructorId = Guid.NewGuid();
        await SeedTestDataAsync(instructorId);

        // Act
        var result = await _repository.GetSchedulesWithRelatedDataByInstructorIdAsync(instructorId);

        // Assert
        var schedules = result.ToList();
        Assert.NotEmpty(schedules);
        Assert.Equal(2, schedules.Count);

        // Verify first schedule has all related entities loaded
        var firstSchedule = schedules[0];
        Assert.NotNull(firstSchedule.Section);
        Assert.NotNull(firstSchedule.Section.Course);
        Assert.NotNull(firstSchedule.Subject);
        Assert.NotNull(firstSchedule.Classroom);
        Assert.NotNull(firstSchedule.Section.StudentEnrollments);

        // Verify second schedule has all related entities loaded
        var secondSchedule = schedules[1];
        Assert.NotNull(secondSchedule.Section);
        Assert.NotNull(secondSchedule.Section.Course);
        Assert.NotNull(secondSchedule.Subject);
        Assert.NotNull(secondSchedule.Classroom);
        Assert.NotNull(secondSchedule.Section.StudentEnrollments);

        // Verify data integrity
        Assert.Equal("Math", firstSchedule.Subject.Name);
        Assert.Equal("BSCS 3A", firstSchedule.Section.Name);
        Assert.Equal("Room 101", firstSchedule.Classroom.Name);
        Assert.Equal("Bachelor of Science in Computer Science", firstSchedule.Section.Course.Name);
    }

    [Fact]
    public async Task GetSchedulesWithRelatedDataByInstructorIdAsync_ExcludesDeletedStudents_WhenStudentEnrollmentsExist()
    {
        // Arrange
        var instructorId = Guid.NewGuid();
        await SeedTestDataWithDeletedStudentsAsync(instructorId);

        // Act
        var result = await _repository.GetSchedulesWithRelatedDataByInstructorIdAsync(instructorId);

        // Assert
        var schedules = result.ToList();
        var schedule = Assert.Single(schedules);
        
        // Verify only active student enrollments are included
        var activeEnrollments = schedule.Section.StudentEnrollments.ToList();
        Assert.Equal(2, activeEnrollments.Count);
        
        // Verify all returned enrollments are active
        Assert.All(activeEnrollments, enrollment => Assert.True(enrollment.IsActive));
        
        // Verify students in enrollments are not deleted
        Assert.All(activeEnrollments, enrollment => 
        {
            Assert.NotNull(enrollment.Student);
            Assert.False(enrollment.Student.IsDeleted);
        });
    }

    [Fact]
    public async Task GetSchedulesWithRelatedDataByInstructorIdAsync_ReturnsEmptyList_WhenInstructorHasNoSchedules()
    {
        // Arrange
        var instructorId = Guid.NewGuid(); // Non-existent instructor
        await SeedTestDataAsync(Guid.NewGuid()); // Seed data for different instructor

        // Act
        var result = await _repository.GetSchedulesWithRelatedDataByInstructorIdAsync(instructorId);

        // Assert
        Assert.Empty(result);
    }

    [Fact]
    public async Task GetSchedulesWithRelatedDataByInstructorIdAsync_LoadsStudentEnrollmentsWithStudents_WhenEnrollmentsExist()
    {
        // Arrange
        var instructorId = Guid.NewGuid();
        await SeedTestDataWithIrregularStudentsAsync(instructorId);

        // Act
        var result = await _repository.GetSchedulesWithRelatedDataByInstructorIdAsync(instructorId);

        // Assert
        var schedules = result.ToList();
        var schedule = Assert.Single(schedules);
        
        var enrollments = schedule.Section.StudentEnrollments.ToList();
        Assert.Equal(3, enrollments.Count);
        
        // Verify each enrollment has student loaded
        Assert.All(enrollments, enrollment =>
        {
            Assert.NotNull(enrollment.Student);
            Assert.NotEmpty(enrollment.Student.Firstname);
            Assert.NotEmpty(enrollment.Student.Lastname);
        });
    }

    private async Task SeedTestDataAsync(Guid instructorId)
    {
        var user = new IdentityUser
        {
            Id = $"user-{instructorId}",
            UserName = $"instructor-{instructorId}@example.com",
            NormalizedUserName = $"INSTRUCTOR-{instructorId}@EXAMPLE.COM".ToUpper(),
            Email = $"instructor-{instructorId}@example.com",
            NormalizedEmail = $"INSTRUCTOR-{instructorId}@EXAMPLE.COM".ToUpper(),
        };

        var course = new Course
        {
            Id = Guid.NewGuid(),
            Name = "Bachelor of Science in Computer Science"
        };

        var section = new Section
        {
            Id = Guid.NewGuid(),
            Name = "BSCS 3A",
            CourseId = course.Id,
            Course = course
        };

        var subject1 = new Subject
        {
            Id = Guid.NewGuid(),
            Name = "Math",
            Code = "MATH101"
        };

        var subject2 = new Subject
        {
            Id = Guid.NewGuid(),
            Name = "Physics",
            Code = "PHYS101"
        };

        var classroom = new Classroom
        {
            Id = Guid.NewGuid(),
            Name = "Room 101"
        };

        var instructor = new Instructor
        {
            Id = instructorId,
            Firstname = "John",
            Lastname = "Doe",
            UserId = user.Id,
            User = user,
        };

        var schedule1 = new Schedules
        {
            Id = Guid.NewGuid(),
            TimeIn = new TimeOnly(8, 0),
            TimeOut = new TimeOnly(10, 0),
            DayOfWeek = "Monday",
            SubjectId = subject1.Id,
            Subject = subject1,
            ClassroomId = classroom.Id,
            Classroom = classroom,
            SectionId = section.Id,
            Section = section,
            InstructorId = instructor.Id,
            Instructor = instructor,
        };

        var schedule2 = new Schedules
        {
            Id = Guid.NewGuid(),
            TimeIn = new TimeOnly(10, 0),
            TimeOut = new TimeOnly(12, 0),
            DayOfWeek = "Tuesday",
            SubjectId = subject2.Id,
            Subject = subject2,
            ClassroomId = classroom.Id,
            Classroom = classroom,
            SectionId = section.Id,
            Section = section,
            InstructorId = instructor.Id,
            Instructor = instructor,
        };

        _context.Users.Add(user);
        _context.Courses.Add(course);
        _context.Sections.Add(section);
        _context.Subjects.AddRange(subject1, subject2);
        _context.Classrooms.Add(classroom);
        _context.Instructors.Add(instructor);
        _context.Schedules.AddRange(schedule1, schedule2);

        await _context.SaveChangesAsync();
    }

    private async Task SeedTestDataWithDeletedStudentsAsync(Guid instructorId)
    {
        var user = new IdentityUser
        {
            Id = $"user-{instructorId}",
            UserName = $"instructor-{instructorId}@example.com",
            NormalizedUserName = $"INSTRUCTOR-{instructorId}@EXAMPLE.COM".ToUpper(),
            Email = $"instructor-{instructorId}@example.com",
            NormalizedEmail = $"INSTRUCTOR-{instructorId}@EXAMPLE.COM".ToUpper(),
        };

        var course = new Course
        {
            Id = Guid.NewGuid(),
            Name = "Bachelor of Science in Information Technology"
        };

        var section = new Section
        {
            Id = Guid.NewGuid(),
            Name = "BSIT 2A",
            CourseId = course.Id,
            Course = course
        };

        var subject = new Subject
        {
            Id = Guid.NewGuid(),
            Name = "Database Systems",
            Code = "DB101"
        };

        var classroom = new Classroom
        {
            Id = Guid.NewGuid(),
            Name = "Room 202"
        };

        var instructor = new Instructor
        {
            Id = instructorId,
            Firstname = "Jane",
            Lastname = "Smith",
            UserId = user.Id,
            User = user,
        };

        var schedule = new Schedules
        {
            Id = Guid.NewGuid(),
            TimeIn = new TimeOnly(13, 0),
            TimeOut = new TimeOnly(15, 0),
            DayOfWeek = "Wednesday",
            SubjectId = subject.Id,
            Subject = subject,
            ClassroomId = classroom.Id,
            Classroom = classroom,
            SectionId = section.Id,
            Section = section,
            InstructorId = instructor.Id,
            Instructor = instructor,
        };

        // Create students - some deleted, some active
        var studentUser1 = new IdentityUser { Id = $"student-{instructorId}-1", UserName = $"s1-{instructorId}@example.com", NormalizedUserName = $"S1-{instructorId}@EXAMPLE.COM".ToUpper(), Email = $"s1-{instructorId}@example.com", NormalizedEmail = $"S1-{instructorId}@EXAMPLE.COM".ToUpper() };
        var studentUser2 = new IdentityUser { Id = $"student-{instructorId}-2", UserName = $"s2-{instructorId}@example.com", NormalizedUserName = $"S2-{instructorId}@EXAMPLE.COM".ToUpper(), Email = $"s2-{instructorId}@example.com", NormalizedEmail = $"S2-{instructorId}@EXAMPLE.COM".ToUpper() };
        var studentUser3 = new IdentityUser { Id = $"student-{instructorId}-3", UserName = $"s3-{instructorId}@example.com", NormalizedUserName = $"S3-{instructorId}@EXAMPLE.COM".ToUpper(), Email = $"s3-{instructorId}@example.com", NormalizedEmail = $"S3-{instructorId}@EXAMPLE.COM".ToUpper() };

        var activeStudent1 = new Student { Id = Guid.NewGuid(), Firstname = "Alice", Lastname = "Johnson", UserId = studentUser1.Id, User = studentUser1, SectionId = section.Id, Section = section, IsRegular = true, IsDeleted = false };
        var activeStudent2 = new Student { Id = Guid.NewGuid(), Firstname = "Bob", Lastname = "Williams", UserId = studentUser2.Id, User = studentUser2, SectionId = section.Id, Section = section, IsRegular = false, IsDeleted = false };
        var deletedStudent = new Student { Id = Guid.NewGuid(), Firstname = "Charlie", Lastname = "Brown", UserId = studentUser3.Id, User = studentUser3, SectionId = section.Id, Section = section, IsRegular = true, IsDeleted = true, DeletedAt = DateTime.UtcNow.AddDays(-1) };

        var activeEnrollment1 = new StudentEnrollment { Id = Guid.NewGuid(), StudentId = activeStudent1.Id, Student = activeStudent1, SectionId = section.Id, Section = section, SubjectId = subject.Id, Subject = subject, IsActive = true, EnrollmentType = "Regular" };
        var activeEnrollment2 = new StudentEnrollment { Id = Guid.NewGuid(), StudentId = activeStudent2.Id, Student = activeStudent2, SectionId = section.Id, Section = section, SubjectId = subject.Id, Subject = subject, IsActive = true, EnrollmentType = "Irregular" };
        var inactiveEnrollment = new StudentEnrollment { Id = Guid.NewGuid(), StudentId = deletedStudent.Id, Student = deletedStudent, SectionId = section.Id, Section = section, SubjectId = subject.Id, Subject = subject, IsActive = false, EnrollmentType = "Regular" };

        _context.Users.AddRange(user, studentUser1, studentUser2, studentUser3);
        _context.Courses.Add(course);
        _context.Sections.Add(section);
        _context.Subjects.Add(subject);
        _context.Classrooms.Add(classroom);
        _context.Instructors.Add(instructor);
        _context.Schedules.Add(schedule);
        _context.Students.AddRange(activeStudent1, activeStudent2, deletedStudent);
        _context.StudentEnrollments.AddRange(activeEnrollment1, activeEnrollment2, inactiveEnrollment);

        await _context.SaveChangesAsync();
    }

    private async Task SeedTestDataWithIrregularStudentsAsync(Guid instructorId)
    {
        var user = new IdentityUser
        {
            Id = $"user-{instructorId}",
            UserName = $"instructor-{instructorId}@example.com",
            NormalizedUserName = $"INSTRUCTOR-{instructorId}@EXAMPLE.COM".ToUpper(),
            Email = $"instructor-{instructorId}@example.com",
            NormalizedEmail = $"INSTRUCTOR-{instructorId}@EXAMPLE.COM".ToUpper(),
        };

        var course = new Course { Id = Guid.NewGuid(), Name = "Bachelor of Science in Engineering" };
        var section = new Section { Id = Guid.NewGuid(), Name = "BSE 1A", CourseId = course.Id, Course = course };
        var subject = new Subject { Id = Guid.NewGuid(), Name = "Calculus", Code = "CALC101" };
        var classroom = new Classroom { Id = Guid.NewGuid(), Name = "Room 303" };
        var instructor = new Instructor { Id = instructorId, Firstname = "Robert", Lastname = "Davis", UserId = user.Id, User = user };

        var schedule = new Schedules
        {
            Id = Guid.NewGuid(),
            TimeIn = new TimeOnly(14, 0),
            TimeOut = new TimeOnly(16, 0),
            DayOfWeek = "Thursday",
            SubjectId = subject.Id,
            Subject = subject,
            ClassroomId = classroom.Id,
            Classroom = classroom,
            SectionId = section.Id,
            Section = section,
            InstructorId = instructor.Id,
            Instructor = instructor,
        };

        var studentUser1 = new IdentityUser { Id = $"student-{instructorId}-1", UserName = $"s1-{instructorId}@example.com", NormalizedUserName = $"S1-{instructorId}@EXAMPLE.COM".ToUpper(), Email = $"s1-{instructorId}@example.com", NormalizedEmail = $"S1-{instructorId}@EXAMPLE.COM".ToUpper() };
        var studentUser2 = new IdentityUser { Id = $"student-{instructorId}-2", UserName = $"s2-{instructorId}@example.com", NormalizedUserName = $"S2-{instructorId}@EXAMPLE.COM".ToUpper(), Email = $"s2-{instructorId}@example.com", NormalizedEmail = $"S2-{instructorId}@EXAMPLE.COM".ToUpper() };
        var studentUser3 = new IdentityUser { Id = $"student-{instructorId}-3", UserName = $"s3-{instructorId}@example.com", NormalizedUserName = $"S3-{instructorId}@EXAMPLE.COM".ToUpper(), Email = $"s3-{instructorId}@example.com", NormalizedEmail = $"S3-{instructorId}@EXAMPLE.COM".ToUpper() };

        var student1 = new Student { Id = Guid.NewGuid(), Firstname = "Emma", Lastname = "Wilson", UserId = studentUser1.Id, User = studentUser1, SectionId = section.Id, Section = section, IsRegular = false, IsDeleted = false };
        var student2 = new Student { Id = Guid.NewGuid(), Firstname = "Oliver", Lastname = "Martinez", UserId = studentUser2.Id, User = studentUser2, SectionId = section.Id, Section = section, IsRegular = false, IsDeleted = false };
        var student3 = new Student { Id = Guid.NewGuid(), Firstname = "Sophia", Lastname = "Garcia", UserId = studentUser3.Id, User = studentUser3, SectionId = section.Id, Section = section, IsRegular = true, IsDeleted = false };

        var enrollment1 = new StudentEnrollment { Id = Guid.NewGuid(), StudentId = student1.Id, Student = student1, SectionId = section.Id, Section = section, SubjectId = subject.Id, Subject = subject, IsActive = true, EnrollmentType = "Irregular" };
        var enrollment2 = new StudentEnrollment { Id = Guid.NewGuid(), StudentId = student2.Id, Student = student2, SectionId = section.Id, Section = section, SubjectId = subject.Id, Subject = subject, IsActive = true, EnrollmentType = "Retake" };
        var enrollment3 = new StudentEnrollment { Id = Guid.NewGuid(), StudentId = student3.Id, Student = student3, SectionId = section.Id, Section = section, SubjectId = subject.Id, Subject = subject, IsActive = true, EnrollmentType = "Regular" };

        _context.Users.AddRange(user, studentUser1, studentUser2, studentUser3);
        _context.Courses.Add(course);
        _context.Sections.Add(section);
        _context.Subjects.Add(subject);
        _context.Classrooms.Add(classroom);
        _context.Instructors.Add(instructor);
        _context.Schedules.Add(schedule);
        _context.Students.AddRange(student1, student2, student3);
        _context.StudentEnrollments.AddRange(enrollment1, enrollment2, enrollment3);

        await _context.SaveChangesAsync();
    }

    public void Dispose()
    {
        _context.Dispose();
    }
}
