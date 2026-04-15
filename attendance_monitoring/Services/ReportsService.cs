using System.Security.Claims;
using attendance_monitoring.Classes;
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
    IAttendanceRepository attendanceRepository,
    ISessionRepository sessionRepository,
    IScheduleRepository scheduleRepository,
    ISectionRepository sectionRepository,
    IInstructorRepository instructorRepository,
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

        var schedules = (await scheduleRepository.GetSchedulesBySectionIdAsync(sectionId).ConfigureAwait(false)).ToList();

        var allSessions = new List<Session>();
        foreach (var schedule in schedules)
        {
            var sessions = await sessionRepository.GetSessionsByScheduleIdAsync(schedule.Id).ConfigureAwait(false);
            allSessions.AddRange(sessions);
        }

        if (filter.StartDate.HasValue)
            allSessions = allSessions.Where(s => s.SessionDate >= filter.StartDate.Value).ToList();
        if (filter.EndDate.HasValue)
            allSessions = allSessions.Where(s => s.SessionDate <= filter.EndDate.Value).ToList();

        var sessionIds = allSessions.Select(s => s.Id).ToList();

        var attendanceRecords = sessionIds.Count > 0
            ? await attendanceRepository.GetBySessionIdsAsync(sessionIds).ConfigureAwait(false)
            : new List<AttendanceRecord>();

        var bySession = attendanceRecords
            .GroupBy(r => r.SessionId)
            .ToDictionary(g => g.Key, g => g.ToList());

        var scheduleMap = schedules.ToDictionary(sc => sc.Id);

        var sessionStats = allSessions
            .OrderByDescending(s => s.SessionDate)
            .Select(s =>
            {
                var records = bySession.GetValueOrDefault(s.Id) ?? new List<AttendanceRecord>();
                var present = records.Count(r => r.Status == "Present");
                var late = records.Count(r => r.Status == "Late");
                var absent = records.Count(r => r.Status == "Absent");
                var excused = records.Count(r => r.Status == "Excused");
                var total = records.Count;
                var rate = total > 0 ? Math.Round((decimal)(present + late) / total * 100, 2) : 0;

                scheduleMap.TryGetValue(s.ScheduleId, out var schedule);
                var subjectName = schedule?.Subject?.Name ?? string.Empty;

                return new SessionAttendanceStatsDto
                {
                    SessionId = s.Id,
                    SessionDate = s.SessionDate,
                    SubjectName = subjectName,
                    ScheduleTitle = schedule != null
                        ? $"{subjectName} ({schedule.DayOfWeek})"
                        : string.Empty,
                    Status = s.Status,
                    PresentCount = present,
                    LateCount = late,
                    AbsentCount = absent,
                    ExcusedCount = excused,
                    TotalRecords = total,
                    AttendanceRate = rate,
                };
            }).ToList();

        var totalPresent = sessionStats.Sum(s => s.PresentCount);
        var totalLate = sessionStats.Sum(s => s.LateCount);
        var totalAbsent = sessionStats.Sum(s => s.AbsentCount);
        var totalExcused = sessionStats.Sum(s => s.ExcusedCount);
        var totalRecords = sessionStats.Sum(s => s.TotalRecords);
        var overallRate = totalRecords > 0
            ? Math.Round((decimal)(totalPresent + totalLate) / totalRecords * 100, 2)
            : 0;

        return new ClassAttendanceSummaryDto
        {
            SectionId = sectionId,
            SectionName = section.Name,
            TotalSessions = allSessions.Count,
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

        var sessions = (await sessionRepository.GetSessionsByInstructorIdAsync(instructorId).ConfigureAwait(false)).ToList();

        if (filter.StartDate.HasValue)
            sessions = sessions.Where(s => s.SessionDate >= filter.StartDate.Value).ToList();
        if (filter.EndDate.HasValue)
            sessions = sessions.Where(s => s.SessionDate <= filter.EndDate.Value).ToList();

        var sessionIds = sessions.Select(s => s.Id).ToList();

        var attendanceRecords = sessionIds.Count > 0
            ? await attendanceRepository.GetBySessionIdsAsync(sessionIds).ConfigureAwait(false)
            : new List<AttendanceRecord>();

        var bySession = attendanceRecords
            .GroupBy(r => r.SessionId)
            .ToDictionary(g => g.Key, g => g.ToList());

        var sessionItems = sessions
            .OrderByDescending(s => s.SessionDate)
            .Select(s =>
            {
                var records = bySession.GetValueOrDefault(s.Id) ?? new List<AttendanceRecord>();
                var present = records.Count(r => r.Status == "Present");
                var late = records.Count(r => r.Status == "Late");
                var absent = records.Count(r => r.Status == "Absent");
                var excused = records.Count(r => r.Status == "Excused");
                var total = records.Count;
                var rate = total > 0 ? Math.Round((decimal)(present + late) / total * 100, 2) : 0;

                var subjectName = s.Schedule?.Subject?.Name ?? string.Empty;
                var sectionName = s.Schedule?.Section?.Name ?? string.Empty;

                return new InstructorSessionItemDto
                {
                    SessionId = s.Id,
                    SessionDate = s.SessionDate,
                    SubjectName = subjectName,
                    SectionName = sectionName,
                    ScheduleTitle = !string.IsNullOrEmpty(subjectName) && !string.IsNullOrEmpty(sectionName)
                        ? $"{subjectName} - {sectionName}"
                        : subjectName,
                    Status = s.Status,
                    PresentCount = present,
                    LateCount = late,
                    AbsentCount = absent,
                    ExcusedCount = excused,
                    TotalRecords = total,
                    AttendanceRate = rate,
                };
            }).ToList();

        var instructorName = $"{instructor.Firstname} {instructor.Lastname}".Trim();

        return new InstructorSessionsReportDto
        {
            InstructorId = instructorId,
            InstructorName = instructorName,
            TotalSessions = sessions.Count,
            Sessions = sessionItems,
        };
    }
}
