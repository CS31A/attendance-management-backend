using System.Security.Claims;
using attendance_monitoring.Classes;
using attendance_monitoring.Controllers;
using attendance_monitoring.Exceptions;
using attendance_monitoring.IServices;
using attendance_monitoring.Models.DTO.Request;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;

namespace attendance.testproject.Controllers_Testing;

public class CourseControllerTest
{
    private readonly Mock<ICourseService> _mockCourseService;
    private readonly Mock<ILogger<CourseController>> _mockLogger;
    private readonly CourseController _controller;

    public CourseControllerTest()
    {
        _mockCourseService = new Mock<ICourseService>();
        _mockLogger = new Mock<ILogger<CourseController>>(MockBehavior.Loose);
        _controller = new CourseController(_mockCourseService.Object, _mockLogger.Object);
    }

    [Fact]
    public async Task GetCourses_ReturnsOkResult_WithCoursesList()
    {
        // Arrange
        var expectedCourses = new List<Course>
        {
            new() { Id = Guid.NewGuid(), Name = "Bachelor of Science in Computer Science", CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow },
            new() { Id = Guid.NewGuid(), Name = "Bachelor of Science in Information Technology", CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow }
        };

        _mockCourseService
            .Setup(s => s.GetAllCoursesAsync())
            .ReturnsAsync(expectedCourses);

        // Act
        var result = await _controller.GetCourses();

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result.Result);
        var courses = Assert.IsAssignableFrom<IList<Course>>(okResult.Value);
        Assert.Equal(2, courses.Count);
        Assert.Equal("Bachelor of Science in Computer Science", courses.First().Name);
        _mockCourseService.Verify(s => s.GetAllCoursesAsync(), Times.Once);
    }

    [Fact]
    public async Task GetCourses_ReturnsServerError_WhenServiceThrowsException()
    {
        // Arrange
        _mockCourseService
            .Setup(s => s.GetAllCoursesAsync())
            .ThrowsAsync(new EntityServiceException("Course", "GetAllCourses", "Database unavailable"));

        // Act
        var result = await _controller.GetCourses();

        // Assert
        var objectResult = Assert.IsType<ObjectResult>(result.Result);
        Assert.Equal(500, objectResult.StatusCode);
        Assert.Equal("An error occurred while retrieving courses", objectResult.Value);
        _mockCourseService.Verify(s => s.GetAllCoursesAsync(), Times.Once);
    }

    [Fact]
    public async Task GetCourse_ReturnsOkResult_WhenCourseExists()
    {
        // Arrange
        var courseId = Guid.NewGuid();
        var expectedCourse = new Course
        {
            Id = courseId,
            Name = "Bachelor of Science in Computer Science",
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        _mockCourseService
            .Setup(s => s.GetCourseByIdAsync(courseId))
            .ReturnsAsync(expectedCourse);

        // Act
        var result = await _controller.GetCourse(courseId);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result.Result);
        var course = Assert.IsType<Course>(okResult.Value);
        Assert.Equal(courseId, course.Id);
        Assert.Equal(expectedCourse.Name, course.Name);
        _mockCourseService.Verify(s => s.GetCourseByIdAsync(courseId), Times.Once);
    }

    [Fact]
    public async Task GetCourseByUuid_ReturnsOkResult_WhenCourseExists()
    {
        var courseUuid = Guid.NewGuid();
        var expectedCourse = new Course
        {
            Id = courseUuid,
            Name = "Bachelor of Science in Data Science",
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        _mockCourseService
            .Setup(s => s.GetCourseByUuidAsync(courseUuid))
            .ReturnsAsync(expectedCourse);

        var result = await _controller.GetCourseByUuid(courseUuid);

        var okResult = Assert.IsType<OkObjectResult>(result.Result);
        var course = Assert.IsType<Course>(okResult.Value);
        Assert.Equal(courseUuid, course.Id);
        _mockCourseService.Verify(s => s.GetCourseByUuidAsync(courseUuid), Times.Once);
    }

    [Fact]
    public async Task GetCourseByUuid_ReturnsNotFound_WhenCourseDoesNotExist()
    {
        var courseUuid = Guid.NewGuid();

        _mockCourseService
            .Setup(s => s.GetCourseByUuidAsync(courseUuid))
            .ThrowsAsync(new EntityNotFoundException<Guid>("Course", courseUuid));

        var result = await _controller.GetCourseByUuid(courseUuid);

        var notFoundResult = Assert.IsType<NotFoundObjectResult>(result.Result);
        Assert.Equal($"Course with UUID {courseUuid} not found", notFoundResult.Value);
        _mockCourseService.Verify(s => s.GetCourseByUuidAsync(courseUuid), Times.Once);
    }

    [Fact]
    public async Task GetCourseByUuid_ReturnsServerError_WhenServiceThrowsException()
    {
        var courseUuid = Guid.NewGuid();

        _mockCourseService
            .Setup(s => s.GetCourseByUuidAsync(courseUuid))
            .ThrowsAsync(new EntityServiceException("Course", "GetCourseByUuid", "Unexpected failure"));

        var result = await _controller.GetCourseByUuid(courseUuid);

        var objectResult = Assert.IsType<ObjectResult>(result.Result);
        Assert.Equal(500, objectResult.StatusCode);
        Assert.Equal("An error occurred while retrieving the course", objectResult.Value);
        _mockCourseService.Verify(s => s.GetCourseByUuidAsync(courseUuid), Times.Once);
    }

    [Fact]
    public async Task GetCourse_ReturnsNotFound_WhenCourseDoesNotExist()
    {
        // Arrange
        var courseId = Guid.NewGuid();
        _mockCourseService
            .Setup(s => s.GetCourseByIdAsync(courseId))
            .ThrowsAsync(new EntityNotFoundException<Guid>("Course", courseId));

        // Act
        var result = await _controller.GetCourse(courseId);

        // Assert
        var notFoundResult = Assert.IsType<NotFoundObjectResult>(result.Result);
        Assert.Equal($"Course with ID {courseId} not found", notFoundResult.Value);
        _mockCourseService.Verify(s => s.GetCourseByIdAsync(courseId), Times.Once);
    }

    [Fact]
    public async Task GetCourse_ReturnsServerError_WhenServiceThrowsException()
    {
        // Arrange
        var courseId = Guid.NewGuid();
        _mockCourseService
            .Setup(s => s.GetCourseByIdAsync(courseId))
            .ThrowsAsync(new EntityServiceException("Course", "GetCourseById", "Unexpected failure"));

        // Act
        var result = await _controller.GetCourse(courseId);

        // Assert
        var objectResult = Assert.IsType<ObjectResult>(result.Result);
        Assert.Equal(500, objectResult.StatusCode);
        Assert.Equal("An error occurred while retrieving the course", objectResult.Value);
        _mockCourseService.Verify(s => s.GetCourseByIdAsync(courseId), Times.Once);
    }

    [Fact]
    public async Task CreateCourse_ReturnsCreatedResult_WhenValidInput()
    {
        // Arrange
        var createCourse = new CreateCourse { Name = "Bachelor of Science in Software Engineering" };
        var user = new ClaimsPrincipal(new ClaimsIdentity());
        SetControllerUser(user);

        var createdCourse = new Course
        {
            Id = Guid.NewGuid(),
            Name = createCourse.Name,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        _mockCourseService
            .Setup(s => s.CreateCourseAsync(createCourse, user))
            .ReturnsAsync(createdCourse);

        // Act
        var result = await _controller.CreateCourse(createCourse);

        // Assert
        var createdAtResult = Assert.IsType<CreatedAtActionResult>(result.Result);
        Assert.Equal(nameof(CourseController.GetCourse), createdAtResult.ActionName);
        Assert.Equal(createdCourse.Id, createdAtResult.RouteValues!["id"]);
        var course = Assert.IsType<Course>(createdAtResult.Value);
        Assert.Equal(createdCourse.Id, course.Id);
        _mockCourseService.Verify(s => s.CreateCourseAsync(createCourse, user), Times.Once);
    }

    [Fact]
    public async Task CreateCourse_ReturnsBadRequest_WhenInvalidModelState()
    {
        // Arrange
        var createCourse = new CreateCourse { Name = string.Empty };
        _controller.ModelState.AddModelError("Name", "Course name is required");

        // Act
        var result = await _controller.CreateCourse(createCourse);

        // Assert
        var badRequestResult = Assert.IsType<BadRequestObjectResult>(result.Result);
        Assert.IsType<SerializableError>(badRequestResult.Value);
        _mockCourseService.Verify(s => s.CreateCourseAsync(It.IsAny<CreateCourse>(), It.IsAny<ClaimsPrincipal>()), Times.Never);
    }

    [Fact]
    public async Task CreateCourse_ReturnsBadRequest_WhenServiceException()
    {
        // Arrange
        var createCourse = new CreateCourse { Name = "Bachelor of Science in Software Engineering" };
        var user = new ClaimsPrincipal(new ClaimsIdentity());
        SetControllerUser(user);
        const string errorMessage = "Course name already exists";

        _mockCourseService
            .Setup(s => s.CreateCourseAsync(createCourse, user))
            .ThrowsAsync(new EntityServiceException("Course", "CreateCourse", errorMessage));

        // Act
        var result = await _controller.CreateCourse(createCourse);

        // Assert
        var badRequestResult = Assert.IsType<BadRequestObjectResult>(result.Result);
        Assert.Equal(errorMessage, badRequestResult.Value);
        _mockCourseService.Verify(s => s.CreateCourseAsync(createCourse, user), Times.Once);
    }

    [Fact]
    public async Task UpdateCourse_ReturnsOkResult_WhenUpdateSucceeds()
    {
        // Arrange
        var courseId = Guid.NewGuid();
        var updateCourse = new UpdateCourse { Name = "Bachelor of Science in Information Systems" };
        var user = new ClaimsPrincipal(new ClaimsIdentity());
        SetControllerUser(user);

        var updatedCourse = new Course
        {
            Id = courseId,
            Name = updateCourse.Name,
            CreatedAt = DateTime.UtcNow.AddDays(-7),
            UpdatedAt = DateTime.UtcNow
        };

        _mockCourseService
            .Setup(s => s.UpdateCourseAsync(courseId, updateCourse, user))
            .ReturnsAsync(updatedCourse);

        // Act
        var result = await _controller.UpdateCourse(courseId, updateCourse);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result.Result);
        var course = Assert.IsType<Course>(okResult.Value);
        Assert.Equal(courseId, course.Id);
        Assert.Equal(updateCourse.Name, course.Name);
        _mockCourseService.Verify(s => s.UpdateCourseAsync(courseId, updateCourse, user), Times.Once);
    }

    [Fact]
    public async Task UpdateCourseByUuid_ReturnsOkResult_WhenUpdateSucceeds()
    {
        var courseUuid = Guid.NewGuid();
        var updateCourse = new UpdateCourse { Name = "Bachelor of Science in Cybersecurity" };
        var user = new ClaimsPrincipal(new ClaimsIdentity());
        SetControllerUser(user);

        var updatedCourse = new Course
        {
            Id = courseUuid,
            Name = updateCourse.Name,
            CreatedAt = DateTime.UtcNow.AddDays(-7),
            UpdatedAt = DateTime.UtcNow
        };

        _mockCourseService
            .Setup(s => s.UpdateCourseByUuidAsync(courseUuid, updateCourse, user))
            .ReturnsAsync(updatedCourse);

        var result = await _controller.UpdateCourseByUuid(courseUuid, updateCourse);

        var okResult = Assert.IsType<OkObjectResult>(result.Result);
        var course = Assert.IsType<Course>(okResult.Value);
        Assert.Equal(courseUuid, course.Id);
        _mockCourseService.Verify(s => s.UpdateCourseByUuidAsync(courseUuid, updateCourse, user), Times.Once);
    }

    [Fact]
    public async Task UpdateCourseByUuid_ReturnsNotFound_WhenCourseDoesNotExist()
    {
        var courseUuid = Guid.NewGuid();
        var updateCourse = new UpdateCourse { Name = "Bachelor of Science in Cybersecurity" };
        var user = new ClaimsPrincipal(new ClaimsIdentity());
        SetControllerUser(user);

        _mockCourseService
            .Setup(s => s.UpdateCourseByUuidAsync(courseUuid, updateCourse, user))
            .ThrowsAsync(new EntityNotFoundException<Guid>("Course", courseUuid));

        var result = await _controller.UpdateCourseByUuid(courseUuid, updateCourse);

        var notFoundResult = Assert.IsType<NotFoundObjectResult>(result.Result);
        Assert.Equal($"Course with UUID {courseUuid} not found", notFoundResult.Value);
        _mockCourseService.Verify(s => s.UpdateCourseByUuidAsync(courseUuid, updateCourse, user), Times.Once);
    }

    [Fact]
    public async Task UpdateCourseByUuid_ReturnsBadRequest_WhenServiceException()
    {
        var courseUuid = Guid.NewGuid();
        var updateCourse = new UpdateCourse { Name = "Bachelor of Science in Cybersecurity" };
        var user = new ClaimsPrincipal(new ClaimsIdentity());
        SetControllerUser(user);
        const string errorMessage = "Unable to update course";

        _mockCourseService
            .Setup(s => s.UpdateCourseByUuidAsync(courseUuid, updateCourse, user))
            .ThrowsAsync(new EntityServiceException("Course", "UpdateCourseByUuid", errorMessage));

        var result = await _controller.UpdateCourseByUuid(courseUuid, updateCourse);

        var badRequestResult = Assert.IsType<BadRequestObjectResult>(result.Result);
        Assert.Equal(errorMessage, badRequestResult.Value);
        _mockCourseService.Verify(s => s.UpdateCourseByUuidAsync(courseUuid, updateCourse, user), Times.Once);
    }

    [Fact]
    public async Task UpdateCourse_ReturnsNotFound_WhenCourseDoesNotExist()
    {
        // Arrange
        var courseId = Guid.NewGuid();
        var updateCourse = new UpdateCourse { Name = "Bachelor of Science in Information Systems" };
        var user = new ClaimsPrincipal(new ClaimsIdentity());
        SetControllerUser(user);

        _mockCourseService
            .Setup(s => s.UpdateCourseAsync(courseId, updateCourse, user))
            .ThrowsAsync(new EntityNotFoundException<Guid>("Course", courseId));

        // Act
        var result = await _controller.UpdateCourse(courseId, updateCourse);

        // Assert
        var notFoundResult = Assert.IsType<NotFoundObjectResult>(result.Result);
        Assert.Equal($"Course with ID {courseId} not found", notFoundResult.Value);
        _mockCourseService.Verify(s => s.UpdateCourseAsync(courseId, updateCourse, user), Times.Once);
    }

    [Fact]
    public async Task UpdateCourse_ReturnsBadRequest_WhenServiceException()
    {
        // Arrange
        var courseId = Guid.NewGuid();
        var updateCourse = new UpdateCourse { Name = "Bachelor of Science in Information Systems" };
        var user = new ClaimsPrincipal(new ClaimsIdentity());
        SetControllerUser(user);
        const string errorMessage = "Unable to update course";

        _mockCourseService
            .Setup(s => s.UpdateCourseAsync(courseId, updateCourse, user))
            .ThrowsAsync(new EntityServiceException("Course", "UpdateCourse", errorMessage));

        // Act
        var result = await _controller.UpdateCourse(courseId, updateCourse);

        // Assert
        var badRequestResult = Assert.IsType<BadRequestObjectResult>(result.Result);
        Assert.Equal(errorMessage, badRequestResult.Value);
        _mockCourseService.Verify(s => s.UpdateCourseAsync(courseId, updateCourse, user), Times.Once);
    }

    [Fact]
    public async Task DeleteCourse_ReturnsNoContent_WhenDeletionSucceeds()
    {
        // Arrange
        var courseId = Guid.NewGuid();
        var user = new ClaimsPrincipal(new ClaimsIdentity());
        SetControllerUser(user);

        _mockCourseService
            .Setup(s => s.DeleteCourseAsync(courseId, user))
            .Returns(Task.CompletedTask);

        // Act
        var result = await _controller.DeleteCourse(courseId);

        // Assert
        Assert.IsType<NoContentResult>(result);
        _mockCourseService.Verify(s => s.DeleteCourseAsync(courseId, user), Times.Once);
    }

    [Fact]
    public async Task DeleteCourseByUuid_ReturnsNoContent_WhenDeletionSucceeds()
    {
        var courseUuid = Guid.NewGuid();
        var user = new ClaimsPrincipal(new ClaimsIdentity());
        SetControllerUser(user);

        _mockCourseService
            .Setup(s => s.DeleteCourseByUuidAsync(courseUuid, user))
            .Returns(Task.CompletedTask);

        var result = await _controller.DeleteCourseByUuid(courseUuid);

        Assert.IsType<NoContentResult>(result);
        _mockCourseService.Verify(s => s.DeleteCourseByUuidAsync(courseUuid, user), Times.Once);
    }

    [Fact]
    public async Task DeleteCourseByUuid_ReturnsNotFound_WhenCourseDoesNotExist()
    {
        var courseUuid = Guid.NewGuid();
        var user = new ClaimsPrincipal(new ClaimsIdentity());
        SetControllerUser(user);

        _mockCourseService
            .Setup(s => s.DeleteCourseByUuidAsync(courseUuid, user))
            .ThrowsAsync(new EntityNotFoundException<Guid>("Course", courseUuid));

        var result = await _controller.DeleteCourseByUuid(courseUuid);

        var notFoundResult = Assert.IsType<NotFoundObjectResult>(result);
        Assert.Equal($"Course with UUID {courseUuid} not found", notFoundResult.Value);
        _mockCourseService.Verify(s => s.DeleteCourseByUuidAsync(courseUuid, user), Times.Once);
    }

    [Fact]
    public async Task DeleteCourseByUuid_ReturnsBadRequest_WhenServiceException()
    {
        var courseUuid = Guid.NewGuid();
        var user = new ClaimsPrincipal(new ClaimsIdentity());
        SetControllerUser(user);
        const string errorMessage = "Unable to delete course";

        _mockCourseService
            .Setup(s => s.DeleteCourseByUuidAsync(courseUuid, user))
            .ThrowsAsync(new EntityServiceException("Course", "DeleteCourseByUuid", errorMessage));

        var result = await _controller.DeleteCourseByUuid(courseUuid);

        var badRequestResult = Assert.IsType<BadRequestObjectResult>(result);
        Assert.Equal(errorMessage, badRequestResult.Value);
        _mockCourseService.Verify(s => s.DeleteCourseByUuidAsync(courseUuid, user), Times.Once);
    }

    [Fact]
    public async Task DeleteCourse_ReturnsNotFound_WhenCourseDoesNotExist()
    {
        // Arrange
        var courseId = Guid.NewGuid();
        var user = new ClaimsPrincipal(new ClaimsIdentity());
        SetControllerUser(user);

        _mockCourseService
            .Setup(s => s.DeleteCourseAsync(courseId, user))
            .ThrowsAsync(new EntityNotFoundException<Guid>("Course", courseId));

        // Act
        var result = await _controller.DeleteCourse(courseId);

        // Assert
        var notFoundResult = Assert.IsType<NotFoundObjectResult>(result);
        Assert.Equal($"Course with ID {courseId} not found", notFoundResult.Value);
        _mockCourseService.Verify(s => s.DeleteCourseAsync(courseId, user), Times.Once);
    }

    [Fact]
    public async Task DeleteCourse_ReturnsBadRequest_WhenServiceException()
    {
        // Arrange
        var courseId = Guid.NewGuid();
        var user = new ClaimsPrincipal(new ClaimsIdentity());
        SetControllerUser(user);
        const string errorMessage = "Unable to delete course";

        _mockCourseService
            .Setup(s => s.DeleteCourseAsync(courseId, user))
            .ThrowsAsync(new EntityServiceException("Course", "DeleteCourse", errorMessage));

        // Act
        var result = await _controller.DeleteCourse(courseId);

        // Assert
        var badRequestResult = Assert.IsType<BadRequestObjectResult>(result);
        Assert.Equal(errorMessage, badRequestResult.Value);
        _mockCourseService.Verify(s => s.DeleteCourseAsync(courseId, user), Times.Once);
    }

    private void SetControllerUser(ClaimsPrincipal user)
    {
        _controller.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext
            {
                User = user
            }
        };
    }
}
