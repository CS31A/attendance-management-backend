using attendance_monitoring.Classes;
using attendance_monitoring.Exceptions;
using attendance_monitoring.IRepository;
using attendance_monitoring.Models.DTO.Request;
using attendance_monitoring.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Moq;

namespace attendance.testproject.Services_Testing;

/// <summary>
/// Unit tests for SubjectService
/// Tests all public methods with happy paths, edge cases, and error scenarios
/// </summary>
public class SubjectServiceTest
{
    private readonly Mock<ISubjectRepository> _mockSubjectRepository;
    private readonly Mock<ILogger<SubjectService>> _mockLogger;
    private readonly SubjectService _service;

    public SubjectServiceTest()
    {
        _mockSubjectRepository = new Mock<ISubjectRepository>();
        _mockLogger = new Mock<ILogger<SubjectService>>();
        _service = new SubjectService(_mockSubjectRepository.Object, _mockLogger.Object);
    }

    #region Constructor Tests

    [Fact]
    public void Constructor_NullDependency_ThrowsArgumentNullException()
    {
        // Arrange & Act & Assert
        Assert.Throws<ArgumentNullException>(() => new SubjectService(null!, _mockLogger.Object));
        Assert.Throws<ArgumentNullException>(() => new SubjectService(_mockSubjectRepository.Object, null!));
    }

    #endregion

    #region GetAllSubjectsAsync Tests

    [Fact]
    public async Task GetAllSubjectsAsync_Success_ReturnsAllSubjects()
    {
        // Arrange
        var subjects = new List<Subject>
        {
            new Subject { Id = 1, Name = "Mathematics", Code = "MATH101" },
            new Subject { Id = 2, Name = "Physics", Code = "PHYS101" }
        };
        _mockSubjectRepository.Setup(r => r.GetAllSubjectsAsync()).ReturnsAsync(subjects);

        // Act
        var result = await _service.GetAllSubjectsAsync();

        // Assert
        Assert.NotNull(result);
        Assert.Equal(2, result.Count());
        _mockSubjectRepository.Verify(r => r.GetAllSubjectsAsync(), Times.Once);
    }

    [Fact]
    public async Task GetAllSubjectsAsync_RepositoryFailure_WrapsInEntityServiceException()
    {
        // Arrange
        var expectedException = new InvalidOperationException("Database error");
        _mockSubjectRepository.Setup(r => r.GetAllSubjectsAsync()).ThrowsAsync(expectedException);

        // Act
        var exception = await Assert.ThrowsAsync<EntityServiceException>(() => _service.GetAllSubjectsAsync());

        // Assert
        Assert.Equal("Subject", exception.EntityName);
        Assert.Equal("GetAllSubjects", exception.Operation);
        Assert.Same(expectedException, exception.InnerException);
    }

    #endregion

    #region GetSubjectByIdAsync Tests

    [Fact]
    public async Task GetSubjectByIdAsync_Success_ReturnsSubject()
    {
        // Arrange
        const int subjectId = 1;
        var subject = new Subject { Id = subjectId, Name = "Mathematics", Code = "MATH101" };
        _mockSubjectRepository.Setup(r => r.GetSubjectByIdAsync(subjectId)).ReturnsAsync(subject);

        // Act
        var result = await _service.GetSubjectByIdAsync(subjectId);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(subjectId, result.Id);
        Assert.Equal("Mathematics", result.Name);
        _mockSubjectRepository.Verify(r => r.GetSubjectByIdAsync(subjectId), Times.Once);
    }

    [Fact]
    public async Task GetSubjectByIdAsync_NotFound_ThrowsEntityNotFoundException()
    {
        // Arrange
        const int subjectId = 999;
        _mockSubjectRepository.Setup(r => r.GetSubjectByIdAsync(subjectId)).ReturnsAsync((Subject?)null);

        // Act & Assert
        var exception = await Assert.ThrowsAsync<EntityNotFoundException<int>>(() => _service.GetSubjectByIdAsync(subjectId));
        Assert.Equal("Subject", exception.EntityName);
        Assert.Equal(subjectId, exception.Key);
    }

    [Fact]
    public async Task GetSubjectByIdAsync_RepositoryFailure_WrapsInEntityServiceException()
    {
        // Arrange
        const int subjectId = 1;
        var expectedException = new InvalidOperationException("Database error");
        _mockSubjectRepository.Setup(r => r.GetSubjectByIdAsync(subjectId)).ThrowsAsync(expectedException);

        // Act
        var exception = await Assert.ThrowsAsync<EntityServiceException>(() => _service.GetSubjectByIdAsync(subjectId));

        // Assert
        Assert.Equal("Subject", exception.EntityName);
        Assert.Equal($"GetSubjectById: {subjectId}", exception.Operation);
        Assert.Same(expectedException, exception.InnerException);
    }

    #endregion

    #region CreateSubjectAsync Tests

    [Fact]
    public async Task CreateSubjectAsync_Success_CreatesSubject()
    {
        // Arrange
        var createSubject = new CreateSubject
        {
            Name = "Mathematics",
            Code = "MATH101"
        };
        Subject? capturedSubject = null;

        _mockSubjectRepository.Setup(r => r.GetSubjectByCodeAsync("MATH101")).ReturnsAsync((Subject?)null);
        _mockSubjectRepository
            .Setup(r => r.CreateSubject(It.IsAny<Subject>()))
            .Callback<Subject>(subject => capturedSubject = subject)
            .ReturnsAsync((Subject subject) => subject);
        _mockSubjectRepository.Setup(r => r.SaveChangesAsync()).ReturnsAsync(1);

        // Act
        var createdAtLowerBound = DateTime.UtcNow;
        var result = await _service.CreateSubjectAsync(createSubject);
        var createdAtUpperBound = DateTime.UtcNow;

        // Assert
        Assert.NotNull(result);
        Assert.NotNull(capturedSubject);
        Assert.Equal("Mathematics", result.Name);
        Assert.Equal("MATH101", result.Code);
        Assert.InRange(capturedSubject.CreatedAt, createdAtLowerBound, createdAtUpperBound);
        Assert.InRange(capturedSubject.UpdatedAt, createdAtLowerBound, createdAtUpperBound);
        Assert.Equal(capturedSubject.CreatedAt, result.CreatedAt);
        Assert.Equal(capturedSubject.UpdatedAt, result.UpdatedAt);
        _mockSubjectRepository.Verify(r => r.CreateSubject(It.IsAny<Subject>()), Times.Once);
        _mockSubjectRepository.Verify(r => r.SaveChangesAsync(), Times.Once);
    }

    [Fact]
    public async Task CreateSubjectAsync_MissingName_ThrowsValidationException()
    {
        // Arrange
        var createSubject = new CreateSubject
        {
            Name = "",
            Code = "MATH101"
        };

        // Act & Assert
        await Assert.ThrowsAsync<ValidationException>(() => _service.CreateSubjectAsync(createSubject));
    }

    [Fact]
    public async Task CreateSubjectAsync_MissingCode_ThrowsValidationException()
    {
        // Arrange
        var createSubject = new CreateSubject
        {
            Name = "Mathematics",
            Code = ""
        };

        // Act & Assert
        await Assert.ThrowsAsync<ValidationException>(() => _service.CreateSubjectAsync(createSubject));
    }

    [Fact]
    public async Task CreateSubjectAsync_DuplicateCodePrecheck_ThrowsEntityAlreadyExistsException()
    {
        // Arrange
        var createSubject = new CreateSubject
        {
            Name = "Mathematics",
            Code = "MATH101"
        };
        var existingSubject = new Subject { Id = 1, Name = "Physics", Code = "MATH101" };

        _mockSubjectRepository.Setup(r => r.GetSubjectByCodeAsync("MATH101")).ReturnsAsync(existingSubject);

        // Act & Assert
        var exception = await Assert.ThrowsAsync<EntityAlreadyExistsException<string>>(() => _service.CreateSubjectAsync(createSubject));
        Assert.Equal("Subject", exception.EntityName);
        Assert.Equal("Code", exception.IdentifierPropertyName);
        Assert.Equal("MATH101", exception.EntityIdentifier);
    }

    [Fact]
    public async Task CreateSubjectAsync_DuplicateKeyDbUpdateException_ThrowsEntityAlreadyExistsException()
    {
        // Arrange
        var createSubject = new CreateSubject
        {
            Name = "Mathematics",
            Code = "MATH101"
        };
        var innerException = new Exception("Violation of UNIQUE KEY constraint 'UK_Subjects_Code'");
        var dbUpdateException = new DbUpdateException("Update exception", innerException);

        _mockSubjectRepository.Setup(r => r.GetSubjectByCodeAsync("MATH101")).ReturnsAsync((Subject?)null);
        _mockSubjectRepository.Setup(r => r.CreateSubject(It.IsAny<Subject>())).ReturnsAsync(new Subject { Id = 1 });
        _mockSubjectRepository.Setup(r => r.SaveChangesAsync()).ThrowsAsync(dbUpdateException);

        // Act & Assert
        var exception = await Assert.ThrowsAsync<EntityAlreadyExistsException<string>>(() => _service.CreateSubjectAsync(createSubject));
        Assert.Equal("Subject", exception.EntityName);
        Assert.Equal("Code", exception.IdentifierPropertyName);
        Assert.Equal("MATH101", exception.EntityIdentifier);
    }

    [Fact]
    public async Task CreateSubjectAsync_RepositoryFailure_WrapsInEntityServiceException()
    {
        // Arrange
        var createSubject = new CreateSubject
        {
            Name = "Mathematics",
            Code = "MATH101"
        };
        var expectedException = new InvalidOperationException("Database error");

        _mockSubjectRepository.Setup(r => r.GetSubjectByCodeAsync("MATH101")).ReturnsAsync((Subject?)null);
        _mockSubjectRepository.Setup(r => r.CreateSubject(It.IsAny<Subject>())).ThrowsAsync(expectedException);

        // Act
        var exception = await Assert.ThrowsAsync<EntityServiceException>(() => _service.CreateSubjectAsync(createSubject));

        // Assert
        Assert.Equal("Subject", exception.EntityName);
        Assert.Equal("CreateSubject", exception.Operation);
    }

    #endregion

    #region UpdateSubjectAsync Tests

    [Fact]
    public async Task UpdateSubjectAsync_Success_UpdatesSubject()
    {
        // Arrange
        const int subjectId = 1;
        var updateSubject = new UpdateSubject
        {
            Name = "Mathematics Updated",
            Code = "MATH101"
        };
        var existingSubject = new Subject
        {
            Id = subjectId,
            Name = "Mathematics",
            Code = "MATH101",
            CreatedAt = DateTime.UtcNow.AddDays(-1),
            UpdatedAt = DateTime.UtcNow.AddDays(-1)
        };
        var updatedSubject = new Subject
        {
            Id = subjectId,
            Name = "Mathematics Updated",
            Code = "MATH101",
            CreatedAt = DateTime.UtcNow.AddDays(-1),
            UpdatedAt = DateTime.UtcNow
        };

        _mockSubjectRepository.Setup(r => r.GetSubjectByIdAsync(subjectId)).ReturnsAsync(existingSubject);
        _mockSubjectRepository.Setup(r => r.GetSubjectByCodeAsync("MATH101")).ReturnsAsync(existingSubject);
        _mockSubjectRepository.Setup(r => r.UpdateSubjectAsync(It.IsAny<Subject>())).ReturnsAsync(updatedSubject);
        _mockSubjectRepository.Setup(r => r.SaveChangesAsync()).ReturnsAsync(1);

        // Act
        var result = await _service.UpdateSubjectAsync(subjectId, updateSubject);

        // Assert
        Assert.NotNull(result);
        Assert.Equal("Mathematics Updated", result.Name);
        Assert.Equal("MATH101", result.Code);
        _mockSubjectRepository.Verify(r => r.UpdateSubjectAsync(It.IsAny<Subject>()), Times.Once);
        _mockSubjectRepository.Verify(r => r.SaveChangesAsync(), Times.Once);
    }

    [Fact]
    public async Task UpdateSubjectAsync_SubjectNotFound_ThrowsEntityNotFoundException()
    {
        // Arrange
        const int subjectId = 999;
        var updateSubject = new UpdateSubject { Name = "Mathematics", Code = "MATH101" };
        _mockSubjectRepository.Setup(r => r.GetSubjectByIdAsync(subjectId)).ReturnsAsync((Subject?)null);

        // Act & Assert
        var exception = await Assert.ThrowsAsync<EntityNotFoundException<int>>(() => _service.UpdateSubjectAsync(subjectId, updateSubject));
        Assert.Equal("Subject", exception.EntityName);
        Assert.Equal(subjectId, exception.Key);
    }

    [Fact]
    public async Task UpdateSubjectAsync_DuplicateCodePrecheck_ThrowsEntityAlreadyExistsException()
    {
        // Arrange
        const int subjectId = 1;
        var updateSubject = new UpdateSubject { Name = "Mathematics", Code = "PHYS101" };
        var existingSubject = new Subject { Id = subjectId, Name = "Mathematics", Code = "MATH101" };
        var duplicateSubject = new Subject { Id = 2, Name = "Physics", Code = "PHYS101" };

        _mockSubjectRepository.Setup(r => r.GetSubjectByIdAsync(subjectId)).ReturnsAsync(existingSubject);
        _mockSubjectRepository.Setup(r => r.GetSubjectByCodeAsync("PHYS101")).ReturnsAsync(duplicateSubject);

        // Act & Assert
        var exception = await Assert.ThrowsAsync<EntityAlreadyExistsException<string>>(() => _service.UpdateSubjectAsync(subjectId, updateSubject));
        Assert.Equal("Subject", exception.EntityName);
        Assert.Equal("Code", exception.IdentifierPropertyName);
        Assert.Equal("PHYS101", exception.EntityIdentifier);
    }

    [Fact]
    public async Task UpdateSubjectAsync_DuplicateKeyDbUpdateException_ThrowsEntityAlreadyExistsException()
    {
        // Arrange
        const int subjectId = 1;
        var updateSubject = new UpdateSubject { Name = "Mathematics", Code = "MATH101" };
        var existingSubject = new Subject { Id = subjectId, Name = "Mathematics", Code = "MATH101" };
        var innerException = new Exception("Violation of UNIQUE KEY constraint 'UK_Subjects_Code'");
        var dbUpdateException = new DbUpdateException("Update exception", innerException);

        _mockSubjectRepository.Setup(r => r.GetSubjectByIdAsync(subjectId)).ReturnsAsync(existingSubject);
        _mockSubjectRepository.Setup(r => r.UpdateSubjectAsync(It.IsAny<Subject>())).ReturnsAsync(existingSubject);
        _mockSubjectRepository.Setup(r => r.SaveChangesAsync()).ThrowsAsync(dbUpdateException);

        // Act & Assert
        var exception = await Assert.ThrowsAsync<EntityAlreadyExistsException<string>>(() => _service.UpdateSubjectAsync(subjectId, updateSubject));
        Assert.Equal("Subject", exception.EntityName);
        Assert.Equal("Code", exception.IdentifierPropertyName);
    }

    [Fact]
    public async Task UpdateSubjectAsync_SaveChangesAsyncZero_ThrowsEntityServiceException()
    {
        // Arrange
        const int subjectId = 1;
        var updateSubject = new UpdateSubject { Name = "Mathematics", Code = "MATH101" };
        var existingSubject = new Subject { Id = subjectId, Name = "Mathematics", Code = "MATH101" };

        _mockSubjectRepository.Setup(r => r.GetSubjectByIdAsync(subjectId)).ReturnsAsync(existingSubject);
        _mockSubjectRepository.Setup(r => r.UpdateSubjectAsync(It.IsAny<Subject>())).ReturnsAsync(existingSubject);
        _mockSubjectRepository.Setup(r => r.SaveChangesAsync()).ReturnsAsync(0);

        // Act & Assert
        var exception = await Assert.ThrowsAsync<EntityServiceException>(() => _service.UpdateSubjectAsync(subjectId, updateSubject));
        Assert.Equal("Subject", exception.EntityName);
        Assert.Contains("may have been updated by another process", exception.Message);
    }

    [Fact]
    public async Task UpdateSubjectAsync_RepositoryFailure_WrapsInEntityServiceException()
    {
        // Arrange
        const int subjectId = 1;
        var updateSubject = new UpdateSubject { Name = "Mathematics", Code = "MATH101" };
        var existingSubject = new Subject { Id = subjectId, Name = "Mathematics", Code = "MATH101" };
        var expectedException = new InvalidOperationException("Database error");

        _mockSubjectRepository.Setup(r => r.GetSubjectByIdAsync(subjectId)).ReturnsAsync(existingSubject);
        _mockSubjectRepository.Setup(r => r.UpdateSubjectAsync(It.IsAny<Subject>())).ThrowsAsync(expectedException);

        // Act
        var exception = await Assert.ThrowsAsync<EntityServiceException>(() => _service.UpdateSubjectAsync(subjectId, updateSubject));

        // Assert
        Assert.Equal("Subject", exception.EntityName);
        Assert.Equal($"UpdateSubject: {subjectId}", exception.Operation);
    }

    #endregion

    #region HasSchedulesInSubjectAsync Tests

    [Fact]
    public async Task HasSchedulesInSubjectAsync_Success_ReturnsTrue()
    {
        // Arrange
        const int subjectId = 1;
        _mockSubjectRepository.Setup(r => r.HasSchedulesInSubjectAsync(subjectId)).ReturnsAsync(true);

        // Act
        var result = await _service.HasSchedulesInSubjectAsync(subjectId);

        // Assert
        Assert.True(result);
        _mockSubjectRepository.Verify(r => r.HasSchedulesInSubjectAsync(subjectId), Times.Once);
    }

    [Fact]
    public async Task HasSchedulesInSubjectAsync_Success_ReturnsFalse()
    {
        // Arrange
        const int subjectId = 1;
        _mockSubjectRepository.Setup(r => r.HasSchedulesInSubjectAsync(subjectId)).ReturnsAsync(false);

        // Act
        var result = await _service.HasSchedulesInSubjectAsync(subjectId);

        // Assert
        Assert.False(result);
    }

    [Fact]
    public async Task HasSchedulesInSubjectAsync_RepositoryFailure_WrapsInEntityServiceException()
    {
        // Arrange
        const int subjectId = 1;
        var expectedException = new InvalidOperationException("Database error");
        _mockSubjectRepository.Setup(r => r.HasSchedulesInSubjectAsync(subjectId)).ThrowsAsync(expectedException);

        // Act
        var exception = await Assert.ThrowsAsync<EntityServiceException>(() => _service.HasSchedulesInSubjectAsync(subjectId));

        // Assert
        Assert.Equal("Subject", exception.EntityName);
        Assert.Equal($"HasSchedulesInSubject: {subjectId}", exception.Operation);
        Assert.Same(expectedException, exception.InnerException);
    }

    #endregion

    #region HasEnrollmentsInSubjectAsync Tests

    [Fact]
    public async Task HasEnrollmentsInSubjectAsync_Success_ReturnsTrue()
    {
        // Arrange
        const int subjectId = 1;
        _mockSubjectRepository.Setup(r => r.HasEnrollmentsInSubjectAsync(subjectId)).ReturnsAsync(true);

        // Act
        var result = await _service.HasEnrollmentsInSubjectAsync(subjectId);

        // Assert
        Assert.True(result);
        _mockSubjectRepository.Verify(r => r.HasEnrollmentsInSubjectAsync(subjectId), Times.Once);
    }

    [Fact]
    public async Task HasEnrollmentsInSubjectAsync_Success_ReturnsFalse()
    {
        // Arrange
        const int subjectId = 1;
        _mockSubjectRepository.Setup(r => r.HasEnrollmentsInSubjectAsync(subjectId)).ReturnsAsync(false);

        // Act
        var result = await _service.HasEnrollmentsInSubjectAsync(subjectId);

        // Assert
        Assert.False(result);
    }

    [Fact]
    public async Task HasEnrollmentsInSubjectAsync_RepositoryFailure_WrapsInEntityServiceException()
    {
        // Arrange
        const int subjectId = 1;
        var expectedException = new InvalidOperationException("Database error");
        _mockSubjectRepository.Setup(r => r.HasEnrollmentsInSubjectAsync(subjectId)).ThrowsAsync(expectedException);

        // Act
        var exception = await Assert.ThrowsAsync<EntityServiceException>(() => _service.HasEnrollmentsInSubjectAsync(subjectId));

        // Assert
        Assert.Equal("Subject", exception.EntityName);
        Assert.Equal($"HasEnrollmentsInSubject: {subjectId}", exception.Operation);
        Assert.Same(expectedException, exception.InnerException);
    }

    #endregion

    #region DeleteSubjectAsync Tests

    [Fact]
    public async Task DeleteSubjectAsync_Success_DeletesSubject()
    {
        // Arrange
        const int subjectId = 1;
        var existingSubject = new Subject { Id = subjectId, Name = "Mathematics", Code = "MATH101" };

        _mockSubjectRepository.Setup(r => r.GetSubjectByIdAsync(subjectId)).ReturnsAsync(existingSubject);
        _mockSubjectRepository.Setup(r => r.DeleteSubjectAsync(subjectId)).ReturnsAsync(true);
        _mockSubjectRepository.Setup(r => r.SaveChangesAsync()).ReturnsAsync(1);

        // Act
        await _service.DeleteSubjectAsync(subjectId);

        // Assert
        _mockSubjectRepository.Verify(r => r.DeleteSubjectAsync(subjectId), Times.Once);
        _mockSubjectRepository.Verify(r => r.SaveChangesAsync(), Times.Once);
    }

    [Fact]
    public async Task DeleteSubjectAsync_SubjectNotFound_ThrowsEntityNotFoundException()
    {
        // Arrange
        const int subjectId = 999;
        _mockSubjectRepository.Setup(r => r.GetSubjectByIdAsync(subjectId)).ReturnsAsync((Subject?)null);

        // Act & Assert
        var exception = await Assert.ThrowsAsync<EntityNotFoundException<int>>(() => _service.DeleteSubjectAsync(subjectId));
        Assert.Equal("Subject", exception.EntityName);
        Assert.Equal(subjectId, exception.Key);
    }

    [Fact]
    public async Task DeleteSubjectAsync_RepositoryDeleteReturnsFalse_ThrowsEntityServiceException()
    {
        // Arrange
        const int subjectId = 1;
        var existingSubject = new Subject { Id = subjectId, Name = "Mathematics", Code = "MATH101" };

        _mockSubjectRepository.Setup(r => r.GetSubjectByIdAsync(subjectId)).ReturnsAsync(existingSubject);
        _mockSubjectRepository.Setup(r => r.DeleteSubjectAsync(subjectId)).ReturnsAsync(false);

        // Act & Assert
        var exception = await Assert.ThrowsAsync<EntityServiceException>(() => _service.DeleteSubjectAsync(subjectId));
        Assert.Equal("Subject", exception.EntityName);
        Assert.Contains("Failed to delete", exception.Message);
    }

    [Fact]
    public async Task DeleteSubjectAsync_SaveChangesAsyncZero_ThrowsEntityServiceException()
    {
        // Arrange
        const int subjectId = 1;
        var existingSubject = new Subject { Id = subjectId, Name = "Mathematics", Code = "MATH101" };

        _mockSubjectRepository.Setup(r => r.GetSubjectByIdAsync(subjectId)).ReturnsAsync(existingSubject);
        _mockSubjectRepository.Setup(r => r.DeleteSubjectAsync(subjectId)).ReturnsAsync(true);
        _mockSubjectRepository.Setup(r => r.SaveChangesAsync()).ReturnsAsync(0);

        // Act & Assert
        var exception = await Assert.ThrowsAsync<EntityServiceException>(() => _service.DeleteSubjectAsync(subjectId));
        Assert.Equal("Subject", exception.EntityName);
        Assert.Contains("may have been deleted by another process", exception.Message);
    }

    [Fact]
    public async Task DeleteSubjectAsync_SchedulesForeignKeyConflict_ThrowsEntityConflictException()
    {
        // Arrange
        const int subjectId = 1;
        var existingSubject = new Subject { Id = subjectId, Name = "Mathematics", Code = "MATH101" };
        var innerException = new Exception("The DELETE statement conflicted with the REFERENCE constraint \"FK_Schedules_Subjects\"");
        var dbUpdateException = new DbUpdateException("Update exception", innerException);

        _mockSubjectRepository.Setup(r => r.GetSubjectByIdAsync(subjectId)).ReturnsAsync(existingSubject);
        _mockSubjectRepository.Setup(r => r.DeleteSubjectAsync(subjectId)).ReturnsAsync(true);
        _mockSubjectRepository.Setup(r => r.SaveChangesAsync()).ThrowsAsync(dbUpdateException);

        // Act & Assert
        var exception = await Assert.ThrowsAsync<EntityConflictException>(() => _service.DeleteSubjectAsync(subjectId));
        Assert.Equal("Subject", exception.EntityName);
        Assert.Equal("schedules", exception.ConflictType);
    }

    [Fact]
    public async Task DeleteSubjectAsync_EnrollmentsForeignKeyConflict_ThrowsEntityConflictException()
    {
        // Arrange
        const int subjectId = 1;
        var existingSubject = new Subject { Id = subjectId, Name = "Mathematics", Code = "MATH101" };
        var innerException = new Exception("The DELETE statement conflicted with the REFERENCE constraint \"FK_StudentEnrollments_Subjects\"");
        var dbUpdateException = new DbUpdateException("Update exception", innerException);

        _mockSubjectRepository.Setup(r => r.GetSubjectByIdAsync(subjectId)).ReturnsAsync(existingSubject);
        _mockSubjectRepository.Setup(r => r.DeleteSubjectAsync(subjectId)).ReturnsAsync(true);
        _mockSubjectRepository.Setup(r => r.SaveChangesAsync()).ThrowsAsync(dbUpdateException);

        // Act & Assert
        var exception = await Assert.ThrowsAsync<EntityConflictException>(() => _service.DeleteSubjectAsync(subjectId));
        Assert.Equal("Subject", exception.EntityName);
        Assert.Equal("enrollments", exception.ConflictType);
    }

    [Fact]
    public async Task DeleteSubjectAsync_GenericForeignKeyConflict_ThrowsEntityConflictException()
    {
        // Arrange
        const int subjectId = 1;
        var existingSubject = new Subject { Id = subjectId, Name = "Mathematics", Code = "MATH101" };
        var innerException = new Exception("The DELETE statement conflicted with the REFERENCE constraint \"FK_Unknown_Subjects\"");
        var dbUpdateException = new DbUpdateException("Update exception", innerException);

        _mockSubjectRepository.Setup(r => r.GetSubjectByIdAsync(subjectId)).ReturnsAsync(existingSubject);
        _mockSubjectRepository.Setup(r => r.DeleteSubjectAsync(subjectId)).ReturnsAsync(true);
        _mockSubjectRepository.Setup(r => r.SaveChangesAsync()).ThrowsAsync(dbUpdateException);

        // Act & Assert
        var exception = await Assert.ThrowsAsync<EntityConflictException>(() => _service.DeleteSubjectAsync(subjectId));
        Assert.Equal("Subject", exception.EntityName);
        Assert.Equal("dependencies", exception.ConflictType);
    }

    [Fact]
    public async Task DeleteSubjectAsync_RepositoryFailure_WrapsInEntityServiceException()
    {
        // Arrange
        const int subjectId = 1;
        var existingSubject = new Subject { Id = subjectId, Name = "Mathematics", Code = "MATH101" };
        var expectedException = new InvalidOperationException("Database error");

        _mockSubjectRepository.Setup(r => r.GetSubjectByIdAsync(subjectId)).ReturnsAsync(existingSubject);
        _mockSubjectRepository.Setup(r => r.DeleteSubjectAsync(subjectId)).ThrowsAsync(expectedException);

        // Act
        var exception = await Assert.ThrowsAsync<EntityServiceException>(() => _service.DeleteSubjectAsync(subjectId));

        // Assert
        Assert.Equal("Subject", exception.EntityName);
        Assert.Equal($"DeleteSubject: {subjectId}", exception.Operation);
    }

    #endregion
}
