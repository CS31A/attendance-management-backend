using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using Microsoft.EntityFrameworkCore;
using attendance_monitoring.Classes;
using attendance_monitoring.Constants;
using attendance_monitoring.Data;
using attendance_monitoring.Exceptions;
using attendance_monitoring.Helpers;
using attendance_monitoring.IRepository;
using attendance_monitoring.IServices;
using attendance_monitoring.Models.DTO.Request;
using attendance_monitoring.Models.DTO.Response;

namespace attendance_monitoring.Services;

/// <summary>
/// Service class for managing QR code operations for attendance tracking.
/// </summary>
public class QrCodeService : IQrCodeService
{
    private readonly IQrCodeRepository _qrCodeRepository;
    private readonly IScheduleRepository _scheduleRepository;
    private readonly ISectionRepository _sectionRepository;
    private readonly IClassroomRepository _classroomRepository;
    private readonly IStudentRepository _studentRepository;
    private readonly IStudentEnrollmentService _studentEnrollmentService;
    private readonly UserContextService _userContextService;
    private readonly ISessionRepository _sessionRepository;
    private readonly IAttendanceService _attendanceService;
    private readonly IAttendanceRepository _attendanceRepository;
    private readonly ApplicationDbContext _context;
    private readonly ILogger<QrCodeService> _logger;
    private readonly INotificationService _notificationService;

    /// <summary>
    /// Initializes a new instance of the QrCodeService class.
    /// </summary>
    public QrCodeService(
        IQrCodeRepository qrCodeRepository,
        IScheduleRepository scheduleRepository,
        ISectionRepository sectionRepository,
        IClassroomRepository classroomRepository,
        IStudentRepository studentRepository,
        IStudentEnrollmentService studentEnrollmentService,
        UserContextService userContextService,
        ISessionRepository sessionRepository,
        IAttendanceService attendanceService,
        IAttendanceRepository attendanceRepository,
        ApplicationDbContext context,
        ILogger<QrCodeService> logger,
        INotificationService notificationService)
    {
        _qrCodeRepository = qrCodeRepository ?? throw new ArgumentNullException(nameof(qrCodeRepository));
        _scheduleRepository = scheduleRepository ?? throw new ArgumentNullException(nameof(scheduleRepository));
        _sectionRepository = sectionRepository ?? throw new ArgumentNullException(nameof(sectionRepository));
        _classroomRepository = classroomRepository ?? throw new ArgumentNullException(nameof(classroomRepository));
        _studentRepository = studentRepository ?? throw new ArgumentNullException(nameof(studentRepository));
        _studentEnrollmentService = studentEnrollmentService ?? throw new ArgumentNullException(nameof(studentEnrollmentService));
        _userContextService = userContextService ?? throw new ArgumentNullException(nameof(userContextService));
        _sessionRepository = sessionRepository ?? throw new ArgumentNullException(nameof(sessionRepository));
        _attendanceService = attendanceService ?? throw new ArgumentNullException(nameof(attendanceService));
        _attendanceRepository = attendanceRepository ?? throw new ArgumentNullException(nameof(attendanceRepository));
        _context = context ?? throw new ArgumentNullException(nameof(context));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _notificationService = notificationService ?? throw new ArgumentNullException(nameof(notificationService));
    }

    #region Read Operations

    public async Task<QrCodeResponseDto?> GetQrCodeByIdAsync(int id)
    {
        try
        {
            _logger.LogInformation("Retrieving QR code with ID: {QrCodeId}", id);

            var qrCode = await _qrCodeRepository.GetQrCodeByIdAsync(id).ConfigureAwait(false);
            if (qrCode == null)
            {
                _logger.LogWarning("QR code with ID {QrCodeId} not found", id);
                return null;
            }

            var responseDto = MapToResponseDto(qrCode);
            _logger.LogInformation("Successfully retrieved QR code with ID: {QrCodeId}", id);
            return responseDto;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error occurred while retrieving QR code with ID: {QrCodeId}", id);
            throw new EntityServiceException("QrCode", $"GetQrCodeById: {id}", "An error occurred while retrieving the QR code", ex);
        }
    }

    public async Task<QrCodeResponseDto?> GetQrCodeByHashAsync(string qrHash)
    {
        try
        {
            _logger.LogInformation("Retrieving QR code with hash: {QrHash}", qrHash);

            var qrCode = await _qrCodeRepository.GetQrCodeByHashAsync(qrHash).ConfigureAwait(false);
            if (qrCode == null)
            {
                _logger.LogWarning("QR code with hash {QrHash} not found", qrHash);
                return null;
            }

            var responseDto = MapToResponseDto(qrCode);
            _logger.LogInformation("Successfully retrieved QR code with hash: {QrHash}", qrHash);
            return responseDto;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error occurred while retrieving QR code with hash: {QrHash}", qrHash);
            throw new EntityServiceException("QrCode", $"GetQrCodeByHash: {qrHash}", "An error occurred while retrieving the QR code", ex);
        }
    }

    public async Task<IEnumerable<QrCodeResponseDto>> GetQrCodesByScheduleIdAsync(int scheduleId)
    {
        try
        {
            _logger.LogInformation("Retrieving QR codes for schedule ID: {ScheduleId}", scheduleId);

            var qrCodes = await _qrCodeRepository.GetQrCodesByScheduleIdAsync(scheduleId).ConfigureAwait(false);
            var responseDtos = new List<QrCodeResponseDto>();

            foreach (var qrCode in qrCodes)
            {
                var responseDto = MapToResponseDto(qrCode);
                responseDtos.Add(responseDto);
            }

            _logger.LogInformation("Successfully retrieved {Count} QR codes for schedule ID: {ScheduleId}", responseDtos.Count, scheduleId);
            return responseDtos;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error occurred while retrieving QR codes for schedule ID: {ScheduleId}", scheduleId);
            throw new EntityServiceException("QrCode", $"GetQrCodesByScheduleId: {scheduleId}", "An error occurred while retrieving QR codes", ex);
        }
    }

    public async Task<IEnumerable<QrCodeResponseDto>> GetQrCodesBySectionIdAsync(int sectionId)
    {
        try
        {
            _logger.LogInformation("Retrieving QR codes for section ID: {SectionId}", sectionId);

            var qrCodes = await _qrCodeRepository.GetQrCodesBySectionIdAsync(sectionId).ConfigureAwait(false);
            var responseDtos = new List<QrCodeResponseDto>();

            foreach (var qrCode in qrCodes)
            {
                var responseDto = MapToResponseDto(qrCode);
                responseDtos.Add(responseDto);
            }

            _logger.LogInformation("Successfully retrieved {Count} QR codes for section ID: {SectionId}", responseDtos.Count, sectionId);
            return responseDtos;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error occurred while retrieving QR codes for section ID: {SectionId}", sectionId);
            throw new EntityServiceException("QrCode", $"GetQrCodesBySectionId: {sectionId}", "An error occurred while retrieving QR codes", ex);
        }
    }

    public async Task<IEnumerable<QrCodeResponseDto>> GetQrCodesBySessionIdAsync(int sessionId)
    {
        try
        {
            _logger.LogInformation("Retrieving QR codes for session ID: {SessionId}", sessionId);

            var qrCodes = await _qrCodeRepository.GetQrCodesBySessionIdAsync(sessionId).ConfigureAwait(false);
            var responseDtos = new List<QrCodeResponseDto>();

            foreach (var qrCode in qrCodes)
            {
                var responseDto = MapToResponseDto(qrCode);
                responseDtos.Add(responseDto);
            }

            _logger.LogInformation("Successfully retrieved {Count} QR codes for session ID: {SessionId}", responseDtos.Count, sessionId);
            return responseDtos;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error occurred while retrieving QR codes for session ID: {SessionId}", sessionId);
            throw new EntityServiceException("QrCode", $"GetQrCodesBySessionId: {sessionId}", "An error occurred while retrieving QR codes", ex);
        }
    }

    public async Task<IEnumerable<QrCodeResponseDto>> GetActiveQrCodesAsync()
    {
        try
        {
            _logger.LogInformation("Retrieving all active QR codes");

            var qrCodes = await _qrCodeRepository.GetActiveQrCodesAsync().ConfigureAwait(false);
            var responseDtos = new List<QrCodeResponseDto>();

            foreach (var qrCode in qrCodes)
            {
                var responseDto = MapToResponseDto(qrCode);
                responseDtos.Add(responseDto);
            }

            _logger.LogInformation("Successfully retrieved {Count} active QR codes", responseDtos.Count);
            return responseDtos;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error occurred while retrieving active QR codes");
            throw new EntityServiceException("QrCode", "GetActiveQrCodes", "An error occurred while retrieving active QR codes", ex);
        }
    }

    public async Task<QrCodeScanHistoryResponseDto> GetScanHistoryAsync(
        int qrCodeId,
        int instructorId,
        string userRole,
        int pageNumber = 1,
        int pageSize = 50)
    {
        _logger.LogInformation(
            "Retrieving scan history for QR code {QrCodeId} by instructor {InstructorId} with role {UserRole}",
            qrCodeId, instructorId, userRole);

        try
        {
            // Validate pagination parameters
            if (pageNumber < 1)
            {
                throw new ValidationException("Page number must be greater than 0");
            }

            if (pageSize < 1 || pageSize > 100)
            {
                throw new ValidationException("Page size must be between 1 and 100");
            }

            // Get the QR code
            var qrCode = await _qrCodeRepository.GetQrCodeByIdAsync(qrCodeId);
            if (qrCode == null)
            {
                throw new EntityNotFoundException<int>("QrCode", qrCodeId);
            }

            // Authorization check - only instructors/admins for their own sections
            if (userRole == RoleConstants.Instructor)
            {
                var authSession = await _context.Sessions
                    .Include(s => s.Schedule)
                    .FirstOrDefaultAsync(s => s.Id == qrCode.SessionId);

                if (authSession == null || authSession.Schedule.InstructorId != instructorId)
                {
                    throw new EntityUnauthorizedException(
                        "QrCode",
                        qrCodeId.ToString(),
                        "You do not have permission to view scan history for this QR code");
                }
            }

            // Get scan history from repository
            var scanHistory = await _qrCodeRepository.GetScanHistoryAsync(
                qrCodeId, pageNumber, pageSize);

            // Get statistics
            var statistics = await _qrCodeRepository.GetScanStatisticsAsync(qrCodeId);

            // Build QR code info DTO
            var session = await _context.Sessions
                .Include(s => s.Schedule)
                    .ThenInclude(sch => sch!.Subject)
                .FirstOrDefaultAsync(s => s.Id == qrCode.SessionId);

            var qrCodeInfo = new QrCodeInfoDto
            {
                Id = qrCode.Id,
                Hash = qrCode.QrHash,
                SessionId = qrCode.SessionId,
                SessionDate = session?.SessionDate ?? DateTime.MinValue,
                SessionSubject = session?.Schedule?.Subject?.Name ?? "Unknown",
                GeneratedAt = qrCode.GeneratedAt,
                ExpiresAt = qrCode.ExpiresAt,
                IsActive = qrCode.IsActive,
                TotalScans = statistics.totalScans,
                UniqueStudents = statistics.uniqueStudents
            };

            // Calculate proper average scan time from all records
            DateTime? averageScanTime = null;
            if (statistics.totalScans > 0)
            {
                var allScanTimes = await _context.AttendanceRecords
                    .Where(ar => ar.QrCodeId == qrCodeId)
                    .Where(ar => !ar.Student.IsDeleted)
                    .Select(ar => ar.CheckInTime)
                    .ToListAsync();

                if (allScanTimes.Any())
                {
                    // Calculate average by summing all ticks and dividing by count
                    var totalTicks = allScanTimes.Sum(dt => dt.Ticks);
                    var avgTicks = totalTicks / allScanTimes.Count;
                    averageScanTime = new DateTime(avgTicks);
                }
            }

            // Build statistics DTO
            var scanStatistics = new QrCodeStatisticsDto
            {
                TotalScans = statistics.totalScans,
                UniqueStudents = statistics.uniqueStudents,
                PresentCount = statistics.statusBreakdown.GetValueOrDefault("Present", 0),
                LateCount = statistics.statusBreakdown.GetValueOrDefault("Late", 0),
                ExcusedCount = statistics.statusBreakdown.GetValueOrDefault("Excused", 0),
                FirstScanAt = statistics.firstScan,
                LastScanAt = statistics.lastScan,
                AverageScanTime = averageScanTime
            };

            // Map attendance records to DTOs
            var scanItems = scanHistory.Items.Select(ar => new QrCodeScanHistoryItemDto
            {
                AttendanceRecordId = ar.Id,
                StudentId = ar.StudentId,
                StudentEmail = ar.Student?.User?.Email ?? "Unknown",
                StudentName = $"{ar.Student?.Firstname} {ar.Student?.Lastname}".Trim(),
                ScannedAt = ar.CheckInTime,
                Status = ar.Status.ToString(),
                SessionId = ar.SessionId,
                SessionDate = ar.Session?.SessionDate ?? DateTime.MinValue,
                SessionSubject = ar.Session?.Schedule?.Subject?.Name ?? "Unknown",
                SessionSection = ar.Session?.Schedule?.Section?.Name ?? "Unknown"
            }).ToList();

            // Build paginated result
            var pagedScans = new PagedResult<QrCodeScanHistoryItemDto>
            {
                Items = scanItems,
                TotalCount = scanHistory.TotalCount,
                PageNumber = scanHistory.PageNumber,
                PageSize = scanHistory.PageSize
            };

            _logger.LogInformation(
                "Successfully retrieved {Count} scan records for QR code {QrCodeId}",
                scanItems.Count, qrCodeId);

            return new QrCodeScanHistoryResponseDto
            {
                QrCodeInfo = qrCodeInfo,
                ScanStatistics = scanStatistics,
                Scans = pagedScans
            };
        }
        catch (ValidationException)
        {
            throw;
        }
        catch (EntityNotFoundException<int>)
        {
            throw;
        }
        catch (EntityUnauthorizedException)
        {
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error occurred while retrieving scan history for QR code {QrCodeId}", qrCodeId);
            throw new EntityServiceException("QrCode", "GetScanHistory", "An error occurred while retrieving scan history", ex);
        }
    }

    public async Task<QrCodeScanHistoryResponseDto> GetScanHistoryByHashAsync(
        string qrHash,
        int instructorId,
        string userRole,
        int pageNumber = 1,
        int pageSize = 50)
    {
        _logger.LogInformation(
            "Retrieving scan history for QR hash {QrHash} by instructor {InstructorId}",
            qrHash, instructorId);

        try
        {
            // Get QR code by hash
            var qrCode = await _qrCodeRepository.GetQrCodeByHashAsync(qrHash);
            if (qrCode == null)
            {
                throw new EntityNotFoundException<string>("QrCode", qrHash);
            }

            // Delegate to ID-based method
            return await GetScanHistoryAsync(qrCode.Id, instructorId, userRole, pageNumber, pageSize);
        }
        catch (ValidationException)
        {
            throw;
        }
        catch (EntityNotFoundException<int>)
        {
            throw;
        }
        catch (EntityNotFoundException<string>)
        {
            throw;
        }
        catch (EntityUnauthorizedException)
        {
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error occurred while retrieving scan history for QR hash {QrHash}", qrHash);
            throw new EntityServiceException("QrCode", "GetScanHistoryByHash", "An error occurred while retrieving scan history", ex);
        }
    }

    #endregion

    #region Write Operations

    public async Task<QrCodeResponseDto> CreateQrCodeAsync(CreateQrCode createQrCode, ClaimsPrincipal user)
    {
        const int maxRetries = 3;
        const int retryDelayMs = 100;

        for (int attempt = 1; attempt <= maxRetries; attempt++)
        {
            try
            {
                _logger.LogInformation("Creating QR code for session ID: {SessionId} (Attempt {Attempt})",
                    createQrCode.SessionId, attempt);

                // Validate user authorization
                var userId = await _userContextService.GetUserIdAsync(user).ConfigureAwait(false);
                if (string.IsNullOrEmpty(userId))
                {
                    _logger.LogWarning("QR code creation failed: User ID not found in token");
                    throw new ValidationException("User ID not found in token");
                }

                var isAuthorized = await _userContextService.IsAuthorizedAsync(user, userId, RoleConstants.Admin, RoleConstants.Instructor).ConfigureAwait(false);
                if (!isAuthorized)
                {
                    _logger.LogWarning("QR code creation failed: User not authorized");
                    throw new EntityUnauthorizedException("QrCode", "Create", userId);
                }

                // Validate session exists and is active
                await ValidateSessionExistsOrThrowAsync(createQrCode.SessionId).ConfigureAwait(false);

                // Create QR code entity
                var qrCode = new QrCode
                {
                    SessionId = createQrCode.SessionId,
                    QrHash = createQrCode.QrHash,
                    ExpiresAt = createQrCode.ExpiresAt,
                    MaxUsage = createQrCode.MaxUsage
                };

                var createdQrCode = await _qrCodeRepository.CreateQrCodeAsync(qrCode).ConfigureAwait(false);
                await _qrCodeRepository.SaveChangesAsync().ConfigureAwait(false);

                var responseDto = MapToResponseDto(createdQrCode);
                _logger.LogInformation("Successfully created QR code with ID: {QrCodeId}", createdQrCode.Id);

                return responseDto;
            }
            catch (DbUpdateException ex) when (ExceptionHandlingHelper.IsUniqueConstraintViolation(ex) && attempt < maxRetries)
            {
                _logger.LogWarning(ex, "Hash collision on attempt {Attempt}/{MaxRetries} for session {SessionId}, retrying...",
                    attempt, maxRetries, createQrCode.SessionId);

                // Exponential backoff with jitter to prevent thundering herd
                var jitter = Random.Shared.Next(0, 50);
                await Task.Delay(retryDelayMs * attempt + jitter).ConfigureAwait(false);

                // Regenerate hash WITHOUT database check - let the constraint catch duplicates
                var entropy = $"{createQrCode.SessionId}{attempt}{DateTime.UtcNow.Ticks}";
                createQrCode.QrHash = GenerateQrHash(entropy);
            }
            catch (DbUpdateException ex) when (ExceptionHandlingHelper.IsUniqueConstraintViolation(ex))
            {
                _logger.LogError(ex, "Failed to create QR code after {MaxRetries} attempts due to hash collision", maxRetries);
                throw new EntityServiceException("QrCode", "Create", "Unable to create QR code due to high system load. Please try again in a moment.", ex);
            }
            catch (ValidationException) { throw; }
            catch (EntityUnauthorizedException) { throw; }
            catch (EntityNotFoundException<int>) { throw; }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while creating QR code");
                throw new EntityServiceException("QrCode", "Create", "An error occurred while creating the QR code", ex);
            }
        }

        throw new EntityServiceException("QrCode", "Create", "Failed to create QR code after multiple attempts");
    }

    public async Task<QrCodeGenerationResponseDto> GenerateQrCodeAsync(QrCodeRequest qrCodeRequest, ClaimsPrincipal user)
    {
        const int maxRetries = 3;
        var attempts = 0;

        while (attempts < maxRetries)
        {
            try
            {
                _logger.LogInformation("Generating QR code for session ID: {SessionId} (Attempt {Attempt})",
                                    qrCodeRequest.SessionId, attempts + 1);

                // Validate user authorization
                var userId = await _userContextService.GetUserIdAsync(user).ConfigureAwait(false);
                if (string.IsNullOrEmpty(userId))
                {
                    _logger.LogWarning("QR code generation failed: User ID not found in token");
                    return new QrCodeGenerationResponseDto { Success = false, Message = "User ID not found in token" };
                }

                var isAuthorized = await _userContextService.IsAuthorizedAsync(user, userId, RoleConstants.Admin, RoleConstants.Instructor).ConfigureAwait(false);
                if (!isAuthorized)
                {
                    _logger.LogWarning("QR code generation failed: User not authorized");
                    return new QrCodeGenerationResponseDto { Success = false, Message = "You are not authorized to generate QR codes" };
                }

                // Validate session exists and is active
                var validationError = await ValidateSessionExistsAsync(qrCodeRequest.SessionId).ConfigureAwait(false);
                if (validationError != null)
                {
                    return new QrCodeGenerationResponseDto { Success = false, Message = validationError };
                }

                // Combine client hash with server-generated hash for uniqueness
                var qrHash = await GenerateUniqueQrHashAsync(qrCodeRequest.UniqueHash).ConfigureAwait(false);
                var expiresAt = DateTime.UtcNow.AddMinutes(qrCodeRequest.ExpirationMinutes);

                // Create QR code entity
                var qrCode = new QrCode
                {
                    SessionId = qrCodeRequest.SessionId,
                    QrHash = qrHash,
                    ExpiresAt = expiresAt,
                    MaxUsage = qrCodeRequest.MaxUsage
                };

                var createdQrCode = await _qrCodeRepository.CreateQrCodeAsync(qrCode).ConfigureAwait(false);
                await _qrCodeRepository.SaveChangesAsync().ConfigureAwait(false);

                _logger.LogInformation("Successfully generated QR code with ID: {QrCodeId}", createdQrCode.Id);

                // Send real-time notification to instructor
                await _notificationService.NotifyQrCodeGeneratedAsync(createdQrCode.Id, userId).ConfigureAwait(false);

                return new QrCodeGenerationResponseDto
                {
                    Success = true,
                    Message = "QR code generated successfully",
                    QrHash = qrHash,
                    QrCodeData = qrHash, // In a real implementation, this would be the actual QR code URL/data
                    GeneratedAt = createdQrCode.GeneratedAt,
                    ExpiresAt = expiresAt,
                    MaxUsage = qrCodeRequest.MaxUsage,
                    QrCodeId = createdQrCode.Id
                };
            }
            catch (DbUpdateException ex) when (ex.InnerException?.Message.Contains("UNIQUE constraint failed") ?? false)
            {
                attempts++;
                _logger.LogWarning(ex, "Attempt {Attempt} to generate a unique QR hash failed due to a database constraint. Retrying...", attempts);
                if (attempts >= maxRetries)
                {
                    _logger.LogError("Failed to generate a unique QR hash after {MaxRetries} attempts.", maxRetries);
                    return new QrCodeGenerationResponseDto { Success = false, Message = "Failed to generate a unique QR hash. Please try again." };
                }
                // Wait for a short, random interval before retrying to reduce collision likelihood
                await Task.Delay(TimeSpan.FromMilliseconds(50 + new Random().Next(0, 100)));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "An unexpected error occurred while generating QR code");
                return new QrCodeGenerationResponseDto { Success = false, Message = "An unexpected error occurred while generating the QR code" };
            }
        }

        return new QrCodeGenerationResponseDto
        {
            Success = false,
            Message = "Failed to generate a unique QR hash after multiple attempts"
        };
    }

    public async Task<QrCodeResponseDto> UpdateQrCodeAsync(int id, UpdateQrCode updateQrCode, ClaimsPrincipal user)
    {
        try
        {
            _logger.LogInformation("Updating QR code with ID: {QrCodeId}", id);

            // Validate user authorization
            var userId = await _userContextService.GetUserIdAsync(user).ConfigureAwait(false);
            if (string.IsNullOrEmpty(userId))
            {
                _logger.LogWarning("QR code update failed: User ID not found in token");
                throw new ValidationException("User ID not found in token");
            }

            var isAuthorized = await _userContextService.IsAuthorizedAsync(user, userId, RoleConstants.Admin, RoleConstants.Instructor).ConfigureAwait(false);
            if (!isAuthorized)
            {
                _logger.LogWarning("QR code update failed: User not authorized");
                throw new EntityUnauthorizedException("QrCode", "Update", userId);
            }

            // Get existing QR code
            var existingQrCode = await _qrCodeRepository.GetQrCodeByIdAsync(id).ConfigureAwait(false);
            if (existingQrCode == null)
            {
                _logger.LogWarning("QR code update failed: QR code not found");
                throw new EntityNotFoundException<int>("QrCode", id);
            }

            // Apply updates
            if (updateQrCode.ExpiresAt.HasValue)
            {
                existingQrCode.ExpiresAt = updateQrCode.ExpiresAt.Value;
            }

            if (updateQrCode.IsActive.HasValue)
            {
                existingQrCode.IsActive = updateQrCode.IsActive.Value;
            }

            if (updateQrCode.MaxUsage.HasValue)
            {
                existingQrCode.MaxUsage = updateQrCode.MaxUsage.Value;
            }

            var updatedQrCode = await _qrCodeRepository.UpdateQrCodeAsync(existingQrCode).ConfigureAwait(false);
            await _qrCodeRepository.SaveChangesAsync().ConfigureAwait(false);

            var responseDto = MapToResponseDto(updatedQrCode);
            _logger.LogInformation("Successfully updated QR code with ID: {QrCodeId}", id);

            return responseDto;
        }
        catch (ValidationException) { throw; }
        catch (EntityUnauthorizedException) { throw; }
        catch (EntityNotFoundException<int>) { throw; }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error occurred while updating QR code");
            throw new EntityServiceException("QrCode", "Update", "An error occurred while updating the QR code", ex);
        }
    }

    public async Task DeactivateQrCodeAsync(int id, ClaimsPrincipal user)
    {
        try
        {
            _logger.LogInformation("Deactivating QR code with ID: {QrCodeId}", id);

            // Validate user authorization
            var userId = await _userContextService.GetUserIdAsync(user).ConfigureAwait(false);
            if (string.IsNullOrEmpty(userId))
            {
                _logger.LogWarning("QR code deactivation failed: User ID not found in token");
                throw new ValidationException("User ID not found in token");
            }

            var isAuthorized = await _userContextService.IsAuthorizedAsync(user, userId, RoleConstants.Admin, RoleConstants.Instructor).ConfigureAwait(false);
            if (!isAuthorized)
            {
                _logger.LogWarning("QR code deactivation failed: User not authorized");
                throw new EntityUnauthorizedException("QrCode", "Deactivate", userId);
            }

            var result = await _qrCodeRepository.DeactivateQrCodeAsync(id).ConfigureAwait(false);
            if (!result)
            {
                _logger.LogWarning("QR code deactivation failed: QR code not found");
                throw new EntityNotFoundException<int>("QrCode", id);
            }

            await _qrCodeRepository.SaveChangesAsync().ConfigureAwait(false);
            _logger.LogInformation("Successfully deactivated QR code with ID: {QrCodeId}", id);
        }
        catch (ValidationException) { throw; }
        catch (EntityUnauthorizedException) { throw; }
        catch (EntityNotFoundException<int>) { throw; }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error occurred while deactivating QR code");
            throw new EntityServiceException("QrCode", "Deactivate", "An error occurred while deactivating the QR code", ex);
        }
    }

    public async Task RevokeQrCodeAsync(int id, string? reason, ClaimsPrincipal user)
    {
        try
        {
            _logger.LogInformation("Revoking QR code with ID: {QrCodeId}", id);

            // Validate user authorization
            var userId = await _userContextService.GetUserIdAsync(user).ConfigureAwait(false);
            if (string.IsNullOrEmpty(userId))
            {
                _logger.LogWarning("QR code revocation failed: User ID not found in token");
                throw new ValidationException("User ID not found in token");
            }

            var isAuthorized = await _userContextService.IsAuthorizedAsync(user, userId, RoleConstants.Admin, RoleConstants.Instructor).ConfigureAwait(false);
            if (!isAuthorized)
            {
                _logger.LogWarning("QR code revocation failed: User not authorized");
                throw new EntityUnauthorizedException("QrCode", "Revoke", userId);
            }

            // Get QR code and add audit trail
            var qrCode = await _qrCodeRepository.GetQrCodeByIdAsync(id).ConfigureAwait(false);
            if (qrCode == null)
            {
                _logger.LogWarning("QR code revocation failed: QR code not found");
                throw new EntityNotFoundException<int>("QrCode", id);
            }

            // Set revocation audit trail
            qrCode.IsActive = false;
            qrCode.RevokedAt = DateTime.UtcNow;
            qrCode.RevokedBy = userId;
            qrCode.RevocationReason = reason;
            qrCode.UpdatedAt = DateTime.UtcNow;

            await _qrCodeRepository.UpdateQrCodeAsync(qrCode).ConfigureAwait(false);
            await _qrCodeRepository.SaveChangesAsync().ConfigureAwait(false);

            _logger.LogInformation("Successfully revoked QR code with ID: {QrCodeId} by user: {UserId}", id, userId);
        }
        catch (ValidationException) { throw; }
        catch (EntityUnauthorizedException) { throw; }
        catch (EntityNotFoundException<int>) { throw; }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error occurred while revoking QR code");
            throw new EntityServiceException("QrCode", "Revoke", "An error occurred while revoking the QR code", ex);
        }
    }

    public async Task DeactivateQrCodeByHashAsync(string qrHash, ClaimsPrincipal user)
    {
        try
        {
            _logger.LogInformation("Deactivating QR code with hash: {QrHash}", qrHash);

            // Validate user authorization
            var userId = await _userContextService.GetUserIdAsync(user).ConfigureAwait(false);
            if (string.IsNullOrEmpty(userId))
            {
                _logger.LogWarning("QR code deactivation failed: User ID not found in token");
                throw new ValidationException("User ID not found in token");
            }

            var isAuthorized = await _userContextService.IsAuthorizedAsync(user, userId, RoleConstants.Admin, RoleConstants.Instructor).ConfigureAwait(false);
            if (!isAuthorized)
            {
                _logger.LogWarning("QR code deactivation failed: User not authorized");
                throw new EntityUnauthorizedException("QrCode", "Deactivate", userId);
            }

            var result = await _qrCodeRepository.DeactivateQrCodeByHashAsync(qrHash).ConfigureAwait(false);
            if (!result)
            {
                _logger.LogWarning("QR code deactivation failed: QR code not found");
                throw new EntityNotFoundException<string>("QrCode", qrHash);
            }

            await _qrCodeRepository.SaveChangesAsync().ConfigureAwait(false);
            _logger.LogInformation("Successfully deactivated QR code with hash: {QrHash}", qrHash);
        }
        catch (ValidationException) { throw; }
        catch (EntityUnauthorizedException) { throw; }
        catch (EntityNotFoundException<string>) { throw; }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error occurred while deactivating QR code");
            throw new EntityServiceException("QrCode", "Deactivate", "An error occurred while deactivating the QR code", ex);
        }
    }

    public async Task RevokeQrCodeByHashAsync(string qrHash, string? reason, ClaimsPrincipal user)
    {
        try
        {
            _logger.LogInformation("Revoking QR code with hash: {QrHash}", qrHash);

            // Validate user authorization
            var userId = await _userContextService.GetUserIdAsync(user).ConfigureAwait(false);
            if (string.IsNullOrEmpty(userId))
            {
                _logger.LogWarning("QR code revocation failed: User ID not found in token");
                throw new ValidationException("User ID not found in token");
            }

            var isAuthorized = await _userContextService.IsAuthorizedAsync(user, userId, RoleConstants.Admin, RoleConstants.Instructor).ConfigureAwait(false);
            if (!isAuthorized)
            {
                _logger.LogWarning("QR code revocation failed: User not authorized");
                throw new EntityUnauthorizedException("QrCode", "Revoke", userId);
            }

            // Get QR code and add audit trail
            var qrCode = await _qrCodeRepository.GetQrCodeByHashAsync(qrHash).ConfigureAwait(false);
            if (qrCode == null)
            {
                _logger.LogWarning("QR code revocation failed: QR code not found");
                throw new EntityNotFoundException<string>("QrCode", qrHash);
            }

            // Set revocation audit trail
            qrCode.IsActive = false;
            qrCode.RevokedAt = DateTime.UtcNow;
            qrCode.RevokedBy = userId;
            qrCode.RevocationReason = reason;
            qrCode.UpdatedAt = DateTime.UtcNow;

            await _qrCodeRepository.UpdateQrCodeAsync(qrCode).ConfigureAwait(false);
            await _qrCodeRepository.SaveChangesAsync().ConfigureAwait(false);

            _logger.LogInformation("Successfully revoked QR code with hash: {QrHash} by user: {UserId}", qrHash, userId);
        }
        catch (ValidationException) { throw; }
        catch (EntityUnauthorizedException) { throw; }
        catch (EntityNotFoundException<string>) { throw; }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error occurred while revoking QR code");
            throw new EntityServiceException("QrCode", "Revoke", "An error occurred while revoking the QR code", ex);
        }
    }

    public async Task ReactivateQrCodeAsync(int id, ClaimsPrincipal user)
    {
        try
        {
            _logger.LogInformation("Reactivating QR code with ID: {QrCodeId}", id);

            // Validate user authorization
            var userId = await _userContextService.GetUserIdAsync(user).ConfigureAwait(false);
            if (string.IsNullOrEmpty(userId))
            {
                _logger.LogWarning("QR code reactivation failed: User ID not found in token");
                throw new ValidationException("User ID not found in token");
            }

            var isAuthorized = await _userContextService.IsAuthorizedAsync(user, userId, RoleConstants.Admin, RoleConstants.Instructor).ConfigureAwait(false);
            if (!isAuthorized)
            {
                _logger.LogWarning("QR code reactivation failed: User not authorized");
                throw new EntityUnauthorizedException("QrCode", "Reactivate", userId);
            }

            // Check if QR code exists and is not expired
            var qrCode = await _qrCodeRepository.GetQrCodeByIdAsync(id).ConfigureAwait(false);
            if (qrCode == null)
            {
                _logger.LogWarning("QR code reactivation failed: QR code not found");
                throw new EntityNotFoundException<int>("QrCode", id);
            }

            if (qrCode.ExpiresAt <= DateTime.UtcNow)
            {
                _logger.LogWarning("QR code reactivation failed: QR code has expired");
                throw new ValidationException("Cannot reactivate an expired QR code");
            }

            var result = await _qrCodeRepository.ReactivateQrCodeAsync(id).ConfigureAwait(false);
            if (!result)
            {
                _logger.LogWarning("QR code reactivation failed: QR code not found");
                throw new EntityNotFoundException<int>("QrCode", id);
            }

            await _qrCodeRepository.SaveChangesAsync().ConfigureAwait(false);
            _logger.LogInformation("Successfully reactivated QR code with ID: {QrCodeId} by user: {UserId}", id, userId);
        }
        catch (ValidationException)
        {
            throw;
        }
        catch (EntityNotFoundException<int>)
        {
            throw;
        }
        catch (EntityUnauthorizedException)
        {
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error occurred while reactivating QR code");
            throw ExceptionHandlingHelper.CreateServiceException("QrCode", "Reactivate", ex);
        }
    }

    public async Task ReactivateQrCodeByHashAsync(string qrHash, ClaimsPrincipal user)
    {
        try
        {
            _logger.LogInformation("Reactivating QR code with hash: {QrHash}", qrHash);

            // Validate user authorization
            var userId = await _userContextService.GetUserIdAsync(user).ConfigureAwait(false);
            if (string.IsNullOrEmpty(userId))
            {
                _logger.LogWarning("QR code reactivation failed: User ID not found in token");
                throw new EntityUnauthorizedException("QrCode", "Reactivate", "unknown", "User ID not found in token");
            }

            var isAuthorized = await _userContextService.IsAuthorizedAsync(user, userId, RoleConstants.Admin, RoleConstants.Instructor).ConfigureAwait(false);
            if (!isAuthorized)
            {
                _logger.LogWarning("QR code reactivation failed: User not authorized");
                throw new EntityUnauthorizedException("QrCode", "Reactivate", userId, "You are not authorized to reactivate QR codes");
            }

            // Check if QR code exists and is not expired
            var qrCode = await _qrCodeRepository.GetQrCodeByHashAsync(qrHash).ConfigureAwait(false);
            if (qrCode == null)
            {
                _logger.LogWarning("QR code reactivation failed: QR code not found");
                throw new EntityNotFoundException<string>("QrCode", qrHash);
            }

            if (qrCode.ExpiresAt <= DateTime.UtcNow)
            {
                _logger.LogWarning("QR code reactivation failed: QR code has expired");
                throw new ValidationException("Cannot reactivate an expired QR code");
            }

            var result = await _qrCodeRepository.ReactivateQrCodeByHashAsync(qrHash).ConfigureAwait(false);
            if (!result)
            {
                _logger.LogWarning("QR code reactivation failed: QR code not found");
                throw new EntityNotFoundException<string>("QrCode", qrHash);
            }

            await _qrCodeRepository.SaveChangesAsync().ConfigureAwait(false);
            _logger.LogInformation("Successfully reactivated QR code with hash: {QrHash} by user: {UserId}", qrHash, userId);
        }
        catch (ValidationException)
        {
            throw;
        }
        catch (EntityNotFoundException<string>)
        {
            throw;
        }
        catch (EntityUnauthorizedException)
        {
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error occurred while reactivating QR code");
            throw ExceptionHandlingHelper.CreateServiceException("QrCode", "Reactivate", ex);
        }
    }

    public async Task DeleteQrCodeAsync(int id, ClaimsPrincipal user)
    {
        try
        {
            _logger.LogInformation("Deleting QR code with ID: {QrCodeId}", id);

            // Validate user authorization (only admins can hard delete)
            var userId = await _userContextService.GetUserIdAsync(user).ConfigureAwait(false);
            if (string.IsNullOrEmpty(userId))
            {
                _logger.LogWarning("QR code deletion failed: User ID not found in token");
                throw new EntityUnauthorizedException("QrCode", "Delete", "unknown", "User ID not found in token");
            }

            var isAuthorized = await _userContextService.IsAuthorizedAsync(user, userId, RoleConstants.Admin).ConfigureAwait(false);
            if (!isAuthorized)
            {
                _logger.LogWarning("QR code deletion failed: User not authorized");
                throw new EntityUnauthorizedException("QrCode", "Delete", userId, "You are not authorized to delete QR codes");
            }

            var result = await _qrCodeRepository.DeleteQrCodeAsync(id).ConfigureAwait(false);
            if (!result)
            {
                _logger.LogWarning("QR code deletion failed: QR code not found");
                throw new EntityNotFoundException<int>("QrCode", id);
            }

            await _qrCodeRepository.SaveChangesAsync().ConfigureAwait(false);
            _logger.LogInformation("Successfully deleted QR code with ID: {QrCodeId}", id);
        }
        catch (EntityNotFoundException<int>)
        {
            throw;
        }
        catch (EntityUnauthorizedException)
        {
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error occurred while deleting QR code");
            throw ExceptionHandlingHelper.CreateServiceException("QrCode", "Delete", ex);
        }
    }

    #endregion

    #region Validation and Usage Operations

    public async Task<QrCodeValidationResponseDto> ValidateQrCodeAsync(string qrHash)
    {
        try
        {
            _logger.LogInformation("Validating QR code with hash: {QrHash}", qrHash);

            var qrCode = await _qrCodeRepository.ValidateQrCodeForUsageAsync(qrHash).ConfigureAwait(false);

            if (qrCode == null)
            {
                _logger.LogWarning("QR code validation failed: QR code not found, expired, inactive, or usage limit reached");
                return new QrCodeValidationResponseDto
                {
                    IsValid = false,
                    Message = "QR code is invalid, expired, or has reached its usage limit"
                };
            }

            // Calculate remaining usage
            int? remainingUsage = null;
            if (qrCode.MaxUsage.HasValue)
            {
                remainingUsage = qrCode.MaxUsage.Value - qrCode.UsageCount;
            }

            // Validate that all required navigation properties are loaded
            if (qrCode.Session == null)
            {
                _logger.LogError("QR code {QrCodeId} missing Session navigation property", qrCode.Id);
                return new QrCodeValidationResponseDto
                {
                    IsValid = false,
                    Message = "QR code data is incomplete. Please contact support."
                };
            }

            if (qrCode.Session.Schedule == null)
            {
                _logger.LogError("QR code {QrCodeId} Session {SessionId} missing Schedule navigation property",
                    qrCode.Id, qrCode.Session.Id);
                return new QrCodeValidationResponseDto
                {
                    IsValid = false,
                    Message = "Session data is incomplete. Please contact support."
                };
            }

            var responseDto = new QrCodeValidationResponseDto
            {
                IsValid = true,
                Message = "QR code is valid",
                QrCodeId = qrCode.Id,
                ScheduleId = qrCode.Session.ScheduleId,
                SectionId = qrCode.Session.Schedule.SectionId,
                ActualRoomId = qrCode.Session.ActualRoomId ?? 0,
                ExpiresAt = qrCode.ExpiresAt,
                RemainingUsage = remainingUsage,
                ScheduleTitle = $"{qrCode.Session.Schedule.DayOfWeek} {qrCode.Session.Schedule.TimeIn}-{qrCode.Session.Schedule.TimeOut}",
                SectionName = qrCode.Session.Schedule.Section?.Name,
                RoomName = qrCode.Session.ActualRoom?.Name
            };

            _logger.LogInformation("QR code validation successful for hash: {QrHash}", qrHash);
            return responseDto;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error occurred while validating QR code with hash: {QrHash}", qrHash);
            return new QrCodeValidationResponseDto
            {
                IsValid = false,
                Message = "An error occurred while validating the QR code"
            };
        }
    }

    public async Task<QrCodeScanResponseDto> ScanQrCodeAsync(ValidateQrCode validateQrCode, ClaimsPrincipal user)
    {
        var utcNow = DateTime.UtcNow;

        // Start transaction for atomic operations
        using var transaction = await _qrCodeRepository.BeginTransactionAsync().ConfigureAwait(false);

        try
        {
            _logger.LogInformation("Scanning QR code with hash: {QrHash} for student ID: {StudentId}",
                validateQrCode.QrHash, validateQrCode.StudentId);

            // TIMING FIX: Step 1 - Get and validate QR code WITHOUT incrementing usage counter
            var qrCode = await _qrCodeRepository.GetQrCodeByHashAsync(validateQrCode.QrHash).ConfigureAwait(false);

            if (qrCode == null)
            {
                _logger.LogWarning("QR code scan failed: QR code not found");
                return new QrCodeScanResponseDto
                {
                    Success = false,
                    Message = "QR code not found",
                    AttendanceMarked = false
                };
            }

            // TIMING FIX: Step 1a - Validate QR code state before any modifications
            _logger.LogDebug("QR code validation: Hash={QrHash}, Active={IsActive}, Expires={ExpiresAt}, Usage={UsageCount}/{MaxUsage}",
                validateQrCode.QrHash, qrCode.IsActive, qrCode.ExpiresAt, qrCode.UsageCount, qrCode.MaxUsage);

            if (!qrCode.IsActive)
            {
                _logger.LogWarning("QR code scan failed: QR code is inactive");
                return new QrCodeScanResponseDto
                {
                    Success = false,
                    Message = "QR code is inactive",
                    AttendanceMarked = false
                };
            }

            if (qrCode.ExpiresAt <= utcNow)
            {
                _logger.LogWarning("QR code scan failed: QR code has expired at {ExpiresAt}", qrCode.ExpiresAt);
                return new QrCodeScanResponseDto
                {
                    Success = false,
                    Message = "QR code has expired",
                    AttendanceMarked = false
                };
            }

            if (qrCode.MaxUsage.HasValue && qrCode.UsageCount >= qrCode.MaxUsage.Value)
            {
                _logger.LogWarning("QR code scan failed: QR code usage limit reached ({UsageCount}/{MaxUsage})",
                    qrCode.UsageCount, qrCode.MaxUsage.Value);
                return new QrCodeScanResponseDto
                {
                    Success = false,
                    Message = "QR code usage limit reached",
                    AttendanceMarked = false
                };
            }

            // TIMING FIX: Step 2 - Validate student exists
            _logger.LogDebug("Validating student: StudentId={StudentId}", validateQrCode.StudentId);
            var student = await _studentRepository.GetStudentByIdAsync(validateQrCode.StudentId).ConfigureAwait(false);
            if (student == null)
            {
                _logger.LogWarning("QR code scan failed: Student not found");
                return new QrCodeScanResponseDto
                {
                    Success = false,
                    Message = "Student not found",
                    AttendanceMarked = false
                };
            }

            // TIMING FIX: Step 2a - Check if student is authorized for this section
            // Student is authorized if they are in their primary section OR enrolled via StudentEnrollment
            var sectionId = qrCode.Session?.Schedule?.SectionId ?? 0;
            bool isAuthorized = student.SectionId == sectionId;

            if (!isAuthorized && qrCode.Session?.Schedule != null)
            {
                // Check if student is enrolled in this section-subject combination (for irregular students)
                isAuthorized = await IsStudentEnrolledInSectionSubjectAsync(
                    validateQrCode.StudentId,
                    sectionId,
                    qrCode.Session.Schedule.SubjectId
                ).ConfigureAwait(false);
            }

            if (!isAuthorized)
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

            // TIMING FIX: Step 3 - Check for duplicate attendance BEFORE incrementing usage counter
            _logger.LogDebug("Checking for duplicate attendance: StudentId={StudentId}, SessionId={SessionId}",
                validateQrCode.StudentId, qrCode.SessionId);

            var existingRecord = await _attendanceRepository.HasAttendanceRecordAsync(
                validateQrCode.StudentId,
                qrCode.SessionId
            ).ConfigureAwait(false);

            if (existingRecord)
            {
                // Student already checked in - don't increment usage counter
                _logger.LogWarning("Duplicate QR scan detected for student ID: {StudentId}, session ID: {SessionId}",
                    validateQrCode.StudentId, qrCode.SessionId);

                // Calculate remaining scans for response
                var remainingScans = qrCode.MaxUsage.HasValue ?
                    Math.Max(0, qrCode.MaxUsage.Value - qrCode.UsageCount) :
                    int.MaxValue;

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
                    InstructorName = qrCode.Session?.Schedule?.Instructor != null ?
                        $"{qrCode.Session.Schedule.Instructor.Firstname} {qrCode.Session.Schedule.Instructor.Lastname}" : "Unknown",
                    RemainingScans = remainingScans,
                    IsDuplicateScan = true
                };
            }

            // TIMING FIX: Step 4 - Increment usage counter ONLY after duplicate check passes
            _logger.LogInformation("No duplicate found, incrementing usage counter for QR hash: {QrHash}", validateQrCode.QrHash);
            var incrementResult = await _qrCodeRepository.AtomicIncrementUsageAsync(
                validateQrCode.QrHash,
                utcNow
            ).ConfigureAwait(false);

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

            // TIMING FIX: Step 5 - Create attendance record after successful increment
            _logger.LogDebug("Creating attendance record: StudentId={StudentId}, SessionId={SessionId}",
                validateQrCode.StudentId, qrCode.SessionId);

            try
            {
                var attendanceRecord = await _attendanceService.CreateAttendanceFromQrScanAsync(
                    validateQrCode.StudentId,
                    qrCode.SessionId,
                    qrCode.Id,
                    utcNow
                ).ConfigureAwait(false);

                await _qrCodeRepository.SaveChangesAsync().ConfigureAwait(false);
                await transaction.CommitAsync().ConfigureAwait(false);

                // Send real-time notification to student and instructor (if opted-in)
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

                // Calculate remaining scans (usage count was incremented, so we use updated value)
                var remainingScans = qrCode.MaxUsage.HasValue ?
                    Math.Max(0, qrCode.MaxUsage.Value - qrCode.UsageCount - 1) : // -1 because we just incremented
                    int.MaxValue;

                var responseDto = new QrCodeScanResponseDto
                {
                    Success = true,
                    Message = "Attendance marked successfully",
                    AttendanceMarked = true,
                    AttendanceTime = utcNow,
                    StudentName = $"{student.Firstname} {student.Lastname}",
                    ClassName = qrCode.Session?.Schedule?.Section?.Name ?? "Unknown",
                    SubjectName = qrCode.Session?.Schedule?.Subject?.Name ?? "Unknown",
                    RoomName = qrCode.Session?.ActualRoom?.Name ?? "Unknown",
                    InstructorName = qrCode.Session?.Schedule?.Instructor != null ?
                        $"{qrCode.Session.Schedule.Instructor.Firstname} {qrCode.Session.Schedule.Instructor.Lastname}" : "Unknown",
                    RemainingScans = remainingScans,
                    AttendanceRecordId = attendanceRecord.Id,
                    AttendanceStatus = attendanceRecord.Status,
                    IsDuplicateScan = false
                };

                _logger.LogInformation("Successfully processed QR code scan for student ID: {StudentId}, attendance record ID: {AttendanceId}",
                    validateQrCode.StudentId, attendanceRecord.Id);
                return responseDto;
            }
            catch (InvalidOperationException ex) when (ex.Message.Contains("duplicate"))
            {
                // This should rarely happen now since we check duplicates before incrementing
                // But keeping it as a safety net for edge cases
                await transaction.RollbackAsync().ConfigureAwait(false);

                _logger.LogWarning("Duplicate detected during attendance creation (edge case): StudentId={StudentId}, SessionId={SessionId}",
                    validateQrCode.StudentId, qrCode.SessionId);

                // Calculate remaining scans
                var remainingScans = qrCode.MaxUsage.HasValue ?
                    Math.Max(0, qrCode.MaxUsage.Value - qrCode.UsageCount) :
                    int.MaxValue;

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
                    InstructorName = qrCode.Session?.Schedule?.Instructor != null ?
                        $"{qrCode.Session.Schedule.Instructor.Firstname} {qrCode.Session.Schedule.Instructor.Lastname}" : "Unknown",
                    RemainingScans = remainingScans,
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

    public async Task<bool> QrHashExistsAsync(string qrHash)
    {
        try
        {
            return await _qrCodeRepository.QrHashExistsAsync(qrHash).ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error occurred while checking if QR hash exists: {QrHash}", qrHash);
            throw new EntityServiceException("QrCode", $"QrHashExists: {qrHash}", "An error occurred while checking QR hash existence", ex);
        }
    }

    #endregion

    #region Utility Operations

    public async Task<string> GenerateUniqueQrHashAsync(string? clientHash = null)
    {
        string qrHash;
        bool exists;

        do
        {
            qrHash = GenerateQrHash(clientHash);
            exists = await _qrCodeRepository.QrHashExistsAsync(qrHash).ConfigureAwait(false);
        } while (exists);

        return qrHash;
    }

    public async Task<int> CleanupExpiredQrCodesAsync(ClaimsPrincipal user)
    {
        try
        {
            _logger.LogInformation("Starting cleanup of expired QR codes");

            // Validate user authorization (only admins can cleanup)
            var userId = await _userContextService.GetUserIdAsync(user).ConfigureAwait(false);
            if (string.IsNullOrEmpty(userId))
            {
                _logger.LogWarning("QR code cleanup failed: User ID not found in token");
                throw new EntityServiceException("QrCode", "CleanupExpiredQrCodes", "User ID not found in token");
            }

            var isAuthorized = await _userContextService.IsAuthorizedAsync(user, userId, RoleConstants.Admin).ConfigureAwait(false);
            if (!isAuthorized)
            {
                _logger.LogWarning("QR code cleanup failed: User not authorized");
                throw new EntityServiceException("QrCode", "CleanupExpiredQrCodes", "You are not authorized to cleanup QR codes");
            }

            var deletedCount = await _qrCodeRepository.DeleteExpiredQrCodesAsync().ConfigureAwait(false);
            await _qrCodeRepository.SaveChangesAsync().ConfigureAwait(false);

            _logger.LogInformation("Successfully cleaned up {Count} expired QR codes", deletedCount);
            return deletedCount;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error occurred while cleaning up expired QR codes");
            throw new EntityServiceException("QrCode", "CleanupExpiredQrCodes", "An error occurred while cleaning up expired QR codes", ex);
        }
    }

    public async Task<IEnumerable<QrCodeResponseDto>> GetQrCodesExpiringSoonAsync(int expiringWithinMinutes = 30)
    {
        try
        {
            _logger.LogInformation("Retrieving QR codes expiring within {Minutes} minutes", expiringWithinMinutes);

            var expiringWithin = TimeSpan.FromMinutes(expiringWithinMinutes);
            var qrCodes = await _qrCodeRepository.GetQrCodesExpiringWithinAsync(expiringWithin).ConfigureAwait(false);

            var responseDtos = new List<QrCodeResponseDto>();
            foreach (var qrCode in qrCodes)
            {
                var responseDto = MapToResponseDto(qrCode);
                responseDtos.Add(responseDto);
            }

            _logger.LogInformation("Successfully retrieved {Count} QR codes expiring soon", responseDtos.Count);
            return responseDtos;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error occurred while retrieving QR codes expiring soon");
            throw new EntityServiceException("QrCode", "GetQrCodesExpiringSoon", "An error occurred while retrieving expiring QR codes", ex);
        }
    }

    public async Task<QrCodeResponseDto> ExtendQrCodeExpirationAsync(int id, int additionalMinutes, ClaimsPrincipal user)
    {
        try
        {
            _logger.LogInformation("Extending expiration for QR code ID: {QrCodeId} by {Minutes} minutes", id, additionalMinutes);

            // Validate user authorization
            var userId = await _userContextService.GetUserIdAsync(user).ConfigureAwait(false);
            if (string.IsNullOrEmpty(userId))
            {
                _logger.LogWarning("QR code expiration extension failed: User ID not found in token");
                throw new EntityUnauthorizedException("QrCode", "ExtendExpiration", "unknown", "User ID not found in token");
            }

            var isAuthorized = await _userContextService.IsAuthorizedAsync(user, userId, RoleConstants.Admin, RoleConstants.Instructor).ConfigureAwait(false);
            if (!isAuthorized)
            {
                _logger.LogWarning("QR code expiration extension failed: User not authorized");
                throw new EntityUnauthorizedException("QrCode", "ExtendExpiration", userId, "You are not authorized to extend QR code expiration");
            }

            // Get existing QR code
            var qrCode = await _qrCodeRepository.GetQrCodeByIdAsync(id).ConfigureAwait(false);
            if (qrCode == null)
            {
                _logger.LogWarning("QR code expiration extension failed: QR code not found");
                throw new EntityNotFoundException<int>("QrCode", id);
            }

            // Extend expiration
            qrCode.ExpiresAt = qrCode.ExpiresAt.AddMinutes(additionalMinutes);

            var updatedQrCode = await _qrCodeRepository.UpdateQrCodeAsync(qrCode).ConfigureAwait(false);
            await _qrCodeRepository.SaveChangesAsync().ConfigureAwait(false);

            var responseDto = MapToResponseDto(updatedQrCode);
            _logger.LogInformation("Successfully extended expiration for QR code ID: {QrCodeId}", id);

            return responseDto;
        }
        catch (EntityNotFoundException<int>)
        {
            throw;
        }
        catch (EntityUnauthorizedException)
        {
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error occurred while extending QR code expiration");
            throw ExceptionHandlingHelper.CreateServiceException("QrCode", "ExtendExpiration", ex);
        }
    }

    #endregion

    #region Private Helper Methods

    private static string GenerateQrHash(string? clientHash = null)
    {
        using var rng = RandomNumberGenerator.Create();
        var serverBytes = new byte[32];
        rng.GetBytes(serverBytes);

        // If client hash provided, combine it with server randomness
        if (!string.IsNullOrEmpty(clientHash))
        {
            var combinedBytes = Encoding.UTF8.GetBytes(clientHash)
                .Concat(serverBytes)
                .ToArray();

            // Hash the combined data for consistent length and security
            var hashBytes = SHA256.HashData(combinedBytes);
            return Convert.ToBase64String(hashBytes).Replace("+", "-").Replace("/", "_").Replace("=", "");
        }

        // Fallback to server-only hash
        return Convert.ToBase64String(serverBytes).Replace("+", "-").Replace("/", "_").Replace("=", "");
    }

    private QrCodeResponseDto MapToResponseDto(QrCode qrCode)
    {
        // Validate critical navigation properties are loaded
        if (qrCode.Session == null)
        {
            _logger.LogWarning("QR code {QrCodeId} missing Session navigation property in MapToResponseDto", qrCode.Id);
        }

        if (qrCode.Session?.Schedule == null)
        {
            _logger.LogWarning("QR code {QrCodeId} Session missing Schedule navigation property in MapToResponseDto", qrCode.Id);
        }

        return new QrCodeResponseDto
        {
            Id = qrCode.Id,
            SessionId = qrCode.SessionId,
            QrHash = qrCode.QrHash,
            GeneratedAt = qrCode.GeneratedAt,
            ExpiresAt = qrCode.ExpiresAt,
            IsActive = qrCode.IsActive,
            UsageCount = qrCode.UsageCount,
            MaxUsage = qrCode.MaxUsage,
            CreatedAt = qrCode.CreatedAt,
            UpdatedAt = qrCode.UpdatedAt,

            // Session information
            ScheduleId = qrCode.Session?.ScheduleId,
            SessionDate = qrCode.Session?.SessionDate,
            SessionStatus = qrCode.Session?.Status,

            // Related entity information (from Session -> Schedule)
            ScheduleTitle = qrCode.Session?.Schedule != null ?
                $"{qrCode.Session.Schedule.DayOfWeek} {qrCode.Session.Schedule.TimeIn}-{qrCode.Session.Schedule.TimeOut}" : null,
            SectionName = qrCode.Session?.Schedule?.Section?.Name,
            ActualRoomName = qrCode.Session?.ActualRoom?.Name,
            SubjectName = qrCode.Session?.Schedule?.Subject?.Name,
            InstructorName = qrCode.Session?.Schedule?.Instructor != null ?
                $"{qrCode.Session.Schedule.Instructor.Firstname} {qrCode.Session.Schedule.Instructor.Lastname}" : null
        };
    }

    /// <summary>
    /// Validates that the session exists and is in active status.
    /// </summary>
    /// <param name="sessionId">The session ID to validate</param>
    /// <returns>Error message if validation fails, null if session exists and is active</returns>
    private async Task<string?> ValidateSessionExistsAsync(int sessionId)
    {
        // Validate session exists
        var session = await _sessionRepository.GetSessionByIdAsync(sessionId)
            .ConfigureAwait(false);

        if (session == null)
        {
            _logger.LogWarning("Session with ID {SessionId} not found", sessionId);
            return "The specified session does not exist";
        }

        // Validate session is active - provide clear error messages for each status
        if (session.Status != "active")
        {
            _logger.LogWarning("Session with ID {SessionId} is not active (status: {Status})", sessionId, session.Status);

            return session.Status switch
            {
                "not_started" => "Session has not started yet. Please start the session before generating QR codes.",
                "ended" => "Session has already ended. QR codes cannot be generated for completed sessions.",
                "cancelled" => "Session has been cancelled. QR codes cannot be generated for cancelled sessions.",
                _ => $"Session is not active. Current status: {session.Status}. Only active sessions can generate QR codes."
            };
        }

        return null; // Session exists and is active
    }

    /// <summary>
    /// Validates that the session exists and is in active status. Throws exceptions on failure.
    /// </summary>
    /// <param name="sessionId">The session ID to validate</param>
    /// <exception cref="EntityNotFoundException{Int32}">Thrown when the session does not exist.</exception>
    /// <exception cref="ValidationException">Thrown when the session is not in active status.</exception>
    private async Task ValidateSessionExistsOrThrowAsync(int sessionId)
    {
        var session = await _sessionRepository.GetSessionByIdAsync(sessionId).ConfigureAwait(false);

        if (session == null)
        {
            _logger.LogWarning("Session with ID {SessionId} not found", sessionId);
            throw new EntityNotFoundException<int>("Session", sessionId);
        }

        if (session.Status != "active")
        {
            _logger.LogWarning("Session with ID {SessionId} is not active (status: {Status})", sessionId, session.Status);

            var message = session.Status switch
            {
                "not_started" => "Session has not started yet. Please start the session before generating QR codes.",
                "ended" => "Session has already ended. QR codes cannot be generated for completed sessions.",
                "cancelled" => "Session has been cancelled. QR codes cannot be generated for cancelled sessions.",
                _ => $"Session is not active. Current status: {session.Status}. Only active sessions can generate QR codes."
            };
            throw new ValidationException(message);
        }
    }

    /// <summary>
    /// Helper method to check if a student is enrolled in a specific section-subject combination.
    /// This supports irregular students who are enrolled in subjects outside their primary section.
    /// </summary>
    /// <param name="studentId">The student ID to check</param>
    /// <param name="sectionId">The section ID to check</param>
    /// <param name="subjectId">The subject ID to check</param>
    /// <returns>True if the student is enrolled in the section-subject combination; otherwise, false</returns>
    private async Task<bool> IsStudentEnrolledInSectionSubjectAsync(int studentId, int sectionId, int subjectId)
    {
        try
        {
            return await _studentEnrollmentService.IsStudentEnrolledInSectionSubjectAsync(studentId, sectionId, subjectId)
                .ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error checking student enrollment for Student: {StudentId}, Section: {SectionId}, Subject: {SubjectId}",
                studentId, sectionId, subjectId);
            return false; // Default to not enrolled if there's an error
        }
    }

    #endregion
}
