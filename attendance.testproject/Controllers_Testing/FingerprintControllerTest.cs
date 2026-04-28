using System.Security.Claims;
using attendance_monitoring.Classes;
using attendance_monitoring.Constants;
using attendance_monitoring.Controllers;
using attendance_monitoring.Exceptions;
using attendance_monitoring.IServices;
using attendance_monitoring.Models.DTO.Response;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;

namespace attendance.testproject.Controllers_Testing;

public class FingerprintControllerTest
{
    private readonly Mock<IFingerprintService> _mockFingerprintService;
    private readonly Mock<ILogger<FingerprintController>> _mockLogger;
    private readonly FingerprintController _controller;

    public FingerprintControllerTest()
    {
        _mockFingerprintService = new Mock<IFingerprintService>();
        _mockLogger = new Mock<ILogger<FingerprintController>>();
        _controller = new FingerprintController(_mockFingerprintService.Object, _mockLogger.Object);
        SetUserContext();
    }

    [Fact]
    public async Task GetFingerprintsByDeviceId_ReturnsFingerprintsForDevice()
    {
        var fingerprintId = Guid.NewGuid();
        var studentId = Guid.NewGuid();
        _mockFingerprintService
            .Setup(service => service.GetFingerprintsByDeviceIdAsync("esp32-attendance-01", It.IsAny<ClaimsPrincipal>()))
            .ReturnsAsync(new List<FingerprintResponseDto>
            {
                new()
                {
                    Id = fingerprintId,
                    StudentId = studentId,
                    DeviceId = "esp32-attendance-01",
                    SensorFingerprintId = 7,
                    IsActive = true
                }
            });

        var result = await _controller.GetFingerprintsByDeviceId("esp32-attendance-01");

        var okResult = Assert.IsType<OkObjectResult>(result.Result);
        var dtos = Assert.IsAssignableFrom<IEnumerable<FingerprintResponseDto>>(okResult.Value);
        var dto = Assert.Single(dtos);
        Assert.Equal(fingerprintId, dto.Id);
        Assert.Equal(studentId, dto.StudentId);
        Assert.Equal("esp32-attendance-01", dto.DeviceId);
    }

    [Fact]
    public async Task GetPendingEnrollmentSession_ReturnsEnrollmentSessionResponse()
    {
        var enrollmentSessionId = Guid.NewGuid();
        var response = new FingerprintEnrollmentSessionResponseDto
        {
            Success = true,
            Message = "Fingerprint enrollment session ready",
            EnrollmentSessionId = enrollmentSessionId,
            StudentId = Guid.NewGuid(),
            StudentName = "John Doe",
            DeviceId = "esp32-attendance-01",
            AssignedSensorFingerprintId = 7,
            Status = "InProgress",
            ExpiresAt = DateTime.UtcNow.AddMinutes(5)
        };

        _mockFingerprintService
            .Setup(service => service.GetPendingEnrollmentSessionAsync("esp32-attendance-01", "device-secret"))
            .ReturnsAsync(response);

        _controller.Request.Headers["X-Device-Api-Key"] = "device-secret";

        var result = await _controller.GetPendingEnrollmentSession("esp32-attendance-01");

        var okResult = Assert.IsType<OkObjectResult>(result.Result);
        var dto = Assert.IsType<FingerprintEnrollmentSessionResponseDto>(okResult.Value);
        Assert.Equal(enrollmentSessionId, dto.EnrollmentSessionId);
        _mockFingerprintService.Verify(service => service.GetPendingEnrollmentSessionAsync("esp32-attendance-01", "device-secret"), Times.Once);
    }

    [Fact]
    public async Task GetEnrollmentSession_ReturnsSession_WhenFound()
    {
        var sessionId = Guid.NewGuid();
        var response = new FingerprintEnrollmentSessionResponseDto
        {
            Success = true,
            Message = "Fingerprint enrollment session ready",
            EnrollmentSessionId = sessionId,
            StudentId = Guid.NewGuid(),
            StudentName = "Test Student",
            DeviceId = "esp32-test",
            AssignedSensorFingerprintId = 5,
            Status = "Pending",
            ExpiresAt = DateTime.UtcNow.AddMinutes(5)
        };

        _mockFingerprintService
            .Setup(service => service.GetEnrollmentSessionAsync(sessionId, It.IsAny<ClaimsPrincipal>()))
            .ReturnsAsync(response);

        var result = await _controller.GetEnrollmentSession(sessionId);

        var okResult = Assert.IsType<OkObjectResult>(result.Result);
        var dto = Assert.IsType<FingerprintEnrollmentSessionResponseDto>(okResult.Value);
        Assert.Equal(sessionId, dto.EnrollmentSessionId);
        Assert.Equal("Test Student", dto.StudentName);
    }

    [Fact]
    public async Task GetEnrollmentSession_Returns404_WhenNotFound()
    {
        var sessionId = Guid.NewGuid();
        _mockFingerprintService
            .Setup(service => service.GetEnrollmentSessionAsync(sessionId, It.IsAny<ClaimsPrincipal>()))
            .ThrowsAsync(new EntityNotFoundException<Guid>("FingerprintEnrollmentSession", sessionId));

        var result = await _controller.GetEnrollmentSession(sessionId);

        var notFoundResult = Assert.IsType<NotFoundObjectResult>(result.Result);
        var dto = Assert.IsType<FingerprintEnrollmentSessionResponseDto>(notFoundResult.Value);
        Assert.False(dto.Success);
    }

    [Fact]
    public async Task GetEnrollmentSession_Returns403_WhenUnauthorized()
    {
        var sessionId = Guid.NewGuid();
        _mockFingerprintService
            .Setup(service => service.GetEnrollmentSessionAsync(sessionId, It.IsAny<ClaimsPrincipal>()))
            .ThrowsAsync(new EntityUnauthorizedException("FingerprintEnrollmentSession", "monitor fingerprint enrollment", "admin-1"));

        var result = await _controller.GetEnrollmentSession(sessionId);

        var objectResult = Assert.IsType<ObjectResult>(result.Result);
        Assert.Equal(StatusCodes.Status403Forbidden, objectResult.StatusCode);
        var dto = Assert.IsType<FingerprintEnrollmentSessionResponseDto>(objectResult.Value);
        Assert.False(dto.Success);
    }

    [Fact]
    public async Task CancelEnrollmentSession_ReturnsCancelledSession()
    {
        var sessionId = Guid.NewGuid();
        var response = new FingerprintEnrollmentSessionResponseDto
        {
            Success = true,
            Message = "Fingerprint enrollment session ready",
            EnrollmentSessionId = sessionId,
            StudentId = Guid.NewGuid(),
            StudentName = "Test Student",
            DeviceId = "esp32-test",
            AssignedSensorFingerprintId = 5,
            Status = "Cancelled",
            ExpiresAt = DateTime.UtcNow.AddMinutes(5),
            FailureReason = "Enrollment cancelled by user"
        };

        _mockFingerprintService
            .Setup(service => service.CancelEnrollmentSessionAsync(sessionId, It.IsAny<ClaimsPrincipal>()))
            .ReturnsAsync(response);

        var result = await _controller.CancelEnrollmentSession(sessionId);

        var okResult = Assert.IsType<OkObjectResult>(result.Result);
        var dto = Assert.IsType<FingerprintEnrollmentSessionResponseDto>(okResult.Value);
        Assert.Equal(sessionId, dto.EnrollmentSessionId);
        Assert.Equal("Cancelled", dto.Status);
        _mockFingerprintService.Verify(
            service => service.CancelEnrollmentSessionAsync(sessionId, It.IsAny<ClaimsPrincipal>()),
            Times.Once);
    }

    [Fact]
    public async Task GetDevices_ReturnsActiveDevices()
    {
        var deviceUuid = Guid.NewGuid();
        var devices = new List<FingerprintDevice>
        {
            new()
            {
                Id = deviceUuid,
                DeviceIdentifier = "esp32-01",
                Name = "Lab Scanner",
                Location = "Lab 201",
                IsActive = true,
                LastSeenAt = DateTime.UtcNow
            }
        };

        _mockFingerprintService
            .Setup(service => service.GetDevicesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(devices);

        var result = await _controller.GetDevices(CancellationToken.None);

        var okResult = Assert.IsType<OkObjectResult>(result.Result);
        var dtos = Assert.IsAssignableFrom<IEnumerable<FingerprintDeviceResponseDto>>(okResult.Value);
        var dto = Assert.Single(dtos);
        Assert.Equal(deviceUuid, dto.Id);
        Assert.Equal("esp32-01", dto.DeviceIdentifier);
        Assert.Equal("Lab Scanner", dto.Name);
        Assert.Equal("Lab 201", dto.Location);
    }

    private void SetUserContext()
    {
        var claims = new[]
        {
            new Claim(ClaimTypes.NameIdentifier, "admin-1"),
            new Claim(ClaimTypes.Role, RoleConstants.Admin)
        };

        _controller.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext
            {
                User = new ClaimsPrincipal(new ClaimsIdentity(claims, "TestAuth"))
            }
        };
    }
}
