using System.Security.Claims;
using attendance_monitoring.Constants;
using attendance_monitoring.Exceptions;
using attendance_monitoring.Helpers;
using attendance_monitoring.IRepository;
using attendance_monitoring.Models.DTO.Request;
using attendance_monitoring.Models.DTO.Response;

namespace attendance_monitoring.Services.QrCode;

internal sealed class QrCodeWriteService
{
    private readonly IQrCodeRepository _qrCodeRepository;
    private readonly QrCodeAuthorizationService _authorizationService;
    private readonly ILogger<QrCodeWriteService> _logger;

    public QrCodeWriteService(
        IQrCodeRepository qrCodeRepository,
        QrCodeAuthorizationService authorizationService,
        ILogger<QrCodeWriteService> logger)
    {
        _qrCodeRepository = qrCodeRepository ?? throw new ArgumentNullException(nameof(qrCodeRepository));
        _authorizationService = authorizationService ?? throw new ArgumentNullException(nameof(authorizationService));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task<QrCodeResponseDto> UpdateQrCodeAsync(int id, UpdateQrCode updateQrCode, ClaimsPrincipal user)
    {
        try
        {
            _logger.LogInformation("Updating QR code with ID: {QrCodeId}", id);

            var userId = await _authorizationService.UserContext.GetUserIdAsync(user).ConfigureAwait(false);
            if (string.IsNullOrEmpty(userId))
            {
                _logger.LogWarning("QR code update failed: User ID not found in token");
                throw new ValidationException("User ID not found in token");
            }

            var isAuthorized = await _authorizationService.UserContext
                .IsAuthorizedAsync(user, userId, RoleConstants.Admin, RoleConstants.Instructor)
                .ConfigureAwait(false);
            if (!isAuthorized)
            {
                _logger.LogWarning("QR code update failed: User not authorized");
                throw new EntityUnauthorizedException("QrCode", "Update", userId);
            }

            var existingQrCode = await _qrCodeRepository.GetQrCodeByIdAsync(id).ConfigureAwait(false);
            if (existingQrCode == null)
            {
                _logger.LogWarning("QR code update failed: QR code not found");
                throw new EntityNotFoundException<int>("QrCode", id);
            }

            if (updateQrCode.ExpiresAt.HasValue) existingQrCode.ExpiresAt = updateQrCode.ExpiresAt.Value;
            if (updateQrCode.IsActive.HasValue) existingQrCode.IsActive = updateQrCode.IsActive.Value;
            if (updateQrCode.MaxUsage.HasValue) existingQrCode.MaxUsage = updateQrCode.MaxUsage.Value;

            var updatedQrCode = await _qrCodeRepository.UpdateQrCodeAsync(existingQrCode).ConfigureAwait(false);
            await _qrCodeRepository.SaveChangesAsync().ConfigureAwait(false);

            var responseDto = QrCodeMapper.MapToResponseDto(updatedQrCode);
            _logger.LogInformation("Successfully updated QR code with ID: {QrCodeId}", id);

            return responseDto;
        }
        catch (ValidationException) { throw; }
        catch (EntityUnauthorizedException) { throw; }
        catch (EntityNotFoundException<int>) { throw; }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error occurred while updating QR code with ID: {QrCodeId}", id);
            throw new EntityServiceException("QrCode", "Update", "An error occurred while updating the QR code", ex);
        }
    }

    public async Task DeactivateQrCodeAsync(int id, ClaimsPrincipal user)
    {
        try
        {
            _logger.LogInformation("Deactivating QR code with ID: {QrCodeId}", id);

            var userId = await _authorizationService.UserContext.GetUserIdAsync(user).ConfigureAwait(false);
            if (string.IsNullOrEmpty(userId))
            {
                _logger.LogWarning("QR code deactivation failed: User ID not found in token");
                throw new ValidationException("User ID not found in token");
            }

            var isAuthorized = await _authorizationService.UserContext
                .IsAuthorizedAsync(user, userId, RoleConstants.Admin, RoleConstants.Instructor)
                .ConfigureAwait(false);
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
            _logger.LogError(ex, "Error occurred while deactivating QR code with ID: {QrCodeId}", id);
            throw new EntityServiceException("QrCode", "Deactivate", "An error occurred while deactivating the QR code", ex);
        }
    }

    public async Task RevokeQrCodeAsync(int id, string? reason, ClaimsPrincipal user)
    {
        try
        {
            _logger.LogInformation("Revoking QR code with ID: {QrCodeId}", id);

            var userId = await _authorizationService.UserContext.GetUserIdAsync(user).ConfigureAwait(false);
            if (string.IsNullOrEmpty(userId))
            {
                _logger.LogWarning("QR code revocation failed: User ID not found in token");
                throw new ValidationException("User ID not found in token");
            }

            var isAuthorized = await _authorizationService.UserContext
                .IsAuthorizedAsync(user, userId, RoleConstants.Admin, RoleConstants.Instructor)
                .ConfigureAwait(false);
            if (!isAuthorized)
            {
                _logger.LogWarning("QR code revocation failed: User not authorized");
                throw new EntityUnauthorizedException("QrCode", "Revoke", userId);
            }

            var qrCode = await _qrCodeRepository.GetQrCodeByIdAsync(id).ConfigureAwait(false);
            if (qrCode == null)
            {
                _logger.LogWarning("QR code revocation failed: QR code not found");
                throw new EntityNotFoundException<int>("QrCode", id);
            }

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
            _logger.LogError(ex, "Error occurred while revoking QR code with ID: {QrCodeId}", id);
            throw new EntityServiceException("QrCode", "Revoke", "An error occurred while revoking the QR code", ex);
        }
    }

    public async Task DeactivateQrCodeByHashAsync(string qrHash, ClaimsPrincipal user)
    {
        try
        {
            _logger.LogInformation("Deactivating QR code with hash: {QrHash}", qrHash);

            var userId = await _authorizationService.UserContext.GetUserIdAsync(user).ConfigureAwait(false);
            if (string.IsNullOrEmpty(userId))
            {
                _logger.LogWarning("QR code deactivation failed: User ID not found in token");
                throw new ValidationException("User ID not found in token");
            }

            var isAuthorized = await _authorizationService.UserContext
                .IsAuthorizedAsync(user, userId, RoleConstants.Admin, RoleConstants.Instructor)
                .ConfigureAwait(false);
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
            _logger.LogError(ex, "Error occurred while deactivating QR code with hash: {QrHash}", qrHash);
            throw new EntityServiceException("QrCode", "Deactivate", "An error occurred while deactivating the QR code", ex);
        }
    }

    public async Task RevokeQrCodeByHashAsync(string qrHash, string? reason, ClaimsPrincipal user)
    {
        try
        {
            _logger.LogInformation("Revoking QR code with hash: {QrHash}", qrHash);

            var userId = await _authorizationService.UserContext.GetUserIdAsync(user).ConfigureAwait(false);
            if (string.IsNullOrEmpty(userId))
            {
                _logger.LogWarning("QR code revocation failed: User ID not found in token");
                throw new ValidationException("User ID not found in token");
            }

            var isAuthorized = await _authorizationService.UserContext
                .IsAuthorizedAsync(user, userId, RoleConstants.Admin, RoleConstants.Instructor)
                .ConfigureAwait(false);
            if (!isAuthorized)
            {
                _logger.LogWarning("QR code revocation failed: User not authorized");
                throw new EntityUnauthorizedException("QrCode", "Revoke", userId);
            }

            var qrCode = await _qrCodeRepository.GetQrCodeByHashAsync(qrHash).ConfigureAwait(false);
            if (qrCode == null)
            {
                _logger.LogWarning("QR code revocation failed: QR code not found");
                throw new EntityNotFoundException<string>("QrCode", qrHash);
            }

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
            _logger.LogError(ex, "Error occurred while revoking QR code with hash: {QrHash}", qrHash);
            throw new EntityServiceException("QrCode", "Revoke", "An error occurred while revoking the QR code", ex);
        }
    }

    public async Task ReactivateQrCodeAsync(int id, ClaimsPrincipal user)
    {
        try
        {
            _logger.LogInformation("Reactivating QR code with ID: {QrCodeId}", id);

            var userId = await _authorizationService.UserContext.GetUserIdAsync(user).ConfigureAwait(false);
            if (string.IsNullOrEmpty(userId))
            {
                _logger.LogWarning("QR code reactivation failed: User ID not found in token");
                throw new ValidationException("User ID not found in token");
            }

            var isAuthorized = await _authorizationService.UserContext
                .IsAuthorizedAsync(user, userId, RoleConstants.Admin, RoleConstants.Instructor)
                .ConfigureAwait(false);
            if (!isAuthorized)
            {
                _logger.LogWarning("QR code reactivation failed: User not authorized");
                throw new EntityUnauthorizedException("QrCode", "Reactivate", userId);
            }

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
        catch (ValidationException) { throw; }
        catch (EntityNotFoundException<int>) { throw; }
        catch (EntityUnauthorizedException) { throw; }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error occurred while reactivating QR code with ID: {QrCodeId}", id);
            throw ExceptionHandlingHelper.CreateServiceException("QrCode", "Reactivate", ex);
        }
    }

    public async Task ReactivateQrCodeByHashAsync(string qrHash, ClaimsPrincipal user)
    {
        try
        {
            _logger.LogInformation("Reactivating QR code with hash: {QrHash}", qrHash);

            var userId = await _authorizationService.UserContext.GetUserIdAsync(user).ConfigureAwait(false);
            if (string.IsNullOrEmpty(userId))
            {
                _logger.LogWarning("QR code reactivation failed: User ID not found in token");
                throw new EntityUnauthorizedException("QrCode", "Reactivate", "unknown", "User ID not found in token");
            }

            var isAuthorized = await _authorizationService.UserContext
                .IsAuthorizedAsync(user, userId, RoleConstants.Admin, RoleConstants.Instructor)
                .ConfigureAwait(false);
            if (!isAuthorized)
            {
                _logger.LogWarning("QR code reactivation failed: User not authorized");
                throw new EntityUnauthorizedException("QrCode", "Reactivate", userId, "You are not authorized to reactivate QR codes");
            }

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
        catch (ValidationException) { throw; }
        catch (EntityNotFoundException<string>) { throw; }
        catch (EntityUnauthorizedException) { throw; }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error occurred while reactivating QR code with hash: {QrHash}", qrHash);
            throw ExceptionHandlingHelper.CreateServiceException("QrCode", "Reactivate", ex);
        }
    }

    public async Task DeleteQrCodeAsync(int id, ClaimsPrincipal user)
    {
        try
        {
            _logger.LogInformation("Deleting QR code with ID: {QrCodeId}", id);

            var userId = await _authorizationService.UserContext.GetUserIdAsync(user).ConfigureAwait(false);
            if (string.IsNullOrEmpty(userId))
            {
                _logger.LogWarning("QR code deletion failed: User ID not found in token");
                throw new EntityUnauthorizedException("QrCode", "Delete", "unknown", "User ID not found in token");
            }

            var isAuthorized = await _authorizationService.UserContext
                .IsAuthorizedAsync(user, userId, RoleConstants.Admin)
                .ConfigureAwait(false);
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
        catch (EntityNotFoundException<int>) { throw; }
        catch (EntityUnauthorizedException) { throw; }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error occurred while deleting QR code with ID: {QrCodeId}", id);
            throw ExceptionHandlingHelper.CreateServiceException("QrCode", "Delete", ex);
        }
    }

    public async Task<QrCodeResponseDto> ExtendQrCodeExpirationAsync(int id, int additionalMinutes, ClaimsPrincipal user)
    {
        try
        {
            _logger.LogInformation("Extending expiration for QR code ID: {QrCodeId} by {Minutes} minutes", id, additionalMinutes);

            var userId = await _authorizationService.UserContext.GetUserIdAsync(user).ConfigureAwait(false);
            if (string.IsNullOrEmpty(userId))
            {
                _logger.LogWarning("QR code expiration extension failed: User ID not found in token");
                throw new EntityUnauthorizedException("QrCode", "ExtendExpiration", "unknown", "User ID not found in token");
            }

            var isAuthorized = await _authorizationService.UserContext
                .IsAuthorizedAsync(user, userId, RoleConstants.Admin, RoleConstants.Instructor)
                .ConfigureAwait(false);
            if (!isAuthorized)
            {
                _logger.LogWarning("QR code expiration extension failed: User not authorized");
                throw new EntityUnauthorizedException("QrCode", "ExtendExpiration", userId, "You are not authorized to extend QR code expiration");
            }

            var qrCode = await _qrCodeRepository.GetQrCodeByIdAsync(id).ConfigureAwait(false);
            if (qrCode == null)
            {
                _logger.LogWarning("QR code expiration extension failed: QR code not found");
                throw new EntityNotFoundException<int>("QrCode", id);
            }

            qrCode.ExpiresAt = qrCode.ExpiresAt.AddMinutes(additionalMinutes);

            var updatedQrCode = await _qrCodeRepository.UpdateQrCodeAsync(qrCode).ConfigureAwait(false);
            await _qrCodeRepository.SaveChangesAsync().ConfigureAwait(false);

            var responseDto = QrCodeMapper.MapToResponseDto(updatedQrCode);
            _logger.LogInformation("Successfully extended expiration for QR code ID: {QrCodeId}", id);

            return responseDto;
        }
        catch (EntityNotFoundException<int>) { throw; }
        catch (EntityUnauthorizedException) { throw; }
        catch (Exception ex)
        {
            _logger.LogError(ex,
                "Error occurred while extending QR code expiration for ID: {QrCodeId} by {Minutes} minutes",
                id,
                additionalMinutes);
            throw ExceptionHandlingHelper.CreateServiceException("QrCode", "ExtendExpiration", ex);
        }
    }

    #region UUID Entrypoints

    public async Task<QrCodeResponseDto> UpdateQrCodeByUuidAsync(Guid uuid, UpdateQrCode updateQrCode, ClaimsPrincipal user)
    {
        try
        {
            _logger.LogInformation("Updating QR code with UUID: {QrCodeUuid}", uuid);

            var userId = await _authorizationService.UserContext.GetUserIdAsync(user).ConfigureAwait(false);
            if (string.IsNullOrEmpty(userId))
            {
                _logger.LogWarning("QR code update failed: User ID not found in token");
                throw new ValidationException("User ID not found in token");
            }

            var isAuthorized = await _authorizationService.UserContext
                .IsAuthorizedAsync(user, userId, RoleConstants.Admin, RoleConstants.Instructor)
                .ConfigureAwait(false);
            if (!isAuthorized)
            {
                _logger.LogWarning("QR code update failed: User not authorized");
                throw new EntityUnauthorizedException("QrCode", "Update", userId);
            }

            var existingQrCode = await _qrCodeRepository.GetQrCodeByUuidAsync(uuid).ConfigureAwait(false);
            if (existingQrCode == null)
            {
                _logger.LogWarning("QR code update failed: QR code not found");
                throw new EntityNotFoundException<Guid>("QrCode", uuid);
            }

            if (updateQrCode.ExpiresAt.HasValue) existingQrCode.ExpiresAt = updateQrCode.ExpiresAt.Value;
            if (updateQrCode.IsActive.HasValue) existingQrCode.IsActive = updateQrCode.IsActive.Value;
            if (updateQrCode.MaxUsage.HasValue) existingQrCode.MaxUsage = updateQrCode.MaxUsage.Value;

            var updatedQrCode = await _qrCodeRepository.UpdateQrCodeAsync(existingQrCode).ConfigureAwait(false);
            await _qrCodeRepository.SaveChangesAsync().ConfigureAwait(false);

            var responseDto = QrCodeMapper.MapToResponseDto(updatedQrCode);
            _logger.LogInformation("Successfully updated QR code with UUID: {QrCodeUuid}", uuid);

            return responseDto;
        }
        catch (ValidationException) { throw; }
        catch (EntityUnauthorizedException) { throw; }
        catch (EntityNotFoundException<Guid>) { throw; }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error occurred while updating QR code with UUID: {QrCodeUuid}", uuid);
            throw new EntityServiceException("QrCode", "Update", "An error occurred while updating the QR code", ex);
        }
    }

    public async Task RevokeQrCodeByUuidAsync(Guid uuid, string? reason, ClaimsPrincipal user)
    {
        try
        {
            _logger.LogInformation("Revoking QR code with UUID: {QrCodeUuid}", uuid);

            var userId = await _authorizationService.UserContext.GetUserIdAsync(user).ConfigureAwait(false);
            if (string.IsNullOrEmpty(userId))
            {
                _logger.LogWarning("QR code revocation failed: User ID not found in token");
                throw new ValidationException("User ID not found in token");
            }

            var isAuthorized = await _authorizationService.UserContext
                .IsAuthorizedAsync(user, userId, RoleConstants.Admin, RoleConstants.Instructor)
                .ConfigureAwait(false);
            if (!isAuthorized)
            {
                _logger.LogWarning("QR code revocation failed: User not authorized");
                throw new EntityUnauthorizedException("QrCode", "Revoke", userId);
            }

            var qrCode = await _qrCodeRepository.GetQrCodeByUuidAsync(uuid).ConfigureAwait(false);
            if (qrCode == null)
            {
                _logger.LogWarning("QR code revocation failed: QR code not found");
                throw new EntityNotFoundException<Guid>("QrCode", uuid);
            }

            qrCode.IsActive = false;
            qrCode.RevokedAt = DateTime.UtcNow;
            qrCode.RevokedBy = userId;
            qrCode.RevocationReason = reason;
            qrCode.UpdatedAt = DateTime.UtcNow;

            await _qrCodeRepository.UpdateQrCodeAsync(qrCode).ConfigureAwait(false);
            await _qrCodeRepository.SaveChangesAsync().ConfigureAwait(false);

            _logger.LogInformation("Successfully revoked QR code with UUID: {QrCodeUuid} by user: {UserId}", uuid, userId);
        }
        catch (ValidationException) { throw; }
        catch (EntityUnauthorizedException) { throw; }
        catch (EntityNotFoundException<Guid>) { throw; }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error occurred while revoking QR code with UUID: {QrCodeUuid}", uuid);
            throw new EntityServiceException("QrCode", "Revoke", "An error occurred while revoking the QR code", ex);
        }
    }

    public async Task ReactivateQrCodeByUuidAsync(Guid uuid, ClaimsPrincipal user)
    {
        try
        {
            _logger.LogInformation("Reactivating QR code with UUID: {QrCodeUuid}", uuid);

            var userId = await _authorizationService.UserContext.GetUserIdAsync(user).ConfigureAwait(false);
            if (string.IsNullOrEmpty(userId))
            {
                _logger.LogWarning("QR code reactivation failed: User ID not found in token");
                throw new ValidationException("User ID not found in token");
            }

            var isAuthorized = await _authorizationService.UserContext
                .IsAuthorizedAsync(user, userId, RoleConstants.Admin, RoleConstants.Instructor)
                .ConfigureAwait(false);
            if (!isAuthorized)
            {
                _logger.LogWarning("QR code reactivation failed: User not authorized");
                throw new EntityUnauthorizedException("QrCode", "Reactivate", userId);
            }

            var qrCode = await _qrCodeRepository.GetQrCodeByUuidAsync(uuid).ConfigureAwait(false);
            if (qrCode == null)
            {
                _logger.LogWarning("QR code reactivation failed: QR code not found");
                throw new EntityNotFoundException<Guid>("QrCode", uuid);
            }

            if (qrCode.ExpiresAt <= DateTime.UtcNow)
            {
                _logger.LogWarning("QR code reactivation failed: QR code has expired");
                throw new ValidationException("Cannot reactivate an expired QR code");
            }

            var result = await _qrCodeRepository.ReactivateQrCodeAsync(qrCode.Id).ConfigureAwait(false);
            if (!result)
            {
                _logger.LogWarning("QR code reactivation failed: QR code not found");
                throw new EntityNotFoundException<Guid>("QrCode", uuid);
            }

            await _qrCodeRepository.SaveChangesAsync().ConfigureAwait(false);
            _logger.LogInformation("Successfully reactivated QR code with UUID: {QrCodeUuid} by user: {UserId}", uuid, userId);
        }
        catch (ValidationException) { throw; }
        catch (EntityNotFoundException<Guid>) { throw; }
        catch (EntityUnauthorizedException) { throw; }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error occurred while reactivating QR code with UUID: {QrCodeUuid}", uuid);
            throw ExceptionHandlingHelper.CreateServiceException("QrCode", "Reactivate", ex);
        }
    }

    public async Task DeleteQrCodeByUuidAsync(Guid uuid, ClaimsPrincipal user)
    {
        try
        {
            _logger.LogInformation("Deleting QR code with UUID: {QrCodeUuid}", uuid);

            var userId = await _authorizationService.UserContext.GetUserIdAsync(user).ConfigureAwait(false);
            if (string.IsNullOrEmpty(userId))
            {
                _logger.LogWarning("QR code deletion failed: User ID not found in token");
                throw new EntityUnauthorizedException("QrCode", "Delete", "unknown", "User ID not found in token");
            }

            var isAuthorized = await _authorizationService.UserContext
                .IsAuthorizedAsync(user, userId, RoleConstants.Admin)
                .ConfigureAwait(false);
            if (!isAuthorized)
            {
                _logger.LogWarning("QR code deletion failed: User not authorized");
                throw new EntityUnauthorizedException("QrCode", "Delete", userId, "You are not authorized to delete QR codes");
            }

            var qrCode = await _qrCodeRepository.GetQrCodeByUuidAsync(uuid).ConfigureAwait(false);
            if (qrCode == null)
            {
                _logger.LogWarning("QR code deletion failed: QR code not found");
                throw new EntityNotFoundException<Guid>("QrCode", uuid);
            }

            var result = await _qrCodeRepository.DeleteQrCodeAsync(qrCode.Id).ConfigureAwait(false);
            if (!result)
            {
                _logger.LogWarning("QR code deletion failed: QR code not found");
                throw new EntityNotFoundException<Guid>("QrCode", uuid);
            }

            await _qrCodeRepository.SaveChangesAsync().ConfigureAwait(false);
            _logger.LogInformation("Successfully deleted QR code with UUID: {QrCodeUuid}", uuid);
        }
        catch (EntityNotFoundException<Guid>) { throw; }
        catch (EntityUnauthorizedException) { throw; }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error occurred while deleting QR code with UUID: {QrCodeUuid}", uuid);
            throw ExceptionHandlingHelper.CreateServiceException("QrCode", "Delete", ex);
        }
    }

    public async Task<QrCodeResponseDto> ExtendQrCodeExpirationByUuidAsync(Guid uuid, int additionalMinutes, ClaimsPrincipal user)
    {
        try
        {
            _logger.LogInformation("Extending expiration for QR code UUID: {QrCodeUuid} by {Minutes} minutes", uuid, additionalMinutes);

            var userId = await _authorizationService.UserContext.GetUserIdAsync(user).ConfigureAwait(false);
            if (string.IsNullOrEmpty(userId))
            {
                _logger.LogWarning("QR code expiration extension failed: User ID not found in token");
                throw new EntityUnauthorizedException("QrCode", "ExtendExpiration", "unknown", "User ID not found in token");
            }

            var isAuthorized = await _authorizationService.UserContext
                .IsAuthorizedAsync(user, userId, RoleConstants.Admin, RoleConstants.Instructor)
                .ConfigureAwait(false);
            if (!isAuthorized)
            {
                _logger.LogWarning("QR code expiration extension failed: User not authorized");
                throw new EntityUnauthorizedException("QrCode", "ExtendExpiration", userId, "You are not authorized to extend QR code expiration");
            }

            var qrCode = await _qrCodeRepository.GetQrCodeByUuidAsync(uuid).ConfigureAwait(false);
            if (qrCode == null)
            {
                _logger.LogWarning("QR code expiration extension failed: QR code not found");
                throw new EntityNotFoundException<Guid>("QrCode", uuid);
            }

            qrCode.ExpiresAt = qrCode.ExpiresAt.AddMinutes(additionalMinutes);

            var updatedQrCode = await _qrCodeRepository.UpdateQrCodeAsync(qrCode).ConfigureAwait(false);
            await _qrCodeRepository.SaveChangesAsync().ConfigureAwait(false);

            var responseDto = QrCodeMapper.MapToResponseDto(updatedQrCode);
            _logger.LogInformation("Successfully extended expiration for QR code UUID: {QrCodeUuid}", uuid);

            return responseDto;
        }
        catch (EntityNotFoundException<Guid>) { throw; }
        catch (EntityUnauthorizedException) { throw; }
        catch (Exception ex)
        {
            _logger.LogError(ex,
                "Error occurred while extending QR code expiration for UUID: {QrCodeUuid} by {Minutes} minutes",
                uuid,
                additionalMinutes);
            throw ExceptionHandlingHelper.CreateServiceException("QrCode", "ExtendExpiration", ex);
        }
    }

    #endregion

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

            int? remainingUsage = null;
            if (qrCode.MaxUsage.HasValue)
            {
                remainingUsage = qrCode.MaxUsage.Value - qrCode.UsageCount;
            }

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

    public async Task<int> CleanupExpiredQrCodesAsync(ClaimsPrincipal user)
    {
        try
        {
            _logger.LogInformation("Starting cleanup of expired QR codes");

            var userId = await _authorizationService.UserContext.GetUserIdAsync(user).ConfigureAwait(false);
            if (string.IsNullOrEmpty(userId))
            {
                _logger.LogWarning("QR code cleanup failed: User ID not found in token");
                throw new EntityServiceException("QrCode", "CleanupExpiredQrCodes", "User ID not found in token");
            }

            var isAuthorized = await _authorizationService.UserContext
                .IsAuthorizedAsync(user, userId, RoleConstants.Admin)
                .ConfigureAwait(false);
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
        catch (EntityServiceException) { throw; }
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
            var responseDtos = qrCodes.Select(QrCodeMapper.MapToResponseDto).ToList();

            _logger.LogInformation("Successfully retrieved {Count} QR codes expiring soon", responseDtos.Count);
            return responseDtos;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error occurred while retrieving QR codes expiring within {Minutes} minutes", expiringWithinMinutes);
            throw new EntityServiceException("QrCode", "GetQrCodesExpiringSoon", "An error occurred while retrieving expiring QR codes", ex);
        }
    }
}
