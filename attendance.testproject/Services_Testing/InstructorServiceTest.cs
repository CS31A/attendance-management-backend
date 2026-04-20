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
    private readonly Mock<IScheduleRepository> _mockScheduleRepository;
    private readonly Mock<IUserContextService> _mockUserContextService;
    private readonly Mock<ILogger<InstructorService>> _mockLogger;
    private readonly InstructorService _service;
    private readonly ClaimsPrincipal _testUserPrincipal;

    public InstructorServiceTest()
    {
        _mockInstructorRepository = new Mock<IInstructorRepository>();
        _mockScheduleRepository = new Mock<IScheduleRepository>();
        _mockUserContextService = new Mock<IUserContextService>();
        _mockLogger = new Mock<ILogger<InstructorService>>();

        _service = new InstructorService(
            _mockInstructorRepository.Object,
            _mockScheduleRepository.Object,
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
        Assert.Throws<ArgumentNullException>(() => new InstructorService(null!, _mockScheduleRepository.Object, _mockUserContextService.Object, _mockLogger.Object));
        Assert.Throws<ArgumentNullException>(() => new InstructorService(_mockInstructorRepository.Object, null!, _mockUserContextService.Object, _mockLogger.Object));
        Assert.Throws<ArgumentNullException>(() => new InstructorService(_mockInstructorRepository.Object, _mockScheduleRepository.Object, null!, _mockLogger.Object));
        Assert.Throws<ArgumentNullException>(() => new InstructorService(_mockInstructorRepository.Object, _mockScheduleRepository.Object, _mockUserContextService.Object, null!));
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
            Lastname = "Doe Updated"
        };
        var existingInstructor = new Instructor
        {
            Id = instructorId,
            Firstname = "John",
            Lastname = "Doe",
            UserId = userId
        };
        var updatedInstructor = new Instructor
        {
            Id = instructorId,
            Firstname = "John Updated",
            Lastname = "Doe Updated",
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
}
