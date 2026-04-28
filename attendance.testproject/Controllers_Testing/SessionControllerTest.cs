using attendance_monitoring.Controllers;
using attendance_monitoring.Constants;
using attendance_monitoring.Exceptions;
using attendance_monitoring.IServices;
using attendance_monitoring.Models.DTO.Request;
using attendance_monitoring.Models.DTO.Response;
using System.Reflection;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Routing;
using Microsoft.Extensions.Logging;

namespace attendance.testproject.Controllers_Testing;

/// <summary>
/// Unit tests for SessionController
/// </summary>
public class SessionControllerTest
{
    private readonly Mock<ISessionService> _mockSessionService;
    private readonly Mock<ILogger<SessionController>> _mockLogger;
    private readonly SessionController _sessionController;

    public SessionControllerTest()
    {
        _mockSessionService = new Mock<ISessionService>();
        _mockLogger = new Mock<ILogger<SessionController>>();
        _sessionController = new SessionController(_mockSessionService.Object, _mockLogger.Object);
    }

    #region GetAllSessions Tests

    [Fact]
    public async Task GetAllSessions_ReturnsOkResult_WithSessionList()
    {
        // Arrange
        var sessions = new List<SessionResponseDto>
        {
            CreateTestSessionResponseDto(),
            CreateTestSessionResponseDto(),
            CreateTestSessionResponseDto()
        };

        _mockSessionService
            .Setup(s => s.GetAllSessionsAsync())
            .ReturnsAsync(sessions);

        // Act
        var result = await _sessionController.GetAllSessions();

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result.Result);
        var returnedSessions = Assert.IsAssignableFrom<IEnumerable<SessionResponseDto>>(okResult.Value);
        Assert.Equal(3, returnedSessions.Count());

        _mockSessionService.Verify(s => s.GetAllSessionsAsync(), Times.Once);
    }

    [Fact]
    public async Task GetAllSessions_ReturnsOkResult_WithEmptyList()
    {
        // Arrange
        _mockSessionService
            .Setup(s => s.GetAllSessionsAsync())
            .ReturnsAsync(new List<SessionResponseDto>());

        // Act
        var result = await _sessionController.GetAllSessions();

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result.Result);
        var returnedSessions = Assert.IsAssignableFrom<IEnumerable<SessionResponseDto>>(okResult.Value);
        Assert.Empty(returnedSessions);
    }

    #endregion

    #region GetSession Tests

    [Fact]
    public async Task GetSession_ReturnsOkResult_WithSession()
    {
        // Arrange
        var sessionId = Guid.NewGuid();
        var expectedSession = CreateTestSessionResponseDto(sessionId);

        _mockSessionService
            .Setup(s => s.GetSessionByIdAsync(sessionId))
            .ReturnsAsync(expectedSession);

        // Act
        var result = await _sessionController.GetSession(sessionId);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result.Result);
        var session = Assert.IsType<SessionResponseDto>(okResult.Value);
        Assert.Equal(expectedSession.Id, session.Id);

        _mockSessionService.Verify(s => s.GetSessionByIdAsync(sessionId), Times.Once);
    }

    [Fact]
    public async Task GetSession_ReturnsNotFound_WhenSessionDoesNotExist()
    {
        // Arrange
        var sessionId = Guid.NewGuid();
        _mockSessionService
            .Setup(s => s.GetSessionByIdAsync(sessionId))
            .ThrowsAsync(new EntityNotFoundException<Guid>("Session", sessionId));

        // Act
        var result = await _sessionController.GetSession(sessionId);

        // Assert
        var notFoundResult = Assert.IsType<NotFoundObjectResult>(result.Result);
        Assert.NotNull(notFoundResult.Value);
    }

    [Fact]
    public async Task GetSessionByUuid_ReturnsOkResult_WithSession()
    {
        var sessionUuid = Guid.NewGuid();
        var expectedSession = CreateTestSessionResponseDto(sessionId: sessionUuid);

        _mockSessionService
            .Setup(s => s.GetSessionByUuidAsync(sessionUuid))
            .ReturnsAsync(expectedSession);

        var result = await _sessionController.GetSessionByUuid(sessionUuid);

        var okResult = Assert.IsType<OkObjectResult>(result.Result);
        var session = Assert.IsType<SessionResponseDto>(okResult.Value);
        Assert.Equal(sessionUuid, session.Id);
        _mockSessionService.Verify(s => s.GetSessionByUuidAsync(sessionUuid), Times.Once);
    }

    [Fact]
    public async Task GetSessionByUuid_ReturnsNotFound_WhenSessionDoesNotExist()
    {
        var sessionUuid = Guid.NewGuid();

        _mockSessionService
            .Setup(s => s.GetSessionByUuidAsync(sessionUuid))
            .ThrowsAsync(new EntityNotFoundException<Guid>("Session", sessionUuid));

        var result = await _sessionController.GetSessionByUuid(sessionUuid);

        var notFoundResult = Assert.IsType<NotFoundObjectResult>(result.Result);
        Assert.NotNull(notFoundResult.Value);
    }

    [Fact]
    public async Task GetSessionByUuid_ReturnsForbidden_WhenServiceRejectsAccess()
    {
        var sessionUuid = Guid.NewGuid();

        _mockSessionService
            .Setup(s => s.GetSessionByUuidAsync(sessionUuid))
            .ThrowsAsync(new EntityUnauthorizedException("Session", "GetSessionByUuid", sessionUuid.ToString(), "Forbidden"));

        var result = await _sessionController.GetSessionByUuid(sessionUuid);

        var forbiddenResult = Assert.IsType<ObjectResult>(result.Result);
        Assert.Equal(403, forbiddenResult.StatusCode);
        Assert.NotNull(forbiddenResult.Value);
    }

    #endregion

    #region GetSessionsBySchedule Tests

    [Fact]
    public async Task GetSessionsBySchedule_ReturnsOkResult_WithSessions()
    {
        // Arrange
        var scheduleId = Guid.NewGuid();
        var sessions = new List<SessionResponseDto>
        {
            CreateTestSessionResponseDto(scheduleId: Guid.NewGuid()),
            CreateTestSessionResponseDto(scheduleId: Guid.NewGuid())
        };

        _mockSessionService
            .Setup(s => s.GetSessionsByScheduleIdAsync(scheduleId))
            .ReturnsAsync(sessions);

        // Act
        var result = await _sessionController.GetSessionsBySchedule(scheduleId);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result.Result);
        var returnedSessions = Assert.IsAssignableFrom<IEnumerable<SessionResponseDto>>(okResult.Value);
        Assert.Equal(2, returnedSessions.Count());
        Assert.Contains(returnedSessions, s => s.Id == sessions[0].Id);
        Assert.Contains(returnedSessions, s => s.Id == sessions[1].Id);
    }

    [Fact]
    public async Task GetSessionsByScheduleUuid_ReturnsOkResult_WithSessions()
    {
        var scheduleUuid = Guid.NewGuid();
        var sessions = new List<SessionResponseDto>
        {
            CreateTestSessionResponseDto(),
            CreateTestSessionResponseDto()
        };

        _mockSessionService
            .Setup(s => s.GetSessionsByScheduleUuidAsync(scheduleUuid))
            .ReturnsAsync(sessions);

        var result = await _sessionController.GetSessionsByScheduleUuid(scheduleUuid);

        var okResult = Assert.IsType<OkObjectResult>(result.Result);
        var returnedSessions = Assert.IsAssignableFrom<IEnumerable<SessionResponseDto>>(okResult.Value);
        Assert.Equal(2, returnedSessions.Count());
        _mockSessionService.Verify(s => s.GetSessionsByScheduleUuidAsync(scheduleUuid), Times.Once);
    }

    #endregion

    #region GetSessionsByStatus Tests

    [Fact]
    public async Task GetSessionsByStatus_ReturnsOkResult_WithSessions()
    {
        // Arrange
        string status = SessionStatusConstants.Active;
        var sessions = new List<SessionResponseDto>
        {
            CreateTestSessionResponseDto(status: SessionStatusConstants.Active),
            CreateTestSessionResponseDto(status: SessionStatusConstants.Active)
        };

        _mockSessionService
            .Setup(s => s.GetSessionsByStatusAsync(status))
            .ReturnsAsync(sessions);

        // Act
        var result = await _sessionController.GetSessionsByStatus(status);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result.Result);
        var returnedSessions = Assert.IsAssignableFrom<IEnumerable<SessionResponseDto>>(okResult.Value);
        Assert.Equal(2, returnedSessions.Count());
        Assert.All(returnedSessions, s => Assert.Equal(SessionStatusConstants.Active, s.Status));
    }

    [Fact]
    public async Task GetSessionsByStatus_ReturnsBadRequest_WhenStatusInvalid()
    {
        // Arrange
        string invalidStatus = "invalid_status";

        // Act
        var result = await _sessionController.GetSessionsByStatus(invalidStatus);

        // Assert
        var badRequestResult = Assert.IsType<BadRequestObjectResult>(result.Result);
        Assert.NotNull(badRequestResult.Value);
    }

    [Fact]
    public async Task GetSessionsByStatus_NormalizesUppercaseStatus_BeforeCallingService()
    {
        // Arrange
        const string uppercaseStatus = "ACTIVE";
        var sessions = new List<SessionResponseDto>
        {
            CreateTestSessionResponseDto(status: SessionStatusConstants.Active)
        };

        _mockSessionService
            .Setup(s => s.GetSessionsByStatusAsync(SessionStatusConstants.Active))
            .ReturnsAsync(sessions);

        // Act
        var result = await _sessionController.GetSessionsByStatus(uppercaseStatus);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result.Result);
        var returnedSessions = Assert.IsAssignableFrom<IEnumerable<SessionResponseDto>>(okResult.Value);
        Assert.Single(returnedSessions);
        _mockSessionService.Verify(s => s.GetSessionsByStatusAsync(SessionStatusConstants.Active), Times.Once);
    }

    [Theory]
    [InlineData(SessionStatusConstants.NotStarted)]
    [InlineData(SessionStatusConstants.Active)]
    [InlineData(SessionStatusConstants.Ended)]
    [InlineData(SessionStatusConstants.Cancelled)]
    public async Task GetSessionsByStatus_AcceptsValidStatuses(string validStatus)
    {
        // Arrange
        _mockSessionService
            .Setup(s => s.GetSessionsByStatusAsync(validStatus))
            .ReturnsAsync(new List<SessionResponseDto>());

        // Act
        var result = await _sessionController.GetSessionsByStatus(validStatus);

        // Assert
        Assert.IsType<OkObjectResult>(result.Result);
    }

    #endregion

    #region GetSessionsByDate Tests

    [Fact]
    public async Task GetSessionsByDate_ReturnsOkResult_WithSessions()
    {
        // Arrange
        var date = new DateTime(2024, 1, 15);
        var sessions = new List<SessionResponseDto>
        {
            CreateTestSessionResponseDto(sessionDate: date),
            CreateTestSessionResponseDto(sessionDate: date)
        };

        _mockSessionService
            .Setup(s => s.GetSessionsByDateAsync(date))
            .ReturnsAsync(sessions);

        // Act
        var result = await _sessionController.GetSessionsByDate(date);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result.Result);
        var returnedSessions = Assert.IsAssignableFrom<IEnumerable<SessionResponseDto>>(okResult.Value);
        Assert.Equal(2, returnedSessions.Count());
    }

    #endregion

    #region CreateSession Tests

    [Fact]
    public async Task CreateSession_ReturnsCreatedAtAction_WhenSuccessful()
    {
        // Arrange
        var request = new CreateSession
        {
            ScheduleId = Guid.NewGuid(),
            SessionDate = DateTime.UtcNow.Date.AddDays(1),
            Description = "Test session"
        };

        var createdSession = CreateTestSessionResponseDto(scheduleId: request.ScheduleId!.Value);

        _mockSessionService
            .Setup(s => s.CreateSessionAsync(request))
            .ReturnsAsync(createdSession);

        // Act
        var result = await _sessionController.CreateSession(request);

        // Assert
        var createdAtActionResult = Assert.IsType<CreatedAtActionResult>(result.Result);
        Assert.Equal(nameof(_sessionController.GetSessionByUuid), createdAtActionResult.ActionName);
        var session = Assert.IsType<SessionResponseDto>(createdAtActionResult.Value);
        Assert.Equal(createdSession.Id, session.Id);

        _mockSessionService.Verify(s => s.CreateSessionAsync(request), Times.Once);
    }

    [Fact]
    public async Task CreateSession_ReturnsBadRequest_WhenModelStateInvalid()
    {
        // Arrange
        var request = new CreateSession();
        _sessionController.ModelState.AddModelError("ScheduleId", "Required");

        // Act
        var result = await _sessionController.CreateSession(request);

        // Assert
        var badRequestResult = Assert.IsType<BadRequestObjectResult>(result.Result);
        Assert.IsType<SerializableError>(badRequestResult.Value);
    }

    [Fact]
    public void CreateSession_WithScheduleIdOnly_PassesValidation()
    {
        // Arrange
        var request = new CreateSession
        {
            ScheduleId = Guid.NewGuid(),
            SessionDate = DateTime.UtcNow.Date.AddDays(1)
        };
        var validationResults = new List<System.ComponentModel.DataAnnotations.ValidationResult>();

        // Act
        var isValid = System.ComponentModel.DataAnnotations.Validator.TryValidateObject(
            request,
            new System.ComponentModel.DataAnnotations.ValidationContext(request),
            validationResults,
            true);

        // Assert
        Assert.True(isValid);
        Assert.Empty(validationResults);
    }

    [Fact]
    public void CreateSession_WithoutScheduleId_FailsValidation()
    {
        // Arrange
        var request = new CreateSession();
        var validationResults = new List<System.ComponentModel.DataAnnotations.ValidationResult>();

        // Act
        var isValid = System.ComponentModel.DataAnnotations.Validator.TryValidateObject(
            request,
            new System.ComponentModel.DataAnnotations.ValidationContext(request),
            validationResults,
            true);

        // Assert
        Assert.False(isValid);
        Assert.Contains(validationResults,
            result => result.ErrorMessage == "ScheduleId is required.");
    }

    [Fact]
    public async Task CreateSession_ThrowsValidationException_WhenServiceThrowsValidationException()
    {
        // Arrange
        var request = new CreateSession
        {
            ScheduleId = Guid.NewGuid(),
            SessionDate = DateTime.UtcNow.Date.AddDays(1)
        };

        _mockSessionService
            .Setup(s => s.CreateSessionAsync(request))
            .ThrowsAsync(new ValidationException("Schedule not found"));

        // Act & Assert - Exception propagates to global handler
        var exception = await Assert.ThrowsAsync<ValidationException>(
            () => _sessionController.CreateSession(request));
        Assert.Equal("Schedule not found", exception.Message);
    }

    [Fact]
    public async Task CreateSession_ThrowsException_WhenServiceThrowsException()
    {
        // Arrange
        var request = new CreateSession
        {
            ScheduleId = Guid.NewGuid(),
            SessionDate = DateTime.UtcNow.Date.AddDays(1)
        };

        _mockSessionService
            .Setup(s => s.CreateSessionAsync(request))
            .ThrowsAsync(new Exception("Database error"));

        // Act & Assert - Exception propagates to global handler
        var exception = await Assert.ThrowsAsync<Exception>(
            () => _sessionController.CreateSession(request));
        Assert.Equal("Database error", exception.Message);
    }

    #endregion

    #region StartSession Tests

    [Fact]
    public async Task StartSession_ReturnsOkResult_WhenSuccessful()
    {
        // Arrange
        var sessionId = Guid.NewGuid();
        var request = new StartSession
        {
            ActualRoomId = Guid.NewGuid(),
            AttendanceCutoffMinutes = 15
        };

        var startedSession = CreateTestSessionResponseDto(sessionId, status: SessionStatusConstants.Active);

        _mockSessionService
            .Setup(s => s.StartSessionAsync(sessionId, request))
            .ReturnsAsync(startedSession);

        // Act
        var result = await _sessionController.StartSession(sessionId, request);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result.Result);
        var session = Assert.IsType<SessionResponseDto>(okResult.Value);
        Assert.Equal(SessionStatusConstants.Active, session.Status);

        _mockSessionService.Verify(s => s.StartSessionAsync(sessionId, request), Times.Once);
    }

    [Fact]
    public async Task StartSession_ReturnsBadRequest_WhenModelStateInvalid()
    {
        // Arrange
        var sessionId = Guid.NewGuid();
        var request = new StartSession();
        _sessionController.ModelState.AddModelError("ActualRoomId", "Required");

        // Act
        var result = await _sessionController.StartSession(sessionId, request);

        // Assert
        var badRequestResult = Assert.IsType<BadRequestObjectResult>(result.Result);
        Assert.IsType<SerializableError>(badRequestResult.Value);
    }

    [Fact]
    public async Task StartSession_ThrowsValidationException_WhenServiceThrowsValidationException()
    {
        // Arrange
        var sessionId = Guid.NewGuid();
        var request = new StartSession();

        _mockSessionService
            .Setup(s => s.StartSessionAsync(sessionId, request))
            .ThrowsAsync(new ValidationException("Session already started"));

        // Act & Assert - Exception propagates to global handler
        var exception = await Assert.ThrowsAsync<ValidationException>(
            () => _sessionController.StartSession(sessionId, request));
        Assert.Equal("Session already started", exception.Message);
    }

    [Fact]
    public async Task StartSession_ThrowsEntityNotFoundException_WhenSessionDoesNotExist()
    {
        // Arrange
        var sessionId = Guid.NewGuid();
        var request = new StartSession();

        _mockSessionService
            .Setup(s => s.StartSessionAsync(sessionId, request))
            .ThrowsAsync(new EntityNotFoundException<Guid>("Session", sessionId));

        // Act & Assert - Exception propagates to global handler
        await Assert.ThrowsAsync<EntityNotFoundException<Guid>>(
            () => _sessionController.StartSession(sessionId, request));
    }

    [Fact]
    public async Task StartSession_ThrowsException_WhenServiceThrowsException()
    {
        // Arrange
        var sessionId = Guid.NewGuid();
        var request = new StartSession();

        _mockSessionService
            .Setup(s => s.StartSessionAsync(sessionId, request))
            .ThrowsAsync(new Exception("Database error"));

        // Act & Assert - Exception propagates to global handler
        var exception = await Assert.ThrowsAsync<Exception>(
            () => _sessionController.StartSession(sessionId, request));
        Assert.Equal("Database error", exception.Message);
    }

    [Fact]
    public async Task StartSessionByUuid_ReturnsOkResult_WhenSuccessful()
    {
        var sessionUuid = Guid.NewGuid();
        var request = new StartSession
        {
            ActualRoomId = Guid.NewGuid(),
            AttendanceCutoffMinutes = 15
        };

        var startedSession = CreateTestSessionResponseDto(sessionId: sessionUuid, status: SessionStatusConstants.Active);

        _mockSessionService
            .Setup(s => s.StartSessionByUuidAsync(sessionUuid, request))
            .ReturnsAsync(startedSession);

        var result = await _sessionController.StartSessionByUuid(sessionUuid, request);

        var okResult = Assert.IsType<OkObjectResult>(result.Result);
        var session = Assert.IsType<SessionResponseDto>(okResult.Value);
        Assert.Equal(sessionUuid, session.Id);
        _mockSessionService.Verify(s => s.StartSessionByUuidAsync(sessionUuid, request), Times.Once);
    }

    #endregion

    #region EndSession Tests

    [Fact]
    public async Task EndSession_ReturnsOkResult_WhenSuccessful()
    {
        // Arrange
        var sessionId = Guid.NewGuid();
        var request = new EndSession
        {
            Description = "Session ended successfully"
        };

        var endedSession = CreateTestSessionResponseDto(sessionId, status: SessionStatusConstants.Ended);

        _mockSessionService
            .Setup(s => s.EndSessionAsync(sessionId, request))
            .ReturnsAsync(endedSession);

        // Act
        var result = await _sessionController.EndSession(sessionId, request);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result.Result);
        var session = Assert.IsType<SessionResponseDto>(okResult.Value);
        Assert.Equal(SessionStatusConstants.Ended, session.Status);

        _mockSessionService.Verify(s => s.EndSessionAsync(sessionId, request), Times.Once);
    }

    [Fact]
    public async Task EndSession_ReturnsBadRequest_WhenModelStateInvalid()
    {
        // Arrange
        var sessionId = Guid.NewGuid();
        var request = new EndSession();
        _sessionController.ModelState.AddModelError("Description", "Invalid");

        // Act
        var result = await _sessionController.EndSession(sessionId, request);

        // Assert
        var badRequestResult = Assert.IsType<BadRequestObjectResult>(result.Result);
        Assert.IsType<SerializableError>(badRequestResult.Value);
    }

    [Fact]
    public async Task EndSession_ThrowsValidationException_WhenServiceThrowsValidationException()
    {
        // Arrange
        var sessionId = Guid.NewGuid();
        var request = new EndSession();

        _mockSessionService
            .Setup(s => s.EndSessionAsync(sessionId, request))
            .ThrowsAsync(new ValidationException("Session not active"));

        // Act & Assert - Exception propagates to global handler
        var exception = await Assert.ThrowsAsync<ValidationException>(
            () => _sessionController.EndSession(sessionId, request));
        Assert.Equal("Session not active", exception.Message);
    }

    [Fact]
    public async Task EndSession_ThrowsEntityNotFoundException_WhenSessionDoesNotExist()
    {
        // Arrange
        var sessionId = Guid.NewGuid();
        var request = new EndSession();

        _mockSessionService
            .Setup(s => s.EndSessionAsync(sessionId, request))
            .ThrowsAsync(new EntityNotFoundException<Guid>("Session", sessionId));

        // Act & Assert - Exception propagates to global handler
        await Assert.ThrowsAsync<EntityNotFoundException<Guid>>(
            () => _sessionController.EndSession(sessionId, request));
    }

    [Fact]
    public async Task EndSession_ThrowsException_WhenServiceThrowsException()
    {
        // Arrange
        var sessionId = Guid.NewGuid();
        var request = new EndSession();

        _mockSessionService
            .Setup(s => s.EndSessionAsync(sessionId, request))
            .ThrowsAsync(new Exception("Database error"));

        // Act & Assert - Exception propagates to global handler
        var exception = await Assert.ThrowsAsync<Exception>(
            () => _sessionController.EndSession(sessionId, request));
        Assert.Equal("Database error", exception.Message);
    }

    #endregion

    #region CancelSession Tests

    [Fact]
    public async Task CancelSession_ReturnsOkResult_WhenSuccessful()
    {
        // Arrange
        var sessionId = Guid.NewGuid();
        var request = new CancelSession
        {
            Reason = "Instructor unavailable"
        };

        var cancelledSession = CreateTestSessionResponseDto(sessionId, status: SessionStatusConstants.Cancelled);

        _mockSessionService
            .Setup(s => s.CancelSessionAsync(sessionId, request))
            .ReturnsAsync(cancelledSession);

        // Act
        var result = await _sessionController.CancelSession(sessionId, request);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result.Result);
        var session = Assert.IsType<SessionResponseDto>(okResult.Value);
        Assert.Equal(SessionStatusConstants.Cancelled, session.Status);

        _mockSessionService.Verify(s => s.CancelSessionAsync(sessionId, request), Times.Once);
    }

    [Fact]
    public async Task CancelSession_ReturnsBadRequest_WhenModelStateInvalid()
    {
        // Arrange
        var sessionId = Guid.NewGuid();
        var request = new CancelSession();
        _sessionController.ModelState.AddModelError("Reason", "Required");

        // Act
        var result = await _sessionController.CancelSession(sessionId, request);

        // Assert
        var badRequestResult = Assert.IsType<BadRequestObjectResult>(result.Result);
        Assert.IsType<SerializableError>(badRequestResult.Value);
    }

    [Fact]
    public async Task CancelSession_ThrowsValidationException_WhenServiceThrowsValidationException()
    {
        // Arrange
        var sessionId = Guid.NewGuid();
        var request = new CancelSession { Reason = "Test" };

        _mockSessionService
            .Setup(s => s.CancelSessionAsync(sessionId, request))
            .ThrowsAsync(new ValidationException("Cannot cancel active session"));

        // Act & Assert - Exception propagates to global handler
        var exception = await Assert.ThrowsAsync<ValidationException>(
            () => _sessionController.CancelSession(sessionId, request));
        Assert.Equal("Cannot cancel active session", exception.Message);
    }

    [Fact]
    public async Task CancelSession_ThrowsEntityNotFoundException_WhenSessionDoesNotExist()
    {
        // Arrange
        var sessionId = Guid.NewGuid();
        var request = new CancelSession { Reason = "Test" };

        _mockSessionService
            .Setup(s => s.CancelSessionAsync(sessionId, request))
            .ThrowsAsync(new EntityNotFoundException<Guid>("Session", sessionId));

        // Act & Assert - Exception propagates to global handler
        await Assert.ThrowsAsync<EntityNotFoundException<Guid>>(
            () => _sessionController.CancelSession(sessionId, request));
    }

    [Fact]
    public async Task CancelSession_ThrowsException_WhenServiceThrowsException()
    {
        // Arrange
        var sessionId = Guid.NewGuid();
        var request = new CancelSession { Reason = "Test" };

        _mockSessionService
            .Setup(s => s.CancelSessionAsync(sessionId, request))
            .ThrowsAsync(new Exception("Database error"));

        // Act & Assert - Exception propagates to global handler
        var exception = await Assert.ThrowsAsync<Exception>(
            () => _sessionController.CancelSession(sessionId, request));
        Assert.Equal("Database error", exception.Message);
    }

    #endregion

    #region UpdateSessionRoom Tests

    [Fact]
    public async Task UpdateSessionRoom_ReturnsOkResult_WhenSuccessful()
    {
        // Arrange
        var sessionId = Guid.NewGuid();
        var request = new UpdateSessionRoom { ActualRoomId = Guid.NewGuid() };

        var updatedSession = CreateTestSessionResponseDto(sessionId, actualRoomId: request.ActualRoomId);

        _mockSessionService
            .Setup(s => s.UpdateSessionRoomAsync(sessionId, request))
            .ReturnsAsync(updatedSession);

        // Act
        var result = await _sessionController.UpdateSessionRoom(sessionId, request);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result.Result);
        var session = Assert.IsType<SessionResponseDto>(okResult.Value);
        Assert.Equal(request.ActualRoomId, session.ActualRoomId);

        _mockSessionService.Verify(s => s.UpdateSessionRoomAsync(sessionId, request), Times.Once);
    }

    [Fact]
    public async Task UpdateSessionRoom_ReturnsBadRequest_WhenModelStateInvalid()
    {
        // Arrange
        var sessionId = Guid.NewGuid();
        var request = new UpdateSessionRoom();
        _sessionController.ModelState.AddModelError("ActualRoomId", "Required");

        // Act
        var result = await _sessionController.UpdateSessionRoom(sessionId, request);

        // Assert
        var badRequestResult = Assert.IsType<BadRequestObjectResult>(result.Result);
        Assert.IsType<SerializableError>(badRequestResult.Value);
    }

    [Fact]
    public void UpdateSessionRoom_WithActualRoomIdOnly_PassesValidation()
    {
        // Arrange
        var request = new UpdateSessionRoom
        {
            ActualRoomId = Guid.NewGuid(),
            RowVersion = [1, 2, 3, 4]
        };
        var validationResults = new List<System.ComponentModel.DataAnnotations.ValidationResult>();

        // Act
        var isValid = System.ComponentModel.DataAnnotations.Validator.TryValidateObject(
            request,
            new System.ComponentModel.DataAnnotations.ValidationContext(request),
            validationResults,
            true);

        // Assert
        Assert.True(isValid);
        Assert.Empty(validationResults);
    }

    [Fact]
    public void UpdateSessionRoom_WithoutActualRoomId_FailsValidation()
    {
        // Arrange
        var request = new UpdateSessionRoom
        {
            RowVersion = [1, 2, 3, 4]
        };
        var validationResults = new List<System.ComponentModel.DataAnnotations.ValidationResult>();

        // Act
        var isValid = System.ComponentModel.DataAnnotations.Validator.TryValidateObject(
            request,
            new System.ComponentModel.DataAnnotations.ValidationContext(request),
            validationResults,
            true);

        // Assert
        Assert.False(isValid);
        Assert.Contains(validationResults,
            result => result.ErrorMessage == "ActualRoomId is required.");
    }

    [Fact]
    public async Task UpdateSessionRoom_ThrowsValidationException_WhenServiceThrowsValidationException()
    {
        // Arrange
        var sessionId = Guid.NewGuid();
        var request = new UpdateSessionRoom { ActualRoomId = Guid.NewGuid() };

        _mockSessionService
            .Setup(s => s.UpdateSessionRoomAsync(sessionId, request))
            .ThrowsAsync(new ValidationException("Session not active"));

        // Act & Assert - Exception propagates to global handler
        var exception = await Assert.ThrowsAsync<ValidationException>(
            () => _sessionController.UpdateSessionRoom(sessionId, request));
        Assert.Equal("Session not active", exception.Message);
    }

    [Fact]
    public async Task UpdateSessionRoom_ThrowsEntityNotFoundException_WhenSessionDoesNotExist()
    {
        // Arrange
        var sessionId = Guid.NewGuid();
        var request = new UpdateSessionRoom { ActualRoomId = Guid.NewGuid() };

        _mockSessionService
            .Setup(s => s.UpdateSessionRoomAsync(sessionId, request))
            .ThrowsAsync(new EntityNotFoundException<Guid>("Session", sessionId));

        // Act & Assert - Exception propagates to global handler
        await Assert.ThrowsAsync<EntityNotFoundException<Guid>>(
            () => _sessionController.UpdateSessionRoom(sessionId, request));
    }

    [Fact]
    public void SliceBRouteTemplates_SeparateIntAndUuidRoutes()
    {
        Assert.Equal("{id:int}", GetHttpTemplate(nameof(SessionController.GetSession)));
        Assert.Equal("{id:guid}", GetHttpTemplate(nameof(SessionController.GetSessionByUuid)));
        Assert.Equal("schedule/{scheduleId:int}", GetHttpTemplate(nameof(SessionController.GetSessionsBySchedule)));
        Assert.Equal("schedule/{id:guid}", GetHttpTemplate(nameof(SessionController.GetSessionsByScheduleUuid)));
        Assert.Equal("{id:int}/room", GetHttpTemplate(nameof(SessionController.UpdateSessionRoom)));
        Assert.Equal("{id:guid}/room", GetHttpTemplate(nameof(SessionController.UpdateSessionRoomByUuid)));
        Assert.Equal("{id:int}/start", GetHttpTemplate(nameof(SessionController.StartSession)));
        Assert.Equal("{id:guid}/start", GetHttpTemplate(nameof(SessionController.StartSessionByUuid)));
        Assert.Equal("{id:int}/end", GetHttpTemplate(nameof(SessionController.EndSession)));
        Assert.Equal("{id:guid}/end", GetHttpTemplate(nameof(SessionController.EndSessionByUuid)));
        Assert.Equal("{id:int}", GetHttpTemplate(nameof(SessionController.CancelSession)));
        Assert.Equal("{id:guid}", GetHttpTemplate(nameof(SessionController.CancelSessionByUuid)));
    }

    #endregion

    #region Helper Methods

    private static string? GetHttpTemplate(string methodName)
    {
        var method = typeof(SessionController).GetMethod(methodName, BindingFlags.Instance | BindingFlags.Public);
        Assert.NotNull(method);
        return method!.GetCustomAttributes()
            .OfType<HttpMethodAttribute>()
            .Single()
            .Template;
    }

    private SessionResponseDto CreateTestSessionResponseDto(
        Guid id = default,
        Guid? sessionId = null,
        Guid? scheduleId = null,
        string status = SessionStatusConstants.NotStarted,
        DateTime? sessionDate = null,
        Guid? actualRoomId = null)
    {
        return new SessionResponseDto
        {
            Id = sessionId ?? (id != default ? id : Guid.NewGuid()),
            ScheduleId = scheduleId ?? Guid.NewGuid(),
            Status = status,
            SessionDate = sessionDate ?? DateTime.UtcNow.Date,
            ActualRoomId = actualRoomId,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow,
            SubjectCode = "CS101",
            SubjectName = "Computer Science",
            SectionName = "A"
        };
    }

    #endregion
}
