using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using Microsoft.EntityFrameworkCore;
using attendance_monitoring.Classes;
using attendance_monitoring.Data;
using attendance_monitoring.Exceptions;
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
    private readonly ILogger<QrCodeService> _logger;

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
        ILogger<QrCodeService> logger)
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
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
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

    #endregion

    #region Write Operations

    public async Task<(QrCodeResponseDto?, string?)> CreateQrCodeAsync(CreateQrCode createQrCode, ClaimsPrincipal user)
    {
        try
        {
            _logger.LogInformation("Creating QR code for session ID: {SessionId}", createQrCode.SessionId);

            // Validate user authorization
            var userId = await _userContextService.GetUserIdAsync(user).ConfigureAwait(false);
            if (string.IsNullOrEmpty(userId))
            {
                _logger.LogWarning("QR code creation failed: User ID not found in token");
                return (null, "User ID not found in token");
            }

            var isAuthorized = await _userContextService.IsAuthorizedAsync(user, userId, "Admin", "Instructor").ConfigureAwait(false);
            if (!isAuthorized)
            {
                _logger.LogWarning("QR code creation failed: User not authorized");
                return (null, "You are not authorized to create QR codes");
            }

            // Validate session exists and is active
            var validationError = await ValidateSessionExistsAsync(createQrCode.SessionId).ConfigureAwait(false);
            if (validationError != null)
            {
                return (null, validationError);
            }

            // Check if QR hash already exists
            var hashExists = await _qrCodeRepository.QrHashExistsAsync(createQrCode.QrHash).ConfigureAwait(false);
            if (hashExists)
            {
                _logger.LogWarning("QR code creation failed: QR hash already exists");
                return (null, "QR code hash already exists");
            }

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

            return (responseDto, null);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error occurred while creating QR code");
            return (null, "An error occurred while creating the QR code");
        }
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

                var isAuthorized = await _userContextService.IsAuthorizedAsync(user, userId, "Admin", "Instructor").ConfigureAwait(false);
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

    public async Task<(QrCodeResponseDto?, string?)> UpdateQrCodeAsync(int id, UpdateQrCode updateQrCode, ClaimsPrincipal user)
    {
        try
        {
            _logger.LogInformation("Updating QR code with ID: {QrCodeId}", id);

            // Validate user authorization
            var userId = await _userContextService.GetUserIdAsync(user).ConfigureAwait(false);
            if (string.IsNullOrEmpty(userId))
            {
                _logger.LogWarning("QR code update failed: User ID not found in token");
                return (null, "User ID not found in token");
            }

            var isAuthorized = await _userContextService.IsAuthorizedAsync(user, userId, "Admin", "Instructor").ConfigureAwait(false);
            if (!isAuthorized)
            {
                _logger.LogWarning("QR code update failed: User not authorized");
                return (null, "You are not authorized to update QR codes");
            }

            // Get existing QR code
            var existingQrCode = await _qrCodeRepository.GetQrCodeByIdAsync(id).ConfigureAwait(false);
            if (existingQrCode == null)
            {
                _logger.LogWarning("QR code update failed: QR code not found");
                return (null, "QR code not found");
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

            return (responseDto, null);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error occurred while updating QR code");
            return (null, "An error occurred while updating the QR code");
        }
    }

    public async Task<string?> DeactivateQrCodeAsync(int id, ClaimsPrincipal user)
    {
        try
        {
            _logger.LogInformation("Deactivating QR code with ID: {QrCodeId}", id);

            // Validate user authorization
            var userId = await _userContextService.GetUserIdAsync(user).ConfigureAwait(false);
            if (string.IsNullOrEmpty(userId))
            {
                _logger.LogWarning("QR code deactivation failed: User ID not found in token");
                return "User ID not found in token";
            }

            var isAuthorized = await _userContextService.IsAuthorizedAsync(user, userId, "Admin", "Instructor").ConfigureAwait(false);
            if (!isAuthorized)
            {
                _logger.LogWarning("QR code deactivation failed: User not authorized");
                return "You are not authorized to deactivate QR codes";
            }

            var result = await _qrCodeRepository.DeactivateQrCodeAsync(id).ConfigureAwait(false);
            if (!result)
            {
                _logger.LogWarning("QR code deactivation failed: QR code not found");
                return "QR code not found";
            }

            await _qrCodeRepository.SaveChangesAsync().ConfigureAwait(false);
            _logger.LogInformation("Successfully deactivated QR code with ID: {QrCodeId}", id);
            return null;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error occurred while deactivating QR code");
            return "An error occurred while deactivating the QR code";
        }
    }

    public async Task<string?> RevokeQrCodeAsync(int id, string? reason, ClaimsPrincipal user)
    {
        try
        {
            _logger.LogInformation("Revoking QR code with ID: {QrCodeId}", id);

            // Validate user authorization
            var userId = await _userContextService.GetUserIdAsync(user).ConfigureAwait(false);
            if (string.IsNullOrEmpty(userId))
            {
                _logger.LogWarning("QR code revocation failed: User ID not found in token");
                return "User ID not found in token";
            }

            var isAuthorized = await _userContextService.IsAuthorizedAsync(user, userId, "Admin", "Instructor").ConfigureAwait(false);
            if (!isAuthorized)
            {
                _logger.LogWarning("QR code revocation failed: User not authorized");
                return "You are not authorized to revoke QR codes";
            }

            // Get QR code and add audit trail
            var qrCode = await _qrCodeRepository.GetQrCodeByIdAsync(id).ConfigureAwait(false);
            if (qrCode == null)
            {
                _logger.LogWarning("QR code revocation failed: QR code not found");
                return "QR code not found";
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
            return null;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error occurred while revoking QR code");
            return "An error occurred while revoking the QR code";
        }
    }

    public async Task<string?> DeactivateQrCodeByHashAsync(string qrHash, ClaimsPrincipal user)
    {
        try
        {
            _logger.LogInformation("Deactivating QR code with hash: {QrHash}", qrHash);

            // Validate user authorization
            var userId = await _userContextService.GetUserIdAsync(user).ConfigureAwait(false);
            if (string.IsNullOrEmpty(userId))
            {
                _logger.LogWarning("QR code deactivation failed: User ID not found in token");
                return "User ID not found in token";
            }

            var isAuthorized = await _userContextService.IsAuthorizedAsync(user, userId, "Admin", "Instructor").ConfigureAwait(false);
            if (!isAuthorized)
            {
                _logger.LogWarning("QR code deactivation failed: User not authorized");
                return "You are not authorized to deactivate QR codes";
            }

            var result = await _qrCodeRepository.DeactivateQrCodeByHashAsync(qrHash).ConfigureAwait(false);
            if (!result)
            {
                _logger.LogWarning("QR code deactivation failed: QR code not found");
                return "QR code not found";
            }

            await _qrCodeRepository.SaveChangesAsync().ConfigureAwait(false);
            _logger.LogInformation("Successfully deactivated QR code with hash: {QrHash}", qrHash);
            return null;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error occurred while deactivating QR code");
            return "An error occurred while deactivating the QR code";
        }
    }

    public async Task<string?> RevokeQrCodeByHashAsync(string qrHash, string? reason, ClaimsPrincipal user)
    {
        try
        {
            _logger.LogInformation("Revoking QR code with hash: {QrHash}", qrHash);

            // Validate user authorization
            var userId = await _userContextService.GetUserIdAsync(user).ConfigureAwait(false);
            if (string.IsNullOrEmpty(userId))
            {
                _logger.LogWarning("QR code revocation failed: User ID not found in token");
                return "User ID not found in token";
            }

            var isAuthorized = await _userContextService.IsAuthorizedAsync(user, userId, "Admin", "Instructor").ConfigureAwait(false);
            if (!isAuthorized)
            {
                _logger.LogWarning("QR code revocation failed: User not authorized");
                return "You are not authorized to revoke QR codes";
            }

            // Get QR code and add audit trail
            var qrCode = await _qrCodeRepository.GetQrCodeByHashAsync(qrHash).ConfigureAwait(false);
            if (qrCode == null)
            {
                _logger.LogWarning("QR code revocation failed: QR code not found");
                return "QR code not found";
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
            return null;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error occurred while revoking QR code");
            return "An error occurred while revoking the QR code";
        }
    }

    public async Task<string?> ReactivateQrCodeAsync(int id, ClaimsPrincipal user)
    {
        try
        {
            _logger.LogInformation("Reactivating QR code with ID: {QrCodeId}", id);

            // Validate user authorization
            var userId = await _userContextService.GetUserIdAsync(user).ConfigureAwait(false);
            if (string.IsNullOrEmpty(userId))
            {
                _logger.LogWarning("QR code reactivation failed: User ID not found in token");
                return "User ID not found in token";
            }

            var isAuthorized = await _userContextService.IsAuthorizedAsync(user, userId, "Admin", "Instructor").ConfigureAwait(false);
            if (!isAuthorized)
            {
                _logger.LogWarning("QR code reactivation failed: User not authorized");
                return "You are not authorized to reactivate QR codes";
            }

            // Check if QR code exists and is not expired
            var qrCode = await _qrCodeRepository.GetQrCodeByIdAsync(id).ConfigureAwait(false);
            if (qrCode == null)
            {
                _logger.LogWarning("QR code reactivation failed: QR code not found");
                return "QR code not found";
            }

            if (qrCode.ExpiresAt <= DateTime.UtcNow)
            {
                _logger.LogWarning("QR code reactivation failed: QR code has expired");
                return "Cannot reactivate an expired QR code";
            }

            var result = await _qrCodeRepository.ReactivateQrCodeAsync(id).ConfigureAwait(false);
            if (!result)
            {
                _logger.LogWarning("QR code reactivation failed: QR code not found");
                return "QR code not found";
            }

            await _qrCodeRepository.SaveChangesAsync().ConfigureAwait(false);
            _logger.LogInformation("Successfully reactivated QR code with ID: {QrCodeId} by user: {UserId}", id, userId);
            return null;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error occurred while reactivating QR code");
            return "An error occurred while reactivating the QR code";
        }
    }

    public async Task<string?> ReactivateQrCodeByHashAsync(string qrHash, ClaimsPrincipal user)
    {
        try
        {
            _logger.LogInformation("Reactivating QR code with hash: {QrHash}", qrHash);

            // Validate user authorization
            var userId = await _userContextService.GetUserIdAsync(user).ConfigureAwait(false);
            if (string.IsNullOrEmpty(userId))
            {
                _logger.LogWarning("QR code reactivation failed: User ID not found in token");
                return "User ID not found in token";
            }

            var isAuthorized = await _userContextService.IsAuthorizedAsync(user, userId, "Admin", "Instructor").ConfigureAwait(false);
            if (!isAuthorized)
            {
                _logger.LogWarning("QR code reactivation failed: User not authorized");
                return "You are not authorized to reactivate QR codes";
            }

            // Check if QR code exists and is not expired
            var qrCode = await _qrCodeRepository.GetQrCodeByHashAsync(qrHash).ConfigureAwait(false);
            if (qrCode == null)
            {
                _logger.LogWarning("QR code reactivation failed: QR code not found");
                return "QR code not found";
            }

            if (qrCode.ExpiresAt <= DateTime.UtcNow)
            {
                _logger.LogWarning("QR code reactivation failed: QR code has expired");
                return "Cannot reactivate an expired QR code";
            }

            var result = await _qrCodeRepository.ReactivateQrCodeByHashAsync(qrHash).ConfigureAwait(false);
            if (!result)
            {
                _logger.LogWarning("QR code reactivation failed: QR code not found");
                return "QR code not found";
            }

            await _qrCodeRepository.SaveChangesAsync().ConfigureAwait(false);
            _logger.LogInformation("Successfully reactivated QR code with hash: {QrHash} by user: {UserId}", qrHash, userId);
            return null;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error occurred while reactivating QR code");
            return "An error occurred while reactivating the QR code";
        }
    }

    public async Task<string?> DeleteQrCodeAsync(int id, ClaimsPrincipal user)
    {
        try
        {
            _logger.LogInformation("Deleting QR code with ID: {QrCodeId}", id);

            // Validate user authorization (only admins can hard delete)
            var userId = await _userContextService.GetUserIdAsync(user).ConfigureAwait(false);
            if (string.IsNullOrEmpty(userId))
            {
                _logger.LogWarning("QR code deletion failed: User ID not found in token");
                return "User ID not found in token";
            }

            var isAuthorized = await _userContextService.IsAuthorizedAsync(user, userId, "Admin").ConfigureAwait(false);
            if (!isAuthorized)
            {
                _logger.LogWarning("QR code deletion failed: User not authorized");
                return "You are not authorized to delete QR codes";
            }

            var result = await _qrCodeRepository.DeleteQrCodeAsync(id).ConfigureAwait(false);
            if (!result)
            {
                _logger.LogWarning("QR code deletion failed: QR code not found");
                return "QR code not found";
            }

            await _qrCodeRepository.SaveChangesAsync().ConfigureAwait(false);
            _logger.LogInformation("Successfully deleted QR code with ID: {QrCodeId}", id);
            return null;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error occurred while deleting QR code");
            return "An error occurred while deleting the QR code";
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

            // Atomic update with all validations - prevents race condition
            var result = await _qrCodeRepository.AtomicIncrementUsageAsync(
                validateQrCode.QrHash,
                utcNow
            ).ConfigureAwait(false);

            if (result == 0)
            {
                // Provide specific error message by checking QR code state
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
                    _logger.LogWarning("QR code scan failed: QR code has expired");
                    return new QrCodeScanResponseDto
                    {
                        Success = false,
                        Message = "QR code has expired",
                        AttendanceMarked = false
                    };
                }

                if (qrCode.MaxUsage.HasValue && qrCode.UsageCount >= qrCode.MaxUsage.Value)
                {
                    _logger.LogWarning("QR code scan failed: QR code usage limit reached");
                    return new QrCodeScanResponseDto
                    {
                        Success = false,
                        Message = "QR code usage limit reached",
                        AttendanceMarked = false
                    };
                }

                _logger.LogWarning("QR code scan failed: Unable to scan QR code");
                return new QrCodeScanResponseDto
                {
                    Success = false,
                    Message = "Unable to scan QR code",
                    AttendanceMarked = false
                };
            }

            // Re-fetch QR code with full navigation properties after successful increment
            var validatedQrCode = await _qrCodeRepository.GetQrCodeByHashAsync(validateQrCode.QrHash).ConfigureAwait(false);
            if (validatedQrCode == null)
            {
                await transaction.RollbackAsync().ConfigureAwait(false);
                _logger.LogError("QR code disappeared after atomic increment");
                return new QrCodeScanResponseDto
                {
                    Success = false,
                    Message = "An error occurred while processing the QR code scan",
                    AttendanceMarked = false
                };
            }

            // Validate student exists
            var student = await _studentRepository.GetStudentByIdAsync(validateQrCode.StudentId).ConfigureAwait(false);
            if (student == null)
            {
                await transaction.RollbackAsync().ConfigureAwait(false);
                _logger.LogWarning("QR code scan failed: Student not found");
                return new QrCodeScanResponseDto
                {
                    Success = false,
                    Message = "Student not found",
                    AttendanceMarked = false
                };
            }

            // Check if student is authorized for this section
            // Student is authorized if they are in their primary section OR enrolled via StudentEnrollment
            var sectionId = validatedQrCode.Session?.Schedule?.SectionId ?? 0;
            bool isAuthorized = student.SectionId == sectionId;

            if (!isAuthorized && validatedQrCode.Session?.Schedule != null)
            {
                // Check if student is enrolled in this section-subject combination (for irregular students)
                isAuthorized = await IsStudentEnrolledInSectionSubjectAsync(
                    validateQrCode.StudentId,
                    sectionId,
                    validatedQrCode.Session.Schedule.SubjectId
                ).ConfigureAwait(false);
            }

            if (!isAuthorized)
            {
                await transaction.RollbackAsync().ConfigureAwait(false);
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

            // Create attendance record
            try
            {
                var attendanceRecord = await _attendanceService.CreateAttendanceFromQrScanAsync(
                    validateQrCode.StudentId,
                    validatedQrCode.SessionId,
                    validatedQrCode.Id,
                    utcNow
                ).ConfigureAwait(false);

                await _qrCodeRepository.SaveChangesAsync().ConfigureAwait(false);
                await transaction.CommitAsync().ConfigureAwait(false);

                // Calculate remaining scans
                var remainingScans = validatedQrCode.MaxUsage.HasValue ?
                    Math.Max(0, validatedQrCode.MaxUsage.Value - validatedQrCode.UsageCount) :
                    int.MaxValue;

                var responseDto = new QrCodeScanResponseDto
                {
                    Success = true,
                    Message = "Attendance marked successfully",
                    AttendanceMarked = true,
                    AttendanceTime = utcNow,
                    StudentName = $"{student.Firstname} {student.Lastname}",
                    ClassName = validatedQrCode.Session?.Schedule?.Section?.Name ?? "Unknown",
                    SubjectName = validatedQrCode.Session?.Schedule?.Subject?.Name ?? "Unknown",
                    RoomName = validatedQrCode.Session?.ActualRoom?.Name ?? "Unknown",
                    InstructorName = validatedQrCode.Session?.Schedule?.Instructor != null ?
                        $"{validatedQrCode.Session.Schedule.Instructor.Firstname} {validatedQrCode.Session.Schedule.Instructor.Lastname}" : "Unknown",
                    RemainingScans = remainingScans,
                    AttendanceRecordId = attendanceRecord.Id,
                    AttendanceStatus = attendanceRecord.Status,
                    IsDuplicateScan = false
                };

                _logger.LogInformation("Successfully processed QR code scan for student ID: {StudentId}", validateQrCode.StudentId);
                return responseDto;
            }
            catch (InvalidOperationException ex) when (ex.Message.Contains("duplicate"))
            {
                await transaction.RollbackAsync().ConfigureAwait(false);

                // Student already checked in
                _logger.LogWarning("Duplicate QR scan detected for student ID: {StudentId}", validateQrCode.StudentId);

                // Calculate remaining scans
                var remainingScans = validatedQrCode.MaxUsage.HasValue ?
                    Math.Max(0, validatedQrCode.MaxUsage.Value - validatedQrCode.UsageCount) :
                    int.MaxValue;

                return new QrCodeScanResponseDto
                {
                    Success = true,
                    Message = "You have already checked in for this session",
                    AttendanceMarked = false,
                    AttendanceTime = utcNow,
                    StudentName = $"{student.Firstname} {student.Lastname}",
                    ClassName = validatedQrCode.Session?.Schedule?.Section?.Name ?? "Unknown",
                    SubjectName = validatedQrCode.Session?.Schedule?.Subject?.Name ?? "Unknown",
                    RoomName = validatedQrCode.Session?.ActualRoom?.Name ?? "Unknown",
                    InstructorName = validatedQrCode.Session?.Schedule?.Instructor != null ?
                        $"{validatedQrCode.Session.Schedule.Instructor.Firstname} {validatedQrCode.Session.Schedule.Instructor.Lastname}" : "Unknown",
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

            var isAuthorized = await _userContextService.IsAuthorizedAsync(user, userId, "Admin").ConfigureAwait(false);
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

    public async Task<(QrCodeResponseDto?, string?)> ExtendQrCodeExpirationAsync(int id, int additionalMinutes, ClaimsPrincipal user)
    {
        try
        {
            _logger.LogInformation("Extending expiration for QR code ID: {QrCodeId} by {Minutes} minutes", id, additionalMinutes);

            // Validate user authorization
            var userId = await _userContextService.GetUserIdAsync(user).ConfigureAwait(false);
            if (string.IsNullOrEmpty(userId))
            {
                _logger.LogWarning("QR code expiration extension failed: User ID not found in token");
                return (null, "User ID not found in token");
            }

            var isAuthorized = await _userContextService.IsAuthorizedAsync(user, userId, "Admin", "Instructor").ConfigureAwait(false);
            if (!isAuthorized)
            {
                _logger.LogWarning("QR code expiration extension failed: User not authorized");
                return (null, "You are not authorized to extend QR code expiration");
            }

            // Get existing QR code
            var qrCode = await _qrCodeRepository.GetQrCodeByIdAsync(id).ConfigureAwait(false);
            if (qrCode == null)
            {
                _logger.LogWarning("QR code expiration extension failed: QR code not found");
                return (null, "QR code not found");
            }

            // Extend expiration
            qrCode.ExpiresAt = qrCode.ExpiresAt.AddMinutes(additionalMinutes);

            var updatedQrCode = await _qrCodeRepository.UpdateQrCodeAsync(qrCode).ConfigureAwait(false);
            await _qrCodeRepository.SaveChangesAsync().ConfigureAwait(false);

            var responseDto = MapToResponseDto(updatedQrCode);
            _logger.LogInformation("Successfully extended expiration for QR code ID: {QrCodeId}", id);

            return (responseDto, null);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error occurred while extending QR code expiration");
            return (null, "An error occurred while extending the QR code expiration");
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
