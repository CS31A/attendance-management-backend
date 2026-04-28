using attendance_monitoring.Services;

namespace attendance_monitoring.IServices;

/// <summary>
/// Interface for the orphaned user cleanup and monitoring service.
/// Orphaned users are Identity users that have been created but lack corresponding profile entries
/// (Student, Instructor, or Admin) due to failed transactions or other issues.
/// 
/// This service provides:
/// 1. Detection of orphaned users
/// 2. Cleanup of orphaned users
/// 3. Monitoring and reporting of data integrity issues
/// </summary>
public interface IOrphanedUserCleanupService
{
    /// <summary>
    /// Detects and returns all orphaned users in the system.
    /// An orphaned user is an AspNetUser that has no corresponding Student, Instructor, or Admin profile.
    /// </summary>
    /// <returns>A collection of orphaned user IDs with their email addresses.</returns>
    Task<IEnumerable<(string UserId, string? Email)>> DetectOrphanedUsersAsync();

    /// <summary>
    /// Cleans up a specific orphaned user by deleting their Identity account.
    /// </summary>
    /// <param name="userId">The ID of the orphaned user to clean up.</param>
    /// <returns>True if the cleanup was successful, false otherwise.</returns>
    Task<bool> CleanupOrphanedUserAsync(string userId);

    /// <summary>
    /// Cleans up all detected orphaned users.
    /// </summary>
    /// <returns>The number of orphaned users that were cleaned up.</returns>
    Task<int> CleanupAllOrphanedUsersAsync();

    /// <summary>
    /// Monitors orphaned users and returns a summary without performing cleanup.
    /// </summary>
    /// <returns>A monitoring result containing orphaned user information.</returns>
    Task<OrphanedUserMonitoringResult> MonitorOrphanedUsersAsync();

    /// <summary>
    /// Gets the current data integrity status including orphaned users and soft delete consistency.
    /// </summary>
    /// <returns>The current data integrity status.</returns>
    Task<DataIntegrityStatus> GetDataIntegrityStatusAsync();
}
