using System.Security.Claims;
using attendance_monitoring.Classes;
using attendance_monitoring.Exceptions;
using attendance_monitoring.IRepository;
using attendance_monitoring.IServices;
using attendance_monitoring.Models.DTO.Request;
using attendance_monitoring.Models.DTO.Response;
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
    UserContextService userContextService,
    ILogger<AttendanceService> logger) : IAttendanceService
{
    #region Create Operations

    /// <summary>
    /// Creates a new attendance record manually.
    /// </summary>
    public async Task<AttendanceRecordResponseDto> CreateAttendanceAsync(CreateAttendanceRequest request, ClaimsPrincipal user)
    {
        logger.LogInformation("Creating attendance record for StudentId: {StudentId}, SessionId: {SessionId}",
            request.StudentId, request.SessionId);

        // Verify student exists
        var student = await studentRepository.GetStudentByIdAsync(request.StudentId).ConfigureAwait(false);
        if (student == null)
        {
            logger.LogWarning("Student with ID {StudentId} not found", request.StudentId);
            throw new EntityNotFoundException<int>("Student", request.StudentId);
        }

        // Verify session exists
        var session = await sessionRepository.GetSessionByIdAsync(request.SessionId).ConfigureAwait(false);
        if (session == null)
        {
            logger.LogWarning("Session with ID {SessionId} not found", request.SessionId);
            throw new EntityNotFoundException<int>("Session", request.SessionId);
        }

        // Check if attendance record already exists
        var existingRecord = await attendanceRepository.HasAttendanceRecordAsync(request.StudentId, request.SessionId)
            .ConfigureAwait(false);
        if (existingRecord)
        {
            logger.LogWarning("Attendance record already exists for StudentId: {StudentId}, SessionId: {SessionId}",
                request.StudentId, request.SessionId);
            throw new InvalidOperationException("Attendance record already exists for this student and session");
        }

        // Verify student is enrolled in the session's section/subject
        var isEnrolled = await VerifyStudentEnrollmentAsync(request.StudentId, session).ConfigureAwait(false);
        if (!isEnrolled)
        {
            logger.LogWarning("Student {StudentId} is not enrolled in session {SessionId}",
                request.StudentId, request.SessionId);
            throw new InvalidOperationException("Student is not enrolled in this session's section or subject");
        }

        // Get current user ID for EnteredBy field
        var currentUserId = await userContextService.GetUserIdAsync(user).ConfigureAwait(false);

        // Create attendance record
        var checkInTime = request.CheckInTime ?? DateTime.UtcNow;
        var attendanceRecord = new AttendanceRecord
        {
            StudentId = request.StudentId,
            SessionId = request.SessionId,
            CheckInTime = checkInTime,
            Status = request.Status,
            Notes = request.Notes,
            IsManualEntry = true,
            EnteredBy = currentUserId
        };

        var createdRecord = await attendanceRepository.CreateAsync(attendanceRecord).ConfigureAwait(false);
        await attendanceRepository.SaveChangesAsync().ConfigureAwait(false);

        logger.LogInformation("Successfully created attendance record with ID: {Id}", createdRecord.Id);

        // Reload with navigation properties
        var recordWithNav = await attendanceRepository.GetByIdAsync(createdRecord.Id).ConfigureAwait(false);
        return MapToResponseDto(recordWithNav!);
    }

    /// <summary>
    /// Creates an attendance record from a QR code scan.
    /// </summary>
    public async Task<AttendanceRecordResponseDto> CreateAttendanceFromQrScanAsync(int studentId, int sessionId, int qrCodeId, DateTime checkInTime)
    {
        logger.LogInformation("Creating attendance record from QR scan for StudentId: {StudentId}, SessionId: {SessionId}",
            studentId, sessionId);

        // Check if attendance record already exists
        var existingRecord = await attendanceRepository.HasAttendanceRecordAsync(studentId, sessionId)
            .ConfigureAwait(false);
        if (existingRecord)
        {
            logger.LogWarning("Duplicate QR scan - attendance already exists for StudentId: {StudentId}, SessionId: {SessionId}",
                studentId, sessionId);
            throw new InvalidOperationException("duplicate - Attendance record already exists for this student and session");
        }

        // Get session to determine status
        var session = await sessionRepository.GetSessionByIdAsync(sessionId).ConfigureAwait(false);
        if (session == null)
        {
            throw new EntityNotFoundException<int>("Session", sessionId);
        }

        // Determine attendance status based on check-in time
        var sessionStartTime = session.ActualStartTime ?? DateTime.Today.Add(session.Schedule.TimeIn.ToTimeSpan());
        var status = DetermineAttendanceStatus(checkInTime, sessionStartTime);

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

        var createdRecord = await attendanceRepository.CreateAsync(attendanceRecord).ConfigureAwait(false);
        await attendanceRepository.SaveChangesAsync().ConfigureAwait(false);

        logger.LogInformation("Successfully created attendance record from QR scan with ID: {Id}", createdRecord.Id);

        // Reload with navigation properties
        var recordWithNav = await attendanceRepository.GetByIdAsync(createdRecord.Id).ConfigureAwait(false);
        return MapToResponseDto(recordWithNav!);
    }

    #endregion

    #region Read Operations

    /// <summary>
    /// Retrieves an attendance record by its ID.
    /// </summary>
    public async Task<AttendanceRecordResponseDto?> GetAttendanceByIdAsync(int id, ClaimsPrincipal user)
    {
        logger.LogInformation("Retrieving attendance record with ID: {Id}", id);

        var record = await attendanceRepository.GetByIdAsync(id).ConfigureAwait(false);
        if (record == null)
        {
            logger.LogWarning("Attendance record with ID {Id} not found", id);
            throw new EntityNotFoundException<int>("AttendanceRecord", id);
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
    /// Retrieves all attendance records with filtering and pagination.
    /// </summary>
    public async Task<PagedResult<AttendanceRecordResponseDto>> GetAllAttendanceAsync(AttendanceFilterRequest filter, ClaimsPrincipal user)
    {
        logger.LogInformation("Retrieving attendance records with filters");

        // Apply role-based filtering
        var userRole = user.FindFirst(ClaimTypes.Role)?.Value;
        if (userRole == "Student")
        {
            // Students can only see their own attendance
            var userId = await userContextService.GetUserIdAsync(user).ConfigureAwait(false);
            var student = await studentRepository.GetStudentByUserIdAsync(userId!).ConfigureAwait(false);
            if (student != null)
            {
                filter.StudentId = student.Id;
            }
        }

        // Build filtered query
        var records = await GetFilteredAttendanceRecordsAsync(filter).ConfigureAwait(false);

        // Get total count for pagination
        var totalCount = records.Count;

        // Apply pagination
        var pagedRecords = records
            .Skip((filter.PageNumber - 1) * filter.PageSize)
            .Take(filter.PageSize)
            .ToList();

        return new PagedResult<AttendanceRecordResponseDto>
        {
            Items = pagedRecords.Select(MapToResponseDto).ToList(),
            TotalCount = totalCount,
            PageNumber = filter.PageNumber,
            PageSize = filter.PageSize
        };
    }

    /// <summary>
    /// Retrieves attendance history for a specific student.
    /// </summary>
    public async Task<StudentAttendanceHistoryDto> GetStudentAttendanceHistoryAsync(int studentId, ClaimsPrincipal user)
    {
        logger.LogInformation("Retrieving attendance history for StudentId: {StudentId}", studentId);

        // Verify student exists
        var student = await studentRepository.GetStudentByIdAsync(studentId).ConfigureAwait(false);
        if (student == null)
        {
            logger.LogWarning("Student with ID {StudentId} not found", studentId);
            throw new EntityNotFoundException<int>("Student", studentId);
        }

        // Authorization check
        var userId = await userContextService.GetUserIdAsync(user).ConfigureAwait(false);
        var userRole = user.FindFirst(ClaimTypes.Role)?.Value;

        if (userRole == "Student" && student.UserId != userId)
        {
            logger.LogWarning("Student user not authorized to view other student's attendance");
            throw new UnauthorizedAccessException("You can only view your own attendance history");
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
            StudentId = studentId,
            StudentName = $"{student.Firstname} {student.Lastname}",
            StudentNumber = student.Id.ToString(),
            TotalSessions = totalSessions,
            PresentCount = presentCount,
            LateCount = lateCount,
            AbsentCount = absentCount,
            ExcusedCount = excusedCount,
            AttendancePercentage = attendancePercentage,
            AttendanceRecords = records.Select(MapToResponseDto).ToList()
        };
    }

    /// <summary>
    /// Retrieves attendance information for a specific session.
    /// </summary>
    public async Task<SessionAttendanceDto> GetSessionAttendanceAsync(int sessionId, ClaimsPrincipal user)
    {
        logger.LogInformation("Retrieving session attendance for SessionId: {SessionId}", sessionId);

        // Verify session exists
        var session = await sessionRepository.GetSessionByIdAsync(sessionId).ConfigureAwait(false);
        if (session == null)
        {
            logger.LogWarning("Session with ID {SessionId} not found", sessionId);
            throw new EntityNotFoundException<int>("Session", sessionId);
        }

        // Authorization: Instructors can only view their own session attendance
        var userRole = user.FindFirst(ClaimTypes.Role)?.Value;
        if (userRole == "Instructor")
        {
            var userId = await userContextService.GetUserIdAsync(user).ConfigureAwait(false);
            var instructor = await instructorRepository.GetInstructorByUserIdAsync(userId!).ConfigureAwait(false);
            if (instructor != null && session.Schedule.InstructorId != instructor.Id)
            {
                logger.LogWarning("Instructor not authorized to view session {SessionId}", sessionId);
                throw new UnauthorizedAccessException("You can only view attendance for your own sessions");
            }
        }

        // Get all students enrolled in the section
        var enrolledStudents = await studentEnrollmentRepository.GetSectionEnrollmentsAsync(session.Schedule.SectionId)
            .ConfigureAwait(false);

        // Get attendance records for the session
        var attendanceRecords = await attendanceRepository.GetBySessionIdAsync(sessionId).ConfigureAwait(false);

        // Create student attendance record DTOs
        var studentAttendanceRecords = new List<StudentAttendanceRecordDto>();
        foreach (var enrollment in enrolledStudents)
        {
            var attendanceRecord = attendanceRecords.FirstOrDefault(a => a.StudentId == enrollment.StudentId);
            studentAttendanceRecords.Add(new StudentAttendanceRecordDto
            {
                StudentId = enrollment.Student.Id,
                StudentName = $"{enrollment.Student.Firstname} {enrollment.Student.Lastname}",
                StudentNumber = enrollment.Student.Id.ToString(),
                AttendanceRecordId = attendanceRecord?.Id,
                Status = attendanceRecord?.Status ?? "Absent",
                CheckInTime = attendanceRecord?.CheckInTime,
                IsManualEntry = attendanceRecord?.IsManualEntry ?? false
            });
        }

        // Calculate statistics
        var presentCount = attendanceRecords.Count(r => r.Status == "Present");
        var lateCount = attendanceRecords.Count(r => r.Status == "Late");
        var absentCount = enrolledStudents.Count() - attendanceRecords.Count;

        var attendanceRate = enrolledStudents.Any()
            ? Math.Round((decimal)(presentCount + lateCount) / enrolledStudents.Count() * 100, 2)
            : 0;

        return new SessionAttendanceDto
        {
            SessionId = sessionId,
            SessionDate = session.SessionDate,
            ScheduleId = session.ScheduleId,
            ScheduleTitle = $"{session.Schedule.Subject.Name} - {session.Schedule.Section.Name}",
            SubjectName = session.Schedule.Subject.Name,
            SectionName = session.Schedule.Section.Name,
            TotalEnrolled = enrolledStudents.Count(),
            PresentCount = presentCount,
            LateCount = lateCount,
            AbsentCount = absentCount,
            AttendanceRate = attendanceRate,
            AttendanceRecords = studentAttendanceRecords
        };
    }

    /// <summary>
    /// Retrieves attendance summary statistics.
    /// </summary>
    public async Task<AttendanceSummaryDto> GetAttendanceSummaryAsync(AttendanceFilterRequest filter, ClaimsPrincipal user)
    {
        logger.LogInformation("Retrieving attendance summary");

        // Apply role-based filtering
        var userRole = user.FindFirst(ClaimTypes.Role)?.Value;
        if (userRole == "Student")
        {
            var userId = await userContextService.GetUserIdAsync(user).ConfigureAwait(false);
            var student = await studentRepository.GetStudentByUserIdAsync(userId!).ConfigureAwait(false);
            if (student != null)
            {
                filter.StudentId = student.Id;
            }
        }

        // Get filtered records
        var records = await GetFilteredAttendanceRecordsAsync(filter).ConfigureAwait(false);

        // Calculate statistics
        var totalPresent = records.Count(r => r.Status == "Present");
        var totalLate = records.Count(r => r.Status == "Late");
        var totalAbsent = records.Count(r => r.Status == "Absent");
        var totalExcused = records.Count(r => r.Status == "Excused");
        var totalSessions = records.Count;

        var attendanceRate = totalSessions > 0
            ? Math.Round((decimal)(totalPresent + totalLate) / totalSessions * 100, 2)
            : 0;

        // Calculate average check-in time
        string? averageCheckInTime = null;
        if (records.Any())
        {
            var avgTicks = (long)records.Average(r => r.CheckInTime.TimeOfDay.Ticks);
            var avgTimeSpan = TimeSpan.FromTicks(avgTicks);
            averageCheckInTime = avgTimeSpan.ToString(@"hh\:mm\:ss");
        }

        // Find most frequent status
        var statusGroups = records.GroupBy(r => r.Status)
            .OrderByDescending(g => g.Count())
            .FirstOrDefault();
        var mostFrequentStatus = statusGroups?.Key ?? "Present";

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
    public async Task<AttendanceRecordResponseDto> UpdateAttendanceAsync(int id, UpdateAttendanceRequest request, ClaimsPrincipal user)
    {
        logger.LogInformation("Updating attendance record with ID: {Id}", id);

        var record = await attendanceRepository.GetByIdAsync(id).ConfigureAwait(false);
        if (record == null)
        {
            logger.LogWarning("Attendance record with ID {Id} not found", id);
            throw new EntityNotFoundException<int>("AttendanceRecord", id);
        }

        // Authorization: Only instructors and admins can update attendance
        var userRole = user.FindFirst(ClaimTypes.Role)?.Value;
        if (userRole != "Admin" && userRole != "Instructor")
        {
            logger.LogWarning("User not authorized to update attendance record {Id}", id);
            throw new UnauthorizedAccessException("Only instructors and administrators can update attendance records");
        }

        // For instructors, verify they own the session
        if (userRole == "Instructor")
        {
            var userId = await userContextService.GetUserIdAsync(user).ConfigureAwait(false);
            var instructor = await instructorRepository.GetInstructorByUserIdAsync(userId!).ConfigureAwait(false);
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
        return MapToResponseDto(updatedRecord!);
    }

    /// <summary>
    /// Deletes an attendance record.
    /// </summary>
    public async Task<bool> DeleteAttendanceAsync(int id, ClaimsPrincipal user)
    {
        logger.LogInformation("Deleting attendance record with ID: {Id}", id);

        var record = await attendanceRepository.GetByIdAsync(id).ConfigureAwait(false);
        if (record == null)
        {
            logger.LogWarning("Attendance record with ID {Id} not found", id);
            throw new EntityNotFoundException<int>("AttendanceRecord", id);
        }

        // Authorization: Only admins can delete attendance records
        var userRole = user.FindFirst(ClaimTypes.Role)?.Value;
        if (userRole != "Admin")
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

    #endregion

    #region Utility Methods

    /// <summary>
    /// Checks if a student can mark attendance for a session.
    /// </summary>
    public async Task<bool> CanMarkAttendanceAsync(int studentId, int sessionId)
    {
        // Check if record already exists
        var hasRecord = await attendanceRepository.HasAttendanceRecordAsync(studentId, sessionId).ConfigureAwait(false);
        if (hasRecord)
        {
            return false;
        }

        // Verify session exists and is active
        var session = await sessionRepository.GetSessionByIdAsync(sessionId).ConfigureAwait(false);
        if (session == null || session.Status != "active")
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
        var timeDifference = checkInTime - sessionStartTime;

        if (timeDifference.TotalMinutes <= 0)
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

    private async Task<bool> VerifyStudentEnrollmentAsync(int studentId, Session session)
    {
        var enrollments = await studentEnrollmentRepository.GetStudentEnrollmentsAsync(studentId).ConfigureAwait(false);
        return enrollments.Any(e => e.SectionId == session.Schedule.SectionId);
    }

    private async Task<bool> IsAuthorizedToViewAttendanceAsync(ClaimsPrincipal user, AttendanceRecord record)
    {
        var userRole = user.FindFirst(ClaimTypes.Role)?.Value;

        // Admins can view all
        if (userRole == "Admin")
        {
            return true;
        }

        // Instructors can view their session attendance
        if (userRole == "Instructor")
        {
            var userId = await userContextService.GetUserIdAsync(user).ConfigureAwait(false);
            var instructor = await instructorRepository.GetInstructorByUserIdAsync(userId!).ConfigureAwait(false);
            return instructor != null && record.Session.Schedule.InstructorId == instructor.Id;
        }

        // Students can view their own attendance
        if (userRole == "Student")
        {
            var userId = await userContextService.GetUserIdAsync(user).ConfigureAwait(false);
            var student = await studentRepository.GetStudentByUserIdAsync(userId!).ConfigureAwait(false);
            return student != null && record.StudentId == student.Id;
        }

        return false;
    }

    private async Task<List<AttendanceRecord>> GetFilteredAttendanceRecordsAsync(AttendanceFilterRequest filter)
    {
        List<AttendanceRecord> records;

        // Start with appropriate base query
        if (filter.StudentId.HasValue)
        {
            records = await attendanceRepository.GetByStudentIdAsync(filter.StudentId.Value).ConfigureAwait(false);
        }
        else if (filter.SessionId.HasValue)
        {
            records = await attendanceRepository.GetBySessionIdAsync(filter.SessionId.Value).ConfigureAwait(false);
        }
        else
        {
            records = await attendanceRepository.GetAllAsync(1, int.MaxValue).ConfigureAwait(false);
        }

        // Apply additional filters
        if (filter.ScheduleId.HasValue)
        {
            records = records.Where(r => r.Session.ScheduleId == filter.ScheduleId.Value).ToList();
        }

        if (filter.SectionId.HasValue)
        {
            records = records.Where(r => r.Session.Schedule.SectionId == filter.SectionId.Value).ToList();
        }

        if (filter.SubjectId.HasValue)
        {
            records = records.Where(r => r.Session.Schedule.SubjectId == filter.SubjectId.Value).ToList();
        }

        if (!string.IsNullOrWhiteSpace(filter.Status))
        {
            records = records.Where(r => r.Status == filter.Status).ToList();
        }

        if (filter.StartDate.HasValue)
        {
            records = records.Where(r => r.CheckInTime >= filter.StartDate.Value).ToList();
        }

        if (filter.EndDate.HasValue)
        {
            records = records.Where(r => r.CheckInTime <= filter.EndDate.Value).ToList();
        }

        if (filter.IsManualEntry.HasValue)
        {
            records = records.Where(r => r.IsManualEntry == filter.IsManualEntry.Value).ToList();
        }

        return records;
    }

    private static AttendanceRecordResponseDto MapToResponseDto(AttendanceRecord record)
    {
        return new AttendanceRecordResponseDto
        {
            Id = record.Id,
            StudentId = record.StudentId,
            StudentName = $"{record.Student.Firstname} {record.Student.Lastname}",
            StudentNumber = record.Student.Id.ToString(),
            SessionId = record.SessionId,
            SessionDate = record.Session.SessionDate,
            QrCodeId = record.QrCodeId,
            CheckInTime = record.CheckInTime,
            Status = record.Status,
            Notes = record.Notes,
            IsManualEntry = record.IsManualEntry,
            EnteredBy = record.EnteredBy,
            CreatedAt = record.CreatedAt,
            UpdatedAt = record.UpdatedAt,
            ScheduleId = record.Session.ScheduleId,
            ScheduleTitle = $"{record.Session.Schedule.Subject.Name} - {record.Session.Schedule.Section.Name}",
            SubjectName = record.Session.Schedule.Subject.Name,
            SectionName = record.Session.Schedule.Section.Name,
            RoomName = record.Session.ActualRoom?.Name ?? record.Session.Schedule.Classroom.Name,
            InstructorName = $"{record.Session.Schedule.Instructor.Firstname} {record.Session.Schedule.Instructor.Lastname}"
        };
    }

    #endregion
}
