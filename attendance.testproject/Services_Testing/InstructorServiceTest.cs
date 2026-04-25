using System.Security.Claims;
using attendance_monitoring.Classes;
using attendance_monitoring.Constants;
using attendance_monitoring.Exceptions;
using attendance_monitoring.IRepository;
using attendance_monitoring.IServices;
using attendance_monitoring.Models.DTO.Request;
using attendance_monitoring.Models.DTO.Response;
using attendance_monitoring.Services;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Moq;

namespace attendance.testproject.Services_Testing;

/// <summary>
/// Unit tests for InstructorService
/// Tests all public methods with happy paths, edge cases, and error scenarios
/// </summary>
public class InstructorServiceTest
{
    private readonly Mock<IInstructorRepository> _mockInstructorRepository;
    private readonly Mock<ISectionRepository> _mockSectionRepository;
    private readonly Mock<IStudentRepository> _mockStudentRepository;
    private readonly Mock<IScheduleRepository> _mockScheduleRepository;
    private readonly Mock<IFingerprintRepository> _mockFingerprintRepository;
    private readonly Mock<IUserContextService> _mockUserContextService;
    private readonly Mock<ILogger<InstructorService>> _mockLogger;
    private readonly InstructorService _service;
    private readonly ClaimsPrincipal _testUserPrincipal;

    public InstructorServiceTest()
    {
        _mockInstructorRepository = new Mock<IInstructorRepository>();
        _mockSectionRepository = new Mock<ISectionRepository>();
        _mockStudentRepository = new Mock<IStudentRepository>();
        _mockScheduleRepository = new Mock<IScheduleRepository>();
        _mockFingerprintRepository = new Mock<IFingerprintRepository>();
        _mockUserContextService = new Mock<IUserContextService>();
        _mockLogger = new Mock<ILogger<InstructorService>>();

        _mockSectionRepository
            .Setup(r => r.GetSectionByIdAsync(It.IsAny<int>()))
            .ReturnsAsync((int id) => new Section { Id = id });
        _mockFingerprintRepository
            .Setup(r => r.GetActiveFingerprintsAsync())
            .ReturnsAsync(new List<Fingerprint>());
        _mockFingerprintRepository
            .Setup(r => r.GetDevicesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<FingerprintDevice>());

        _service = new InstructorService(
            _mockInstructorRepository.Object,
            _mockSectionRepository.Object,
            _mockStudentRepository.Object,
            _mockScheduleRepository.Object,
            _mockFingerprintRepository.Object,
            _mockUserContextService.Object,
            _mockLogger.Object
        );

        // Create a reusable test ClaimsPrincipal
        var claims = new List<Claim>
        {
            new Claim(ClaimTypes.NameIdentifier, "test-user-id"),
            new Claim(ClaimTypes.Name, "Test User")
        };
        _testUserPrincipal = new ClaimsPrincipal(new ClaimsIdentity(claims, "TestAuth"));
    }

    #region Constructor Tests

    [Fact]
    public void Constructor_NullDependency_ThrowsArgumentNullException()
    {
        // Arrange & Act & Assert
        Assert.Throws<ArgumentNullException>(() => new InstructorService(null!, _mockSectionRepository.Object, _mockStudentRepository.Object, _mockScheduleRepository.Object, _mockFingerprintRepository.Object, _mockUserContextService.Object, _mockLogger.Object));
        Assert.Throws<ArgumentNullException>(() => new InstructorService(_mockInstructorRepository.Object, null!, _mockStudentRepository.Object, _mockScheduleRepository.Object, _mockFingerprintRepository.Object, _mockUserContextService.Object, _mockLogger.Object));
        Assert.Throws<ArgumentNullException>(() => new InstructorService(_mockInstructorRepository.Object, _mockSectionRepository.Object, null!, _mockScheduleRepository.Object, _mockFingerprintRepository.Object, _mockUserContextService.Object, _mockLogger.Object));
        Assert.Throws<ArgumentNullException>(() => new InstructorService(_mockInstructorRepository.Object, _mockSectionRepository.Object, _mockStudentRepository.Object, null!, _mockFingerprintRepository.Object, _mockUserContextService.Object, _mockLogger.Object));
        Assert.Throws<ArgumentNullException>(() => new InstructorService(_mockInstructorRepository.Object, _mockSectionRepository.Object, _mockStudentRepository.Object, _mockScheduleRepository.Object, null!, _mockUserContextService.Object, _mockLogger.Object));
        Assert.Throws<ArgumentNullException>(() => new InstructorService(_mockInstructorRepository.Object, _mockSectionRepository.Object, _mockStudentRepository.Object, _mockScheduleRepository.Object, _mockFingerprintRepository.Object, null!, _mockLogger.Object));
        Assert.Throws<ArgumentNullException>(() => new InstructorService(_mockInstructorRepository.Object, _mockSectionRepository.Object, _mockStudentRepository.Object, _mockScheduleRepository.Object, _mockFingerprintRepository.Object, _mockUserContextService.Object, null!));
    }

    #endregion

    #region GetAllInstructorsAsync Tests

    [Fact]
    public async Task GetAllInstructorsAsync_Success_ReturnsAllInstructors()
    {
        // Arrange
        var instructors = new List<Instructor>
        {
            new Instructor { Id = 1, Firstname = "John", Lastname = "Doe" },
            new Instructor { Id = 2, Firstname = "Jane", Lastname = "Smith" }
        };
        _mockInstructorRepository.Setup(r => r.GetAllInstructorsAsync()).ReturnsAsync(instructors);

        // Act
        var result = await _service.GetAllInstructorsAsync();

        // Assert
        Assert.NotNull(result);
        Assert.Equal(2, result.Count);
        _mockInstructorRepository.Verify(r => r.GetAllInstructorsAsync(), Times.Once);
    }

    [Fact]
    public async Task GetAllInstructorsAsync_RepositoryFailure_WrapsInEntityServiceException()
    {
        // Arrange
        var expectedException = new InvalidOperationException("Database error");
        _mockInstructorRepository.Setup(r => r.GetAllInstructorsAsync()).ThrowsAsync(expectedException);

        // Act
        var exception = await Assert.ThrowsAsync<EntityServiceException>(() => _service.GetAllInstructorsAsync());

        // Assert
        Assert.Equal("Instructor", exception.EntityName);
        Assert.Equal("GetAllInstructors", exception.Operation);
        Assert.Same(expectedException, exception.InnerException);
    }

    #endregion

    #region GetInstructorByIdAsync Tests

    [Fact]
    public async Task GetInstructorByIdAsync_Success_ReturnsInstructor()
    {
        // Arrange
        const int instructorId = 1;
        var instructor = new Instructor { Id = instructorId, Firstname = "John", Lastname = "Doe" };
        _mockInstructorRepository.Setup(r => r.GetInstructorByIdAsync(instructorId)).ReturnsAsync(instructor);

        // Act
        var result = await _service.GetInstructorByIdAsync(instructorId);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(instructorId, result.Id);
        _mockInstructorRepository.Verify(r => r.GetInstructorByIdAsync(instructorId), Times.Once);
    }

    [Fact]
    public async Task GetInstructorByIdAsync_NotFound_ThrowsEntityNotFoundException()
    {
        // Arrange
        const int instructorId = 999;
        _mockInstructorRepository.Setup(r => r.GetInstructorByIdAsync(instructorId)).ReturnsAsync((Instructor?)null);

        // Act & Assert
        var exception = await Assert.ThrowsAsync<EntityNotFoundException<int>>(() => _service.GetInstructorByIdAsync(instructorId));
        Assert.Equal("Instructor", exception.EntityName);
        Assert.Equal(instructorId, exception.Key);
    }

    [Fact]
    public async Task GetInstructorByIdAsync_RepositoryFailure_WrapsInEntityServiceException()
    {
        // Arrange
        const int instructorId = 1;
        var expectedException = new InvalidOperationException("Database error");
        _mockInstructorRepository.Setup(r => r.GetInstructorByIdAsync(instructorId)).ThrowsAsync(expectedException);

        // Act
        var exception = await Assert.ThrowsAsync<EntityServiceException>(() => _service.GetInstructorByIdAsync(instructorId));

        // Assert
        Assert.Equal("Instructor", exception.EntityName);
        Assert.Equal($"GetInstructorById: {instructorId}", exception.Operation);
        Assert.Same(expectedException, exception.InnerException);
    }

    #endregion

    #region GetSubjectsByInstructorIdAsync Tests

    [Fact]
    public async Task GetSubjectsByInstructorIdAsync_Success_ReturnsSubjectDtos()
    {
        // Arrange
        const int instructorId = 1;
        var instructor = new Instructor { Id = instructorId, Firstname = "John", Lastname = "Doe" };
        var subjects = new List<Subject>
        {
            new Subject { Id = 1, Name = "Mathematics", Code = "MATH101" },
            new Subject { Id = 2, Name = "Physics", Code = "PHYS101" }
        };
        _mockInstructorRepository.Setup(r => r.GetInstructorByIdAsync(instructorId)).ReturnsAsync(instructor);
        _mockScheduleRepository.Setup(r => r.GetSubjectsByInstructorIdAsync(instructorId)).ReturnsAsync(subjects);

        // Act
        var result = await _service.GetSubjectsByInstructorIdAsync(instructorId);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(2, result.Count());
        Assert.Equal("Mathematics", result.First().Name);
        Assert.Equal("PHYS101", result.Last().Code);
        _mockInstructorRepository.Verify(r => r.GetInstructorByIdAsync(instructorId), Times.Once);
        _mockScheduleRepository.Verify(r => r.GetSubjectsByInstructorIdAsync(instructorId), Times.Once);
    }

    [Fact]
    public async Task GetSubjectsByInstructorIdAsync_InstructorNotFound_ThrowsEntityNotFoundException()
    {
        // Arrange
        const int instructorId = 999;
        _mockInstructorRepository.Setup(r => r.GetInstructorByIdAsync(instructorId)).ReturnsAsync((Instructor?)null);

        // Act & Assert
        var exception = await Assert.ThrowsAsync<EntityNotFoundException<int>>(() => _service.GetSubjectsByInstructorIdAsync(instructorId));
        Assert.Equal("Instructor", exception.EntityName);
        Assert.Equal(instructorId, exception.Key);
    }

    [Fact]
    public async Task GetSubjectsByInstructorIdAsync_RepositoryFailure_WrapsInEntityServiceException()
    {
        // Arrange
        const int instructorId = 1;
        var instructor = new Instructor { Id = instructorId, Firstname = "John", Lastname = "Doe" };
        var expectedException = new InvalidOperationException("Database error");
        _mockInstructorRepository.Setup(r => r.GetInstructorByIdAsync(instructorId)).ReturnsAsync(instructor);
        _mockScheduleRepository.Setup(r => r.GetSubjectsByInstructorIdAsync(instructorId)).ThrowsAsync(expectedException);

        // Act
        var exception = await Assert.ThrowsAsync<EntityServiceException>(() => _service.GetSubjectsByInstructorIdAsync(instructorId));

        // Assert
        Assert.Equal("Instructor", exception.EntityName);
        Assert.Equal($"GetSubjectsByInstructorId: {instructorId}", exception.Operation);
        Assert.Same(expectedException, exception.InnerException);
    }

    #endregion

    #region GetSchedulesByInstructorAsync Tests

    [Fact]
    public async Task GetSchedulesByInstructorAsync_Success_ReturnsScheduleDtos()
    {
        // Arrange
        const string userId = "test-user-id";
        const int instructorId = 1;
        var instructor = new Instructor { Id = instructorId, Firstname = "John", Lastname = "Doe", UserId = userId };

        var schedule = new Schedules
        {
            Id = 1,
            TimeIn = TimeOnly.FromTimeSpan(TimeSpan.FromHours(8)),
            TimeOut = TimeOnly.FromTimeSpan(TimeSpan.FromHours(10)),
            DayOfWeek = "Monday",
            Subject = new Subject { Id = 1, Name = "Mathematics", Code = "MATH101" },
            Classroom = new Classroom { Id = 1, Name = "Room 101" },
            Section = new Section { Id = 1, Name = "CS-3A", CourseId = 1 },
            Instructor = instructor
        };

        _mockUserContextService.Setup(s => s.GetUserIdAsync(_testUserPrincipal)).ReturnsAsync(userId);
        _mockInstructorRepository.Setup(r => r.GetInstructorByUserIdAsync(userId)).ReturnsAsync(instructor);
        _mockScheduleRepository.Setup(r => r.GetSchedulesByInstructorIdAsync(instructorId)).ReturnsAsync(new List<Schedules> { schedule });

        // Act
        var result = await _service.GetSchedulesByInstructorAsync(_testUserPrincipal);

        // Assert
        Assert.NotNull(result);
        Assert.Single(result);
        Assert.Equal("Mathematics", result.First().Subject.Name);
        Assert.Equal("Room 101", result.First().Classroom.Name);
    }

    [Fact]
    public async Task GetSchedulesByInstructorAsync_MissingUserId_ThrowsEntityNotFoundException()
    {
        // Arrange
        _mockUserContextService.Setup(s => s.GetUserIdAsync(_testUserPrincipal)).ReturnsAsync((string?)null);

        // Act & Assert
        var exception = await Assert.ThrowsAsync<EntityNotFoundException<string>>(() => _service.GetSchedulesByInstructorAsync(_testUserPrincipal));
        Assert.Equal("User", exception.EntityName);
    }

    [Fact]
    public async Task GetSchedulesByInstructorAsync_InstructorNotFound_ThrowsEntityNotFoundException()
    {
        // Arrange
        const string userId = "test-user-id";
        _mockUserContextService.Setup(s => s.GetUserIdAsync(_testUserPrincipal)).ReturnsAsync(userId);
        _mockInstructorRepository.Setup(r => r.GetInstructorByUserIdAsync(userId)).ReturnsAsync((Instructor?)null);

        // Act & Assert
        var exception = await Assert.ThrowsAsync<EntityNotFoundException<string>>(() => _service.GetSchedulesByInstructorAsync(_testUserPrincipal));
        Assert.Equal("Instructor", exception.EntityName);
    }

    [Fact]
    public async Task GetSchedulesByInstructorAsync_RepositoryFailure_WrapsInEntityServiceException()
    {
        // Arrange
        const string userId = "test-user-id";
        const int instructorId = 1;
        var instructor = new Instructor { Id = instructorId, Firstname = "John", Lastname = "Doe", UserId = userId };
        var expectedException = new InvalidOperationException("Database error");

        _mockUserContextService.Setup(s => s.GetUserIdAsync(_testUserPrincipal)).ReturnsAsync(userId);
        _mockInstructorRepository.Setup(r => r.GetInstructorByUserIdAsync(userId)).ReturnsAsync(instructor);
        _mockScheduleRepository.Setup(r => r.GetSchedulesByInstructorIdAsync(instructorId)).ThrowsAsync(expectedException);

        // Act
        var exception = await Assert.ThrowsAsync<EntityServiceException>(() => _service.GetSchedulesByInstructorAsync(_testUserPrincipal));

        // Assert
        Assert.Equal("Instructor", exception.EntityName);
        Assert.Equal("GetSchedulesByInstructor", exception.Operation);
        Assert.Same(expectedException, exception.InnerException);
    }

    #endregion

    #region GetInstructorProfileAsync Tests

    [Fact]
    public async Task GetInstructorProfileAsync_Success_ReturnsProfileDto()
    {
        // Arrange
        const string userId = "test-user-id";
        const int instructorId = 1;
        var user = new IdentityUser { Id = userId, Email = "test@example.com" };
        var instructor = new Instructor
        {
            Id = instructorId,
            Firstname = "John",
            Lastname = "Doe",
            Department = "Computer Science",
            UserId = userId,
            User = user,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        _mockUserContextService.Setup(s => s.GetUserIdAsync(_testUserPrincipal)).ReturnsAsync(userId);
        _mockInstructorRepository.Setup(r => r.GetInstructorByUserIdAsync(userId)).ReturnsAsync(instructor);

        // Act
        var result = await _service.GetInstructorProfileAsync(_testUserPrincipal);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(instructorId, result.Id);
        Assert.Equal("John", result.Firstname);
        Assert.Equal("Doe", result.Lastname);
        Assert.Equal("test@example.com", result.Email);
        Assert.Equal("Computer Science", result.Department);
    }

    [Fact]
    public async Task GetInstructorProfileAsync_MissingUserId_ReturnsNull()
    {
        // Arrange
        _mockUserContextService.Setup(s => s.GetUserIdAsync(_testUserPrincipal)).ReturnsAsync((string?)null);

        // Act
        var result = await _service.GetInstructorProfileAsync(_testUserPrincipal);

        // Assert
        Assert.Null(result);
    }

    [Fact]
    public async Task GetInstructorProfileAsync_InstructorNotFound_ReturnsNull()
    {
        // Arrange
        const string userId = "test-user-id";
        _mockUserContextService.Setup(s => s.GetUserIdAsync(_testUserPrincipal)).ReturnsAsync(userId);
        _mockInstructorRepository.Setup(r => r.GetInstructorByUserIdAsync(userId)).ReturnsAsync((Instructor?)null);

        // Act
        var result = await _service.GetInstructorProfileAsync(_testUserPrincipal);

        // Assert
        Assert.Null(result);
    }

    [Fact]
    public async Task GetInstructorProfileAsync_RepositoryFailure_WrapsInEntityServiceException()
    {
        // Arrange
        const string userId = "test-user-id";
        var expectedException = new InvalidOperationException("Database error");
        _mockUserContextService.Setup(s => s.GetUserIdAsync(_testUserPrincipal)).ReturnsAsync(userId);
        _mockInstructorRepository.Setup(r => r.GetInstructorByUserIdAsync(userId)).ThrowsAsync(expectedException);

        // Act
        var exception = await Assert.ThrowsAsync<EntityServiceException>(() => _service.GetInstructorProfileAsync(_testUserPrincipal));

        // Assert
        Assert.Equal("Instructor", exception.EntityName);
        Assert.Equal("GetInstructorProfile", exception.Operation);
        Assert.Same(expectedException, exception.InnerException);
    }

    #endregion

    #region CreateInstructorAsync Tests

    [Fact]
    public async Task CreateInstructorAsync_Success_CreatesInstructor()
    {
        // Arrange
        const string userId = "test-user-id";
        var createInstructor = new CreateInstructor
        {
            Firstname = "John",
            Lastname = "Doe"
        };
        var createdInstructor = new Instructor
        {
            Id = 1,
            Firstname = "John",
            Lastname = "Doe",
            UserId = userId,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        _mockUserContextService.Setup(s => s.GetUserIdAsync(_testUserPrincipal)).ReturnsAsync(userId);
        _mockInstructorRepository.Setup(r => r.GetInstructorByUserIdAsync(userId)).ReturnsAsync((Instructor?)null);
        _mockInstructorRepository.Setup(r => r.CreateInstructorAsync(It.IsAny<Instructor>())).ReturnsAsync(createdInstructor);
        _mockInstructorRepository.Setup(r => r.SaveChangesAsync()).ReturnsAsync(1);

        // Act
        var result = await _service.CreateInstructorAsync(createInstructor, _testUserPrincipal);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(userId, result.UserId);
        Assert.Equal("John", result.Firstname);
        Assert.Equal("Doe", result.Lastname);
        _mockInstructorRepository.Verify(r => r.CreateInstructorAsync(It.IsAny<Instructor>()), Times.Once);
        _mockInstructorRepository.Verify(r => r.SaveChangesAsync(), Times.Once);
    }

    [Fact]
    public async Task CreateInstructorAsync_MissingFirstname_ThrowsValidationException()
    {
        // Arrange
        var createInstructor = new CreateInstructor
        {
            Firstname = "",
            Lastname = "Doe"
        };

        // Act & Assert
        await Assert.ThrowsAsync<ValidationException>(() => _service.CreateInstructorAsync(createInstructor, _testUserPrincipal));
    }

    [Fact]
    public async Task CreateInstructorAsync_MissingLastname_ThrowsValidationException()
    {
        // Arrange
        var createInstructor = new CreateInstructor
        {
            Firstname = "John",
            Lastname = ""
        };

        // Act & Assert
        await Assert.ThrowsAsync<ValidationException>(() => _service.CreateInstructorAsync(createInstructor, _testUserPrincipal));
    }

    [Fact]
    public async Task CreateInstructorAsync_MissingUserId_ThrowsValidationException()
    {
        // Arrange
        var createInstructor = new CreateInstructor
        {
            Firstname = "John",
            Lastname = "Doe"
        };
        _mockUserContextService.Setup(s => s.GetUserIdAsync(_testUserPrincipal)).ReturnsAsync((string?)null);

        // Act & Assert
        await Assert.ThrowsAsync<ValidationException>(() => _service.CreateInstructorAsync(createInstructor, _testUserPrincipal));
    }

    [Fact]
    public async Task CreateInstructorAsync_DuplicateInstructor_ThrowsEntityAlreadyExistsException()
    {
        // Arrange
        const string userId = "test-user-id";
        var createInstructor = new CreateInstructor
        {
            Firstname = "John",
            Lastname = "Doe"
        };
        var existingInstructor = new Instructor { Id = 1, Firstname = "John", Lastname = "Doe", UserId = userId };

        _mockUserContextService.Setup(s => s.GetUserIdAsync(_testUserPrincipal)).ReturnsAsync(userId);
        _mockInstructorRepository.Setup(r => r.GetInstructorByUserIdAsync(userId)).ReturnsAsync(existingInstructor);

        // Act & Assert
        var exception = await Assert.ThrowsAsync<EntityAlreadyExistsException<string>>(() => _service.CreateInstructorAsync(createInstructor, _testUserPrincipal));
        Assert.Equal("Instructor", exception.EntityName);
        Assert.Equal("UserId", exception.IdentifierPropertyName);
        Assert.Equal(userId, exception.EntityIdentifier);
    }

    [Fact]
    public async Task CreateInstructorAsync_RepositoryFailure_WrapsInEntityServiceException()
    {
        // Arrange
        const string userId = "test-user-id";
        var createInstructor = new CreateInstructor
        {
            Firstname = "John",
            Lastname = "Doe"
        };
        var expectedException = new InvalidOperationException("Database error");

        _mockUserContextService.Setup(s => s.GetUserIdAsync(_testUserPrincipal)).ReturnsAsync(userId);
        _mockInstructorRepository.Setup(r => r.GetInstructorByUserIdAsync(userId)).ReturnsAsync((Instructor?)null);
        _mockInstructorRepository.Setup(r => r.CreateInstructorAsync(It.IsAny<Instructor>())).ThrowsAsync(expectedException);

        // Act
        var exception = await Assert.ThrowsAsync<EntityServiceException>(() => _service.CreateInstructorAsync(createInstructor, _testUserPrincipal));

        // Assert
        Assert.Equal("Instructor", exception.EntityName);
        Assert.Equal("CreateInstructor", exception.Operation);
    }

    #endregion

    #region UpdateInstructorAsync Tests

    [Fact]
    public async Task UpdateInstructorAsync_Success_UpdatesInstructor()
    {
        // Arrange
        const int instructorId = 1;
        const string userId = "test-user-id";
        var updateInstructor = new UpdateInstructor
        {
            Firstname = "John Updated",
            Lastname = "Doe Updated",
            Department = "Engineering"
        };
        var existingInstructor = new Instructor
        {
            Id = instructorId,
            Firstname = "John",
            Lastname = "Doe",
            Department = "Computer Science",
            UserId = userId
        };
        var updatedInstructor = new Instructor
        {
            Id = instructorId,
            Firstname = "John Updated",
            Lastname = "Doe Updated",
            Department = "Engineering",
            UserId = userId,
            UpdatedAt = DateTime.UtcNow
        };

        _mockUserContextService.Setup(s => s.GetUserIdAsync(_testUserPrincipal)).ReturnsAsync(userId);
        _mockInstructorRepository.Setup(r => r.GetInstructorByIdAsync(instructorId)).ReturnsAsync(existingInstructor);
        _mockUserContextService.Setup(s => s.IsAuthorizedAsync(_testUserPrincipal, userId, RoleConstants.Admin, RoleConstants.Instructor)).ReturnsAsync(true);
        _mockInstructorRepository.Setup(r => r.UpdateInstructorAsync(It.IsAny<Instructor>())).ReturnsAsync(updatedInstructor);
        _mockInstructorRepository.Setup(r => r.SaveChangesAsync()).ReturnsAsync(1);

        // Act
        var result = await _service.UpdateInstructorAsync(instructorId, updateInstructor, _testUserPrincipal);

        // Assert
        Assert.NotNull(result);
        Assert.Equal("John Updated", result.Firstname);
        Assert.Equal("Doe Updated", result.Lastname);
        Assert.Equal("Engineering", result.Department);
        _mockInstructorRepository.Verify(r => r.UpdateInstructorAsync(It.IsAny<Instructor>()), Times.Once);
        _mockInstructorRepository.Verify(r => r.SaveChangesAsync(), Times.Once);
    }

    [Fact]
    public async Task UpdateInstructorAsync_MissingUserId_ThrowsEntityServiceException()
    {
        // Arrange
        const int instructorId = 1;
        var updateInstructor = new UpdateInstructor { Firstname = "John", Lastname = "Doe" };
        _mockUserContextService.Setup(s => s.GetUserIdAsync(_testUserPrincipal)).ReturnsAsync((string?)null);

        // Act & Assert
        var exception = await Assert.ThrowsAsync<EntityServiceException>(() => _service.UpdateInstructorAsync(instructorId, updateInstructor, _testUserPrincipal));
        Assert.Equal("Instructor", exception.EntityName);
        Assert.Contains("User ID not found", exception.Message);
    }

    [Fact]
    public async Task UpdateInstructorAsync_InstructorNotFound_ThrowsEntityNotFoundException()
    {
        // Arrange
        const int instructorId = 999;
        const string userId = "test-user-id";
        var updateInstructor = new UpdateInstructor { Firstname = "John", Lastname = "Doe" };
        _mockUserContextService.Setup(s => s.GetUserIdAsync(_testUserPrincipal)).ReturnsAsync(userId);
        _mockInstructorRepository.Setup(r => r.GetInstructorByIdAsync(instructorId)).ReturnsAsync((Instructor?)null);

        // Act & Assert
        var exception = await Assert.ThrowsAsync<EntityNotFoundException<int>>(() => _service.UpdateInstructorAsync(instructorId, updateInstructor, _testUserPrincipal));
        Assert.Equal("Instructor", exception.EntityName);
        Assert.Equal(instructorId, exception.Key);
    }

    [Fact]
    public async Task UpdateInstructorAsync_Unauthorized_ThrowsEntityUnauthorizedException()
    {
        // Arrange
        const int instructorId = 1;
        const string userId = "test-user-id";
        var updateInstructor = new UpdateInstructor { Firstname = "John", Lastname = "Doe" };
        var existingInstructor = new Instructor { Id = instructorId, Firstname = "John", Lastname = "Doe", UserId = "different-user-id" };
        _mockUserContextService.Setup(s => s.GetUserIdAsync(_testUserPrincipal)).ReturnsAsync(userId);
        _mockInstructorRepository.Setup(r => r.GetInstructorByIdAsync(instructorId)).ReturnsAsync(existingInstructor);
        _mockUserContextService.Setup(s => s.IsAuthorizedAsync(_testUserPrincipal, "different-user-id", RoleConstants.Admin, RoleConstants.Instructor)).ReturnsAsync(false);

        // Act & Assert
        await Assert.ThrowsAsync<EntityUnauthorizedException>(() => _service.UpdateInstructorAsync(instructorId, updateInstructor, _testUserPrincipal));
    }

    [Fact]
    public async Task UpdateInstructorAsync_RepositoryFailure_WrapsInEntityServiceException()
    {
        // Arrange
        const int instructorId = 1;
        const string userId = "test-user-id";
        var updateInstructor = new UpdateInstructor { Firstname = "John", Lastname = "Doe" };
        var existingInstructor = new Instructor { Id = instructorId, Firstname = "John", Lastname = "Doe", UserId = userId };
        var expectedException = new InvalidOperationException("Database error");

        _mockUserContextService.Setup(s => s.GetUserIdAsync(_testUserPrincipal)).ReturnsAsync(userId);
        _mockInstructorRepository.Setup(r => r.GetInstructorByIdAsync(instructorId)).ReturnsAsync(existingInstructor);
        _mockUserContextService.Setup(s => s.IsAuthorizedAsync(_testUserPrincipal, userId, RoleConstants.Admin, RoleConstants.Instructor)).ReturnsAsync(true);
        _mockInstructorRepository.Setup(r => r.UpdateInstructorAsync(It.IsAny<Instructor>())).ThrowsAsync(expectedException);

        // Act
        var exception = await Assert.ThrowsAsync<EntityServiceException>(() => _service.UpdateInstructorAsync(instructorId, updateInstructor, _testUserPrincipal));

        // Assert
        Assert.Equal("Instructor", exception.EntityName);
        Assert.Equal($"UpdateInstructor: {instructorId}", exception.Operation);
        Assert.Same(expectedException, exception.InnerException);
    }

    #endregion

    #region SoftDeleteInstructorAsync Tests

    [Fact]
    public async Task SoftDeleteInstructorAsync_Success_SoftDeletesInstructor()
    {
        // Arrange
        const int instructorId = 1;
        const string userId = "test-user-id";
        var existingInstructor = new Instructor { Id = instructorId, Firstname = "John", Lastname = "Doe", UserId = userId };

        _mockUserContextService.Setup(s => s.GetUserIdAsync(_testUserPrincipal)).ReturnsAsync(userId);
        _mockInstructorRepository.Setup(r => r.GetInstructorByIdAsync(instructorId)).ReturnsAsync(existingInstructor);
        _mockUserContextService.Setup(s => s.IsAuthorizedAsync(_testUserPrincipal, userId, RoleConstants.Admin, RoleConstants.Instructor)).ReturnsAsync(true);
        _mockInstructorRepository.Setup(r => r.SoftDeleteInstructorAsync(instructorId)).ReturnsAsync(true);
        _mockInstructorRepository.Setup(r => r.SaveChangesAsync()).ReturnsAsync(1);

        // Act
        await _service.SoftDeleteInstructorAsync(instructorId, _testUserPrincipal);

        // Assert
        _mockInstructorRepository.Verify(r => r.SoftDeleteInstructorAsync(instructorId), Times.Once);
        _mockInstructorRepository.Verify(r => r.SaveChangesAsync(), Times.Once);
    }

    [Fact]
    public async Task SoftDeleteInstructorAsync_InvalidId_ThrowsEntityServiceException()
    {
        // Arrange
        const int instructorId = -1;

        // Act & Assert
        var exception = await Assert.ThrowsAsync<EntityServiceException>(() => _service.SoftDeleteInstructorAsync(instructorId, _testUserPrincipal));
        Assert.Equal("Instructor", exception.EntityName);
        Assert.Contains("Invalid instructor ID", exception.Message);
    }

    [Fact]
    public async Task SoftDeleteInstructorAsync_MissingUserId_ThrowsEntityServiceException()
    {
        // Arrange
        const int instructorId = 1;
        _mockUserContextService.Setup(s => s.GetUserIdAsync(_testUserPrincipal)).ReturnsAsync((string?)null);

        // Act & Assert
        var exception = await Assert.ThrowsAsync<EntityServiceException>(() => _service.SoftDeleteInstructorAsync(instructorId, _testUserPrincipal));
        Assert.Equal("Instructor", exception.EntityName);
        Assert.Contains("User ID not found", exception.Message);
    }

    [Fact]
    public async Task SoftDeleteInstructorAsync_InstructorNotFound_ThrowsEntityNotFoundException()
    {
        // Arrange
        const int instructorId = 999;
        const string userId = "test-user-id";
        _mockUserContextService.Setup(s => s.GetUserIdAsync(_testUserPrincipal)).ReturnsAsync(userId);
        _mockInstructorRepository.Setup(r => r.GetInstructorByIdAsync(instructorId)).ReturnsAsync((Instructor?)null);

        // Act & Assert
        var exception = await Assert.ThrowsAsync<EntityNotFoundException<int>>(() => _service.SoftDeleteInstructorAsync(instructorId, _testUserPrincipal));
        Assert.Equal("Instructor", exception.EntityName);
        Assert.Equal(instructorId, exception.Key);
    }

    [Fact]
    public async Task SoftDeleteInstructorAsync_Unauthorized_ThrowsEntityUnauthorizedException()
    {
        // Arrange
        const int instructorId = 1;
        const string userId = "test-user-id";
        var existingInstructor = new Instructor { Id = instructorId, Firstname = "John", Lastname = "Doe", UserId = "different-user-id" };
        _mockUserContextService.Setup(s => s.GetUserIdAsync(_testUserPrincipal)).ReturnsAsync(userId);
        _mockInstructorRepository.Setup(r => r.GetInstructorByIdAsync(instructorId)).ReturnsAsync(existingInstructor);
        _mockUserContextService.Setup(s => s.IsAuthorizedAsync(_testUserPrincipal, "different-user-id", RoleConstants.Admin, RoleConstants.Instructor)).ReturnsAsync(false);

        // Act & Assert
        await Assert.ThrowsAsync<EntityUnauthorizedException>(() => _service.SoftDeleteInstructorAsync(instructorId, _testUserPrincipal));
    }

    [Fact]
    public async Task SoftDeleteInstructorAsync_RepositoryDeleteFailure_ThrowsEntityServiceException()
    {
        // Arrange
        const int instructorId = 1;
        const string userId = "test-user-id";
        var existingInstructor = new Instructor { Id = instructorId, Firstname = "John", Lastname = "Doe", UserId = userId };

        _mockUserContextService.Setup(s => s.GetUserIdAsync(_testUserPrincipal)).ReturnsAsync(userId);
        _mockInstructorRepository.Setup(r => r.GetInstructorByIdAsync(instructorId)).ReturnsAsync(existingInstructor);
        _mockUserContextService.Setup(s => s.IsAuthorizedAsync(_testUserPrincipal, userId, RoleConstants.Admin, RoleConstants.Instructor)).ReturnsAsync(true);
        _mockInstructorRepository.Setup(r => r.SoftDeleteInstructorAsync(instructorId)).ReturnsAsync(false);

        // Act & Assert
        var exception = await Assert.ThrowsAsync<EntityServiceException>(() => _service.SoftDeleteInstructorAsync(instructorId, _testUserPrincipal));
        Assert.Equal("Instructor", exception.EntityName);
        Assert.Contains("Failed to soft delete", exception.Message);
    }

    [Fact]
    public async Task SoftDeleteInstructorAsync_RepositoryFailure_WrapsInEntityServiceException()
    {
        // Arrange
        const int instructorId = 1;
        const string userId = "test-user-id";
        var existingInstructor = new Instructor { Id = instructorId, Firstname = "John", Lastname = "Doe", UserId = userId };
        var expectedException = new InvalidOperationException("Database error");

        _mockUserContextService.Setup(s => s.GetUserIdAsync(_testUserPrincipal)).ReturnsAsync(userId);
        _mockInstructorRepository.Setup(r => r.GetInstructorByIdAsync(instructorId)).ReturnsAsync(existingInstructor);
        _mockUserContextService.Setup(s => s.IsAuthorizedAsync(_testUserPrincipal, userId, RoleConstants.Admin, RoleConstants.Instructor)).ReturnsAsync(true);
        _mockInstructorRepository.Setup(r => r.SoftDeleteInstructorAsync(instructorId)).ThrowsAsync(expectedException);

        // Act
        var exception = await Assert.ThrowsAsync<EntityServiceException>(() => _service.SoftDeleteInstructorAsync(instructorId, _testUserPrincipal));

        // Assert
        Assert.Equal("Instructor", exception.EntityName);
        Assert.Equal($"SoftDeleteInstructor: {instructorId}", exception.Operation);
        Assert.Same(expectedException, exception.InnerException);
    }

    #endregion

    #region HardDeleteInstructorAsync Tests

    [Fact]
    public async Task HardDeleteInstructorAsync_Success_HardDeletesInstructor()
    {
        // Arrange
        const int instructorId = 1;
        const string userId = "test-user-id";
        var existingInstructor = new Instructor { Id = instructorId, Firstname = "John", Lastname = "Doe", UserId = userId };

        _mockUserContextService.Setup(s => s.GetUserIdAsync(_testUserPrincipal)).ReturnsAsync(userId);
        _mockInstructorRepository.Setup(r => r.GetInstructorByIdAsync(instructorId)).ReturnsAsync(existingInstructor);
        _mockUserContextService.Setup(s => s.IsAuthorizedAsync(_testUserPrincipal, userId, "Admin")).ReturnsAsync(true);
        _mockInstructorRepository.Setup(r => r.HardDeleteInstructorAsync(instructorId)).ReturnsAsync(true);
        _mockInstructorRepository.Setup(r => r.SaveChangesAsync()).ReturnsAsync(1);

        // Act
        await _service.HardDeleteInstructorAsync(instructorId, _testUserPrincipal);

        // Assert
        _mockInstructorRepository.Verify(r => r.HardDeleteInstructorAsync(instructorId), Times.Once);
        _mockInstructorRepository.Verify(r => r.SaveChangesAsync(), Times.Once);
    }

    [Fact]
    public async Task HardDeleteInstructorAsync_InvalidId_ThrowsValidationException()
    {
        // Arrange
        const int instructorId = -1;

        // Act & Assert
        await Assert.ThrowsAsync<ValidationException>(() => _service.HardDeleteInstructorAsync(instructorId, _testUserPrincipal));
    }

    [Fact]
    public async Task HardDeleteInstructorAsync_MissingUserId_ThrowsValidationException()
    {
        // Arrange
        const int instructorId = 1;
        _mockUserContextService.Setup(s => s.GetUserIdAsync(_testUserPrincipal)).ReturnsAsync((string?)null);

        // Act & Assert
        await Assert.ThrowsAsync<ValidationException>(() => _service.HardDeleteInstructorAsync(instructorId, _testUserPrincipal));
    }

    [Fact]
    public async Task HardDeleteInstructorAsync_InstructorNotFound_ThrowsEntityNotFoundException()
    {
        // Arrange
        const int instructorId = 999;
        const string userId = "test-user-id";
        _mockUserContextService.Setup(s => s.GetUserIdAsync(_testUserPrincipal)).ReturnsAsync(userId);
        _mockInstructorRepository.Setup(r => r.GetInstructorByIdAsync(instructorId)).ReturnsAsync((Instructor?)null);

        // Act & Assert
        var exception = await Assert.ThrowsAsync<EntityNotFoundException<int>>(() => _service.HardDeleteInstructorAsync(instructorId, _testUserPrincipal));
        Assert.Equal("Instructor", exception.EntityName);
        Assert.Equal(instructorId, exception.Key);
    }

    [Fact]
    public async Task HardDeleteInstructorAsync_Unauthorized_ThrowsEntityUnauthorizedException()
    {
        // Arrange
        const int instructorId = 1;
        const string userId = "test-user-id";
        var existingInstructor = new Instructor { Id = instructorId, Firstname = "John", Lastname = "Doe", UserId = "different-user-id" };
        _mockUserContextService.Setup(s => s.GetUserIdAsync(_testUserPrincipal)).ReturnsAsync(userId);
        _mockInstructorRepository.Setup(r => r.GetInstructorByIdAsync(instructorId)).ReturnsAsync(existingInstructor);
        _mockUserContextService.Setup(s => s.IsAuthorizedAsync(_testUserPrincipal, "different-user-id", "Admin")).ReturnsAsync(false);

        // Act & Assert
        await Assert.ThrowsAsync<EntityUnauthorizedException>(() => _service.HardDeleteInstructorAsync(instructorId, _testUserPrincipal));
    }

    [Fact]
    public async Task HardDeleteInstructorAsync_RepositoryDeleteFailure_ThrowsEntityServiceException()
    {
        // Arrange
        const int instructorId = 1;
        const string userId = "test-user-id";
        var existingInstructor = new Instructor { Id = instructorId, Firstname = "John", Lastname = "Doe", UserId = userId };

        _mockUserContextService.Setup(s => s.GetUserIdAsync(_testUserPrincipal)).ReturnsAsync(userId);
        _mockInstructorRepository.Setup(r => r.GetInstructorByIdAsync(instructorId)).ReturnsAsync(existingInstructor);
        _mockUserContextService.Setup(s => s.IsAuthorizedAsync(_testUserPrincipal, userId, "Admin")).ReturnsAsync(true);
        _mockInstructorRepository.Setup(r => r.HardDeleteInstructorAsync(instructorId)).ReturnsAsync(false);

        // Act & Assert
        var exception = await Assert.ThrowsAsync<EntityServiceException>(() => _service.HardDeleteInstructorAsync(instructorId, _testUserPrincipal));
        Assert.Equal("Instructor", exception.EntityName);
        Assert.Contains("Failed to hard delete", exception.Message);
    }

    [Fact]
    public async Task HardDeleteInstructorAsync_RepositoryFailure_WrapsInEntityServiceException()
    {
        // Arrange
        const int instructorId = 1;
        const string userId = "test-user-id";
        var existingInstructor = new Instructor { Id = instructorId, Firstname = "John", Lastname = "Doe", UserId = userId };
        var expectedException = new InvalidOperationException("Database error");

        _mockUserContextService.Setup(s => s.GetUserIdAsync(_testUserPrincipal)).ReturnsAsync(userId);
        _mockInstructorRepository.Setup(r => r.GetInstructorByIdAsync(instructorId)).ReturnsAsync(existingInstructor);
        _mockUserContextService.Setup(s => s.IsAuthorizedAsync(_testUserPrincipal, userId, "Admin")).ReturnsAsync(true);
        _mockInstructorRepository.Setup(r => r.HardDeleteInstructorAsync(instructorId)).ThrowsAsync(expectedException);

        // Act
        var exception = await Assert.ThrowsAsync<EntityServiceException>(() => _service.HardDeleteInstructorAsync(instructorId, _testUserPrincipal));

        // Assert
        Assert.Equal("Instructor", exception.EntityName);
        Assert.Equal($"HardDeleteInstructor: {instructorId}", exception.Operation);
    }

    #endregion

    #region RestoreInstructorAsync Tests

    [Fact]
    public async Task RestoreInstructorAsync_Success_RestoresInstructor()
    {
        // Arrange
        const int instructorId = 1;
        const string userId = "test-user-id";
        var existingInstructor = new Instructor
        {
            Id = instructorId,
            Firstname = "John",
            Lastname = "Doe",
            UserId = userId,
            DeletedAt = DateTime.UtcNow.AddDays(-1)
        };

        _mockUserContextService.Setup(s => s.GetUserIdAsync(_testUserPrincipal)).ReturnsAsync(userId);
        _mockInstructorRepository.Setup(r => r.GetInstructorByIdIgnoreDeleteStatus(instructorId)).ReturnsAsync(existingInstructor);
        _mockUserContextService.Setup(s => s.IsAuthorizedAsync(_testUserPrincipal, userId, RoleConstants.Admin, RoleConstants.Instructor)).ReturnsAsync(true);
        _mockInstructorRepository.Setup(r => r.RestoreInstructorAsync(instructorId)).ReturnsAsync(true);
        _mockInstructorRepository.Setup(r => r.SaveChangesAsync()).ReturnsAsync(1);

        // Act
        await _service.RestoreInstructorAsync(instructorId, _testUserPrincipal);

        // Assert
        _mockInstructorRepository.Verify(r => r.RestoreInstructorAsync(instructorId), Times.Once);
        _mockInstructorRepository.Verify(r => r.SaveChangesAsync(), Times.Once);
    }

    [Fact]
    public async Task RestoreInstructorAsync_InvalidId_ThrowsValidationException()
    {
        // Arrange
        const int instructorId = -1;

        // Act & Assert
        await Assert.ThrowsAsync<ValidationException>(() => _service.RestoreInstructorAsync(instructorId, _testUserPrincipal));
    }

    [Fact]
    public async Task RestoreInstructorAsync_MissingUserId_ThrowsValidationException()
    {
        // Arrange
        const int instructorId = 1;
        _mockUserContextService.Setup(s => s.GetUserIdAsync(_testUserPrincipal)).ReturnsAsync((string?)null);

        // Act & Assert
        await Assert.ThrowsAsync<ValidationException>(() => _service.RestoreInstructorAsync(instructorId, _testUserPrincipal));
    }

    [Fact]
    public async Task RestoreInstructorAsync_InstructorNotFound_ThrowsEntityNotFoundException()
    {
        // Arrange
        const int instructorId = 999;
        const string userId = "test-user-id";
        _mockUserContextService.Setup(s => s.GetUserIdAsync(_testUserPrincipal)).ReturnsAsync(userId);
        _mockInstructorRepository.Setup(r => r.GetInstructorByIdIgnoreDeleteStatus(instructorId)).ReturnsAsync((Instructor?)null);

        // Act & Assert
        var exception = await Assert.ThrowsAsync<EntityNotFoundException<int>>(() => _service.RestoreInstructorAsync(instructorId, _testUserPrincipal));
        Assert.Equal("Instructor", exception.EntityName);
        Assert.Equal(instructorId, exception.Key);
    }

    [Fact]
    public async Task RestoreInstructorAsync_Unauthorized_ThrowsEntityUnauthorizedException()
    {
        // Arrange
        const int instructorId = 1;
        const string userId = "test-user-id";
        var existingInstructor = new Instructor
        {
            Id = instructorId,
            Firstname = "John",
            Lastname = "Doe",
            UserId = "different-user-id",
            DeletedAt = DateTime.UtcNow.AddDays(-1)
        };
        _mockUserContextService.Setup(s => s.GetUserIdAsync(_testUserPrincipal)).ReturnsAsync(userId);
        _mockInstructorRepository.Setup(r => r.GetInstructorByIdIgnoreDeleteStatus(instructorId)).ReturnsAsync(existingInstructor);
        _mockUserContextService.Setup(s => s.IsAuthorizedAsync(_testUserPrincipal, "different-user-id", RoleConstants.Admin, RoleConstants.Instructor)).ReturnsAsync(false);

        // Act & Assert
        await Assert.ThrowsAsync<EntityUnauthorizedException>(() => _service.RestoreInstructorAsync(instructorId, _testUserPrincipal));
    }

    [Fact]
    public async Task RestoreInstructorAsync_NotDeleted_ThrowsValidationException()
    {
        // Arrange
        const int instructorId = 1;
        const string userId = "test-user-id";
        var existingInstructor = new Instructor
        {
            Id = instructorId,
            Firstname = "John",
            Lastname = "Doe",
            UserId = userId,
            DeletedAt = null // Not deleted
        };

        _mockUserContextService.Setup(s => s.GetUserIdAsync(_testUserPrincipal)).ReturnsAsync(userId);
        _mockInstructorRepository.Setup(r => r.GetInstructorByIdIgnoreDeleteStatus(instructorId)).ReturnsAsync(existingInstructor);
        _mockUserContextService.Setup(s => s.IsAuthorizedAsync(_testUserPrincipal, userId, RoleConstants.Admin, RoleConstants.Instructor)).ReturnsAsync(true);

        // Act & Assert
        await Assert.ThrowsAsync<ValidationException>(() => _service.RestoreInstructorAsync(instructorId, _testUserPrincipal));
    }

    [Fact]
    public async Task RestoreInstructorAsync_RepositoryRestoreFailure_ThrowsEntityServiceException()
    {
        // Arrange
        const int instructorId = 1;
        const string userId = "test-user-id";
        var existingInstructor = new Instructor
        {
            Id = instructorId,
            Firstname = "John",
            Lastname = "Doe",
            UserId = userId,
            DeletedAt = DateTime.UtcNow.AddDays(-1)
        };

        _mockUserContextService.Setup(s => s.GetUserIdAsync(_testUserPrincipal)).ReturnsAsync(userId);
        _mockInstructorRepository.Setup(r => r.GetInstructorByIdIgnoreDeleteStatus(instructorId)).ReturnsAsync(existingInstructor);
        _mockUserContextService.Setup(s => s.IsAuthorizedAsync(_testUserPrincipal, userId, RoleConstants.Admin, RoleConstants.Instructor)).ReturnsAsync(true);
        _mockInstructorRepository.Setup(r => r.RestoreInstructorAsync(instructorId)).ReturnsAsync(false);

        // Act & Assert
        var exception = await Assert.ThrowsAsync<EntityServiceException>(() => _service.RestoreInstructorAsync(instructorId, _testUserPrincipal));
        Assert.Equal("Instructor", exception.EntityName);
        Assert.Contains("Failed to restore", exception.Message);
    }

    [Fact]
    public async Task RestoreInstructorAsync_RepositoryFailure_WrapsInEntityServiceException()
    {
        // Arrange
        const int instructorId = 1;
        const string userId = "test-user-id";
        var existingInstructor = new Instructor
        {
            Id = instructorId,
            Firstname = "John",
            Lastname = "Doe",
            UserId = userId,
            DeletedAt = DateTime.UtcNow.AddDays(-1)
        };
        var expectedException = new InvalidOperationException("Database error");

        _mockUserContextService.Setup(s => s.GetUserIdAsync(_testUserPrincipal)).ReturnsAsync(userId);
        _mockInstructorRepository.Setup(r => r.GetInstructorByIdIgnoreDeleteStatus(instructorId)).ReturnsAsync(existingInstructor);
        _mockUserContextService.Setup(s => s.IsAuthorizedAsync(_testUserPrincipal, userId, RoleConstants.Admin, RoleConstants.Instructor)).ReturnsAsync(true);
        _mockInstructorRepository.Setup(r => r.RestoreInstructorAsync(instructorId)).ThrowsAsync(expectedException);

        // Act
        var exception = await Assert.ThrowsAsync<EntityServiceException>(() => _service.RestoreInstructorAsync(instructorId, _testUserPrincipal));

        // Assert
        Assert.Equal("Instructor", exception.EntityName);
        Assert.Equal($"RestoreInstructor: {instructorId}", exception.Operation);
    }

    #endregion

    #region GetSectionsWithStudentsByInstructorAsync Tests

    [Fact]
    public async Task GetSectionsWithStudentsByInstructorAsync_Success_ReturnsMappedDto()
    {
        // Arrange
        const string userId = "test-user-id";
        const int instructorId = 1;
        var instructorGuid = Guid.NewGuid();
        var sectionGuid = Guid.NewGuid();
        var courseGuid = Guid.NewGuid();
        var subjectGuid = Guid.NewGuid();
        var scheduleGuid = Guid.NewGuid();
        var classroomGuid = Guid.NewGuid();
        var student1Guid = Guid.NewGuid();
        var student2Guid = Guid.NewGuid();

        var instructor = new Instructor
        {
            Id = instructorId,
            Uuid = instructorGuid,
            Firstname = "John",
            Lastname = "Doe",
            UserId = userId
        };

        var course = new Course { Id = 1, Uuid = courseGuid, Name = "Computer Science" };
        var section = new Section
        {
            Id = 1,
            Uuid = sectionGuid,
            Name = "CS-3A",
            CourseId = 1,
            Course = course,
            StudentEnrollments = new List<StudentEnrollment>()
        };

        var subject = new Subject { Id = 1, Uuid = subjectGuid, Name = "Data Structures", Code = "CS301" };
        var classroom = new Classroom { Id = 1, Uuid = classroomGuid, Name = "Room 101" };

        var student1 = new Student
        {
            Id = 1,
            Uuid = student1Guid,
            Firstname = "Alice",
            Lastname = "Smith",
            SectionId = 1,
            IsDeleted = false
        };

        var student2 = new Student
        {
            Id = 2,
            Uuid = student2Guid,
            Firstname = "Bob",
            Lastname = "Johnson",
            SectionId = 2, // Different section - irregular student
            IsDeleted = false
        };

        var enrollment1 = new StudentEnrollment
        {
            StudentId = 1,
            SectionId = 1,
            SubjectId = 1,
            Student = student1,
            IsActive = true,
            EnrollmentType = "Regular"
        };

        var enrollment2 = new StudentEnrollment
        {
            StudentId = 2,
            SectionId = 1,
            SubjectId = 1,
            Student = student2,
            IsActive = true,
            EnrollmentType = "Irregular"
        };

        section.StudentEnrollments = new List<StudentEnrollment> { enrollment1, enrollment2 };

        var schedule = new Schedules
        {
            Id = 1,
            Uuid = scheduleGuid,
            InstructorId = instructorId,
            SectionId = 1,
            SubjectId = 1,
            ClassroomId = 1,
            DayOfWeek = "Monday",
            TimeIn = TimeOnly.FromTimeSpan(TimeSpan.FromHours(8)),
            TimeOut = TimeOnly.FromTimeSpan(TimeSpan.FromHours(10)),
            Section = section,
            Subject = subject,
            Classroom = classroom,
            Instructor = instructor
        };

        var schedules = new List<Schedules> { schedule };

        _mockUserContextService.Setup(s => s.GetUserIdAsync(_testUserPrincipal)).ReturnsAsync(userId);
        _mockInstructorRepository.Setup(r => r.GetInstructorByUserIdAsync(userId)).ReturnsAsync(instructor);
        _mockInstructorRepository.Setup(r => r.GetSchedulesWithRelatedDataByInstructorIdAsync(instructorId))
            .ReturnsAsync(schedules);
        _mockInstructorRepository.Setup(r => r.GetRegularStudentsBySectionIdAsync(section.Id))
            .ReturnsAsync(new List<Student> { student1 });

        // Act
        var result = await _service.GetSectionsWithStudentsByInstructorAsync(_testUserPrincipal);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(instructorGuid, result.InstructorId);
        Assert.Equal("John", result.InstructorFirstname);
        Assert.Equal("Doe", result.InstructorLastname);
        Assert.Single(result.Sections);

        var sectionDto = result.Sections.First();
        Assert.Equal(sectionGuid, sectionDto.SectionId);
        Assert.Equal("CS-3A", sectionDto.SectionName);
        Assert.Equal(courseGuid, sectionDto.CourseId);
        Assert.Equal("Computer Science", sectionDto.CourseName);
        Assert.Single(sectionDto.Subjects);

        var subjectDto = sectionDto.Subjects.First();
        Assert.Equal(subjectGuid, subjectDto.SubjectId);
        Assert.Equal("Data Structures", subjectDto.SubjectName);
        Assert.Equal("CS301", subjectDto.SubjectCode);
        Assert.Equal(scheduleGuid, subjectDto.ScheduleId);
        Assert.Equal("Monday", subjectDto.DayOfWeek);
        Assert.Equal(classroomGuid, subjectDto.ClassroomId);
        Assert.Equal("Room 101", subjectDto.ClassroomName);
        Assert.Equal(2, subjectDto.Students.Count);

        var regularStudent = subjectDto.Students.First(s => s.StudentId == student1Guid);
        Assert.Equal(student1Guid, regularStudent.StudentId);
        Assert.Equal("Alice", regularStudent.Firstname);
        Assert.Equal("Smith", regularStudent.Lastname);
        Assert.True(regularStudent.IsRegular);
        Assert.Equal("Regular", regularStudent.EnrollmentType);

        var irregularStudent = subjectDto.Students.First(s => s.StudentId == student2Guid);
        Assert.Equal(student2Guid, irregularStudent.StudentId);
        Assert.Equal("Bob", irregularStudent.Firstname);
        Assert.Equal("Johnson", irregularStudent.Lastname);
        Assert.False(irregularStudent.IsRegular);
        Assert.Equal("Irregular", irregularStudent.EnrollmentType);

        _mockInstructorRepository.Verify(r => r.GetSchedulesWithRelatedDataByInstructorIdAsync(instructorId), Times.Once);
    }

    [Fact]
    public async Task GetSectionsWithStudentsByInstructorAsync_RegularAndIrregularStudents_AggregatesCorrectly()
    {
        // Arrange
        const string userId = "test-user-id";
        const int instructorId = 1;

        var instructor = new Instructor { Id = instructorId, Uuid = Guid.NewGuid(), Firstname = "John", Lastname = "Doe", UserId = userId };
        var course = new Course { Id = 1, Uuid = Guid.NewGuid(), Name = "Computer Science" };
        var section = new Section
        {
            Id = 1,
            Uuid = Guid.NewGuid(),
            Name = "CS-3A",
            CourseId = 1,
            Course = course,
            StudentEnrollments = new List<StudentEnrollment>()
        };

        var subject = new Subject { Id = 1, Uuid = Guid.NewGuid(), Name = "Mathematics", Code = "MATH101" };
        var classroom = new Classroom { Id = 1, Uuid = Guid.NewGuid(), Name = "Room 101" };

        // Regular student (SectionId matches)
        var regularStudent = new Student
        {
            Id = 1,
            Uuid = Guid.NewGuid(),
            Firstname = "Regular",
            Lastname = "Student",
            SectionId = 1,
            IsDeleted = false
        };

        // Irregular student (SectionId different)
        var irregularStudent = new Student
        {
            Id = 2,
            Uuid = Guid.NewGuid(),
            Firstname = "Irregular",
            Lastname = "Student",
            SectionId = 2,
            IsDeleted = false
        };

        var enrollment1 = new StudentEnrollment
        {
            StudentId = 1,
            SectionId = 1,
            SubjectId = 1,
            Student = regularStudent,
            IsActive = true,
            EnrollmentType = "Regular"
        };

        var enrollment2 = new StudentEnrollment
        {
            StudentId = 2,
            SectionId = 1,
            SubjectId = 1,
            Student = irregularStudent,
            IsActive = true,
            EnrollmentType = "Irregular"
        };

        section.StudentEnrollments = new List<StudentEnrollment> { enrollment1, enrollment2 };

        var schedule = new Schedules
        {
            Id = 1,
            Uuid = Guid.NewGuid(),
            InstructorId = instructorId,
            SectionId = 1,
            SubjectId = 1,
            ClassroomId = 1,
            DayOfWeek = "Monday",
            TimeIn = TimeOnly.FromTimeSpan(TimeSpan.FromHours(8)),
            TimeOut = TimeOnly.FromTimeSpan(TimeSpan.FromHours(10)),
            Section = section,
            Subject = subject,
            Classroom = classroom,
            Instructor = instructor
        };

        _mockUserContextService.Setup(s => s.GetUserIdAsync(_testUserPrincipal)).ReturnsAsync(userId);
        _mockInstructorRepository.Setup(r => r.GetInstructorByUserIdAsync(userId)).ReturnsAsync(instructor);
        _mockInstructorRepository.Setup(r => r.GetSchedulesWithRelatedDataByInstructorIdAsync(instructorId))
            .ReturnsAsync(new List<Schedules> { schedule });
        _mockInstructorRepository.Setup(r => r.GetRegularStudentsBySectionIdAsync(section.Id))
            .ReturnsAsync(new List<Student> { regularStudent });

        // Act
        var result = await _service.GetSectionsWithStudentsByInstructorAsync(_testUserPrincipal);

        // Assert
        Assert.NotNull(result);
        var students = result.Sections.First().Subjects.First().Students;
        Assert.Equal(2, students.Count);

        var regular = students.First(s => s.StudentId == regularStudent.Uuid);
        Assert.True(regular.IsRegular);
        Assert.Equal("Regular", regular.EnrollmentType);

        var irregular = students.First(s => s.StudentId == irregularStudent.Uuid);
        Assert.False(irregular.IsRegular);
        Assert.Equal("Irregular", irregular.EnrollmentType);
    }

    [Fact]
    public async Task GetSectionsWithStudentsByInstructorAsync_MissingUserId_ThrowsEntityNotFoundException()
    {
        // Arrange
        _mockUserContextService.Setup(s => s.GetUserIdAsync(_testUserPrincipal)).ReturnsAsync((string?)null);

        // Act & Assert
        var exception = await Assert.ThrowsAsync<EntityNotFoundException<string>>(
            () => _service.GetSectionsWithStudentsByInstructorAsync(_testUserPrincipal));
        Assert.Equal("User", exception.EntityName);
    }

    [Fact]
    public async Task GetSectionsWithStudentsByInstructorAsync_InstructorNotFound_ThrowsEntityNotFoundException()
    {
        // Arrange
        const string userId = "test-user-id";
        _mockUserContextService.Setup(s => s.GetUserIdAsync(_testUserPrincipal)).ReturnsAsync(userId);
        _mockInstructorRepository.Setup(r => r.GetInstructorByUserIdAsync(userId)).ReturnsAsync((Instructor?)null);

        // Act & Assert
        var exception = await Assert.ThrowsAsync<EntityNotFoundException<string>>(
            () => _service.GetSectionsWithStudentsByInstructorAsync(_testUserPrincipal));
        Assert.Equal("Instructor", exception.EntityName);
    }

    [Fact]
    public async Task GetSectionsWithStudentsByInstructorAsync_NoSchedules_ReturnsEmptySections()
    {
        // Arrange
        const string userId = "test-user-id";
        const int instructorId = 1;
        var instructor = new Instructor
        {
            Id = instructorId,
            Uuid = Guid.NewGuid(),
            Firstname = "John",
            Lastname = "Doe",
            UserId = userId
        };

        _mockUserContextService.Setup(s => s.GetUserIdAsync(_testUserPrincipal)).ReturnsAsync(userId);
        _mockInstructorRepository.Setup(r => r.GetInstructorByUserIdAsync(userId)).ReturnsAsync(instructor);
        _mockInstructorRepository.Setup(r => r.GetSchedulesWithRelatedDataByInstructorIdAsync(instructorId))
            .ReturnsAsync(new List<Schedules>());

        // Act
        var result = await _service.GetSectionsWithStudentsByInstructorAsync(_testUserPrincipal);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(instructor.Uuid, result.InstructorId);
        Assert.Equal("John", result.InstructorFirstname);
        Assert.Equal("Doe", result.InstructorLastname);
        Assert.Empty(result.Sections);
    }

    [Fact]
    public async Task GetSectionsWithStudentsByInstructorAsync_SoftDeletedStudents_ExcludesDeletedStudents()
    {
        // Arrange
        const string userId = "test-user-id";
        const int instructorId = 1;

        var instructor = new Instructor { Id = instructorId, Uuid = Guid.NewGuid(), Firstname = "John", Lastname = "Doe", UserId = userId };
        var course = new Course { Id = 1, Uuid = Guid.NewGuid(), Name = "Computer Science" };
        var section = new Section
        {
            Id = 1,
            Uuid = Guid.NewGuid(),
            Name = "CS-3A",
            CourseId = 1,
            Course = course,
            StudentEnrollments = new List<StudentEnrollment>()
        };

        var subject = new Subject { Id = 1, Uuid = Guid.NewGuid(), Name = "Mathematics", Code = "MATH101" };
        var classroom = new Classroom { Id = 1, Uuid = Guid.NewGuid(), Name = "Room 101" };

        // Active student
        var activeStudent = new Student
        {
            Id = 1,
            Uuid = Guid.NewGuid(),
            Firstname = "Active",
            Lastname = "Student",
            SectionId = 1,
            IsDeleted = false
        };

        // Soft-deleted student
        var deletedStudent = new Student
        {
            Id = 2,
            Uuid = Guid.NewGuid(),
            Firstname = "Deleted",
            Lastname = "Student",
            SectionId = 1,
            IsDeleted = true,
            DeletedAt = DateTime.UtcNow.AddDays(-1)
        };

        var enrollment1 = new StudentEnrollment
        {
            StudentId = 1,
            SectionId = 1,
            SubjectId = 1,
            Student = activeStudent,
            IsActive = true,
            EnrollmentType = "Regular"
        };

        var enrollment2 = new StudentEnrollment
        {
            StudentId = 2,
            SectionId = 1,
            SubjectId = 1,
            Student = deletedStudent,
            IsActive = true,
            EnrollmentType = "Regular"
        };

        section.StudentEnrollments = new List<StudentEnrollment> { enrollment1, enrollment2 };

        var schedule = new Schedules
        {
            Id = 1,
            Uuid = Guid.NewGuid(),
            InstructorId = instructorId,
            SectionId = 1,
            SubjectId = 1,
            ClassroomId = 1,
            DayOfWeek = "Monday",
            TimeIn = TimeOnly.FromTimeSpan(TimeSpan.FromHours(8)),
            TimeOut = TimeOnly.FromTimeSpan(TimeSpan.FromHours(10)),
            Section = section,
            Subject = subject,
            Classroom = classroom,
            Instructor = instructor
        };

        _mockUserContextService.Setup(s => s.GetUserIdAsync(_testUserPrincipal)).ReturnsAsync(userId);
        _mockInstructorRepository.Setup(r => r.GetInstructorByUserIdAsync(userId)).ReturnsAsync(instructor);
        _mockInstructorRepository.Setup(r => r.GetSchedulesWithRelatedDataByInstructorIdAsync(instructorId))
            .ReturnsAsync(new List<Schedules> { schedule });
        _mockInstructorRepository.Setup(r => r.GetRegularStudentsBySectionIdAsync(section.Id))
            .ReturnsAsync(new List<Student> { activeStudent });

        // Act
        var result = await _service.GetSectionsWithStudentsByInstructorAsync(_testUserPrincipal);

        // Assert
        Assert.NotNull(result);
        var students = result.Sections.First().Subjects.First().Students;
        Assert.Single(students);
        Assert.Equal(activeStudent.Uuid, students.First().StudentId);
        Assert.Equal("Active", students.First().Firstname);
    }

    [Fact]
    public async Task GetSectionsWithStudentsByInstructorAsync_MultipleSubjectsForSameSection_ReturnsAllSubjects()
    {
        // Arrange
        const string userId = "test-user-id";
        const int instructorId = 1;

        var instructor = new Instructor { Id = instructorId, Uuid = Guid.NewGuid(), Firstname = "John", Lastname = "Doe", UserId = userId };
        var course = new Course { Id = 1, Uuid = Guid.NewGuid(), Name = "Computer Science" };
        var section = new Section
        {
            Id = 1,
            Uuid = Guid.NewGuid(),
            Name = "CS-3A",
            CourseId = 1,
            Course = course,
            StudentEnrollments = new List<StudentEnrollment>()
        };

        var subject1 = new Subject { Id = 1, Uuid = Guid.NewGuid(), Name = "Mathematics", Code = "MATH101" };
        var subject2 = new Subject { Id = 2, Uuid = Guid.NewGuid(), Name = "Physics", Code = "PHYS101" };
        var classroom = new Classroom { Id = 1, Uuid = Guid.NewGuid(), Name = "Room 101" };

        var student = new Student
        {
            Id = 1,
            Uuid = Guid.NewGuid(),
            Firstname = "Alice",
            Lastname = "Smith",
            SectionId = 1,
            IsDeleted = false
        };

        var enrollment1 = new StudentEnrollment
        {
            StudentId = 1,
            SectionId = 1,
            SubjectId = 1,
            Student = student,
            IsActive = true,
            EnrollmentType = "Regular"
        };

        var enrollment2 = new StudentEnrollment
        {
            StudentId = 1,
            SectionId = 1,
            SubjectId = 2,
            Student = student,
            IsActive = true,
            EnrollmentType = "Regular"
        };

        section.StudentEnrollments = new List<StudentEnrollment> { enrollment1, enrollment2 };

        var schedule1 = new Schedules
        {
            Id = 1,
            Uuid = Guid.NewGuid(),
            InstructorId = instructorId,
            SectionId = 1,
            SubjectId = 1,
            ClassroomId = 1,
            DayOfWeek = "Monday",
            TimeIn = TimeOnly.FromTimeSpan(TimeSpan.FromHours(8)),
            TimeOut = TimeOnly.FromTimeSpan(TimeSpan.FromHours(10)),
            Section = section,
            Subject = subject1,
            Classroom = classroom,
            Instructor = instructor
        };

        var schedule2 = new Schedules
        {
            Id = 2,
            Uuid = Guid.NewGuid(),
            InstructorId = instructorId,
            SectionId = 1,
            SubjectId = 2,
            ClassroomId = 1,
            DayOfWeek = "Tuesday",
            TimeIn = TimeOnly.FromTimeSpan(TimeSpan.FromHours(10)),
            TimeOut = TimeOnly.FromTimeSpan(TimeSpan.FromHours(12)),
            Section = section,
            Subject = subject2,
            Classroom = classroom,
            Instructor = instructor
        };

        _mockUserContextService.Setup(s => s.GetUserIdAsync(_testUserPrincipal)).ReturnsAsync(userId);
        _mockInstructorRepository.Setup(r => r.GetInstructorByUserIdAsync(userId)).ReturnsAsync(instructor);
        _mockInstructorRepository.Setup(r => r.GetSchedulesWithRelatedDataByInstructorIdAsync(instructorId))
            .ReturnsAsync(new List<Schedules> { schedule1, schedule2 });
        _mockInstructorRepository.Setup(r => r.GetRegularStudentsBySectionIdAsync(section.Id))
            .ReturnsAsync(new List<Student> { student });

        // Act
        var result = await _service.GetSectionsWithStudentsByInstructorAsync(_testUserPrincipal);

        // Assert
        Assert.NotNull(result);
        Assert.Single(result.Sections);
        var sectionDto = result.Sections.First();
        Assert.Equal(2, sectionDto.Subjects.Count);

        var mathSubject = sectionDto.Subjects.First(s => s.SubjectCode == "MATH101");
        Assert.Equal("Mathematics", mathSubject.SubjectName);
        Assert.Equal("Monday", mathSubject.DayOfWeek);

        var physicsSubject = sectionDto.Subjects.First(s => s.SubjectCode == "PHYS101");
        Assert.Equal("Physics", physicsSubject.SubjectName);
        Assert.Equal("Tuesday", physicsSubject.DayOfWeek);
    }

    [Fact]
    public async Task GetSectionsWithStudentsByInstructorAsync_RepositoryFailure_WrapsInEntityServiceException()
    {
        // Arrange
        const string userId = "test-user-id";
        const int instructorId = 1;
        var instructor = new Instructor { Id = instructorId, Uuid = Guid.NewGuid(), Firstname = "John", Lastname = "Doe", UserId = userId };
        var expectedException = new InvalidOperationException("Database error");

        _mockUserContextService.Setup(s => s.GetUserIdAsync(_testUserPrincipal)).ReturnsAsync(userId);
        _mockInstructorRepository.Setup(r => r.GetInstructorByUserIdAsync(userId)).ReturnsAsync(instructor);
        _mockInstructorRepository.Setup(r => r.GetSchedulesWithRelatedDataByInstructorIdAsync(instructorId))
            .ThrowsAsync(expectedException);

        // Act
        var exception = await Assert.ThrowsAsync<EntityServiceException>(
            () => _service.GetSectionsWithStudentsByInstructorAsync(_testUserPrincipal));

        // Assert
        Assert.Equal("Instructor", exception.EntityName);
        Assert.Equal("GetSectionsWithStudentsByInstructor", exception.Operation);
        Assert.Same(expectedException, exception.InnerException);
    }

    [Fact]
    public async Task GetSectionsWithStudentsByInstructorAsync_PrimarySectionStudentsWithoutEnrollmentRows_AreIncluded()
    {
        // Arrange
        const string userId = "test-user-id";
        const int instructorId = 1;

        var instructor = new Instructor { Id = instructorId, Uuid = Guid.NewGuid(), Firstname = "John", Lastname = "Doe", UserId = userId };
        var course = new Course { Id = 1, Uuid = Guid.NewGuid(), Name = "Computer Science" };
        var section = new Section
        {
            Id = 1,
            Uuid = Guid.NewGuid(),
            Name = "CS-3A",
            CourseId = 1,
            Course = course,
            StudentEnrollments = new List<StudentEnrollment>()
        };

        var subject = new Subject { Id = 1, Uuid = Guid.NewGuid(), Name = "Mathematics", Code = "MATH101" };
        var classroom = new Classroom { Id = 1, Uuid = Guid.NewGuid(), Name = "Room 101" };

        var regularStudent = new Student
        {
            Id = 1,
            Uuid = Guid.NewGuid(),
            Firstname = "Regular",
            Lastname = "Student",
            SectionId = 1,
            IsDeleted = false
        };

        var schedule = new Schedules
        {
            Id = 1,
            Uuid = Guid.NewGuid(),
            InstructorId = instructorId,
            SectionId = 1,
            SubjectId = 1,
            ClassroomId = 1,
            DayOfWeek = "Monday",
            TimeIn = TimeOnly.FromTimeSpan(TimeSpan.FromHours(8)),
            TimeOut = TimeOnly.FromTimeSpan(TimeSpan.FromHours(10)),
            Section = section,
            Subject = subject,
            Classroom = classroom,
            Instructor = instructor
        };

        _mockUserContextService.Setup(s => s.GetUserIdAsync(_testUserPrincipal)).ReturnsAsync(userId);
        _mockInstructorRepository.Setup(r => r.GetInstructorByUserIdAsync(userId)).ReturnsAsync(instructor);
        _mockInstructorRepository.Setup(r => r.GetSchedulesWithRelatedDataByInstructorIdAsync(instructorId))
            .ReturnsAsync(new List<Schedules> { schedule });
        _mockInstructorRepository.Setup(r => r.GetRegularStudentsBySectionIdAsync(section.Id))
            .ReturnsAsync(new List<Student> { regularStudent });

        // Act
        var result = await _service.GetSectionsWithStudentsByInstructorAsync(_testUserPrincipal);

        // Assert
        var students = result.Sections.First().Subjects.First().Students;
        var returnedStudent = Assert.Single(students);
        Assert.Equal(regularStudent.Uuid, returnedStudent.StudentId);
        Assert.True(returnedStudent.IsRegular);
        Assert.Equal(EnrollmentTypeConstants.Regular, returnedStudent.EnrollmentType);
    }

    #endregion

    #region GetInstructorSectionsOverviewAsync Tests

    [Fact]
    public async Task GetInstructorSectionsOverviewAsync_Success_ReturnsOverviewDtos()
    {
        // Arrange
        const string userId = "test-user-id";
        const int instructorId = 1;
        var instructor = new Instructor { Id = instructorId, Firstname = "John", Lastname = "Doe", UserId = userId };

        var course = new Course { Id = 1, Uuid = Guid.NewGuid(), Name = "Computer Science" };
        var section1 = new Section
        {
            Id = 1,
            Uuid = Guid.NewGuid(),
            Name = "CS-3A",
            CourseId = 1,
            Course = course,
            StudentEnrollments = new List<StudentEnrollment>()
        };
        var section2 = new Section { Id = 2, Uuid = Guid.NewGuid(), Name = "CS-3B", CourseId = 1, Course = course };

        var subject = new Subject { Id = 1, Uuid = Guid.NewGuid(), Name = "Data Structures", Code = "CS301" };
        var classroom = new Classroom { Id = 1, Uuid = Guid.NewGuid(), Name = "Room 101" };

        var schedule1 = new Schedules
        {
            Id = 1, Uuid = Guid.NewGuid(), InstructorId = instructorId, SectionId = 1, SubjectId = 1,
            DayOfWeek = "Monday", TimeIn = TimeOnly.FromTimeSpan(TimeSpan.FromHours(8)), TimeOut = TimeOnly.FromTimeSpan(TimeSpan.FromHours(10)),
            Section = section1, Subject = subject, Classroom = classroom, Instructor = instructor
        };

        var schedule2 = new Schedules
        {
            Id = 2, Uuid = Guid.NewGuid(), InstructorId = instructorId, SectionId = 1, SubjectId = 2,
            DayOfWeek = "Tuesday", TimeIn = TimeOnly.FromTimeSpan(TimeSpan.FromHours(10)), TimeOut = TimeOnly.FromTimeSpan(TimeSpan.FromHours(12)),
            Section = section1, Subject = subject, Classroom = classroom, Instructor = instructor
        };

        var student1 = new Student { Id = 1, Uuid = Guid.NewGuid(), Firstname = "Alice", Lastname = "Smith", SectionId = 1, IsDeleted = false };
        var student2 = new Student { Id = 2, Uuid = Guid.NewGuid(), Firstname = "Bob", Lastname = "Johnson", SectionId = 2, IsDeleted = false };
        var irregularStudent = new Student { Id = 3, Uuid = Guid.NewGuid(), Firstname = "Cara", Lastname = "Lopez", SectionId = 99, IsDeleted = false };

        section1.StudentEnrollments.Add(new StudentEnrollment
        {
            StudentId = irregularStudent.Id,
            SectionId = section1.Id,
            SubjectId = schedule1.SubjectId,
            Student = irregularStudent,
            IsActive = true,
            EnrollmentType = EnrollmentTypeConstants.Irregular
        });

        _mockUserContextService.Setup(s => s.GetUserIdAsync(_testUserPrincipal)).ReturnsAsync(userId);
        _mockInstructorRepository.Setup(r => r.GetInstructorByUserIdAsync(userId)).ReturnsAsync(instructor);
        _mockInstructorRepository.Setup(r => r.GetHandledSectionsByInstructorIdAsync(instructorId))
            .ReturnsAsync(new List<Section> { section1, section2 });
        _mockInstructorRepository.Setup(r => r.GetHandledClassesBySectionAndInstructorAsync(1, instructorId))
            .ReturnsAsync(new List<Schedules> { schedule1, schedule2 });
        _mockInstructorRepository.Setup(r => r.GetHandledClassesBySectionAndInstructorAsync(2, instructorId))
            .ReturnsAsync(new List<Schedules>());
        _mockInstructorRepository.Setup(r => r.GetRegularStudentsBySectionIdAsync(1))
            .ReturnsAsync(new List<Student> { student1 });
        _mockInstructorRepository.Setup(r => r.GetRegularStudentsBySectionIdAsync(2))
            .ReturnsAsync(new List<Student> { student2 });

        // Act
        var result = await _service.GetInstructorSectionsOverviewAsync(_testUserPrincipal);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(2, result.Count);

        var overview1 = result.First(s => s.SectionId == section1.Uuid);
        Assert.Equal("CS-3A", overview1.SectionName);
        Assert.Equal(2, overview1.HandledClassCount);
        Assert.Equal(2, overview1.UniqueStudentCount);

        var overview2 = result.First(s => s.SectionId == section2.Uuid);
        Assert.Equal("CS-3B", overview2.SectionName);
        Assert.Equal(0, overview2.HandledClassCount);
        Assert.Equal(0, overview2.UniqueStudentCount);
    }

    [Fact]
    public async Task GetInstructorSectionDetailAsync_MultipleHandledClasses_LoadsRegularStudentsOnce()
    {
        // Arrange
        const string userId = "test-user-id";
        const int instructorId = 1;
        const int sectionId = 1;

        var instructor = new Instructor { Id = instructorId, Firstname = "John", Lastname = "Doe", UserId = userId };
        var course = new Course { Id = 1, Uuid = Guid.NewGuid(), Name = "Computer Science" };
        var section = new Section
        {
            Id = sectionId,
            Uuid = Guid.NewGuid(),
            Name = "CS-3A",
            CourseId = 1,
            Course = course,
            StudentEnrollments = new List<StudentEnrollment>()
        };

        var classroom = new Classroom { Id = 1, Uuid = Guid.NewGuid(), Name = "Room 101" };
        var subject1 = new Subject { Id = 1, Uuid = Guid.NewGuid(), Name = "Data Structures", Code = "CS301" };
        var subject2 = new Subject { Id = 2, Uuid = Guid.NewGuid(), Name = "Algorithms", Code = "CS302" };
        var regularStudent = new Student { Id = 1, Uuid = Guid.NewGuid(), Firstname = "Alice", Lastname = "Smith", SectionId = sectionId, IsDeleted = false };

        var schedules = new List<Schedules>
        {
            new()
            {
                Id = 1,
                Uuid = Guid.NewGuid(),
                InstructorId = instructorId,
                SectionId = sectionId,
                SubjectId = subject1.Id,
                ClassroomId = classroom.Id,
                DayOfWeek = "Monday",
                TimeIn = TimeOnly.FromTimeSpan(TimeSpan.FromHours(8)),
                TimeOut = TimeOnly.FromTimeSpan(TimeSpan.FromHours(10)),
                Section = section,
                Subject = subject1,
                Classroom = classroom,
                Instructor = instructor
            },
            new()
            {
                Id = 2,
                Uuid = Guid.NewGuid(),
                InstructorId = instructorId,
                SectionId = sectionId,
                SubjectId = subject2.Id,
                ClassroomId = classroom.Id,
                DayOfWeek = "Tuesday",
                TimeIn = TimeOnly.FromTimeSpan(TimeSpan.FromHours(10)),
                TimeOut = TimeOnly.FromTimeSpan(TimeSpan.FromHours(12)),
                Section = section,
                Subject = subject2,
                Classroom = classroom,
                Instructor = instructor
            }
        };

        _mockUserContextService.Setup(s => s.GetUserIdAsync(_testUserPrincipal)).ReturnsAsync(userId);
        _mockInstructorRepository.Setup(r => r.GetInstructorByUserIdAsync(userId)).ReturnsAsync(instructor);
        _mockInstructorRepository.Setup(r => r.IsInstructorHandlingSectionAsync(instructorId, sectionId)).ReturnsAsync(true);
        _mockInstructorRepository.Setup(r => r.GetHandledClassesBySectionAndInstructorAsync(sectionId, instructorId))
            .ReturnsAsync(schedules);
        _mockInstructorRepository.Setup(r => r.GetRegularStudentsBySectionIdAsync(sectionId))
            .ReturnsAsync(new List<Student> { regularStudent });
        _mockInstructorRepository.Setup(r => r.GetHomeSectionStudentsAsync(sectionId))
            .ReturnsAsync(new List<Student> { regularStudent });

        // Act
        var result = await _service.GetInstructorSectionDetailAsync(_testUserPrincipal, sectionId);

        // Assert
        Assert.Equal(2, result.HandledClassCount);
        _mockInstructorRepository.Verify(r => r.GetRegularStudentsBySectionIdAsync(sectionId), Times.Once);
    }

    [Fact]
    public async Task GetInstructorSectionsOverviewAsync_MissingUserId_ThrowsEntityNotFoundException()
    {
        // Arrange
        _mockUserContextService.Setup(s => s.GetUserIdAsync(_testUserPrincipal)).ReturnsAsync((string?)null);

        // Act & Assert
        var exception = await Assert.ThrowsAsync<EntityNotFoundException<string>>(
            () => _service.GetInstructorSectionsOverviewAsync(_testUserPrincipal));
        Assert.Equal("User", exception.EntityName);
    }

    [Fact]
    public async Task GetInstructorSectionsOverviewAsync_InstructorNotFound_ThrowsEntityNotFoundException()
    {
        // Arrange
        const string userId = "test-user-id";
        _mockUserContextService.Setup(s => s.GetUserIdAsync(_testUserPrincipal)).ReturnsAsync(userId);
        _mockInstructorRepository.Setup(r => r.GetInstructorByUserIdAsync(userId)).ReturnsAsync((Instructor?)null);

        // Act & Assert
        var exception = await Assert.ThrowsAsync<EntityNotFoundException<string>>(
            () => _service.GetInstructorSectionsOverviewAsync(_testUserPrincipal));
        Assert.Equal("Instructor", exception.EntityName);
    }

    [Fact]
    public async Task GetInstructorSectionsOverviewAsync_RepositoryFailure_WrapsInEntityServiceException()
    {
        // Arrange
        const string userId = "test-user-id";
        const int instructorId = 1;
        var instructor = new Instructor { Id = instructorId, Firstname = "John", Lastname = "Doe", UserId = userId };
        var expectedException = new InvalidOperationException("Database error");

        _mockUserContextService.Setup(s => s.GetUserIdAsync(_testUserPrincipal)).ReturnsAsync(userId);
        _mockInstructorRepository.Setup(r => r.GetInstructorByUserIdAsync(userId)).ReturnsAsync(instructor);
        _mockInstructorRepository.Setup(r => r.GetHandledSectionsByInstructorIdAsync(instructorId))
            .ThrowsAsync(expectedException);

        // Act
        var exception = await Assert.ThrowsAsync<EntityServiceException>(
            () => _service.GetInstructorSectionsOverviewAsync(_testUserPrincipal));

        // Assert
        Assert.Equal("Instructor", exception.EntityName);
        Assert.Equal("GetInstructorSectionsOverview", exception.Operation);
        Assert.Same(expectedException, exception.InnerException);
    }

    #endregion

    #region GetInstructorSectionDetailAsync Tests

    [Fact]
    public async Task GetInstructorSectionDetailAsync_Success_ReturnsSectionDetailDto()
    {
        // Arrange
        const string userId = "test-user-id";
        const int instructorId = 1;
        const int sectionId = 1;
        var instructor = new Instructor { Id = instructorId, Firstname = "John", Lastname = "Doe", UserId = userId };

        var course = new Course { Id = 1, Uuid = Guid.NewGuid(), Name = "Computer Science" };
        var section = new Section
        {
            Id = sectionId, Uuid = Guid.NewGuid(), Name = "CS-3A", CourseId = 1, Course = course,
            StudentEnrollments = new List<StudentEnrollment>()
        };

        var subject = new Subject { Id = 1, Uuid = Guid.NewGuid(), Name = "Data Structures", Code = "CS301" };
        var classroom = new Classroom { Id = 1, Uuid = Guid.NewGuid(), Name = "Room 101" };

        var schedule = new Schedules
        {
            Id = 1, Uuid = Guid.NewGuid(), InstructorId = instructorId, SectionId = sectionId, SubjectId = 1, ClassroomId = 1,
            DayOfWeek = "Monday", TimeIn = TimeOnly.FromTimeSpan(TimeSpan.FromHours(8)), TimeOut = TimeOnly.FromTimeSpan(TimeSpan.FromHours(10)),
            Section = section, Subject = subject, Classroom = classroom, Instructor = instructor
        };

        var regularStudent = new Student { Id = 1, Uuid = Guid.NewGuid(), Firstname = "Alice", Lastname = "Smith", SectionId = sectionId, IsDeleted = false };

        _mockUserContextService.Setup(s => s.GetUserIdAsync(_testUserPrincipal)).ReturnsAsync(userId);
        _mockInstructorRepository.Setup(r => r.GetInstructorByUserIdAsync(userId)).ReturnsAsync(instructor);
        _mockInstructorRepository.Setup(r => r.IsInstructorHandlingSectionAsync(instructorId, sectionId)).ReturnsAsync(true);
        _mockInstructorRepository.Setup(r => r.GetHandledClassesBySectionAndInstructorAsync(sectionId, instructorId))
            .ReturnsAsync(new List<Schedules> { schedule });
        _mockInstructorRepository.Setup(r => r.GetRegularStudentsBySectionIdAsync(sectionId))
            .ReturnsAsync(new List<Student> { regularStudent });
        _mockInstructorRepository.Setup(r => r.GetHomeSectionStudentsAsync(sectionId))
            .ReturnsAsync(new List<Student> { regularStudent });

        // Act
        var result = await _service.GetInstructorSectionDetailAsync(_testUserPrincipal, sectionId);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(section.Uuid, result.SectionId);
        Assert.Equal("CS-3A", result.SectionName);
        Assert.Equal(1, result.HandledClassCount);
        Assert.Single(result.HandledClasses);
        Assert.Equal("Data Structures", result.HandledClasses.First().SubjectName);
        Assert.Single(result.HomeSectionStudents);
    }

    [Fact]
    public async Task GetInstructorSectionDetailAsync_FingerprintData_UsesBulkLookupAndMapsDeviceName()
    {
        // Arrange
        const string userId = "test-user-id";
        const int instructorId = 1;
        const int sectionId = 1;
        var instructor = new Instructor { Id = instructorId, Firstname = "John", Lastname = "Doe", UserId = userId };

        var course = new Course { Id = 1, Uuid = Guid.NewGuid(), Name = "Computer Science" };
        var section = new Section
        {
            Id = sectionId,
            Uuid = Guid.NewGuid(),
            Name = "CS-3A",
            CourseId = course.Id,
            Course = course,
            StudentEnrollments = new List<StudentEnrollment>()
        };

        var subject = new Subject { Id = 1, Uuid = Guid.NewGuid(), Name = "Data Structures", Code = "CS301" };
        var classroom = new Classroom { Id = 1, Uuid = Guid.NewGuid(), Name = "Room 101" };
        var schedule = new Schedules
        {
            Id = 1,
            Uuid = Guid.NewGuid(),
            InstructorId = instructorId,
            SectionId = sectionId,
            SubjectId = subject.Id,
            ClassroomId = classroom.Id,
            DayOfWeek = "Monday",
            TimeIn = TimeOnly.FromTimeSpan(TimeSpan.FromHours(8)),
            TimeOut = TimeOnly.FromTimeSpan(TimeSpan.FromHours(10)),
            Section = section,
            Subject = subject,
            Classroom = classroom,
            Instructor = instructor
        };

        var regularStudent = new Student
        {
            Id = 1,
            Uuid = Guid.NewGuid(),
            Firstname = "Alice",
            Lastname = "Smith",
            SectionId = sectionId,
            IsDeleted = false,
            UserId = "student-user-id"
        };

        var fingerprint = new Fingerprint
        {
            Id = 1,
            Uuid = Guid.NewGuid(),
            UserId = regularStudent.UserId,
            DeviceId = "device-001",
            TemplateData = "template",
            IsDeleted = false
        };

        _mockUserContextService.Setup(s => s.GetUserIdAsync(_testUserPrincipal)).ReturnsAsync(userId);
        _mockInstructorRepository.Setup(r => r.GetInstructorByUserIdAsync(userId)).ReturnsAsync(instructor);
        _mockInstructorRepository.Setup(r => r.IsInstructorHandlingSectionAsync(instructorId, sectionId)).ReturnsAsync(true);
        _mockInstructorRepository.Setup(r => r.GetHandledClassesBySectionAndInstructorAsync(sectionId, instructorId))
            .ReturnsAsync(new List<Schedules> { schedule });
        _mockInstructorRepository.Setup(r => r.GetRegularStudentsBySectionIdAsync(sectionId))
            .ReturnsAsync(new List<Student> { regularStudent });
        _mockInstructorRepository.Setup(r => r.GetHomeSectionStudentsAsync(sectionId))
            .ReturnsAsync(new List<Student> { regularStudent });
        _mockFingerprintRepository.Setup(r => r.GetActiveFingerprintsAsync())
            .ReturnsAsync(new List<Fingerprint> { fingerprint });
        _mockFingerprintRepository.Setup(r => r.GetDevicesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<FingerprintDevice>
            {
                new() { DeviceIdentifier = "device-001", Name = "Main Lab Scanner", Location = "Lab 1", IsActive = true }
            });

        // Act
        var result = await _service.GetInstructorSectionDetailAsync(_testUserPrincipal, sectionId);

        // Assert
        var handledStudent = Assert.Single(result.HandledClasses.Single().Students);
        Assert.True(handledStudent.HasFingerprint);
        Assert.Equal("device-001", handledStudent.FingerprintDeviceId);
        Assert.Equal("Main Lab Scanner", handledStudent.FingerprintDeviceName);

        var homeStudent = Assert.Single(result.HomeSectionStudents);
        Assert.True(homeStudent.HasFingerprint);
        Assert.Equal("device-001", homeStudent.FingerprintDeviceId);
        Assert.Equal("Main Lab Scanner", homeStudent.FingerprintDeviceName);

        _mockFingerprintRepository.Verify(r => r.GetActiveFingerprintsAsync(), Times.Once);
        _mockFingerprintRepository.Verify(r => r.GetFingerprintByStudentIdAsync(It.IsAny<int>()), Times.Never);
    }

    [Fact]
    public async Task GetInstructorSectionDetailAsync_NotHandlingSection_ThrowsEntityUnauthorizedException()
    {
        // Arrange
        const string userId = "test-user-id";
        const int instructorId = 1;
        const int sectionId = 1;
        var instructor = new Instructor { Id = instructorId, Firstname = "John", Lastname = "Doe", UserId = userId };

        _mockUserContextService.Setup(s => s.GetUserIdAsync(_testUserPrincipal)).ReturnsAsync(userId);
        _mockInstructorRepository.Setup(r => r.GetInstructorByUserIdAsync(userId)).ReturnsAsync(instructor);
        _mockInstructorRepository.Setup(r => r.IsInstructorHandlingSectionAsync(instructorId, sectionId)).ReturnsAsync(false);

        // Act & Assert
        var exception = await Assert.ThrowsAsync<EntityUnauthorizedException>(
            () => _service.GetInstructorSectionDetailAsync(_testUserPrincipal, sectionId));
        Assert.Equal("Section", exception.EntityName);
    }

    [Fact]
    public async Task GetInstructorSectionDetailAsync_SectionNotFound_ThrowsEntityNotFoundException()
    {
        // Arrange
        const string userId = "test-user-id";
        const int instructorId = 1;
        const int sectionId = 999;
        var instructor = new Instructor { Id = instructorId, Firstname = "John", Lastname = "Doe", UserId = userId };

        _mockUserContextService.Setup(s => s.GetUserIdAsync(_testUserPrincipal)).ReturnsAsync(userId);
        _mockInstructorRepository.Setup(r => r.GetInstructorByUserIdAsync(userId)).ReturnsAsync(instructor);
        _mockSectionRepository.Setup(r => r.GetSectionByIdAsync(sectionId)).ReturnsAsync((Section?)null);

        // Act & Assert
        var exception = await Assert.ThrowsAsync<EntityNotFoundException<int>>(
            () => _service.GetInstructorSectionDetailAsync(_testUserPrincipal, sectionId));
        Assert.Equal("Section", exception.EntityName);
        Assert.Equal(sectionId, exception.Key);
        _mockInstructorRepository.Verify(r => r.IsInstructorHandlingSectionAsync(It.IsAny<int>(), It.IsAny<int>()), Times.Never);
    }

    [Fact]
    public async Task GetInstructorSectionDetailAsync_MissingUserId_ThrowsEntityNotFoundException()
    {
        // Arrange
        _mockUserContextService.Setup(s => s.GetUserIdAsync(_testUserPrincipal)).ReturnsAsync((string?)null);

        // Act & Assert
        var exception = await Assert.ThrowsAsync<EntityNotFoundException<string>>(
            () => _service.GetInstructorSectionDetailAsync(_testUserPrincipal, 1));
        Assert.Equal("User", exception.EntityName);
    }

    [Fact]
    public async Task GetInstructorSectionDetailAsync_InstructorNotFound_ThrowsEntityNotFoundException()
    {
        // Arrange
        const string userId = "test-user-id";
        _mockUserContextService.Setup(s => s.GetUserIdAsync(_testUserPrincipal)).ReturnsAsync(userId);
        _mockInstructorRepository.Setup(r => r.GetInstructorByUserIdAsync(userId)).ReturnsAsync((Instructor?)null);

        // Act & Assert
        var exception = await Assert.ThrowsAsync<EntityNotFoundException<string>>(
            () => _service.GetInstructorSectionDetailAsync(_testUserPrincipal, 1));
        Assert.Equal("Instructor", exception.EntityName);
    }

    [Fact]
    public async Task GetInstructorSectionDetailAsync_RepositoryFailure_WrapsInEntityServiceException()
    {
        // Arrange
        const string userId = "test-user-id";
        const int instructorId = 1;
        const int sectionId = 1;
        var instructor = new Instructor { Id = instructorId, Firstname = "John", Lastname = "Doe", UserId = userId };
        var expectedException = new InvalidOperationException("Database error");

        _mockUserContextService.Setup(s => s.GetUserIdAsync(_testUserPrincipal)).ReturnsAsync(userId);
        _mockInstructorRepository.Setup(r => r.GetInstructorByUserIdAsync(userId)).ReturnsAsync(instructor);
        _mockInstructorRepository.Setup(r => r.IsInstructorHandlingSectionAsync(instructorId, sectionId)).ReturnsAsync(true);
        _mockInstructorRepository.Setup(r => r.GetHandledClassesBySectionAndInstructorAsync(sectionId, instructorId))
            .ThrowsAsync(expectedException);

        // Act
        var exception = await Assert.ThrowsAsync<EntityServiceException>(
            () => _service.GetInstructorSectionDetailAsync(_testUserPrincipal, sectionId));

        // Assert
        Assert.Equal("Instructor", exception.EntityName);
        Assert.Equal($"GetInstructorSectionDetail: {sectionId}", exception.Operation);
        Assert.Same(expectedException, exception.InnerException);
    }

    #endregion

    #region GetInstructorStudentDetailAsync Tests

    [Fact]
    public async Task GetInstructorStudentDetailAsync_Success_ReturnsStudentDetailDto()
    {
        // Arrange
        const string userId = "test-user-id";
        const int instructorId = 1;
        const int studentId = 1;
        var instructor = new Instructor { Id = instructorId, Firstname = "John", Lastname = "Doe", UserId = userId };

        var course = new Course { Id = 1, Uuid = Guid.NewGuid(), Name = "Computer Science" };
        var section = new Section { Id = 1, Uuid = Guid.NewGuid(), Name = "CS-3A", CourseId = 1, Course = course };

        var student = new Student
        {
            Id = studentId, Uuid = Guid.NewGuid(), Firstname = "Alice", Lastname = "Smith",
            SectionId = 1, IsRegular = true, IsDeleted = false, Section = section,
            AdditionalEnrollments = new List<StudentEnrollment>()
        };

        var attendanceRecord = new AttendanceRecord
        {
            Id = 1, StudentId = studentId, SessionId = 1, Status = "Present",
            CheckInTime = DateTime.UtcNow,
            Session = new Session
            {
                Id = 1, Schedule = new Schedules
                {
                    Id = 1, InstructorId = instructorId, Subject = new Subject { Id = 1, Name = "Data Structures", Code = "CS301" }
                }
            }
        };

        var enrollmentSubjectGuid = Guid.NewGuid();
        var instructorSchedules = new List<Schedules>
        {
            new()
            {
                Id = 1, Uuid = Guid.NewGuid(),
                SubjectId = 1, Subject = new Subject { Id = 1, Uuid = enrollmentSubjectGuid, Name = "Data Structures", Code = "CS301" },
                SectionId = section.Id, Section = section,
                ClassroomId = 1, Classroom = new Classroom { Id = 1, Name = "Room 101" },
                InstructorId = instructorId,
                DayOfWeek = "Monday", TimeIn = new TimeOnly(9, 0), TimeOut = new TimeOnly(11, 0)
            }
        };

        _mockUserContextService.Setup(s => s.GetUserIdAsync(_testUserPrincipal)).ReturnsAsync(userId);
        _mockInstructorRepository.Setup(r => r.GetInstructorByUserIdAsync(userId)).ReturnsAsync(instructor);
        _mockInstructorRepository.Setup(r => r.GetStudentWithDetailsAsync(studentId)).ReturnsAsync(student);
        _mockInstructorRepository.Setup(r => r.IsInstructorHandlingSectionAsync(instructorId, student.SectionId)).ReturnsAsync(true);
        _mockInstructorRepository.Setup(r => r.GetHandledClassesBySectionAndInstructorAsync(student.SectionId, instructorId))
            .ReturnsAsync(instructorSchedules);
        _mockInstructorRepository.Setup(r => r.GetStudentAttendanceForInstructorSubjectsAsync(studentId, instructorId))
            .ReturnsAsync(new List<AttendanceRecord> { attendanceRecord });

        // Act
        var result = await _service.GetInstructorStudentDetailAsync(_testUserPrincipal, studentId);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(student.Uuid, result.StudentId);
        Assert.Equal("Alice", result.Firstname);
        Assert.Equal("Smith", result.Lastname);
        Assert.Equal(section.Uuid, result.SectionId);
        Assert.Equal("CS-3A", result.SectionName);
        Assert.True(result.IsRegular);
        Assert.Equal("Regular", result.EnrollmentType);
        Assert.Single(result.Enrollments);
        Assert.Equal(enrollmentSubjectGuid, result.Enrollments[0].SubjectId);
        Assert.Equal("Data Structures", result.Enrollments[0].SubjectName);
        Assert.Equal("CS301", result.Enrollments[0].SubjectCode);
        Assert.Equal("Regular", result.Enrollments[0].EnrollmentType);
        Assert.Equal(1, result.AttendanceSummary.TotalSessions);
        Assert.Equal(1, result.AttendanceSummary.PresentCount);
        Assert.Equal(0, result.AttendanceSummary.AbsentCount);
        Assert.Equal(0, result.AttendanceSummary.LateCount);
        Assert.Equal(100.0, result.AttendanceSummary.AttendanceRate);
    }

    [Fact]
    public async Task GetInstructorStudentDetailAsync_FingerprintData_MapsDeviceNameAndLocation()
    {
        // Arrange
        const string userId = "test-user-id";
        const int instructorId = 1;
        const int studentId = 1;
        var instructor = new Instructor { Id = instructorId, Firstname = "John", Lastname = "Doe", UserId = userId };
        var course = new Course { Id = 1, Uuid = Guid.NewGuid(), Name = "Computer Science" };
        var section = new Section { Id = 1, Uuid = Guid.NewGuid(), Name = "CS-3A", CourseId = 1, Course = course };
        var student = new Student
        {
            Id = studentId,
            Uuid = Guid.NewGuid(),
            Firstname = "Alice",
            Lastname = "Smith",
            SectionId = section.Id,
            IsRegular = true,
            IsDeleted = false,
            Section = section,
            AdditionalEnrollments = new List<StudentEnrollment>()
        };

        var instructorSchedules = new List<Schedules>
        {
            new()
            {
                Id = 1,
                Uuid = Guid.NewGuid(),
                SubjectId = 1,
                Subject = new Subject { Id = 1, Uuid = Guid.NewGuid(), Name = "Data Structures", Code = "CS301" },
                SectionId = section.Id,
                Section = section,
                ClassroomId = 1,
                Classroom = new Classroom { Id = 1, Name = "Room 101" },
                InstructorId = instructorId,
                DayOfWeek = "Monday",
                TimeIn = new TimeOnly(9, 0),
                TimeOut = new TimeOnly(11, 0)
            }
        };

        var fingerprint = new Fingerprint
        {
            Id = 1,
            Uuid = Guid.NewGuid(),
            UserId = "student-user-id",
            DeviceId = "device-001",
            TemplateData = "template",
            IsDeleted = false,
            CreatedAt = DateTime.UtcNow
        };

        _mockUserContextService.Setup(s => s.GetUserIdAsync(_testUserPrincipal)).ReturnsAsync(userId);
        _mockInstructorRepository.Setup(r => r.GetInstructorByUserIdAsync(userId)).ReturnsAsync(instructor);
        _mockInstructorRepository.Setup(r => r.GetStudentWithDetailsAsync(studentId)).ReturnsAsync(student);
        _mockInstructorRepository.Setup(r => r.IsInstructorHandlingSectionAsync(instructorId, student.SectionId)).ReturnsAsync(true);
        _mockInstructorRepository.Setup(r => r.GetHandledClassesBySectionAndInstructorAsync(student.SectionId, instructorId))
            .ReturnsAsync(instructorSchedules);
        _mockInstructorRepository.Setup(r => r.GetStudentAttendanceForInstructorSubjectsAsync(studentId, instructorId))
            .ReturnsAsync(new List<AttendanceRecord>());
        _mockFingerprintRepository.Setup(r => r.GetFingerprintByStudentIdAsync(studentId)).ReturnsAsync(fingerprint);
        _mockFingerprintRepository.Setup(r => r.GetDevicesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<FingerprintDevice>
            {
                new() { DeviceIdentifier = "device-001", Name = "Main Lab Scanner", Location = "Lab 1", IsActive = true }
            });

        // Act
        var result = await _service.GetInstructorStudentDetailAsync(_testUserPrincipal, studentId);

        // Assert
        Assert.NotNull(result.Fingerprint);
        Assert.Equal("device-001", result.Fingerprint.DeviceId);
        Assert.Equal("Main Lab Scanner", result.Fingerprint.DeviceName);
        Assert.Equal("Lab 1", result.Fingerprint.DeviceLocation);
    }

    [Fact]
    public async Task GetInstructorStudentDetailAsync_RegularStudentWithAdditionalEnrollments_ReturnsCombinedEnrollments()
    {
        // Arrange
        const string userId = "test-user-id";
        const int instructorId = 1;
        const int studentId = 1;
        var instructor = new Instructor { Id = instructorId, Firstname = "John", Lastname = "Doe", UserId = userId };

        var course = new Course { Id = 1, Uuid = Guid.NewGuid(), Name = "Computer Science" };
        var section = new Section { Id = 1, Uuid = Guid.NewGuid(), Name = "CS-3A", CourseId = 1, Course = course };
        var otherSection = new Section { Id = 2, Uuid = Guid.NewGuid(), Name = "CS-3B", CourseId = 1, Course = course };

        var student = new Student
        {
            Id = studentId, Uuid = Guid.NewGuid(), Firstname = "Alice", Lastname = "Smith",
            SectionId = section.Id, IsRegular = true, IsDeleted = false, Section = section,
            AdditionalEnrollments = new List<StudentEnrollment>
            {
                new()
                {
                    Id = 1, Uuid = Guid.NewGuid(), StudentId = studentId,
                    SectionId = otherSection.Id, Section = otherSection,
                    SubjectId = 2, Subject = new Subject { Id = 2, Name = "Algorithms", Code = "CS302" },
                    IsActive = true, EnrollmentType = EnrollmentTypeConstants.Irregular
                }
            }
        };

        var instructorSchedules = new List<Schedules>
        {
            new()
            {
                Id = 1, Uuid = Guid.NewGuid(),
                SubjectId = 1, Subject = new Subject { Id = 1, Name = "Data Structures", Code = "CS301" },
                SectionId = section.Id, Section = section,
                ClassroomId = 1, Classroom = new Classroom { Id = 1, Name = "Room 101" },
                InstructorId = instructorId,
                DayOfWeek = "Monday", TimeIn = new TimeOnly(9, 0), TimeOut = new TimeOnly(11, 0)
            }
        };

        _mockUserContextService.Setup(s => s.GetUserIdAsync(_testUserPrincipal)).ReturnsAsync(userId);
        _mockInstructorRepository.Setup(r => r.GetInstructorByUserIdAsync(userId)).ReturnsAsync(instructor);
        _mockInstructorRepository.Setup(r => r.GetStudentWithDetailsAsync(studentId)).ReturnsAsync(student);
        _mockInstructorRepository.Setup(r => r.IsInstructorHandlingSectionAsync(instructorId, student.SectionId)).ReturnsAsync(true);
        _mockInstructorRepository.Setup(r => r.GetHandledClassesBySectionAndInstructorAsync(student.SectionId, instructorId))
            .ReturnsAsync(instructorSchedules);
        _mockInstructorRepository.Setup(r => r.GetStudentAttendanceForInstructorSubjectsAsync(studentId, instructorId))
            .ReturnsAsync(new List<AttendanceRecord>());

        // Act
        var result = await _service.GetInstructorStudentDetailAsync(_testUserPrincipal, studentId);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(2, result.Enrollments.Count);

        var homeEnrollment = result.Enrollments.First(e => e.SubjectName == "Data Structures");
        Assert.Equal("Data Structures", homeEnrollment.SubjectName);
        Assert.Equal("CS301", homeEnrollment.SubjectCode);
        Assert.Equal(section.Uuid, homeEnrollment.SectionId);
        Assert.Equal("Regular", homeEnrollment.EnrollmentType);

        var additionalEnrollment = result.Enrollments.First(e => e.SubjectName == "Algorithms");
        Assert.Equal("Algorithms", additionalEnrollment.SubjectName);
        Assert.Equal("CS302", additionalEnrollment.SubjectCode);
        Assert.Equal(otherSection.Uuid, additionalEnrollment.SectionId);
        Assert.Equal(EnrollmentTypeConstants.Irregular, additionalEnrollment.EnrollmentType);
    }

    [Fact]
    public async Task GetInstructorStudentDetailAsync_AdditionalEnrollmentForDifferentSubject_ThrowsEntityUnauthorizedException()
    {
        // Arrange
        const string userId = "test-user-id";
        const int instructorId = 1;
        const int studentId = 1;
        var instructor = new Instructor { Id = instructorId, Firstname = "John", Lastname = "Doe", UserId = userId };

        var course = new Course { Id = 1, Uuid = Guid.NewGuid(), Name = "Computer Science" };
        var homeSection = new Section { Id = 1, Uuid = Guid.NewGuid(), Name = "CS-3A", CourseId = 1, Course = course };
        var handledSection = new Section { Id = 2, Uuid = Guid.NewGuid(), Name = "CS-3B", CourseId = 1, Course = course };

        var student = new Student
        {
            Id = studentId,
            Uuid = Guid.NewGuid(),
            Firstname = "Alice",
            Lastname = "Smith",
            SectionId = homeSection.Id,
            IsRegular = false,
            IsDeleted = false,
            Section = homeSection,
            AdditionalEnrollments = new List<StudentEnrollment>
            {
                new()
                {
                    Id = 1,
                    Uuid = Guid.NewGuid(),
                    StudentId = studentId,
                    SectionId = handledSection.Id,
                    Section = handledSection,
                    SubjectId = 2,
                    Subject = new Subject { Id = 2, Name = "Algorithms", Code = "CS302" },
                    IsActive = true,
                    EnrollmentType = EnrollmentTypeConstants.Irregular
                }
            }
        };

        var instructorSchedules = new List<Schedules>
        {
            new()
            {
                Id = 1,
                Uuid = Guid.NewGuid(),
                SubjectId = 1,
                Subject = new Subject { Id = 1, Name = "Data Structures", Code = "CS301" },
                SectionId = handledSection.Id,
                Section = handledSection,
                ClassroomId = 1,
                Classroom = new Classroom { Id = 1, Name = "Room 101" },
                InstructorId = instructorId,
                DayOfWeek = "Monday",
                TimeIn = new TimeOnly(9, 0),
                TimeOut = new TimeOnly(11, 0)
            }
        };

        _mockUserContextService.Setup(s => s.GetUserIdAsync(_testUserPrincipal)).ReturnsAsync(userId);
        _mockInstructorRepository.Setup(r => r.GetInstructorByUserIdAsync(userId)).ReturnsAsync(instructor);
        _mockInstructorRepository.Setup(r => r.GetStudentWithDetailsAsync(studentId)).ReturnsAsync(student);
        _mockInstructorRepository.Setup(r => r.IsInstructorHandlingSectionAsync(instructorId, student.SectionId)).ReturnsAsync(false);
        _mockScheduleRepository.Setup(r => r.GetSchedulesByInstructorIdAsync(instructorId)).ReturnsAsync(instructorSchedules);

        // Act & Assert
        var exception = await Assert.ThrowsAsync<EntityUnauthorizedException>(
            () => _service.GetInstructorStudentDetailAsync(_testUserPrincipal, studentId));
        Assert.Equal("Student", exception.EntityName);
    }

    [Fact]
    public async Task GetInstructorStudentDetailAsync_AdditionalEnrollmentForMatchingSubject_ReturnsStudentDetailDto()
    {
        // Arrange
        const string userId = "test-user-id";
        const int instructorId = 1;
        const int studentId = 1;
        var instructor = new Instructor { Id = instructorId, Firstname = "John", Lastname = "Doe", UserId = userId };

        var course = new Course { Id = 1, Uuid = Guid.NewGuid(), Name = "Computer Science" };
        var homeSection = new Section { Id = 1, Uuid = Guid.NewGuid(), Name = "CS-3A", CourseId = 1, Course = course };
        var handledSection = new Section { Id = 2, Uuid = Guid.NewGuid(), Name = "CS-3B", CourseId = 1, Course = course };
        var matchingSubject = new Subject { Id = 2, Name = "Algorithms", Code = "CS302" };

        var student = new Student
        {
            Id = studentId,
            Uuid = Guid.NewGuid(),
            Firstname = "Alice",
            Lastname = "Smith",
            SectionId = homeSection.Id,
            IsRegular = false,
            IsDeleted = false,
            Section = homeSection,
            AdditionalEnrollments = new List<StudentEnrollment>
            {
                new()
                {
                    Id = 1,
                    Uuid = Guid.NewGuid(),
                    StudentId = studentId,
                    SectionId = handledSection.Id,
                    Section = handledSection,
                    SubjectId = matchingSubject.Id,
                    Subject = matchingSubject,
                    IsActive = true,
                    EnrollmentType = EnrollmentTypeConstants.Irregular
                }
            }
        };

        var instructorSchedules = new List<Schedules>
        {
            new()
            {
                Id = 1,
                Uuid = Guid.NewGuid(),
                SubjectId = matchingSubject.Id,
                Subject = matchingSubject,
                SectionId = handledSection.Id,
                Section = handledSection,
                ClassroomId = 1,
                Classroom = new Classroom { Id = 1, Name = "Room 101" },
                InstructorId = instructorId,
                DayOfWeek = "Monday",
                TimeIn = new TimeOnly(9, 0),
                TimeOut = new TimeOnly(11, 0)
            }
        };

        _mockUserContextService.Setup(s => s.GetUserIdAsync(_testUserPrincipal)).ReturnsAsync(userId);
        _mockInstructorRepository.Setup(r => r.GetInstructorByUserIdAsync(userId)).ReturnsAsync(instructor);
        _mockInstructorRepository.Setup(r => r.GetStudentWithDetailsAsync(studentId)).ReturnsAsync(student);
        _mockInstructorRepository.Setup(r => r.IsInstructorHandlingSectionAsync(instructorId, student.SectionId)).ReturnsAsync(false);
        _mockScheduleRepository.Setup(r => r.GetSchedulesByInstructorIdAsync(instructorId)).ReturnsAsync(instructorSchedules);
        _mockInstructorRepository.Setup(r => r.GetHandledClassesBySectionAndInstructorAsync(student.SectionId, instructorId))
            .ReturnsAsync(new List<Schedules>());
        _mockInstructorRepository.Setup(r => r.GetStudentAttendanceForInstructorSubjectsAsync(studentId, instructorId))
            .ReturnsAsync(new List<AttendanceRecord>());

        // Act
        var result = await _service.GetInstructorStudentDetailAsync(_testUserPrincipal, studentId);

        // Assert
        Assert.NotNull(result);
        Assert.Single(result.Enrollments);
        Assert.Equal(matchingSubject.Uuid, result.Enrollments[0].SubjectId);
        Assert.Equal(handledSection.Uuid, result.Enrollments[0].SectionId);
        Assert.Equal(EnrollmentTypeConstants.Irregular, result.Enrollments[0].EnrollmentType);
    }

    [Fact]
    public async Task GetInstructorStudentDetailAsync_StudentNotFound_ThrowsEntityNotFoundException()
    {
        // Arrange
        const string userId = "test-user-id";
        const int instructorId = 1;
        const int studentId = 999;
        var instructor = new Instructor { Id = instructorId, Firstname = "John", Lastname = "Doe", UserId = userId };

        _mockUserContextService.Setup(s => s.GetUserIdAsync(_testUserPrincipal)).ReturnsAsync(userId);
        _mockInstructorRepository.Setup(r => r.GetInstructorByUserIdAsync(userId)).ReturnsAsync(instructor);
        _mockInstructorRepository.Setup(r => r.GetStudentWithDetailsAsync(studentId)).ReturnsAsync((Student?)null);

        // Act & Assert
        var exception = await Assert.ThrowsAsync<EntityNotFoundException<int>>(
            () => _service.GetInstructorStudentDetailAsync(_testUserPrincipal, studentId));
        Assert.Equal("Student", exception.EntityName);
        Assert.Equal(studentId, exception.Key);
    }

    [Fact]
    public async Task GetInstructorStudentDetailAsync_NotVisibleToInstructor_ThrowsEntityUnauthorizedException()
    {
        // Arrange
        const string userId = "test-user-id";
        const int instructorId = 1;
        const int studentId = 1;
        var instructor = new Instructor { Id = instructorId, Firstname = "John", Lastname = "Doe", UserId = userId };

        var course = new Course { Id = 1, Uuid = Guid.NewGuid(), Name = "Computer Science" };
        var section = new Section { Id = 1, Uuid = Guid.NewGuid(), Name = "CS-3A", CourseId = 1, Course = course };

        var student = new Student
        {
            Id = studentId, Uuid = Guid.NewGuid(), Firstname = "Alice", Lastname = "Smith",
            SectionId = 1, IsRegular = true, IsDeleted = false, Section = section,
            AdditionalEnrollments = new List<StudentEnrollment>()
        };

        _mockUserContextService.Setup(s => s.GetUserIdAsync(_testUserPrincipal)).ReturnsAsync(userId);
        _mockInstructorRepository.Setup(r => r.GetInstructorByUserIdAsync(userId)).ReturnsAsync(instructor);
        _mockInstructorRepository.Setup(r => r.GetStudentWithDetailsAsync(studentId)).ReturnsAsync(student);
        _mockInstructorRepository.Setup(r => r.IsInstructorHandlingSectionAsync(instructorId, student.SectionId)).ReturnsAsync(false);
        _mockScheduleRepository.Setup(r => r.GetSchedulesByInstructorIdAsync(instructorId)).ReturnsAsync(new List<Schedules>());

        // Act & Assert
        var exception = await Assert.ThrowsAsync<EntityUnauthorizedException>(
            () => _service.GetInstructorStudentDetailAsync(_testUserPrincipal, studentId));
        Assert.Equal("Student", exception.EntityName);
    }

    [Fact]
    public async Task GetInstructorStudentDetailAsync_MissingUserId_ThrowsEntityNotFoundException()
    {
        // Arrange
        _mockUserContextService.Setup(s => s.GetUserIdAsync(_testUserPrincipal)).ReturnsAsync((string?)null);

        // Act & Assert
        var exception = await Assert.ThrowsAsync<EntityNotFoundException<string>>(
            () => _service.GetInstructorStudentDetailAsync(_testUserPrincipal, 1));
        Assert.Equal("User", exception.EntityName);
    }

    [Fact]
    public async Task GetInstructorStudentDetailAsync_InstructorNotFound_ThrowsEntityNotFoundException()
    {
        // Arrange
        const string userId = "test-user-id";
        _mockUserContextService.Setup(s => s.GetUserIdAsync(_testUserPrincipal)).ReturnsAsync(userId);
        _mockInstructorRepository.Setup(r => r.GetInstructorByUserIdAsync(userId)).ReturnsAsync((Instructor?)null);

        // Act & Assert
        var exception = await Assert.ThrowsAsync<EntityNotFoundException<string>>(
            () => _service.GetInstructorStudentDetailAsync(_testUserPrincipal, 1));
        Assert.Equal("Instructor", exception.EntityName);
    }

    [Fact]
    public async Task GetInstructorStudentDetailAsync_RepositoryFailure_WrapsInEntityServiceException()
    {
        // Arrange
        const string userId = "test-user-id";
        const int instructorId = 1;
        const int studentId = 1;
        var instructor = new Instructor { Id = instructorId, Firstname = "John", Lastname = "Doe", UserId = userId };
        var expectedException = new InvalidOperationException("Database error");

        _mockUserContextService.Setup(s => s.GetUserIdAsync(_testUserPrincipal)).ReturnsAsync(userId);
        _mockInstructorRepository.Setup(r => r.GetInstructorByUserIdAsync(userId)).ReturnsAsync(instructor);
        _mockInstructorRepository.Setup(r => r.GetStudentWithDetailsAsync(studentId)).ThrowsAsync(expectedException);

        // Act
        var exception = await Assert.ThrowsAsync<EntityServiceException>(
            () => _service.GetInstructorStudentDetailAsync(_testUserPrincipal, studentId));

        // Assert
        Assert.Equal("Instructor", exception.EntityName);
        Assert.Equal($"GetInstructorStudentDetail: {studentId}", exception.Operation);
        Assert.Same(expectedException, exception.InnerException);
    }

    #endregion
}
