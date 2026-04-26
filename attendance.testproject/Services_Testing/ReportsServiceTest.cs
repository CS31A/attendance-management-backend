using System.Security.Claims;
using attendance_monitoring.Classes;
using attendance_monitoring.IRepository;
using attendance_monitoring.IServices;
using attendance_monitoring.Models.DTO.Request;
using attendance_monitoring.Models.DTO.Response;
using attendance_monitoring.Services;
using Microsoft.Extensions.Logging;

namespace attendance.testproject.Services_Testing;

public class ReportsServiceTest
{
    private readonly Mock<IAttendanceService> _attendanceService = new(MockBehavior.Strict);
    private readonly Mock<ISessionRepository> _sessionRepository = new(MockBehavior.Strict);
    private readonly Mock<ISectionRepository> _sectionRepository = new(MockBehavior.Strict);
    private readonly Mock<IInstructorRepository> _instructorRepository = new(MockBehavior.Strict);
    private readonly Mock<IScheduleRepository> _scheduleRepository = new(MockBehavior.Strict);
    private readonly Mock<IUserContextService> _userContextService = new(MockBehavior.Strict);
    private readonly Mock<ILogger<ReportsService>> _logger = new();
    private readonly ClaimsPrincipal _user = new(new ClaimsIdentity());

    [Fact]
    public async Task GetClassAttendanceReportAsync_UsesProjectedRowsAndComputesTotals()
    {
        // Arrange
        var sectionId = Guid.NewGuid();
        var filter = new AttendanceFilterRequest
        {
            StartDate = new DateTime(2026, 4, 1),
            EndDate = new DateTime(2026, 4, 30),
        };

        _sectionRepository
            .Setup(repository => repository.GetSectionByIdAsync(sectionId))
            .ReturnsAsync(new Section { Id = sectionId, Name = "BSCS 3A" });

        var sessionId1 = Guid.NewGuid();
        var sessionId2 = Guid.NewGuid();
        _sessionRepository
            .Setup(repository => repository.GetSectionSessionReportRowsAsync(sectionId, filter.StartDate, filter.EndDate))
            .ReturnsAsync([
                new SessionReportRowDto
                {
                    SessionId = sessionId1,
                    SessionDate = new DateTime(2026, 4, 10),
                    SubjectName = "Math",
                    DayOfWeek = "Thursday",
                    Status = "Ended",
                    PresentCount = 8,
                    LateCount = 1,
                    AbsentCount = 2,
                    ExcusedCount = 1,
                    TotalRecords = 12,
                    TotalEnrolled = 12,
                },
                new SessionReportRowDto
                {
                    SessionId = sessionId2,
                    SessionDate = new DateTime(2026, 4, 8),
                    SubjectName = "Math",
                    DayOfWeek = "Tuesday",
                    Status = "Ended",
                    PresentCount = 0,
                    LateCount = 0,
                    AbsentCount = 0,
                    ExcusedCount = 0,
                    TotalRecords = 0,
                    TotalEnrolled = 0,
                },
            ]);

        var service = new ReportsService(
            _attendanceService.Object,
            _sessionRepository.Object,
            _sectionRepository.Object,
            _instructorRepository.Object,
            _scheduleRepository.Object,
            _userContextService.Object,
            _logger.Object);

        // Act
        var result = await service.GetClassAttendanceReportAsync(sectionId, filter, _user);

        // Assert
        Assert.Equal(sectionId, result.SectionId);
        Assert.Equal("BSCS 3A", result.SectionName);
        Assert.Equal(2, result.TotalSessions);
        Assert.Equal(8, result.TotalPresent);
        Assert.Equal(1, result.TotalLate);
        Assert.Equal(2, result.TotalAbsent);
        Assert.Equal(1, result.TotalExcused);
        Assert.Equal(75m, result.AttendanceRate);
        Assert.Equal([sessionId1, sessionId2], result.Sessions.Select(session => session.SessionId).ToArray());
        Assert.Equal("Math (Thursday)", result.Sessions[0].ScheduleTitle);
        Assert.Equal(0m, result.Sessions[1].AttendanceRate);

        _sessionRepository.Verify(repository => repository.GetSectionSessionReportRowsAsync(sectionId, filter.StartDate, filter.EndDate), Times.Once);
    }

    [Fact]
    public async Task GetInstructorSessionsReportAsync_UsesProjectedRowsAndBuildsScheduleTitles()
    {
        // Arrange
        var instructorId = Guid.NewGuid();
        var filter = new AttendanceFilterRequest
        {
            StartDate = new DateTime(2026, 4, 1),
            EndDate = new DateTime(2026, 4, 30),
        };

        _instructorRepository
            .Setup(repository => repository.GetInstructorByIdAsync(instructorId))
            .ReturnsAsync(new Instructor
            {
                Id = instructorId,
                Firstname = "Ada",
                Lastname = "Lovelace",
                UserId = "inst-9",
            });

        var sessionId3 = Guid.NewGuid();
        var sessionId4 = Guid.NewGuid();
        _sessionRepository
            .Setup(repository => repository.GetInstructorSessionReportRowsAsync(instructorId, filter.StartDate, filter.EndDate))
            .ReturnsAsync([
                new SessionReportRowDto
                {
                    SessionId = sessionId3,
                    SessionDate = new DateTime(2026, 4, 12),
                    SubjectName = "Physics",
                    SectionName = "BSCS 3A",
                    Status = "Ended",
                    PresentCount = 6,
                    LateCount = 2,
                    AbsentCount = 1,
                    ExcusedCount = 0,
                    TotalRecords = 9,
                    TotalEnrolled = 9,
                },
                new SessionReportRowDto
                {
                    SessionId = sessionId4,
                    SessionDate = new DateTime(2026, 4, 5),
                    SubjectName = "Physics",
                    SectionName = string.Empty,
                    Status = "Ended",
                    PresentCount = 0,
                    LateCount = 0,
                    AbsentCount = 0,
                    ExcusedCount = 0,
                    TotalRecords = 0,
                    TotalEnrolled = 0,
                },
            ]);

        var service = new ReportsService(
            _attendanceService.Object,
            _sessionRepository.Object,
            _sectionRepository.Object,
            _instructorRepository.Object,
            _scheduleRepository.Object,
            _userContextService.Object,
            _logger.Object);

        // Act
        var result = await service.GetInstructorSessionsReportAsync(instructorId, filter, _user);

        // Assert
        Assert.Equal(instructorId, result.InstructorId);
        Assert.Equal("Ada Lovelace", result.InstructorName);
        Assert.Equal(2, result.TotalSessions);
        Assert.Equal([sessionId3, sessionId4], result.Sessions.Select(session => session.SessionId).ToArray());
        Assert.Equal("Physics - BSCS 3A", result.Sessions[0].ScheduleTitle);
        Assert.Equal("Physics", result.Sessions[1].ScheduleTitle);
        Assert.Equal(88.89m, result.Sessions[0].AttendanceRate);
        Assert.Equal(0m, result.Sessions[1].AttendanceRate);

        _sessionRepository.Verify(repository => repository.GetInstructorSessionReportRowsAsync(instructorId, filter.StartDate, filter.EndDate), Times.Once);
    }

    [Fact]
    public async Task GetInstructorSessionsReportAsync_WithInstructorId_BuildsScheduleTitles()
    {
        // Arrange
        var instructorId = Guid.NewGuid();
        var filter = new AttendanceFilterRequest
        {
            StartDate = new DateTime(2026, 4, 1),
            EndDate = new DateTime(2026, 4, 30),
        };

        _instructorRepository
            .Setup(repository => repository.GetInstructorByIdAsync(instructorId))
            .ReturnsAsync(new Instructor
            {
                Id = instructorId,
                Firstname = "Ada",
                Lastname = "Lovelace",
                UserId = "inst-9",
            });

        _sessionRepository
            .Setup(repository => repository.GetInstructorSessionReportRowsAsync(instructorId, filter.StartDate, filter.EndDate))
            .ReturnsAsync([
                new SessionReportRowDto
                {
                    SessionId = Guid.NewGuid(),
                    SessionDate = new DateTime(2026, 4, 12),
                    SubjectName = "Physics",
                    SectionName = "BSCS 3A",
                    Status = "Ended",
                    PresentCount = 6,
                    LateCount = 2,
                    AbsentCount = 1,
                    ExcusedCount = 0,
                    TotalRecords = 9,
                    TotalEnrolled = 9,
                },
            ]);

        var service = new ReportsService(
            _attendanceService.Object,
            _sessionRepository.Object,
            _sectionRepository.Object,
            _instructorRepository.Object,
            _scheduleRepository.Object,
            _userContextService.Object,
            _logger.Object);

        // Act
        var result = await service.GetInstructorSessionsReportAsync(instructorId, filter, _user);

        // Assert
        Assert.NotEqual(Guid.Empty, result.InstructorId);
        Assert.Equal("Ada Lovelace", result.InstructorName);
        Assert.Single(result.Sessions);
        Assert.Equal("Physics - BSCS 3A", result.Sessions[0].ScheduleTitle);
        _instructorRepository.Verify(repository => repository.GetInstructorByIdAsync(instructorId), Times.Once);
        _sessionRepository.Verify(repository => repository.GetInstructorSessionReportRowsAsync(instructorId, filter.StartDate, filter.EndDate), Times.Once);
    }


    [Fact]
    public void InstructorSessionItemDto_InheritsFrom_SessionAttendanceStatsDto()
    {
        // Assert
        Assert.True(typeof(InstructorSessionItemDto).IsSubclassOf(typeof(SessionAttendanceStatsDto)),
            "InstructorSessionItemDto should inherit from SessionAttendanceStatsDto");
    }

    [Fact]
    public void InstructorSessionItemDto_HasSectionName_AsOwnProperty()
    {
        // Arrange
        var sectionNameProperty = typeof(InstructorSessionItemDto).GetProperty("SectionName");

        // Assert
        Assert.NotNull(sectionNameProperty);
        Assert.Equal(typeof(InstructorSessionItemDto), sectionNameProperty.DeclaringType);
    }
}
