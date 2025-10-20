using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using attendance_monitoring.Classes;
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
    private readonly UserContextService _userContextService;
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
        UserContextService userContextService,
        ILogger<QrCodeService> logger)
    {
        _qrCodeRepository = qrCodeRepository ?? throw new ArgumentNullException(nameof(qrCodeRepository));
        _scheduleRepository = scheduleRepository ?? throw new ArgumentNullException(nameof(scheduleRepository));
        _sectionRepository = sectionRepository ?? throw new ArgumentNullException(nameof(sectionRepository));
        _classroomRepository = classroomRepository ?? throw new ArgumentNullException(nameof(classroomRepository));
        _studentRepository = studentRepository ?? throw new ArgumentNullException(nameof(studentRepository));
        _userContextService = userContextService ?? throw new ArgumentNullException(nameof(userContextService));
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

            var responseDto = await MapToResponseDtoAsync(qrCode).ConfigureAwait(false);
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

            var responseDto = await MapToResponseDtoAsync(qrCode).ConfigureAwait(false);
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
                var responseDto = await MapToResponseDtoAsync(qrCode).ConfigureAwait(false);
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
                var responseDto = await MapToResponseDtoAsync(qrCode).ConfigureAwait(false);
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
                var responseDto = await MapToResponseDtoAsync(qrCode).ConfigureAwait(false);
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
            _logger.LogInformation("Creating QR code for schedule ID: {ScheduleId}", createQrCode.ScheduleId);

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

            // Validate entities exist
            var validationError = await ValidateEntitiesExistAsync(createQrCode.ScheduleId, createQrCode.SectionId, createQrCode.ActualRoomId).ConfigureAwait(false);
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
                ScheduleId = createQrCode.ScheduleId,
                SectionId = createQrCode.SectionId,
                ActualRoomId = createQrCode.ActualRoomId,
                QrHash = createQrCode.QrHash,
                ExpiresAt = createQrCode.ExpiresAt,
                MaxUsage = createQrCode.MaxUsage
            };

            var createdQrCode = await _qrCodeRepository.CreateQrCodeAsync(qrCode).ConfigureAwait(false);
            await _qrCodeRepository.SaveChangesAsync().ConfigureAwait(false);

            var responseDto = await MapToResponseDtoAsync(createdQrCode).ConfigureAwait(false);
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
        try
        {
            _logger.LogInformation("Generating QR code for schedule ID: {ScheduleId}", qrCodeRequest.ScheduleId);

            // Validate user authorization
            var userId = await _userContextService.GetUserIdAsync(user).ConfigureAwait(false);
            if (string.IsNullOrEmpty(userId))
            {
                _logger.LogWarning("QR code generation failed: User ID not found in token");
                return new QrCodeGenerationResponseDto
                {
                    Success = false,
                    Message = "User ID not found in token"
                };
            }

            var isAuthorized = await _userContextService.IsAuthorizedAsync(user, userId, "Admin", "Instructor").ConfigureAwait(false);
            if (!isAuthorized)
            {
                _logger.LogWarning("QR code generation failed: User not authorized");
                return new QrCodeGenerationResponseDto
                {
                    Success = false,
                    Message = "You are not authorized to generate QR codes"
                };
            }

            // Validate entities exist
            var validationError = await ValidateEntitiesExistAsync(qrCodeRequest.ScheduleId, qrCodeRequest.SectionId, qrCodeRequest.ActualRoomId).ConfigureAwait(false);
            if (validationError != null)
            {
                return new QrCodeGenerationResponseDto
                {
                    Success = false,
                    Message = validationError
                };
            }

            // Generate unique QR hash
            var qrHash = await GenerateUniqueQrHashAsync().ConfigureAwait(false);
            var expiresAt = DateTime.UtcNow.AddMinutes(qrCodeRequest.ExpirationMinutes);

            // Create QR code entity
            var qrCode = new QrCode
            {
                ScheduleId = qrCodeRequest.ScheduleId,
                SectionId = qrCodeRequest.SectionId,
                ActualRoomId = qrCodeRequest.ActualRoomId,
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
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error occurred while generating QR code");
            return new QrCodeGenerationResponseDto
            {
                Success = false,
                Message = "An error occurred while generating the QR code"
            };
        }
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
            if (updateQrCode.ActualRoomId.HasValue)
            {
                // Validate new room exists
                var classroom = await _classroomRepository.GetClassroomByIdAsync(updateQrCode.ActualRoomId.Value).ConfigureAwait(false);
                if (classroom == null)
                {
                    return (null, "The specified classroom does not exist");
                }
                existingQrCode.ActualRoomId = updateQrCode.ActualRoomId.Value;
            }

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

            var responseDto = await MapToResponseDtoAsync(updatedQrCode).ConfigureAwait(false);
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

            var responseDto = new QrCodeValidationResponseDto
            {
                IsValid = true,
                Message = "QR code is valid",
                QrCodeId = qrCode.Id,
                ScheduleId = qrCode.ScheduleId,
                SectionId = qrCode.SectionId,
                ActualRoomId = qrCode.ActualRoomId,
                ExpiresAt = qrCode.ExpiresAt,
                RemainingUsage = remainingUsage,
                ScheduleTitle = qrCode.Schedule != null ? $"{qrCode.Schedule.DayOfWeek} {qrCode.Schedule.TimeIn}-{qrCode.Schedule.TimeOut}" : null,
                SectionName = qrCode.Section?.Name,
                RoomName = qrCode.ActualRoom?.Name
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
        try
        {
            _logger.LogInformation("Scanning QR code with hash: {QrHash} for student ID: {StudentId}", 
                validateQrCode.QrHash, validateQrCode.StudentId);

            // Validate QR code first
            var qrCode = await _qrCodeRepository.ValidateQrCodeForUsageAsync(validateQrCode.QrHash).ConfigureAwait(false);
            if (qrCode == null)
            {
                _logger.LogWarning("QR code scan failed: Invalid QR code");
                return new QrCodeScanResponseDto
                {
                    Success = false,
                    Message = "QR code is invalid, expired, or has reached its usage limit",
                    AttendanceMarked = false
                };
            }

            // Validate student exists
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

            // Check if student belongs to the section
            if (student.SectionId != qrCode.SectionId)
            {
                _logger.LogWarning("QR code scan failed: Student does not belong to this section");
                return new QrCodeScanResponseDto
                {
                    Success = false,
                    Message = "Student does not belong to this section",
                    AttendanceMarked = false,
                    StudentName = $"{student.Firstname} {student.Lastname}"
                };
            }

            // Increment usage count
            await _qrCodeRepository.IncrementUsageCountByHashAsync(validateQrCode.QrHash).ConfigureAwait(false);
            await _qrCodeRepository.SaveChangesAsync().ConfigureAwait(false);

            // Calculate remaining scans
            var remainingScans = qrCode.MaxUsage.HasValue ? 
                Math.Max(0, qrCode.MaxUsage.Value - (qrCode.UsageCount + 1)) : 
                int.MaxValue;

            var responseDto = new QrCodeScanResponseDto
            {
                Success = true,
                Message = "Attendance marked successfully",
                AttendanceMarked = true,
                AttendanceTime = DateTime.UtcNow,
                StudentName = $"{student.Firstname} {student.Lastname}",
                ClassName = qrCode.Section?.Name ?? "Unknown",
                SubjectName = qrCode.Schedule?.Subject?.Name ?? "Unknown",
                RoomName = qrCode.ActualRoom?.Name ?? "Unknown",
                InstructorName = qrCode.Schedule?.Instructor?.Firstname + " " + qrCode.Schedule?.Instructor?.Lastname ?? "Unknown",
                RemainingScans = remainingScans
            };

            _logger.LogInformation("Successfully processed QR code scan for student ID: {StudentId}", validateQrCode.StudentId);
            return responseDto;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error occurred while scanning QR code");
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

    public async Task<string> GenerateUniqueQrHashAsync()
    {
        string qrHash;
        bool exists;
        
        do
        {
            qrHash = GenerateQrHash();
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
                var responseDto = await MapToResponseDtoAsync(qrCode).ConfigureAwait(false);
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

            var responseDto = await MapToResponseDtoAsync(updatedQrCode).ConfigureAwait(false);
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

    private static string GenerateQrHash()
    {
        using var rng = RandomNumberGenerator.Create();
        var bytes = new byte[32];
        rng.GetBytes(bytes);
        return Convert.ToBase64String(bytes).Replace("+", "-").Replace("/", "_").Replace("=", "");
    }

    private async Task<QrCodeResponseDto> MapToResponseDtoAsync(QrCode qrCode)
    {
        return new QrCodeResponseDto
        {
            Id = qrCode.Id,
            ScheduleId = qrCode.ScheduleId,
            SectionId = qrCode.SectionId,
            ActualRoomId = qrCode.ActualRoomId,
            QrHash = qrCode.QrHash,
            GeneratedAt = qrCode.GeneratedAt,
            ExpiresAt = qrCode.ExpiresAt,
            IsActive = qrCode.IsActive,
            UsageCount = qrCode.UsageCount,
            MaxUsage = qrCode.MaxUsage,
            CreatedAt = qrCode.CreatedAt,
            UpdatedAt = qrCode.UpdatedAt,
            ScheduleTitle = qrCode.Schedule != null ? $"{qrCode.Schedule.DayOfWeek} {qrCode.Schedule.TimeIn}-{qrCode.Schedule.TimeOut}" : null,
            SectionName = qrCode.Section?.Name,
            ActualRoomName = qrCode.ActualRoom?.Name,
            SubjectName = qrCode.Schedule?.Subject?.Name,
            InstructorName = qrCode.Schedule?.Instructor != null ? 
                $"{qrCode.Schedule.Instructor.Firstname} {qrCode.Schedule.Instructor.Lastname}" : null
        };
    }

    private async Task<string?> ValidateEntitiesExistAsync(int scheduleId, int sectionId, int actualRoomId)
    {
        // Validate schedule exists
        var schedule = await _scheduleRepository.GetScheduleByIdAsync(scheduleId).ConfigureAwait(false);
        if (schedule == null)
        {
            _logger.LogWarning("Entity validation failed: Schedule with ID {ScheduleId} not found", scheduleId);
            return "The specified schedule does not exist";
        }

        // Validate section exists
        var section = await _sectionRepository.GetSectionByIdAsync(sectionId).ConfigureAwait(false);
        if (section == null)
        {
            _logger.LogWarning("Entity validation failed: Section with ID {SectionId} not found", sectionId);
            return "The specified section does not exist";
        }

        // Validate classroom exists
        var classroom = await _classroomRepository.GetClassroomByIdAsync(actualRoomId).ConfigureAwait(false);
        if (classroom == null)
        {
            _logger.LogWarning("Entity validation failed: Classroom with ID {ClassroomId} not found", actualRoomId);
            return "The specified classroom does not exist";
        }

        return null; // All validations passed
    }

    #endregion
}