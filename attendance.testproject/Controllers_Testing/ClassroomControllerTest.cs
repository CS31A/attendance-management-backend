using attendance_monitoring.Classes;
using attendance_monitoring.Controllers;
using attendance_monitoring.Exceptions;
using attendance_monitoring.IServices;
using attendance_monitoring.Models.DTO.Request;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace attendance.testproject.Controllers_Testing;

public class ClassroomControllerTest
{
    private readonly Mock<IClassroomService> _service;
    private readonly Mock<ILogger<ClassroomController>> _logger;
    private readonly ClassroomController _controller;

    public ClassroomControllerTest()
    {
        _service = new Mock<IClassroomService>();
        _logger = new Mock<ILogger<ClassroomController>>();
        _controller = new ClassroomController(_service.Object, _logger.Object);
    }

    [Fact]
    public async Task GetClassrooms_ReturnsOkWithClassrooms()
    {
        var classrooms = new List<Classroom>
        {
            new() { Id = 1, Name = "Room 101", CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow },
            new() { Id = 2, Name = "Room 102", CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow }
        };
        _service.Setup(s => s.GetAllClassroomsAsync()).ReturnsAsync(classrooms);

        var result = await _controller.GetClassrooms();

        var okResult = Assert.IsType<OkObjectResult>(result.Result);
        var returnValue = Assert.IsAssignableFrom<IEnumerable<Classroom>>(okResult.Value);
        Assert.Equal(2, returnValue.Count());
    }

    [Fact]
    public async Task GetClassrooms_ReturnsServerErrorOnException()
    {
        _service.Setup(s => s.GetAllClassroomsAsync())
            .ThrowsAsync(new EntityServiceException("Classroom", "GetAll", "Database error"));

        var result = await _controller.GetClassrooms();

        var objResult = Assert.IsType<ObjectResult>(result.Result);
        Assert.Equal(500, objResult.StatusCode);
    }

    [Fact]
    public async Task GetClassroom_ReturnsOkWithClassroom()
    {
        var classroom = new Classroom
        {
            Id = 1,
            Name = "Room 101",
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
        _service.Setup(s => s.GetClassroomByIdAsync(1)).ReturnsAsync(classroom);

        var result = await _controller.GetClassroom(1);

        var okResult = Assert.IsType<OkObjectResult>(result.Result);
        var returnValue = Assert.IsType<Classroom>(okResult.Value);
        Assert.Equal(1, returnValue.Id);
        Assert.Equal("Room 101", returnValue.Name);
    }

    [Fact]
    public async Task GetClassroom_ReturnsNotFoundWhenDoesNotExist()
    {
        _service.Setup(s => s.GetClassroomByIdAsync(999))
            .ThrowsAsync(new EntityNotFoundException<int>("Classroom", 999));

        var result = await _controller.GetClassroom(999);

        Assert.IsType<NotFoundObjectResult>(result.Result);
    }

    [Fact]
    public async Task CreateClassroom_ReturnsCreatedWithClassroom()
    {
        var createDto = new CreateClassroom { Name = "New Room" };
        var classroom = new Classroom
        {
            Id = 1,
            Name = "New Room",
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
        _service.Setup(s => s.CreateClassroomAsync(createDto))
            .ReturnsAsync((classroom, null as string));

        var result = await _controller.CreateClassroom(createDto);

        var createdResult = Assert.IsType<CreatedAtActionResult>(result.Result);
        var returnValue = Assert.IsType<Classroom>(createdResult.Value);
        Assert.Equal("New Room", returnValue.Name);
        Assert.Equal(1, returnValue.Id);
    }

    [Fact]
    public async Task CreateClassroom_ReturnsBadRequestOnError()
    {
        var createDto = new CreateClassroom { Name = "Existing Room" };
        _service.Setup(s => s.CreateClassroomAsync(createDto))
            .ReturnsAsync((null as Classroom, "A classroom with this name already exists"));

        var result = await _controller.CreateClassroom(createDto);

        var badResult = Assert.IsType<BadRequestObjectResult>(result.Result);
        Assert.Equal("A classroom with this name already exists", badResult.Value);
    }

    [Fact]
    public async Task CreateClassroom_ReturnsBadRequestWhenClassroomIsNull()
    {
        var createDto = new CreateClassroom { Name = "Test Room" };
        _service.Setup(s => s.CreateClassroomAsync(createDto))
            .ReturnsAsync((null as Classroom, null as string));

        var result = await _controller.CreateClassroom(createDto);

        var badResult = Assert.IsType<BadRequestObjectResult>(result.Result);
        Assert.Equal("An unexpected error occurred while creating the classroom.", badResult.Value);
    }

    [Fact]
    public async Task UpdateClassroom_ReturnsOkWithUpdatedClassroom()
    {
        var updateDto = new UpdateClassroom { Name = "Updated Room" };
        var classroom = new Classroom
        {
            Id = 1,
            Name = "Updated Room",
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
        _service.Setup(s => s.UpdateClassroomAsync(1, updateDto))
            .ReturnsAsync((classroom, null as string));

        var result = await _controller.UpdateClassroom(1, updateDto);

        var okResult = Assert.IsType<OkObjectResult>(result.Result);
        var returnValue = Assert.IsType<Classroom>(okResult.Value);
        Assert.Equal("Updated Room", returnValue.Name);
    }

    [Fact]
    public async Task UpdateClassroom_ReturnsNotFoundWhenDoesNotExist()
    {
        var updateDto = new UpdateClassroom { Name = "Updated" };
        _service.Setup(s => s.UpdateClassroomAsync(999, updateDto))
            .ThrowsAsync(new EntityNotFoundException<int>("Classroom", 999));

        var result = await _controller.UpdateClassroom(999, updateDto);

        Assert.IsType<NotFoundObjectResult>(result.Result);
    }

    [Fact]
    public async Task UpdateClassroom_ReturnsBadRequestOnError()
    {
        var updateDto = new UpdateClassroom { Name = "Duplicate" };
        _service.Setup(s => s.UpdateClassroomAsync(1, updateDto))
            .ReturnsAsync((null as Classroom, "A classroom with this name already exists"));

        var result = await _controller.UpdateClassroom(1, updateDto);

        var badResult = Assert.IsType<BadRequestObjectResult>(result.Result);
        Assert.Equal("A classroom with this name already exists", badResult.Value);
    }

    [Fact]
    public async Task DeleteClassroom_ReturnsNoContentOnSuccess()
    {
        _service.Setup(s => s.DeleteClassroomAsync(1))
            .ReturnsAsync(null as string);

        var result = await _controller.DeleteClassroom(1);

        Assert.IsType<NoContentResult>(result);
    }

    [Fact]
    public async Task DeleteClassroom_ReturnsNotFoundWhenDoesNotExist()
    {
        _service.Setup(s => s.DeleteClassroomAsync(999))
            .ThrowsAsync(new EntityNotFoundException<int>("Classroom", 999));

        var result = await _controller.DeleteClassroom(999);

        Assert.IsType<NotFoundObjectResult>(result);
    }

    [Fact]
    public async Task DeleteClassroom_ReturnsBadRequestOnError()
    {
        _service.Setup(s => s.DeleteClassroomAsync(1))
            .ReturnsAsync("Cannot delete classroom with active sections");

        var result = await _controller.DeleteClassroom(1);

        var badResult = Assert.IsType<BadRequestObjectResult>(result);
        Assert.Equal("Cannot delete classroom with active sections", badResult.Value);
    }
}