using Microsoft.AspNetCore.Mvc;
using attendance_monitoring.Controllers;
using attendance_monitoring.IServices;
using attendance_monitoring.Classes;
using attendance_monitoring.Models.DTO.Request;
using attendance_monitoring.Models.DTO.Response;
using Microsoft.Extensions.Logging;
using System.Security.Claims;

namespace attendance.testproject.Controllers_Testing;

/// <summary>
/// Unit tests for ScheduleController
/// </summary>
public class ScheduleControllerTest
{
    private readonly Mock<IScheduleService> _mockScheduleService;
    private readonly Mock<ILogger<ScheduleController>> _mockLogger;
    private readonly ScheduleController _scheduleController;
    private readonly ClaimsPrincipal _testUser;

    public ScheduleControllerTest()
    {
        _mockScheduleService = new Mock<IScheduleService>();
        _mockLogger = new Mock<ILogger<ScheduleController>>();
        _scheduleController = new ScheduleController(_mockScheduleService.Object, _mockLogger.Object);

        // Setup test user with claims
        _testUser = new ClaimsPrincipal(new ClaimsIdentity(new[]
        {
            new Claim(ClaimTypes.NameIdentifier, "1"),
            new Claim(ClaimTypes.Role, "Admin")
        }, "TestAuthentication"));
    }

    #region GetSchedule Tests

    [Fact]
    public async Task GetSchedule_ReturnsOkResult_WithSchedule()
    {
        // Arrange
        int scheduleId = 1;
        var expectedSchedule = new ScheduleResponseDto
        {
            Id = scheduleId,
            TimeIn = new TimeOnly(8, 0),
            TimeOut = new TimeOnly(9, 0),
            DayOfWeek = "Monday"
        };

        _mockScheduleService
            .Setup(s => s.GetScheduleByIdAsync(scheduleId))
            .ReturnsAsync(expectedSchedule);

        // Act
        var result = await _scheduleController.GetSchedule(scheduleId);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result.Result);
        var schedule = Assert.IsType<ScheduleResponseDto>(okResult.Value);
        Assert.Equal(scheduleId, schedule.Id);

        _mockScheduleService.Verify(s => s.GetScheduleByIdAsync(scheduleId), Times.Once);
    }

    #endregion

    #region PostSchedule Tests

    [Fact]
    public async Task PostSchedule_ReturnsCreatedAtAction_WithValidSchedule()
    {
        // Arrange
        var createSchedule = new CreateSchedule
        {
            TimeIn = new TimeOnly(8, 0),
            TimeOut = new TimeOnly(9, 0),
            DayOfWeek = "Monday",
            SubjectId = 1,
            ClassroomId = 1,
            SectionId = 1,
            InstructorId = 1
        };

        var createdSchedule = new Schedules
        {
            Id = 1,
            TimeIn = createSchedule.TimeIn,
            TimeOut = createSchedule.TimeOut,
            DayOfWeek = createSchedule.DayOfWeek
        };

        _mockScheduleService
            .Setup(s => s.CreateScheduleAsync(createSchedule))
            .ReturnsAsync((createdSchedule, (string?)null));

        // Act
        var result = await _scheduleController.PostSchedule(createSchedule);

        // Assert
        var createdAtActionResult = Assert.IsType<CreatedAtActionResult>(result.Result);
        Assert.Equal(nameof(_scheduleController.GetSchedule), createdAtActionResult.ActionName);
        var schedule = Assert.IsType<Schedules>(createdAtActionResult.Value);
        Assert.Equal(1, schedule.Id);

        _mockScheduleService.Verify(s => s.CreateScheduleAsync(createSchedule), Times.Once);
    }

    [Fact]
    public async Task PostSchedule_ReturnsBadRequest_WhenValidationFails()
    {
        // Arrange
        var createSchedule = new CreateSchedule
        {
            TimeIn = new TimeOnly(8, 0),
            TimeOut = new TimeOnly(9, 0),
            DayOfWeek = "Monday",
            SubjectId = 1,
            ClassroomId = 1,
            SectionId = 1,
            InstructorId = 1
        };

        _scheduleController.ModelState.AddModelError("DayOfWeek", "Required");

        // Act
        var result = await _scheduleController.PostSchedule(createSchedule);

        // Assert
        var badRequestResult = Assert.IsType<BadRequestObjectResult>(result.Result);
        Assert.IsType<SerializableError>(badRequestResult.Value);
    }

    [Fact]
    public async Task PostSchedule_ReturnsBadRequest_WhenServiceReturnsError()
    {
        // Arrange
        var createSchedule = new CreateSchedule
        {
            TimeIn = new TimeOnly(8, 0),
            TimeOut = new TimeOnly(9, 0),
            DayOfWeek = "Monday",
            SubjectId = 1,
            ClassroomId = 1,
            SectionId = 1,
            InstructorId = 1
        };

        _mockScheduleService
            .Setup(s => s.CreateScheduleAsync(createSchedule))
            .ReturnsAsync(((Schedules?)null, "Schedule conflict detected"));

        // Act
        var result = await _scheduleController.PostSchedule(createSchedule);

        // Assert
        var badRequestResult = Assert.IsType<BadRequestObjectResult>(result.Result);
        // Controller now returns structured JSON: { message = "..." }
        Assert.NotNull(badRequestResult.Value);
        var messageProperty = badRequestResult.Value.GetType().GetProperty("message");
        Assert.NotNull(messageProperty);
        var message = messageProperty.GetValue(badRequestResult.Value) as string;
        Assert.Equal("Schedule conflict detected", message);
    }

    [Fact]
    public async Task PostSchedule_ReturnsBadRequest_WhenScheduleIsNull()
    {
        // Arrange
        var createSchedule = new CreateSchedule
        {
            TimeIn = new TimeOnly(8, 0),
            TimeOut = new TimeOnly(9, 0),
            DayOfWeek = "Monday",
            SubjectId = 1,
            ClassroomId = 1,
            SectionId = 1,
            InstructorId = 1
        };

        _mockScheduleService
            .Setup(s => s.CreateScheduleAsync(createSchedule))
            .ReturnsAsync(((Schedules?)null, (string?)null));

        // Act
        var result = await _scheduleController.PostSchedule(createSchedule);

        // Assert
        var badRequestResult = Assert.IsType<BadRequestObjectResult>(result.Result);
        Assert.Contains("unexpected error", badRequestResult.Value?.ToString(), StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task PostSchedule_ThrowsException_WhenExceptionOccurs()
    {
        // Arrange
        var createSchedule = new CreateSchedule
        {
            TimeIn = new TimeOnly(8, 0),
            TimeOut = new TimeOnly(9, 0),
            DayOfWeek = "Monday",
            SubjectId = 1,
            ClassroomId = 1,
            SectionId = 1,
            InstructorId = 1
        };

        _mockScheduleService
            .Setup(s => s.CreateScheduleAsync(createSchedule))
            .ThrowsAsync(new Exception("Database error"));

        // Act & Assert
        // The controller no longer catches generic exceptions - they propagate to the global handler
        await Assert.ThrowsAsync<Exception>(() => _scheduleController.PostSchedule(createSchedule));
    }

    #endregion

    #region UpdateSchedule Tests

    [Fact]
    public async Task UpdateSchedule_ReturnsOkResult_WithUpdatedSchedule()
    {
        // Arrange
        int scheduleId = 1;
        var updateSchedule = new UpdateSchedule
        {
            TimeIn = new TimeOnly(9, 0),
            TimeOut = new TimeOnly(10, 0),
            DayOfWeek = "Tuesday",
            SubjectId = 1,
            ClassroomId = 1,
            SectionId = 1,
            InstructorId = 1
        };

        var updatedSchedule = new Schedules
        {
            Id = scheduleId,
            TimeIn = updateSchedule.TimeIn,
            TimeOut = updateSchedule.TimeOut,
            DayOfWeek = updateSchedule.DayOfWeek
        };

        _mockScheduleService
            .Setup(s => s.UpdateScheduleAsync(scheduleId, updateSchedule))
            .ReturnsAsync((updatedSchedule, (string?)null));

        // Act
        var result = await _scheduleController.UpdateSchedule(scheduleId, updateSchedule);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result.Result);
        var schedule = Assert.IsType<Schedules>(okResult.Value);
        Assert.Equal(scheduleId, schedule.Id);

        _mockScheduleService.Verify(s => s.UpdateScheduleAsync(scheduleId, updateSchedule), Times.Once);
    }

    [Fact]
    public async Task UpdateSchedule_ReturnsBadRequest_WhenModelStateIsInvalid()
    {
        // Arrange
        int scheduleId = 1;
        var updateSchedule = new UpdateSchedule
        {
            TimeIn = new TimeOnly(9, 0),
            TimeOut = new TimeOnly(10, 0),
            DayOfWeek = "Tuesday",
            SubjectId = 1,
            ClassroomId = 1,
            SectionId = 1,
            InstructorId = 1
        };

        _scheduleController.ModelState.AddModelError("DayOfWeek", "Required");

        // Act
        var result = await _scheduleController.UpdateSchedule(scheduleId, updateSchedule);

        // Assert
        var badRequestResult = Assert.IsType<BadRequestObjectResult>(result.Result);
        Assert.IsType<SerializableError>(badRequestResult.Value);
    }

    [Fact]
    public async Task UpdateSchedule_ReturnsBadRequest_WhenServiceReturnsError()
    {
        // Arrange
        int scheduleId = 1;
        var updateSchedule = new UpdateSchedule
        {
            TimeIn = new TimeOnly(9, 0),
            TimeOut = new TimeOnly(10, 0),
            DayOfWeek = "Tuesday",
            SubjectId = 1,
            ClassroomId = 1,
            SectionId = 1,
            InstructorId = 1
        };

        _mockScheduleService
            .Setup(s => s.UpdateScheduleAsync(scheduleId, updateSchedule))
            .ReturnsAsync(((Schedules?)null, "Schedule conflict detected"));

        // Act
        var result = await _scheduleController.UpdateSchedule(scheduleId, updateSchedule);

        // Assert
        var badRequestResult = Assert.IsType<BadRequestObjectResult>(result.Result);
        // Controller now returns structured JSON: { message = "..." }
        Assert.NotNull(badRequestResult.Value);
        var messageProperty = badRequestResult.Value.GetType().GetProperty("message");
        Assert.NotNull(messageProperty);
        var message = messageProperty.GetValue(badRequestResult.Value) as string;
        Assert.Equal("Schedule conflict detected", message);
    }

    [Fact]
    public async Task UpdateSchedule_ThrowsException_WhenExceptionOccurs()
    {
        // Arrange
        int scheduleId = 1;
        var updateSchedule = new UpdateSchedule
        {
            TimeIn = new TimeOnly(9, 0),
            TimeOut = new TimeOnly(10, 0),
            DayOfWeek = "Tuesday",
            SubjectId = 1,
            ClassroomId = 1,
            SectionId = 1,
            InstructorId = 1
        };

        _mockScheduleService
            .Setup(s => s.UpdateScheduleAsync(scheduleId, updateSchedule))
            .ThrowsAsync(new Exception("Database error"));

        // Act & Assert
        // The controller no longer catches generic exceptions - they propagate to the global handler
        await Assert.ThrowsAsync<Exception>(() => _scheduleController.UpdateSchedule(scheduleId, updateSchedule));
    }

    #endregion

    #region DeleteSchedule Tests

    [Fact]
    public async Task DeleteSchedule_ReturnsNoContent_WhenSuccessful()
    {
        // Arrange
        int scheduleId = 1;

        _mockScheduleService
            .Setup(s => s.DeleteScheduleAsync(scheduleId, It.IsAny<ClaimsPrincipal>()))
            .ReturnsAsync((string?)null);

        // Act
        var result = await _scheduleController.DeleteSchedule(scheduleId);

        // Assert
        Assert.IsType<NoContentResult>(result);

        _mockScheduleService.Verify(s => s.DeleteScheduleAsync(scheduleId, It.IsAny<ClaimsPrincipal>()), Times.Once);
    }

    [Fact]
    public async Task DeleteSchedule_ReturnsBadRequest_WhenServiceReturnsError()
    {
        // Arrange
        int scheduleId = 1;
        string errorMessage = "Cannot delete schedule with existing dependencies";

        _mockScheduleService
            .Setup(s => s.DeleteScheduleAsync(scheduleId, It.IsAny<ClaimsPrincipal>()))
            .ReturnsAsync(errorMessage);

        // Act
        var result = await _scheduleController.DeleteSchedule(scheduleId);

        // Assert
        var badRequestResult = Assert.IsType<BadRequestObjectResult>(result);
        // Controller now returns structured JSON: { message = "..." }
        Assert.NotNull(badRequestResult.Value);
        var messageProperty = badRequestResult.Value.GetType().GetProperty("message");
        Assert.NotNull(messageProperty);
        var message = messageProperty.GetValue(badRequestResult.Value) as string;
        Assert.Equal(errorMessage, message);
    }

    [Fact]
    public async Task DeleteSchedule_ThrowsException_WhenUnexpectedExceptionOccurs()
    {
        // Arrange
        int scheduleId = 1;

        _mockScheduleService
            .Setup(s => s.DeleteScheduleAsync(scheduleId, It.IsAny<ClaimsPrincipal>()))
            .ThrowsAsync(new Exception("Unexpected error"));

        // Act & Assert
        // The controller no longer catches generic exceptions - they propagate to the global handler
        await Assert.ThrowsAsync<Exception>(() => _scheduleController.DeleteSchedule(scheduleId));
    }

    #endregion
}
