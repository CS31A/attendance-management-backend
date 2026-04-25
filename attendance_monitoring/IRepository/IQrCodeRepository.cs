using attendance_monitoring.Classes;

namespace attendance_monitoring.IRepository;

/// <summary>
/// Represents the repository for managing QR codes.
/// </summary>
public interface IQrCodeRepository : ISaveableRepository
{
    /// <summary>
    /// Retrieves a QR code by its ID.
    /// </summary>
    /// <param name="id">The QR code ID.</param>
    /// <returns>The QR code if found; otherwise, null.</returns>
    Task<QrCode?> GetQrCodeByIdAsync(int id);

    /// <summary>
    /// Retrieves a QR code by its hash value.
    /// </summary>
    /// <param name="qrHash">The QR code hash.</param>
    /// <returns>The QR code if found; otherwise, null.</returns>
    Task<QrCode?> GetQrCodeByHashAsync(string qrHash);

    /// <summary>
    /// Retrieves all QR codes for a specific schedule.
    /// </summary>
    /// <param name="scheduleId">The schedule ID.</param>
    /// <returns>A collection of QR codes for the specified schedule.</returns>
    Task<IEnumerable<QrCode>> GetQrCodesByScheduleIdAsync(int scheduleId);

    /// <summary>
    /// Retrieves all QR codes for a specific section.
    /// </summary>
    /// <param name="sectionId">The section ID.</param>
    /// <returns>A collection of QR codes for the specified section.</returns>
    Task<IEnumerable<QrCode>> GetQrCodesBySectionIdAsync(int sectionId);

    /// <summary>
    /// Retrieves all QR codes for a specific session.
    /// </summary>
    /// <param name="sessionId">The session ID.</param>
    /// <returns>A collection of QR codes for the specified session.</returns>
    Task<IEnumerable<QrCode>> GetQrCodesBySessionIdAsync(int sessionId);

    /// <summary>
    /// Retrieves all active QR codes that have not expired.
    /// </summary>
    /// <returns>A collection of active, non-expired QR codes.</returns>
    Task<IEnumerable<QrCode>> GetActiveQrCodesAsync();

    /// <summary>
    /// Retrieves all active QR codes for a specific schedule that have not expired.
    /// </summary>
    /// <param name="scheduleId">The schedule ID.</param>
    /// <returns>A collection of active, non-expired QR codes for the specified schedule.</returns>
    Task<IEnumerable<QrCode>> GetActiveQrCodesByScheduleIdAsync(int scheduleId);

    /// <summary>
    /// Retrieves all expired QR codes.
    /// </summary>
    /// <returns>A collection of expired QR codes.</returns>
    Task<IEnumerable<QrCode>> GetExpiredQrCodesAsync();

    /// <summary>
    /// Retrieves QR codes that are close to expiring within the specified time span.
    /// </summary>
    /// <param name="expiringWithin">The time span to check for expiration.</param>
    /// <returns>A collection of QR codes expiring within the specified time.</returns>
    Task<IEnumerable<QrCode>> GetQrCodesExpiringWithinAsync(TimeSpan expiringWithin);

    /// <summary>
    /// Creates a new QR code.
    /// </summary>
    /// <param name="qrCode">The QR code to create.</param>
    /// <returns>The created QR code.</returns>
    Task<QrCode> CreateQrCodeAsync(QrCode qrCode);

    /// <summary>
    /// Updates an existing QR code.
    /// </summary>
    /// <param name="qrCode">The QR code to update.</param>
    /// <returns>The updated QR code.</returns>
    Task<QrCode> UpdateQrCodeAsync(QrCode qrCode);

    /// <summary>
    /// Deactivates a QR code by setting its IsActive status to false.
    /// </summary>
    /// <param name="id">The QR code ID.</param>
    /// <returns>True if the QR code was deactivated; otherwise, false.</returns>
    Task<bool> DeactivateQrCodeAsync(int id);

    /// <summary>
    /// Deactivates a QR code by its hash value.
    /// </summary>
    /// <param name="qrHash">The QR code hash.</param>
    /// <returns>True if the QR code was deactivated; otherwise, false.</returns>
    Task<bool> DeactivateQrCodeByHashAsync(string qrHash);

    /// <summary>
    /// Reactivates a QR code by setting its IsActive status to true and clearing revocation info.
    /// </summary>
    /// <param name="id">The QR code ID.</param>
    /// <returns>True if the QR code was reactivated; otherwise, false.</returns>
    Task<bool> ReactivateQrCodeAsync(int id);

    /// <summary>
    /// Reactivates a QR code by its hash value.
    /// </summary>
    /// <param name="qrHash">The QR code hash.</param>
    /// <returns>True if the QR code was reactivated; otherwise, false.</returns>
    Task<bool> ReactivateQrCodeByHashAsync(string qrHash);

    /// <summary>
    /// Increments the usage count for a QR code.
    /// </summary>
    /// <param name="id">The QR code ID.</param>
    /// <returns>The updated QR code if found; otherwise, null.</returns>
    Task<QrCode?> IncrementUsageCountAsync(int id);

    /// <summary>
    /// Increments the usage count for a QR code by its hash.
    /// </summary>
    /// <param name="qrHash">The QR code hash.</param>
    /// <returns>The updated QR code if found; otherwise, null.</returns>
    Task<QrCode?> IncrementUsageCountByHashAsync(string qrHash);

    /// <summary>
    /// Deletes a QR code by its ID.
    /// </summary>
    /// <param name="id">The QR code ID.</param>
    /// <returns>True if the QR code was deleted; otherwise, false.</returns>
    Task<bool> DeleteQrCodeAsync(int id);

    /// <summary>
    /// Deletes all expired QR codes from the database.
    /// </summary>
    /// <returns>The number of QR codes deleted.</returns>
    Task<int> DeleteExpiredQrCodesAsync();

    /// <summary>
    /// Checks if a QR code hash already exists in the database.
    /// </summary>
    /// <param name="qrHash">The QR code hash to check.</param>
    /// <returns>True if the hash exists; otherwise, false.</returns>
    Task<bool> QrHashExistsAsync(string qrHash);

    /// <summary>
    /// Validates if a QR code is usable (active, not expired, within usage limits).
    /// </summary>
    /// <param name="qrHash">The QR code hash to validate.</param>
    /// <returns>The QR code if valid and usable; otherwise, null.</returns>
    Task<QrCode?> ValidateQrCodeForUsageAsync(string qrHash);

    /// <summary>
    /// Atomically increments the usage count for a QR code with validation in a single database operation.
    /// Prevents race conditions by performing validation and increment atomically.
    /// </summary>
    /// <param name="qrHash">The QR code hash.</param>
    /// <param name="currentTime">The current UTC time for expiration checking.</param>
    /// <returns>The number of rows affected (1 if successful, 0 if validation failed).</returns>
    Task<int> AtomicIncrementUsageAsync(string qrHash, DateTime currentTime);

    /// <summary>
    /// Begins a database transaction for atomic operations.
    /// </summary>
    /// <returns>A database transaction.</returns>
    Task<Microsoft.EntityFrameworkCore.Storage.IDbContextTransaction> BeginTransactionAsync();

    /// <summary>
    /// Get paginated scan history for a QR code by ID
    /// </summary>
    /// <param name="qrCodeId">The QR code ID</param>
    /// <param name="pageNumber">Page number (1-based)</param>
    /// <param name="pageSize">Number of items per page</param>
    /// <returns>Paginated list of attendance records for the QR code</returns>
    Task<attendance_monitoring.Models.DTO.Response.PagedResult<attendance_monitoring.Classes.AttendanceRecord>> GetScanHistoryAsync(
        int qrCodeId,
        int pageNumber,
        int pageSize);

    /// <summary>
    /// Get paginated scan history for a QR code by hash
    /// </summary>
    /// <param name="qrHash">The QR code hash</param>
    /// <param name="pageNumber">Page number (1-based)</param>
    /// <param name="pageSize">Number of items per page</param>
    /// <returns>Paginated list of attendance records for the QR code</returns>
    Task<attendance_monitoring.Models.DTO.Response.PagedResult<attendance_monitoring.Classes.AttendanceRecord>> GetScanHistoryByHashAsync(
        string qrHash,
        int pageNumber,
        int pageSize);

    /// <summary>
    /// Get statistical information about QR code scans
    /// </summary>
    /// <param name="qrCodeId">The QR code ID</param>
    /// <returns>Tuple containing total scans, unique students, status breakdown, and scan time range</returns>
    Task<(int totalScans, int uniqueStudents, Dictionary<string, int> statusBreakdown, DateTime? firstScan, DateTime? lastScan)> GetScanStatisticsAsync(
        int qrCodeId);

    /// <summary>
    /// Retrieves a QR code by its UUID with all navigation properties loaded (read-only).
    /// </summary>
    /// <param name="uuid">The QR code UUID</param>
    /// <returns>The QR code if found, null otherwise</returns>
    Task<QrCode?> GetQrCodeByUuidAsync(Guid uuid);

    /// <summary>
    /// Retrieves a QR code by its UUID with all navigation properties loaded (tracked for updates).
    /// </summary>
    /// <param name="uuid">The QR code UUID</param>
    /// <returns>The QR code if found, null otherwise</returns>
    Task<QrCode?> GetQrCodeByUuidTrackedAsync(Guid uuid);
}