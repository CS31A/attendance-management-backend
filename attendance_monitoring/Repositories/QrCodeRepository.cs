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
    #endregion

    #region GetQrCodeByUuidAsync
    public async Task<QrCode?> GetQrCodeByUuidAsync(Guid uuid)
    {
        return await context.QrCodes
            .AsNoTracking()
            .AsSplitQuery()
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
            .FirstOrDefaultAsync(q => q.Uuid == uuid)
            .ConfigureAwait(false);
    }
    #endregion

    #region GetQrCodeByUuidTrackedAsync
    public async Task<QrCode?> GetQrCodeByUuidTrackedAsync(Guid uuid)
    {
        return await context.QrCodes
            .AsSplitQuery()
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
            .FirstOrDefaultAsync(q => q.Uuid == uuid)
            .ConfigureAwait(false);
    }
    #endregion

    #region GetQrCodeByHashAsync
    public async Task<QrCode?> GetQrCodeByHashAsync(string qrHash)
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
    #endregion

    #region GetQrCodesByScheduleIdAsync
    public async Task<IEnumerable<QrCode>> GetQrCodesByScheduleIdAsync(int scheduleId)
    {
        // Use AsSplitQuery to avoid cartesian explosion with multiple ThenInclude chains
        return await context.QrCodes
            .AsNoTracking()
            .AsSplitQuery() // Executes separate queries for each Include to improve performance
            .Include(q => q.Session)
                .ThenInclude(s => s.Schedule)
            .Include(q => q.Session)
                .ThenInclude(s => s.ActualRoom)
            .Where(q => q.Session.ScheduleId == scheduleId)
            .ToListAsync()
            .ConfigureAwait(false);
    }
    #endregion

    #region GetQrCodesBySectionIdAsync
    public async Task<IEnumerable<QrCode>> GetQrCodesBySectionIdAsync(int sectionId)
    {
        // Use AsSplitQuery to avoid cartesian explosion with multiple ThenInclude chains
        return await context.QrCodes
            .AsNoTracking()
            .AsSplitQuery() // Executes separate queries for each Include to improve performance
            .Include(q => q.Session)
                .ThenInclude(s => s.Schedule)
                    .ThenInclude(sch => sch.Section)
            .Include(q => q.Session)
                .ThenInclude(s => s.ActualRoom)
            .Where(q => q.Session.Schedule.SectionId == sectionId)
            .ToListAsync()
            .ConfigureAwait(false);
    }

    public async Task<IEnumerable<QrCode>> GetQrCodesBySessionIdAsync(int sessionId)
    {
        // Use AsSplitQuery to avoid cartesian explosion with multiple ThenInclude chains
        return await context.QrCodes
            .AsNoTracking()
            .AsSplitQuery() // Executes separate queries for each Include to improve performance
            .Include(q => q.Session)
                .ThenInclude(s => s.Schedule)
                    .ThenInclude(sch => sch.Section)
            .Include(q => q.Session)
                .ThenInclude(s => s.Schedule)
                    .ThenInclude(sch => sch.Subject)
            .Include(q => q.Session)
                .ThenInclude(s => s.ActualRoom)
            .Where(q => q.SessionId == sessionId)
            .OrderByDescending(q => q.GeneratedAt)
            .ToListAsync()
            .ConfigureAwait(false);
    }
    #endregion

    #region GetActiveQrCodesAsync
    public async Task<IEnumerable<QrCode>> GetActiveQrCodesAsync()
    {
        var currentTime = DateTime.UtcNow;
        // Use AsSplitQuery to avoid cartesian explosion with multiple ThenInclude chains
        return await context.QrCodes
            .AsNoTracking()
            .AsSplitQuery() // Executes separate queries for each Include to improve performance
            .Include(q => q.Session)
                .ThenInclude(s => s.Schedule)
            .Include(q => q.Session)
                .ThenInclude(s => s.ActualRoom)
            .Where(q => q.IsActive && q.ExpiresAt > currentTime)
            .ToListAsync()
            .ConfigureAwait(false);
    }
    #endregion

    #region GetActiveQrCodesByScheduleIdAsync
    public async Task<IEnumerable<QrCode>> GetActiveQrCodesByScheduleIdAsync(int scheduleId)
    {
        var currentTime = DateTime.UtcNow;
        // Use AsSplitQuery to avoid cartesian explosion with multiple ThenInclude chains
        return await context.QrCodes
            .AsNoTracking()
            .AsSplitQuery() // Executes separate queries for each Include to improve performance
            .Include(q => q.Session)
                .ThenInclude(s => s.Schedule)
            .Include(q => q.Session)
                .ThenInclude(s => s.ActualRoom)
            .Where(q => q.Session.ScheduleId == scheduleId && q.IsActive && q.ExpiresAt > currentTime)
            .ToListAsync()
            .ConfigureAwait(false);
    }
    #endregion

    #region GetExpiredQrCodesAsync
    public async Task<IEnumerable<QrCode>> GetExpiredQrCodesAsync()
    {
        var currentTime = DateTime.UtcNow;
        return await context.QrCodes
            .AsNoTracking()
            .Where(q => q.ExpiresAt <= currentTime)
            .ToListAsync()
            .ConfigureAwait(false);
    }
    #endregion

    #region GetQrCodesExpiringWithinAsync
    public async Task<IEnumerable<QrCode>> GetQrCodesExpiringWithinAsync(TimeSpan expiringWithin)
    {
        var currentTime = DateTime.UtcNow;
        var expirationThreshold = currentTime.Add(expiringWithin);
        
        // Use AsSplitQuery to avoid cartesian explosion with multiple ThenInclude chains
        return await context.QrCodes
            .AsNoTracking()
            .AsSplitQuery() // Executes separate queries for each Include to improve performance
            .Include(q => q.Session)
                .ThenInclude(s => s.Schedule)
            .Include(q => q.Session)
                .ThenInclude(s => s.ActualRoom)
            .Where(q => q.IsActive && q.ExpiresAt > currentTime && q.ExpiresAt <= expirationThreshold)
            .ToListAsync()
            .ConfigureAwait(false);
    }
    #endregion

    #endregion

    #region Write Operations

    #region CreateQrCodeAsync
    public async Task<QrCode> CreateQrCodeAsync(QrCode qrCode)
    {
        qrCode.GeneratedAt = DateTime.UtcNow;
        qrCode.CreatedAt = DateTime.UtcNow;
        qrCode.UpdatedAt = DateTime.UtcNow;
        
        var entry = await context.QrCodes.AddAsync(qrCode).ConfigureAwait(false);
        return entry.Entity;
    }
    #endregion

    #region UpdateQrCodeAsync
    public Task<QrCode> UpdateQrCodeAsync(QrCode qrCode)
    {
        qrCode.UpdatedAt = DateTime.UtcNow;
        var entry = context.QrCodes.Update(qrCode);
        return Task.FromResult(entry.Entity);
    }
    #endregion

    #region DeactivateQrCodeAsync
    public async Task<bool> DeactivateQrCodeAsync(int id)
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
    #endregion

    #region DeactivateQrCodeByHashAsync
    public async Task<bool> DeactivateQrCodeByHashAsync(string qrHash)
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
    #endregion

    #region ReactivateQrCodeAsync
    public async Task<bool> ReactivateQrCodeAsync(int id)
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
    #endregion

    #region ReactivateQrCodeByHashAsync
    public async Task<bool> ReactivateQrCodeByHashAsync(string qrHash)
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
    #endregion

    #region IncrementUsageCountAsync
    public async Task<QrCode?> IncrementUsageCountAsync(int id)
    {
        var qrCode = await context.QrCodes.FindAsync(id).ConfigureAwait(false);
        if (qrCode == null)
        {
            logger.LogWarning("QR code with ID {QrCodeId} not found for usage increment in database.", id);
            return null;
        }
        
        // Overflow protection: deactivate QR code if usage count reaches maximum
        if (qrCode.UsageCount >= int.MaxValue - 1)
        {
            logger.LogWarning("QR Code {QrCodeId} has reached maximum usage count, deactivating.", id);
            qrCode.IsActive = false;
        }
        else
        {
            qrCode.UsageCount++;
        }
        qrCode.UpdatedAt = DateTime.UtcNow;
        
        context.QrCodes.Update(qrCode);
        return qrCode;
    }
    #endregion

    #region IncrementUsageCountByHashAsync
    public async Task<QrCode?> IncrementUsageCountByHashAsync(string qrHash)
    {
        var qrCode = await context.QrCodes
            .FirstOrDefaultAsync(q => q.QrHash == qrHash)
            .ConfigureAwait(false);
        
        if (qrCode == null)
        {
            logger.LogWarning("QR code with hash {QrHash} not found for usage increment in database.", qrHash);
            return null;
        }
        
        // Overflow protection: deactivate QR code if usage count reaches maximum
        if (qrCode.UsageCount >= int.MaxValue - 1)
        {
            logger.LogWarning("QR Code with hash {QrHash} has reached maximum usage count, deactivating.", qrHash);
            qrCode.IsActive = false;
        }
        else
        {
            qrCode.UsageCount++;
        }
        qrCode.UpdatedAt = DateTime.UtcNow;
        
        context.QrCodes.Update(qrCode);
        return qrCode;
    }
    #endregion

    #region DeleteQrCodeAsync
    public async Task<bool> DeleteQrCodeAsync(int id)
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
    #endregion

    #region DeleteExpiredQrCodesAsync
    public async Task<int> DeleteExpiredQrCodesAsync()
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
    #endregion

    #endregion

    #region Validation Operations

    #region QrHashExistsAsync
    public async Task<bool> QrHashExistsAsync(string qrHash)
    {
        return await context.QrCodes
            .AsNoTracking()
            .AnyAsync(q => q.QrHash == qrHash)
            .ConfigureAwait(false);
    }
    #endregion

    #region ValidateQrCodeForUsageAsync
    public async Task<QrCode?> ValidateQrCodeForUsageAsync(string qrHash)
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
    #endregion

    #region BeginTransactionAsync
    public async Task<Microsoft.EntityFrameworkCore.Storage.IDbContextTransaction> BeginTransactionAsync()
    {
        return await context.Database.BeginTransactionAsync().ConfigureAwait(false);
    }
    #endregion

    #region GetScanHistoryAsync
    public async Task<attendance_monitoring.Models.DTO.Response.PagedResult<AttendanceRecord>> GetScanHistoryAsync(
        int qrCodeId,
        int pageNumber,
        int pageSize)
    {
        var query = context.AttendanceRecords
            .Where(ar => ar.QrCodeId == qrCodeId)
            .Where(ar => !ar.Student.IsDeleted)
            .Include(ar => ar.Student)
                .ThenInclude(s => s.User)
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
    #endregion

    #region GetScanHistoryByHashAsync
    public async Task<attendance_monitoring.Models.DTO.Response.PagedResult<AttendanceRecord>> GetScanHistoryByHashAsync(
        string qrHash,
        int pageNumber,
        int pageSize)
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
    #endregion

    #region GetScanStatisticsAsync
    public async Task<(int totalScans, int uniqueStudents, Dictionary<string, int> statusBreakdown, DateTime? firstScan, DateTime? lastScan)> GetScanStatisticsAsync(
        int qrCodeId)
    {
        // Get aggregated statistics directly from SQL
        var stats = await context.AttendanceRecords
            .Where(ar => ar.QrCodeId == qrCodeId)
            .Where(ar => !ar.Student.IsDeleted)
            .GroupBy(ar => 1) // Group all records together
            .Select(g => new
            {
                TotalScans = g.Count(),
                UniqueStudents = g.Select(ar => ar.StudentId).Distinct().Count(),
                FirstScan = g.Min(ar => ar.CheckInTime),
                LastScan = g.Max(ar => ar.CheckInTime)
            })
            .AsNoTracking()
            .FirstOrDefaultAsync()
            .ConfigureAwait(false);
        
        if (stats == null)
        {
            return (0, 0, new Dictionary<string, int>(), null, null);
        }
        
        // Get status breakdown separately (can't be combined with above due to EF limitations)
        var statusBreakdown = await context.AttendanceRecords
            .Where(ar => ar.QrCodeId == qrCodeId)
            .Where(ar => !ar.Student.IsDeleted)
            .GroupBy(ar => ar.Status)
            .Select(g => new { Status = g.Key, Count = g.Count() })
            .AsNoTracking()
            .ToDictionaryAsync(x => x.Status.ToString(), x => x.Count)
            .ConfigureAwait(false);
        
        return (stats.TotalScans, stats.UniqueStudents, statusBreakdown, stats.FirstScan, stats.LastScan);
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
