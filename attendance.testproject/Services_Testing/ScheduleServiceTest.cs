using attendance_monitoring.Classes;
using attendance_monitoring.Data;
using attendance_monitoring.Exceptions;
using attendance_monitoring.IRepository;
using attendance_monitoring.IServices;
using attendance_monitoring.Models.DTO.Request;
using attendance_monitoring.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Moq;

namespace attendance.testproject.Services_Testing;

/// <summary>
/// Unit tests for ScheduleService
/// Tests CreateScheduleAsync and UpdateScheduleAsync with in-memory ApplicationDbContext
/// </summary>
public class ScheduleServiceTest : IDisposable
{
    private readonly ApplicationDbContext _context;
    private readonly Mock<IScheduleRepository> _mockScheduleRepository;
    private readonly Mock<IInstructorRepository> _mockInstructorRepository;
    private readonly Mock<IUserContextService> _mockUserContextService;
    private readonly Mock<IHttpContextAccessor> _mockHttpContextAccessor;
    private readonly Mock<ILogger<ScheduleService>> _mockLogger;
    private readonly ScheduleService _service;

    public ScheduleServiceTest()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        _context = new ApplicationDbContext(options);
        _mockScheduleRepository = new Mock<IScheduleRepository>();
        _mockInstructorRepository = new Mock<IInstructorRepository>();
        _mockUserContextService = new Mock<IUserContextService>();
        _mockHttpContextAccessor = new Mock<IHttpContextAccessor>();
        _mockLogger = new Mock<ILogger<ScheduleService>>();

        _service = new ScheduleService(
            _mockScheduleRepository.Object,
            _mockInstructorRepository.Object,
            _mockUserContextService.Object,
            _mockHttpContextAccessor.Object,
            _context,
            _mockLogger.Object
        );

        SeedTestData();
    }

    private void SeedTestData()
    {
        var subject = new Subject
        {
            Id = 1,
            Name = "Mathematics",
            Code = "MATH101",
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        var classroom = new Classroom
        {
            Id = 1,
            Name = "Room 101",
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        var course = new Course
        {
            Id = 1,
            Name = "Computer Science",
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        var section = new Section
        {
            Id = 1,
            Name = "CS-3A",
            CourseId = 1,
            Course = course,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        var instructor = new Instructor
        {
            Id = 1,
            Firstname = "John",
            Lastname = "Doe",
            UserId = "instructor-123",
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        _context.Subjects.Add(subject);
        _context.Classrooms.Add(classroom);
        _context.Courses.Add(course);
        _context.Sections.Add(section);
        _context.Instructors.Add(instructor);
        _context.SaveChanges();
    }

    public void Dispose()
    {
        _context.Database.EnsureDeleted();
        _context.Dispose();
    }

    #region CreateScheduleAsync Tests

    [Fact]
    public async Task CreateScheduleAsync_InvalidDayOfWeek_ThrowsValidationException()
    {
        // Arrange
        var createSchedule = new CreateSchedule
        {
            TimeIn = TimeOnly.FromTimeSpan(TimeSpan.FromHours(8)),
            TimeOut = TimeOnly.FromTimeSpan(TimeSpan.FromHours(10)),
            DayOfWeek = "InvalidDay",
            SubjectId = 1,
            ClassroomId = 1,
            SectionId = 1,
            InstructorId = 1
        };

        // Act & Assert
        await Assert.ThrowsAsync<ValidationException>(() => _service.CreateScheduleAsync(createSchedule));
    }

    [Fact]
    public async Task CreateScheduleAsync_TimeOutNotAfterTimeIn_ThrowsValidationException()
    {
        // Arrange
        var createSchedule = new CreateSchedule
        {
            TimeIn = TimeOnly.FromTimeSpan(TimeSpan.FromHours(10)),
            TimeOut = TimeOnly.FromTimeSpan(TimeSpan.FromHours(8)),
            DayOfWeek = "Monday",
            SubjectId = 1,
            ClassroomId = 1,
            SectionId = 1,
            InstructorId = 1
        };

        // Act & Assert
        await Assert.ThrowsAsync<ValidationException>(() => _service.CreateScheduleAsync(createSchedule));
    }

    [Fact]
    public async Task CreateScheduleAsync_TimeOutEqualsTimeIn_ThrowsValidationException()
    {
        // Arrange
        var createSchedule = new CreateSchedule
        {
            TimeIn = TimeOnly.FromTimeSpan(TimeSpan.FromHours(8)),
            TimeOut = TimeOnly.FromTimeSpan(TimeSpan.FromHours(8)),
            DayOfWeek = "Monday",
            SubjectId = 1,
            ClassroomId = 1,
            SectionId = 1,
            InstructorId = 1
        };

        // Act & Assert
        await Assert.ThrowsAsync<ValidationException>(() => _service.CreateScheduleAsync(createSchedule));
    }

    [Fact]
    public async Task CreateScheduleAsync_SubjectNotFound_ThrowsEntityNotFoundException()
    {
        // Arrange
        var createSchedule = new CreateSchedule
        {
            TimeIn = TimeOnly.FromTimeSpan(TimeSpan.FromHours(8)),
            TimeOut = TimeOnly.FromTimeSpan(TimeSpan.FromHours(10)),
            DayOfWeek = "Monday",
            SubjectId = 999,
            ClassroomId = 1,
            SectionId = 1,
            InstructorId = 1
        };

        // Act & Assert
        var exception = await Assert.ThrowsAsync<EntityNotFoundException<int>>(() => _service.CreateScheduleAsync(createSchedule));
        Assert.Equal("Subject", exception.EntityName);
        Assert.Equal(999, exception.Key);
    }

    [Fact]
    public async Task CreateScheduleAsync_ClassroomNotFound_ThrowsEntityNotFoundException()
    {
        // Arrange
        var createSchedule = new CreateSchedule
        {
            TimeIn = TimeOnly.FromTimeSpan(TimeSpan.FromHours(8)),
            TimeOut = TimeOnly.FromTimeSpan(TimeSpan.FromHours(10)),
            DayOfWeek = "Monday",
            SubjectId = 1,
            ClassroomId = 999,
            SectionId = 1,
            InstructorId = 1
        };

        // Act & Assert
        var exception = await Assert.ThrowsAsync<EntityNotFoundException<int>>(() => _service.CreateScheduleAsync(createSchedule));
        Assert.Equal("Classroom", exception.EntityName);
        Assert.Equal(999, exception.Key);
    }

    [Fact]
    public async Task CreateScheduleAsync_SectionNotFound_ThrowsEntityNotFoundException()
    {
        // Arrange
        var createSchedule = new CreateSchedule
        {
            TimeIn = TimeOnly.FromTimeSpan(TimeSpan.FromHours(8)),
            TimeOut = TimeOnly.FromTimeSpan(TimeSpan.FromHours(10)),
            DayOfWeek = "Monday",
            SubjectId = 1,
            ClassroomId = 1,
            SectionId = 999,
            InstructorId = 1
        };

        // Act & Assert
        var exception = await Assert.ThrowsAsync<EntityNotFoundException<int>>(() => _service.CreateScheduleAsync(createSchedule));
        Assert.Equal("Section", exception.EntityName);
        Assert.Equal(999, exception.Key);
    }

    [Fact]
    public async Task CreateScheduleAsync_InstructorNotFound_ThrowsEntityNotFoundException()
    {
        // Arrange
        var createSchedule = new CreateSchedule
        {
            TimeIn = TimeOnly.FromTimeSpan(TimeSpan.FromHours(8)),
            TimeOut = TimeOnly.FromTimeSpan(TimeSpan.FromHours(10)),
            DayOfWeek = "Monday",
            SubjectId = 1,
            ClassroomId = 1,
            SectionId = 1,
            InstructorId = 999
        };

        // Act & Assert
        var exception = await Assert.ThrowsAsync<EntityNotFoundException<int>>(() => _service.CreateScheduleAsync(createSchedule));
        Assert.Equal("Instructor", exception.EntityName);
        Assert.Equal(999, exception.Key);
    }

    [Fact]
    public async Task CreateScheduleAsync_ValidInput_ReturnsCreatedSchedule()
    {
        // Arrange
        var createSchedule = new CreateSchedule
        {
            TimeIn = TimeOnly.FromTimeSpan(TimeSpan.FromHours(8)),
            TimeOut = TimeOnly.FromTimeSpan(TimeSpan.FromHours(10)),
            DayOfWeek = "Monday",
            SubjectId = 1,
            ClassroomId = 1,
            SectionId = 1,
            InstructorId = 1
        };

        var expectedSchedule = new Schedules
        {
            Id = 1,
            TimeIn = createSchedule.TimeIn,
            TimeOut = createSchedule.TimeOut,
            DayOfWeek = createSchedule.DayOfWeek,
            SubjectId = createSchedule.SubjectId,
            ClassroomId = createSchedule.ClassroomId,
            SectionId = createSchedule.SectionId,
            InstructorId = createSchedule.InstructorId,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        _mockScheduleRepository.Setup(r => r.AddScheduleAsync(It.IsAny<Schedules>()))
            .ReturnsAsync(expectedSchedule);

        // Act
        var result = await _service.CreateScheduleAsync(createSchedule);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(createSchedule.TimeIn, result.TimeIn);
        Assert.Equal(createSchedule.TimeOut, result.TimeOut);
        Assert.Equal(createSchedule.DayOfWeek, result.DayOfWeek);
        Assert.Equal(createSchedule.SubjectId, result.SubjectId);
        Assert.Equal(createSchedule.ClassroomId, result.ClassroomId);
        Assert.Equal(createSchedule.SectionId, result.SectionId);
        Assert.Equal(createSchedule.InstructorId, result.InstructorId);
        _mockScheduleRepository.Verify(r => r.AddScheduleAsync(It.IsAny<Schedules>()), Times.Once);
    }

    #endregion

    #region UpdateScheduleAsync Tests

    [Fact]
    public async Task UpdateScheduleAsync_ScheduleNotFound_ThrowsEntityNotFoundException()
    {
        // Arrange
        const int scheduleId = 999;
        var updateSchedule = new UpdateSchedule
        {
            DayOfWeek = "Tuesday"
        };

        _mockScheduleRepository.Setup(r => r.GetScheduleByIdAsync(scheduleId)).ReturnsAsync((Schedules?)null);

        // Act & Assert
        var exception = await Assert.ThrowsAsync<EntityNotFoundException<int>>(() => _service.UpdateScheduleAsync(scheduleId, updateSchedule));
        Assert.Equal("Schedule", exception.EntityName);
        Assert.Equal(scheduleId, exception.Key);
    }

    [Fact]
    public async Task UpdateScheduleAsync_InvalidDayOfWeek_ThrowsValidationException()
    {
        // Arrange
        const int scheduleId = 1;
        var existingSchedule = new Schedules
        {
            Id = scheduleId,
            TimeIn = TimeOnly.FromTimeSpan(TimeSpan.FromHours(8)),
            TimeOut = TimeOnly.FromTimeSpan(TimeSpan.FromHours(10)),
            DayOfWeek = "Monday",
            SubjectId = 1,
            ClassroomId = 1,
            SectionId = 1,
            InstructorId = 1,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        var updateSchedule = new UpdateSchedule
        {
            DayOfWeek = "InvalidDay"
        };

        _mockScheduleRepository.Setup(r => r.GetScheduleByIdAsync(scheduleId)).ReturnsAsync(existingSchedule);

        // Act & Assert
        await Assert.ThrowsAsync<ValidationException>(() => _service.UpdateScheduleAsync(scheduleId, updateSchedule));
    }

    [Fact]
    public async Task UpdateScheduleAsync_PartialUpdateCausingInvalidTimeRange_ThrowsValidationException()
    {
        // Arrange
        const int scheduleId = 1;
        var existingSchedule = new Schedules
        {
            Id = scheduleId,
            TimeIn = TimeOnly.FromTimeSpan(TimeSpan.FromHours(8)),
            TimeOut = TimeOnly.FromTimeSpan(TimeSpan.FromHours(10)),
            DayOfWeek = "Monday",
            SubjectId = 1,
            ClassroomId = 1,
            SectionId = 1,
            InstructorId = 1,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        var updateSchedule = new UpdateSchedule
        {
            TimeOut = TimeOnly.FromTimeSpan(TimeSpan.FromHours(7))
        };

        _mockScheduleRepository.Setup(r => r.GetScheduleByIdAsync(scheduleId)).ReturnsAsync(existingSchedule);

        // Act & Assert
        await Assert.ThrowsAsync<ValidationException>(() => _service.UpdateScheduleAsync(scheduleId, updateSchedule));
    }

    [Fact]
    public async Task UpdateScheduleAsync_ChangedSubjectIdNotFound_ThrowsEntityNotFoundException()
    {
        // Arrange
        const int scheduleId = 1;
        var existingSchedule = new Schedules
        {
            Id = scheduleId,
            TimeIn = TimeOnly.FromTimeSpan(TimeSpan.FromHours(8)),
            TimeOut = TimeOnly.FromTimeSpan(TimeSpan.FromHours(10)),
            DayOfWeek = "Monday",
            SubjectId = 1,
            ClassroomId = 1,
            SectionId = 1,
            InstructorId = 1,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        var updateSchedule = new UpdateSchedule
        {
            SubjectId = 999
        };

        _mockScheduleRepository.Setup(r => r.GetScheduleByIdAsync(scheduleId)).ReturnsAsync(existingSchedule);

        // Act & Assert
        var exception = await Assert.ThrowsAsync<EntityNotFoundException<int>>(() => _service.UpdateScheduleAsync(scheduleId, updateSchedule));
        Assert.Equal("Subject", exception.EntityName);
        Assert.Equal(999, exception.Key);
    }

    [Fact]
    public async Task UpdateScheduleAsync_ChangedClassroomIdNotFound_ThrowsEntityNotFoundException()
    {
        // Arrange
        const int scheduleId = 1;
        var existingSchedule = new Schedules
        {
            Id = scheduleId,
            TimeIn = TimeOnly.FromTimeSpan(TimeSpan.FromHours(8)),
            TimeOut = TimeOnly.FromTimeSpan(TimeSpan.FromHours(10)),
            DayOfWeek = "Monday",
            SubjectId = 1,
            ClassroomId = 1,
            SectionId = 1,
            InstructorId = 1,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        var updateSchedule = new UpdateSchedule
        {
            ClassroomId = 999
        };

        _mockScheduleRepository.Setup(r => r.GetScheduleByIdAsync(scheduleId)).ReturnsAsync(existingSchedule);

        // Act & Assert
        var exception = await Assert.ThrowsAsync<EntityNotFoundException<int>>(() => _service.UpdateScheduleAsync(scheduleId, updateSchedule));
        Assert.Equal("Classroom", exception.EntityName);
        Assert.Equal(999, exception.Key);
    }

    [Fact]
    public async Task UpdateScheduleAsync_ChangedSectionIdNotFound_ThrowsEntityNotFoundException()
    {
        // Arrange
        const int scheduleId = 1;
        var existingSchedule = new Schedules
        {
            Id = scheduleId,
            TimeIn = TimeOnly.FromTimeSpan(TimeSpan.FromHours(8)),
            TimeOut = TimeOnly.FromTimeSpan(TimeSpan.FromHours(10)),
            DayOfWeek = "Monday",
            SubjectId = 1,
            ClassroomId = 1,
            SectionId = 1,
            InstructorId = 1,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        var updateSchedule = new UpdateSchedule
        {
            SectionId = 999
        };

        _mockScheduleRepository.Setup(r => r.GetScheduleByIdAsync(scheduleId)).ReturnsAsync(existingSchedule);

        // Act & Assert
        var exception = await Assert.ThrowsAsync<EntityNotFoundException<int>>(() => _service.UpdateScheduleAsync(scheduleId, updateSchedule));
        Assert.Equal("Section", exception.EntityName);
        Assert.Equal(999, exception.Key);
    }

    [Fact]
    public async Task UpdateScheduleAsync_ChangedInstructorIdNotFound_ThrowsEntityNotFoundException()
    {
        // Arrange
        const int scheduleId = 1;
        var existingSchedule = new Schedules
        {
            Id = scheduleId,
            TimeIn = TimeOnly.FromTimeSpan(TimeSpan.FromHours(8)),
            TimeOut = TimeOnly.FromTimeSpan(TimeSpan.FromHours(10)),
            DayOfWeek = "Monday",
            SubjectId = 1,
            ClassroomId = 1,
            SectionId = 1,
            InstructorId = 1,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        var updateSchedule = new UpdateSchedule
        {
            InstructorId = 999
        };

        _mockScheduleRepository.Setup(r => r.GetScheduleByIdAsync(scheduleId)).ReturnsAsync(existingSchedule);

        // Act & Assert
        var exception = await Assert.ThrowsAsync<EntityNotFoundException<int>>(() => _service.UpdateScheduleAsync(scheduleId, updateSchedule));
        Assert.Equal("Instructor", exception.EntityName);
        Assert.Equal(999, exception.Key);
    }

    [Fact]
    public async Task UpdateScheduleAsync_ValidPartialUpdate_OnlyProvidedFieldsChange()
    {
        // Arrange
        const int scheduleId = 1;
        var existingSchedule = new Schedules
        {
            Id = scheduleId,
            TimeIn = TimeOnly.FromTimeSpan(TimeSpan.FromHours(8)),
            TimeOut = TimeOnly.FromTimeSpan(TimeSpan.FromHours(10)),
            DayOfWeek = "Monday",
            SubjectId = 1,
            ClassroomId = 1,
            SectionId = 1,
            InstructorId = 1,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        var updateSchedule = new UpdateSchedule
        {
            DayOfWeek = "Tuesday"
        };

        var updatedSchedule = new Schedules
        {
            Id = scheduleId,
            TimeIn = TimeOnly.FromTimeSpan(TimeSpan.FromHours(8)),
            TimeOut = TimeOnly.FromTimeSpan(TimeSpan.FromHours(10)),
            DayOfWeek = "Tuesday",
            SubjectId = 1,
            ClassroomId = 1,
            SectionId = 1,
            InstructorId = 1,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        _mockScheduleRepository.Setup(r => r.GetScheduleByIdAsync(scheduleId)).ReturnsAsync(existingSchedule);
        _mockScheduleRepository.Setup(r => r.UpdateScheduleAsync(It.IsAny<Schedules>())).ReturnsAsync(updatedSchedule);

        // Act
        var result = await _service.UpdateScheduleAsync(scheduleId, updateSchedule);

        // Assert
        Assert.NotNull(result);
        Assert.Equal("Tuesday", result.DayOfWeek);
        Assert.Equal(TimeOnly.FromTimeSpan(TimeSpan.FromHours(8)), result.TimeIn);
        Assert.Equal(TimeOnly.FromTimeSpan(TimeSpan.FromHours(10)), result.TimeOut);
        Assert.Equal(1, result.SubjectId);
        Assert.Equal(1, result.ClassroomId);
        Assert.Equal(1, result.SectionId);
        Assert.Equal(1, result.InstructorId);
        _mockScheduleRepository.Verify(r => r.UpdateScheduleAsync(It.IsAny<Schedules>()), Times.Once);
    }

    [Fact]
    public async Task UpdateScheduleAsync_RepositoryUpdateReturnsNull_ThrowsEntityServiceException()
    {
        // Arrange
        const int scheduleId = 1;
        var existingSchedule = new Schedules
        {
            Id = scheduleId,
            TimeIn = TimeOnly.FromTimeSpan(TimeSpan.FromHours(8)),
            TimeOut = TimeOnly.FromTimeSpan(TimeSpan.FromHours(10)),
            DayOfWeek = "Monday",
            SubjectId = 1,
            ClassroomId = 1,
            SectionId = 1,
            InstructorId = 1,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        var updateSchedule = new UpdateSchedule
        {
            DayOfWeek = "Tuesday"
        };

        _mockScheduleRepository.Setup(r => r.GetScheduleByIdAsync(scheduleId)).ReturnsAsync(existingSchedule);
        _mockScheduleRepository.Setup(r => r.UpdateScheduleAsync(It.IsAny<Schedules>())).ReturnsAsync((Schedules?)null);

        // Act & Assert
        var exception = await Assert.ThrowsAsync<EntityServiceException>(() => _service.UpdateScheduleAsync(scheduleId, updateSchedule));
        Assert.Equal("Schedule", exception.EntityName);
        Assert.Contains("UpdateSchedule", exception.Operation);
    }

    #endregion
}
