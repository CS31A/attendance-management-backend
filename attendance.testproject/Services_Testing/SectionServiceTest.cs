using attendance_monitoring.Exceptions;
using attendance_monitoring.IRepository;
using attendance_monitoring.Classes;
using attendance_monitoring.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace attendance.testproject.Services_Testing;

public class SectionServiceTest
{
    private readonly Mock<ISectionRepository> _mockSectionRepository;
    private readonly Mock<ILogger<SectionService>> _mockLogger;
    private readonly SectionService _service;

    public SectionServiceTest()
    {
        _mockSectionRepository = new Mock<ISectionRepository>();
        _mockLogger = new Mock<ILogger<SectionService>>();
        _service = new SectionService(_mockSectionRepository.Object, _mockLogger.Object);
    }

    [Fact]
    public async Task GetSectionByUuidAsync_ReturnsSection_WhenFound()
    {
        var sectionUuid = Guid.NewGuid();
        var section = new Section { Id = sectionUuid, Name = "CS-3A", CourseId = Guid.NewGuid() };
        _mockSectionRepository
            .Setup(repository => repository.GetSectionByUuidAsync(sectionUuid))
            .ReturnsAsync(section);

        var result = await _service.GetSectionByUuidAsync(sectionUuid);

        Assert.Same(section, result);
    }

    [Fact]
    public async Task GetSectionByUuidAsync_ThrowsEntityNotFoundException_WhenMissing()
    {
        var sectionUuid = Guid.NewGuid();
        _mockSectionRepository
            .Setup(repository => repository.GetSectionByUuidAsync(sectionUuid))
            .ReturnsAsync((Section?)null);

        var exception = await Assert.ThrowsAsync<EntityNotFoundException<Guid>>(() => _service.GetSectionByUuidAsync(sectionUuid));

        Assert.Equal("Section", exception.EntityName);
        Assert.Equal(sectionUuid, exception.Key);
    }

    [Fact]
    public async Task HasSchedulesInSectionAsync_ReturnsRepositoryResult()
    {
        // Arrange
        var sectionId = Guid.NewGuid();
        _mockSectionRepository
            .Setup(repository => repository.HasSchedulesInSectionAsync(sectionId))
            .ReturnsAsync(true);

        // Act
        var result = await _service.HasSchedulesInSectionAsync(sectionId);

        // Assert
        Assert.True(result);
        _mockSectionRepository.Verify(repository => repository.HasSchedulesInSectionAsync(sectionId), Times.Once);
    }

    [Fact]
    public async Task HasSchedulesInSectionAsync_WrapsUnexpectedRepositoryFailures()
    {
        // Arrange
        var sectionId = Guid.NewGuid();
        var expectedException = new InvalidOperationException("Schedule lookup failed");
        _mockSectionRepository
            .Setup(repository => repository.HasSchedulesInSectionAsync(sectionId))
            .ThrowsAsync(expectedException);

        // Act
        var exception = await Assert.ThrowsAsync<EntityServiceException>(() => _service.HasSchedulesInSectionAsync(sectionId));

        // Assert
        Assert.Equal("Section", exception.EntityName);
        Assert.Equal($"HasSchedulesInSection: {sectionId}", exception.Operation);
        Assert.Equal("Error checking section dependencies", exception.Message);
        Assert.Same(expectedException, exception.InnerException);
    }

    [Fact]
    public async Task DeleteSectionAsync_Success_DeletesSection()
    {
        // Arrange
        var sectionId = Guid.NewGuid();
        _mockSectionRepository
            .Setup(repository => repository.DeleteSectionAsync(sectionId))
            .ReturnsAsync(true);
        _mockSectionRepository
            .Setup(repository => repository.SaveChangesAsync())
            .ReturnsAsync(1);

        // Act
        await _service.DeleteSectionAsync(sectionId);

        // Assert
        _mockSectionRepository.Verify(repository => repository.DeleteSectionAsync(sectionId), Times.Once);
        _mockSectionRepository.Verify(repository => repository.SaveChangesAsync(), Times.Once);
    }

    [Fact]
    public async Task DeleteSectionAsync_NotFound_ThrowsEntityNotFoundException()
    {
        // Arrange
        var sectionId = Guid.NewGuid();
        _mockSectionRepository
            .Setup(repository => repository.DeleteSectionAsync(sectionId))
            .ReturnsAsync(false);

        // Act & Assert
        var exception = await Assert.ThrowsAsync<EntityNotFoundException<Guid>>(() => _service.DeleteSectionAsync(sectionId));
        Assert.Equal("Section", exception.EntityName);
        Assert.Equal(sectionId, exception.Key);
    }

    [Fact]
    public async Task DeleteSectionAsync_SchedulesConflict_ThrowsEntityConflictException()
    {
        // Arrange
        var sectionId = Guid.NewGuid();
        _mockSectionRepository
            .Setup(repository => repository.DeleteSectionAsync(sectionId))
            .ReturnsAsync(true);

        var innerException = new Exception("The DELETE statement conflicted with the REFERENCE constraint \"FK_Schedules_Sections\". The conflict occurred in database \"attendance_db\", table \"dbo.Schedules\"");
        var dbUpdateException = new DbUpdateException("Update exception", innerException);

        _mockSectionRepository
            .Setup(repository => repository.SaveChangesAsync())
            .ThrowsAsync(dbUpdateException);

        // Act & Assert
        var exception = await Assert.ThrowsAsync<EntityConflictException>(() => _service.DeleteSectionAsync(sectionId));
        Assert.Equal("Section", exception.EntityName);
        Assert.Equal("schedules", exception.ConflictType);
        Assert.Contains("schedules", exception.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task DeleteSectionAsync_StudentsConflict_ThrowsEntityConflictException()
    {
        // Arrange
        var sectionId = Guid.NewGuid();
        _mockSectionRepository
            .Setup(repository => repository.DeleteSectionAsync(sectionId))
            .ReturnsAsync(true);

        var innerException = new Exception("The DELETE statement conflicted with the REFERENCE constraint \"FK_Students_Sections\". The conflict occurred in database \"attendance_db\", table \"dbo.Students\"");
        var dbUpdateException = new DbUpdateException("Update exception", innerException);

        _mockSectionRepository
            .Setup(repository => repository.SaveChangesAsync())
            .ThrowsAsync(dbUpdateException);

        // Act & Assert
        var exception = await Assert.ThrowsAsync<EntityConflictException>(() => _service.DeleteSectionAsync(sectionId));
        Assert.Equal("Section", exception.EntityName);
        Assert.Equal("students", exception.ConflictType);
        Assert.Contains("students", exception.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task DeleteSectionAsync_EnrollmentsConflict_ThrowsEntityConflictException()
    {
        // Arrange
        var sectionId = Guid.NewGuid();
        _mockSectionRepository
            .Setup(repository => repository.DeleteSectionAsync(sectionId))
            .ReturnsAsync(true);

        var innerException = new Exception("The DELETE statement conflicted with the REFERENCE constraint \"FK_StudentEnrollments_Sections\". The conflict occurred in database \"attendance_db\", table \"dbo.StudentEnrollments\"");
        var dbUpdateException = new DbUpdateException("Update exception", innerException);

        _mockSectionRepository
            .Setup(repository => repository.SaveChangesAsync())
            .ThrowsAsync(dbUpdateException);

        // Act & Assert
        var exception = await Assert.ThrowsAsync<EntityConflictException>(() => _service.DeleteSectionAsync(sectionId));
        Assert.Equal("Section", exception.EntityName);
        Assert.Equal("enrollments", exception.ConflictType);
        Assert.Contains("enrollments", exception.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task DeleteSectionAsync_UnrelatedException_ThrowsEntityServiceException()
    {
        // Arrange
        var sectionId = Guid.NewGuid();
        _mockSectionRepository
            .Setup(repository => repository.DeleteSectionAsync(sectionId))
            .ReturnsAsync(true);

        var expectedException = new InvalidOperationException("Unexpected database error");
        _mockSectionRepository
            .Setup(repository => repository.SaveChangesAsync())
            .ThrowsAsync(expectedException);

        // Act & Assert
        var exception = await Assert.ThrowsAsync<EntityServiceException>(() => _service.DeleteSectionAsync(sectionId));
        Assert.Equal("Section", exception.EntityName);
        Assert.Equal($"DeleteSection: {sectionId}", exception.Operation);
        Assert.Same(expectedException, exception.InnerException);
    }

    [Fact]
    public async Task DeleteSectionAsync_PostgreSQLSchedulesConflict_ThrowsEntityConflictException()
    {
        // Arrange
        var sectionId = Guid.NewGuid();
        _mockSectionRepository
            .Setup(repository => repository.DeleteSectionAsync(sectionId))
            .ReturnsAsync(true);

        var innerException = new Exception("23503: insert or update on table \"Schedules\" violates foreign key constraint \"FK_Schedules_Sections\"");
        var dbUpdateException = new DbUpdateException("Update exception", innerException);

        _mockSectionRepository
            .Setup(repository => repository.SaveChangesAsync())
            .ThrowsAsync(dbUpdateException);

        // Act & Assert
        var exception = await Assert.ThrowsAsync<EntityConflictException>(() => _service.DeleteSectionAsync(sectionId));
        Assert.Equal("Section", exception.EntityName);
        Assert.Equal("schedules", exception.ConflictType);
    }

    [Fact]
    public async Task DeleteSectionAsync_GenericConstraintViolation_ThrowsEntityServiceException()
    {
        // Arrange
        var sectionId = Guid.NewGuid();
        _mockSectionRepository
            .Setup(repository => repository.DeleteSectionAsync(sectionId))
            .ReturnsAsync(true);

        // A constraint violation that is NOT a foreign key violation
        // This should NOT be matched as a dependency conflict
        var innerException = new Exception("constraint violation: value violates check constraint \"CK_Sections_NameNotEmpty\"");
        var dbUpdateException = new DbUpdateException("Update exception", innerException);

        _mockSectionRepository
            .Setup(repository => repository.SaveChangesAsync())
            .ThrowsAsync(dbUpdateException);

        // Act & Assert
        var exception = await Assert.ThrowsAsync<EntityServiceException>(() => _service.DeleteSectionAsync(sectionId));
        Assert.Equal("Section", exception.EntityName);
        Assert.Equal($"DeleteSection: {sectionId}", exception.Operation);
        Assert.Same(dbUpdateException, exception.InnerException);
    }
}
