using attendance_monitoring.Data;
using attendance_monitoring.IServices;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace attendance_monitoring.Services;

/// <summary>
/// Background service that periodically detects, monitors, and cleans up orphaned Identity users.
/// Orphaned users are those with no corresponding Student, Instructor, or Admin profile.
/// 
/// This service provides multiple functions:
/// 1. **Monitoring**: Logs and tracks orphaned users for visibility
/// 2. **Cleanup**: Safely removes orphaned users as a safety net
/// 3. **Reporting**: Provides data integrity reports
/// 
/// The database constraints (CK_Students_SoftDeleteConsistency, etc.) prevent most orphaned
/// users at creation time. This service handles edge cases and provides ongoing monitoring.
/// </summary>
public class OrphanedUserCleanupService : BackgroundService, IOrphanedUserCleanupService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<OrphanedUserCleanupService> _logger;
    private readonly TimeSpan _cleanupInterval;
    private readonly TimeSpan _monitoringInterval;
    private readonly bool _enabled;
    private readonly bool _cleanupEnabled;

    public OrphanedUserCleanupService(
        IServiceScopeFactory scopeFactory,
        ILogger<OrphanedUserCleanupService> logger,
        IConfiguration configuration)
    {
        _scopeFactory = scopeFactory;
        _logger = logger;
        
        // Configure cleanup interval from settings (default: 24 hours)
        var intervalHours = configuration.GetValue<int>("OrphanedUserCleanup:IntervalHours", 24);
        _cleanupInterval = TimeSpan.FromHours(intervalHours);
        
        // Configure monitoring interval (default: 1 hour) - more frequent than cleanup
        var monitoringIntervalMinutes = configuration.GetValue<int>("OrphanedUserCleanup:MonitoringIntervalMinutes", 60);
        _monitoringInterval = TimeSpan.FromMinutes(monitoringIntervalMinutes);
        
        // Allow disabling the background service
        _enabled = configuration.GetValue<bool>("OrphanedUserCleanup:Enabled", true);
        
        // Allow disabling cleanup while keeping monitoring (default: true)
        _cleanupEnabled = configuration.GetValue<bool>("OrphanedUserCleanup:CleanupEnabled", true);
    }

    /// <summary>
    /// Background service execution loop.
    /// Performs monitoring more frequently than cleanup.
    /// </summary>
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        if (!_enabled)
        {
            _logger.LogInformation("OrphanedUserCleanupService is disabled by configuration.");
            return;
        }

        _logger.LogInformation("OrphanedUserCleanupService started. Cleanup interval: {CleanupInterval} hours, Monitoring interval: {MonitoringInterval} minutes, Cleanup enabled: {CleanupEnabled}", 
            _cleanupInterval.TotalHours, _monitoringInterval.TotalMinutes, _cleanupEnabled);

        // Initial delay to allow application startup to complete
        await Task.Delay(TimeSpan.FromMinutes(5), stoppingToken);

        var lastCleanupTime = DateTime.MinValue;

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                // Always perform monitoring
                _logger.LogInformation("Starting orphaned user monitoring cycle...");
                var monitoringResult = await MonitorOrphanedUsersAsync();
                _logger.LogInformation("Monitoring cycle completed. Found {Count} orphaned users.", monitoringResult.OrphanedUserCount);

                // Check for constraint violations using the diagnostic view
                await CheckConstraintViolationsAsync();

                // Perform cleanup if enabled and interval has passed
                if (_cleanupEnabled && DateTime.UtcNow - lastCleanupTime >= _cleanupInterval)
                {
                    _logger.LogInformation("Starting orphaned user cleanup cycle...");
                    var cleanedCount = await CleanupAllOrphanedUsersAsync();
                    _logger.LogInformation("Orphaned user cleanup cycle completed. Cleaned up {Count} users.", cleanedCount);
                    lastCleanupTime = DateTime.UtcNow;
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during orphaned user monitoring/cleanup cycle.");
            }

            await Task.Delay(_monitoringInterval, stoppingToken);
        }
    }

    /// <summary>
    /// Monitors orphaned users and returns a summary without performing cleanup.
    /// </summary>
    public async Task<OrphanedUserMonitoringResult> MonitorOrphanedUsersAsync()
    {
        var orphanedUsers = await DetectOrphanedUsersAsync();
        var orphanedList = orphanedUsers.ToList();

        var result = new OrphanedUserMonitoringResult
        {
            OrphanedUserCount = orphanedList.Count,
            CheckedAt = DateTime.UtcNow,
            OrphanedUsers = orphanedList
        };

        if (orphanedList.Count > 0)
        {
            _logger.LogWarning("Detected {Count} orphaned users during monitoring: {Users}", 
                orphanedList.Count, 
                string.Join(", ", orphanedList.Select(u => $"{u.Email ?? "unknown"} ({u.UserId})")));
        }

        return result;
    }

    /// <summary>
    /// Checks for database constraint violations using the VW_OrphanedUsers view.
    /// </summary>
    private async Task CheckConstraintViolationsAsync()
    {
        try
        {
            using var scope = _scopeFactory.CreateScope();
            var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

            // Try to query the diagnostic view (may not exist if migration hasn't run)
            var orphanedFromView = await context.Database
                .SqlQueryRaw<OrphanedUserViewResult>(
                    "SELECT UserId, UserName, Email, RoleName, OrphanReason FROM VW_OrphanedUsers")
                .ToListAsync();

            if (orphanedFromView.Count > 0)
            {
                _logger.LogWarning("Data integrity check found {Count} users violating profile constraints via VW_OrphanedUsers view.", 
                    orphanedFromView.Count);
                
                foreach (var user in orphanedFromView)
                {
                    _logger.LogWarning("Constraint violation: User {UserId} ({Email}) with role {Role} - {Reason}", 
                        user.UserId, user.Email, user.RoleName, user.OrphanReason);
                }
            }
            else
            {
                _logger.LogDebug("Data integrity check completed. No constraint violations found.");
            }
        }
        catch (Exception ex)
        {
            // View might not exist yet if migration hasn't been applied
            _logger.LogDebug(ex, "Could not query VW_OrphanedUsers view. This is normal if the migration hasn't been applied yet.");
        }
    }

    /// <summary>
    /// Detects and returns all orphaned users in the system.
    /// An orphaned user is an AspNetUser that has no corresponding Student, Instructor, or Admin profile.
    /// </summary>
    public async Task<IEnumerable<(string UserId, string? Email)>> DetectOrphanedUsersAsync()
    {
        using var scope = _scopeFactory.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

        // Find all users that have no profile in any of the profile tables
        var orphanedUsers = await context.Users
            .AsNoTracking()
            .Where(u => 
                !context.Students.Any(s => s.UserId == u.Id) &&
                !context.Instructors.Any(i => i.UserId == u.Id) &&
                !context.Admins.Any(a => a.UserId == u.Id))
            .Select(u => new { u.Id, u.Email })
            .ToListAsync();

        return orphanedUsers.Select(u => (u.Id, u.Email));
    }

    /// <summary>
    /// Cleans up a specific orphaned user by deleting their Identity account.
    /// </summary>
    public async Task<bool> CleanupOrphanedUserAsync(string userId)
    {
        using var scope = _scopeFactory.CreateScope();
        var userManager = scope.ServiceProvider.GetRequiredService<UserManager<IdentityUser>>();
        var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

        try
        {
            // Verify the user is actually orphaned before deleting
            var hasProfile = await context.Students.AnyAsync(s => s.UserId == userId) ||
                            await context.Instructors.AnyAsync(i => i.UserId == userId) ||
                            await context.Admins.AnyAsync(a => a.UserId == userId);

            if (hasProfile)
            {
                _logger.LogWarning("User {UserId} is not orphaned (has a profile). Skipping cleanup.", userId);
                return false;
            }

            var user = await userManager.FindByIdAsync(userId);
            if (user == null)
            {
                _logger.LogWarning("User {UserId} not found during cleanup.", userId);
                return false;
            }

            // Delete any refresh tokens for this user
            var refreshTokens = await context.RefreshTokens
                .Where(rt => rt.UserId == userId)
                .ToListAsync();
            
            if (refreshTokens.Count != 0)
            {
                context.RefreshTokens.RemoveRange(refreshTokens);
                await context.SaveChangesAsync();
                _logger.LogInformation("Removed {Count} refresh tokens for orphaned user {UserId}.", 
                    refreshTokens.Count, userId);
            }

            // Delete the user
            var result = await userManager.DeleteAsync(user);
            if (result.Succeeded)
            {
                _logger.LogInformation("Successfully cleaned up orphaned user {UserId} ({Email}).", 
                    userId, user.Email);
                return true;
            }
            else
            {
                _logger.LogError("Failed to delete orphaned user {UserId}: {Errors}", 
                    userId, string.Join(", ", result.Errors.Select(e => e.Description)));
                return false;
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error cleaning up orphaned user {UserId}.", userId);
            return false;
        }
    }

    /// <summary>
    /// Cleans up all detected orphaned users.
    /// </summary>
    public async Task<int> CleanupAllOrphanedUsersAsync()
    {
        var orphanedUsers = await DetectOrphanedUsersAsync();
        var orphanedList = orphanedUsers.ToList();

        if (orphanedList.Count == 0)
        {
            _logger.LogInformation("No orphaned users detected.");
            return 0;
        }

        _logger.LogInformation("Detected {Count} orphaned users. Starting cleanup...", orphanedList.Count);

        var cleanedCount = 0;
        foreach (var (userId, email) in orphanedList)
        {
            if (await CleanupOrphanedUserAsync(userId))
            {
                cleanedCount++;
            }
        }

        return cleanedCount;
    }

    /// <summary>
    /// Gets the current data integrity status.
    /// </summary>
    public async Task<DataIntegrityStatus> GetDataIntegrityStatusAsync()
    {
        var orphanedUsers = await DetectOrphanedUsersAsync();
        var orphanedList = orphanedUsers.ToList();

        using var scope = _scopeFactory.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

        // Check for soft delete inconsistencies
        var studentsWithInconsistentSoftDelete = await context.Students
            .AsNoTracking()
            .Where(s => (s.IsDeleted && s.DeletedAt == null) || (!s.IsDeleted && s.DeletedAt != null))
            .CountAsync();

        var instructorsWithInconsistentSoftDelete = await context.Instructors
            .AsNoTracking()
            .Where(i => (i.IsDeleted && i.DeletedAt == null) || (!i.IsDeleted && i.DeletedAt != null))
            .CountAsync();

        var adminsWithInconsistentSoftDelete = await context.Admins
            .AsNoTracking()
            .Where(a => (a.IsDeleted && a.DeletedAt == null) || (!a.IsDeleted && a.DeletedAt != null))
            .CountAsync();

        return new DataIntegrityStatus
        {
            IsHealthy = orphanedList.Count == 0 && 
                        studentsWithInconsistentSoftDelete == 0 && 
                        instructorsWithInconsistentSoftDelete == 0 && 
                        adminsWithInconsistentSoftDelete == 0,
            OrphanedUserCount = orphanedList.Count,
            StudentsWithInconsistentSoftDelete = studentsWithInconsistentSoftDelete,
            InstructorsWithInconsistentSoftDelete = instructorsWithInconsistentSoftDelete,
            AdminsWithInconsistentSoftDelete = adminsWithInconsistentSoftDelete,
            CheckedAt = DateTime.UtcNow
        };
    }
}

/// <summary>
/// Result of orphaned user monitoring.
/// </summary>
public class OrphanedUserMonitoringResult
{
    public int OrphanedUserCount { get; set; }
    public DateTime CheckedAt { get; set; }
    public List<(string UserId, string? Email)> OrphanedUsers { get; set; } = [];
}

/// <summary>
/// Represents the overall data integrity status.
/// </summary>
public class DataIntegrityStatus
{
    public bool IsHealthy { get; set; }
    public int OrphanedUserCount { get; set; }
    public int StudentsWithInconsistentSoftDelete { get; set; }
    public int InstructorsWithInconsistentSoftDelete { get; set; }
    public int AdminsWithInconsistentSoftDelete { get; set; }
    public DateTime CheckedAt { get; set; }
}

/// <summary>
/// Result type for the VW_OrphanedUsers view query.
/// </summary>
internal class OrphanedUserViewResult
{
    public string UserId { get; set; } = string.Empty;
    public string? UserName { get; set; }
    public string? Email { get; set; }
    public string? RoleName { get; set; }
    public string? OrphanReason { get; set; }
}
