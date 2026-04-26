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
            Id = Guid.NewGuid(),
            Name = "Mathematics",
            Code = "MATH101",
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        var classroom = new Classroom
        {
            Id = Guid.NewGuid(),
            Name = "Room 101",
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        var course = new Course
        {
            Id = Guid.NewGuid(),
            Name = "Computer Science",
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        var section = new Section
        {
            Id = Guid.NewGuid(),
            Name = "CS-3A",
            CourseId = Guid.NewGuid(),
            Course = course,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        var instructor = new Instructor
        {
            Id = Guid.NewGuid(),
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

    private Guid SubjectUuid => _context.Subjects.Single().Id;
    private Guid ClassroomUuid => _context.Classrooms.Single().Id;
    private Guid SectionUuid => _context.Sections.Single().Id;
    private Guid InstructorUuid => _context.Instructors.Single().Id;

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
            SubjectId = SubjectUuid,
            ClassroomId = ClassroomUuid,
            SectionId = SectionUuid,
            InstructorId = InstructorUuid
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
            SubjectId = SubjectUuid,
            ClassroomId = ClassroomUuid,
            SectionId = SectionUuid,
            InstructorId = InstructorUuid
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
            SubjectId = SubjectUuid,
            ClassroomId = ClassroomUuid,
            SectionId = SectionUuid,
            InstructorId = InstructorUuid
        };

        // Act & Assert
        await Assert.ThrowsAsync<ValidationException>(() => _service.CreateScheduleAsync(createSchedule));
    }

    [Fact]
    public async Task CreateScheduleAsync_SubjectNotFound_ThrowsEntityNotFoundException()
    {
        // Arrange
        var missingSubjectId = Guid.NewGuid();
        var createSchedule = new CreateSchedule
        {
            TimeIn = TimeOnly.FromTimeSpan(TimeSpan.FromHours(8)),
            TimeOut = TimeOnly.FromTimeSpan(TimeSpan.FromHours(10)),
            DayOfWeek = "Monday",
            SubjectId = missingSubjectId,
            ClassroomId = ClassroomUuid,
            SectionId = SectionUuid,
            InstructorId = InstructorUuid
        };

        // Act & Assert
        var exception = await Assert.ThrowsAsync<EntityNotFoundException<Guid>>(() => _service.CreateScheduleAsync(createSchedule));
        Assert.Equal("Subject", exception.EntityName);
        Assert.Equal(missingSubjectId, exception.Key);
    }

    [Fact]
    public async Task CreateScheduleAsync_ClassroomNotFound_ThrowsEntityNotFoundException()
    {
        // Arrange
        var missingClassroomId = Guid.NewGuid();
        var createSchedule = new CreateSchedule
        {
            TimeIn = TimeOnly.FromTimeSpan(TimeSpan.FromHours(8)),
            TimeOut = TimeOnly.FromTimeSpan(TimeSpan.FromHours(10)),
            DayOfWeek = "Monday",
            SubjectId = SubjectUuid,
            ClassroomId = missingClassroomId,
            SectionId = SectionUuid,
            InstructorId = InstructorUuid
        };

        // Act & Assert
        var exception = await Assert.ThrowsAsync<EntityNotFoundException<Guid>>(() => _service.CreateScheduleAsync(createSchedule));
        Assert.Equal("Classroom", exception.EntityName);
        Assert.Equal(missingClassroomId, exception.Key);
    }

    [Fact]
    public async Task CreateScheduleAsync_SectionNotFound_ThrowsEntityNotFoundException()
    {
        // Arrange
        var missingSectionId = Guid.NewGuid();
        var createSchedule = new CreateSchedule
        {
            TimeIn = TimeOnly.FromTimeSpan(TimeSpan.FromHours(8)),
            TimeOut = TimeOnly.FromTimeSpan(TimeSpan.FromHours(10)),
            DayOfWeek = "Monday",
            SubjectId = SubjectUuid,
            ClassroomId = ClassroomUuid,
            SectionId = missingSectionId,
            InstructorId = InstructorUuid
        };

        // Act & Assert
        var exception = await Assert.ThrowsAsync<EntityNotFoundException<Guid>>(() => _service.CreateScheduleAsync(createSchedule));
        Assert.Equal("Section", exception.EntityName);
        Assert.Equal(missingSectionId, exception.Key);
    }

    [Fact]
    public async Task CreateScheduleAsync_InstructorNotFound_ThrowsEntityNotFoundException()
    {
        // Arrange
        var missingInstructorId = Guid.NewGuid();
        var createSchedule = new CreateSchedule
        {
            TimeIn = TimeOnly.FromTimeSpan(TimeSpan.FromHours(8)),
            TimeOut = TimeOnly.FromTimeSpan(TimeSpan.FromHours(10)),
            DayOfWeek = "Monday",
            SubjectId = SubjectUuid,
            ClassroomId = ClassroomUuid,
            SectionId = SectionUuid,
            InstructorId = missingInstructorId
        };

        // Act & Assert
        var exception = await Assert.ThrowsAsync<EntityNotFoundException<Guid>>(() => _service.CreateScheduleAsync(createSchedule));
        Assert.Equal("Instructor", exception.EntityName);
        Assert.Equal(missingInstructorId, exception.Key);
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
            SubjectId = SubjectUuid,
            ClassroomId = ClassroomUuid,
            SectionId = SectionUuid,
            InstructorId = InstructorUuid
        };

        var expectedSchedule = new Schedules
        {
            Id = Guid.NewGuid(),
            TimeIn = createSchedule.TimeIn,
            TimeOut = createSchedule.TimeOut,
            DayOfWeek = createSchedule.DayOfWeek,
            SubjectId = Guid.NewGuid(),
            ClassroomId = Guid.NewGuid(),
            SectionId = Guid.NewGuid(),
            InstructorId = Guid.NewGuid(),
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
        Assert.NotEqual(Guid.Empty, result.SubjectId);
        Assert.NotEqual(Guid.Empty, result.ClassroomId);
        Assert.NotEqual(Guid.Empty, result.SectionId);
        Assert.NotEqual(Guid.Empty, result.InstructorId);
        _mockScheduleRepository.Verify(r => r.AddScheduleAsync(It.IsAny<Schedules>()), Times.Once);
    }

    [Fact]
    public async Task CreateScheduleAsync_CanonicalGuidIds_ResolvesToGuidIdentifiers()
    {
        var createSchedule = new CreateSchedule
        {
            TimeIn = TimeOnly.FromTimeSpan(TimeSpan.FromHours(8)),
            TimeOut = TimeOnly.FromTimeSpan(TimeSpan.FromHours(10)),
            DayOfWeek = "Monday",
            SubjectId = SubjectUuid,
            ClassroomId = ClassroomUuid,
            SectionId = SectionUuid,
            InstructorId = InstructorUuid,
        };

        _mockScheduleRepository
            .Setup(r => r.AddScheduleAsync(It.IsAny<Schedules>()))
            .ReturnsAsync((Schedules schedule) => schedule);

        var result = await _service.CreateScheduleAsync(createSchedule);

        Assert.NotEqual(Guid.Empty, result.SubjectId);
        Assert.NotEqual(Guid.Empty, result.ClassroomId);
        Assert.NotEqual(Guid.Empty, result.SectionId);
        Assert.NotEqual(Guid.Empty, result.InstructorId);
    }

    [Fact]
    public async Task GetScheduleByUuidAsync_ReturnsMappedSchedule_WhenFound()
    {
        var scheduleUuid = Guid.NewGuid();
        var schedule = new Schedules
        {
            Id = scheduleUuid,
            TimeIn = TimeOnly.FromTimeSpan(TimeSpan.FromHours(8)),
            TimeOut = TimeOnly.FromTimeSpan(TimeSpan.FromHours(10)),
            DayOfWeek = "Monday",
            SubjectId = Guid.NewGuid(),
            Subject = new Subject { Id = Guid.NewGuid(), Name = "Mathematics", Code = "MATH101" },
            ClassroomId = Guid.NewGuid(),
            Classroom = new Classroom { Id = Guid.NewGuid(), Name = "Room 101" },
            SectionId = Guid.NewGuid(),
            Section = new Section { Id = Guid.NewGuid(), Name = "CS-3A", CourseId = Guid.NewGuid() },
            InstructorId = Guid.NewGuid(),
            Instructor = new Instructor { Id = Guid.NewGuid(), Firstname = "Ada", Lastname = "Lovelace", UserId = "instructor-1" },
        };

        _mockScheduleRepository.Setup(r => r.GetScheduleByUuidAsync(scheduleUuid)).ReturnsAsync(schedule);

        var result = await _service.GetScheduleByUuidAsync(scheduleUuid);

        Assert.Equal(schedule.Id, result.Id);
        Assert.Equal(schedule.DayOfWeek, result.DayOfWeek);
    }

    #endregion

    #region UpdateScheduleAsync Tests

    [Fact]
    public async Task UpdateScheduleAsync_ScheduleNotFound_ThrowsEntityNotFoundException()
    {
        // Arrange
        var scheduleId = Guid.NewGuid();
        var updateSchedule = new UpdateSchedule
        {
            DayOfWeek = "Tuesday"
        };

        _mockScheduleRepository.Setup(r => r.GetScheduleByIdAsync(scheduleId)).ReturnsAsync((Schedules?)null);

        // Act & Assert
        var exception = await Assert.ThrowsAsync<EntityNotFoundException<Guid>>(() => _service.UpdateScheduleAsync(scheduleId, updateSchedule));
        Assert.Equal("Schedule", exception.EntityName);
        Assert.Equal(scheduleId, exception.Key);
    }

    [Fact]
    public async Task UpdateScheduleAsync_InvalidDayOfWeek_ThrowsValidationException()
    {
        // Arrange
        var scheduleId = Guid.NewGuid();
        var existingSchedule = new Schedules
        {
            Id = scheduleId,
            TimeIn = TimeOnly.FromTimeSpan(TimeSpan.FromHours(8)),
            TimeOut = TimeOnly.FromTimeSpan(TimeSpan.FromHours(10)),
            DayOfWeek = "Monday",
            SubjectId = Guid.NewGuid(),
            ClassroomId = Guid.NewGuid(),
            SectionId = Guid.NewGuid(),
            InstructorId = Guid.NewGuid(),
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
        var scheduleId = Guid.NewGuid();
        var existingSchedule = new Schedules
        {
            Id = scheduleId,
            TimeIn = TimeOnly.FromTimeSpan(TimeSpan.FromHours(8)),
            TimeOut = TimeOnly.FromTimeSpan(TimeSpan.FromHours(10)),
            DayOfWeek = "Monday",
            SubjectId = Guid.NewGuid(),
            ClassroomId = Guid.NewGuid(),
            SectionId = Guid.NewGuid(),
            InstructorId = Guid.NewGuid(),
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
        var scheduleId = Guid.NewGuid();
        var existingSchedule = new Schedules
        {
            Id = scheduleId,
            TimeIn = TimeOnly.FromTimeSpan(TimeSpan.FromHours(8)),
            TimeOut = TimeOnly.FromTimeSpan(TimeSpan.FromHours(10)),
            DayOfWeek = "Monday",
            SubjectId = Guid.NewGuid(),
            ClassroomId = Guid.NewGuid(),
            SectionId = Guid.NewGuid(),
            InstructorId = Guid.NewGuid(),
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        var missingSubjectId = Guid.NewGuid();
        var updateSchedule = new UpdateSchedule { SubjectId = missingSubjectId };

        _mockScheduleRepository.Setup(r => r.GetScheduleByIdAsync(scheduleId)).ReturnsAsync(existingSchedule);

        // Act & Assert
        var exception = await Assert.ThrowsAsync<EntityNotFoundException<Guid>>(() => _service.UpdateScheduleAsync(scheduleId, updateSchedule));
        Assert.Equal("Subject", exception.EntityName);
        Assert.Equal(missingSubjectId, exception.Key);
    }

    [Fact]
    public async Task UpdateScheduleAsync_ChangedClassroomIdNotFound_ThrowsEntityNotFoundException()
    {
        // Arrange
        var scheduleId = Guid.NewGuid();
        var existingSchedule = new Schedules
        {
            Id = scheduleId,
            TimeIn = TimeOnly.FromTimeSpan(TimeSpan.FromHours(8)),
            TimeOut = TimeOnly.FromTimeSpan(TimeSpan.FromHours(10)),
            DayOfWeek = "Monday",
            SubjectId = Guid.NewGuid(),
            ClassroomId = Guid.NewGuid(),
            SectionId = Guid.NewGuid(),
            InstructorId = Guid.NewGuid(),
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        var missingClassroomId = Guid.NewGuid();
        var updateSchedule = new UpdateSchedule { ClassroomId = missingClassroomId };

        _mockScheduleRepository.Setup(r => r.GetScheduleByIdAsync(scheduleId)).ReturnsAsync(existingSchedule);

        // Act & Assert
        var exception = await Assert.ThrowsAsync<EntityNotFoundException<Guid>>(() => _service.UpdateScheduleAsync(scheduleId, updateSchedule));
        Assert.Equal("Classroom", exception.EntityName);
        Assert.Equal(missingClassroomId, exception.Key);
    }

    [Fact]
    public async Task UpdateScheduleAsync_ChangedSectionIdNotFound_ThrowsEntityNotFoundException()
    {
        // Arrange
        var scheduleId = Guid.NewGuid();
        var existingSchedule = new Schedules
        {
            Id = scheduleId,
            TimeIn = TimeOnly.FromTimeSpan(TimeSpan.FromHours(8)),
            TimeOut = TimeOnly.FromTimeSpan(TimeSpan.FromHours(10)),
            DayOfWeek = "Monday",
            SubjectId = Guid.NewGuid(),
            ClassroomId = Guid.NewGuid(),
            SectionId = Guid.NewGuid(),
            InstructorId = Guid.NewGuid(),
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        var missingSectionId = Guid.NewGuid();
        var updateSchedule = new UpdateSchedule { SectionId = missingSectionId };

        _mockScheduleRepository.Setup(r => r.GetScheduleByIdAsync(scheduleId)).ReturnsAsync(existingSchedule);

        // Act & Assert
        var exception = await Assert.ThrowsAsync<EntityNotFoundException<Guid>>(() => _service.UpdateScheduleAsync(scheduleId, updateSchedule));
        Assert.Equal("Section", exception.EntityName);
        Assert.Equal(missingSectionId, exception.Key);
    }

    [Fact]
    public async Task UpdateScheduleAsync_ChangedInstructorIdNotFound_ThrowsEntityNotFoundException()
    {
        // Arrange
        var scheduleId = Guid.NewGuid();
        var existingSchedule = new Schedules
        {
            Id = scheduleId,
            TimeIn = TimeOnly.FromTimeSpan(TimeSpan.FromHours(8)),
            TimeOut = TimeOnly.FromTimeSpan(TimeSpan.FromHours(10)),
            DayOfWeek = "Monday",
            SubjectId = Guid.NewGuid(),
            ClassroomId = Guid.NewGuid(),
            SectionId = Guid.NewGuid(),
            InstructorId = Guid.NewGuid(),
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        var missingInstructorId = Guid.NewGuid();
        var updateSchedule = new UpdateSchedule { InstructorId = missingInstructorId };

        _mockScheduleRepository.Setup(r => r.GetScheduleByIdAsync(scheduleId)).ReturnsAsync(existingSchedule);

        // Act & Assert
        var exception = await Assert.ThrowsAsync<EntityNotFoundException<Guid>>(() => _service.UpdateScheduleAsync(scheduleId, updateSchedule));
        Assert.Equal("Instructor", exception.EntityName);
        Assert.Equal(missingInstructorId, exception.Key);
    }

    [Fact]
    public async Task UpdateScheduleAsync_ValidPartialUpdate_OnlyProvidedFieldsChange()
    {
        // Arrange
        var scheduleId = Guid.NewGuid();
        var existingSchedule = new Schedules
        {
            Id = scheduleId,
            TimeIn = TimeOnly.FromTimeSpan(TimeSpan.FromHours(8)),
            TimeOut = TimeOnly.FromTimeSpan(TimeSpan.FromHours(10)),
            DayOfWeek = "Monday",
            SubjectId = Guid.NewGuid(),
            ClassroomId = Guid.NewGuid(),
            SectionId = Guid.NewGuid(),
            InstructorId = Guid.NewGuid(),
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
            SubjectId = Guid.NewGuid(),
            ClassroomId = Guid.NewGuid(),
            SectionId = Guid.NewGuid(),
            InstructorId = Guid.NewGuid(),
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
        Assert.NotEqual(Guid.Empty, result.SubjectId);
        Assert.NotEqual(Guid.Empty, result.ClassroomId);
        Assert.NotEqual(Guid.Empty, result.SectionId);
        Assert.NotEqual(Guid.Empty, result.InstructorId);
        _mockScheduleRepository.Verify(r => r.UpdateScheduleAsync(It.IsAny<Schedules>()), Times.Once);
    }

    [Fact]
    public async Task UpdateScheduleAsync_RepositoryUpdateReturnsNull_ThrowsEntityServiceException()
    {
        // Arrange
        var scheduleId = Guid.NewGuid();
        var existingSchedule = new Schedules
        {
            Id = scheduleId,
            TimeIn = TimeOnly.FromTimeSpan(TimeSpan.FromHours(8)),
            TimeOut = TimeOnly.FromTimeSpan(TimeSpan.FromHours(10)),
            DayOfWeek = "Monday",
            SubjectId = Guid.NewGuid(),
            ClassroomId = Guid.NewGuid(),
            SectionId = Guid.NewGuid(),
            InstructorId = Guid.NewGuid(),
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
