using attendance_monitoring.Classes;

namespace attendance_monitoring.IRepository;

/// <summary>
/// Repository interface for managing fingerprint biometric data.
/// </summary>
public interface IFingerprintRepository : ISaveableRepository
{
    #region Read Operations

    /// <summary>
    /// Retrieves a fingerprint by its ID.
    /// </summary>
    /// <param name="id">The fingerprint ID.</param>
    /// <returns>The fingerprint if found; otherwise, null.</returns>
    Task<Fingerprint?> GetFingerprintByIdAsync(Guid id);

    /// <summary>
    /// Retrieves a fingerprint by its UUID.
    /// </summary>
    /// <param name="id">The fingerprint UUID.</param>
    /// <returns>The fingerprint if found; otherwise, null.</returns>
    Task<Fingerprint?> GetFingerprintByUuidAsync(Guid id);

    /// <summary>
    /// Retrieves a fingerprint by the associated user ID.
    /// </summary>
    /// <param name="userId">The user ID.</param>
    /// <returns>The fingerprint if found; otherwise, null.</returns>
    Task<Fingerprint?> GetFingerprintByUserIdAsync(string userId);

    /// <summary>
    /// Retrieves a fingerprint by the associated student ID.
    /// </summary>
    /// <param name="studentId">The student ID.</param>
    /// <returns>The fingerprint if found; otherwise, null.</returns>
    Task<Fingerprint?> GetFingerprintByStudentIdAsync(Guid studentId);

    /// <summary>
    /// Retrieves a fingerprint by the associated student ID, including soft-deleted rows.
    /// </summary>
    /// <param name="studentId">The student ID.</param>
    /// <returns>The fingerprint if found; otherwise, null.</returns>
    Task<Fingerprint?> GetFingerprintByStudentIdIncludingDeletedAsync(Guid studentId);

    /// <summary>
    /// Retrieves all fingerprints for a specific device.
    /// </summary>
    /// <param name="deviceId">The device ID.</param>
    /// <returns>A collection of fingerprints for the specified device.</returns>
    Task<IEnumerable<Fingerprint>> GetFingerprintsByDeviceIdAsync(string deviceId);

    /// <summary>
    /// Retrieves all active (non-deleted) fingerprints.
    /// </summary>
    /// <returns>A collection of active fingerprints.</returns>
    Task<IEnumerable<Fingerprint>> GetActiveFingerprintsAsync();

    /// <summary>
    /// Searches for a fingerprint by template data using exact match.
    /// Note: In production, this should use a biometric matching algorithm.
    /// </summary>
    /// <param name="templateData">The template data to match.</param>
    /// <returns>The matching fingerprint if found; otherwise, null.</returns>
    Task<Fingerprint?> FindFingerprintByTemplateAsync(string templateData);

    /// <summary>
    /// Searches for fingerprints by device ID and sensor fingerprint ID.
    /// </summary>
    /// <param name="deviceId">The device ID.</param>
    /// <param name="sensorFingerprintId">The sensor fingerprint ID.</param>
    /// <returns>The matching fingerprint if found; otherwise, null.</returns>
    Task<Fingerprint?> FindFingerprintByDeviceAndSensorIdAsync(string deviceId, int sensorFingerprintId);

    /// <summary>
    /// Checks if a user has a registered fingerprint.
    /// </summary>
    /// <param name="userId">The user ID.</param>
    /// <returns>True if the user has a registered fingerprint; otherwise, false.</returns>
    Task<bool> UserHasFingerprintAsync(string userId);

    /// <summary>
    /// Checks if a student has a registered fingerprint.
    /// </summary>
    /// <param name="studentId">The student ID.</param>
    /// <returns>True if the student has a registered fingerprint; otherwise, false.</returns>
    Task<bool> StudentHasFingerprintAsync(Guid studentId);

    /// <summary>
    /// Gets the count of registered fingerprints for a device.
    /// </summary>
    /// <param name="deviceId">The device ID.</param>
    /// <returns>The number of fingerprints registered for the device.</returns>
    Task<int> GetFingerprintCountForDeviceAsync(string deviceId);

    /// <summary>
    /// Retrieves all active fingerprint devices ordered by name.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A list of active fingerprint devices.</returns>
    Task<List<FingerprintDevice>> GetDevicesAsync(CancellationToken cancellationToken = default);

    #endregion

    #region Write Operations

    /// <summary>
    /// Creates a new fingerprint registration.
    /// </summary>
    /// <param name="fingerprint">The fingerprint to create.</param>
    /// <returns>The created fingerprint.</returns>
    Task<Fingerprint> CreateFingerprintAsync(Fingerprint fingerprint);

    /// <summary>
    /// Updates an existing fingerprint.
    /// </summary>
    /// <param name="fingerprint">The fingerprint to update.</param>
    /// <returns>The updated fingerprint.</returns>
    Task<Fingerprint> UpdateFingerprintAsync(Fingerprint fingerprint);

    /// <summary>
    /// Soft deletes a fingerprint by setting IsDeleted to true.
    /// </summary>
    /// <param name="id">The fingerprint ID.</param>
    /// <returns>True if the fingerprint was deleted; otherwise, false.</returns>
    Task<bool> SoftDeleteFingerprintAsync(Guid id);

    /// <summary>
    /// Soft deletes a fingerprint by UUID.
    /// </summary>
    /// <param name="id">The fingerprint UUID.</param>
    /// <returns>True if the fingerprint was deleted; otherwise, false.</returns>
    Task<bool> SoftDeleteFingerprintByUuidAsync(Guid id);

    /// <summary>
    /// Soft deletes a fingerprint by user ID.
    /// </summary>
    /// <param name="userId">The user ID.</param>
    /// <returns>True if the fingerprint was deleted; otherwise, false.</returns>
    Task<bool> SoftDeleteFingerprintByUserIdAsync(string userId);

    /// <summary>
    /// Restores a soft deleted fingerprint.
    /// </summary>
    /// <param name="id">The fingerprint ID.</param>
    /// <returns>True if the fingerprint was restored; otherwise, false.</returns>
    Task<bool> RestoreFingerprintAsync(Guid id);

    /// <summary>
    /// Hard deletes a fingerprint from the database.
    /// </summary>
    /// <param name="id">The fingerprint ID.</param>
    /// <returns>True if the fingerprint was deleted; otherwise, false.</returns>
    Task<bool> HardDeleteFingerprintAsync(Guid id);

    #endregion

    #region Transaction Support

    /// <summary>
    /// Begins a database transaction for atomic operations.
    /// </summary>
    /// <returns>A database transaction.</returns>
    Task<Microsoft.EntityFrameworkCore.Storage.IDbContextTransaction> BeginTransactionAsync();

    #endregion
}
