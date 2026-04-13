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
        const int classroomId = 5;
        _mockClassroomService
            .Setup(service => service.HasSchedulesInClassroomAsync(classroomId))
            .ReturnsAsync(true);

        var result = await _controller.HasSchedulesInClassroom(classroomId);

        var okResult = Assert.IsType<OkObjectResult>(result.Result);
        Assert.True(Assert.IsType<bool>(okResult.Value));
    }

    [Fact]
    public async Task HasSessionsInClassroom_ReturnsOk_WithBooleanResult()
    {
        const int classroomId = 8;
        _mockClassroomService
            .Setup(service => service.HasSessionsInClassroomAsync(classroomId))
            .ReturnsAsync(true);

        var result = await _controller.HasSessionsInClassroom(classroomId);

        var okResult = Assert.IsType<OkObjectResult>(result.Result);
        Assert.True(Assert.IsType<bool>(okResult.Value));
    }
}
