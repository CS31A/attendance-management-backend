using System.Security.Claims;
using attendance_monitoring.Controllers;
using attendance_monitoring.IServices;
using attendance_monitoring.Models.DTO.Request;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;

namespace attendance.testproject.Controllers_Testing;

public class ReportsControllerTest
{
    private readonly Mock<IReportsService> _reportsService = new();
    private readonly Mock<ILogger<ReportsController>> _logger = new();
    private readonly ReportsController _controller;

    public ReportsControllerTest()
    {
        _controller = new ReportsController(_reportsService.Object, _logger.Object)
        {
            ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext
                {
                    User = new ClaimsPrincipal(new ClaimsIdentity([
                        new Claim(ClaimTypes.NameIdentifier, "inst-9"),
                        new Claim(ClaimTypes.Role, "Instructor"),
                    ], "TestAuth")),
                },
            },
        };
    }

    [Fact]
    public async Task GetInstructorSessionsReport_ReturnsForbid_WhenServiceRejectsCrossInstructorAccess()
    {
        // Arrange
        var instructorId = Guid.NewGuid();
        var filter = new AttendanceFilterRequest();

        _reportsService
            .Setup(service => service.GetInstructorSessionsReportAsync(instructorId, filter, It.IsAny<ClaimsPrincipal>()))
            .ThrowsAsync(new UnauthorizedAccessException("You can only view your own sessions report"));

        // Act
        var result = await _controller.GetInstructorSessionsReport(instructorId, filter);

        // Assert
        Assert.IsType<ForbidResult>(result.Result);
    }
}
