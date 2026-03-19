using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using Microsoft.EntityFrameworkCore;
using attendance_monitoring.Constants;
using attendance_monitoring.Exceptions;
using attendance_monitoring.Helpers;
using attendance_monitoring.IRepository;
using attendance_monitoring.IServices;
using attendance_monitoring.Models.DTO.Request;
using attendance_monitoring.Models.DTO.Response;
using QrCodeEntity = attendance_monitoring.Classes.QrCode;

namespace attendance_monitoring.Services.QrCode;

internal sealed class QrCodeGenerationService
{
    private const int MaxCreateRetries = 3;
    private const int MaxHashGenerationAttempts = 10;
    private static readonly string[] QrHashConstraintHints = ["IX_QrCodes_QrHash", "QrCodes.QrHash", "QrHash"];

    private readonly IQrCodeRepository _qrCodeRepository;
    private readonly INotificationService _notificationService;
    private readonly QrCodeAuthorizationService _authorizationService;
    private readonly ILogger<QrCodeGenerationService> _logger;

    public QrCodeGenerationService(
        IQrCodeRepository qrCodeRepository,
        INotificationService notificationService,
        QrCodeAuthorizationService authorizationService,
        ILogger<QrCodeGenerationService> logger)
    {
        _qrCodeRepository = qrCodeRepository ?? throw new ArgumentNullException(nameof(qrCodeRepository));
        _notificationService = notificationService ?? throw new ArgumentNullException(nameof(notificationService));
        _authorizationService = authorizationService ?? throw new ArgumentNullException(nameof(authorizationService));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task<QrCodeResponseDto> CreateQrCodeAsync(CreateQrCode createQrCode, ClaimsPrincipal user)
    {
        for (int attempt = 1; attempt <= MaxCreateRetries; attempt++)
        {
            try
            {
                _logger.LogInformation("Creating QR code for session ID: {SessionId} (Attempt {Attempt})",
                    createQrCode.SessionId, attempt);

                var userId = await _authorizationService.UserContext.GetUserIdAsync(user).ConfigureAwait(false);
                if (string.IsNullOrEmpty(userId))
                {
                    _logger.LogWarning("QR code creation failed: User ID not found in token");
                    throw new ValidationException("User ID not found in token");
                }

                var isAuthorized = await _authorizationService.UserContext
                    .IsAuthorizedAsync(user, userId, RoleConstants.Admin, RoleConstants.Instructor)
                    .ConfigureAwait(false);
                if (!isAuthorized)
                {
                    _logger.LogWarning("QR code creation failed: User not authorized");
                    throw new EntityUnauthorizedException("QrCode", "Create", userId);
                }

                await _authorizationService.ValidateSessionExistsOrThrowAsync(createQrCode.SessionId).ConfigureAwait(false);

                var qrCode = new QrCodeEntity
                {
                    SessionId = createQrCode.SessionId,
                    QrHash = createQrCode.QrHash,
                    ExpiresAt = createQrCode.ExpiresAt,
                    MaxUsage = createQrCode.MaxUsage
                };

                var createdQrCode = await _qrCodeRepository.CreateQrCodeAsync(qrCode).ConfigureAwait(false);
                await _qrCodeRepository.SaveChangesAsync().ConfigureAwait(false);

                var responseDto = QrCodeMapper.MapToResponseDto(createdQrCode);
                _logger.LogInformation("Successfully created QR code with ID: {QrCodeId}", createdQrCode.Id);

                return responseDto;
            }
            catch (DbUpdateException ex) when (IsQrHashCollision(ex) && attempt < MaxCreateRetries)
            {
                _logger.LogWarning(ex, "QR hash collision on attempt {Attempt}/{MaxRetries} for session {SessionId}, retrying...",
                    attempt, MaxCreateRetries, createQrCode.SessionId);

                await DelayBeforeRetryAsync(attempt).ConfigureAwait(false);
                var entropy = $"{createQrCode.SessionId}{attempt}{DateTime.UtcNow.Ticks}";
                createQrCode.QrHash = GenerateQrHash(entropy);
            }
            catch (DbUpdateException ex) when (IsQrHashCollision(ex))
            {
                _logger.LogError(ex, "Failed to create QR code after {MaxRetries} attempts due to QR hash collision", MaxCreateRetries);
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
        var attempts = 0;

        while (attempts < MaxCreateRetries)
        {
            try
            {
                _logger.LogInformation("Generating QR code for session ID: {SessionId} (Attempt {Attempt})",
                    qrCodeRequest.SessionId, attempts + 1);

                var userId = await _authorizationService.UserContext.GetUserIdAsync(user).ConfigureAwait(false);
                if (string.IsNullOrEmpty(userId))
                {
                    _logger.LogWarning("QR code generation failed: User ID not found in token");
                    return new QrCodeGenerationResponseDto { Success = false, Message = "User ID not found in token" };
                }

                var isAuthorized = await _authorizationService.UserContext
                    .IsAuthorizedAsync(user, userId, RoleConstants.Admin, RoleConstants.Instructor)
                    .ConfigureAwait(false);
                if (!isAuthorized)
                {
                    _logger.LogWarning("QR code generation failed: User not authorized");
                    return new QrCodeGenerationResponseDto { Success = false, Message = "You are not authorized to generate QR codes" };
                }

                var validationError = await _authorizationService.ValidateSessionExistsAsync(qrCodeRequest.SessionId).ConfigureAwait(false);
                if (validationError != null)
                {
                    return new QrCodeGenerationResponseDto { Success = false, Message = validationError };
                }

                var qrHash = await GenerateUniqueQrHashAsync(qrCodeRequest.UniqueHash).ConfigureAwait(false);
                var expiresAt = DateTime.UtcNow.AddMinutes(qrCodeRequest.ExpirationMinutes);

                var qrCode = new QrCodeEntity
                {
                    SessionId = qrCodeRequest.SessionId,
                    QrHash = qrHash,
                    ExpiresAt = expiresAt,
                    MaxUsage = qrCodeRequest.MaxUsage
                };

                var createdQrCode = await _qrCodeRepository.CreateQrCodeAsync(qrCode).ConfigureAwait(false);
                await _qrCodeRepository.SaveChangesAsync().ConfigureAwait(false);

                _logger.LogInformation("Successfully generated QR code with ID: {QrCodeId}", createdQrCode.Id);

                await _notificationService.NotifyQrCodeGeneratedAsync(createdQrCode.Id, userId).ConfigureAwait(false);

                return new QrCodeGenerationResponseDto
                {
                    Success = true,
                    Message = "QR code generated successfully",
                    QrHash = qrHash,
                    QrCodeData = qrHash,
                    GeneratedAt = createdQrCode.GeneratedAt,
                    ExpiresAt = expiresAt,
                    MaxUsage = qrCodeRequest.MaxUsage,
                    QrCodeId = createdQrCode.Id
                };
            }
            catch (DbUpdateException ex) when (IsQrHashCollision(ex))
            {
                attempts++;
                _logger.LogWarning(ex, "Attempt {Attempt} to persist a generated QR hash failed due to a QR hash collision. Retrying...", attempts);
                if (attempts >= MaxCreateRetries)
                {
                    _logger.LogError(ex, "Failed to generate a unique QR hash after {MaxRetries} attempts.", MaxCreateRetries);
                    return new QrCodeGenerationResponseDto { Success = false, Message = "Failed to generate a unique QR hash. Please try again." };
                }

                await DelayBeforeRetryAsync(attempts).ConfigureAwait(false);
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

    public async Task<string> GenerateUniqueQrHashAsync(string? clientHash = null)
    {
        for (int attempt = 1; attempt <= MaxHashGenerationAttempts; attempt++)
        {
            var qrHash = GenerateQrHash(clientHash);
            var exists = await _qrCodeRepository.QrHashExistsAsync(qrHash).ConfigureAwait(false);
            if (!exists)
            {
                return qrHash;
            }

            _logger.LogWarning("Generated QR hash already exists on attempt {Attempt}/{MaxAttempts}. Retrying hash generation.",
                attempt, MaxHashGenerationAttempts);
        }

        throw new EntityServiceException("QrCode", "GenerateUniqueQrHash",
            "Unable to generate a unique QR hash after multiple attempts.");
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

    private static string GenerateQrHash(string? clientHash = null)
    {
        using var rng = RandomNumberGenerator.Create();
        var serverBytes = new byte[32];
        rng.GetBytes(serverBytes);

        if (!string.IsNullOrEmpty(clientHash))
        {
            var combinedBytes = Encoding.UTF8.GetBytes(clientHash).Concat(serverBytes).ToArray();
            var hashBytes = System.Security.Cryptography.SHA256.HashData(combinedBytes);
            return Convert.ToBase64String(hashBytes).Replace("+", "-").Replace("/", "_").Replace("=", "");
        }

        return Convert.ToBase64String(serverBytes).Replace("+", "-").Replace("/", "_").Replace("=", "");
    }

    private static bool IsQrHashCollision(DbUpdateException ex)
        => ExceptionHandlingHelper.IsUniqueConstraintViolation(ex, QrHashConstraintHints);

    private static Task DelayBeforeRetryAsync(int attempt)
    {
        const int retryDelayMs = 100;
        var jitter = Random.Shared.Next(0, 50);
        return Task.Delay(retryDelayMs * attempt + jitter);
    }
}
