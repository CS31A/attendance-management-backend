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
        _mockScheduleRepository
            .Setup(r => r.FindClassroomOverlapAsync(It.IsAny<Guid>(), It.IsAny<string>(), It.IsAny<TimeOnly>(), It.IsAny<TimeOnly>(), It.IsAny<Guid?>()))
            .ReturnsAsync((ScheduleConflictDetails?)null);
        _mockScheduleRepository
            .Setup(r => r.FindInstructorOverlapAsync(It.IsAny<Guid>(), It.IsAny<string>(), It.IsAny<TimeOnly>(), It.IsAny<TimeOnly>(), It.IsAny<Guid?>()))
            .ReturnsAsync((ScheduleConflictDetails?)null);
        _mockScheduleRepository
            .Setup(r => r.FindSectionOverlapAsync(It.IsAny<Guid>(), It.IsAny<string>(), It.IsAny<TimeOnly>(), It.IsAny<TimeOnly>(), It.IsAny<Guid?>()))
            .ReturnsAsync((ScheduleConflictDetails?)null);

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
    private const string ConflictMessage = "Schedule conflict: classroom Room 101 is already booked on Monday from 08:00 to 10:00.";

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

    [Theory]
    [InlineData("classroom")]
    [InlineData("instructor")]
    [InlineData("section")]
    public async Task CreateScheduleAsync_OverlappingResource_ThrowsEntityConflictException(string resourceType)
    {
        var createSchedule = CreateValidScheduleRequest();
        SetupCreateConflict(resourceType, CreateConflictDetails(resourceType));

        var exception = await Assert.ThrowsAsync<EntityConflictException>(() => _service.CreateScheduleAsync(createSchedule));

        Assert.Equal("Schedule", exception.EntityName);
        Assert.Equal(resourceType, exception.ConflictType);
        Assert.Contains(resourceType, exception.Message);
        Assert.Contains("Monday", exception.Message);
        Assert.Contains("08:00", exception.Message);
        Assert.Contains("10:00", exception.Message);
        _mockScheduleRepository.Verify(r => r.AddScheduleAsync(It.IsAny<Schedules>()), Times.Never);
    }

    [Theory]
    [InlineData("classroom")]
    [InlineData("instructor")]
    [InlineData("section")]
    public async Task CreateScheduleAsync_BackToBackResourceSchedule_AddsSchedule(string resourceType)
    {
        var createSchedule = CreateValidScheduleRequest();
        _mockScheduleRepository
            .Setup(r => r.AddScheduleAsync(It.IsAny<Schedules>()))
            .ReturnsAsync((Schedules schedule) => schedule);

        var result = await _service.CreateScheduleAsync(createSchedule);

        Assert.Equal(createSchedule.TimeIn, result.TimeIn);
        Assert.Equal(createSchedule.TimeOut, result.TimeOut);
        VerifyCreateLookup(resourceType, createSchedule.TimeIn, createSchedule.TimeOut);
        _mockScheduleRepository.Verify(r => r.AddScheduleAsync(It.IsAny<Schedules>()), Times.Once);
    }

    [Fact]
    public async Task CreateScheduleAsync_ExactDuplicateConstraint_ThrowsEntityConflictException()
    {
        var createSchedule = CreateValidScheduleRequest();
        _mockScheduleRepository
            .Setup(r => r.AddScheduleAsync(It.IsAny<Schedules>()))
            .ThrowsAsync(CreateScheduleUniqueConstraintException());

        var exception = await Assert.ThrowsAsync<EntityConflictException>(() => _service.CreateScheduleAsync(createSchedule));

        Assert.Equal("Schedule", exception.EntityName);
        Assert.Equal("duplicate", exception.ConflictType);
        Assert.Contains("Schedule conflict", exception.Message);
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

    [Theory]
    [InlineData("classroom")]
    [InlineData("instructor")]
    [InlineData("section")]
    public async Task UpdateScheduleAsync_OverlappingResource_ThrowsEntityConflictException_AndExcludesUpdatedSchedule(string resourceType)
    {
        var scheduleId = Guid.NewGuid();
        var existingSchedule = CreateExistingSchedule(scheduleId);
        var updateSchedule = new UpdateSchedule
        {
            TimeIn = TimeOnly.FromTimeSpan(TimeSpan.FromHours(9)),
            TimeOut = TimeOnly.FromTimeSpan(TimeSpan.FromHours(11)),
        };
        _mockScheduleRepository.Setup(r => r.GetScheduleByIdAsync(scheduleId)).ReturnsAsync(existingSchedule);
        SetupUpdateConflict(resourceType, existingSchedule, updateSchedule, CreateConflictDetails(resourceType));

        var exception = await Assert.ThrowsAsync<EntityConflictException>(() => _service.UpdateScheduleAsync(scheduleId, updateSchedule));

        Assert.Equal("Schedule", exception.EntityName);
        Assert.Equal(resourceType, exception.ConflictType);
        Assert.Contains(resourceType, exception.Message);
        VerifyUpdateLookup(resourceType, scheduleId, updateSchedule.TimeIn.Value, updateSchedule.TimeOut.Value);
        _mockScheduleRepository.Verify(r => r.UpdateScheduleAsync(It.IsAny<Schedules>()), Times.Never);
    }

    [Fact]
    public async Task UpdateScheduleAsync_UnchangedSchedule_DoesNotConflictWithItself()
    {
        var scheduleId = Guid.NewGuid();
        var existingSchedule = CreateExistingSchedule(scheduleId);
        _mockScheduleRepository.Setup(r => r.GetScheduleByIdAsync(scheduleId)).ReturnsAsync(existingSchedule);
        _mockScheduleRepository.Setup(r => r.UpdateScheduleAsync(It.IsAny<Schedules>())).ReturnsAsync((Schedules schedule) => schedule);

        var result = await _service.UpdateScheduleAsync(scheduleId, new UpdateSchedule { DayOfWeek = "Monday" });

        Assert.Equal(scheduleId, result.Id);
        VerifyUpdateLookup("classroom", scheduleId, existingSchedule.TimeIn, existingSchedule.TimeOut);
        VerifyUpdateLookup("instructor", scheduleId, existingSchedule.TimeIn, existingSchedule.TimeOut);
        VerifyUpdateLookup("section", scheduleId, existingSchedule.TimeIn, existingSchedule.TimeOut);
    }

    [Fact]
    public async Task UpdateScheduleAsync_OnlyInstructorChanged_ValidatesNewInstructorWithExistingDayAndTimeRange()
    {
        var scheduleId = Guid.NewGuid();
        var existingSchedule = CreateExistingSchedule(scheduleId);
        var newInstructorId = AddInstructor("Grace", "Hopper");
        var updateSchedule = new UpdateSchedule { InstructorId = newInstructorId };
        _mockScheduleRepository.Setup(r => r.GetScheduleByIdAsync(scheduleId)).ReturnsAsync(existingSchedule);
        _mockScheduleRepository.Setup(r => r.UpdateScheduleAsync(It.IsAny<Schedules>())).ReturnsAsync((Schedules schedule) => schedule);

        var result = await _service.UpdateScheduleAsync(scheduleId, updateSchedule);

        Assert.Equal(newInstructorId, result.InstructorId);
        _mockScheduleRepository.Verify(r => r.FindInstructorOverlapAsync(
            newInstructorId,
            existingSchedule.DayOfWeek,
            existingSchedule.TimeIn,
            existingSchedule.TimeOut,
            scheduleId), Times.Once);
    }

    [Fact]
    public async Task UpdateScheduleAsync_OnlySectionChanged_ValidatesNewSectionWithExistingDayAndTimeRange()
    {
        var scheduleId = Guid.NewGuid();
        var existingSchedule = CreateExistingSchedule(scheduleId);
        var newSectionId = AddSection("CS-3B");
        var updateSchedule = new UpdateSchedule { SectionId = newSectionId };
        _mockScheduleRepository.Setup(r => r.GetScheduleByIdAsync(scheduleId)).ReturnsAsync(existingSchedule);
        _mockScheduleRepository.Setup(r => r.UpdateScheduleAsync(It.IsAny<Schedules>())).ReturnsAsync((Schedules schedule) => schedule);

        var result = await _service.UpdateScheduleAsync(scheduleId, updateSchedule);

        Assert.Equal(newSectionId, result.SectionId);
        _mockScheduleRepository.Verify(r => r.FindSectionOverlapAsync(
            newSectionId,
            existingSchedule.DayOfWeek,
            existingSchedule.TimeIn,
            existingSchedule.TimeOut,
            scheduleId), Times.Once);
    }

    [Theory]
    [InlineData("classroom")]
    [InlineData("instructor")]
    [InlineData("section")]
    public async Task UpdateScheduleAsync_BackToBackResourceSchedule_UpdatesSchedule(string resourceType)
    {
        var scheduleId = Guid.NewGuid();
        var existingSchedule = CreateExistingSchedule(scheduleId);
        var updateSchedule = new UpdateSchedule
        {
            TimeIn = TimeOnly.FromTimeSpan(TimeSpan.FromHours(10)),
            TimeOut = TimeOnly.FromTimeSpan(TimeSpan.FromHours(12)),
        };
        _mockScheduleRepository.Setup(r => r.GetScheduleByIdAsync(scheduleId)).ReturnsAsync(existingSchedule);
        _mockScheduleRepository.Setup(r => r.UpdateScheduleAsync(It.IsAny<Schedules>())).ReturnsAsync((Schedules schedule) => schedule);

        var result = await _service.UpdateScheduleAsync(scheduleId, updateSchedule);

        Assert.Equal(updateSchedule.TimeIn, result.TimeIn);
        Assert.Equal(updateSchedule.TimeOut, result.TimeOut);
        VerifyUpdateLookup(resourceType, scheduleId, updateSchedule.TimeIn.Value, updateSchedule.TimeOut.Value);
        _mockScheduleRepository.Verify(r => r.UpdateScheduleAsync(It.IsAny<Schedules>()), Times.Once);
    }

    [Fact]
    public async Task UpdateScheduleAsync_ExactDuplicateConstraint_ThrowsEntityConflictException()
    {
        var scheduleId = Guid.NewGuid();
        var existingSchedule = CreateExistingSchedule(scheduleId);
        _mockScheduleRepository.Setup(r => r.GetScheduleByIdAsync(scheduleId)).ReturnsAsync(existingSchedule);
        _mockScheduleRepository
            .Setup(r => r.UpdateScheduleAsync(It.IsAny<Schedules>()))
            .ThrowsAsync(CreateScheduleUniqueConstraintException());

        var exception = await Assert.ThrowsAsync<EntityConflictException>(() => _service.UpdateScheduleAsync(scheduleId, new UpdateSchedule { DayOfWeek = "Tuesday" }));

        Assert.Equal("Schedule", exception.EntityName);
        Assert.Equal("duplicate", exception.ConflictType);
        Assert.Contains("Schedule conflict", exception.Message);
    }

    #endregion

    private CreateSchedule CreateValidScheduleRequest()
    {
        return new CreateSchedule
        {
            TimeIn = TimeOnly.FromTimeSpan(TimeSpan.FromHours(8)),
            TimeOut = TimeOnly.FromTimeSpan(TimeSpan.FromHours(10)),
            DayOfWeek = "Monday",
            SubjectId = SubjectUuid,
            ClassroomId = ClassroomUuid,
            SectionId = SectionUuid,
            InstructorId = InstructorUuid,
        };
    }

    private Schedules CreateExistingSchedule(Guid scheduleId)
    {
        return new Schedules
        {
            Id = scheduleId,
            TimeIn = TimeOnly.FromTimeSpan(TimeSpan.FromHours(8)),
            TimeOut = TimeOnly.FromTimeSpan(TimeSpan.FromHours(10)),
            DayOfWeek = "Monday",
            SubjectId = SubjectUuid,
            ClassroomId = ClassroomUuid,
            SectionId = SectionUuid,
            InstructorId = InstructorUuid,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow,
        };
    }

    private ScheduleConflictDetails CreateConflictDetails(string resourceType)
    {
        return new ScheduleConflictDetails
        {
            ScheduleId = Guid.NewGuid(),
            DayOfWeek = "Monday",
            TimeIn = TimeOnly.FromTimeSpan(TimeSpan.FromHours(8)),
            TimeOut = TimeOnly.FromTimeSpan(TimeSpan.FromHours(10)),
            ClassroomName = resourceType == "classroom" ? "Room 101" : null,
            InstructorName = resourceType == "instructor" ? "John Doe" : null,
            SectionName = resourceType == "section" ? "CS-3A" : null,
        };
    }

    private void SetupCreateConflict(string resourceType, ScheduleConflictDetails conflict)
    {
        switch (resourceType)
        {
            case "classroom":
                _mockScheduleRepository
                    .Setup(r => r.FindClassroomOverlapAsync(ClassroomUuid, "Monday", It.IsAny<TimeOnly>(), It.IsAny<TimeOnly>(), null))
                    .ReturnsAsync(conflict);
                break;
            case "instructor":
                _mockScheduleRepository
                    .Setup(r => r.FindInstructorOverlapAsync(InstructorUuid, "Monday", It.IsAny<TimeOnly>(), It.IsAny<TimeOnly>(), null))
                    .ReturnsAsync(conflict);
                break;
            case "section":
                _mockScheduleRepository
                    .Setup(r => r.FindSectionOverlapAsync(SectionUuid, "Monday", It.IsAny<TimeOnly>(), It.IsAny<TimeOnly>(), null))
                    .ReturnsAsync(conflict);
                break;
        }
    }

    private void SetupUpdateConflict(string resourceType, Schedules existingSchedule, UpdateSchedule updateSchedule, ScheduleConflictDetails conflict)
    {
        var timeIn = updateSchedule.TimeIn ?? existingSchedule.TimeIn;
        var timeOut = updateSchedule.TimeOut ?? existingSchedule.TimeOut;
        switch (resourceType)
        {
            case "classroom":
                _mockScheduleRepository
                    .Setup(r => r.FindClassroomOverlapAsync(existingSchedule.ClassroomId, existingSchedule.DayOfWeek, timeIn, timeOut, existingSchedule.Id))
                    .ReturnsAsync(conflict);
                break;
            case "instructor":
                _mockScheduleRepository
                    .Setup(r => r.FindInstructorOverlapAsync(existingSchedule.InstructorId, existingSchedule.DayOfWeek, timeIn, timeOut, existingSchedule.Id))
                    .ReturnsAsync(conflict);
                break;
            case "section":
                _mockScheduleRepository
                    .Setup(r => r.FindSectionOverlapAsync(existingSchedule.SectionId, existingSchedule.DayOfWeek, timeIn, timeOut, existingSchedule.Id))
                    .ReturnsAsync(conflict);
                break;
        }
    }

    private void VerifyCreateLookup(string resourceType, TimeOnly timeIn, TimeOnly timeOut)
    {
        switch (resourceType)
        {
            case "classroom":
                _mockScheduleRepository.Verify(r => r.FindClassroomOverlapAsync(ClassroomUuid, "Monday", timeIn, timeOut, null), Times.Once);
                break;
            case "instructor":
                _mockScheduleRepository.Verify(r => r.FindInstructorOverlapAsync(InstructorUuid, "Monday", timeIn, timeOut, null), Times.Once);
                break;
            case "section":
                _mockScheduleRepository.Verify(r => r.FindSectionOverlapAsync(SectionUuid, "Monday", timeIn, timeOut, null), Times.Once);
                break;
        }
    }

    private void VerifyUpdateLookup(string resourceType, Guid scheduleId, TimeOnly timeIn, TimeOnly timeOut)
    {
        switch (resourceType)
        {
            case "classroom":
                _mockScheduleRepository.Verify(r => r.FindClassroomOverlapAsync(ClassroomUuid, "Monday", timeIn, timeOut, scheduleId), Times.Once);
                break;
            case "instructor":
                _mockScheduleRepository.Verify(r => r.FindInstructorOverlapAsync(InstructorUuid, "Monday", timeIn, timeOut, scheduleId), Times.Once);
                break;
            case "section":
                _mockScheduleRepository.Verify(r => r.FindSectionOverlapAsync(SectionUuid, "Monday", timeIn, timeOut, scheduleId), Times.Once);
                break;
        }
    }

    private Guid AddInstructor(string firstName, string lastName)
    {
        var instructor = new Instructor
        {
            Id = Guid.NewGuid(),
            Firstname = firstName,
            Lastname = lastName,
            UserId = Guid.NewGuid().ToString(),
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow,
        };
        _context.Instructors.Add(instructor);
        _context.SaveChanges();
        return instructor.Id;
    }

    private Guid AddSection(string name)
    {
        var section = new Section
        {
            Id = Guid.NewGuid(),
            Name = name,
            CourseId = _context.Courses.Single().Id,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow,
        };
        _context.Sections.Add(section);
        _context.SaveChanges();
        return section.Id;
    }

    private static DbUpdateException CreateScheduleUniqueConstraintException()
    {
        return new DbUpdateException(
            "Could not save schedule",
            new InvalidOperationException("Cannot insert duplicate key row in object 'dbo.Schedules' with unique index 'IX_Schedules_ClassroomId_DayOfWeek_TimeIn_TimeOut'."));
    }
}
