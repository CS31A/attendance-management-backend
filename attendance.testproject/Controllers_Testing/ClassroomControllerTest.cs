using attendance_monitoring.Classes;
using attendance_monitoring.Controllers;
using attendance_monitoring.Exceptions;
using attendance_monitoring.IServices;
using attendance_monitoring.Models.DTO.Request;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;

namespace attendance.testproject.Controllers_Testing;

public class ClassroomControllerTest
{
    private readonly Mock<IClassroomService> _mockClassroomService;
    private readonly Mock<ILogger<ClassroomController>> _mockLogger;
    private readonly ClassroomController _controller;

    public ClassroomControllerTest()
    {
        _mockClassroomService = new Mock<IClassroomService>();
        _mockLogger = new Mock<ILogger<ClassroomController>>();
        _controller = new ClassroomController(_mockClassroomService.Object, _mockLogger.Object);
    }

    [Fact]
    public async Task GetClassrooms_ReturnsOkResult_WithClassroomsList()
    {
        // Arrange
        var expectedClassrooms = new List<Classroom>
        {
            new() { Id = 1, Name = "Room 101", CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow },
            new() { Id = 2, Name = "Lab 202", CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow }
        };

        _mockClassroomService
            .Setup(s => s.GetAllClassroomsAsync())
            .ReturnsAsync(expectedClassrooms);

        // Act
        var result = await _controller.GetClassrooms();

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result.Result);
        var classrooms = Assert.IsAssignableFrom<IEnumerable<Classroom>>(okResult.Value);
        Assert.Equal(2, classrooms.Count());
        Assert.Equal("Room 101", classrooms.First().Name);
        Assert.Equal("Lab 202", classrooms.Last().Name);
        _mockClassroomService.Verify(s => s.GetAllClassroomsAsync(), Times.Once);
    }

    [Fact]
    public async Task GetClassroom_ReturnsOkResult_WhenClassroomExists()
    {
        // Arrange
        const int classroomId = 1;
        var expectedClassroom = new Classroom
        {
            Id = classroomId,
            Name = "Room 101",
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        _mockClassroomService
            .Setup(s => s.GetClassroomByIdAsync(classroomId))
            .ReturnsAsync(expectedClassroom);

        // Act
        var result = await _controller.GetClassroom(classroomId);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result.Result);
        var classroom = Assert.IsType<Classroom>(okResult.Value);
        Assert.Equal(classroomId, classroom.Id);
        Assert.Equal("Room 101", classroom.Name);
        _mockClassroomService.Verify(s => s.GetClassroomByIdAsync(classroomId), Times.Once);
    }

    [Fact]
    public async Task GetClassroomByUuid_ReturnsOkResult_WhenClassroomExists()
    {
        var classroomUuid = Guid.NewGuid();
        var expectedClassroom = new Classroom
        {
            Id = 4,
            Uuid = classroomUuid,
            Name = "Room 204",
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        _mockClassroomService
            .Setup(s => s.GetClassroomByUuidAsync(classroomUuid))
            .ReturnsAsync(expectedClassroom);

        var result = await _controller.GetClassroomByUuid(classroomUuid);

        var okResult = Assert.IsType<OkObjectResult>(result.Result);
        var classroom = Assert.IsType<Classroom>(okResult.Value);
        Assert.Equal(classroomUuid, classroom.Uuid);
    }

    [Fact]
    public async Task GetClassroomByUuid_ReturnsNotFound_WhenClassroomDoesNotExist()
    {
        var classroomUuid = Guid.NewGuid();

        _mockClassroomService
            .Setup(s => s.GetClassroomByUuidAsync(classroomUuid))
            .ThrowsAsync(new EntityNotFoundException<Guid>("Classroom", classroomUuid));

        var result = await _controller.GetClassroomByUuid(classroomUuid);

        var notFoundResult = Assert.IsType<NotFoundObjectResult>(result.Result);
        Assert.NotNull(notFoundResult.Value);
        _mockClassroomService.Verify(s => s.GetClassroomByUuidAsync(classroomUuid), Times.Once);
    }

    [Fact]
    public async Task GetClassroom_ReturnsNotFound_WhenClassroomDoesNotExist()
    {
        // Arrange
        const int classroomId = 99;

        _mockClassroomService
            .Setup(s => s.GetClassroomByIdAsync(classroomId))
            .ThrowsAsync(new EntityNotFoundException<int>("Classroom", classroomId));

        // Act
        var result = await _controller.GetClassroom(classroomId);

        // Assert
        var notFoundResult = Assert.IsType<NotFoundObjectResult>(result.Result);
        Assert.NotNull(notFoundResult.Value);
        _mockClassroomService.Verify(s => s.GetClassroomByIdAsync(classroomId), Times.Once);
    }

    [Fact]
    public async Task CreateClassroom_ReturnsCreatedResult_WhenValidInput()
    {
        // Arrange
        var createClassroom = new CreateClassroom
        {
            Name = "Room 301"
        };

        var createdClassroom = new Classroom
        {
            Id = 3,
            Name = "Room 301",
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        _mockClassroomService
            .Setup(s => s.CreateClassroomAsync(createClassroom))
            .ReturnsAsync(createdClassroom);

        // Act
        var result = await _controller.CreateClassroom(createClassroom);

        // Assert
        var createdAtActionResult = Assert.IsType<CreatedAtActionResult>(result.Result);
        var classroom = Assert.IsType<Classroom>(createdAtActionResult.Value);
        Assert.Equal(nameof(ClassroomController.GetClassroom), createdAtActionResult.ActionName);
        Assert.Equal(createdClassroom.Id, createdAtActionResult.RouteValues!["id"]);
        Assert.Equal(createdClassroom.Id, classroom.Id);
        Assert.Equal("Room 301", classroom.Name);
        _mockClassroomService.Verify(s => s.CreateClassroomAsync(createClassroom), Times.Once);
    }

    [Fact]
    public async Task CreateClassroom_ReturnsBadRequest_WhenInvalidModelState()
    {
        // Arrange
        var createClassroom = new CreateClassroom
        {
            Name = string.Empty
        };
        _controller.ModelState.AddModelError("Name", "Classroom name is required");

        // Act
        var result = await _controller.CreateClassroom(createClassroom);

        // Assert
        var badRequestResult = Assert.IsType<BadRequestObjectResult>(result.Result);
        Assert.IsType<SerializableError>(badRequestResult.Value);
        _mockClassroomService.Verify(s => s.CreateClassroomAsync(It.IsAny<CreateClassroom>()), Times.Never);
    }

    [Fact]
    public async Task UpdateClassroom_ReturnsOkResult_WhenUpdateSucceeds()
    {
        // Arrange
        const int classroomId = 1;
        var updateClassroom = new UpdateClassroom
        {
            Name = "Updated Room 101"
        };

        var updatedClassroom = new Classroom
        {
            Id = classroomId,
            Name = "Updated Room 101",
            CreatedAt = DateTime.UtcNow.AddDays(-1),
            UpdatedAt = DateTime.UtcNow
        };

        _mockClassroomService
            .Setup(s => s.UpdateClassroomAsync(classroomId, updateClassroom))
            .ReturnsAsync(updatedClassroom);

        // Act
        var result = await _controller.UpdateClassroom(classroomId, updateClassroom);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result.Result);
        var classroom = Assert.IsType<Classroom>(okResult.Value);
        Assert.Equal(classroomId, classroom.Id);
        Assert.Equal("Updated Room 101", classroom.Name);
        _mockClassroomService.Verify(s => s.UpdateClassroomAsync(classroomId, updateClassroom), Times.Once);
    }

    [Fact]
    public async Task UpdateClassroomByUuid_ReturnsOkResult_WhenUpdateSucceeds()
    {
        var classroomUuid = Guid.NewGuid();
        var updateClassroom = new UpdateClassroom
        {
            Name = "Updated Room 204"
        };

        var updatedClassroom = new Classroom
        {
            Id = 4,
            Uuid = classroomUuid,
            Name = updateClassroom.Name!,
            CreatedAt = DateTime.UtcNow.AddDays(-1),
            UpdatedAt = DateTime.UtcNow
        };

        _mockClassroomService
            .Setup(s => s.UpdateClassroomByUuidAsync(classroomUuid, updateClassroom))
            .ReturnsAsync(updatedClassroom);

        var result = await _controller.UpdateClassroomByUuid(classroomUuid, updateClassroom);

        var okResult = Assert.IsType<OkObjectResult>(result.Result);
        var classroom = Assert.IsType<Classroom>(okResult.Value);
        Assert.Equal(classroomUuid, classroom.Uuid);
    }

    [Fact]
    public async Task UpdateClassroom_ReturnsBadRequest_WhenInvalidModelState()
    {
        // Arrange
        const int classroomId = 1;
        var updateClassroom = new UpdateClassroom
        {
            Name = "A"
        };
        _controller.ModelState.AddModelError("Name", "Classroom name must be between 2 and 100 characters");

        // Act
        var result = await _controller.UpdateClassroom(classroomId, updateClassroom);

        // Assert
        var badRequestResult = Assert.IsType<BadRequestObjectResult>(result.Result);
        Assert.IsType<SerializableError>(badRequestResult.Value);
        _mockClassroomService.Verify(s => s.UpdateClassroomAsync(It.IsAny<int>(), It.IsAny<UpdateClassroom>()), Times.Never);
    }

    [Fact]
    public async Task DeleteClassroom_ReturnsNoContent_WhenDeletionSucceeds()
    {
        // Arrange
        const int classroomId = 1;

        _mockClassroomService
            .Setup(s => s.DeleteClassroomAsync(classroomId))
            .Returns(Task.CompletedTask);

        // Act
        var result = await _controller.DeleteClassroom(classroomId);

        // Assert
        Assert.IsType<NoContentResult>(result);
        _mockClassroomService.Verify(s => s.DeleteClassroomAsync(classroomId), Times.Once);
    }

    [Fact]
    public async Task DeleteClassroomByUuid_ReturnsNoContent_WhenDeletionSucceeds()
    {
        var classroomUuid = Guid.NewGuid();

        _mockClassroomService
            .Setup(s => s.DeleteClassroomByUuidAsync(classroomUuid))
            .Returns(Task.CompletedTask);

        var result = await _controller.DeleteClassroomByUuid(classroomUuid);

        Assert.IsType<NoContentResult>(result);
        _mockClassroomService.Verify(s => s.DeleteClassroomByUuidAsync(classroomUuid), Times.Once);
    }

    [Fact]
    public async Task DeleteClassroom_PropagatesEntityNotFoundException_WhenClassroomDoesNotExist()
    {
        // Arrange
        const int classroomId = 99;

        _mockClassroomService
            .Setup(s => s.DeleteClassroomAsync(classroomId))
            .ThrowsAsync(new EntityNotFoundException<int>("Classroom", classroomId));

        // Act
        var exception = await Assert.ThrowsAsync<EntityNotFoundException<int>>(() => _controller.DeleteClassroom(classroomId));

        // Assert
        Assert.Equal($"Classroom with ID {classroomId} was not found.", exception.Message);
        _mockClassroomService.Verify(s => s.DeleteClassroomAsync(classroomId), Times.Once);
    }
}
