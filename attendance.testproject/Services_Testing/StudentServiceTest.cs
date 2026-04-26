using System.Security.Claims;
using attendance_monitoring.Classes;
using attendance_monitoring.Constants;
using attendance_monitoring.Exceptions;
using attendance_monitoring.IRepository;
using attendance_monitoring.IServices;
using attendance_monitoring.Models.DTO.Request;
using attendance_monitoring.Services;
using Microsoft.Extensions.Logging;
using Moq;

namespace attendance.testproject.Services_Testing;

/// <summary>
/// Unit tests for StudentService
/// Tests CreateStudentAsync and RestoreStudentAsync
/// </summary>
public class StudentServiceTest
{
    private readonly Mock<IStudentRepository> _mockStudentRepository;
    private readonly Mock<IUserContextService> _mockUserContextService;
    private readonly Mock<ISectionRepository> _mockSectionRepository;
    private readonly Mock<ILogger<StudentService>> _mockLogger;
    private readonly StudentService _service;
    private readonly ClaimsPrincipal _testUserPrincipal;

    public StudentServiceTest()
    {
        _mockStudentRepository = new Mock<IStudentRepository>();
        _mockUserContextService = new Mock<IUserContextService>();
        _mockSectionRepository = new Mock<ISectionRepository>();
        _mockLogger = new Mock<ILogger<StudentService>>();

        _service = new StudentService(
            _mockStudentRepository.Object,
            _mockUserContextService.Object,
            _mockSectionRepository.Object,
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

    #region CreateStudentAsync Tests

    [Fact]
    public async Task CreateStudentAsync_InvalidSectionId_ThrowsEntityServiceException()
    {
        // Arrange
        var createStudent = new CreateStudent
        {
            Firstname = "John",
            Lastname = "Doe",
            IsRegular = true,
            SectionId = Guid.NewGuid()
        };

        // Act & Assert
        var exception = await Assert.ThrowsAsync<EntityServiceException>(() => _service.CreateStudentAsync(createStudent, _testUserPrincipal));
        Assert.Equal("Student", exception.EntityName);
        Assert.Contains("specified section does not exist", exception.Message);
    }

    [Fact]
    public async Task CreateStudentAsync_SectionNotFound_ThrowsEntityServiceException()
    {
        // Arrange
        var sectionId = Guid.NewGuid();
        var createStudent = new CreateStudent
        {
            Firstname = "John",
            Lastname = "Doe",
            IsRegular = true,
            SectionId = sectionId
        };

        _mockSectionRepository.Setup(r => r.GetSectionByIdAsync(sectionId)).ReturnsAsync((Section?)null);

        // Act & Assert
        var exception = await Assert.ThrowsAsync<EntityServiceException>(() => _service.CreateStudentAsync(createStudent, _testUserPrincipal));
        Assert.Equal("Student", exception.EntityName);
        Assert.Contains("does not exist", exception.Message);
    }

    [Fact]
    public async Task CreateStudentAsync_MissingUserId_ThrowsEntityServiceException()
    {
        // Arrange
        var createStudent = new CreateStudent
        {
            Firstname = "John",
            Lastname = "Doe",
            IsRegular = true,
            SectionId = Guid.NewGuid()
        };

        _mockSectionRepository.Setup(r => r.GetSectionByIdAsync(It.IsAny<Guid>())).ReturnsAsync(new Section { Id = Guid.NewGuid(), Name = "CS-3A", CourseId = Guid.NewGuid() });
        _mockUserContextService.Setup(s => s.GetUserIdAsync(_testUserPrincipal)).ReturnsAsync((string?)null);

        // Act & Assert
        var exception = await Assert.ThrowsAsync<EntityServiceException>(() => _service.CreateStudentAsync(createStudent, _testUserPrincipal));
        Assert.Equal("Student", exception.EntityName);
        Assert.Contains("User ID not found", exception.Message);
    }

    [Fact]
    public async Task CreateStudentAsync_ExistingStudentForUser_ThrowsEntityAlreadyExistsException()
    {
        // Arrange
        const string userId = "test-user-id";
        var createStudent = new CreateStudent
        {
            Firstname = "John",
            Lastname = "Doe",
            IsRegular = true,
            SectionId = Guid.NewGuid()
        };

        var existingStudent = new Student { Id = Guid.NewGuid(), Firstname = "Jane", Lastname = "Smith", UserId = userId };

        _mockSectionRepository.Setup(r => r.GetSectionByIdAsync(It.IsAny<Guid>())).ReturnsAsync(new Section { Id = Guid.NewGuid(), Name = "CS-3A", CourseId = Guid.NewGuid() });
        _mockUserContextService.Setup(s => s.GetUserIdAsync(_testUserPrincipal)).ReturnsAsync(userId);
        _mockStudentRepository.Setup(r => r.GetStudentByUserIdAsync(userId)).ReturnsAsync(existingStudent);

        // Act & Assert
        var exception = await Assert.ThrowsAsync<EntityAlreadyExistsException<string>>(() => _service.CreateStudentAsync(createStudent, _testUserPrincipal));
        Assert.Equal("Student", exception.EntityName);
        Assert.Equal("UserId", exception.IdentifierPropertyName);
        Assert.Equal(userId, exception.EntityIdentifier);
    }

    [Fact]
    public async Task CreateStudentAsync_ValidInput_CreatesStudent()
    {
        // Arrange
        const string userId = "test-user-id";
        var createStudent = new CreateStudent
        {
            Firstname = "John",
            Lastname = "Doe",
            IsRegular = true,
            SectionId = Guid.NewGuid()
        };

        var createdStudent = new Student
        {
            Id = Guid.NewGuid(),
            Firstname = "John",
            Lastname = "Doe",
            IsRegular = true,
            UserId = userId,
            SectionId = Guid.NewGuid(),
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        _mockSectionRepository.Setup(r => r.GetSectionByIdAsync(It.IsAny<Guid>())).ReturnsAsync(new Section { Id = Guid.NewGuid(), Name = "CS-3A", CourseId = Guid.NewGuid() });
        _mockUserContextService.Setup(s => s.GetUserIdAsync(_testUserPrincipal)).ReturnsAsync(userId);
        _mockStudentRepository.Setup(r => r.GetStudentByUserIdAsync(userId)).ReturnsAsync((Student?)null);
        _mockStudentRepository.Setup(r => r.CreateStudent(It.IsAny<Student>())).ReturnsAsync(createdStudent);
        _mockStudentRepository.Setup(r => r.SaveChangesAsync()).ReturnsAsync(1);

        // Act
        var result = await _service.CreateStudentAsync(createStudent, _testUserPrincipal);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(userId, result.UserId);
        Assert.NotEqual(Guid.Empty, result.SectionId);
        Assert.Equal("John", result.Firstname);
        Assert.Equal("Doe", result.Lastname);
        Assert.True(result.IsRegular);
        _mockStudentRepository.Verify(r => r.CreateStudent(It.IsAny<Student>()), Times.Once);
        _mockStudentRepository.Verify(r => r.SaveChangesAsync(), Times.Once);
    }

    [Fact]
    public async Task CreateStudentAsync_RepositoryFailure_WrapsInEntityServiceException()
    {
        // Arrange
        const string userId = "test-user-id";
        var createStudent = new CreateStudent
        {
            Firstname = "John",
            Lastname = "Doe",
            IsRegular = true,
            SectionId = Guid.NewGuid()
        };

        var expectedException = new InvalidOperationException("Database error");

        _mockSectionRepository.Setup(r => r.GetSectionByIdAsync(It.IsAny<Guid>())).ReturnsAsync(new Section { Id = Guid.NewGuid(), Name = "CS-3A", CourseId = Guid.NewGuid() });
        _mockUserContextService.Setup(s => s.GetUserIdAsync(_testUserPrincipal)).ReturnsAsync(userId);
        _mockStudentRepository.Setup(r => r.GetStudentByUserIdAsync(userId)).ReturnsAsync((Student?)null);
        _mockStudentRepository.Setup(r => r.CreateStudent(It.IsAny<Student>())).ThrowsAsync(expectedException);

        // Act
        var exception = await Assert.ThrowsAsync<EntityServiceException>(() => _service.CreateStudentAsync(createStudent, _testUserPrincipal));

        // Assert
        Assert.Equal("Student", exception.EntityName);
        Assert.Equal("CreateStudent", exception.Operation);
        Assert.Same(expectedException, exception.InnerException);
    }

    #endregion

    #region GetStudentByUuidAsync Tests

    [Fact]
    public async Task GetStudentByUuidAsync_WhenStudentExists_ReturnsStudent()
    {
        // Arrange
        var studentUuid = Guid.NewGuid();
        var student = new Student
        {
            Id = studentUuid,
            Firstname = "John",
            Lastname = "Doe",
            UserId = "test-user-id",
            SectionId = Guid.NewGuid()
        };

        _mockStudentRepository
            .Setup(r => r.GetStudentByUuidAsync(studentUuid))
            .ReturnsAsync(student);

        // Act
        var result = await _service.GetStudentByUuidAsync(studentUuid);

        // Assert
        Assert.Same(student, result);
        _mockStudentRepository.Verify(r => r.GetStudentByUuidAsync(studentUuid), Times.Once);
    }

    [Fact]
    public async Task GetStudentByUuidAsync_WhenStudentMissing_ThrowsEntityNotFoundException()
    {
        // Arrange
        var studentUuid = Guid.NewGuid();

        _mockStudentRepository
            .Setup(r => r.GetStudentByUuidAsync(studentUuid))
            .ReturnsAsync((Student?)null);

        // Act
        var exception = await Assert.ThrowsAsync<EntityNotFoundException<Guid>>(() => _service.GetStudentByUuidAsync(studentUuid));

        // Assert
        Assert.Equal("Student", exception.EntityName);
        Assert.Equal(studentUuid, exception.Key);
    }

    #endregion

    #region RestoreStudentAsync Tests

    [Fact]
    public async Task RestoreStudentAsync_InvalidId_ReturnsInvalidStudentIdMessage()
    {
        // Arrange
        var studentId = Guid.Empty;

        // Act
        var result = await _service.RestoreStudentAsync(studentId, _testUserPrincipal);

        // Assert
        Assert.Equal("Invalid student ID", result);
    }

    [Fact]
    public async Task RestoreStudentAsync_MissingUserId_ReturnsUserIdNotFoundMessage()
    {
        // Arrange
        var studentId = Guid.NewGuid();
        _mockUserContextService.Setup(s => s.GetUserIdAsync(_testUserPrincipal)).ReturnsAsync((string?)null);

        // Act
        var result = await _service.RestoreStudentAsync(studentId, _testUserPrincipal);

        // Assert
        Assert.Equal("User ID not found in token", result);
    }

    [Fact]
    public async Task RestoreStudentAsync_StudentNotFound_ReturnsStudentNotFoundMessage()
    {
        // Arrange
        var studentId = Guid.NewGuid();
        const string userId = "test-user-id";

        _mockUserContextService.Setup(s => s.GetUserIdAsync(_testUserPrincipal)).ReturnsAsync(userId);
        _mockStudentRepository.Setup(r => r.GetStudentByIdIgnoreDeleteStatus(studentId)).ReturnsAsync((Student?)null);

        // Act
        var result = await _service.RestoreStudentAsync(studentId, _testUserPrincipal);

        // Assert
        Assert.Equal("Student not found", result);
    }

    [Fact]
    public async Task RestoreStudentAsync_UnauthorizedCaller_ReturnsUnauthorizedMessage()
    {
        // Arrange
        var studentId = Guid.NewGuid();
        const string userId = "test-user-id";
        var existingStudent = new Student
        {
            Id = studentId,
            Firstname = "John",
            Lastname = "Doe",
            UserId = "different-user-id",
            DeletedAt = DateTime.UtcNow
        };

        _mockUserContextService.Setup(s => s.GetUserIdAsync(_testUserPrincipal)).ReturnsAsync(userId);
        _mockStudentRepository.Setup(r => r.GetStudentByIdIgnoreDeleteStatus(studentId)).ReturnsAsync(existingStudent);
        _mockUserContextService.Setup(s => s.IsAuthorizedAsync(_testUserPrincipal, "different-user-id", RoleConstants.Admin, RoleConstants.Instructor)).ReturnsAsync(false);

        // Act
        var result = await _service.RestoreStudentAsync(studentId, _testUserPrincipal);

        // Assert
        Assert.Equal("You are not authorized to restore this student record.", result);
    }

    [Fact]
    public async Task RestoreStudentAsync_StudentNotDeleted_ReturnsStudentNotDeletedMessage()
    {
        // Arrange
        var studentId = Guid.NewGuid();
        const string userId = "test-user-id";
        var existingStudent = new Student
        {
            Id = studentId,
            Firstname = "John",
            Lastname = "Doe",
            UserId = userId,
            DeletedAt = null
        };

        _mockUserContextService.Setup(s => s.GetUserIdAsync(_testUserPrincipal)).ReturnsAsync(userId);
        _mockStudentRepository.Setup(r => r.GetStudentByIdIgnoreDeleteStatus(studentId)).ReturnsAsync(existingStudent);
        _mockUserContextService.Setup(s => s.IsAuthorizedAsync(_testUserPrincipal, userId, RoleConstants.Admin, RoleConstants.Instructor)).ReturnsAsync(true);

        // Act
        var result = await _service.RestoreStudentAsync(studentId, _testUserPrincipal);

        // Assert
        Assert.Equal("Student is not deleted", result);
    }

    [Fact]
    public async Task RestoreStudentAsync_RepositoryRestoreReturnsFalse_ReturnsFailedMessage()
    {
        // Arrange
        var studentId = Guid.NewGuid();
        const string userId = "test-user-id";
        var existingStudent = new Student
        {
            Id = studentId,
            Firstname = "John",
            Lastname = "Doe",
            UserId = userId,
            DeletedAt = DateTime.UtcNow
        };

        _mockUserContextService.Setup(s => s.GetUserIdAsync(_testUserPrincipal)).ReturnsAsync(userId);
        _mockStudentRepository.Setup(r => r.GetStudentByIdIgnoreDeleteStatus(studentId)).ReturnsAsync(existingStudent);
        _mockUserContextService.Setup(s => s.IsAuthorizedAsync(_testUserPrincipal, userId, RoleConstants.Admin, RoleConstants.Instructor)).ReturnsAsync(true);
        _mockStudentRepository.Setup(r => r.RestoreStudentAsync(studentId)).ReturnsAsync(false);

        // Act
        var result = await _service.RestoreStudentAsync(studentId, _testUserPrincipal);

        // Assert
        Assert.Equal("Failed to restore student", result);
    }

    [Fact]
    public async Task RestoreStudentAsync_Success_ReturnsNullAndCallsSaveChangesAsync()
    {
        // Arrange
        var studentId = Guid.NewGuid();
        const string userId = "test-user-id";
        var existingStudent = new Student
        {
            Id = studentId,
            Firstname = "John",
            Lastname = "Doe",
            UserId = userId,
            DeletedAt = DateTime.UtcNow
        };

        _mockUserContextService.Setup(s => s.GetUserIdAsync(_testUserPrincipal)).ReturnsAsync(userId);
        _mockStudentRepository.Setup(r => r.GetStudentByIdIgnoreDeleteStatus(studentId)).ReturnsAsync(existingStudent);
        _mockUserContextService.Setup(s => s.IsAuthorizedAsync(_testUserPrincipal, userId, RoleConstants.Admin, RoleConstants.Instructor)).ReturnsAsync(true);
        _mockStudentRepository.Setup(r => r.RestoreStudentAsync(studentId)).ReturnsAsync(true);
        _mockStudentRepository.Setup(r => r.SaveChangesAsync()).ReturnsAsync(1);

        // Act
        var result = await _service.RestoreStudentAsync(studentId, _testUserPrincipal);

        // Assert
        Assert.Null(result);
        _mockStudentRepository.Verify(r => r.SaveChangesAsync(), Times.Once);
    }

    [Fact]
    public async Task RestoreStudentAsync_SaveChangesAsyncThrows_ReturnsErrorMessage()
    {
        // Arrange
        var studentId = Guid.NewGuid();
        const string userId = "test-user-id";
        var existingStudent = new Student
        {
            Id = studentId,
            Firstname = "John",
            Lastname = "Doe",
            UserId = userId,
            DeletedAt = DateTime.UtcNow
        };

        _mockUserContextService.Setup(s => s.GetUserIdAsync(_testUserPrincipal)).ReturnsAsync(userId);
        _mockStudentRepository.Setup(r => r.GetStudentByIdIgnoreDeleteStatus(studentId)).ReturnsAsync(existingStudent);
        _mockUserContextService.Setup(s => s.IsAuthorizedAsync(_testUserPrincipal, userId, RoleConstants.Admin, RoleConstants.Instructor)).ReturnsAsync(true);
        _mockStudentRepository.Setup(r => r.RestoreStudentAsync(studentId)).ReturnsAsync(true);
        _mockStudentRepository.Setup(r => r.SaveChangesAsync()).ThrowsAsync(new InvalidOperationException("Database error"));

        // Act
        var result = await _service.RestoreStudentAsync(studentId, _testUserPrincipal);

        // Assert
        Assert.Equal("An error occurred while restoring the student. Please try again later.", result);
    }

    #endregion
}
