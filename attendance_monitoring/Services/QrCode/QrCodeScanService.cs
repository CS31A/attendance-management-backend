using System.Security.Claims;
using attendance_monitoring.Classes;
using attendance_monitoring.Constants;
using attendance_monitoring.Data;
using attendance_monitoring.IRepository;
using attendance_monitoring.IServices;
using attendance_monitoring.Models.DTO.Request;
using attendance_monitoring.Models.DTO.Response;
using Microsoft.EntityFrameworkCore;
using QrCodeEntity = attendance_monitoring.Classes.QrCode;

namespace attendance_monitoring.Services.QrCode;

/// <summary>
/// Coordinates QR code scan validation, attendance creation, and transaction handling.
/// Keeps the QR usage increment and attendance write in a single atomic unit of work.
/// Notifications are dispatched only after the transaction commits successfully.
/// </summary>
internal sealed class QrCodeScanService
{
    private readonly ApplicationDbContext _dbContext;
    private readonly IQrCodeRepository _qrCodeRepository;
    private readonly IStudentRepository _studentRepository;
    private readonly IAttendanceService _attendanceService;
    private readonly IAttendanceRepository _attendanceRepository;
    private readonly INotificationService _notificationService;
    private readonly QrCodeAuthorizationService _authorizationService;
    private readonly ILogger<QrCodeScanService> _logger;

    public QrCodeScanService(
        ApplicationDbContext dbContext,
        IQrCodeRepository qrCodeRepository,
        IStudentRepository studentRepository,
        IAttendanceService attendanceService,
        IAttendanceRepository attendanceRepository,
        INotificationService notificationService,
        QrCodeAuthorizationService authorizationService,
        ILogger<QrCodeScanService> logger)
    {
        _dbContext = dbContext ?? throw new ArgumentNullException(nameof(dbContext));
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
        if (_dbContext.Database.IsInMemory() || _dbContext.Database.CurrentTransaction != null)
        {
            return await ScanQrCodeWithinTransactionAsync(validateQrCode, user).ConfigureAwait(false);
        }

        var strategy = _dbContext.Database.CreateExecutionStrategy();
        return await strategy.ExecuteAsync(() => ScanQrCodeWithinTransactionAsync(validateQrCode, user)).ConfigureAwait(false);
    }

    private async Task<QrCodeScanResponseDto> ScanQrCodeWithinTransactionAsync(ValidateQrCode validateQrCode, ClaimsPrincipal user)
    {
        var utcNow = DateTime.UtcNow;
        var localNow = DateTime.Now;
        int? resolvedStudentId = null;

        try
        {
            var normalizedRoles = user
                .FindAll(ClaimTypes.Role)
                .Select(claim => RoleConstants.NormalizeRole(claim.Value))
                .Where(role => !string.IsNullOrWhiteSpace(role))
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToArray();

            var isStudent = normalizedRoles.Any(role =>
                string.Equals(role, RoleConstants.Student, StringComparison.OrdinalIgnoreCase));

            if (!isStudent)
            {
                var roleSummary = normalizedRoles.Length == 0
                    ? "Unknown"
                    : string.Join(", ", normalizedRoles);
                _logger.LogWarning("QR code scan rejected for non-student role set: {Roles}", roleSummary);
                return new QrCodeScanResponseDto
                {
                    Success = false,
                    Message = "Only students can scan QR codes",
                    AttendanceMarked = false
                };
            }

            var currentUserId = await _authorizationService.UserContext.GetUserIdAsync(user).ConfigureAwait(false);
            if (string.IsNullOrWhiteSpace(currentUserId))
            {
                _logger.LogWarning("QR code scan rejected: Unable to resolve authenticated user ID");
                return new QrCodeScanResponseDto
                {
                    Success = false,
                    Message = "Unable to verify authenticated user",
                    AttendanceMarked = false
                };
            }

            var student = await _studentRepository.GetStudentByUserIdAsync(currentUserId).ConfigureAwait(false);
            if (student == null)
            {
                _logger.LogWarning("QR code scan rejected: Student profile not found for user {UserId}", currentUserId);
                return new QrCodeScanResponseDto
                {
                    Success = false,
                    Message = "Student profile not found for authenticated user",
                    AttendanceMarked = false
                };
            }

            if (validateQrCode.StudentId.HasValue && validateQrCode.StudentId.Value != student.Id)
            {
                _logger.LogWarning(
                    "QR code scan rejected: Request StudentId {RequestStudentId} does not match authenticated StudentId {StudentId} for user {UserId}",
                    validateQrCode.StudentId.Value,
                    student.Id,
                    currentUserId);

                return new QrCodeScanResponseDto
                {
                    Success = false,
                    Message = "Student ID does not match authenticated user",
                    AttendanceMarked = false,
                    StudentName = GetStudentName(student)
                };
            }

            var studentId = student.Id;
            resolvedStudentId = studentId;

            _logger.LogInformation("Scanning QR code with hash: {QrHash} for authenticated student ID: {StudentId}",
                validateQrCode.QrHash, studentId);

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

            var sectionId = qrCode.Session?.Schedule?.SectionId ?? 0;
            var isStudentAuthorized = student.SectionId == sectionId;

            if (!isStudentAuthorized && qrCode.Session?.Schedule != null)
            {
                isStudentAuthorized = await _authorizationService.IsStudentEnrolledInSectionSubjectAsync(
                    studentId,
                    sectionId,
                    qrCode.Session.Schedule.SubjectId
                ).ConfigureAwait(false);
            }

            if (!isStudentAuthorized)
            {
                _logger.LogWarning("QR code scan failed: Student {StudentId} is not authorized for section {SectionId}",
                    studentId, sectionId);
                return CreateUnauthorizedStudentResponse(student);
            }

            _logger.LogDebug("Checking for duplicate attendance: StudentId={StudentId}, SessionId={SessionId}",
                studentId, qrCode.SessionId);

            var existingRecord = await _attendanceRepository.HasAttendanceRecordAsync(
                studentId,
                qrCode.SessionId
            ).ConfigureAwait(false);

            if (existingRecord)
            {
                _logger.LogWarning("Duplicate QR scan detected for student ID: {StudentId}, session ID: {SessionId}",
                    studentId, qrCode.SessionId);

                var remainingScansForDuplicate = qrCode.MaxUsage.HasValue
                    ? Math.Max(0, qrCode.MaxUsage.Value - qrCode.UsageCount)
                    : int.MaxValue;

                return CreateDuplicateScanResponse(qrCode, student, localNow, remainingScansForDuplicate);
            }

            await using var transaction = await _qrCodeRepository.BeginTransactionAsync().ConfigureAwait(false);

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
                    studentId, qrCode.SessionId);

                var attendanceRecord = await _attendanceService.CreateAttendanceFromQrScanAsync(
                    studentId,
                    qrCode.SessionId,
                    qrCode.Id,
                    localNow
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
                    studentId, attendanceRecord.Id);

                return CreateSuccessfulScanResponse(
                    qrCode,
                    student,
                    attendanceRecord.CheckInTime,
                    remainingScans,
                    attendanceRecord.Id,
                    attendanceRecord.Status);
            }
            catch (InvalidOperationException ex) when (ex.Message.Contains("duplicate"))
            {
                await transaction.RollbackAsync().ConfigureAwait(false);

                _logger.LogWarning("Duplicate detected during attendance creation (edge case): StudentId={StudentId}, SessionId={SessionId}",
                    studentId, qrCode.SessionId);

                var remainingScansEdge = qrCode.MaxUsage.HasValue
                    ? Math.Max(0, qrCode.MaxUsage.Value - qrCode.UsageCount)
                    : int.MaxValue;

                return CreateDuplicateScanResponse(qrCode, student, localNow, remainingScansEdge);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to scan QR code {QrHash} for student {StudentId}",
                validateQrCode.QrHash, resolvedStudentId ?? validateQrCode.StudentId);
            return new QrCodeScanResponseDto
            {
                Success = false,
                Message = "An error occurred while processing the QR code scan",
                AttendanceMarked = false
            };
        }
    }

    private static QrCodeScanResponseDto CreateUnauthorizedStudentResponse(Student student)
        => new()
        {
            Success = false,
            Message = "You are not enrolled in this section or subject",
            AttendanceMarked = false,
            StudentName = GetStudentName(student)
        };

    private static QrCodeScanResponseDto CreateDuplicateScanResponse(
        QrCodeEntity qrCode,
        Student student,
        DateTime attendanceTime,
        int remainingScans)
        => CreateScanResponse(
            qrCode,
            student,
            success: true,
            message: "You have already checked in for this session",
            attendanceMarked: false,
            attendanceTime: attendanceTime,
            remainingScans: remainingScans,
            isDuplicateScan: true);

    private static QrCodeScanResponseDto CreateSuccessfulScanResponse(
        QrCodeEntity qrCode,
        Student student,
        DateTime attendanceTime,
        int remainingScans,
        int attendanceRecordId,
        string? attendanceStatus)
        => CreateScanResponse(
            qrCode,
            student,
            success: true,
            message: "Attendance marked successfully",
            attendanceMarked: true,
            attendanceTime: attendanceTime,
            remainingScans: remainingScans,
            attendanceRecordId: attendanceRecordId,
            attendanceStatus: attendanceStatus,
            isDuplicateScan: false);

    private static QrCodeScanResponseDto CreateScanResponse(
        QrCodeEntity qrCode,
        Student student,
        bool success,
        string message,
        bool attendanceMarked,
        DateTime attendanceTime,
        int remainingScans,
        bool isDuplicateScan,
        int? attendanceRecordId = null,
        string? attendanceStatus = null)
        => new()
        {
            Success = success,
            Message = message,
            AttendanceMarked = attendanceMarked,
            AttendanceTime = attendanceTime,
            StudentName = GetStudentName(student),
            ClassName = qrCode.Session?.Schedule?.Section?.Name ?? "Unknown",
            SubjectName = qrCode.Session?.Schedule?.Subject?.Name ?? "Unknown",
            RoomName = qrCode.Session?.ActualRoom?.Name ?? "Unknown",
            InstructorName = GetInstructorName(qrCode),
            RemainingScans = remainingScans,
            AttendanceRecordId = attendanceRecordId,
            AttendanceStatus = attendanceStatus,
            IsDuplicateScan = isDuplicateScan
        };

    private static string GetStudentName(Student student)
        => $"{student.Firstname} {student.Lastname}";

    private static string GetInstructorName(QrCodeEntity qrCode)
        => qrCode.Session?.Schedule?.Instructor != null
            ? $"{qrCode.Session.Schedule.Instructor.Firstname} {qrCode.Session.Schedule.Instructor.Lastname}"
            : "Unknown";
}
