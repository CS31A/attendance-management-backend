using attendance_monitoring.Classes;
using attendance_monitoring.Data;
using attendance_monitoring.IRepository;
using Microsoft.EntityFrameworkCore;

namespace attendance_monitoring.Repositories;

/// <summary>
/// Repository implementation for managing fingerprint biometric data.
/// </summary>
public class FingerprintRepository(ApplicationDbContext context, ILogger<FingerprintRepository> logger) : IFingerprintRepository
{
    #region Read Operations

    public async Task<Fingerprint?> GetFingerprintByIdAsync(int id)
    {
        try
        {
            return await context.Fingerprints
                .AsNoTracking()
                .Include(f => f.User)
                .FirstOrDefaultAsync(f => f.Id == id)
                .ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "An error occurred while retrieving fingerprint with ID {FingerprintId} from database.", id);
            throw;
        }
    }

    public async Task<Fingerprint?> GetFingerprintByUserIdAsync(string userId)
    {
        try
        {
            return await context.Fingerprints
                .AsNoTracking()
                .Include(f => f.User)
                .Where(f => !f.IsDeleted)
                .FirstOrDefaultAsync(f => f.UserId == userId)
                .ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "An error occurred while retrieving fingerprint for user {UserId} from database.", userId);
            throw;
        }
    }

    public async Task<Fingerprint?> GetFingerprintByStudentIdAsync(int studentId)
    {
        try
        {
            return await context.Fingerprints
                .AsNoTracking()
                .Include(f => f.User)
                .Where(f => !f.IsDeleted)
                .Join(context.Students, f => f.UserId, s => s.UserId, (f, s) => new { Fingerprint = f, Student = s })
                .Where(x => x.Student.Id == studentId)
                .Select(x => x.Fingerprint)
                .FirstOrDefaultAsync()
                .ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "An error occurred while retrieving fingerprint for student {StudentId} from database.", studentId);
            throw;
        }
    }

    public async Task<Fingerprint?> GetFingerprintByStudentIdIncludingDeletedAsync(int studentId)
    {
        try
        {
            return await context.Fingerprints
                .AsNoTracking()
                .Include(f => f.User)
                .Join(context.Students, f => f.UserId, s => s.UserId, (f, s) => new { Fingerprint = f, Student = s })
                .Where(x => x.Student.Id == studentId)
                .Select(x => x.Fingerprint)
                .FirstOrDefaultAsync()
                .ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "An error occurred while retrieving fingerprint, including deleted rows, for student {StudentId} from database.", studentId);
            throw;
        }
    }

    public async Task<IEnumerable<Fingerprint>> GetFingerprintsByDeviceIdAsync(string deviceId)
    {
        try
        {
            return await context.Fingerprints
                .AsNoTracking()
                .Where(f => f.DeviceId == deviceId && !f.IsDeleted)
                .ToListAsync()
                .ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "An error occurred while retrieving fingerprints for device {DeviceId} from database.", deviceId);
            throw;
        }
    }

    public async Task<IEnumerable<Fingerprint>> GetActiveFingerprintsAsync()
    {
        try
        {
            return await context.Fingerprints
                .AsNoTracking()
                .Include(f => f.User)
                .Where(f => !f.IsDeleted)
                .ToListAsync()
                .ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "An error occurred while retrieving active fingerprints from database.");
            throw;
        }
    }

    public async Task<Fingerprint?> FindFingerprintByTemplateAsync(string templateData)
    {
        try
        {
            // In production, this should use a proper biometric matching algorithm
            // For now, we use exact match as a placeholder
            return await context.Fingerprints
                .AsNoTracking()
                .Include(f => f.User)
                .Where(f => !f.IsDeleted && f.TemplateData == templateData)
                .FirstOrDefaultAsync()
                .ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "An error occurred while finding fingerprint by template data.");
            throw;
        }
    }

    public async Task<Fingerprint?> FindFingerprintByDeviceAndSensorIdAsync(string deviceId, int sensorFingerprintId)
    {
        try
        {
            return await context.Fingerprints
                .AsNoTracking()
                .Include(f => f.User)
                .Where(f => !f.IsDeleted && f.DeviceId == deviceId && f.SensorFingerprintId == sensorFingerprintId)
                .FirstOrDefaultAsync()
                .ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "An error occurred while finding fingerprint by device {DeviceId} and sensor ID {SensorId}.", deviceId, sensorFingerprintId);
            throw;
        }
    }

    public async Task<bool> UserHasFingerprintAsync(string userId)
    {
        try
        {
            return await context.Fingerprints
                .Where(f => f.UserId == userId && !f.IsDeleted)
                .AnyAsync()
                .ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "An error occurred while checking if user {UserId} has a fingerprint.", userId);
            throw;
        }
    }

    public async Task<bool> StudentHasFingerprintAsync(int studentId)
    {
        try
        {
            return await context.Fingerprints
                .Where(f => !f.IsDeleted)
                .Join(context.Students, f => f.UserId, s => s.UserId, (f, s) => new { Fingerprint = f, Student = s })
                .Where(x => x.Student.Id == studentId)
                .AnyAsync()
                .ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "An error occurred while checking if student {StudentId} has a fingerprint.", studentId);
            throw;
        }
    }

    public async Task<int> GetFingerprintCountForDeviceAsync(string deviceId)
    {
        try
        {
            return await context.Fingerprints
                .Where(f => f.DeviceId == deviceId && !f.IsDeleted)
                .CountAsync()
                .ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "An error occurred while counting fingerprints for device {DeviceId}.", deviceId);
            throw;
        }
    }

    #endregion

    #region Write Operations

    public async Task<Fingerprint> CreateFingerprintAsync(Fingerprint fingerprint)
    {
        try
        {
            fingerprint.CreatedAt = DateTime.UtcNow;
            fingerprint.UpdatedAt = DateTime.UtcNow;
            fingerprint.IsDeleted = false;

            context.Fingerprints.Add(fingerprint);
            await context.SaveChangesAsync().ConfigureAwait(false);

            logger.LogInformation("Created fingerprint with ID {FingerprintId} for user {UserId}.", fingerprint.Id, fingerprint.UserId);
            return fingerprint;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "An error occurred while creating fingerprint for user {UserId}.", fingerprint.UserId);
            throw;
        }
    }

    public async Task<Fingerprint> UpdateFingerprintAsync(Fingerprint fingerprint)
    {
        try
        {
            fingerprint.UpdatedAt = DateTime.UtcNow;

            context.Fingerprints.Update(fingerprint);
            await context.SaveChangesAsync().ConfigureAwait(false);

            logger.LogInformation("Updated fingerprint with ID {FingerprintId}.", fingerprint.Id);
            return fingerprint;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "An error occurred while updating fingerprint with ID {FingerprintId}.", fingerprint.Id);
            throw;
        }
    }

    public async Task<bool> SoftDeleteFingerprintAsync(int id)
    {
        try
        {
            var fingerprint = await context.Fingerprints.FindAsync(id).ConfigureAwait(false);
            if (fingerprint == null)
            {
                return false;
            }

            fingerprint.IsDeleted = true;
            fingerprint.DeletedAt = DateTime.UtcNow;
            fingerprint.UpdatedAt = DateTime.UtcNow;

            await context.SaveChangesAsync().ConfigureAwait(false);

            logger.LogInformation("Soft deleted fingerprint with ID {FingerprintId}.", id);
            return true;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "An error occurred while soft deleting fingerprint with ID {FingerprintId}.", id);
            throw;
        }
    }

    public async Task<bool> SoftDeleteFingerprintByUserIdAsync(string userId)
    {
        try
        {
            var fingerprint = await context.Fingerprints
                .Where(f => f.UserId == userId && !f.IsDeleted)
                .FirstOrDefaultAsync()
                .ConfigureAwait(false);

            if (fingerprint == null)
            {
                return false;
            }

            fingerprint.IsDeleted = true;
            fingerprint.DeletedAt = DateTime.UtcNow;
            fingerprint.UpdatedAt = DateTime.UtcNow;

            await context.SaveChangesAsync().ConfigureAwait(false);

            logger.LogInformation("Soft deleted fingerprint for user {UserId}.", userId);
            return true;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "An error occurred while soft deleting fingerprint for user {UserId}.", userId);
            throw;
        }
    }

    public async Task<bool> RestoreFingerprintAsync(int id)
    {
        try
        {
            var fingerprint = await context.Fingerprints.FindAsync(id).ConfigureAwait(false);
            if (fingerprint == null)
            {
                return false;
            }

            fingerprint.IsDeleted = false;
            fingerprint.DeletedAt = null;
            fingerprint.UpdatedAt = DateTime.UtcNow;

            await context.SaveChangesAsync().ConfigureAwait(false);

            logger.LogInformation("Restored fingerprint with ID {FingerprintId}.", id);
            return true;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "An error occurred while restoring fingerprint with ID {FingerprintId}.", id);
            throw;
        }
    }

    public async Task<bool> HardDeleteFingerprintAsync(int id)
    {
        try
        {
            var fingerprint = await context.Fingerprints.FindAsync(id).ConfigureAwait(false);
            if (fingerprint == null)
            {
                return false;
            }

            context.Fingerprints.Remove(fingerprint);
            await context.SaveChangesAsync().ConfigureAwait(false);

            logger.LogInformation("Hard deleted fingerprint with ID {FingerprintId}.", id);
            return true;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "An error occurred while hard deleting fingerprint with ID {FingerprintId}.", id);
            throw;
        }
    }

    #endregion

    #region Transaction Support

    public async Task<Microsoft.EntityFrameworkCore.Storage.IDbContextTransaction> BeginTransactionAsync()
    {
        return await context.Database.BeginTransactionAsync().ConfigureAwait(false);
    }

    #endregion

    #region ISaveableRepository Implementation

    public async Task<int> SaveChangesAsync()
    {
        try
        {
            var result = await context.SaveChangesAsync().ConfigureAwait(false);
            return result;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "An error occurred while saving changes to the database.");
            throw;
        }
    }

    #endregion
}
