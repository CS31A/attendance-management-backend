using attendance_monitoring.Classes;
using attendance_monitoring.Exceptions;
using attendance_monitoring.IRepository;
using attendance_monitoring.Models.DTO.Request;
using attendance_monitoring.Services;
using attendance_monitoring.Services.Crud;
using Microsoft.Extensions.Logging;
using Moq;

namespace attendance.testproject.Services_Testing;

public class ClassroomServiceTest
{
    private readonly Mock<IClassroomRepository> _mockClassroomRepository;
    private readonly Mock<ICrudService<Classroom, CreateClassroom, UpdateClassroom>> _mockCrudService;
    private readonly Mock<ILogger<ClassroomService>> _mockLogger;
    private readonly ClassroomService _service;

    public ClassroomServiceTest()
    {
        _mockClassroomRepository = new Mock<IClassroomRepository>();
        _mockCrudService = new Mock<ICrudService<Classroom, CreateClassroom, UpdateClassroom>>();
        _mockLogger = new Mock<ILogger<ClassroomService>>();
        _service = new ClassroomService(_mockCrudService.Object, _mockClassroomRepository.Object, _mockLogger.Object);
    }

    #region GetAllClassroomsAsync

    [Fact]
    public async Task GetAllClassroomsAsync_DelegatesToCrudService()
    {
        var classrooms = new List<Classroom>
        {
            new() { Id = Guid.NewGuid(), Name = "Room A" },
            new() { Id = Guid.NewGuid(), Name = "Room B" },
        };
        _mockCrudService.Setup(s => s.GetAllAsync()).ReturnsAsync(classrooms);

        var result = await _service.GetAllClassroomsAsync();

        Assert.Equal(classrooms, result);
        _mockCrudService.Verify(s => s.GetAllAsync(), Times.Once);
    }

    #endregion

    #region GetClassroomByIdAsync

    [Fact]
    public async Task GetClassroomByIdAsync_DelegatesToCrudService()
    {
        var id = Guid.NewGuid();
        var classroom = new Classroom { Id = id, Name = "Room 207" };
        _mockCrudService.Setup(s => s.GetByIdAsync(id)).ReturnsAsync(classroom);

        var result = await _service.GetClassroomByIdAsync(id);

        Assert.Same(classroom, result);
        _mockCrudService.Verify(s => s.GetByIdAsync(id), Times.Once);
    }

    [Fact]
    public async Task GetClassroomByIdAsync_ThrowsEntityNotFoundException_WhenMissing()
    {
        var id = Guid.NewGuid();
        _mockCrudService.Setup(s => s.GetByIdAsync(id))
            .ThrowsAsync(new EntityNotFoundException<Guid>("Classroom", id));

        var exception = await Assert.ThrowsAsync<EntityNotFoundException<Guid>>(
            () => _service.GetClassroomByIdAsync(id));

        Assert.Equal("Classroom", exception.EntityName);
    }

    #endregion

    #region GetClassroomByUuidAsync

    [Fact]
    public async Task GetClassroomByUuidAsync_DelegatesToCrudService()
    {
        var id = Guid.NewGuid();
        var classroom = new Classroom { Id = id, Name = "Room 207" };
        _mockCrudService.Setup(s => s.GetByIdAsync(id)).ReturnsAsync(classroom);

        var result = await _service.GetClassroomByUuidAsync(id);

        Assert.Same(classroom, result);
        _mockCrudService.Verify(s => s.GetByIdAsync(id), Times.Once);
    }

    #endregion

    #region CreateClassroomAsync

    [Fact]
    public async Task CreateClassroomAsync_DelegatesToCrudService()
    {
        var dto = new CreateClassroom { Name = "Lab 1" };
        var created = new Classroom { Id = Guid.NewGuid(), Name = "Lab 1" };
        _mockCrudService.Setup(s => s.CreateAsync(dto)).ReturnsAsync(created);

        var result = await _service.CreateClassroomAsync(dto);

        Assert.Same(created, result);
        _mockCrudService.Verify(s => s.CreateAsync(dto), Times.Once);
    }

    [Fact]
    public async Task CreateClassroomAsync_PropagatesValidationException()
    {
        var dto = new CreateClassroom { Name = "" };
        _mockCrudService.Setup(s => s.CreateAsync(dto))
            .ThrowsAsync(new ValidationException("Name is required"));

        await Assert.ThrowsAsync<ValidationException>(() => _service.CreateClassroomAsync(dto));
    }

    [Fact]
    public async Task CreateClassroomAsync_PropagatesEntityAlreadyExistsException()
    {
        var dto = new CreateClassroom { Name = "Room 101" };
        _mockCrudService.Setup(s => s.CreateAsync(dto))
            .ThrowsAsync(new EntityAlreadyExistsException<string>("Classroom", "Name", "Room 101"));

        var exception = await Assert.ThrowsAsync<EntityAlreadyExistsException<string>>(
            () => _service.CreateClassroomAsync(dto));

        Assert.Equal("Room 101", exception.EntityIdentifier);
    }

    #endregion

    #region UpdateClassroomAsync

    [Fact]
    public async Task UpdateClassroomAsync_DelegatesToCrudService()
    {
        var id = Guid.NewGuid();
        var dto = new UpdateClassroom { Name = "Updated" };
        var updated = new Classroom { Id = id, Name = "Updated" };
        _mockCrudService.Setup(s => s.UpdateAsync(id, dto)).ReturnsAsync(updated);

        var result = await _service.UpdateClassroomAsync(id, dto);

        Assert.Same(updated, result);
        _mockCrudService.Verify(s => s.UpdateAsync(id, dto), Times.Once);
    }

    [Fact]
    public async Task UpdateClassroomAsync_PropagatesEntityNotFoundException()
    {
        var id = Guid.NewGuid();
        var dto = new UpdateClassroom { Name = "Updated" };
        _mockCrudService.Setup(s => s.UpdateAsync(id, dto))
            .ThrowsAsync(new EntityNotFoundException<Guid>("Classroom", id));

        await Assert.ThrowsAsync<EntityNotFoundException<Guid>>(
            () => _service.UpdateClassroomAsync(id, dto));
    }

    #endregion

    #region UpdateClassroomByUuidAsync

    [Fact]
    public async Task UpdateClassroomByUuidAsync_DelegatesToCrudService()
    {
        var id = Guid.NewGuid();
        var dto = new UpdateClassroom { Name = "Updated" };
        var updated = new Classroom { Id = id, Name = "Updated" };
        _mockCrudService.Setup(s => s.UpdateAsync(id, dto)).ReturnsAsync(updated);

        var result = await _service.UpdateClassroomByUuidAsync(id, dto);

        Assert.Same(updated, result);
        _mockCrudService.Verify(s => s.UpdateAsync(id, dto), Times.Once);
    }

    #endregion

    #region DeleteClassroomAsync

    [Fact]
    public async Task DeleteClassroomAsync_DelegatesToCrudService()
    {
        var id = Guid.NewGuid();
        _mockCrudService.Setup(s => s.DeleteAsync(id)).Returns(Task.CompletedTask);

        await _service.DeleteClassroomAsync(id);

        _mockCrudService.Verify(s => s.DeleteAsync(id), Times.Once);
    }

    [Fact]
    public async Task DeleteClassroomAsync_PropagatesEntityNotFoundException()
    {
        var id = Guid.NewGuid();
        _mockCrudService.Setup(s => s.DeleteAsync(id))
            .ThrowsAsync(new EntityNotFoundException<Guid>("Classroom", id));

        await Assert.ThrowsAsync<EntityNotFoundException<Guid>>(
            () => _service.DeleteClassroomAsync(id));
    }

    #endregion

    #region DeleteClassroomByUuidAsync

    [Fact]
    public async Task DeleteClassroomByUuidAsync_DelegatesToCrudService()
    {
        var id = Guid.NewGuid();
        _mockCrudService.Setup(s => s.DeleteAsync(id)).Returns(Task.CompletedTask);

        await _service.DeleteClassroomByUuidAsync(id);

        _mockCrudService.Verify(s => s.DeleteAsync(id), Times.Once);
    }

    #endregion

    #region Dependency Check Operations

    [Fact]
    public async Task HasSchedulesInClassroomAsync_ReturnsRepositoryResult()
    {
        _mockClassroomRepository
            .Setup(repository => repository.HasSchedulesInClassroomAsync(It.IsAny<Guid>()))
            .ReturnsAsync(true);

        var result = await _service.HasSchedulesInClassroomAsync(Guid.NewGuid());

        Assert.True(result);
    }

    [Fact]
    public async Task HasSchedulesInClassroomAsync_WrapsUnexpectedFailures()
    {
        var expectedException = new InvalidOperationException("Lookup failed");
        _mockClassroomRepository
            .Setup(repository => repository.HasSchedulesInClassroomAsync(It.IsAny<Guid>()))
            .ThrowsAsync(expectedException);

        var exception = await Assert.ThrowsAsync<EntityServiceException>(() => _service.HasSchedulesInClassroomAsync(Guid.NewGuid()));

        Assert.Equal("Classroom", exception.EntityName);
        Assert.Contains("HasSchedulesInClassroom:", exception.Operation);
        Assert.Same(expectedException, exception.InnerException);
    }

    [Fact]
    public async Task HasSessionsInClassroomAsync_ReturnsRepositoryResult()
    {
        _mockClassroomRepository
            .Setup(repository => repository.HasSessionsInClassroomAsync(It.IsAny<Guid>()))
            .ReturnsAsync(true);

        var result = await _service.HasSessionsInClassroomAsync(Guid.NewGuid());

        Assert.True(result);
    }

    [Fact]
    public async Task HasSessionsInClassroomAsync_WrapsUnexpectedFailures()
    {
        var expectedException = new InvalidOperationException("Lookup failed");
        _mockClassroomRepository
            .Setup(repository => repository.HasSessionsInClassroomAsync(It.IsAny<Guid>()))
            .ThrowsAsync(expectedException);

        var exception = await Assert.ThrowsAsync<EntityServiceException>(() => _service.HasSessionsInClassroomAsync(Guid.NewGuid()));

        Assert.Equal("Classroom", exception.EntityName);
        Assert.Contains("HasSessionsInClassroom:", exception.Operation);
        Assert.Same(expectedException, exception.InnerException);
    }

    #endregion
}
