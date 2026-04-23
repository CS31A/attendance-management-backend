using attendance_monitoring.Classes;
using attendance_monitoring.Exceptions;
using attendance_monitoring.IRepository;
using attendance_monitoring.Models.DTO.Request;
using attendance_monitoring.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Moq;

namespace attendance.testproject.Services_Testing;

public class ClassroomServiceTest
{
    private readonly Mock<IClassroomRepository> _mockClassroomRepository;
    private readonly Mock<ILogger<ClassroomService>> _mockLogger;
    private readonly ClassroomService _service;

    public ClassroomServiceTest()
    {
        _mockClassroomRepository = new Mock<IClassroomRepository>();
        _mockLogger = new Mock<ILogger<ClassroomService>>();
        _service = new ClassroomService(_mockClassroomRepository.Object, _mockLogger.Object);
    }

    [Fact]
    public void Constructor_NullRepository_ThrowsArgumentNullException()
    {
        var exception = Assert.Throws<ArgumentNullException>(() => new ClassroomService(null!, _mockLogger.Object));

        Assert.Equal("classroomRepository", exception.ParamName);
    }

    [Fact]
    public void Constructor_NullLogger_ThrowsArgumentNullException()
    {
        var exception = Assert.Throws<ArgumentNullException>(() => new ClassroomService(_mockClassroomRepository.Object, null!));

        Assert.Equal("logger", exception.ParamName);
    }

    [Fact]
    public async Task GetAllClassroomsAsync_ReturnsMaterializedClassrooms()
    {
        var classrooms = new List<Classroom>
        {
            new() { Id = 1, Name = "Room A" },
            new() { Id = 2, Name = "Room B" },
        };

        _mockClassroomRepository
            .Setup(repository => repository.GetAllClassroomsAsync())
            .ReturnsAsync(classrooms);

        var result = await _service.GetAllClassroomsAsync();

        var materialized = Assert.IsType<List<Classroom>>(result);
        Assert.Collection(materialized,
            classroom => Assert.Equal("Room A", classroom.Name),
            classroom => Assert.Equal("Room B", classroom.Name));
    }

    [Fact]
    public async Task GetAllClassroomsAsync_WrapsUnexpectedFailures()
    {
        var expectedException = new InvalidOperationException("Lookup failed");
        _mockClassroomRepository
            .Setup(repository => repository.GetAllClassroomsAsync())
            .ThrowsAsync(expectedException);

        var exception = await Assert.ThrowsAsync<EntityServiceException>(() => _service.GetAllClassroomsAsync());

        Assert.Equal("Classroom", exception.EntityName);
        Assert.Equal("GetAllClassrooms", exception.Operation);
        Assert.Same(expectedException, exception.InnerException);
    }

    [Fact]
    public async Task GetClassroomByIdAsync_ReturnsClassroom_WhenFound()
    {
        var classroom = new Classroom { Id = 7, Name = "Room 207" };
        _mockClassroomRepository
            .Setup(repository => repository.GetClassroomByIdAsync(7))
            .ReturnsAsync(classroom);

        var result = await _service.GetClassroomByIdAsync(7);

        Assert.Same(classroom, result);
    }

    [Fact]
    public async Task GetClassroomByIdAsync_ThrowsEntityNotFoundException_WhenMissing()
    {
        _mockClassroomRepository
            .Setup(repository => repository.GetClassroomByIdAsync(7))
            .ReturnsAsync((Classroom?)null);

        var exception = await Assert.ThrowsAsync<EntityNotFoundException<int>>(() => _service.GetClassroomByIdAsync(7));

        Assert.Equal("Classroom", exception.EntityName);
        Assert.Equal(7, exception.Key);
    }

    [Fact]
    public async Task GetClassroomByIdAsync_WrapsUnexpectedFailures()
    {
        var expectedException = new InvalidOperationException("Lookup failed");
        _mockClassroomRepository
            .Setup(repository => repository.GetClassroomByIdAsync(7))
            .ThrowsAsync(expectedException);

        var exception = await Assert.ThrowsAsync<EntityServiceException>(() => _service.GetClassroomByIdAsync(7));

        Assert.Equal("Classroom", exception.EntityName);
        Assert.Equal("GetClassroomById: 7", exception.Operation);
        Assert.Same(expectedException, exception.InnerException);
    }

    [Fact]
    public async Task GetClassroomByUuidAsync_ReturnsClassroom_WhenFound()
    {
        var classroomUuid = Guid.NewGuid();
        var classroom = new Classroom { Id = 7, Uuid = classroomUuid, Name = "Room 207" };
        _mockClassroomRepository
            .Setup(repository => repository.GetClassroomByUuidAsync(classroomUuid))
            .ReturnsAsync(classroom);

        var result = await _service.GetClassroomByUuidAsync(classroomUuid);

        Assert.Same(classroom, result);
    }

    [Fact]
    public async Task GetClassroomByUuidAsync_ThrowsEntityNotFoundException_WhenMissing()
    {
        var classroomUuid = Guid.NewGuid();
        _mockClassroomRepository
            .Setup(repository => repository.GetClassroomByUuidAsync(classroomUuid))
            .ReturnsAsync((Classroom?)null);

        var exception = await Assert.ThrowsAsync<EntityNotFoundException<Guid>>(() => _service.GetClassroomByUuidAsync(classroomUuid));

        Assert.Equal("Classroom", exception.EntityName);
        Assert.Equal(classroomUuid, exception.Key);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public async Task CreateClassroomAsync_BlankName_ThrowsValidationException(string? name)
    {
        var createClassroom = new CreateClassroom { Name = name! };

        var exception = await Assert.ThrowsAsync<ValidationException>(() => _service.CreateClassroomAsync(createClassroom));

        Assert.Equal("Classroom name is required", exception.Message);
    }

    [Fact]
    public async Task CreateClassroomAsync_DuplicateNamePreCheck_ThrowsEntityAlreadyExistsException()
    {
        _mockClassroomRepository
            .Setup(repository => repository.GetClassroomByNameAsync("Room 101"))
            .ReturnsAsync(new Classroom { Id = 1, Name = "Room 101" });

        var exception = await Assert.ThrowsAsync<EntityAlreadyExistsException<string>>(
            () => _service.CreateClassroomAsync(new CreateClassroom { Name = "Room 101" }));

        Assert.Equal("Classroom", exception.EntityName);
        Assert.Equal("Name", exception.IdentifierPropertyName);
        Assert.Equal("Room 101", exception.EntityIdentifier);
    }

    [Fact]
    public async Task CreateClassroomAsync_ValidInput_CreatesClassroom()
    {
        Classroom? capturedClassroom = null;
        var createdClassroom = new Classroom { Id = 3, Name = "Lab 1" };

        _mockClassroomRepository
            .Setup(repository => repository.GetClassroomByNameAsync("Lab 1"))
            .ReturnsAsync((Classroom?)null);
        _mockClassroomRepository
            .Setup(repository => repository.CreateClassroom(It.IsAny<Classroom>()))
            .Callback<Classroom>(classroom => capturedClassroom = classroom)
            .ReturnsAsync(createdClassroom);
        _mockClassroomRepository
            .Setup(repository => repository.SaveChangesAsync())
            .ReturnsAsync(1);

        var result = await _service.CreateClassroomAsync(new CreateClassroom { Name = "Lab 1" });

        Assert.Same(createdClassroom, result);
        Assert.NotNull(capturedClassroom);
        Assert.Equal("Lab 1", capturedClassroom.Name);
        _mockClassroomRepository.Verify(repository => repository.CreateClassroom(It.IsAny<Classroom>()), Times.Once);
        _mockClassroomRepository.Verify(repository => repository.SaveChangesAsync(), Times.Once);
    }

    [Fact]
    public async Task CreateClassroomAsync_UniqueConstraintViolation_ThrowsEntityAlreadyExistsException()
    {
        var dbUpdateException = new DbUpdateException(
            "Duplicate",
            new Exception("Cannot insert duplicate key row with unique index 'IX_Classrooms_Name'"));

        _mockClassroomRepository
            .Setup(repository => repository.GetClassroomByNameAsync("Lab 1"))
            .ReturnsAsync((Classroom?)null);
        _mockClassroomRepository
            .Setup(repository => repository.CreateClassroom(It.IsAny<Classroom>()))
            .ThrowsAsync(dbUpdateException);

        var exception = await Assert.ThrowsAsync<EntityAlreadyExistsException<string>>(
            () => _service.CreateClassroomAsync(new CreateClassroom { Name = "Lab 1" }));

        Assert.Equal("Classroom", exception.EntityName);
        Assert.Equal("Name", exception.IdentifierPropertyName);
        Assert.Equal("Lab 1", exception.EntityIdentifier);
    }

    [Fact]
    public async Task CreateClassroomAsync_WrapsUnexpectedFailures()
    {
        var expectedException = new InvalidOperationException("Create failed");
        _mockClassroomRepository
            .Setup(repository => repository.GetClassroomByNameAsync("Lab 1"))
            .ThrowsAsync(expectedException);

        var exception = await Assert.ThrowsAsync<EntityServiceException>(
            () => _service.CreateClassroomAsync(new CreateClassroom { Name = "Lab 1" }));

        Assert.Equal("Classroom", exception.EntityName);
        Assert.Equal("CreateClassroom", exception.Operation);
        Assert.Same(expectedException, exception.InnerException);
    }

    [Fact]
    public async Task UpdateClassroomAsync_NotFound_ThrowsEntityNotFoundException()
    {
        _mockClassroomRepository
            .Setup(repository => repository.GetClassroomByIdAsync(8))
            .ReturnsAsync((Classroom?)null);

        var exception = await Assert.ThrowsAsync<EntityNotFoundException<int>>(
            () => _service.UpdateClassroomAsync(8, new UpdateClassroom { Name = "Updated" }));

        Assert.Equal("Classroom", exception.EntityName);
        Assert.Equal(8, exception.Key);
    }

    [Fact]
    public async Task UpdateClassroomAsync_ValidInput_UpdatesClassroom()
    {
        var existingClassroom = new Classroom { Id = 8, Name = "Old" };
        Classroom? updatedEntity = null;

        _mockClassroomRepository
            .Setup(repository => repository.GetClassroomByIdAsync(8))
            .ReturnsAsync(existingClassroom);
        _mockClassroomRepository
            .Setup(repository => repository.GetClassroomByNameAsync("New"))
            .ReturnsAsync((Classroom?)null);
        _mockClassroomRepository
            .Setup(repository => repository.UpdateClassroomAsync(It.IsAny<Classroom>()))
            .Callback<Classroom>(classroom => updatedEntity = classroom)
            .ReturnsAsync((Classroom classroom) => classroom);
        _mockClassroomRepository
            .Setup(repository => repository.SaveChangesAsync())
            .ReturnsAsync(1);

        var result = await _service.UpdateClassroomAsync(8, new UpdateClassroom { Name = "New" });

        Assert.Equal("New", result.Name);
        Assert.NotNull(updatedEntity);
        Assert.Equal("New", updatedEntity.Name);
        _mockClassroomRepository.Verify(repository => repository.SaveChangesAsync(), Times.Once);
    }

    [Fact]
    public async Task UpdateClassroomAsync_EmptyName_LeavesNameUnchanged()
    {
        var existingClassroom = new Classroom { Id = 8, Name = "Old" };

        _mockClassroomRepository
            .Setup(repository => repository.GetClassroomByIdAsync(8))
            .ReturnsAsync(existingClassroom);
        _mockClassroomRepository
            .Setup(repository => repository.UpdateClassroomAsync(It.IsAny<Classroom>()))
            .ReturnsAsync((Classroom classroom) => classroom);
        _mockClassroomRepository
            .Setup(repository => repository.SaveChangesAsync())
            .ReturnsAsync(1);

        var result = await _service.UpdateClassroomAsync(8, new UpdateClassroom { Name = string.Empty });

        Assert.Equal("Old", result.Name);
        _mockClassroomRepository.Verify(repository => repository.GetClassroomByNameAsync(It.IsAny<string>()), Times.Never);
    }

    [Fact]
    public async Task UpdateClassroomAsync_DuplicateName_ThrowsEntityAlreadyExistsException()
    {
        _mockClassroomRepository
            .Setup(repository => repository.GetClassroomByIdAsync(8))
            .ReturnsAsync(new Classroom { Id = 8, Name = "Old" });
        _mockClassroomRepository
            .Setup(repository => repository.GetClassroomByNameAsync("Taken"))
            .ReturnsAsync(new Classroom { Id = 11, Name = "Taken" });

        var exception = await Assert.ThrowsAsync<EntityAlreadyExistsException<string>>(
            () => _service.UpdateClassroomAsync(8, new UpdateClassroom { Name = "Taken" }));

        Assert.Equal("Classroom", exception.EntityName);
        Assert.Equal("Name", exception.IdentifierPropertyName);
        Assert.Equal("Taken", exception.EntityIdentifier);
    }

    [Fact]
    public async Task UpdateClassroomAsync_UniqueConstraintViolation_ThrowsEntityAlreadyExistsException()
    {
        var dbUpdateException = new DbUpdateException(
            "Duplicate",
            new Exception("duplicate key value violates unique constraint \"IX_Classrooms_Name\""));

        _mockClassroomRepository
            .Setup(repository => repository.GetClassroomByIdAsync(8))
            .ReturnsAsync(new Classroom { Id = 8, Name = "Old" });
        _mockClassroomRepository
            .Setup(repository => repository.GetClassroomByNameAsync("New"))
            .ReturnsAsync((Classroom?)null);
        _mockClassroomRepository
            .Setup(repository => repository.UpdateClassroomAsync(It.IsAny<Classroom>()))
            .ThrowsAsync(dbUpdateException);

        var exception = await Assert.ThrowsAsync<EntityAlreadyExistsException<string>>(
            () => _service.UpdateClassroomAsync(8, new UpdateClassroom { Name = "New" }));

        Assert.Equal("Classroom", exception.EntityName);
        Assert.Equal("Name", exception.IdentifierPropertyName);
        Assert.Equal("New", exception.EntityIdentifier);
    }

    [Fact]
    public async Task UpdateClassroomAsync_NoRowsAffected_ThrowsEntityServiceException()
    {
        _mockClassroomRepository
            .Setup(repository => repository.GetClassroomByIdAsync(8))
            .ReturnsAsync(new Classroom { Id = 8, Name = "Old" });
        _mockClassroomRepository
            .Setup(repository => repository.GetClassroomByNameAsync("New"))
            .ReturnsAsync((Classroom?)null);
        _mockClassroomRepository
            .Setup(repository => repository.UpdateClassroomAsync(It.IsAny<Classroom>()))
            .ReturnsAsync((Classroom classroom) => classroom);
        _mockClassroomRepository
            .Setup(repository => repository.SaveChangesAsync())
            .ReturnsAsync(0);

        var exception = await Assert.ThrowsAsync<EntityServiceException>(
            () => _service.UpdateClassroomAsync(8, new UpdateClassroom { Name = "New" }));

        Assert.Equal("Classroom", exception.EntityName);
        Assert.Equal("UpdateClassroom: 8", exception.Operation);
        Assert.Contains("updated by another process", exception.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task UpdateClassroomAsync_WrapsUnexpectedFailures()
    {
        var expectedException = new InvalidOperationException("Update failed");
        _mockClassroomRepository
            .Setup(repository => repository.GetClassroomByIdAsync(8))
            .ThrowsAsync(expectedException);

        var exception = await Assert.ThrowsAsync<EntityServiceException>(
            () => _service.UpdateClassroomAsync(8, new UpdateClassroom { Name = "New" }));

        Assert.Equal("Classroom", exception.EntityName);
        Assert.Equal("UpdateClassroom: 8", exception.Operation);
        Assert.Same(expectedException, exception.InnerException);
    }

    [Fact]
    public async Task DeleteClassroomAsync_NotFound_ThrowsEntityNotFoundException()
    {
        _mockClassroomRepository
            .Setup(repository => repository.GetClassroomByIdAsync(5))
            .ReturnsAsync((Classroom?)null);

        var exception = await Assert.ThrowsAsync<EntityNotFoundException<int>>(() => _service.DeleteClassroomAsync(5));

        Assert.Equal("Classroom", exception.EntityName);
        Assert.Equal(5, exception.Key);
    }

    [Fact]
    public async Task DeleteClassroomAsync_Success_DeletesClassroom()
    {
        _mockClassroomRepository
            .Setup(repository => repository.GetClassroomByIdAsync(5))
            .ReturnsAsync(new Classroom { Id = 5, Name = "Room 5" });
        _mockClassroomRepository
            .Setup(repository => repository.DeleteClassroomAsync(5))
            .ReturnsAsync(true);
        _mockClassroomRepository
            .Setup(repository => repository.SaveChangesAsync())
            .ReturnsAsync(1);

        await _service.DeleteClassroomAsync(5);

        _mockClassroomRepository.Verify(repository => repository.DeleteClassroomAsync(5), Times.Once);
        _mockClassroomRepository.Verify(repository => repository.SaveChangesAsync(), Times.Once);
    }

    [Fact]
    public async Task DeleteClassroomAsync_DeleteReturnsFalse_ThrowsEntityServiceException()
    {
        _mockClassroomRepository
            .Setup(repository => repository.GetClassroomByIdAsync(5))
            .ReturnsAsync(new Classroom { Id = 5, Name = "Room 5" });
        _mockClassroomRepository
            .Setup(repository => repository.DeleteClassroomAsync(5))
            .ReturnsAsync(false);

        var exception = await Assert.ThrowsAsync<EntityServiceException>(() => _service.DeleteClassroomAsync(5));

        Assert.Equal("Classroom", exception.EntityName);
        Assert.Equal("DeleteClassroom: 5", exception.Operation);
    }

    [Fact]
    public async Task DeleteClassroomAsync_NoRowsAffected_ThrowsEntityServiceException()
    {
        _mockClassroomRepository
            .Setup(repository => repository.GetClassroomByIdAsync(5))
            .ReturnsAsync(new Classroom { Id = 5, Name = "Room 5" });
        _mockClassroomRepository
            .Setup(repository => repository.DeleteClassroomAsync(5))
            .ReturnsAsync(true);
        _mockClassroomRepository
            .Setup(repository => repository.SaveChangesAsync())
            .ReturnsAsync(0);

        var exception = await Assert.ThrowsAsync<EntityServiceException>(() => _service.DeleteClassroomAsync(5));

        Assert.Equal("Classroom", exception.EntityName);
        Assert.Equal("DeleteClassroom: 5", exception.Operation);
        Assert.Contains("deleted by another process", exception.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Theory]
    [InlineData("The DELETE statement conflicted with the REFERENCE constraint \"FK_Sessions_Classrooms_ActualRoomId\"", "sessions")]
    [InlineData("The DELETE statement conflicted with the REFERENCE constraint \"FK_Schedules_Classrooms\"", "schedules")]
    [InlineData("23503: update or delete on table \"Classrooms\" violates foreign key constraint \"FK_Unknown\"", "dependencies")]
    public async Task DeleteClassroomAsync_ForeignKeyViolation_ThrowsEntityConflictException(string innerMessage, string expectedConflictType)
    {
        var dbUpdateException = new DbUpdateException("Delete failed", new Exception(innerMessage));

        _mockClassroomRepository
            .Setup(repository => repository.GetClassroomByIdAsync(5))
            .ReturnsAsync(new Classroom { Id = 5, Name = "Room 5" });
        _mockClassroomRepository
            .Setup(repository => repository.DeleteClassroomAsync(5))
            .ReturnsAsync(true);
        _mockClassroomRepository
            .Setup(repository => repository.SaveChangesAsync())
            .ThrowsAsync(dbUpdateException);

        var exception = await Assert.ThrowsAsync<EntityConflictException>(() => _service.DeleteClassroomAsync(5));

        Assert.Equal("Classroom", exception.EntityName);
        Assert.Equal(expectedConflictType, exception.ConflictType);
    }

    [Fact]
    public async Task DeleteClassroomAsync_WrapsUnexpectedFailures()
    {
        var expectedException = new InvalidOperationException("Delete failed");
        _mockClassroomRepository
            .Setup(repository => repository.GetClassroomByIdAsync(5))
            .ThrowsAsync(expectedException);

        var exception = await Assert.ThrowsAsync<EntityServiceException>(() => _service.DeleteClassroomAsync(5));

        Assert.Equal("Classroom", exception.EntityName);
        Assert.Equal("DeleteClassroom: 5", exception.Operation);
        Assert.Same(expectedException, exception.InnerException);
    }

    [Fact]
    public async Task HasSchedulesInClassroomAsync_ReturnsRepositoryResult()
    {
        _mockClassroomRepository
            .Setup(repository => repository.HasSchedulesInClassroomAsync(3))
            .ReturnsAsync(true);

        var result = await _service.HasSchedulesInClassroomAsync(3);

        Assert.True(result);
    }

    [Fact]
    public async Task HasSchedulesInClassroomAsync_WrapsUnexpectedFailures()
    {
        var expectedException = new InvalidOperationException("Lookup failed");
        _mockClassroomRepository
            .Setup(repository => repository.HasSchedulesInClassroomAsync(3))
            .ThrowsAsync(expectedException);

        var exception = await Assert.ThrowsAsync<EntityServiceException>(() => _service.HasSchedulesInClassroomAsync(3));

        Assert.Equal("Classroom", exception.EntityName);
        Assert.Equal("HasSchedulesInClassroom: 3", exception.Operation);
        Assert.Same(expectedException, exception.InnerException);
    }

    [Fact]
    public async Task HasSessionsInClassroomAsync_ReturnsRepositoryResult()
    {
        _mockClassroomRepository
            .Setup(repository => repository.HasSessionsInClassroomAsync(3))
            .ReturnsAsync(true);

        var result = await _service.HasSessionsInClassroomAsync(3);

        Assert.True(result);
    }

    [Fact]
    public async Task HasSessionsInClassroomAsync_WrapsUnexpectedFailures()
    {
        var expectedException = new InvalidOperationException("Lookup failed");
        _mockClassroomRepository
            .Setup(repository => repository.HasSessionsInClassroomAsync(3))
            .ThrowsAsync(expectedException);

        var exception = await Assert.ThrowsAsync<EntityServiceException>(() => _service.HasSessionsInClassroomAsync(3));

        Assert.Equal("Classroom", exception.EntityName);
        Assert.Equal("HasSessionsInClassroom: 3", exception.Operation);
        Assert.Same(expectedException, exception.InnerException);
    }
}
