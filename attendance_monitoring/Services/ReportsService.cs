using System.Security.Claims;
using attendance_monitoring.Classes;
using attendance_monitoring.Constants;
using attendance_monitoring.Exceptions;
using attendance_monitoring.IRepository;
using attendance_monitoring.IServices;
using attendance_monitoring.Models.DTO.Request;
using attendance_monitoring.Models.DTO.Response;

namespace attendance_monitoring.Services;

/// <summary>
/// Service for generating aggregated attendance reports.
/// Delegates single-entity report methods to IAttendanceService and
/// implements section/instructor-level aggregation directly.
/// </summary>
public class ReportsService(
    IAttendanceService attendanceService,
    ISessionRepository sessionRepository,
    ISectionRepository sectionRepository,
    IInstructorRepository instructorRepository,
    IScheduleRepository scheduleRepository,
    IUserContextService userContextService,
    ILogger<ReportsService> logger) : IReportsService
{
    public Task<AttendanceSummaryDto> GetAttendanceSummaryAsync(AttendanceFilterRequest filter, ClaimsPrincipal user)
        => attendanceService.GetAttendanceSummaryAsync(filter, user);

    public Task<StudentAttendanceHistoryDto> GetStudentAttendanceReportAsync(int studentId, ClaimsPrincipal user)
        => attendanceService.GetStudentAttendanceHistoryAsync(studentId, user);

    public Task<SessionAttendanceDto> GetSessionAttendanceReportAsync(int sessionId, ClaimsPrincipal user)
        => attendanceService.GetSessionAttendanceAsync(sessionId, user);

    public async Task<ClassAttendanceSummaryDto> GetClassAttendanceReportAsync(
        int sectionId, AttendanceFilterRequest filter, ClaimsPrincipal user)
    {
        logger.LogInformation("Retrieving class attendance report for SectionId: {SectionId}", sectionId);

        var section = await sectionRepository.GetSectionByIdAsync(sectionId).ConfigureAwait(false);
        if (section == null)
            throw new EntityNotFoundException<int>("Section", sectionId);

        // Authorization: Instructors can only view reports for sections they teach
        var userRole = user.FindFirst(ClaimTypes.Role)?.Value;
        if (userRole == RoleConstants.Instructor)
        {
            var userId = await userContextService.GetUserIdAsync(user).ConfigureAwait(false);
            if (userId == null)
            {
                logger.LogWarning("Unable to retrieve user ID for instructor");
                throw new UnauthorizedAccessException("Unable to verify instructor identity");
            }

            var instructor = await instructorRepository.GetInstructorByUserIdAsync(userId).ConfigureAwait(false);
            if (instructor == null)
            {
                logger.LogWarning("Instructor profile not found for user ID: {UserId}", userId);
                throw new EntityNotFoundException<string>("Instructor", userId,
                    "Instructor profile not found for authenticated user");
            }

            // Verify instructor teaches at least one schedule in this section
            var sectionSchedules = await scheduleRepository.GetSchedulesBySectionIdAsync(sectionId).ConfigureAwait(false);
            if (!sectionSchedules.Any(s => s.InstructorId == instructor.Id))
            {
                logger.LogWarning("Instructor {InstructorId} not authorized to view section {SectionId} attendance report",
                    instructor.Id, sectionId);
                throw new UnauthorizedAccessException("You can only view attendance reports for sections you teach");
            }
        }

        var sessionRows = await sessionRepository
            .GetSectionSessionReportRowsAsync(sectionId, filter.StartDate, filter.EndDate)
            .ConfigureAwait(false);

        var sessionStats = sessionRows
            .Select(row =>
            {
                var enrolledCount = row.TotalEnrolled;
                var calculatedAbsentCount = Math.Max(0, enrolledCount - row.PresentCount - row.LateCount - row.ExcusedCount);

                return new SessionAttendanceStatsDto
                {
                    SessionId = row.SessionId,
                    SessionDate = row.SessionDate,
                    SubjectName = row.SubjectName,
                    ScheduleTitle = $"{row.SubjectName} ({row.DayOfWeek})",
                    Status = row.Status,
                    PresentCount = row.PresentCount,
                    LateCount = row.LateCount,
                    AbsentCount = calculatedAbsentCount,
                    ExcusedCount = row.ExcusedCount,
                    TotalRecords = row.TotalRecords,
                    TotalEnrolled = enrolledCount,
                    AttendanceRate = enrolledCount > 0
                        ? Math.Round((decimal)(row.PresentCount + row.LateCount) / enrolledCount * 100, 2)
                        : 0,
                };
            }).ToList();

        var totalPresent = sessionStats.Sum(s => s.PresentCount);
        var totalLate = sessionStats.Sum(s => s.LateCount);
        var totalAbsent = sessionStats.Sum(s => s.AbsentCount);
        var totalExcused = sessionStats.Sum(s => s.ExcusedCount);
        var totalEnrolled = sessionStats.Sum(s => s.TotalEnrolled);
        var overallRate = totalEnrolled > 0
            ? Math.Round((decimal)(totalPresent + totalLate) / totalEnrolled * 100, 2)
            : 0;

        return new ClassAttendanceSummaryDto
        {
            SectionId = sectionId,
            SectionName = section.Name,
            TotalSessions = sessionRows.Count,
            TotalPresent = totalPresent,
            TotalLate = totalLate,
            TotalAbsent = totalAbsent,
            TotalExcused = totalExcused,
            AttendanceRate = overallRate,
            Sessions = sessionStats,
        };
    }

    public async Task<InstructorSessionsReportDto> GetInstructorSessionsReportAsync(
        int instructorId, AttendanceFilterRequest filter, ClaimsPrincipal user)
    {
        logger.LogInformation("Retrieving instructor sessions report for InstructorId: {InstructorId}", instructorId);

        var instructor = await instructorRepository.GetInstructorByIdAsync(instructorId).ConfigureAwait(false);
        if (instructor == null)
            throw new EntityNotFoundException<int>("Instructor", instructorId);

        return await BuildInstructorSessionsReportAsync(instructor, filter, user).ConfigureAwait(false);
    }

    public async Task<InstructorSessionsReportDto> GetInstructorSessionsReportAsync(
        Guid instructorUuid, AttendanceFilterRequest filter, ClaimsPrincipal user)
    {
        logger.LogInformation("Retrieving instructor sessions report for InstructorUuid: {InstructorUuid}", instructorUuid);

        var instructor = await instructorRepository.GetInstructorByUuidAsync(instructorUuid).ConfigureAwait(false);
        if (instructor == null)
            throw new EntityNotFoundException<Guid>("Instructor", instructorUuid);

        return await BuildInstructorSessionsReportAsync(instructor, filter, user).ConfigureAwait(false);
    }

    private async Task<InstructorSessionsReportDto> BuildInstructorSessionsReportAsync(
        Instructor instructor,
        AttendanceFilterRequest filter,
        ClaimsPrincipal user)
    {
        var instructorId = instructor.Id;

        // Authorization: Instructors can only view their own session reports
        var userRole = user.FindFirst(ClaimTypes.Role)?.Value;
        if (userRole == RoleConstants.Instructor)
        {
            var userId = await userContextService.GetUserIdAsync(user).ConfigureAwait(false);
            if (userId == null)
            {
                logger.LogWarning("Unable to retrieve user ID for instructor");
                throw new UnauthorizedAccessException("Unable to verify instructor identity");
            }

            var currentInstructor = await instructorRepository.GetInstructorByUserIdAsync(userId).ConfigureAwait(false);
            if (currentInstructor == null)
            {
                logger.LogWarning("Instructor profile not found for user ID: {UserId}", userId);
                throw new EntityNotFoundException<string>("Instructor", userId,
                    "Instructor profile not found for authenticated user");
            }

            if (currentInstructor.Id != instructorId)
            {
                logger.LogWarning("Instructor {CurrentInstructorId} not authorized to view instructor {TargetInstructorId} session report",
                    currentInstructor.Id, instructorId);
                throw new UnauthorizedAccessException("You can only view your own session reports");
            }
        }

        var sessionRows = await sessionRepository
            .GetInstructorSessionReportRowsAsync(instructorId, filter.StartDate, filter.EndDate)
            .ConfigureAwait(false);

        var sessionItems = sessionRows
            .Select(row =>
            {
                var enrolledCount = row.TotalEnrolled;
                var calculatedAbsentCount = Math.Max(0, enrolledCount - row.PresentCount - row.LateCount - row.ExcusedCount);

                return new InstructorSessionItemDto
                {
                    SessionId = row.SessionId,
                    SessionDate = row.SessionDate,
                    SubjectName = row.SubjectName,
                    SectionName = row.SectionName,
                    ScheduleTitle = !string.IsNullOrEmpty(row.SubjectName) && !string.IsNullOrEmpty(row.SectionName)
                        ? $"{row.SubjectName} - {row.SectionName}"
                        : !string.IsNullOrEmpty(row.SubjectName)
                            ? row.SubjectName
                            : row.SectionName,
                    Status = row.Status,
                    PresentCount = row.PresentCount,
                    LateCount = row.LateCount,
                    AbsentCount = calculatedAbsentCount,
                    ExcusedCount = row.ExcusedCount,
                    TotalRecords = row.TotalRecords,
                    TotalEnrolled = enrolledCount,
                    AttendanceRate = enrolledCount > 0
                        ? Math.Round((decimal)(row.PresentCount + row.LateCount) / enrolledCount * 100, 2)
                        : 0,
                };
            }).ToList();

        var instructorName = $"{instructor.Firstname} {instructor.Lastname}".Trim();

        return new InstructorSessionsReportDto
        {
            InstructorId = instructorId,
            InstructorName = instructorName,
            TotalSessions = sessionRows.Count,
            Sessions = sessionItems,
        };
    }
}
