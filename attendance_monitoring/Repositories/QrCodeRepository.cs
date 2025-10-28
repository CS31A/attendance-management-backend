using attendance_monitoring.Classes;
using attendance_monitoring.Data;
using attendance_monitoring.IRepository;
using Microsoft.EntityFrameworkCore;

namespace attendance_monitoring.Repositories;

public class QrCodeRepository(ApplicationDbContext context, ILogger<QrCodeRepository> logger) : IQrCodeRepository
{
    #region Read Operations

    #region GetQrCodeByIdAsync
    public async Task<QrCode?> GetQrCodeByIdAsync(int id)
    {
        try
        {
            // Use AsSplitQuery to avoid cartesian explosion with multiple ThenInclude chains
            return await context.QrCodes
                .AsNoTracking()
                .AsSplitQuery() // Executes separate queries for each Include to improve performance
                .Include(q => q.Session)
                    .ThenInclude(s => s.Schedule)
                        .ThenInclude(sch => sch.Subject)
                .Include(q => q.Session)
                    .ThenInclude(s => s.Schedule)
                        .ThenInclude(sch => sch.Section)
                .Include(q => q.Session)
                    .ThenInclude(s => s.Schedule)
                        .ThenInclude(sch => sch.Instructor)
                .Include(q => q.Session)
                    .ThenInclude(s => s.ActualRoom)
                .FirstOrDefaultAsync(q => q.Id == id)
                .ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "An error occurred while retrieving QR code with ID {QrCodeId} from database.", id);
            throw;
        }
    }
    #endregion

    #region GetQrCodeByHashAsync
    public async Task<QrCode?> GetQrCodeByHashAsync(string qrHash)
    {
        try
        {
            // Use AsSplitQuery to avoid cartesian explosion with multiple ThenInclude chains
            return await context.QrCodes
                .AsNoTracking()
                .AsSplitQuery() // Executes separate queries for each Include to improve performance
                .Include(q => q.Session)
                    .ThenInclude(s => s.Schedule)
                        .ThenInclude(sch => sch.Subject)
                .Include(q => q.Session)
                    .ThenInclude(s => s.Schedule)
                        .ThenInclude(sch => sch.Section)
                .Include(q => q.Session)
                    .ThenInclude(s => s.Schedule)
                        .ThenInclude(sch => sch.Instructor)
                .Include(q => q.Session)
                    .ThenInclude(s => s.ActualRoom)
                .FirstOrDefaultAsync(q => q.QrHash == qrHash)
                .ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "An error occurred while retrieving QR code with hash {QrHash} from database.", qrHash);
            throw;
        }
    }
    #endregion

    #region GetQrCodesByScheduleIdAsync
    public async Task<IEnumerable<QrCode>> GetQrCodesByScheduleIdAsync(int scheduleId)
    {
        try
        {
            return await context.QrCodes
                .AsNoTracking()
                .Include(q => q.Session)
                    .ThenInclude(s => s.Schedule)
                .Include(q => q.Session)
                    .ThenInclude(s => s.ActualRoom)
                .Where(q => q.Session.ScheduleId == scheduleId)
                .ToListAsync()
                .ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "An error occurred while retrieving QR codes for schedule ID {ScheduleId} from database.", scheduleId);
            throw;
        }
    }
    #endregion

    #region GetQrCodesBySectionIdAsync
    public async Task<IEnumerable<QrCode>> GetQrCodesBySectionIdAsync(int sectionId)
    {
        try
        {
            return await context.QrCodes
                .AsNoTracking()
                .Include(q => q.Session)
                    .ThenInclude(s => s.Schedule)
                        .ThenInclude(sch => sch.Section)
                .Include(q => q.Session)
                    .ThenInclude(s => s.ActualRoom)
                .Where(q => q.Session.Schedule.SectionId == sectionId)
                .ToListAsync()
                .ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "An error occurred while retrieving QR codes for section ID {SectionId} from database.", sectionId);
            throw;
        }
    }
    #endregion

    #region GetActiveQrCodesAsync
    public async Task<IEnumerable<QrCode>> GetActiveQrCodesAsync()
    {
        try
        {
            var currentTime = DateTime.UtcNow;
            return await context.QrCodes
                .AsNoTracking()
                .Include(q => q.Session)
                    .ThenInclude(s => s.Schedule)
                .Include(q => q.Session)
                    .ThenInclude(s => s.ActualRoom)
                .Where(q => q.IsActive && q.ExpiresAt > currentTime)
                .ToListAsync()
                .ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "An error occurred while retrieving active QR codes from database.");
            throw;
        }
    }
    #endregion

    #region GetActiveQrCodesByScheduleIdAsync
    public async Task<IEnumerable<QrCode>> GetActiveQrCodesByScheduleIdAsync(int scheduleId)
    {
        try
        {
            var currentTime = DateTime.UtcNow;
            return await context.QrCodes
                .AsNoTracking()
                .Include(q => q.Session)
                    .ThenInclude(s => s.Schedule)
                .Include(q => q.Session)
                    .ThenInclude(s => s.ActualRoom)
                .Where(q => q.Session.ScheduleId == scheduleId && q.IsActive && q.ExpiresAt > currentTime)
                .ToListAsync()
                .ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "An error occurred while retrieving active QR codes for schedule ID {ScheduleId} from database.", scheduleId);
            throw;
        }
    }
    #endregion

    #region GetExpiredQrCodesAsync
    public async Task<IEnumerable<QrCode>> GetExpiredQrCodesAsync()
    {
        try
        {
            var currentTime = DateTime.UtcNow;
            return await context.QrCodes
                .AsNoTracking()
                .Where(q => q.ExpiresAt <= currentTime)
                .ToListAsync()
                .ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "An error occurred while retrieving expired QR codes from database.");
            throw;
        }
    }
    #endregion

    #region GetQrCodesExpiringWithinAsync
    public async Task<IEnumerable<QrCode>> GetQrCodesExpiringWithinAsync(TimeSpan expiringWithin)
    {
        try
        {
            var currentTime = DateTime.UtcNow;
            var expirationThreshold = currentTime.Add(expiringWithin);

            return await context.QrCodes
                .AsNoTracking()
                .Include(q => q.Session)
                    .ThenInclude(s => s.Schedule)
                .Include(q => q.Session)
                    .ThenInclude(s => s.ActualRoom)
                .Where(q => q.IsActive && q.ExpiresAt > currentTime && q.ExpiresAt <= expirationThreshold)
                .ToListAsync()
                .ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "An error occurred while retrieving QR codes expiring within {ExpiringWithin} from database.", expiringWithin);
            throw;
        }
    }
    #endregion

    #endregion

    #region Write Operations

    #region CreateQrCodeAsync
    public async Task<QrCode> CreateQrCodeAsync(QrCode qrCode)
    {
        try
        {
            qrCode.GeneratedAt = DateTime.UtcNow;
            qrCode.CreatedAt = DateTime.UtcNow;
            qrCode.UpdatedAt = DateTime.UtcNow;
            
            var entry = await context.QrCodes.AddAsync(qrCode).ConfigureAwait(false);
            return entry.Entity;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "An error occurred while creating QR code with hash {QrHash} in database.", qrCode.QrHash);
            throw;
        }
    }
    #endregion

    #region UpdateQrCodeAsync
    public Task<QrCode> UpdateQrCodeAsync(QrCode qrCode)
    {
        try
        {
            qrCode.UpdatedAt = DateTime.UtcNow;
            var entry = context.QrCodes.Update(qrCode);
            return Task.FromResult(entry.Entity);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "An error occurred while updating QR code with ID {QrCodeId} in database.", qrCode.Id);
            throw;
        }
    }
    #endregion

    #region DeactivateQrCodeAsync
    public async Task<bool> DeactivateQrCodeAsync(int id)
    {
        try
        {
            var qrCode = await context.QrCodes.FindAsync(id).ConfigureAwait(false);
            if (qrCode == null)
            {
                logger.LogWarning("QR code with ID {QrCodeId} not found for deactivation in database.", id);
                return false;
            }

            qrCode.IsActive = false;
            qrCode.UpdatedAt = DateTime.UtcNow;
            // Note: RevokedBy and RevocationReason should be set by the service layer
            
            context.QrCodes.Update(qrCode);
            return true;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "An error occurred while deactivating QR code with ID {QrCodeId} in database.", id);
            throw;
        }
    }
    #endregion

    #region DeactivateQrCodeByHashAsync
    public async Task<bool> DeactivateQrCodeByHashAsync(string qrHash)
    {
        try
        {
            var qrCode = await context.QrCodes
                .FirstOrDefaultAsync(q => q.QrHash == qrHash)
                .ConfigureAwait(false);
                
            if (qrCode == null)
            {
                logger.LogWarning("QR code with hash {QrHash} not found for deactivation in database.", qrHash);
                return false;
            }

            qrCode.IsActive = false;
            qrCode.UpdatedAt = DateTime.UtcNow;
            // Note: RevokedBy and RevocationReason should be set by the service layer
            
            context.QrCodes.Update(qrCode);
            return true;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "An error occurred while deactivating QR code with hash {QrHash} in database.", qrHash);
            throw;
        }
    }
    #endregion

    #region ReactivateQrCodeAsync
    public async Task<bool> ReactivateQrCodeAsync(int id)
    {
        try
        {
            var qrCode = await context.QrCodes.FindAsync(id).ConfigureAwait(false);
            if (qrCode == null)
            {
                logger.LogWarning("QR code with ID {QrCodeId} not found for reactivation in database.", id);
                return false;
            }

            qrCode.IsActive = true;
            qrCode.RevokedAt = null;
            qrCode.RevokedBy = null;
            qrCode.RevocationReason = null;
            qrCode.UpdatedAt = DateTime.UtcNow;
            
            context.QrCodes.Update(qrCode);
            logger.LogInformation("Reactivated QR code with ID {QrCodeId} in database.", id);
            return true;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "An error occurred while reactivating QR code with ID {QrCodeId} in database.", id);
            throw;
        }
    }
    #endregion

    #region ReactivateQrCodeByHashAsync
    public async Task<bool> ReactivateQrCodeByHashAsync(string qrHash)
    {
        try
        {
            var qrCode = await context.QrCodes
                .FirstOrDefaultAsync(q => q.QrHash == qrHash)
                .ConfigureAwait(false);
                
            if (qrCode == null)
            {
                logger.LogWarning("QR code with hash {QrHash} not found for reactivation in database.", qrHash);
                return false;
            }

            qrCode.IsActive = true;
            qrCode.RevokedAt = null;
            qrCode.RevokedBy = null;
            qrCode.RevocationReason = null;
            qrCode.UpdatedAt = DateTime.UtcNow;
            
            context.QrCodes.Update(qrCode);
            logger.LogInformation("Reactivated QR code with hash {QrHash} in database.", qrHash);
            return true;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "An error occurred while reactivating QR code with hash {QrHash} in database.", qrHash);
            throw;
        }
    }
    #endregion

    #region IncrementUsageCountAsync
    public async Task<QrCode?> IncrementUsageCountAsync(int id)
    {
        try
        {
            var qrCode = await context.QrCodes.FindAsync(id).ConfigureAwait(false);
            if (qrCode == null)
            {
                logger.LogWarning("QR code with ID {QrCodeId} not found for usage increment in database.", id);
                return null;
            }

            qrCode.UsageCount++;
            qrCode.UpdatedAt = DateTime.UtcNow;
            
            context.QrCodes.Update(qrCode);
            return qrCode;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "An error occurred while incrementing usage count for QR code with ID {QrCodeId} in database.", id);
            throw;
        }
    }
    #endregion

    #region IncrementUsageCountByHashAsync
    public async Task<QrCode?> IncrementUsageCountByHashAsync(string qrHash)
    {
        try
        {
            var qrCode = await context.QrCodes
                .FirstOrDefaultAsync(q => q.QrHash == qrHash)
                .ConfigureAwait(false);
                
            if (qrCode == null)
            {
                logger.LogWarning("QR code with hash {QrHash} not found for usage increment in database.", qrHash);
                return null;
            }

            qrCode.UsageCount++;
            qrCode.UpdatedAt = DateTime.UtcNow;
            
            context.QrCodes.Update(qrCode);
            return qrCode;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "An error occurred while incrementing usage count for QR code with hash {QrHash} in database.", qrHash);
            throw;
        }
    }
    #endregion

    #region DeleteQrCodeAsync
    public async Task<bool> DeleteQrCodeAsync(int id)
    {
        try
        {
            var qrCode = await context.QrCodes.FindAsync(id).ConfigureAwait(false);
            if (qrCode == null)
            {
                logger.LogWarning("QR code with ID {QrCodeId} not found for deletion in database.", id);
                return false;
            }

            context.QrCodes.Remove(qrCode);
            return true;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "An error occurred while deleting QR code with ID {QrCodeId} from database.", id);
            throw;
        }
    }
    #endregion

    #region DeleteExpiredQrCodesAsync
    public async Task<int> DeleteExpiredQrCodesAsync()
    {
        try
        {
            var currentTime = DateTime.UtcNow;
            var expiredQrCodes = await context.QrCodes
                .AsNoTracking()
                .Where(q => q.ExpiresAt <= currentTime)
                .ToListAsync()
                .ConfigureAwait(false);

            if (expiredQrCodes.Count == 0)
            {
                logger.LogInformation("No expired QR codes found for deletion.");
                return 0;
            }

            context.QrCodes.RemoveRange(expiredQrCodes);
            logger.LogInformation("Deleted {Count} expired QR codes from database.", expiredQrCodes.Count);
            return expiredQrCodes.Count;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "An error occurred while deleting expired QR codes from database.");
            throw;
        }
    }
    #endregion

    #endregion

    #region Validation Operations

    #region QrHashExistsAsync
    public async Task<bool> QrHashExistsAsync(string qrHash)
    {
        try
        {
            return await context.QrCodes
                .AsNoTracking()
                .AnyAsync(q => q.QrHash == qrHash)
                .ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "An error occurred while checking if QR hash {QrHash} exists in database.", qrHash);
            throw;
        }
    }
    #endregion

    #region ValidateQrCodeForUsageAsync
    public async Task<QrCode?> ValidateQrCodeForUsageAsync(string qrHash)
    {
        try
        {
            var currentTime = DateTime.UtcNow;
            // Use AsSplitQuery to avoid cartesian explosion with multiple ThenInclude chains
            var qrCode = await context.QrCodes
                .AsNoTracking()
                .AsSplitQuery() // Executes separate queries for each Include to improve performance
                .Include(q => q.Session)
                    .ThenInclude(s => s.Schedule)
                        .ThenInclude(sch => sch.Subject)
                .Include(q => q.Session)
                    .ThenInclude(s => s.Schedule)
                        .ThenInclude(sch => sch.Section)
                .Include(q => q.Session)
                    .ThenInclude(s => s.Schedule)
                        .ThenInclude(sch => sch.Instructor)
                .Include(q => q.Session)
                    .ThenInclude(s => s.ActualRoom)
                .FirstOrDefaultAsync(q => q.QrHash == qrHash)
                .ConfigureAwait(false);

            if (qrCode == null)
            {
                logger.LogWarning("QR code with hash {QrHash} not found for validation.", qrHash);
                return null;
            }

            // Check if QR code is active
            if (!qrCode.IsActive)
            {
                logger.LogWarning("QR code with hash {QrHash} is not active.", qrHash);
                return null;
            }

            // Check if QR code has expired
            if (qrCode.ExpiresAt <= currentTime)
            {
                logger.LogWarning("QR code with hash {QrHash} has expired at {ExpiresAt}.", qrHash, qrCode.ExpiresAt);
                return null;
            }

            // Check usage limits if applicable
            if (qrCode.MaxUsage.HasValue && qrCode.UsageCount >= qrCode.MaxUsage.Value)
            {
                logger.LogWarning("QR code with hash {QrHash} has reached maximum usage limit of {MaxUsage}.", qrHash, qrCode.MaxUsage.Value);
                return null;
            }

            return qrCode;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "An error occurred while validating QR code with hash {QrHash} for usage.", qrHash);
            throw;
        }
    }
    #endregion

    #endregion

    #region Utility Operations

    #region SaveChangesAsync
    public async Task<int> SaveChangesAsync()
    {
        return await context.SaveChangesAsync().ConfigureAwait(false);
    }
    #endregion

    #endregion
}