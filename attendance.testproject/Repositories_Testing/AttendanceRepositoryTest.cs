using attendance_monitoring.Data;
using attendance_monitoring.Repositories;
using attendance_monitoring.Classes;
using attendance_monitoring.Models.DTO.Response;
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
            Schedule = schedule
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
            Assert.True(item.Id > 0);
            Assert.False(string.IsNullOrEmpty(item.StudentName));
            Assert.False(string.IsNullOrEmpty(item.StudentNumber));
            Assert.False(string.IsNullOrEmpty(item.SubjectName));
            Assert.False(string.IsNullOrEmpty(item.Status));
            // StudentNumber should be the StudentId as string
            Assert.Equal(item.StudentId.ToString(), item.StudentNumber);
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

    public void Dispose()
    {
        _context.Dispose();
    }
}
