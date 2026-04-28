using System.Security.Claims;
using attendance_monitoring.Classes;
using attendance_monitoring.Exceptions;
using attendance_monitoring.Extensions;
using attendance_monitoring.IRepository;
using attendance_monitoring.IServices;
using attendance_monitoring.Models.DTO.Request;
using attendance_monitoring.Models.DTO.Response;
using attendance_monitoring.Constants;
using attendance_monitoring.Helpers;
using Microsoft.EntityFrameworkCore;

namespace attendance_monitoring.Services;

/// <summary>
/// Service implementation for attendance-related business logic operations.
/// </summary>
public class AttendanceService(
    IAttendanceRepository attendanceRepository,
    IStudentRepository studentRepository,
    IInstructorRepository instructorRepository,
    ISessionRepository sessionRepository,
    IStudentEnrollmentRepository studentEnrollmentRepository,
    ISectionRepository sectionRepository,
    IUserContextService userContextService,
    ILogger<AttendanceService> logger,
    ConfiguredTimeZoneProvider clock,
    IAutomaticSessionEndService? automaticSessionEndService = null) : IAttendanceService
{
    #region Create Operations

    /// <summary>
    /// Creates a new attendance record manually.
    /// </summary>
    public async Task<AttendanceRecordResponseDto> CreateAttendanceAsync(CreateAttendanceRequest request, ClaimsPrincipal user)
    {
        var student = await ResolveStudentAsync(request.StudentId).ConfigureAwait(false);
        var session = await ResolveSessionAsync(request.SessionId).ConfigureAwait(false);
        var studentId = student.Id;
        var sessionId = session.Id;

        logger.LogInformation("Creating attendance record for StudentId: {StudentId}, SessionId: {SessionId}",
            studentId, sessionId);

        // Verify student is enrolled in the session's section/subject
        var isEnrolled = await VerifyStudentEnrollmentAsync(studentId, session).ConfigureAwait(false);
        if (!isEnrolled)
        {
            logger.LogWarning("Student {StudentId} is not enrolled in session {SessionId}",
                studentId, sessionId);
            throw new InvalidOperationException("Student is not enrolled in this session's section or subject");
        }

        // Get current user ID for EnteredBy field
        var currentUserId = await userContextService.GetUserIdAsync(user).ConfigureAwait(false);

        // Create attendance record
        var checkInTime = request.CheckInTime ?? clock.GetUtcNow().UtcDateTime;
        var attendanceRecord = new AttendanceRecord
        {
            StudentId = studentId,
            SessionId = sessionId,
            CheckInTime = checkInTime,
            Status = request.Status,
            Notes = request.Notes,
            IsManualEntry = true,
            EnteredBy = currentUserId
        };

        // Create attendance record - rely on database composite unique index to prevent duplicates
        try
        {
            var createdRecord = await attendanceRepository.CreateAsync(attendanceRecord).ConfigureAwait(false);
            await attendanceRepository.SaveChangesAsync().ConfigureAwait(false);

            logger.LogInformation("Successfully created attendance record with ID: {Id}", createdRecord.Id);

            // Reload with navigation properties
            var recordWithNav = await attendanceRepository.GetByIdAsync(createdRecord.Id).ConfigureAwait(false);
            if (recordWithNav == null)
            {
                throw new EntityNotFoundException<Guid>("AttendanceRecord", createdRecord.Id, $"Attendance record with ID {createdRecord.Id} was not found after creation");
            }
            return MapToResponseDto(recordWithNav);
        }
        catch (DbUpdateException ex) when (ex.InnerException?.Message.Contains("UNIQUE constraint") == true ||
                                            ex.InnerException?.Message.Contains("duplicate key") == true ||
                                            ex.InnerException?.Message.Contains("IX_AttendanceRecords_StudentId_SessionId") == true)
        {
            logger.LogWarning(ex, "Duplicate attendance - returning existing record for StudentId: {StudentId}, SessionId: {SessionId}",
                studentId, sessionId);

            var existingRecord = await attendanceRepository
                .GetBySessionAndStudentAsync(sessionId, studentId)
                .ConfigureAwait(false);

            if (existingRecord == null)
            {
                logger.LogError("Duplicate attendance detected but existing record could not be loaded for StudentId: {StudentId}, SessionId: {SessionId}",
                    studentId, sessionId);
                throw;
            }

            return MapToResponseDto(existingRecord, isIdempotentRetry: true);
        }
    }

    /// <summary>
    /// Creates an attendance record from a QR code scan.
    /// </summary>
    public async Task<AttendanceRecordResponseDto> CreateAttendanceFromQrScanAsync(Guid studentId, Guid sessionId, Guid qrCodeId, DateTime checkInTime)
    {
        logger.LogInformation("Creating attendance record from QR scan for StudentId: {StudentId}, SessionId: {SessionId}",
            studentId, sessionId);

        // Get session to determine status
        var session = await sessionRepository.GetSessionByIdAsync(sessionId).ConfigureAwait(false);
        if (session == null)
        {
            throw new EntityNotFoundException<Guid>("Session", sessionId);
        }

        // Determine attendance status based on check-in time
        DateTime sessionStartTime;
        if (session.ActualStartTime.HasValue)
        {
            sessionStartTime = DateTime.SpecifyKind(session.ActualStartTime.Value, DateTimeKind.Utc);
        }
        else
        {
            var localStartTime = session.SessionDate.Date.Add(session.Schedule.TimeIn.ToTimeSpan());
            sessionStartTime = TimeZoneInfo.ConvertTimeToUtc(localStartTime, clock.TimeZone);
        }

        var lateCutoffMinutes = 15;
        if (session.AttendanceCutOff.HasValue)
        {
            var attendanceCutOffUtc = DateTime.SpecifyKind(session.AttendanceCutOff.Value, DateTimeKind.Utc);
            lateCutoffMinutes = Math.Max(
                0,
                (int)Math.Round(
                    (attendanceCutOffUtc - sessionStartTime).TotalMinutes,
                    MidpointRounding.AwayFromZero));
        }

        logger.LogInformation(
            "Attendance status calculation from QR scan: CheckInTime={CheckInTime:O}, SessionStartTime={SessionStartTime:O}, TimeDifference={TimeDifference}min, LateCutoff={LateCutoff}min",
            checkInTime, sessionStartTime, (checkInTime - sessionStartTime).TotalMinutes, lateCutoffMinutes);

        var status = DetermineAttendanceStatus(checkInTime, sessionStartTime, lateCutoffMinutes);

        // Create attendance record
        var attendanceRecord = new AttendanceRecord
        {
            StudentId = studentId,
            SessionId = sessionId,
            QrCodeId = qrCodeId,
            CheckInTime = checkInTime,
            Status = status,
            IsManualEntry = false
        };

        // Create attendance record - rely on database composite unique index to prevent duplicates
        try
        {
            var createdRecord = await attendanceRepository.CreateAsync(attendanceRecord).ConfigureAwait(false);
            await attendanceRepository.SaveChangesAsync().ConfigureAwait(false);

            logger.LogInformation("Successfully created attendance record from QR scan with ID: {Id}", createdRecord.Id);

            // Reload with navigation properties
            var recordWithNav = await attendanceRepository.GetByIdAsync(createdRecord.Id).ConfigureAwait(false);
            if (recordWithNav == null)
            {
                throw new EntityNotFoundException<Guid>("AttendanceRecord", createdRecord.Id, $"Attendance record with ID {createdRecord.Id} was not found after creation");
            }
            return MapToResponseDto(recordWithNav);
        }
        catch (DbUpdateException ex) when (ex.InnerException?.Message.Contains("UNIQUE constraint") == true ||
                                            ex.InnerException?.Message.Contains("duplicate key") == true ||
                                            ex.InnerException?.Message.Contains("IX_AttendanceRecords_StudentId_SessionId") == true)
        {
            logger.LogWarning(ex, "Duplicate QR scan - race condition detected for StudentId: {StudentId}, SessionId: {SessionId}",
                studentId, sessionId);
            throw new InvalidOperationException("duplicate - Attendance record already exists for this student and session");
        }
    }

    #endregion

    #region Read Operations

    /// <summary>
    /// Retrieves an attendance record by its ID.
    /// </summary>
    public async Task<AttendanceRecordResponseDto?> GetAttendanceByIdAsync(Guid id, ClaimsPrincipal user)
    {
        logger.LogInformation("Retrieving attendance record with ID: {Id}", id);

        var record = await attendanceRepository.GetByIdAsync(id).ConfigureAwait(false);
        if (record == null)
        {
            logger.LogWarning("Attendance record with ID {Id} not found", id);
            throw new EntityNotFoundException<Guid>("AttendanceRecord", id);
        }

        // Authorization: Students can only view their own records, Instructors/Admins can view all
        if (!await IsAuthorizedToViewAttendanceAsync(user, record).ConfigureAwait(false))
        {
            logger.LogWarning("User not authorized to view attendance record {Id}", id);
            throw new UnauthorizedAccessException("You are not authorized to view this attendance record");
        }

        return MapToResponseDto(record);
    }

    /// <summary>
    /// Retrieves an attendance record by its UUID.
    /// </summary>
    public async Task<AttendanceRecordResponseDto?> GetAttendanceByUuidAsync(Guid id, ClaimsPrincipal user)
    {
        logger.LogInformation("Retrieving attendance record with UUID: {AttendanceUuid}", id);

        var record = await attendanceRepository.GetAttendanceByUuidAsync(id).ConfigureAwait(false);
        if (record == null)
        {
            logger.LogWarning("Attendance record with UUID {AttendanceUuid} not found", id);
            throw new EntityNotFoundException<Guid>("AttendanceRecord", id);
        }

        if (!await IsAuthorizedToViewAttendanceAsync(user, record).ConfigureAwait(false))
        {
            logger.LogWarning("User not authorized to view attendance record UUID {AttendanceUuid}", id);
            throw new UnauthorizedAccessException("You are not authorized to view this attendance record");
        }

        return MapToResponseDto(record);
    }

    /// <summary>
    /// Retrieves all attendance records with filtering and pagination.
    /// </summary>
    public async Task<PagedResult<AttendanceRecordResponseDto>> GetAllAttendanceAsync(AttendanceFilterRequest filter, ClaimsPrincipal user)
    {
        logger.LogInformation("Retrieving attendance records with filters");

        // Apply role-based filtering
        var userRole = user.FindFirst(ClaimTypes.Role)?.Value;
        if (userRole == RoleConstants.Student)
        {
            // Students can only see their own attendance - fail-secure pattern
            var userId = await userContextService.GetUserIdAsync(user).ConfigureAwait(false);
            if (userId == null)
            {
                logger.LogWarning("Unable to retrieve user ID for student role");
                throw new UnauthorizedAccessException("Unable to verify user identity");
            }

            var student = await studentRepository.GetStudentByUserIdAsync(userId).ConfigureAwait(false);
            if (student == null)
            {
                logger.LogWarning("Student profile not found for user ID: {UserId}", userId);
                throw new EntityNotFoundException<string>("Student", userId,
                    "Student profile not found for authenticated user");
            }

            filter.StudentId = student.Id;
        }

        // Build filtered query with pagination applied at database level
        var (records, totalCount) = await GetFilteredAttendanceRecordsAsync(filter).ConfigureAwait(false);

        return new PagedResult<AttendanceRecordResponseDto>
        {
            Items = records.Select(record => MapToResponseDto(record)).ToList(),
            TotalCount = totalCount,
            PageNumber = filter.PageNumber,
            PageSize = filter.PageSize
        };
    }

    /// <summary>
    /// Retrieves attendance history for a specific student.
    /// </summary>
    public async Task<StudentAttendanceHistoryDto> GetStudentAttendanceHistoryAsync(Guid studentId, ClaimsPrincipal user)
    {
        logger.LogInformation("Retrieving attendance history for StudentId: {StudentId}", studentId);

        // Verify student exists
        var student = await studentRepository.GetStudentByIdAsync(studentId).ConfigureAwait(false);
        if (student == null)
        {
            logger.LogWarning("Student with ID {StudentId} not found", studentId);
            throw new EntityNotFoundException<Guid>("Student", studentId);
        }

        // Authorization check - fail-secure pattern
        var userRole = user.FindFirst(ClaimTypes.Role)?.Value;
        if (userRole == RoleConstants.Student)
        {
            var userId = await userContextService.GetUserIdAsync(user).ConfigureAwait(false);
            if (userId == null)
            {
                logger.LogWarning("Unable to retrieve user ID for student role in attendance history");
                throw new UnauthorizedAccessException("Unable to verify user identity");
            }

            if (student.UserId == null)
            {
                logger.LogWarning("Student profile has no UserId assigned for StudentId: {StudentId}", studentId);
                throw new UnauthorizedAccessException("Student profile is not properly configured");
            }

            if (student.UserId != userId)
            {
                logger.LogWarning("Student user {UserId} not authorized to view student {StudentId} attendance (owned by {StudentUserId})",
                    userId, studentId, student.UserId);
                throw new UnauthorizedAccessException("You can only view your own attendance history");
            }
        }

        // Get all attendance records for student
        var records = await attendanceRepository.GetByStudentIdAsync(studentId).ConfigureAwait(false);

        // Calculate statistics
        var presentCount = records.Count(r => r.Status == "Present");
        var lateCount = records.Count(r => r.Status == "Late");
        var absentCount = records.Count(r => r.Status == "Absent");
        var excusedCount = records.Count(r => r.Status == "Excused");
        var totalSessions = records.Count;

        var attendancePercentage = totalSessions > 0
            ? Math.Round((decimal)(presentCount + lateCount) / totalSessions * 100, 2)
            : 0;

        return new StudentAttendanceHistoryDto
        {
            StudentId = student.Id,
            StudentName = $"{student.Firstname} {student.Lastname}",
            StudentNumber = student.Usn ?? string.Empty,
            TotalSessions = totalSessions,
            PresentCount = presentCount,
            LateCount = lateCount,
            AbsentCount = absentCount,
            ExcusedCount = excusedCount,
            AttendancePercentage = attendancePercentage,
            AttendanceRecords = records.Select(record => MapToResponseDto(record)).ToList()
        };
    }

    public async Task<StudentAttendanceHistoryDto> GetStudentAttendanceHistoryByUuidAsync(Guid studentUuid, ClaimsPrincipal user)
    {
        var student = await studentRepository.GetStudentByUuidAsync(studentUuid).ConfigureAwait(false);
        if (student == null)
        {
            throw new EntityNotFoundException<Guid>("Student", studentUuid);
        }

        return await GetStudentAttendanceHistoryAsync(student.Id, user).ConfigureAwait(false);
    }

    /// <summary>
    /// Retrieves attendance information for a specific session.
    /// </summary>
    public async Task<SessionAttendanceDto> GetSessionAttendanceAsync(Guid sessionId, ClaimsPrincipal user)
    {
        logger.LogInformation("Retrieving session attendance for SessionId: {SessionId}", sessionId);

        // Verify session exists
        var session = await sessionRepository.GetSessionByIdAsync(sessionId).ConfigureAwait(false);
        if (session == null)
        {
            logger.LogWarning("Session with ID {SessionId} not found", sessionId);
            throw new EntityNotFoundException<Guid>("Session", sessionId);
        }

        // Authorization: Instructors can only view their own session attendance
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
                throw new EntityNotFoundException<string>("Instructor", userId, "Instructor profile not found for authenticated user");
            }

            if (session.Schedule == null)
            {
                throw new InvalidOperationException("Session schedule information is not available");
            }

            if (session.Schedule.InstructorId != instructor.Id)
            {
                logger.LogWarning("Instructor {InstructorId} not authorized to view session {SessionId}", instructor.Id, sessionId);
                throw new UnauthorizedAccessException("You can only view attendance for your own sessions");
            }
        }

        // Get all students in the section: regular students (via Student.SectionId) + enrolled students (via StudentEnrollment)
        var regularStudents = await sectionRepository.GetActiveStudentsBySectionIdAsync(session.Schedule.SectionId)
            .ConfigureAwait(false);

        var enrolledStudents = await studentEnrollmentRepository.GetSectionEnrollmentsAsync(session.Schedule.SectionId)
            .ConfigureAwait(false);

        // Merge both sources, avoiding duplicates (prefer StudentEnrollment data when available)
        var allStudentsDict = new Dictionary<Guid, Student>();

        foreach (var student in regularStudents)
        {
            allStudentsDict[student.Id] = student;
        }

        foreach (var enrollment in enrolledStudents)
        {
            if (enrollment.Student != null)
            {
                allStudentsDict[enrollment.Student.Id] = enrollment.Student;
            }
        }

        var allStudents = allStudentsDict.Values.ToList();

        // Get attendance records for the session
        var attendanceRecords = await attendanceRepository.GetBySessionIdAsync(sessionId).ConfigureAwait(false);

        // Create student attendance record DTOs
        var studentAttendanceRecords = new List<StudentAttendanceRecordDto>();
        foreach (var student in allStudents)
        {
            var attendanceRecord = attendanceRecords.FirstOrDefault(a => a.StudentId == student.Id);
            studentAttendanceRecords.Add(new StudentAttendanceRecordDto
            {
                StudentId = student.Id,
                StudentName = $"{student.Firstname} {student.Lastname}",
                StudentNumber = student.Usn ?? string.Empty,
                AttendanceRecordId = attendanceRecord?.Id,
                Status = attendanceRecord?.Status ?? "Absent",
                CheckInTime = attendanceRecord?.CheckInTime,
                IsManualEntry = attendanceRecord?.IsManualEntry ?? false
            });
        }

        // Calculate statistics
        var presentCount = attendanceRecords.Count(r => r.Status == "Present");
        var lateCount = attendanceRecords.Count(r => r.Status == "Late");
        var absentCount = allStudents.Count - attendanceRecords.Count;

        var attendanceRate = allStudents.Any()
            ? Math.Round((decimal)(presentCount + lateCount) / allStudents.Count * 100, 2)
            : 0;

        return new SessionAttendanceDto
        {
            SessionId = session.Id,
            SessionDate = session.SessionDate,
            ScheduleId = session.Schedule.Id,
            ScheduleTitle = $"{session.Schedule.Subject.Name} - {session.Schedule.Section.Name}",
            SubjectName = session.Schedule.Subject.Name,
            SectionName = session.Schedule.Section.Name,
            TotalEnrolled = allStudents.Count,
            PresentCount = presentCount,
            LateCount = lateCount,
            AbsentCount = absentCount,
            AttendanceRate = attendanceRate,
            AttendanceRecords = studentAttendanceRecords
        };
    }

    public async Task<SessionAttendanceDto> GetSessionAttendanceByUuidAsync(Guid sessionUuid, ClaimsPrincipal user)
    {
        var session = await sessionRepository.GetSessionByUuidAsync(sessionUuid).ConfigureAwait(false);
        if (session == null)
        {
            throw new EntityNotFoundException<Guid>("Session", sessionUuid);
        }

        return await GetSessionAttendanceAsync(session.Id, user).ConfigureAwait(false);
    }

    /// <summary>
    /// Retrieves attendance summary statistics.
    /// </summary>
    public async Task<AttendanceSummaryDto> GetAttendanceSummaryAsync(AttendanceFilterRequest filter, ClaimsPrincipal user)
    {
        logger.LogInformation("Retrieving attendance summary");

        // Apply role-based filtering
        var userRole = user.FindFirst(ClaimTypes.Role)?.Value;
        if (userRole == RoleConstants.Student)
        {
            // Students can only see their own attendance - fail-secure pattern
            var userId = await userContextService.GetUserIdAsync(user).ConfigureAwait(false);
            if (userId == null)
            {
                logger.LogWarning("Unable to retrieve user ID for student role in summary");
                throw new UnauthorizedAccessException("Unable to verify user identity");
            }

            var student = await studentRepository.GetStudentByUserIdAsync(userId).ConfigureAwait(false);
            if (student == null)
            {
                logger.LogWarning("Student profile not found for user ID: {UserId} in summary", userId);
                throw new EntityNotFoundException<string>("Student", userId,
                    "Student profile not found for authenticated user");
            }

            filter.StudentId = student.Id;
        }

        // Get statistics from database (optimized - no records loaded into memory)
        var (totalSessions, totalPresent, totalLate, totalAbsent, totalExcused, avgCheckInTicks) =
            await attendanceRepository.GetStatisticsAsync(
                studentId: filter.StudentId,
                sessionId: filter.SessionId,
                scheduleId: filter.ScheduleId,
                sectionId: filter.SectionId,
                subjectId: filter.SubjectId,
                status: filter.Status,
                startDate: filter.StartDate,
                endDate: filter.EndDate,
                isManualEntry: filter.IsManualEntry
            ).ConfigureAwait(false);

        var attendanceRate = totalSessions > 0
            ? Math.Round((decimal)(totalPresent + totalLate) / totalSessions * 100, 2)
            : 0;

        // Calculate average check-in time
        string? averageCheckInTime = null;
        if (totalSessions > 0)
        {
            var avgTimeSpan = TimeSpan.FromTicks(avgCheckInTicks);
            averageCheckInTime = avgTimeSpan.ToString(@"hh\:mm\:ss");
        }

        // Determine most frequent status from counts
        var statusCounts = new Dictionary<string, int>
        {
            { "Present", totalPresent },
            { "Late", totalLate },
            { "Absent", totalAbsent },
            { "Excused", totalExcused }
        };

        // Mashinay but better
        var mostFrequentStatus = statusCounts.Any(kvp => kvp.Value > 0)
            ? statusCounts.OrderByDescending(kvp => kvp.Value).First().Key
            : "Present"; // Safe default value

        return new AttendanceSummaryDto
        {
            TotalSessions = totalSessions,
            TotalPresent = totalPresent,
            TotalLate = totalLate,
            TotalAbsent = totalAbsent,
            TotalExcused = totalExcused,
            AttendanceRate = attendanceRate,
            AverageCheckInTime = averageCheckInTime,
            MostFrequentStatus = mostFrequentStatus
        };
    }

    #endregion

    #region Update and Delete Operations

    /// <summary>
    /// Updates an existing attendance record.
    /// </summary>
    public async Task<AttendanceRecordResponseDto> UpdateAttendanceAsync(Guid id, UpdateAttendanceRequest request, ClaimsPrincipal user)
    {
        logger.LogInformation("Updating attendance record with ID: {Id}", id);

        var record = await attendanceRepository.GetByIdTrackedAsync(id).ConfigureAwait(false);
        if (record == null)
        {
            logger.LogWarning("Attendance record with ID {Id} not found", id);
            throw new EntityNotFoundException<Guid>("AttendanceRecord", id);
        }

        // Authorization: Only instructors and admins can update attendance
        var userRole = user.FindFirst(ClaimTypes.Role)?.Value;
        if (userRole != RoleConstants.Admin && userRole != RoleConstants.Instructor)
        {
            logger.LogWarning("User not authorized to update attendance record {Id}", id);
            throw new UnauthorizedAccessException("Only instructors and administrators can update attendance records");
        }

        // For instructors, verify they own the session
        if (userRole == RoleConstants.Instructor)
        {
            var userId = await userContextService.GetUserIdAsync(user).ConfigureAwait(false);
            if (userId == null)
            {
                logger.LogWarning("Unable to retrieve user ID for instructor");
                throw new UnauthorizedAccessException("Unable to verify instructor identity");
            }

            var instructor = await instructorRepository.GetInstructorByUserIdAsync(userId).ConfigureAwait(false);
            if (record.Session?.Schedule == null)
            {
                throw new InvalidOperationException("Session schedule information is not available");
            }

            if (instructor == null || record.Session.Schedule.InstructorId != instructor.Id)
            {
                logger.LogWarning("Instructor not authorized to update attendance record {Id}", id);
                throw new UnauthorizedAccessException("You can only update attendance for your own sessions");
            }
        }

        // Update fields
        if (!string.IsNullOrWhiteSpace(request.Status))
        {
            record.Status = request.Status;
        }

        if (request.Notes != null)
        {
            record.Notes = request.Notes;
        }

        await attendanceRepository.UpdateAsync(record).ConfigureAwait(false);
        await attendanceRepository.SaveChangesAsync().ConfigureAwait(false);

        logger.LogInformation("Successfully updated attendance record with ID: {Id}", id);

        // Reload with navigation properties
        var updatedRecord = await attendanceRepository.GetByIdAsync(id).ConfigureAwait(false);
        if (updatedRecord == null)
        {
            throw new EntityNotFoundException<Guid>("AttendanceRecord", id, $"Attendance record with ID {id} was not found after update");
        }
        return MapToResponseDto(updatedRecord);
    }

    /// <summary>
    /// Updates an existing attendance record by its UUID.
    /// </summary>
    public async Task<AttendanceRecordResponseDto> UpdateAttendanceByUuidAsync(Guid id, UpdateAttendanceRequest request, ClaimsPrincipal user)
    {
        logger.LogInformation("Updating attendance record with UUID: {AttendanceUuid}", id);

        var record = await attendanceRepository.GetAttendanceByUuidAsync(id).ConfigureAwait(false);
        if (record == null)
        {
            logger.LogWarning("Attendance record with UUID {AttendanceUuid} not found", id);
            throw new EntityNotFoundException<Guid>("AttendanceRecord", id);
        }

        return await UpdateAttendanceAsync(record.Id, request, user).ConfigureAwait(false);
    }

    /// <summary>
    /// Deletes an attendance record.
    /// </summary>
    public async Task<bool> DeleteAttendanceAsync(Guid id, ClaimsPrincipal user)
    {
        logger.LogInformation("Deleting attendance record with ID: {Id}", id);

        var record = await attendanceRepository.GetByIdAsync(id).ConfigureAwait(false);
        if (record == null)
        {
            logger.LogWarning("Attendance record with ID {Id} not found", id);
            throw new EntityNotFoundException<Guid>("AttendanceRecord", id);
        }

        // Authorization: Only admins can delete attendance records
        var userRole = user.FindFirst(ClaimTypes.Role)?.Value;
        if (userRole != RoleConstants.Admin)
        {
            logger.LogWarning("User not authorized to delete attendance record {Id}", id);
            throw new UnauthorizedAccessException("Only administrators can delete attendance records");
        }

        var deleted = await attendanceRepository.DeleteAsync(id).ConfigureAwait(false);
        if (deleted)
        {
            await attendanceRepository.SaveChangesAsync().ConfigureAwait(false);
            logger.LogInformation("Successfully deleted attendance record with ID: {Id}", id);
        }

        return deleted;
    }

    /// <summary>
    /// Deletes an attendance record by its UUID.
    /// </summary>
    public async Task<bool> DeleteAttendanceByUuidAsync(Guid id, ClaimsPrincipal user)
    {
        logger.LogInformation("Deleting attendance record with UUID: {AttendanceUuid}", id);

        var record = await attendanceRepository.GetAttendanceByUuidAsync(id).ConfigureAwait(false);
        if (record == null)
        {
            logger.LogWarning("Attendance record with UUID {AttendanceUuid} not found", id);
            throw new EntityNotFoundException<Guid>("AttendanceRecord", id);
        }

        return await DeleteAttendanceAsync(record.Id, user).ConfigureAwait(false);
    }

    #endregion

    #region Utility Methods

    /// <summary>
    /// Checks if a student can mark attendance for a session.
    /// </summary>
    public async Task<bool> CanMarkAttendanceAsync(Guid studentId, Guid sessionId)
    {
        // Check if record already exists
        var hasRecord = await attendanceRepository.HasAttendanceRecordAsync(studentId, sessionId).ConfigureAwait(false);
        if (hasRecord)
        {
            return false;
        }

        // Verify session exists and is active
        var session = await sessionRepository.GetSessionByIdAsync(sessionId).ConfigureAwait(false);
        if (session != null && automaticSessionEndService != null)
        {
            session = await automaticSessionEndService.AutoEndIfExpiredAsync(session).ConfigureAwait(false);
        }
        if (session == null || session.Status != SessionStatusConstants.Active)
        {
            return false;
        }

        // Verify student enrollment
        return await VerifyStudentEnrollmentAsync(studentId, session).ConfigureAwait(false);
    }

    /// <summary>
    /// Determines the attendance status based on check-in time.
    /// </summary>
    public string DetermineAttendanceStatus(DateTime checkInTime, DateTime sessionStartTime, int lateCutoffMinutes = 15)
    {
        const int GracePeriodMinutes = 1;
        var timeDifference = checkInTime - sessionStartTime;

        if (timeDifference.TotalMinutes <= GracePeriodMinutes)
        {
            return "Present";
        }

        if (timeDifference.TotalMinutes <= lateCutoffMinutes)
        {
            return "Late";
        }

        return "Late"; // Still mark as Late even if very late, instructor can manually change to Absent
    }

    #endregion

    #region Private Helper Methods

    private async Task<bool> VerifyStudentEnrollmentAsync(Guid studentId, Session session)
    {
        if (session.Schedule == null)
        {
            throw new InvalidOperationException("Session schedule information is not available");
        }

        var enrollments = await studentEnrollmentRepository.GetStudentEnrollmentsAsync(studentId).ConfigureAwait(false);
        return enrollments.Any(e => e.SectionId == session.Schedule.SectionId);
    }

    private async Task<Student> ResolveStudentAsync(Guid? studentId)
    {
        if (!studentId.HasValue || studentId.Value == Guid.Empty)
        {
            throw new ValidationException("StudentId is required.");
        }

        var student = await studentRepository.GetStudentByUuidAsync(studentId.Value).ConfigureAwait(false);
        if (student == null)
        {
            throw new EntityNotFoundException<Guid>("Student", studentId.Value);
        }

        return student;
    }

    private async Task<Session> ResolveSessionAsync(Guid? sessionId)
    {
        if (!sessionId.HasValue || sessionId.Value == Guid.Empty)
        {
            throw new ValidationException("SessionId is required.");
        }

        var session = await sessionRepository.GetSessionByUuidAsync(sessionId.Value).ConfigureAwait(false);
        if (session == null)
        {
            throw new EntityNotFoundException<Guid>("Session", sessionId.Value);
        }

        var wasActive = session.Status == SessionStatusConstants.Active;
        if (automaticSessionEndService != null)
        {
            session = await automaticSessionEndService.AutoEndIfExpiredAsync(session).ConfigureAwait(false);
        }

        if (wasActive && session.Status != SessionStatusConstants.Active)
        {
            throw new ValidationException("Cannot submit attendance for a session that is not active.");
        }

        return session;
    }

    private async Task<bool> IsAuthorizedToViewAttendanceAsync(ClaimsPrincipal user, AttendanceRecord record)
    {
        var userRole = user.FindFirst(ClaimTypes.Role)?.Value;

        // Admins can view all
        if (userRole == RoleConstants.Admin)
        {
            return true;
        }

        // Instructors can view their session attendance
        if (userRole == RoleConstants.Instructor)
        {
            var userId = await userContextService.GetUserIdAsync(user).ConfigureAwait(false);
            if (userId == null)
            {
                return false;
            }

            var instructor = await instructorRepository.GetInstructorByUserIdAsync(userId).ConfigureAwait(false);
            if (instructor == null || record.Session?.Schedule == null)
            {
                return false;
            }

            return record.Session.Schedule.InstructorId == instructor.Id;
        }

        // Students can view their own attendance - fail-secure pattern
        if (userRole == RoleConstants.Student)
        {
            var userId = await userContextService.GetUserIdAsync(user).ConfigureAwait(false);
            if (userId == null)
            {
                logger.LogWarning("Unable to retrieve user ID for student role in view authorization");
                throw new UnauthorizedAccessException("Unable to verify user identity");
            }

            var student = await studentRepository.GetStudentByUserIdAsync(userId).ConfigureAwait(false);
            if (student == null)
            {
                logger.LogWarning("Student profile not found for user ID: {UserId} in view authorization", userId);
                throw new EntityNotFoundException<string>("Student", userId,
                    "Student profile not found for authenticated user");
            }

            return record.StudentId == student.Id;
        }

        return false;
    }

    /// <summary>
    /// Retrieves filtered attendance records using optimized database-level filtering.
    /// All filters are applied at the database level before loading data.
    /// </summary>
    private async Task<(List<AttendanceRecord> Records, int TotalCount)> GetFilteredAttendanceRecordsAsync(AttendanceFilterRequest filter)
    {
        return await attendanceRepository.GetFilteredAsync(
            studentId: filter.StudentId,
            sessionId: filter.SessionId,
            scheduleId: filter.ScheduleId,
            sectionId: filter.SectionId,
            subjectId: filter.SubjectId,
            status: filter.Status,
            startDate: filter.StartDate,
            endDate: filter.EndDate,
            isManualEntry: filter.IsManualEntry,
            pageNumber: filter.PageNumber,
            pageSize: filter.PageSize
        ).ConfigureAwait(false);
    }

    private static AttendanceRecordResponseDto MapToResponseDto(AttendanceRecord record, bool isIdempotentRetry = false)
    {
        // Validate required navigation properties
        if (record.Student == null)
        {
            throw new InvalidOperationException("Student navigation property is not loaded");
        }

        if (record.Session == null)
        {
            throw new InvalidOperationException("Session navigation property is not loaded");
        }

        if (record.Session.Schedule == null)
        {
            throw new InvalidOperationException("Session.Schedule navigation property is not loaded");
        }

        if (record.Session.Schedule.Subject == null)
        {
            throw new InvalidOperationException("Session.Schedule.Subject navigation property is not loaded");
        }

        if (record.Session.Schedule.Section == null)
        {
            throw new InvalidOperationException("Session.Schedule.Section navigation property is not loaded");
        }

        if (record.Session.Schedule.Classroom == null)
        {
            throw new InvalidOperationException("Session.Schedule.Classroom navigation property is not loaded");
        }

        if (record.Session.Schedule.Instructor == null)
        {
            throw new InvalidOperationException("Session.Schedule.Instructor navigation property is not loaded");
        }

        var response = new AttendanceRecordResponseDto
        {
            Id = record.Id,
            StudentId = record.Student.Id,
            StudentName = $"{record.Student.Firstname} {record.Student.Lastname}",
            StudentNumber = record.Student.Usn ?? string.Empty,
            SessionId = record.Session.Id,
            SessionDate = record.Session.SessionDate,
            QrCodeId = record.QrCode?.Id,
            CheckInTime = record.CheckInTime,
            Status = record.Status,
            Notes = record.Notes,
            IsManualEntry = record.IsManualEntry,
            EnteredBy = record.EnteredBy,
            CreatedAt = record.CreatedAt,
            UpdatedAt = record.UpdatedAt,
            ScheduleId = record.Session.Schedule.Id,
            ScheduleTitle = $"{record.Session.Schedule.Subject.Name} - {record.Session.Schedule.Section.Name}",
            SubjectName = record.Session.Schedule.Subject.Name,
            SectionName = record.Session.Schedule.Section.Name,
            RoomName = record.Session.ActualRoom?.Name ?? record.Session.Schedule.Classroom.Name,
            InstructorName = $"{record.Session.Schedule.Instructor.Firstname} {record.Session.Schedule.Instructor.Lastname}"
        };

        return isIdempotentRetry
            ? new IdempotentAttendanceRetryResponseDto(response)
            : response;
    }

    private sealed class IdempotentAttendanceRetryResponseDto : AttendanceRecordResponseDto, IIdempotentAttendanceRetryResult
    {
        public IdempotentAttendanceRetryResponseDto(AttendanceRecordResponseDto source)
        {
            Id = source.Id;
            StudentId = source.StudentId;
            StudentName = source.StudentName;
            StudentNumber = source.StudentNumber;
            SessionId = source.SessionId;
            SessionDate = source.SessionDate;
            QrCodeId = source.QrCodeId;
            CheckInTime = source.CheckInTime;
            Status = source.Status;
            Notes = source.Notes;
            IsManualEntry = source.IsManualEntry;
            EnteredBy = source.EnteredBy;
            CreatedAt = source.CreatedAt;
            UpdatedAt = source.UpdatedAt;
            ScheduleId = source.ScheduleId;
            ScheduleTitle = source.ScheduleTitle;
            SubjectName = source.SubjectName;
            SectionName = source.SectionName;
            RoomName = source.RoomName;
            InstructorName = source.InstructorName;
        }
    }

    #endregion
}
