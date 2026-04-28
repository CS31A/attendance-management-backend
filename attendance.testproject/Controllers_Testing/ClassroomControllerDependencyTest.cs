using attendance_monitoring.Controllers;
using attendance_monitoring.Exceptions;
using attendance_monitoring.IServices;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;

namespace attendance.testproject.Controllers_Testing;

public class ClassroomControllerDependencyTest
{
    private readonly Mock<IClassroomService> _mockClassroomService;
    private readonly ClassroomController _controller;

    public ClassroomControllerDependencyTest()
    {
        _mockClassroomService = new Mock<IClassroomService>();
        var mockLogger = new Mock<ILogger<ClassroomController>>();
        _controller = new ClassroomController(_mockClassroomService.Object, mockLogger.Object);
    }

    [Fact]
    public async Task HasSchedulesInClassroom_ReturnsOk_WithBooleanResult()
    {
        var classroomId = Guid.NewGuid();
        _mockClassroomService
            .Setup(service => service.HasSchedulesInClassroomAsync(classroomId))
            .ReturnsAsync(true);

        var result = await _controller.HasSchedulesInClassroom(classroomId);

        var okResult = Assert.IsType<OkObjectResult>(result.Result);
        Assert.True(Assert.IsType<bool>(okResult.Value));
    }

    [Fact]
    public async Task HasSchedulesInClassroom_ReturnsBadRequest_ForInvalidId()
    {
        var result = await _controller.HasSchedulesInClassroom(Guid.Empty);

        var badRequestResult = Assert.IsType<BadRequestObjectResult>(result.Result);
        Assert.Equal("Classroom ID must be greater than 0.", badRequestResult.Value);
        _mockClassroomService.Verify(service => service.HasSchedulesInClassroomAsync(It.IsAny<Guid>()), Times.Never);
    }

    [Fact]
    public async Task HasSchedulesInClassroom_ReturnsServerError_WhenServiceThrowsException()
    {
        var classroomId = Guid.NewGuid();
        _mockClassroomService
            .Setup(service => service.HasSchedulesInClassroomAsync(classroomId))
            .ThrowsAsync(new EntityServiceException("Classroom", $"HasSchedulesInClassroom: {classroomId}", "Error checking classroom dependencies"));

        var result = await _controller.HasSchedulesInClassroom(classroomId);

        var objectResult = Assert.IsType<ObjectResult>(result.Result);
        Assert.Equal(500, objectResult.StatusCode);
        Assert.Equal("An error occurred while checking classroom dependencies", objectResult.Value);
    }

    [Fact]
    public async Task HasSessionsInClassroom_ReturnsOk_WithBooleanResult()
    {
        var classroomId = Guid.NewGuid();
        _mockClassroomService
            .Setup(service => service.HasSessionsInClassroomAsync(classroomId))
            .ReturnsAsync(true);

        var result = await _controller.HasSessionsInClassroom(classroomId);

        var okResult = Assert.IsType<OkObjectResult>(result.Result);
        Assert.True(Assert.IsType<bool>(okResult.Value));
    }

    [Fact]
    public async Task HasSessionsInClassroom_ReturnsBadRequest_ForInvalidId()
    {
        var result = await _controller.HasSessionsInClassroom(Guid.Empty);

        var badRequestResult = Assert.IsType<BadRequestObjectResult>(result.Result);
        Assert.Equal("Classroom ID must be greater than 0.", badRequestResult.Value);
        _mockClassroomService.Verify(service => service.HasSessionsInClassroomAsync(It.IsAny<Guid>()), Times.Never);
    }

    [Fact]
    public async Task HasSessionsInClassroom_ReturnsServerError_WhenServiceThrowsException()
    {
        var classroomId = Guid.NewGuid();
        _mockClassroomService
            .Setup(service => service.HasSessionsInClassroomAsync(classroomId))
            .ThrowsAsync(new EntityServiceException("Classroom", $"HasSessionsInClassroom: {classroomId}", "Error checking classroom dependencies"));

        var result = await _controller.HasSessionsInClassroom(classroomId);

        var objectResult = Assert.IsType<ObjectResult>(result.Result);
        Assert.Equal(500, objectResult.StatusCode);
        Assert.Equal("An error occurred while checking classroom dependencies", objectResult.Value);
    }
}
