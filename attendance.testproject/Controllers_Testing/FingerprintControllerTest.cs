using System.Security.Claims;
using attendance_monitoring.Classes;
using attendance_monitoring.Constants;
using attendance_monitoring.Controllers;
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
            Id = enrollmentSessionId,
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
        Assert.Equal(enrollmentSessionId, dto.Id);
        _mockFingerprintService.Verify(service => service.GetPendingEnrollmentSessionAsync("esp32-attendance-01", "device-secret"), Times.Once);
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
