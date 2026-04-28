using System.Security.Claims;
using attendance_monitoring.Classes;
using attendance_monitoring.Exceptions;
using attendance_monitoring.Models.DTO.Request;
using attendance_monitoring.Models.DTO.Response;

namespace attendance_monitoring.IServices;

/// <summary>
/// Service interface for managing QR codes for attendance tracking.
/// </summary>
public interface IQrCodeService
{
    #region Read Operations

    /// <summary>
    /// Retrieves a QR code by its ID.
    /// </summary>
    /// <param name="id">The QR code ID.</param>
    /// <returns>The QR code response DTO if found.</returns>
    /// <exception cref="EntityNotFoundException{Int32}">Thrown when QR code is not found.</exception>
    Task<QrCodeResponseDto?> GetQrCodeByIdAsync(Guid id);

    /// <summary>
    /// Retrieves a QR code by its hash value.
    /// </summary>
    /// <param name="qrHash">The QR code hash.</param>
    /// <returns>The QR code response DTO if found; otherwise, null.</returns>
    Task<QrCodeResponseDto?> GetQrCodeByHashAsync(string qrHash);

    /// <summary>
    /// Retrieves a QR code by its UUID.
    /// </summary>
    /// <param name="id">The QR code UUID.</param>
    /// <returns>The QR code response DTO if found; otherwise, null.</returns>
    Task<QrCodeResponseDto?> GetQrCodeByUuidAsync(Guid id);

    /// <summary>
    /// Retrieves all QR codes for a specific schedule.
    /// </summary>
    /// <param name="scheduleId">The schedule ID.</param>
    /// <returns>A collection of QR code response DTOs for the specified schedule.</returns>
    Task<IEnumerable<QrCodeResponseDto>> GetQrCodesByScheduleIdAsync(Guid scheduleId);

    /// <summary>
    /// Retrieves all QR codes for a specific section.
    /// </summary>
    /// <param name="sectionId">The section ID.</param>
    /// <returns>A collection of QR code response DTOs for the specified section.</returns>
    Task<IEnumerable<QrCodeResponseDto>> GetQrCodesBySectionIdAsync(Guid sectionId);

    /// <summary>
    /// Retrieves all QR codes for a specific session.
    /// </summary>
    /// <param name="sessionId">The session ID.</param>
    /// <returns>A collection of QR code response DTOs for the specified session.</returns>
    Task<IEnumerable<QrCodeResponseDto>> GetQrCodesBySessionIdAsync(Guid sessionId);

    /// <summary>
    /// Retrieves all QR codes for a specific session UUID.
    /// </summary>
    /// <param name="sessionUuid">The session UUID.</param>
    /// <returns>A collection of QR code response DTOs for the specified session.</returns>
    Task<IEnumerable<QrCodeResponseDto>> GetQrCodesBySessionUuidAsync(Guid sessionUuid);

    /// <summary>
    /// Retrieves all active QR codes that have not expired.
    /// </summary>
    /// <returns>A collection of active, non-expired QR code response DTOs.</returns>
    Task<IEnumerable<QrCodeResponseDto>> GetActiveQrCodesAsync();

    /// <summary>
    /// Get scan history for a QR code by ID
    /// </summary>
    /// <param name="qrCodeId">The QR code ID</param>
    /// <param name="instructorId">The authenticated instructor's ID</param>
    /// <param name="userRole">The authenticated user's role</param>
    /// <param name="pageNumber">Page number (default: 1)</param>
    /// <param name="pageSize">Page size (default: 50, max: 100)</param>
    /// <returns>Complete scan history response with pagination and statistics</returns>
    Task<QrCodeScanHistoryResponseDto> GetScanHistoryAsync(
        Guid qrCodeId,
        Guid instructorId,
        string userRole,
        int pageNumber = 1,
        int pageSize = 50);

    /// <summary>
    /// Get scan history for a QR code by hash
    /// </summary>
    /// <param name="qrHash">The QR code hash</param>
    /// <param name="instructorId">The authenticated instructor's ID</param>
    /// <param name="userRole">The authenticated user's role</param>
    /// <param name="pageNumber">Page number (default: 1)</param>
    /// <param name="pageSize">Page size (default: 50, max: 100)</param>
    /// <returns>Complete scan history response with pagination and statistics</returns>
    Task<QrCodeScanHistoryResponseDto> GetScanHistoryByHashAsync(
        string qrHash,
        Guid instructorId,
        string userRole,
        int pageNumber = 1,
        int pageSize = 50);

    /// <summary>
    /// Get scan history for a QR code by UUID.
    /// </summary>
    /// <param name="id">The QR code UUID.</param>
    /// <param name="instructorId">The authenticated instructor's ID.</param>
    /// <param name="userRole">The authenticated user's role.</param>
    /// <param name="pageNumber">Page number (default: 1).</param>
    /// <param name="pageSize">Page size (default: 50, max: 100).</param>
    /// <returns>Complete scan history response with pagination and statistics.</returns>
    Task<QrCodeScanHistoryResponseDto> GetScanHistoryByUuidAsync(
        Guid id,
        Guid instructorId,
        string userRole,
        int pageNumber = 1,
        int pageSize = 50);

    #endregion

    #region Write Operations

    /// <summary>
    /// Creates a new QR code directly from the create DTO.
    /// </summary>
    /// <param name="createQrCode">The QR code creation data.</param>
    /// <param name="user">The authenticated user making the request.</param>
    /// <returns>The created QR code response DTO.</returns>
    /// <exception cref="ValidationException">Thrown when validation fails.</exception>
    /// <exception cref="EntityNotFoundException{Int32}">Thrown when referenced entity is not found.</exception>
    /// <exception cref="EntityUnauthorizedException">Thrown when user is not authorized.</exception>
    /// <exception cref="EntityServiceException">Thrown when a service error occurs.</exception>
    Task<QrCodeResponseDto> CreateQrCodeAsync(CreateQrCode createQrCode, ClaimsPrincipal user);

    /// <summary>
    /// Generates a new QR code with automatic hash generation and validation.
    /// </summary>
    /// <param name="qrCodeRequest">The QR code generation parameters.</param>
    /// <param name="user">The authenticated user making the request.</param>
    /// <returns>A QR code generation response with success status and generated data.</returns>
    Task<QrCodeGenerationResponseDto> GenerateQrCodeAsync(QrCodeRequest qrCodeRequest, ClaimsPrincipal user);

    /// <summary>
    /// Updates an existing QR code.
    /// </summary>
    /// <param name="id">The QR code ID to update.</param>
    /// <param name="updateQrCode">The update data.</param>
    /// <param name="user">The authenticated user making the request.</param>
    /// <returns>The updated QR code response DTO.</returns>
    /// <exception cref="ValidationException">Thrown when validation fails.</exception>
    /// <exception cref="EntityNotFoundException{Int32}">Thrown when QR code is not found.</exception>
    /// <exception cref="EntityUnauthorizedException">Thrown when user is not authorized.</exception>
    Task<QrCodeResponseDto> UpdateQrCodeAsync(Guid id, UpdateQrCode updateQrCode, ClaimsPrincipal user);

    /// <summary>
    /// Updates an existing QR code by its UUID.
    /// </summary>
    /// <param name="id">The QR code UUID to update.</param>
    /// <param name="updateQrCode">The update data.</param>
    /// <param name="user">The authenticated user making the request.</param>
    /// <returns>The updated QR code response DTO.</returns>
    /// <exception cref="ValidationException">Thrown when validation fails.</exception>
    /// <exception cref="EntityNotFoundException{Guid}">Thrown when QR code is not found.</exception>
    /// <exception cref="EntityUnauthorizedException">Thrown when user is not authorized.</exception>
    Task<QrCodeResponseDto> UpdateQrCodeByUuidAsync(Guid id, UpdateQrCode updateQrCode, ClaimsPrincipal user);

    /// <summary>
    /// Deactivates a QR code by setting its IsActive status to false.
    /// </summary>
    /// <param name="id">The QR code ID to deactivate.</param>
    /// <param name="user">The authenticated user making the request.</param>
    /// <exception cref="EntityNotFoundException{Int32}">Thrown when QR code is not found.</exception>
    /// <exception cref="EntityUnauthorizedException">Thrown when user is not authorized.</exception>
    Task DeactivateQrCodeAsync(Guid id, ClaimsPrincipal user);

    /// <summary>
    /// Deactivates a QR code by its hash value.
    /// </summary>
    /// <param name="qrHash">The QR code hash to deactivate.</param>
    /// <param name="user">The authenticated user making the request.</param>
    /// <exception cref="EntityNotFoundException{String}">Thrown when QR code is not found.</exception>
    /// <exception cref="EntityUnauthorizedException">Thrown when user is not authorized.</exception>
    Task DeactivateQrCodeByHashAsync(string qrHash, ClaimsPrincipal user);

    /// <summary>
    /// Revokes a QR code with audit trail (deactivates with reason and user info).
    /// </summary>
    /// <param name="id">The QR code ID to revoke.</param>
    /// <param name="reason">The reason for revocation.</param>
    /// <param name="user">The authenticated user making the request.</param>
    /// <exception cref="EntityNotFoundException{Int32}">Thrown when QR code is not found.</exception>
    /// <exception cref="EntityUnauthorizedException">Thrown when user is not authorized.</exception>
    Task RevokeQrCodeAsync(Guid id, string? reason, ClaimsPrincipal user);

    /// <summary>
    /// Revokes a QR code by its hash value with audit trail.
    /// </summary>
    /// <param name="qrHash">The QR code hash to revoke.</param>
    /// <param name="reason">The reason for revocation.</param>
    /// <param name="user">The authenticated user making the request.</param>
    /// <exception cref="EntityNotFoundException{String}">Thrown when QR code is not found.</exception>
    /// <exception cref="EntityUnauthorizedException">Thrown when user is not authorized.</exception>
    Task RevokeQrCodeByHashAsync(string qrHash, string? reason, ClaimsPrincipal user);

    /// <summary>
    /// Revokes a QR code by its UUID with audit trail.
    /// </summary>
    /// <param name="id">The QR code UUID to revoke.</param>
    /// <param name="reason">The reason for revocation.</param>
    /// <param name="user">The authenticated user making the request.</param>
    /// <exception cref="EntityNotFoundException{Guid}">Thrown when QR code is not found.</exception>
    /// <exception cref="EntityUnauthorizedException">Thrown when user is not authorized.</exception>
    Task RevokeQrCodeByUuidAsync(Guid id, string? reason, ClaimsPrincipal user);

    /// <summary>
    /// Reactivates a previously revoked QR code.
    /// </summary>
    /// <param name="id">The QR code ID to reactivate.</param>
    /// <param name="user">The authenticated user making the request.</param>
    /// <exception cref="EntityNotFoundException{Int32}">Thrown when QR code is not found.</exception>
    /// <exception cref="EntityUnauthorizedException">Thrown when user is not authorized.</exception>
    /// <exception cref="ValidationException">Thrown when QR code is already active.</exception>
    Task ReactivateQrCodeAsync(Guid id, ClaimsPrincipal user);

    /// <summary>
    /// Reactivates a previously revoked QR code by its hash value.
    /// </summary>
    /// <param name="qrHash">The QR code hash to reactivate.</param>
    /// <param name="user">The authenticated user making the request.</param>
    /// <exception cref="EntityNotFoundException{String}">Thrown when QR code is not found.</exception>
    /// <exception cref="EntityUnauthorizedException">Thrown when user is not authorized.</exception>
    /// <exception cref="ValidationException">Thrown when QR code is already active.</exception>
    Task ReactivateQrCodeByHashAsync(string qrHash, ClaimsPrincipal user);

    /// <summary>
    /// Reactivates a previously revoked QR code by its UUID.
    /// </summary>
    /// <param name="id">The QR code UUID to reactivate.</param>
    /// <param name="user">The authenticated user making the request.</param>
    /// <exception cref="EntityNotFoundException{Guid}">Thrown when QR code is not found.</exception>
    /// <exception cref="EntityUnauthorizedException">Thrown when user is not authorized.</exception>
    /// <exception cref="ValidationException">Thrown when QR code is already active.</exception>
    Task ReactivateQrCodeByUuidAsync(Guid id, ClaimsPrincipal user);

    /// <summary>
    /// Hard deletes a QR code from the database.
    /// </summary>
    /// <param name="id">The QR code ID to delete.</param>
    /// <param name="user">The authenticated user making the request.</param>
    /// <exception cref="EntityNotFoundException{Int32}">Thrown when QR code is not found.</exception>
    /// <exception cref="EntityUnauthorizedException">Thrown when user is not authorized.</exception>
    Task DeleteQrCodeAsync(Guid id, ClaimsPrincipal user);

    /// <summary>
    /// Hard deletes a QR code from the database by its UUID.
    /// </summary>
    /// <param name="id">The QR code UUID to delete.</param>
    /// <param name="user">The authenticated user making the request.</param>
    /// <exception cref="EntityNotFoundException{Guid}">Thrown when QR code is not found.</exception>
    /// <exception cref="EntityUnauthorizedException">Thrown when user is not authorized.</exception>
    Task DeleteQrCodeByUuidAsync(Guid id, ClaimsPrincipal user);

    #endregion

    #region Validation and Usage Operations

    /// <summary>
    /// Validates a QR code for usage without recording attendance.
    /// </summary>
    /// <param name="qrHash">The QR code hash to validate.</param>
    /// <returns>A validation response with status and context information.</returns>
    Task<QrCodeValidationResponseDto> ValidateQrCodeAsync(string qrHash);

    /// <summary>
    /// Scans a QR code and records attendance for a student.
    /// </summary>
    /// <param name="validateQrCode">The QR code validation data including student information.</param>
    /// <param name="user">The authenticated user making the request.</param>
    /// <returns>A scan response with attendance status and context information.</returns>
    Task<QrCodeScanResponseDto> ScanQrCodeAsync(ValidateQrCode validateQrCode, ClaimsPrincipal user);

    /// <summary>
    /// Checks if a QR code hash already exists in the system.
    /// </summary>
    /// <param name="qrHash">The QR code hash to check.</param>
    /// <returns>True if the hash exists; otherwise, false.</returns>
    Task<bool> QrHashExistsAsync(string qrHash);

    #endregion

    #region Utility Operations

    /// <summary>
    /// Generates a unique QR code hash, optionally combining with client-provided hash.
    /// </summary>
    /// <param name="clientHash">Optional client-provided hash for additional uniqueness.</param>
    /// <returns>A unique QR code hash string.</returns>
    Task<string> GenerateUniqueQrHashAsync(string? clientHash = null);

    /// <summary>
    /// Cleans up expired QR codes from the database.
    /// </summary>
    /// <param name="user">The authenticated user making the request.</param>
    /// <returns>The number of QR codes that were deleted.</returns>
    Task<int> CleanupExpiredQrCodesAsync(ClaimsPrincipal user);

    /// <summary>
    /// Gets QR codes that are expiring soon for proactive management.
    /// </summary>
    /// <param name="expiringWithinMinutes">The number of minutes to look ahead for expiring codes.</param>
    /// <returns>A collection of QR codes expiring within the specified timeframe.</returns>
    Task<IEnumerable<QrCodeResponseDto>> GetQrCodesExpiringSoonAsync(int expiringWithinMinutes = 30);

    /// <summary>
    /// Extends the expiration time of a QR code.
    /// </summary>
    /// <param name="id">The QR code ID.</param>
    /// <param name="additionalMinutes">The number of minutes to add to the current expiration time.</param>
    /// <param name="user">The authenticated user making the request.</param>
    /// <returns>The updated QR code response DTO.</returns>
    /// <exception cref="ValidationException">Thrown when validation fails.</exception>
    /// <exception cref="EntityNotFoundException{Int32}">Thrown when QR code is not found.</exception>
    /// <exception cref="EntityUnauthorizedException">Thrown when user is not authorized.</exception>
    Task<QrCodeResponseDto> ExtendQrCodeExpirationAsync(Guid id, int additionalMinutes, ClaimsPrincipal user);

    /// <summary>
    /// Extends the expiration time of a QR code by its UUID.
    /// </summary>
    /// <param name="id">The QR code UUID.</param>
    /// <param name="additionalMinutes">The number of minutes to add to the current expiration time.</param>
    /// <param name="user">The authenticated user making the request.</param>
    /// <returns>The updated QR code response DTO.</returns>
    /// <exception cref="ValidationException">Thrown when validation fails.</exception>
    /// <exception cref="EntityNotFoundException{Guid}">Thrown when QR code is not found.</exception>
    /// <exception cref="EntityUnauthorizedException">Thrown when user is not authorized.</exception>
    Task<QrCodeResponseDto> ExtendQrCodeExpirationByUuidAsync(Guid id, int additionalMinutes, ClaimsPrincipal user);

    #endregion
}
