using Microsoft.AspNetCore.Mvc;
using attendance_monitoring.Controllers;
using attendance_monitoring.IServices;
using attendance_monitoring.Classes;
using attendance_monitoring.Models.DTO.Request;
using attendance_monitoring.Models.DTO.Response;
using attendance_monitoring.Exceptions;
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
        var scheduleId = Guid.NewGuid();
        var expectedScheduleId = Guid.NewGuid();
        var expectedSchedule = new ScheduleResponseDto
        {
            Id = expectedScheduleId,
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
        Assert.Equal(expectedScheduleId, schedule.Id);

        _mockScheduleService.Verify(s => s.GetScheduleByIdAsync(scheduleId), Times.Once);
    }

    [Fact]
    public async Task GetScheduleByUuid_ReturnsOkResult_WithSchedule()
    {
        var scheduleUuid = Guid.NewGuid();
        var subjectUuid = Guid.NewGuid();
        var classroomUuid = Guid.NewGuid();
        var sectionUuid = Guid.NewGuid();
        var expectedSchedule = new ScheduleResponseDto
        {
            Id = scheduleUuid,
            TimeIn = new TimeOnly(8, 0),
            TimeOut = new TimeOnly(9, 0),
            DayOfWeek = "Monday",
            Subject = new SubjectResponseDto { Id = subjectUuid, Name = "Math", Code = "MATH101" },
            Classroom = new ClassroomResponseDto { Id = classroomUuid, Name = "Room 301" },
            Section = new SectionResponseDto { Id = sectionUuid, CourseId = Guid.NewGuid(), Name = "BSCS 3A" },
            Instructor = new InstructorResponseDto { Id = Guid.NewGuid(), Firstname = "Ada", Lastname = "Lovelace" }
        };

        _mockScheduleService
            .Setup(s => s.GetScheduleByUuidAsync(scheduleUuid))
            .ReturnsAsync(expectedSchedule);

        var result = await _scheduleController.GetScheduleByUuid(scheduleUuid);

        var okResult = Assert.IsType<OkObjectResult>(result.Result);
        var schedule = Assert.IsType<ScheduleResponseDto>(okResult.Value);
        Assert.Equal(scheduleUuid, schedule.Id);
        Assert.Equal(subjectUuid, schedule.Subject.Id);
        Assert.Equal(classroomUuid, schedule.Classroom.Id);
        Assert.Equal(sectionUuid, schedule.Section.Id);
    }

    [Fact]
    public async Task GetScheduleByUuid_ReturnsNotFound_WhenScheduleDoesNotExist()
    {
        var scheduleUuid = Guid.NewGuid();

        _mockScheduleService
            .Setup(s => s.GetScheduleByUuidAsync(scheduleUuid))
            .ThrowsAsync(new EntityNotFoundException<Guid>("Schedule", scheduleUuid));

        var result = await _scheduleController.GetScheduleByUuid(scheduleUuid);

        var notFoundResult = Assert.IsType<NotFoundObjectResult>(result.Result);
        Assert.NotNull(notFoundResult.Value);
        _mockScheduleService.Verify(s => s.GetScheduleByUuidAsync(scheduleUuid), Times.Once);
    }

    [Fact]
    public async Task GetScheduleByUuid_ThrowsException_WhenUnexpectedExceptionOccurs()
    {
        var scheduleUuid = Guid.NewGuid();

        _mockScheduleService
            .Setup(s => s.GetScheduleByUuidAsync(scheduleUuid))
            .ThrowsAsync(new Exception("Database error"));

        await Assert.ThrowsAsync<Exception>(() => _scheduleController.GetScheduleByUuid(scheduleUuid));
    }

    #endregion

    #region Dependency Check Tests

    [Fact]
    public async Task HasSessionsInSchedule_ReturnsOk_WithBooleanResult()
    {
        var scheduleId = Guid.NewGuid();
        _mockScheduleService
            .Setup(service => service.HasSessionsInScheduleAsync(scheduleId))
            .ReturnsAsync(true);

        var result = await _scheduleController.HasSessionsInSchedule(scheduleId);

        var okResult = Assert.IsType<OkObjectResult>(result.Result);
        Assert.True(Assert.IsType<bool>(okResult.Value));
    }

    [Fact]
    public async Task HasSessionsInSchedule_ReturnsBadRequest_ForInvalidId()
    {
        var result = await _scheduleController.HasSessionsInSchedule(Guid.Empty);

        var badRequestResult = Assert.IsType<BadRequestObjectResult>(result.Result);
        Assert.Equal("Schedule ID must be greater than 0.", badRequestResult.Value);
        _mockScheduleService.Verify(service => service.HasSessionsInScheduleAsync(It.IsAny<Guid>()), Times.Never);
    }

    [Fact]
    public async Task HasSessionsInSchedule_ReturnsServerError_WhenServiceThrowsEntityServiceException()
    {
        var scheduleId = Guid.NewGuid();
        _mockScheduleService
            .Setup(service => service.HasSessionsInScheduleAsync(scheduleId))
            .ThrowsAsync(new EntityServiceException("Schedule", $"HasSessionsInSchedule: {scheduleId}", "Error checking schedule dependencies"));

        var result = await _scheduleController.HasSessionsInSchedule(scheduleId);

        var objectResult = Assert.IsType<ObjectResult>(result.Result);
        Assert.Equal(500, objectResult.StatusCode);
        Assert.Equal("An error occurred while checking schedule dependencies", objectResult.Value);
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
            SubjectId = Guid.NewGuid(),
            ClassroomId = Guid.NewGuid(),
            SectionId = Guid.NewGuid(),
            InstructorId = Guid.NewGuid()
        };

        var createdSchedule = new Schedules
        {
            Id = Guid.NewGuid(),
            TimeIn = createSchedule.TimeIn,
            TimeOut = createSchedule.TimeOut,
            DayOfWeek = createSchedule.DayOfWeek
        };

        _mockScheduleService
            .Setup(s => s.CreateScheduleAsync(createSchedule))
            .ReturnsAsync(createdSchedule);

        // Act
        var result = await _scheduleController.PostSchedule(createSchedule);

        // Assert
        var createdAtActionResult = Assert.IsType<CreatedAtActionResult>(result.Result);
        Assert.Equal(nameof(_scheduleController.GetSchedule), createdAtActionResult.ActionName);
        var schedule = Assert.IsType<Schedules>(createdAtActionResult.Value);
        Assert.NotEqual(Guid.Empty, schedule.Id);

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
            SubjectId = Guid.NewGuid(),
            ClassroomId = Guid.NewGuid(),
            SectionId = Guid.NewGuid(),
            InstructorId = Guid.NewGuid()
        };

        _scheduleController.ModelState.AddModelError("DayOfWeek", "Required");

        // Act
        var result = await _scheduleController.PostSchedule(createSchedule);

        // Assert
        var badRequestResult = Assert.IsType<BadRequestObjectResult>(result.Result);
        Assert.IsType<SerializableError>(badRequestResult.Value);
    }

    [Fact]
    public async Task PostSchedule_ThrowsValidationException_WhenServiceThrowsError()
    {
        // Arrange
        var createSchedule = new CreateSchedule
        {
            TimeIn = new TimeOnly(8, 0),
            TimeOut = new TimeOnly(9, 0),
            DayOfWeek = "Monday",
            SubjectId = Guid.NewGuid(),
            ClassroomId = Guid.NewGuid(),
            SectionId = Guid.NewGuid(),
            InstructorId = Guid.NewGuid()
        };

        _mockScheduleService
            .Setup(s => s.CreateScheduleAsync(createSchedule))
            .ThrowsAsync(new ValidationException("Schedule conflict detected"));

        // Act & Assert
        // The controller lets exceptions propagate to the global handler
        var exception = await Assert.ThrowsAsync<ValidationException>(() => _scheduleController.PostSchedule(createSchedule));
        Assert.Equal("Schedule conflict detected", exception.Message);
    }

    [Fact]
    public async Task PostSchedule_ThrowsEntityServiceException_WhenUnexpectedErrorOccurs()
    {
        // Arrange
        var createSchedule = new CreateSchedule
        {
            TimeIn = new TimeOnly(8, 0),
            TimeOut = new TimeOnly(9, 0),
            DayOfWeek = "Monday",
            SubjectId = Guid.NewGuid(),
            ClassroomId = Guid.NewGuid(),
            SectionId = Guid.NewGuid(),
            InstructorId = Guid.NewGuid()
        };

        _mockScheduleService
            .Setup(s => s.CreateScheduleAsync(createSchedule))
            .ThrowsAsync(new EntityServiceException("Schedule", "create", "An unexpected error occurred"));

        // Act & Assert
        // The controller lets exceptions propagate to the global handler
        var exception = await Assert.ThrowsAsync<EntityServiceException>(() => _scheduleController.PostSchedule(createSchedule));
        Assert.Contains("unexpected error", exception.Message, StringComparison.OrdinalIgnoreCase);
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
            SubjectId = Guid.NewGuid(),
            ClassroomId = Guid.NewGuid(),
            SectionId = Guid.NewGuid(),
            InstructorId = Guid.NewGuid()
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
        var scheduleId = Guid.NewGuid();
        var updateSchedule = new UpdateSchedule
        {
            TimeIn = new TimeOnly(9, 0),
            TimeOut = new TimeOnly(10, 0),
            DayOfWeek = "Tuesday",
            SubjectId = Guid.NewGuid(),
            ClassroomId = Guid.NewGuid(),
            SectionId = Guid.NewGuid(),
            InstructorId = Guid.NewGuid()
        };

        var updatedSchedule = new Schedules
        {
            Id = scheduleId,
            TimeIn = updateSchedule.TimeIn!.Value,
            TimeOut = updateSchedule.TimeOut!.Value,
            DayOfWeek = updateSchedule.DayOfWeek!
        };

        _mockScheduleService
            .Setup(s => s.UpdateScheduleAsync(scheduleId, updateSchedule))
            .ReturnsAsync(updatedSchedule);

        // Act
        var result = await _scheduleController.UpdateSchedule(scheduleId, updateSchedule);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result.Result);
        var schedule = Assert.IsType<Schedules>(okResult.Value);
        Assert.Equal(scheduleId, schedule.Id);

        _mockScheduleService.Verify(s => s.UpdateScheduleAsync(scheduleId, updateSchedule), Times.Once);
    }

    [Fact]
    public async Task UpdateScheduleByUuid_ReturnsOkResult_WithUpdatedSchedule()
    {
        var scheduleUuid = Guid.NewGuid();
        var updateSchedule = new UpdateSchedule
        {
            DayOfWeek = "Wednesday"
        };

        var updatedSchedule = new Schedules
        {
            Id = scheduleUuid,
            TimeIn = new TimeOnly(9, 0),
            TimeOut = new TimeOnly(10, 0),
            DayOfWeek = updateSchedule.DayOfWeek!
        };

        _mockScheduleService
            .Setup(s => s.UpdateScheduleByUuidAsync(scheduleUuid, updateSchedule))
            .ReturnsAsync(updatedSchedule);

        var result = await _scheduleController.UpdateScheduleByUuid(scheduleUuid, updateSchedule);

        var okResult = Assert.IsType<OkObjectResult>(result.Result);
        var schedule = Assert.IsType<Schedules>(okResult.Value);
        Assert.Equal(scheduleUuid, schedule.Id);
    }

    [Fact]
    public async Task UpdateScheduleByUuid_ReturnsBadRequest_WhenModelStateIsInvalid()
    {
        var scheduleUuid = Guid.NewGuid();
        var updateSchedule = new UpdateSchedule
        {
            DayOfWeek = "Wednesday"
        };

        _scheduleController.ModelState.AddModelError("DayOfWeek", "Required");

        var result = await _scheduleController.UpdateScheduleByUuid(scheduleUuid, updateSchedule);

        var badRequestResult = Assert.IsType<BadRequestObjectResult>(result.Result);
        Assert.IsType<SerializableError>(badRequestResult.Value);
        _mockScheduleService.Verify(s => s.UpdateScheduleByUuidAsync(It.IsAny<Guid>(), It.IsAny<UpdateSchedule>()), Times.Never);
    }

    [Fact]
    public async Task UpdateScheduleByUuid_ThrowsValidationException_WhenServiceThrowsError()
    {
        var scheduleUuid = Guid.NewGuid();
        var updateSchedule = new UpdateSchedule
        {
            DayOfWeek = "Wednesday"
        };

        _mockScheduleService
            .Setup(s => s.UpdateScheduleByUuidAsync(scheduleUuid, updateSchedule))
            .ThrowsAsync(new ValidationException("Schedule conflict detected"));

        var exception = await Assert.ThrowsAsync<ValidationException>(() => _scheduleController.UpdateScheduleByUuid(scheduleUuid, updateSchedule));
        Assert.Equal("Schedule conflict detected", exception.Message);
    }

    [Fact]
    public async Task UpdateSchedule_ReturnsOkResult_WithPartialUpdate()
    {
        // Arrange - Test PATCH endpoint with only TimeIn field provided
        var scheduleId = Guid.NewGuid();
        var updateSchedule = new UpdateSchedule
        {
            TimeIn = new TimeOnly(10, 0) // Only updating TimeIn
        };

        var updatedSchedule = new Schedules
        {
            Id = scheduleId,
            TimeIn = updateSchedule.TimeIn!.Value,
            TimeOut = new TimeOnly(11, 0), // Existing value kept
            DayOfWeek = "Monday" // Existing value kept
        };

        _mockScheduleService
            .Setup(s => s.UpdateScheduleAsync(scheduleId, updateSchedule))
            .ReturnsAsync(updatedSchedule);

        // Act
        var result = await _scheduleController.UpdateSchedule(scheduleId, updateSchedule);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result.Result);
        var schedule = Assert.IsType<Schedules>(okResult.Value);
        Assert.Equal(scheduleId, schedule.Id);
        Assert.Equal(updateSchedule.TimeIn, schedule.TimeIn);

        _mockScheduleService.Verify(s => s.UpdateScheduleAsync(scheduleId, updateSchedule), Times.Once);
    }

    [Fact]
    public async Task UpdateSchedule_ReturnsBadRequest_WhenModelStateIsInvalid()
    {
        // Arrange
        var scheduleId = Guid.NewGuid();
        var updateSchedule = new UpdateSchedule
        {
            TimeIn = new TimeOnly(9, 0),
            TimeOut = new TimeOnly(10, 0),
            DayOfWeek = "Tuesday",
            SubjectId = Guid.NewGuid(),
            ClassroomId = Guid.NewGuid(),
            SectionId = Guid.NewGuid(),
            InstructorId = Guid.NewGuid()
        };

        _scheduleController.ModelState.AddModelError("DayOfWeek", "Required");

        // Act
        var result = await _scheduleController.UpdateSchedule(scheduleId, updateSchedule);

        // Assert
        var badRequestResult = Assert.IsType<BadRequestObjectResult>(result.Result);
        Assert.IsType<SerializableError>(badRequestResult.Value);
    }

    [Fact]
    public async Task UpdateSchedule_ThrowsValidationException_WhenServiceThrowsError()
    {
        // Arrange
        var scheduleId = Guid.NewGuid();
        var updateSchedule = new UpdateSchedule
        {
            TimeIn = new TimeOnly(9, 0),
            TimeOut = new TimeOnly(10, 0),
            DayOfWeek = "Tuesday",
            SubjectId = Guid.NewGuid(),
            ClassroomId = Guid.NewGuid(),
            SectionId = Guid.NewGuid(),
            InstructorId = Guid.NewGuid()
        };

        _mockScheduleService
            .Setup(s => s.UpdateScheduleAsync(scheduleId, updateSchedule))
            .ThrowsAsync(new ValidationException("Schedule conflict detected"));

        // Act & Assert
        // The controller lets exceptions propagate to the global handler
        var exception = await Assert.ThrowsAsync<ValidationException>(() => _scheduleController.UpdateSchedule(scheduleId, updateSchedule));
        Assert.Equal("Schedule conflict detected", exception.Message);
    }

    [Fact]
    public async Task UpdateSchedule_ThrowsException_WhenExceptionOccurs()
    {
        // Arrange
        var scheduleId = Guid.NewGuid();
        var updateSchedule = new UpdateSchedule
        {
            TimeIn = new TimeOnly(9, 0),
            TimeOut = new TimeOnly(10, 0),
            DayOfWeek = "Tuesday",
            SubjectId = Guid.NewGuid(),
            ClassroomId = Guid.NewGuid(),
            SectionId = Guid.NewGuid(),
            InstructorId = Guid.NewGuid()
        };

        _mockScheduleService
            .Setup(s => s.UpdateScheduleAsync(scheduleId, updateSchedule))
            .ThrowsAsync(new Exception("Database error"));

        // Act & Assert
        // The controller no longer catches generic exceptions - they propagate to the global handler
        await Assert.ThrowsAsync<Exception>(() => _scheduleController.UpdateSchedule(scheduleId, updateSchedule));
    }

    [Fact]
    public async Task UpdateSchedule_ThrowsValidationException_WhenTimeOutBeforeTimeIn()
    {
        // Arrange
        var scheduleId = Guid.NewGuid();
        var updateSchedule = new UpdateSchedule
        {
            TimeIn = new TimeOnly(10, 0),  // 10:00 AM
            TimeOut = new TimeOnly(9, 0)   // 9:00 AM - invalid, before TimeIn
        };

        _mockScheduleService
            .Setup(s => s.UpdateScheduleAsync(scheduleId, updateSchedule))
            .ThrowsAsync(new ValidationException("TimeOut must be after TimeIn"));

        // Act & Assert
        // The controller lets exceptions propagate to the global handler
        var exception = await Assert.ThrowsAsync<ValidationException>(() => _scheduleController.UpdateSchedule(scheduleId, updateSchedule));
        Assert.Equal("TimeOut must be after TimeIn", exception.Message);
    }

    [Fact]
    public async Task UpdateSchedule_ThrowsValidationException_WhenInvalidDayOfWeek()
    {
        // Arrange
        var scheduleId = Guid.NewGuid();
        var updateSchedule = new UpdateSchedule
        {
            DayOfWeek = "InvalidDay"
        };

        _mockScheduleService
            .Setup(s => s.UpdateScheduleAsync(scheduleId, updateSchedule))
            .ThrowsAsync(new ValidationException("Invalid DayOfWeek. Must be one of: Monday, Tuesday, Wednesday, Thursday, Friday, Saturday, Sunday"));

        // Act & Assert
        // The controller lets exceptions propagate to the global handler
        var exception = await Assert.ThrowsAsync<ValidationException>(() => _scheduleController.UpdateSchedule(scheduleId, updateSchedule));
        Assert.Contains("Invalid DayOfWeek", exception.Message);
    }

    #endregion

    #region DeleteSchedule Tests

    [Fact]
    public async Task DeleteSchedule_ReturnsNoContent_WhenSuccessful()
    {
        // Arrange
        var scheduleId = Guid.NewGuid();

        _mockScheduleService
            .Setup(s => s.DeleteScheduleAsync(scheduleId, It.IsAny<ClaimsPrincipal>()))
            .Returns(Task.CompletedTask);

        // Act
        var result = await _scheduleController.DeleteSchedule(scheduleId);

        // Assert
        Assert.IsType<NoContentResult>(result);

        _mockScheduleService.Verify(s => s.DeleteScheduleAsync(scheduleId, It.IsAny<ClaimsPrincipal>()), Times.Once);
    }

    [Fact]
    public async Task DeleteScheduleByUuid_ReturnsNoContent_WhenSuccessful()
    {
        var scheduleUuid = Guid.NewGuid();

        _mockScheduleService
            .Setup(s => s.DeleteScheduleByUuidAsync(scheduleUuid, It.IsAny<ClaimsPrincipal>()))
            .Returns(Task.CompletedTask);

        var result = await _scheduleController.DeleteScheduleByUuid(scheduleUuid);

        Assert.IsType<NoContentResult>(result);
        _mockScheduleService.Verify(s => s.DeleteScheduleByUuidAsync(scheduleUuid, It.IsAny<ClaimsPrincipal>()), Times.Once);
    }

    [Fact]
    public async Task DeleteScheduleByUuid_ThrowsValidationException_WhenServiceThrowsError()
    {
        var scheduleUuid = Guid.NewGuid();

        _mockScheduleService
            .Setup(s => s.DeleteScheduleByUuidAsync(scheduleUuid, It.IsAny<ClaimsPrincipal>()))
            .ThrowsAsync(new ValidationException("Cannot delete schedule with existing dependencies"));

        var exception = await Assert.ThrowsAsync<ValidationException>(() => _scheduleController.DeleteScheduleByUuid(scheduleUuid));
        Assert.Equal("Cannot delete schedule with existing dependencies", exception.Message);
    }

    [Fact]
    public async Task DeleteSchedule_ThrowsValidationException_WhenServiceThrowsError()
    {
        // Arrange
        var scheduleId = Guid.NewGuid();
        string errorMessage = "Cannot delete schedule with existing dependencies";

        _mockScheduleService
            .Setup(s => s.DeleteScheduleAsync(scheduleId, It.IsAny<ClaimsPrincipal>()))
            .ThrowsAsync(new ValidationException(errorMessage));

        // Act & Assert
        // The controller lets exceptions propagate to the global handler
        var exception = await Assert.ThrowsAsync<ValidationException>(() => _scheduleController.DeleteSchedule(scheduleId));
        Assert.Equal(errorMessage, exception.Message);
    }

    [Fact]
    public async Task DeleteSchedule_ThrowsException_WhenUnexpectedExceptionOccurs()
    {
        // Arrange
        var scheduleId = Guid.NewGuid();

        _mockScheduleService
            .Setup(s => s.DeleteScheduleAsync(scheduleId, It.IsAny<ClaimsPrincipal>()))
            .ThrowsAsync(new Exception("Unexpected error"));

        // Act & Assert
        // The controller no longer catches generic exceptions - they propagate to the global handler
        await Assert.ThrowsAsync<Exception>(() => _scheduleController.DeleteSchedule(scheduleId));
    }

    #endregion
}
