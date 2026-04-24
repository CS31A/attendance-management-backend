using System.Reflection;
using attendance_monitoring.Controllers;
using attendance_monitoring.IServices;
using attendance_monitoring.Models.DTO.Request;
using attendance_monitoring.Models.DTO.Response;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Routing;
using Microsoft.Extensions.Logging;

namespace attendance.testproject.Controllers_Testing;

public class AttendanceControllerTest
{
    private readonly Mock<IAttendanceService> _attendanceService = new();
    private readonly AttendanceController _controller;

    public AttendanceControllerTest()
    {
        _controller = new AttendanceController(_attendanceService.Object, new Mock<ILogger<AttendanceController>>().Object);
    }

    [Fact]
    public void GetSessionAttendance_RequiresPrivilegedPolicy()
    {
        var method = typeof(AttendanceController).GetMethod(nameof(AttendanceController.GetSessionAttendance));
        Assert.NotNull(method);

        var authorizeAttribute = method.GetCustomAttribute<AuthorizeAttribute>();
        Assert.NotNull(authorizeAttribute);
        Assert.Equal("PrivilegedPolicy", authorizeAttribute.Policy);
    }

    [Fact]
    public async Task GetAttendanceByUuid_ReturnsOkResult()
    {
        var attendanceUuid = Guid.NewGuid();
        var dto = new AttendanceRecordResponseDto
        {
            Id = 7,
            Uuid = attendanceUuid,
            StudentId = 11,
            StudentUuid = Guid.NewGuid(),
            SessionId = 12,
            SessionUuid = Guid.NewGuid(),
            ScheduleId = 13,
            ScheduleUuid = Guid.NewGuid(),
            Status = "Present",
            StudentName = "Ada Lovelace",
            StudentNumber = "11",
            InstructorName = "Prof. Turing",
            SubjectName = "Algorithms",
            SectionName = "CS-3A",
            RoomName = "Lab 1",
            ScheduleTitle = "Algorithms - CS-3A"
        };

        _attendanceService
            .Setup(service => service.GetAttendanceByUuidAsync(attendanceUuid, _controller.User))
            .ReturnsAsync(dto);

        var result = await _controller.GetAttendanceByUuid(attendanceUuid);

        var okResult = Assert.IsType<OkObjectResult>(result.Result);
        var response = Assert.IsType<AttendanceRecordResponseDto>(okResult.Value);
        Assert.Equal(attendanceUuid, response.Uuid);
    }

    [Fact]
    public async Task UpdateAttendanceByUuid_ReturnsOkResult()
    {
        var attendanceUuid = Guid.NewGuid();
        var request = new UpdateAttendanceRequest { Status = "Late" };
        var dto = new AttendanceRecordResponseDto
        {
            Id = 7,
            Uuid = attendanceUuid,
            Status = "Late",
            StudentId = 11,
            SessionId = 12,
            ScheduleId = 13,
            StudentName = "Ada Lovelace",
            StudentNumber = "11",
            InstructorName = "Prof. Turing",
            SubjectName = "Algorithms",
            SectionName = "CS-3A",
            RoomName = "Lab 1",
            ScheduleTitle = "Algorithms - CS-3A"
        };

        _attendanceService
            .Setup(service => service.UpdateAttendanceByUuidAsync(attendanceUuid, request, _controller.User))
            .ReturnsAsync(dto);

        var result = await _controller.UpdateAttendanceByUuid(attendanceUuid, request);

        var okResult = Assert.IsType<OkObjectResult>(result.Result);
        var response = Assert.IsType<AttendanceRecordResponseDto>(okResult.Value);
        Assert.Equal(attendanceUuid, response.Uuid);
    }

    [Fact]
    public async Task DeleteAttendanceByUuid_ReturnsNoContent()
    {
        var attendanceUuid = Guid.NewGuid();

        _attendanceService
            .Setup(service => service.DeleteAttendanceByUuidAsync(attendanceUuid, _controller.User))
            .ReturnsAsync(true);

        var result = await _controller.DeleteAttendanceByUuid(attendanceUuid);

        Assert.IsType<NoContentResult>(result);
    }

    [Fact]
    public void SliceBRouteTemplates_SeparateIntAndUuidRoutes()
    {
        Assert.Equal("{id:int}", GetHttpTemplate(nameof(AttendanceController.GetAttendance)));
        Assert.Equal("uuid/{uuid:guid}", GetHttpTemplate(nameof(AttendanceController.GetAttendanceByUuid)));
        Assert.Equal("{id:int}", GetHttpTemplate(nameof(AttendanceController.UpdateAttendance)));
        Assert.Equal("uuid/{uuid:guid}", GetHttpTemplate(nameof(AttendanceController.UpdateAttendanceByUuid)));
        Assert.Equal("{id:int}", GetHttpTemplate(nameof(AttendanceController.DeleteAttendance)));
        Assert.Equal("uuid/{uuid:guid}", GetHttpTemplate(nameof(AttendanceController.DeleteAttendanceByUuid)));
        Assert.Equal("student/{studentId:int}", GetHttpTemplate(nameof(AttendanceController.GetStudentAttendanceHistory)));
        Assert.Equal("student/uuid/{studentUuid:guid}", GetHttpTemplate(nameof(AttendanceController.GetStudentAttendanceHistoryByUuid)));
        Assert.Equal("session/{sessionId:int}", GetHttpTemplate(nameof(AttendanceController.GetSessionAttendance)));
        Assert.Equal("session/uuid/{sessionUuid:guid}", GetHttpTemplate(nameof(AttendanceController.GetSessionAttendanceByUuid)));
    }

    private static string? GetHttpTemplate(string methodName)
    {
        var method = typeof(AttendanceController).GetMethod(methodName, BindingFlags.Instance | BindingFlags.Public);
        Assert.NotNull(method);
        return method!.GetCustomAttributes()
            .OfType<HttpMethodAttribute>()
            .Single()
            .Template;
    }
}
