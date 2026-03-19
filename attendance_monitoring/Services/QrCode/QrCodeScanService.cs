using System.Security.Claims;
using attendance_monitoring.IRepository;
using attendance_monitoring.IServices;
using attendance_monitoring.Models.DTO.Request;
using attendance_monitoring.Models.DTO.Response;

namespace attendance_monitoring.Services.QrCode;

/// <summary>
/// Coordinates QR code scan validation, attendance creation, and transaction handling.
/// Keeps the QR usage increment and attendance write in a single atomic unit of work.
/// Notifications are dispatched only after the transaction commits successfully.
/// </summary>
internal sealed class QrCodeScanService
{
    private readonly IQrCodeRepository _qrCodeRepository;
    private readonly IStudentRepository _studentRepository;
    private readonly IAttendanceService _attendanceService;
    private readonly IAttendanceRepository _attendanceRepository;
    private readonly INotificationService _notificationService;
    private readonly QrCodeAuthorizationService _authorizationService;
    private readonly ILogger<QrCodeScanService> _logger;

    public QrCodeScanService(
        IQrCodeRepository qrCodeRepository,
        IStudentRepository studentRepository,
        IAttendanceService attendanceService,
        IAttendanceRepository attendanceRepository,
        INotificationService notificationService,
        QrCodeAuthorizationService authorizationService,
        ILogger<QrCodeScanService> logger)
    {
        _qrCodeRepository = qrCodeRepository ?? throw new ArgumentNullException(nameof(qrCodeRepository));
        _studentRepository = studentRepository ?? throw new ArgumentNullException(nameof(studentRepository));
        _attendanceService = attendanceService ?? throw new ArgumentNullException(nameof(attendanceService));
        _attendanceRepository = attendanceRepository ?? throw new ArgumentNullException(nameof(attendanceRepository));
        _notificationService = notificationService ?? throw new ArgumentNullException(nameof(notificationService));
        _authorizationService = authorizationService ?? throw new ArgumentNullException(nameof(authorizationService));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task<QrCodeScanResponseDto> ScanQrCodeAsync(ValidateQrCode validateQrCode, ClaimsPrincipal user)
    {
        _ = user;

        var utcNow = DateTime.UtcNow;

        using var transaction = await _qrCodeRepository.BeginTransactionAsync().ConfigureAwait(false);

        try
        {
            _logger.LogInformation("Scanning QR code with hash: {QrHash} for student ID: {StudentId}",
                validateQrCode.QrHash, validateQrCode.StudentId);

            var qrCode = await _qrCodeRepository.GetQrCodeByHashAsync(validateQrCode.QrHash).ConfigureAwait(false);

            if (qrCode == null)
            {
                _logger.LogWarning("QR code scan failed: QR code not found");
                return new QrCodeScanResponseDto { Success = false, Message = "QR code not found", AttendanceMarked = false };
            }

            _logger.LogDebug("QR code validation: Hash={QrHash}, Active={IsActive}, Expires={ExpiresAt}, Usage={UsageCount}/{MaxUsage}",
                validateQrCode.QrHash, qrCode.IsActive, qrCode.ExpiresAt, qrCode.UsageCount, qrCode.MaxUsage);

            if (!qrCode.IsActive)
            {
                _logger.LogWarning("QR code scan failed: QR code is inactive");
                return new QrCodeScanResponseDto { Success = false, Message = "QR code is inactive", AttendanceMarked = false };
            }

            if (qrCode.ExpiresAt <= utcNow)
            {
                _logger.LogWarning("QR code scan failed: QR code has expired at {ExpiresAt}", qrCode.ExpiresAt);
                return new QrCodeScanResponseDto { Success = false, Message = "QR code has expired", AttendanceMarked = false };
            }

            if (qrCode.MaxUsage.HasValue && qrCode.UsageCount >= qrCode.MaxUsage.Value)
            {
                _logger.LogWarning("QR code scan failed: QR code usage limit reached ({UsageCount}/{MaxUsage})",
                    qrCode.UsageCount, qrCode.MaxUsage.Value);
                return new QrCodeScanResponseDto { Success = false, Message = "QR code usage limit reached", AttendanceMarked = false };
            }

            _logger.LogDebug("Validating student: StudentId={StudentId}", validateQrCode.StudentId);
            var student = await _studentRepository.GetStudentByIdAsync(validateQrCode.StudentId).ConfigureAwait(false);
            if (student == null)
            {
                _logger.LogWarning("QR code scan failed: Student not found");
                return new QrCodeScanResponseDto { Success = false, Message = "Student not found", AttendanceMarked = false };
            }

            var sectionId = qrCode.Session?.Schedule?.SectionId ?? 0;
            var isStudentAuthorized = student.SectionId == sectionId;

            if (!isStudentAuthorized && qrCode.Session?.Schedule != null)
            {
                isStudentAuthorized = await _authorizationService.IsStudentEnrolledInSectionSubjectAsync(
                    validateQrCode.StudentId,
                    sectionId,
                    qrCode.Session.Schedule.SubjectId
                ).ConfigureAwait(false);
            }

            if (!isStudentAuthorized)
            {
                _logger.LogWarning("QR code scan failed: Student {StudentId} is not authorized for section {SectionId}",
                    validateQrCode.StudentId, sectionId);
                return new QrCodeScanResponseDto
                {
                    Success = false,
                    Message = "You are not enrolled in this section or subject",
                    AttendanceMarked = false,
                    StudentName = $"{student.Firstname} {student.Lastname}"
                };
            }

            _logger.LogDebug("Checking for duplicate attendance: StudentId={StudentId}, SessionId={SessionId}",
                validateQrCode.StudentId, qrCode.SessionId);

            var existingRecord = await _attendanceRepository.HasAttendanceRecordAsync(
                validateQrCode.StudentId,
                qrCode.SessionId
            ).ConfigureAwait(false);

            if (existingRecord)
            {
                _logger.LogWarning("Duplicate QR scan detected for student ID: {StudentId}, session ID: {SessionId}",
                    validateQrCode.StudentId, qrCode.SessionId);

                var remainingScansForDuplicate = qrCode.MaxUsage.HasValue
                    ? Math.Max(0, qrCode.MaxUsage.Value - qrCode.UsageCount)
                    : int.MaxValue;

                return new QrCodeScanResponseDto
                {
                    Success = true,
                    Message = "You have already checked in for this session",
                    AttendanceMarked = false,
                    AttendanceTime = utcNow,
                    StudentName = $"{student.Firstname} {student.Lastname}",
                    ClassName = qrCode.Session?.Schedule?.Section?.Name ?? "Unknown",
                    SubjectName = qrCode.Session?.Schedule?.Subject?.Name ?? "Unknown",
                    RoomName = qrCode.Session?.ActualRoom?.Name ?? "Unknown",
                    InstructorName = qrCode.Session?.Schedule?.Instructor != null
                        ? $"{qrCode.Session.Schedule.Instructor.Firstname} {qrCode.Session.Schedule.Instructor.Lastname}"
                        : "Unknown",
                    RemainingScans = remainingScansForDuplicate,
                    IsDuplicateScan = true
                };
            }

            _logger.LogInformation("No duplicate found, incrementing usage counter for QR hash: {QrHash}", validateQrCode.QrHash);
            var incrementResult = await _qrCodeRepository.AtomicIncrementUsageAsync(validateQrCode.QrHash, utcNow).ConfigureAwait(false);

            if (incrementResult == 0)
            {
                await transaction.RollbackAsync().ConfigureAwait(false);
                _logger.LogWarning("QR code usage increment failed - another scan may have reached the limit");
                return new QrCodeScanResponseDto
                {
                    Success = false,
                    Message = "QR code usage limit reached or QR code state changed",
                    AttendanceMarked = false
                };
            }

            try
            {
                _logger.LogDebug("Creating attendance record: StudentId={StudentId}, SessionId={SessionId}",
                    validateQrCode.StudentId, qrCode.SessionId);

                var attendanceRecord = await _attendanceService.CreateAttendanceFromQrScanAsync(
                    validateQrCode.StudentId,
                    qrCode.SessionId,
                    qrCode.Id,
                    utcNow
                ).ConfigureAwait(false);

                await _qrCodeRepository.SaveChangesAsync().ConfigureAwait(false);

                // Commit the QR usage increment and attendance creation together.
                await transaction.CommitAsync().ConfigureAwait(false);

                var instructorUserId = qrCode.Session?.Schedule?.Instructor?.UserId ?? string.Empty;
                if (!string.IsNullOrEmpty(instructorUserId))
                {
                    await _notificationService.NotifyStudentCheckedInAsync(
                        student.UserId,
                        instructorUserId,
                        qrCode.SessionId,
                        attendanceRecord.Status
                    ).ConfigureAwait(false);
                }

                var remainingScans = qrCode.MaxUsage.HasValue
                    ? Math.Max(0, qrCode.MaxUsage.Value - qrCode.UsageCount - 1)
                    : int.MaxValue;

                _logger.LogInformation("Successfully processed QR code scan for student ID: {StudentId}, attendance record ID: {AttendanceId}",
                    validateQrCode.StudentId, attendanceRecord.Id);

                return new QrCodeScanResponseDto
                {
                    Success = true,
                    Message = "Attendance marked successfully",
                    AttendanceMarked = true,
                    AttendanceTime = utcNow,
                    StudentName = $"{student.Firstname} {student.Lastname}",
                    ClassName = qrCode.Session?.Schedule?.Section?.Name ?? "Unknown",
                    SubjectName = qrCode.Session?.Schedule?.Subject?.Name ?? "Unknown",
                    RoomName = qrCode.Session?.ActualRoom?.Name ?? "Unknown",
                    InstructorName = qrCode.Session?.Schedule?.Instructor != null
                        ? $"{qrCode.Session.Schedule.Instructor.Firstname} {qrCode.Session.Schedule.Instructor.Lastname}"
                        : "Unknown",
                    RemainingScans = remainingScans,
                    AttendanceRecordId = attendanceRecord.Id,
                    AttendanceStatus = attendanceRecord.Status,
                    IsDuplicateScan = false
                };
            }
            catch (InvalidOperationException ex) when (ex.Message.Contains("duplicate"))
            {
                await transaction.RollbackAsync().ConfigureAwait(false);

                _logger.LogWarning("Duplicate detected during attendance creation (edge case): StudentId={StudentId}, SessionId={SessionId}",
                    validateQrCode.StudentId, qrCode.SessionId);

                var remainingScansEdge = qrCode.MaxUsage.HasValue
                    ? Math.Max(0, qrCode.MaxUsage.Value - qrCode.UsageCount)
                    : int.MaxValue;

                return new QrCodeScanResponseDto
                {
                    Success = true,
                    Message = "You have already checked in for this session",
                    AttendanceMarked = false,
                    AttendanceTime = utcNow,
                    StudentName = $"{student.Firstname} {student.Lastname}",
                    ClassName = qrCode.Session?.Schedule?.Section?.Name ?? "Unknown",
                    SubjectName = qrCode.Session?.Schedule?.Subject?.Name ?? "Unknown",
                    RoomName = qrCode.Session?.ActualRoom?.Name ?? "Unknown",
                    InstructorName = qrCode.Session?.Schedule?.Instructor != null
                        ? $"{qrCode.Session.Schedule.Instructor.Firstname} {qrCode.Session.Schedule.Instructor.Lastname}"
                        : "Unknown",
                    RemainingScans = remainingScansEdge,
                    IsDuplicateScan = true
                };
            }
        }
        catch (Exception ex)
        {
            await transaction.RollbackAsync().ConfigureAwait(false);
            _logger.LogError(ex, "Failed to scan QR code {QrHash} for student {StudentId}",
                validateQrCode.QrHash, validateQrCode.StudentId);
            return new QrCodeScanResponseDto
            {
                Success = false,
                Message = "An error occurred while processing the QR code scan",
                AttendanceMarked = false
            };
        }
    }
}
