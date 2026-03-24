using System.Security.Claims;
using attendance_monitoring.Classes;
using attendance_monitoring.Constants;
using attendance_monitoring.Data;
using attendance_monitoring.Exceptions;
using attendance_monitoring.IRepository;
using attendance_monitoring.IServices;
using attendance_monitoring.Models.DTO.Request;
using attendance_monitoring.Models.DTO.Response;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.EntityFrameworkCore;
using System.Security.Cryptography;
using System.Text;
using ValidationException = attendance_monitoring.Exceptions.ValidationException;

namespace attendance_monitoring.Services;

/// <summary>
/// Service implementation for fingerprint biometric authentication and attendance operations.
/// </summary>
public class FingerprintService(
    IFingerprintRepository fingerprintRepository,
    IStudentRepository studentRepository,
    ISessionRepository sessionRepository,
    IScheduleRepository scheduleRepository,
    IStudentEnrollmentRepository studentEnrollmentRepository,
    IAttendanceRepository attendanceRepository,
    IAttendanceService attendanceService,
    INotificationService notificationService,
    ApplicationDbContext context,
    UserContextService userContextService,
    IConfiguration configuration,
    IDataProtectionProvider dataProtectionProvider,
    ILogger<FingerprintService> logger) : IFingerprintService
{
    private readonly IAttendanceService _attendanceService = attendanceService;
    private const string PendingStatus = "Pending";
    private const string InProgressStatus = "InProgress";
    private const string CompletedStatus = "Completed";
    private const string FailedStatus = "Failed";
    private const string ExpiredStatus = "Expired";
    private static readonly TimeSpan EnrollmentLifetime = TimeSpan.FromMinutes(5);
    private readonly IDataProtector _templateProtector =
        dataProtectionProvider.CreateProtector("attendance-monitoring.fingerprint.template-backup.v1");

    #region Registration Operations

    public async Task<FingerprintRegistrationResponseDto> RegisterFingerprintAsync(RegisterFingerprint request, ClaimsPrincipal user)
    {
        logger.LogInformation("Registering fingerprint for StudentId: {StudentId}", request.StudentId);

        await EnsurePrivilegedUserAsync(user, "register fingerprints").ConfigureAwait(false);

        var student = await studentRepository.GetStudentByIdAsync(request.StudentId).ConfigureAwait(false);
        if (student == null)
        {
            throw new EntityNotFoundException<int>("Student", request.StudentId);
        }

        if (student.IsDeleted)
        {
            throw new ValidationException("Cannot register fingerprint for a deleted student");
        }

        var existingFingerprint = await fingerprintRepository
            .GetFingerprintByStudentIdIncludingDeletedAsync(request.StudentId)
            .ConfigureAwait(false);

        var encryptedTemplate = ProtectTemplate(request.TemplateData);

        if (existingFingerprint != null && !existingFingerprint.IsDeleted)
        {
            throw new ValidationException("Student already has a registered fingerprint. Please remove the existing fingerprint first.");
        }

        if (existingFingerprint != null && existingFingerprint.IsDeleted)
        {
            existingFingerprint.IsDeleted = false;
            existingFingerprint.DeletedAt = null;
            existingFingerprint.TemplateData = encryptedTemplate;
            existingFingerprint.DeviceId = request.DeviceId;
            existingFingerprint.SensorFingerprintId = request.SensorFingerprintId;
            existingFingerprint.UpdatedAt = DateTime.UtcNow;

            await fingerprintRepository.UpdateFingerprintAsync(existingFingerprint).ConfigureAwait(false);

            return new FingerprintRegistrationResponseDto
            {
                Success = true,
                Message = "Fingerprint registration restored successfully",
                FingerprintId = existingFingerprint.Id,
                StudentId = request.StudentId,
                StudentName = $"{student.Firstname} {student.Lastname}"
            };
        }

        var fingerprint = new Fingerprint
        {
            UserId = student.UserId,
            TemplateData = encryptedTemplate,
            DeviceId = request.DeviceId,
            SensorFingerprintId = request.SensorFingerprintId,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow,
            IsDeleted = false
        };

        var createdFingerprint = await fingerprintRepository.CreateFingerprintAsync(fingerprint).ConfigureAwait(false);

        return new FingerprintRegistrationResponseDto
        {
            Success = true,
            Message = "Fingerprint registered successfully",
            FingerprintId = createdFingerprint.Id,
            StudentId = request.StudentId,
            StudentName = $"{student.Firstname} {student.Lastname}"
        };
    }

    public async Task<FingerprintRegistrationResponseDto> UpdateFingerprintAsync(int fingerprintId, RegisterFingerprint request, ClaimsPrincipal user)
    {
        logger.LogInformation("Updating fingerprint with ID: {FingerprintId}", fingerprintId);

        await EnsurePrivilegedUserAsync(user, "update fingerprints").ConfigureAwait(false);

        var fingerprint = await fingerprintRepository.GetFingerprintByIdAsync(fingerprintId).ConfigureAwait(false);
        if (fingerprint == null)
        {
            throw new EntityNotFoundException<int>("Fingerprint", fingerprintId);
        }

        if (fingerprint.IsDeleted)
        {
            throw new ValidationException("Cannot update a deleted fingerprint");
        }

        fingerprint.TemplateData = ProtectTemplate(request.TemplateData);
        fingerprint.DeviceId = request.DeviceId;
        fingerprint.SensorFingerprintId = request.SensorFingerprintId;
        fingerprint.UpdatedAt = DateTime.UtcNow;

        await fingerprintRepository.UpdateFingerprintAsync(fingerprint).ConfigureAwait(false);

        var student = await studentRepository.GetStudentByUserIdAsync(fingerprint.UserId).ConfigureAwait(false);

        return new FingerprintRegistrationResponseDto
        {
            Success = true,
            Message = "Fingerprint updated successfully",
            FingerprintId = fingerprint.Id,
            StudentId = student?.Id,
            StudentName = student != null ? $"{student.Firstname} {student.Lastname}" : null
        };
    }

    public async Task<FingerprintRegistrationResponseDto> RemoveFingerprintAsync(int fingerprintId, ClaimsPrincipal user)
    {
        logger.LogInformation("Removing fingerprint with ID: {FingerprintId}", fingerprintId);

        await EnsurePrivilegedUserAsync(user, "remove fingerprints").ConfigureAwait(false);

        var result = await fingerprintRepository.SoftDeleteFingerprintAsync(fingerprintId).ConfigureAwait(false);

        if (!result)
        {
            throw new EntityNotFoundException<int>("Fingerprint", fingerprintId);
        }

        return new FingerprintRegistrationResponseDto
        {
            Success = true,
            Message = "Fingerprint removed successfully"
        };
    }

    public async Task<FingerprintEnrollmentSessionResponseDto> StartEnrollmentSessionAsync(
        StartFingerprintEnrollmentSessionRequest request,
        ClaimsPrincipal user)
    {
        logger.LogInformation(
            "Starting fingerprint enrollment session for StudentId: {StudentId} on DeviceId: {DeviceId}",
            request.StudentId,
            request.DeviceId);

        var requestedByUserId = await EnsurePrivilegedUserAsync(user, "start fingerprint enrollment").ConfigureAwait(false);
        var student = await studentRepository.GetStudentByIdAsync(request.StudentId).ConfigureAwait(false);
        if (student == null)
        {
            throw new EntityNotFoundException<int>("Student", request.StudentId);
        }

        if (student.IsDeleted)
        {
            throw new ValidationException("Cannot enroll fingerprint for a deleted student");
        }

        var existingFingerprint = await fingerprintRepository
            .GetFingerprintByStudentIdIncludingDeletedAsync(request.StudentId)
            .ConfigureAwait(false);

        if (existingFingerprint != null && !existingFingerprint.IsDeleted)
        {
            throw new ValidationException("Student already has an active fingerprint registration");
        }

        var device = await GetOrCreateManagedDeviceAsync(request.DeviceId).ConfigureAwait(false);
        await ExpireStaleEnrollmentSessionsAsync(device.Id).ConfigureAwait(false);

        var activeSession = await context.FingerprintEnrollmentSessions
            .AsNoTracking()
            .Where(session =>
                session.DeviceId == device.Id &&
                (session.Status == PendingStatus || session.Status == InProgressStatus) &&
                session.ExpiresAt > DateTime.UtcNow)
            .FirstOrDefaultAsync()
            .ConfigureAwait(false);

        if (activeSession != null)
        {
            throw new ValidationException("The selected device already has an active enrollment session");
        }

        var deviceFingerprints = await fingerprintRepository
            .GetFingerprintsByDeviceIdAsync(request.DeviceId)
            .ConfigureAwait(false);

        var assignedSlot = SelectNextAvailableSlot(deviceFingerprints.Select(fingerprint => fingerprint.SensorFingerprintId));

        var now = DateTime.UtcNow;
        var enrollmentSession = new FingerprintEnrollmentSession
        {
            EnrollmentSessionId = Guid.NewGuid(),
            DeviceId = device.Id,
            StudentId = student.Id,
            RequestedByUserId = requestedByUserId ?? "unknown",
            AssignedSensorFingerprintId = assignedSlot,
            Status = PendingStatus,
            ExpiresAt = now.Add(EnrollmentLifetime),
            CreatedAt = now,
            UpdatedAt = now
        };

        context.FingerprintEnrollmentSessions.Add(enrollmentSession);
        await context.SaveChangesAsync().ConfigureAwait(false);

        return MapEnrollmentSessionDto(enrollmentSession, device.DeviceIdentifier, student);
    }

    public async Task<FingerprintEnrollmentSessionResponseDto?> GetPendingEnrollmentSessionAsync(string deviceId, string apiKey)
    {
        ValidateDeviceApiKey(deviceId, apiKey);

        var device = await GetProvisionedActiveDeviceAsync(deviceId).ConfigureAwait(false);
        await ExpireStaleEnrollmentSessionsAsync(device.Id).ConfigureAwait(false);

        var enrollmentSession = await context.FingerprintEnrollmentSessions
            .Where(session =>
                session.DeviceId == device.Id &&
                (session.Status == PendingStatus || session.Status == InProgressStatus) &&
                session.ExpiresAt > DateTime.UtcNow)
            .OrderBy(session => session.CreatedAt)
            .FirstOrDefaultAsync()
            .ConfigureAwait(false);

        if (enrollmentSession == null)
        {
            return null;
        }

        if (enrollmentSession.Status == PendingStatus)
        {
            enrollmentSession.Status = InProgressStatus;
            enrollmentSession.StartedAt ??= DateTime.UtcNow;
            enrollmentSession.UpdatedAt = DateTime.UtcNow;
            await context.SaveChangesAsync().ConfigureAwait(false);
        }

        var student = await studentRepository.GetStudentByIdAsync(enrollmentSession.StudentId).ConfigureAwait(false)
                      ?? throw new EntityNotFoundException<int>("Student", enrollmentSession.StudentId);

        return MapEnrollmentSessionDto(enrollmentSession, device.DeviceIdentifier, student);
    }

    public async Task<FingerprintRegistrationResponseDto> CompleteEnrollmentSessionAsync(
        CompleteFingerprintEnrollmentRequest request,
        string apiKey)
    {
        ValidateDeviceApiKey(request.DeviceId, apiKey);

        var enrollmentSession = await context.FingerprintEnrollmentSessions
            .Include(session => session.Device)
            .FirstOrDefaultAsync(session =>
                session.EnrollmentSessionId == request.EnrollmentSessionId)
            .ConfigureAwait(false);

        if (enrollmentSession == null)
        {
            throw new EntityNotFoundException<string>("FingerprintEnrollmentSession", request.EnrollmentSessionId.ToString());
        }

        if (!string.Equals(enrollmentSession.Device.DeviceIdentifier, request.DeviceId, StringComparison.Ordinal))
        {
            throw new ValidationException("The fingerprint enrollment session does not belong to the requesting device");
        }

        if (!enrollmentSession.Device.IsActive)
        {
            throw new ValidationException("The fingerprint device is inactive");
        }

        enrollmentSession.Device.LastSeenAt = DateTime.UtcNow;
        enrollmentSession.Device.UpdatedAt = DateTime.UtcNow;

        if (enrollmentSession.ExpiresAt <= DateTime.UtcNow)
        {
            enrollmentSession.Status = ExpiredStatus;
            enrollmentSession.UpdatedAt = DateTime.UtcNow;
            await context.SaveChangesAsync().ConfigureAwait(false);
            throw new ValidationException("The fingerprint enrollment session has expired");
        }

        if (request.SensorFingerprintId != enrollmentSession.AssignedSensorFingerprintId)
        {
            throw new ValidationException("The device reported a sensor slot different from the assigned slot");
        }

        if (!request.Success)
        {
            enrollmentSession.Status = FailedStatus;
            enrollmentSession.FailureReason = request.FailureReason ?? "Device enrollment failed";
            enrollmentSession.CompletedAt = DateTime.UtcNow;
            enrollmentSession.UpdatedAt = DateTime.UtcNow;
            await context.SaveChangesAsync().ConfigureAwait(false);

            return new FingerprintRegistrationResponseDto
            {
                Success = false,
                Message = enrollmentSession.FailureReason
            };
        }

        if (string.IsNullOrWhiteSpace(request.BackupTemplateBase64))
        {
            throw new ValidationException("A backup template is required for successful enrollment");
        }

        var student = await studentRepository.GetStudentByIdAsync(enrollmentSession.StudentId).ConfigureAwait(false)
                      ?? throw new EntityNotFoundException<int>("Student", enrollmentSession.StudentId);
        if (student.IsDeleted)
        {
            throw new ValidationException("Cannot complete enrollment for a deleted student");
        }

        var existingFingerprintForStudent = await fingerprintRepository
            .GetFingerprintByStudentIdIncludingDeletedAsync(student.Id)
            .ConfigureAwait(false);

        var existingFingerprintForSlot = await fingerprintRepository
            .FindFingerprintByDeviceAndSensorIdAsync(request.DeviceId, request.SensorFingerprintId)
            .ConfigureAwait(false);

        if (existingFingerprintForSlot != null &&
            existingFingerprintForSlot.UserId != student.UserId &&
            !existingFingerprintForSlot.IsDeleted)
        {
            throw new ValidationException("The assigned sensor slot is already linked to another student");
        }

        var protectedTemplate = ProtectTemplate(request.BackupTemplateBase64);
        Fingerprint fingerprint;

        if (existingFingerprintForStudent == null)
        {
            fingerprint = await fingerprintRepository.CreateFingerprintAsync(new Fingerprint
            {
                UserId = student.UserId,
                TemplateData = protectedTemplate,
                DeviceId = request.DeviceId,
                SensorFingerprintId = request.SensorFingerprintId,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow,
                IsDeleted = false
            }).ConfigureAwait(false);
        }
        else
        {
            existingFingerprintForStudent.IsDeleted = false;
            existingFingerprintForStudent.DeletedAt = null;
            existingFingerprintForStudent.TemplateData = protectedTemplate;
            existingFingerprintForStudent.DeviceId = request.DeviceId;
            existingFingerprintForStudent.SensorFingerprintId = request.SensorFingerprintId;
            existingFingerprintForStudent.UpdatedAt = DateTime.UtcNow;

            fingerprint = await fingerprintRepository.UpdateFingerprintAsync(existingFingerprintForStudent).ConfigureAwait(false);
        }

        enrollmentSession.Status = CompletedStatus;
        enrollmentSession.CompletedAt = DateTime.UtcNow;
        enrollmentSession.FailureReason = null;
        enrollmentSession.UpdatedAt = DateTime.UtcNow;
        await context.SaveChangesAsync().ConfigureAwait(false);

        return new FingerprintRegistrationResponseDto
        {
            Success = true,
            Message = "Fingerprint registered successfully",
            FingerprintId = fingerprint.Id,
            StudentId = student.Id,
            StudentName = $"{student.Firstname} {student.Lastname}"
        };
    }

    #endregion

    #region Scan/Attendance Operations

    public async Task<FingerprintScanResponseDto> ScanFingerprintAsync(ScanFingerprint request)
    {
        logger.LogInformation("Processing template-based fingerprint scan from device: {DeviceId}", request.DeviceId);

        var fingerprint = await MatchFingerprintAsync(request.TemplateData).ConfigureAwait(false);
        if (fingerprint == null)
        {
            return new FingerprintScanResponseDto
            {
                Success = false,
                Message = "No matching fingerprint found",
                MatchMethod = "TemplateExact"
            };
        }

        return await HandleMatchedFingerprintAsync(
            fingerprint,
            request.SessionId,
            "TemplateExact",
            null,
            scanEvent: null).ConfigureAwait(false);
    }

    public async Task<FingerprintScanResponseDto> ScanFingerprintBySensorAsync(ScanFingerprintBySensorRequest request, string apiKey)
    {
        logger.LogInformation(
            "Processing sensor-slot fingerprint scan from device: {DeviceId}, Slot: {Slot}",
            request.DeviceId,
            request.SensorFingerprintId);

        ValidateDeviceApiKey(request.DeviceId, apiKey);

        var device = await GetProvisionedActiveDeviceAsync(request.DeviceId).ConfigureAwait(false);
        var scanEvent = new FingerprintScanEvent
        {
            DeviceId = device.Id,
            CapturedAt = request.CapturedAt ?? DateTime.UtcNow,
            ReceivedAt = DateTime.UtcNow,
            CreatedAt = DateTime.UtcNow,
            MatchScore = NormalizeConfidence(request.Confidence),
            Status = "Pending",
            RowVersion = Array.Empty<byte>()
        };

        var fingerprint = await fingerprintRepository
            .FindFingerprintByDeviceAndSensorIdAsync(request.DeviceId, request.SensorFingerprintId)
            .ConfigureAwait(false);

        if (fingerprint == null || fingerprint.IsDeleted)
        {
            scanEvent.Status = "NoMatch";
            scanEvent.FailureReason = "No matching fingerprint found";
            context.FingerprintScanEvents.Add(scanEvent);
            await context.SaveChangesAsync().ConfigureAwait(false);

            return new FingerprintScanResponseDto
            {
                Success = false,
                Message = "No matching fingerprint found",
                MatchMethod = "DeviceSlot",
                MatchScore = request.Confidence
            };
        }

        return await HandleMatchedFingerprintAsync(
            fingerprint,
            request.SessionId,
            "DeviceSlot",
            request.Confidence,
            scanEvent).ConfigureAwait(false);
    }

    public async Task<FingerprintScanResponseDto> ValidateFingerprintAsync(string templateData)
    {
        logger.LogInformation("Validating fingerprint template");

        var fingerprint = await MatchFingerprintAsync(templateData).ConfigureAwait(false);
        if (fingerprint == null)
        {
            return new FingerprintScanResponseDto
            {
                Success = false,
                Message = "No matching fingerprint found",
                MatchMethod = "TemplateExact"
            };
        }

        var student = await studentRepository.GetStudentByUserIdAsync(fingerprint.UserId).ConfigureAwait(false);
        if (student == null || student.IsDeleted)
        {
            return new FingerprintScanResponseDto
            {
                Success = false,
                Message = "Student account not found or inactive",
                MatchMethod = "TemplateExact"
            };
        }

        return new FingerprintScanResponseDto
        {
            Success = true,
            Message = "Fingerprint validated successfully",
            StudentId = student.Id,
            StudentName = $"{student.Firstname} {student.Lastname}",
            MatchMethod = "TemplateExact"
        };
    }

    #endregion

    #region Query Operations

    public async Task<FingerprintResponseDto> GetFingerprintByStudentIdAsync(int studentId, ClaimsPrincipal user)
    {
        logger.LogInformation("Retrieving fingerprint for student ID: {StudentId}", studentId);

        var currentUserRole = user.FindFirst(ClaimTypes.Role)?.Value;
        var currentUserId = await userContextService.GetUserIdAsync(user).ConfigureAwait(false);

        if (currentUserRole == RoleConstants.Student)
        {
            var student = await studentRepository.GetStudentByIdAsync(studentId).ConfigureAwait(false);
            if (student == null || student.UserId != currentUserId)
            {
                throw new EntityUnauthorizedException(
                    "Fingerprint",
                    "View",
                    currentUserId ?? "unknown",
                    "You can only view your own fingerprint information");
            }
        }

        var fingerprint = await fingerprintRepository.GetFingerprintByStudentIdAsync(studentId).ConfigureAwait(false);
        if (fingerprint == null || fingerprint.IsDeleted)
        {
            throw new EntityNotFoundException<int>("Fingerprint", studentId);
        }

        return MapToResponseDto(fingerprint, studentId);
    }

    public async Task<IEnumerable<FingerprintResponseDto>> GetFingerprintsByDeviceIdAsync(string deviceId, ClaimsPrincipal user)
    {
        logger.LogInformation("Retrieving fingerprints for device: {DeviceId}", deviceId);

        await EnsurePrivilegedUserAsync(user, "view device fingerprints").ConfigureAwait(false);

        var fingerprints = await fingerprintRepository.GetFingerprintsByDeviceIdAsync(deviceId).ConfigureAwait(false);

        var result = new List<FingerprintResponseDto>();
        foreach (var fingerprint in fingerprints)
        {
            var student = await studentRepository.GetStudentByUserIdAsync(fingerprint.UserId).ConfigureAwait(false);
            result.Add(MapToResponseDto(fingerprint, student?.Id));
        }

        return result;
    }

    public async Task<IEnumerable<FingerprintResponseDto>> GetAllActiveFingerprintsAsync(ClaimsPrincipal user)
    {
        logger.LogInformation("Retrieving all active fingerprints");

        await EnsurePrivilegedUserAsync(user, "view all fingerprints").ConfigureAwait(false);

        var fingerprints = await fingerprintRepository.GetActiveFingerprintsAsync().ConfigureAwait(false);

        var result = new List<FingerprintResponseDto>();
        foreach (var fingerprint in fingerprints)
        {
            var student = await studentRepository.GetStudentByUserIdAsync(fingerprint.UserId).ConfigureAwait(false);
            result.Add(MapToResponseDto(fingerprint, student?.Id));
        }

        return result;
    }

    public async Task<bool> StudentHasFingerprintAsync(int studentId)
    {
        return await fingerprintRepository.StudentHasFingerprintAsync(studentId).ConfigureAwait(false);
    }

    #endregion

    #region Private Helper Methods

    private async Task<Fingerprint?> MatchFingerprintAsync(string templateData)
    {
        var fingerprints = await fingerprintRepository.GetActiveFingerprintsAsync().ConfigureAwait(false);

        foreach (var fingerprint in fingerprints)
        {
            if (TryReadStoredTemplate(fingerprint.TemplateData) == templateData)
            {
                return fingerprint;
            }
        }

        return null;
    }

    private async Task<FingerprintScanResponseDto> HandleMatchedFingerprintAsync(
        Fingerprint fingerprint,
        int? requestedSessionId,
        string matchMethod,
        int? matchScore,
        FingerprintScanEvent? scanEvent)
    {
        var student = await studentRepository.GetStudentByUserIdAsync(fingerprint.UserId).ConfigureAwait(false);
        if (student == null || student.IsDeleted)
        {
            if (scanEvent != null)
            {
                scanEvent.Status = "Rejected";
                scanEvent.FailureReason = "Student account not found or inactive";
                context.FingerprintScanEvents.Add(scanEvent);
                await context.SaveChangesAsync().ConfigureAwait(false);
            }

            return new FingerprintScanResponseDto
            {
                Success = false,
                Message = "Student account not found or inactive",
                MatchMethod = matchMethod,
                MatchScore = matchScore
            };
        }

        Session? session;
        if (requestedSessionId.HasValue)
        {
            session = await sessionRepository.GetSessionByIdAsync(requestedSessionId.Value).ConfigureAwait(false);
            if (session == null)
            {
                if (scanEvent != null)
                {
                    scanEvent.Status = "Rejected";
                    scanEvent.MatchedStudentId = student.Id;
                    scanEvent.FailureReason = "Session not found";
                    context.FingerprintScanEvents.Add(scanEvent);
                    await context.SaveChangesAsync().ConfigureAwait(false);
                }

                return new FingerprintScanResponseDto
                {
                    Success = false,
                    Message = "Session not found",
                    StudentId = student.Id,
                    StudentName = $"{student.Firstname} {student.Lastname}",
                    MatchMethod = matchMethod,
                    MatchScore = matchScore
                };
            }
        }
        else
        {
            session = await FindActiveSessionForStudentAsync(student.Id).ConfigureAwait(false);
            if (session == null)
            {
                if (scanEvent != null)
                {
                    scanEvent.Status = "Rejected";
                    scanEvent.MatchedStudentId = student.Id;
                    scanEvent.FailureReason = "No active session found at this time";
                    context.FingerprintScanEvents.Add(scanEvent);
                    await context.SaveChangesAsync().ConfigureAwait(false);
                }

                return new FingerprintScanResponseDto
                {
                    Success = false,
                    Message = "No active session found at this time",
                    StudentId = student.Id,
                    StudentName = $"{student.Firstname} {student.Lastname}",
                    MatchMethod = matchMethod,
                    MatchScore = matchScore
                };
            }
        }

        var isEnrolled = await VerifyStudentEnrollmentAsync(student.Id, session).ConfigureAwait(false);
        if (!isEnrolled)
        {
            if (scanEvent != null)
            {
                scanEvent.Status = "Rejected";
                scanEvent.MatchedStudentId = student.Id;
                scanEvent.SessionId = session.Id;
                scanEvent.FailureReason = "Student is not enrolled in this session";
                context.FingerprintScanEvents.Add(scanEvent);
                await context.SaveChangesAsync().ConfigureAwait(false);
            }

            return new FingerprintScanResponseDto
            {
                Success = false,
                Message = "Student is not enrolled in this session",
                StudentId = student.Id,
                StudentName = $"{student.Firstname} {student.Lastname}",
                MatchMethod = matchMethod,
                MatchScore = matchScore
            };
        }

        var existingAttendance = await attendanceRepository
            .GetAttendanceByStudentAndSessionAsync(student.Id, session.Id)
            .ConfigureAwait(false);

        if (existingAttendance != null)
        {
            if (scanEvent != null)
            {
                scanEvent.Status = "Duplicate";
                scanEvent.MatchedStudentId = student.Id;
                scanEvent.SessionId = session.Id;
                scanEvent.AttendanceRecordId = existingAttendance.Id;
                context.FingerprintScanEvents.Add(scanEvent);
                await context.SaveChangesAsync().ConfigureAwait(false);
            }

            return new FingerprintScanResponseDto
            {
                Success = true,
                Message = "Already checked in",
                AttendanceMarked = false,
                AttendanceTime = existingAttendance.CheckInTime,
                StudentId = student.Id,
                StudentName = $"{student.Firstname} {student.Lastname}",
                ClassName = session.Schedule.Section.Name,
                SubjectName = session.Schedule.Subject.Name,
                RoomName = session.ActualRoom?.Name ?? "TBD",
                InstructorName = $"{session.Schedule.Instructor.Firstname} {session.Schedule.Instructor.Lastname}",
                AttendanceRecordId = existingAttendance.Id,
                AttendanceStatus = existingAttendance.Status,
                IsDuplicateScan = true,
                SessionId = session.Id,
                MatchMethod = matchMethod,
                MatchScore = matchScore
            };
        }

        using var transaction = await fingerprintRepository.BeginTransactionAsync().ConfigureAwait(false);

        try
        {
            var checkInTime = DateTime.Now;
            var attendanceRecord = new AttendanceRecord
            {
                StudentId = student.Id,
                SessionId = session.Id,
                CheckInTime = checkInTime,
                Status = DetermineAttendanceStatus(checkInTime, session),
                IsManualEntry = false,
                CreatedAt = checkInTime,
                UpdatedAt = checkInTime
            };

            var createdRecord = await attendanceRepository.CreateAsync(attendanceRecord).ConfigureAwait(false);
            await attendanceRepository.SaveChangesAsync().ConfigureAwait(false);

            await transaction.CommitAsync().ConfigureAwait(false);

            if (scanEvent != null)
            {
                scanEvent.Status = "AttendanceMarked";
                scanEvent.MatchedStudentId = student.Id;
                scanEvent.SessionId = session.Id;
                scanEvent.AttendanceRecordId = createdRecord.Id;
                context.FingerprintScanEvents.Add(scanEvent);
                await context.SaveChangesAsync().ConfigureAwait(false);
            }

            _ = Task.Run(async () =>
            {
                try
                {
                    var instructorUserId = session.Schedule.Instructor.UserId;
                    if (!string.IsNullOrWhiteSpace(instructorUserId))
                    {
                        await notificationService.NotifyStudentCheckedInAsync(
                            student.UserId,
                            instructorUserId,
                            session.Id,
                            attendanceRecord.Status).ConfigureAwait(false);
                    }
                }
                catch (Exception ex)
                {
                    logger.LogError(ex, "Failed to send notification for fingerprint check-in");
                }
            });

            return new FingerprintScanResponseDto
            {
                Success = true,
                Message = "Attendance marked successfully via fingerprint",
                AttendanceMarked = true,
                AttendanceTime = checkInTime,
                StudentId = student.Id,
                StudentName = $"{student.Firstname} {student.Lastname}",
                ClassName = session.Schedule.Section.Name,
                SubjectName = session.Schedule.Subject.Name,
                RoomName = session.ActualRoom?.Name ?? "TBD",
                InstructorName = $"{session.Schedule.Instructor.Firstname} {session.Schedule.Instructor.Lastname}",
                AttendanceRecordId = createdRecord.Id,
                AttendanceStatus = attendanceRecord.Status,
                IsDuplicateScan = false,
                SessionId = session.Id,
                MatchMethod = matchMethod,
                MatchScore = matchScore
            };
        }
        catch (Exception ex)
        {
            await transaction.RollbackAsync().ConfigureAwait(false);

            if (scanEvent != null)
            {
                scanEvent.Status = "Error";
                scanEvent.MatchedStudentId = student.Id;
                scanEvent.SessionId = session.Id;
                scanEvent.FailureReason = "Failed to mark attendance";
                context.FingerprintScanEvents.Add(scanEvent);
                await context.SaveChangesAsync().ConfigureAwait(false);
            }

            throw new EntityServiceException("FingerprintScan", $"StudentId:{student.Id}", "Failed to mark attendance", ex);
        }
    }

    private async Task<Session?> FindActiveSessionForStudentAsync(int studentId)
    {
        var now = DateTime.Now;
        var today = now.Date;

        var enrollments = await studentEnrollmentRepository
            .GetByStudentIdAsync(studentId)
            .ConfigureAwait(false);

        var activeEnrollments = enrollments
            .Where(enrollment => enrollment.IsActive)
            .ToList();

        if (activeEnrollments.Count == 0)
        {
            return null;
        }

        var sectionIds = activeEnrollments.Select(enrollment => enrollment.SectionId).Distinct().ToList();
        var subjectIds = activeEnrollments.Select(enrollment => enrollment.SubjectId).Distinct().ToList();

        var schedules = await scheduleRepository
            .GetSchedulesBySectionsAndSubjectsAsync(sectionIds, subjectIds)
            .ConfigureAwait(false);

        foreach (var schedule in schedules)
        {
            var sessions = await sessionRepository
                .GetSessionsByScheduleIdAsync(schedule.Id)
                .ConfigureAwait(false);

            foreach (var session in sessions.Where(session =>
                         session.SessionDate.Date == today &&
                         session.Status == SessionStatusConstants.Active &&
                         session.ActualStartTime.HasValue &&
                         session.ActualStartTime <= now &&
                         (!session.ActualEndTime.HasValue || session.ActualEndTime > now)))
            {
                return session;
            }
        }

        return null;
    }

    private async Task<bool> VerifyStudentEnrollmentAsync(int studentId, Session session)
    {
        var student = await studentRepository.GetStudentByIdAsync(studentId).ConfigureAwait(false);
        if (student == null)
        {
            return false;
        }

        if (student.SectionId == session.Schedule.SectionId)
        {
            return true;
        }

        var enrollment = await studentEnrollmentRepository
            .GetByStudentSectionSubjectAsync(studentId, session.Schedule.SectionId, session.Schedule.SubjectId)
            .ConfigureAwait(false);

        return enrollment?.IsActive == true;
    }

    private string DetermineAttendanceStatus(DateTime checkInTime, Session session)
    {
        var sessionStartTime = session.ActualStartTime ??
                               DateTime.Today.Add(session.Schedule.TimeIn.ToTimeSpan());
        var lateCutoffMinutes = 15;

        if (session.AttendanceCutOff.HasValue)
        {
            lateCutoffMinutes = Math.Max(
                0,
                (int)Math.Round(
                    (session.AttendanceCutOff.Value - sessionStartTime).TotalMinutes,
                    MidpointRounding.AwayFromZero));
        }

        return _attendanceService.DetermineAttendanceStatus(checkInTime, sessionStartTime, lateCutoffMinutes);
    }

    private FingerprintResponseDto MapToResponseDto(Fingerprint fingerprint, int? studentId)
    {
        return new FingerprintResponseDto
        {
            Id = fingerprint.Id,
            UserId = fingerprint.UserId,
            StudentId = studentId,
            DeviceId = fingerprint.DeviceId,
            SensorFingerprintId = fingerprint.SensorFingerprintId,
            RegisteredAt = fingerprint.CreatedAt,
            UpdatedAt = fingerprint.UpdatedAt,
            IsActive = !fingerprint.IsDeleted
        };
    }

    private FingerprintEnrollmentSessionResponseDto MapEnrollmentSessionDto(
        FingerprintEnrollmentSession enrollmentSession,
        string deviceIdentifier,
        Student student)
    {
        return new FingerprintEnrollmentSessionResponseDto
        {
            Success = true,
            Message = "Fingerprint enrollment session ready",
            EnrollmentSessionId = enrollmentSession.EnrollmentSessionId,
            StudentId = student.Id,
            StudentName = $"{student.Firstname} {student.Lastname}",
            DeviceId = deviceIdentifier,
            AssignedSensorFingerprintId = enrollmentSession.AssignedSensorFingerprintId,
            Status = enrollmentSession.Status,
            ExpiresAt = enrollmentSession.ExpiresAt
        };
    }

    private async Task<string?> EnsurePrivilegedUserAsync(ClaimsPrincipal user, string operation)
    {
        var currentUserRole = user.FindFirst(ClaimTypes.Role)?.Value;
        var currentUserId = await userContextService.GetUserIdAsync(user).ConfigureAwait(false);

        if (currentUserRole != RoleConstants.Admin &&
            currentUserRole != RoleConstants.Instructor)
        {
            throw new EntityUnauthorizedException("Fingerprint", operation, currentUserId ?? "unknown", $"Only Admin and Instructors can {operation}.");
        }

        return currentUserId;
    }

    private void ValidateDeviceApiKey(string deviceId, string apiKey)
    {
        var configuredApiKey = configuration[$"FingerprintDeviceAuth:Devices:{deviceId}"];
        if (string.IsNullOrWhiteSpace(configuredApiKey) || string.IsNullOrWhiteSpace(apiKey))
        {
            throw new EntityUnauthorizedException("FingerprintDevice", "Authenticate", "device", "Invalid device API key");
        }

        var configuredApiKeyBytes = Encoding.UTF8.GetBytes(configuredApiKey);
        var providedApiKeyBytes = Encoding.UTF8.GetBytes(apiKey);
        if (!CryptographicOperations.FixedTimeEquals(configuredApiKeyBytes, providedApiKeyBytes))
        {
            throw new EntityUnauthorizedException("FingerprintDevice", "Authenticate", "device", "Invalid device API key");
        }
    }

    private async Task<FingerprintDevice> GetProvisionedActiveDeviceAsync(string deviceIdentifier)
    {
        var device = await context.FingerprintDevices
            .FirstOrDefaultAsync(existingDevice => existingDevice.DeviceIdentifier == deviceIdentifier)
            .ConfigureAwait(false);

        if (device == null)
        {
            throw new EntityUnauthorizedException(
                "FingerprintDevice",
                "Authenticate",
                deviceIdentifier,
                "Fingerprint device is not provisioned for API access");
        }

        if (!device.IsActive)
        {
            throw new ValidationException("The fingerprint device is inactive");
        }

        device.LastSeenAt = DateTime.UtcNow;
        device.UpdatedAt = DateTime.UtcNow;
        await context.SaveChangesAsync().ConfigureAwait(false);
        return device;
    }

    private async Task<FingerprintDevice> GetOrCreateManagedDeviceAsync(string deviceIdentifier)
    {
        var configuredApiKey = configuration[$"FingerprintDeviceAuth:Devices:{deviceIdentifier}"];
        if (string.IsNullOrWhiteSpace(configuredApiKey))
        {
            throw new ValidationException("The fingerprint device is not registered for API access");
        }

        var device = await context.FingerprintDevices
            .FirstOrDefaultAsync(existingDevice => existingDevice.DeviceIdentifier == deviceIdentifier)
            .ConfigureAwait(false);

        if (device == null)
        {
            device = new FingerprintDevice
            {
                DeviceIdentifier = deviceIdentifier,
                IsActive = true,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow,
                LastSeenAt = DateTime.UtcNow
            };

            context.FingerprintDevices.Add(device);
            await context.SaveChangesAsync().ConfigureAwait(false);
            return device;
        }

        if (!device.IsActive)
        {
            throw new ValidationException("The fingerprint device is inactive");
        }

        device.LastSeenAt = DateTime.UtcNow;
        device.UpdatedAt = DateTime.UtcNow;
        await context.SaveChangesAsync().ConfigureAwait(false);
        return device;
    }

    private async Task ExpireStaleEnrollmentSessionsAsync(int deviceId)
    {
        var staleSessions = await context.FingerprintEnrollmentSessions
            .Where(session =>
                session.DeviceId == deviceId &&
                (session.Status == PendingStatus || session.Status == InProgressStatus) &&
                session.ExpiresAt <= DateTime.UtcNow)
            .ToListAsync()
            .ConfigureAwait(false);

        if (staleSessions.Count == 0)
        {
            return;
        }

        foreach (var staleSession in staleSessions)
        {
            staleSession.Status = ExpiredStatus;
            staleSession.UpdatedAt = DateTime.UtcNow;
            staleSession.CompletedAt ??= DateTime.UtcNow;
            staleSession.FailureReason ??= "Enrollment session expired";
        }

        await context.SaveChangesAsync().ConfigureAwait(false);
    }

    private static int SelectNextAvailableSlot(IEnumerable<int> occupiedSlots)
    {
        var occupied = occupiedSlots.Where(slot => slot > 0).ToHashSet();
        for (var slot = 1; slot <= 127; slot++)
        {
            if (!occupied.Contains(slot))
            {
                return slot;
            }
        }

        throw new ValidationException("The fingerprint device has no available enrollment slots");
    }

    private string ProtectTemplate(string templateData)
    {
        return _templateProtector.Protect(templateData);
    }

    private string TryReadStoredTemplate(string storedTemplate)
    {
        try
        {
            return _templateProtector.Unprotect(storedTemplate);
        }
        catch
        {
            return storedTemplate;
        }
    }

    private static decimal NormalizeConfidence(int confidence)
    {
        var normalized = Math.Clamp(confidence, 0, 255) / 255m;
        return decimal.Round(normalized, 4, MidpointRounding.AwayFromZero);
    }

    #endregion
}
