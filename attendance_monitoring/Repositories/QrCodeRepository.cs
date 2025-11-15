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

    #region Atomic Operations

    #region AtomicIncrementUsageAsync
    /// <summary>
    /// Atomically increments the usage count for a QR code with all validations in a single database operation.
    /// This prevents race conditions where multiple concurrent requests could pass validation.
    /// </summary>
    public async Task<int> AtomicIncrementUsageAsync(string qrHash, DateTime currentTime)
    {
        try
        {
            // Execute atomic UPDATE with all validations in WHERE clause
            // This ensures only one request can increment when conditions are met
            var result = await context.Database.ExecuteSqlInterpolatedAsync(
                $@"UPDATE QrCodes
                   SET UsageCount = UsageCount + 1, UpdatedAt = {currentTime}
                   WHERE QrHash = {qrHash}
                     AND IsActive = 1
                     AND ExpiresAt > {currentTime}
                     AND (MaxUsage IS NULL OR UsageCount < MaxUsage)")
                .ConfigureAwait(false);

            logger.LogInformation("Atomic increment result for QR hash {QrHash}: {Result} rows affected", qrHash, result);
            return result;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "An error occurred during atomic increment for QR code with hash {QrHash}", qrHash);
            throw;
        }
    }
    #endregion

    #region BeginTransactionAsync
    public async Task<Microsoft.EntityFrameworkCore.Storage.IDbContextTransaction> BeginTransactionAsync()
    {
        try
        {
            return await context.Database.BeginTransactionAsync().ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "An error occurred while beginning database transaction");
            throw;
        }
    }
    #endregion

    #region GetScanHistoryAsync
    public async Task<attendance_monitoring.Models.DTO.Response.PagedResult<AttendanceRecord>> GetScanHistoryAsync(
        int qrCodeId,
        int pageNumber,
        int pageSize)
    {
        try
        {
            var query = context.AttendanceRecords
                .Where(ar => ar.QrCodeId == qrCodeId)
                .Include(ar => ar.Student)
                .Include(ar => ar.Session)
                    .ThenInclude(s => s!.Schedule)
                        .ThenInclude(sch => sch!.Subject)
                .Include(ar => ar.Session)
                    .ThenInclude(s => s!.Schedule)
                        .ThenInclude(sch => sch!.Section)
                .OrderByDescending(ar => ar.CheckInTime);

            var totalCount = await query.CountAsync().ConfigureAwait(false);

            var items = await query
                .Skip((pageNumber - 1) * pageSize)
                .Take(pageSize)
                .AsNoTracking()
                .ToListAsync()
                .ConfigureAwait(false);

            return new attendance_monitoring.Models.DTO.Response.PagedResult<AttendanceRecord>
            {
                Items = items,
                TotalCount = totalCount,
                PageNumber = pageNumber,
                PageSize = pageSize
            };
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "An error occurred while retrieving scan history for QR code ID {QrCodeId}", qrCodeId);
            throw;
        }
    }
    #endregion

    #region GetScanHistoryByHashAsync
    public async Task<attendance_monitoring.Models.DTO.Response.PagedResult<AttendanceRecord>> GetScanHistoryByHashAsync(
        string qrHash,
        int pageNumber,
        int pageSize)
    {
        try
        {
            // First, find the QR code by hash
            var qrCode = await context.QrCodes
                .AsNoTracking()
                .FirstOrDefaultAsync(qr => qr.QrHash == qrHash)
                .ConfigureAwait(false);

            if (qrCode == null)
            {
                return new attendance_monitoring.Models.DTO.Response.PagedResult<AttendanceRecord>
                {
                    Items = new List<AttendanceRecord>(),
                    TotalCount = 0,
                    PageNumber = pageNumber,
                    PageSize = pageSize
                };
            }

            // Reuse the ID-based method
            return await GetScanHistoryAsync(qrCode.Id, pageNumber, pageSize).ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "An error occurred while retrieving scan history for QR hash {QrHash}", qrHash);
            throw;
        }
    }
    #endregion

    #region GetScanStatisticsAsync
    public async Task<(int totalScans, int uniqueStudents, Dictionary<string, int> statusBreakdown, DateTime? firstScan, DateTime? lastScan)> GetScanStatisticsAsync(
        int qrCodeId)
    {
        try
        {
            var scans = await context.AttendanceRecords
                .Where(ar => ar.QrCodeId == qrCodeId)
                .AsNoTracking()
                .ToListAsync()
                .ConfigureAwait(false);

            if (!scans.Any())
            {
                return (0, 0, new Dictionary<string, int>(), null, null);
            }

            var totalScans = scans.Count;
            var uniqueStudents = scans.Select(s => s.StudentId).Distinct().Count();

            var statusBreakdown = scans
                .GroupBy(s => s.Status.ToString())
                .ToDictionary(g => g.Key, g => g.Count());

            var firstScan = scans.Min(s => s.CheckInTime);
            var lastScan = scans.Max(s => s.CheckInTime);

            return (totalScans, uniqueStudents, statusBreakdown, firstScan, lastScan);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "An error occurred while retrieving scan statistics for QR code ID {QrCodeId}", qrCodeId);
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