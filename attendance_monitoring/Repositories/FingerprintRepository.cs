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
        => await context.Fingerprints
            .AsNoTracking()
            .Include(f => f.User)
            .FirstOrDefaultAsync(f => f.Id == id)
            .ConfigureAwait(false);

    public async Task<Fingerprint?> GetFingerprintByUserIdAsync(string userId)
        => await context.Fingerprints
            .AsNoTracking()
            .Include(f => f.User)
            .Where(f => !f.IsDeleted)
            .FirstOrDefaultAsync(f => f.UserId == userId)
            .ConfigureAwait(false);

    public async Task<Fingerprint?> GetFingerprintByStudentIdAsync(int studentId)
        => await context.Fingerprints
            .AsNoTracking()
            .Include(f => f.User)
            .Where(f => !f.IsDeleted)
            .Join(context.Students, f => f.UserId, s => s.UserId, (f, s) => new { Fingerprint = f, Student = s })
            .Where(x => x.Student.Id == studentId)
            .Select(x => x.Fingerprint)
            .FirstOrDefaultAsync()
            .ConfigureAwait(false);

    public async Task<Fingerprint?> GetFingerprintByStudentIdIncludingDeletedAsync(int studentId)
        => await context.Fingerprints
            .AsNoTracking()
            .Include(f => f.User)
            .Join(context.Students, f => f.UserId, s => s.UserId, (f, s) => new { Fingerprint = f, Student = s })
            .Where(x => x.Student.Id == studentId)
            .Select(x => x.Fingerprint)
            .FirstOrDefaultAsync()
            .ConfigureAwait(false);

    public async Task<IEnumerable<Fingerprint>> GetFingerprintsByDeviceIdAsync(string deviceId)
        => await context.Fingerprints
            .AsNoTracking()
            .Where(f => f.DeviceId == deviceId && !f.IsDeleted)
            .ToListAsync()
            .ConfigureAwait(false);

    public async Task<IEnumerable<Fingerprint>> GetActiveFingerprintsAsync()
        => await context.Fingerprints
            .AsNoTracking()
            .Include(f => f.User)
            .Where(f => !f.IsDeleted)
            .ToListAsync()
            .ConfigureAwait(false);

    public async Task<Fingerprint?> FindFingerprintByTemplateAsync(string templateData)
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

    public async Task<Fingerprint?> FindFingerprintByDeviceAndSensorIdAsync(string deviceId, int sensorFingerprintId)
        => await context.Fingerprints
            .AsNoTracking()
            .Include(f => f.User)
            .Where(f => !f.IsDeleted && f.DeviceId == deviceId && f.SensorFingerprintId == sensorFingerprintId)
            .FirstOrDefaultAsync()
            .ConfigureAwait(false);

    public async Task<bool> UserHasFingerprintAsync(string userId)
        => await context.Fingerprints
            .Where(f => f.UserId == userId && !f.IsDeleted)
            .AnyAsync()
            .ConfigureAwait(false);

    public async Task<bool> StudentHasFingerprintAsync(int studentId)
        => await context.Fingerprints
            .Where(f => !f.IsDeleted)
            .Join(context.Students, f => f.UserId, s => s.UserId, (f, s) => new { Fingerprint = f, Student = s })
            .Where(x => x.Student.Id == studentId)
            .AnyAsync()
            .ConfigureAwait(false);

    public async Task<int> GetFingerprintCountForDeviceAsync(string deviceId)
        => await context.Fingerprints
            .Where(f => f.DeviceId == deviceId && !f.IsDeleted)
            .CountAsync()
            .ConfigureAwait(false);

    #endregion

    #region Write Operations

    public async Task<Fingerprint> CreateFingerprintAsync(Fingerprint fingerprint)
    {
        fingerprint.CreatedAt = DateTime.UtcNow;
        fingerprint.UpdatedAt = DateTime.UtcNow;
        fingerprint.IsDeleted = false;

        context.Fingerprints.Add(fingerprint);
        await context.SaveChangesAsync().ConfigureAwait(false);

        logger.LogInformation("Created fingerprint with ID {FingerprintId} for user {UserId}.", fingerprint.Id, fingerprint.UserId);
        return fingerprint;
    }

    public async Task<Fingerprint> UpdateFingerprintAsync(Fingerprint fingerprint)
    {
        fingerprint.UpdatedAt = DateTime.UtcNow;

        context.Fingerprints.Update(fingerprint);
        await context.SaveChangesAsync().ConfigureAwait(false);

        logger.LogInformation("Updated fingerprint with ID {FingerprintId}.", fingerprint.Id);
        return fingerprint;
    }

    public async Task<bool> SoftDeleteFingerprintAsync(int id)
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

    public async Task<bool> SoftDeleteFingerprintByUserIdAsync(string userId)
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

    public async Task<bool> RestoreFingerprintAsync(int id)
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

    public async Task<bool> HardDeleteFingerprintAsync(int id)
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

    #endregion

    #region Transaction Support

    public async Task<Microsoft.EntityFrameworkCore.Storage.IDbContextTransaction> BeginTransactionAsync()
    {
        return await context.Database.BeginTransactionAsync().ConfigureAwait(false);
    }

    #endregion

    #region ISaveableRepository Implementation

    public async Task<int> SaveChangesAsync()
        => await context.SaveChangesAsync().ConfigureAwait(false);

    #endregion
}
