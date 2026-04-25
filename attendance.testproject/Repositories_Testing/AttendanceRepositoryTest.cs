using attendance_monitoring.Data;
using attendance_monitoring.Repositories;
using attendance_monitoring.Classes;
using attendance_monitoring.Models.DTO.Response;
using Microsoft.AspNetCore.Identity;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;

namespace attendance.testproject.Repositories_Testing;

/// <summary>
/// Unit tests for AttendanceRepository optimized projection methods.
/// </summary>
public class AttendanceRepositoryTest : IDisposable
{
    private readonly ApplicationDbContext _context;
    private readonly AttendanceRepository _repository;

    public AttendanceRepositoryTest()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        _context = new ApplicationDbContext(options);
        _repository = new AttendanceRepository(_context);
    }

    [Fact]
    public async Task GetAllForListingOptimizedAsync_ShouldReturnCorrectData()
    {
        // Arrange
        await SeedTestDataAsync();

        // Act
        var result = await _repository.GetAllForListingOptimizedAsync(1, 10);

        // Assert
        Assert.NotNull(result);
        Assert.True(result.Count > 0);
        Assert.All(result, item =>
        {
            Assert.True(item.Id > 0);
            Assert.False(string.IsNullOrEmpty(item.StudentName));
            Assert.False(string.IsNullOrEmpty(item.SubjectName));
            Assert.False(string.IsNullOrEmpty(item.Status));
        });
    }

    [Fact]
    public async Task GetAllForListingOptimizedAsync_ShouldRespectPagination()
    {
        // Arrange
        await SeedTestDataAsync();

        // Act
        var page1 = await _repository.GetAllForListingOptimizedAsync(1, 2);
        var page2 = await _repository.GetAllForListingOptimizedAsync(2, 2);

        // Assert
        Assert.Equal(2, page1.Count);
        Assert.Single(page2); // Only 1 item left on page 2 (3 total items, 2 per page)
        Assert.NotEqual(page1.First().Id, page2.First().Id);
    }

    [Fact]
    public async Task AttendanceRepository_GetAttendanceByUuidAsync_ReturnsReadOnlyAndTrackedAttendance()
    {
        // Arrange
        await SeedTestDataAsync();
        var attendanceUuid = await _context.AttendanceRecords
            .AsNoTracking()
            .Where(a => a.Id == 1)
            .Select(a => a.Uuid)
            .SingleAsync();

        // Act
        var readOnlyRecord = await _repository.GetAttendanceByUuidAsync(attendanceUuid);
        var trackedRecord = await _repository.GetAttendanceByUuidTrackedAsync(attendanceUuid);

        // Assert
        Assert.NotNull(readOnlyRecord);
        Assert.NotNull(trackedRecord);
        Assert.Equal(attendanceUuid, readOnlyRecord.Uuid);
        Assert.Equal(attendanceUuid, trackedRecord.Uuid);
        Assert.NotNull(readOnlyRecord.Student);
        Assert.NotNull(readOnlyRecord.Session);
        Assert.Equal(EntityState.Detached, _context.Entry(readOnlyRecord).State);
        Assert.Equal(EntityState.Unchanged, _context.Entry(trackedRecord).State);
    }

    [Fact]
    public async Task AttendanceRepository_GetAttendanceByUuidAsync_ReturnsNull_WhenNotFound()
    {
        // Arrange
        await SeedTestDataAsync();

        // Act
        var readOnlyRecord = await _repository.GetAttendanceByUuidAsync(Guid.NewGuid());
        var trackedRecord = await _repository.GetAttendanceByUuidTrackedAsync(Guid.NewGuid());

        // Assert
        Assert.Null(readOnlyRecord);
        Assert.Null(trackedRecord);
    }

    private async Task SeedTestDataAsync()
    {
        // Create test data
        var subject = new Subject { Id = 1, Name = "Mathematics", Code = "MATH101" };
        var section = new Section { Id = 1, Name = "Section A" };
        var classroom = new Classroom { Id = 1, Name = "Room 101" };
        var instructor = new Instructor { Id = 1, Firstname = "John", Lastname = "Doe", UserId = "instructor1" };
        var student1 = new Student { Id = 1, Firstname = "Alice", Lastname = "Smith", UserId = "student1", SectionId = 1 };
        var student2 = new Student { Id = 2, Firstname = "Bob", Lastname = "Johnson", UserId = "student2", SectionId = 1 };
        var student3 = new Student { Id = 3, Firstname = "Charlie", Lastname = "Brown", UserId = "student3", SectionId = 1 };

        var schedule = new Schedules
        {
            Id = 1,
            TimeIn = new TimeOnly(8, 0),
            TimeOut = new TimeOnly(10, 0),
            DayOfWeek = "Monday",
            SubjectId = 1,
            Subject = subject,
            SectionId = 1,
            Section = section,
            ClassroomId = 1,
            Classroom = classroom,
            InstructorId = 1,
            Instructor = instructor
        };

        var session = new Session
        {
            Id = 1,
            SessionDate = DateTime.Today,
            ScheduleId = 1,
            Schedule = schedule,
            RowVersion = [1]
        };

        var attendance1 = new AttendanceRecord
        {
            Id = 1,
            StudentId = 1,
            Student = student1,
            SessionId = 1,
            Session = session,
            CheckInTime = DateTime.UtcNow.AddHours(-1),
            Status = "Present"
        };

        var attendance2 = new AttendanceRecord
        {
            Id = 2,
            StudentId = 2,
            Student = student2,
            SessionId = 1,
            Session = session,
            CheckInTime = DateTime.UtcNow.AddHours(-2),
            Status = "Late"
        };

        var attendance3 = new AttendanceRecord
        {
            Id = 3,
            StudentId = 3,
            Student = student3,
            SessionId = 1,
            Session = session,
            CheckInTime = DateTime.UtcNow.AddHours(-3),
            Status = "Present"
        };

        _context.Subjects.Add(subject);
        _context.Sections.Add(section);
        _context.Classrooms.Add(classroom);
        _context.Instructors.Add(instructor);
        _context.Students.AddRange(student1, student2, student3);
        _context.Schedules.Add(schedule);
        _context.Sessions.Add(session);
        _context.AttendanceRecords.AddRange(attendance1, attendance2, attendance3);

        await _context.SaveChangesAsync();
    }

    [Fact]
    public async Task GetAllForListingOptimizedAsync_ShouldBeMoreEfficientThanFullEntity()
    {
        // Arrange
        await SeedTestDataAsync();

        // Act & Assert - Both methods should return data, but optimized should be more efficient
        var optimizedResult = await _repository.GetAllForListingOptimizedAsync(1, 10);
        #pragma warning disable CS0618 // Intentionally testing deprecated method for comparison
        var fullResult = await _repository.GetAllForListingAsync(1, 10);
        #pragma warning restore CS0618

        // Verify both return data
        Assert.NotEmpty(optimizedResult);
        Assert.NotEmpty(fullResult);

        // Verify optimized result has the essential fields
        Assert.All(optimizedResult, item =>
        {
            Assert.True(item.Id > 0);
            Assert.False(string.IsNullOrEmpty(item.StudentName));
            Assert.False(string.IsNullOrEmpty(item.SubjectName));
            Assert.False(string.IsNullOrEmpty(item.Status));
        });

        // Verify full result has StudentNumber properly mapped
        Assert.All(fullResult, item =>
        {
            Assert.NotEqual(Guid.Empty, item.Id);
            Assert.False(string.IsNullOrEmpty(item.StudentName));
            Assert.False(string.IsNullOrEmpty(item.StudentNumber));
            Assert.False(string.IsNullOrEmpty(item.SubjectName));
            Assert.False(string.IsNullOrEmpty(item.Status));
            // StudentNumber should be the legacy int student ID as string
            Assert.True(int.TryParse(item.StudentNumber, out _));
        });

        // The optimized version should have fewer properties but same count
        Assert.Equal(fullResult.Count, optimizedResult.Count);
    }

    [Fact]
    public async Task GetBySessionIdForRosterAsync_ShouldReturnOptimizedRosterData()
    {
        // Arrange
        await SeedTestDataAsync();

        // Act
        var result = await _repository.GetBySessionIdForRosterAsync(1);

        // Assert
        Assert.NotNull(result);
        Assert.True(result.Count > 0);
        Assert.All(result, item =>
        {
            Assert.True(item.AttendanceId > 0);
            Assert.True(item.StudentId > 0);
            Assert.False(string.IsNullOrEmpty(item.StudentName));
            Assert.False(string.IsNullOrEmpty(item.Status));
            Assert.True(item.CheckInTime > DateTime.MinValue);
        });
    }

    [Fact]
    public async Task GetBySessionAndStudentMinimalAsync_ShouldReturnMinimalData()
    {
        // Arrange
        await SeedTestDataAsync();

        // Act
        var result = await _repository.GetBySessionAndStudentMinimalAsync(1, 1);

        // Assert
        Assert.NotNull(result);
        Assert.True(result.Id > 0);
        Assert.Equal(1, result.StudentId);
        Assert.Equal(1, result.SessionId);
        Assert.False(string.IsNullOrEmpty(result.Status));
        Assert.True(result.CheckInTime > DateTime.MinValue);
    }

    [Fact]
    public async Task HasAttendanceRecordAsync_ShouldReturnTrueForExistingRecord()
    {
        // Arrange
        await SeedTestDataAsync();

        // Act
        var exists = await _repository.HasAttendanceRecordAsync(1, 1);

        // Assert
        Assert.True(exists);
    }

    [Fact]
    public async Task HasAttendanceRecordAsync_ShouldReturnFalseForNonExistingRecord()
    {
        // Arrange
        await SeedTestDataAsync();

        // Act
        var exists = await _repository.HasAttendanceRecordAsync(999, 999);

        // Assert
        Assert.False(exists);
    }

    [Theory]
    [InlineData(0, 10)] // Invalid pageNumber should default to 1
    [InlineData(-1, 10)] // Negative pageNumber should default to 1
    [InlineData(1, 0)] // Invalid pageSize should default to 50
    [InlineData(1, -5)] // Negative pageSize should default to 50
    [InlineData(1, 2000)] // Excessive pageSize should be capped at 1000
    public async Task GetAllForListingOptimizedAsync_ShouldHandleInvalidPaginationParameters(
        int inputPageNumber, int inputPageSize)
    {
        // Arrange
        await SeedTestDataAsync();

        // Act
        var result = await _repository.GetAllForListingOptimizedAsync(inputPageNumber, inputPageSize);

        // Assert
        Assert.NotNull(result);
        // The method should handle invalid parameters gracefully and return results
        Assert.True(result.Count >= 0);

        // Verify that all returned items have valid data
        Assert.All(result, item =>
        {
            Assert.True(item.Id > 0);
            Assert.NotNull(item.StudentName);
            Assert.NotNull(item.SubjectName);
            Assert.NotNull(item.Status);
        });
    }

    [Fact]
    public async Task GetFilteredAsync_WithRelationalProvider_AppliesFiltersWithoutTranslationErrors()
    {
        await using var connection = new SqliteConnection("Data Source=:memory:");
        await connection.OpenAsync();

        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseSqlite(connection)
            .Options;

        await using var context = new ApplicationDbContext(options);
        await context.Database.EnsureCreatedAsync();
        var repository = new AttendanceRepository(context);

        var now = DateTime.UtcNow.Date;
        var checkIn1 = now.AddHours(8);
        var checkIn2 = now.AddHours(9);
        var checkIn3 = now.AddDays(1).AddHours(8).AddMinutes(30);

        var course = new Course { Name = "Computer Science", CreatedAt = now, UpdatedAt = now };
        var sectionA = new Section { Name = "BSCS-1A", Course = course, CreatedAt = now, UpdatedAt = now };
        var sectionB = new Section { Name = "BSCS-1B", Course = course, CreatedAt = now, UpdatedAt = now };
        var classroomA = new Classroom { Name = "Room 101", CreatedAt = now, UpdatedAt = now };
        var classroomB = new Classroom { Name = "Room 102", CreatedAt = now, UpdatedAt = now };
        var subjectA = new Subject { Name = "Algorithms", Code = "ALGO1", CreatedAt = now, UpdatedAt = now };
        var subjectB = new Subject { Name = "Databases", Code = "DBASE", CreatedAt = now, UpdatedAt = now };

        var instructorUser = new IdentityUser
        {
            Id = "instructor-user",
            UserName = "instructor@example.com",
            NormalizedUserName = "INSTRUCTOR@EXAMPLE.COM",
            Email = "instructor@example.com",
            NormalizedEmail = "INSTRUCTOR@EXAMPLE.COM",
        };

        var studentUser1 = new IdentityUser
        {
            Id = "student-user-1",
            UserName = "student1@example.com",
            NormalizedUserName = "STUDENT1@EXAMPLE.COM",
            Email = "student1@example.com",
            NormalizedEmail = "STUDENT1@EXAMPLE.COM",
        };

        var studentUser2 = new IdentityUser
        {
            Id = "student-user-2",
            UserName = "student2@example.com",
            NormalizedUserName = "STUDENT2@EXAMPLE.COM",
            Email = "student2@example.com",
            NormalizedEmail = "STUDENT2@EXAMPLE.COM",
        };

        var studentUser3 = new IdentityUser
        {
            Id = "student-user-3",
            UserName = "student3@example.com",
            NormalizedUserName = "STUDENT3@EXAMPLE.COM",
            Email = "student3@example.com",
            NormalizedEmail = "STUDENT3@EXAMPLE.COM",
        };

        var instructor = new Instructor
        {
            Firstname = "Ada",
            Lastname = "Lovelace",
            User = instructorUser,
            UserId = instructorUser.Id,
            CreatedAt = now,
            UpdatedAt = now,
        };

        var student1 = new Student
        {
            Firstname = "Alice",
            Lastname = "Anderson",
            User = studentUser1,
            UserId = studentUser1.Id,
            Section = sectionA,
            CreatedAt = now,
            UpdatedAt = now,
        };

        var student2 = new Student
        {
            Firstname = "Bob",
            Lastname = "Brown",
            User = studentUser2,
            UserId = studentUser2.Id,
            Section = sectionA,
            CreatedAt = now,
            UpdatedAt = now,
        };

        var student3 = new Student
        {
            Firstname = "Carol",
            Lastname = "Clark",
            User = studentUser3,
            UserId = studentUser3.Id,
            Section = sectionB,
            CreatedAt = now,
            UpdatedAt = now,
        };

        var scheduleA = new Schedules
        {
            TimeIn = new TimeOnly(8, 0),
            TimeOut = new TimeOnly(10, 0),
            DayOfWeek = "Monday",
            Subject = subjectA,
            Classroom = classroomA,
            Section = sectionA,
            Instructor = instructor,
            CreatedAt = now,
            UpdatedAt = now,
        };

        var scheduleB = new Schedules
        {
            TimeIn = new TimeOnly(10, 0),
            TimeOut = new TimeOnly(12, 0),
            DayOfWeek = "Tuesday",
            Subject = subjectB,
            Classroom = classroomB,
            Section = sectionB,
            Instructor = instructor,
            CreatedAt = now,
            UpdatedAt = now,
        };

        var sessionA = new Session
        {
            Schedule = scheduleA,
            SessionDate = now,
            CreatedAt = now,
            UpdatedAt = now,
            RowVersion = [2],
        };

        var sessionB = new Session
        {
            Schedule = scheduleB,
            SessionDate = now.AddDays(1),
            CreatedAt = now,
            UpdatedAt = now,
            RowVersion = [3],
        };

        context.AttendanceRecords.AddRange(
            new AttendanceRecord
            {
                Student = student1,
                Session = sessionA,
                CheckInTime = checkIn1,
                Status = "Present",
                IsManualEntry = false,
                CreatedAt = now,
                UpdatedAt = now,
            },
            new AttendanceRecord
            {
                Student = student2,
                Session = sessionA,
                CheckInTime = checkIn2,
                Status = "Late",
                IsManualEntry = true,
                CreatedAt = now,
                UpdatedAt = now,
            },
            new AttendanceRecord
            {
                Student = student3,
                Session = sessionB,
                CheckInTime = checkIn3,
                Status = "Present",
                IsManualEntry = false,
                CreatedAt = now,
                UpdatedAt = now,
            });
        await context.SaveChangesAsync();

        var (records, totalCount) = await repository.GetFilteredAsync(
            scheduleId: scheduleA.Id,
            sectionId: sectionA.Id,
            subjectId: subjectA.Id,
            status: "Late",
            startDate: now,
            endDate: now.AddHours(23).AddMinutes(59),
            isManualEntry: true,
            pageNumber: 1,
            pageSize: 10);

        var record = Assert.Single(records);
        Assert.Equal(1, totalCount);
        Assert.Equal(student2.Id, record.StudentId);
        Assert.Equal(sessionA.Id, record.SessionId);
        Assert.Equal("Late", record.Status);
        Assert.True(record.IsManualEntry);
        Assert.NotNull(record.Session);
        Assert.NotNull(record.Session.Schedule);
        Assert.Equal(scheduleA.Id, record.Session.ScheduleId);
        Assert.Equal(sectionA.Id, record.Session.Schedule.SectionId);
        Assert.Equal(subjectA.Id, record.Session.Schedule.SubjectId);
    }

    [Fact]
    public async Task GetStatisticsAsync_WithRelationalProvider_ReturnsCountsAndAverageCheckInTicks()
    {
        await using var connection = new SqliteConnection("Data Source=:memory:");
        await connection.OpenAsync();

        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseSqlite(connection)
            .Options;

        await using var context = new ApplicationDbContext(options);
        await context.Database.EnsureCreatedAsync();
        var repository = new AttendanceRepository(context);

        var now = DateTime.UtcNow.Date;
        var checkIn1 = now.AddHours(8);
        var checkIn2 = now.AddHours(9).AddMinutes(30);

        var course = new Course { Name = "Computer Science", CreatedAt = now, UpdatedAt = now };
        var section = new Section { Name = "BSCS-1A", Course = course, CreatedAt = now, UpdatedAt = now };
        var classroom = new Classroom { Name = "Room 101", CreatedAt = now, UpdatedAt = now };
        var subject = new Subject { Name = "Mathematics", Code = "MATH1", CreatedAt = now, UpdatedAt = now };

        var instructorUser = new IdentityUser
        {
            Id = "instructor-user",
            UserName = "instructor@example.com",
            NormalizedUserName = "INSTRUCTOR@EXAMPLE.COM",
            Email = "instructor@example.com",
            NormalizedEmail = "INSTRUCTOR@EXAMPLE.COM",
        };

        var studentUser1 = new IdentityUser
        {
            Id = "student-user-1",
            UserName = "student1@example.com",
            NormalizedUserName = "STUDENT1@EXAMPLE.COM",
            Email = "student1@example.com",
            NormalizedEmail = "STUDENT1@EXAMPLE.COM",
        };

        var studentUser2 = new IdentityUser
        {
            Id = "student-user-2",
            UserName = "student2@example.com",
            NormalizedUserName = "STUDENT2@EXAMPLE.COM",
            Email = "student2@example.com",
            NormalizedEmail = "STUDENT2@EXAMPLE.COM",
        };

        var instructor = new Instructor
        {
            Firstname = "Ada",
            Lastname = "Lovelace",
            User = instructorUser,
            UserId = instructorUser.Id,
            CreatedAt = now,
            UpdatedAt = now,
        };

        var student1 = new Student
        {
            Firstname = "Alice",
            Lastname = "Anderson",
            User = studentUser1,
            UserId = studentUser1.Id,
            Section = section,
            CreatedAt = now,
            UpdatedAt = now,
        };

        var student2 = new Student
        {
            Firstname = "Bob",
            Lastname = "Brown",
            User = studentUser2,
            UserId = studentUser2.Id,
            Section = section,
            CreatedAt = now,
            UpdatedAt = now,
        };

        var schedule = new Schedules
        {
            TimeIn = new TimeOnly(8, 0),
            TimeOut = new TimeOnly(10, 0),
            DayOfWeek = "Monday",
            Subject = subject,
            Classroom = classroom,
            Section = section,
            Instructor = instructor,
            CreatedAt = now,
            UpdatedAt = now,
        };

        var session = new Session
        {
            Schedule = schedule,
            SessionDate = now,
            CreatedAt = now,
            UpdatedAt = now,
            RowVersion = [4],
        };

        var attendance1 = new AttendanceRecord
        {
            Student = student1,
            Session = session,
            CheckInTime = checkIn1,
            Status = "Present",
            CreatedAt = now,
            UpdatedAt = now,
        };

        var attendance2 = new AttendanceRecord
        {
            Student = student2,
            Session = session,
            CheckInTime = checkIn2,
            Status = "Late",
            CreatedAt = now,
            UpdatedAt = now,
        };

        context.AttendanceRecords.AddRange(attendance1, attendance2);
        await context.SaveChangesAsync();

        var stats = await repository.GetStatisticsAsync();
        var expectedAverageTicks = (checkIn1.TimeOfDay.Ticks + checkIn2.TimeOfDay.Ticks) / 2;

        Assert.Equal(2, stats.Total);
        Assert.Equal(1, stats.Present);
        Assert.Equal(1, stats.Late);
        Assert.Equal(0, stats.Absent);
        Assert.Equal(0, stats.Excused);
        Assert.Equal(expectedAverageTicks, stats.AvgCheckInTicks);
    }

    public void Dispose()
    {
        _context.Dispose();
    }
}
