using attendance_monitoring.Controllers;
using attendance_monitoring.Exceptions;
using attendance_monitoring.IServices;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;

namespace attendance.testproject.Controllers_Testing;

public class CourseControllerDependencyTest
{
    private readonly Mock<ICourseService> _mockCourseService;
    private readonly CourseController _controller;

    public CourseControllerDependencyTest()
    {
        _mockCourseService = new Mock<ICourseService>();
        var mockLogger = new Mock<ILogger<CourseController>>();
        _controller = new CourseController(_mockCourseService.Object, mockLogger.Object);
    }

    [Fact]
    public async Task HasSectionsInCourse_ReturnsOk_WithBooleanResult()
    {
        var courseId = Guid.NewGuid();
        _mockCourseService
            .Setup(service => service.HasSectionsInCourseAsync(courseId))
            .ReturnsAsync(true);

        var result = await _controller.HasSectionsInCourse(courseId);

        var okResult = Assert.IsType<OkObjectResult>(result.Result);
        Assert.True(Assert.IsType<bool>(okResult.Value));
    }

    [Fact]
    public async Task HasSectionsInCourse_ReturnsBadRequest_ForInvalidId()
    {
        var result = await _controller.HasSectionsInCourse(Guid.Empty);

        var badRequestResult = Assert.IsType<BadRequestObjectResult>(result.Result);
        Assert.Equal("Course ID must be greater than 0.", badRequestResult.Value);
        _mockCourseService.Verify(service => service.HasSectionsInCourseAsync(It.IsAny<Guid>()), Times.Never);
    }

    [Fact]
    public async Task HasSectionsInCourse_ReturnsServerError_WhenServiceThrowsEntityServiceException()
    {
        var courseId = Guid.NewGuid();
        _mockCourseService
            .Setup(service => service.HasSectionsInCourseAsync(courseId))
            .ThrowsAsync(new EntityServiceException("Course", $"HasSectionsInCourse: {courseId}", "Error checking course dependencies"));

        var result = await _controller.HasSectionsInCourse(courseId);

        var objectResult = Assert.IsType<ObjectResult>(result.Result);
        Assert.Equal(500, objectResult.StatusCode);
        Assert.Equal("An error occurred while checking course dependencies", objectResult.Value);
    }
}
