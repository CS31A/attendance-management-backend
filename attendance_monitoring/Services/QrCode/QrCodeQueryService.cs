using Microsoft.EntityFrameworkCore;
using attendance_monitoring.Constants;
using attendance_monitoring.Data;
using attendance_monitoring.Exceptions;
using attendance_monitoring.IRepository;
using attendance_monitoring.Models.DTO.Response;

namespace attendance_monitoring.Services.QrCode;

/// <summary>
/// Focused unit responsible for read/query operations on QR codes.
/// Handles retrieval by ID, hash, schedule, section, session, and scan history.
/// </summary>
internal sealed class QrCodeQueryService
{
    private readonly IQrCodeRepository _qrCodeRepository;
    private readonly ApplicationDbContext _context;
    private readonly ILogger<QrCodeQueryService> _logger;

    public QrCodeQueryService(
        IQrCodeRepository qrCodeRepository,
        ApplicationDbContext context,
        ILogger<QrCodeQueryService> logger)
    {
        _qrCodeRepository = qrCodeRepository ?? throw new ArgumentNullException(nameof(qrCodeRepository));
        _context = context ?? throw new ArgumentNullException(nameof(context));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task<QrCodeResponseDto?> GetQrCodeByIdAsync(int id)
    {
        try
        {
            _logger.LogInformation("Retrieving QR code with ID: {QrCodeId}", id);

            var qrCode = await _qrCodeRepository.GetQrCodeByIdAsync(id).ConfigureAwait(false);
            if (qrCode == null)
            {
                _logger.LogWarning("QR code with ID {QrCodeId} not found", id);
                return null;
            }

            var responseDto = QrCodeMapper.MapToResponseDto(qrCode);
            _logger.LogInformation("Successfully retrieved QR code with ID: {QrCodeId}", id);
            return responseDto;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error occurred while retrieving QR code with ID: {QrCodeId}", id);
            throw new EntityServiceException("QrCode", $"GetQrCodeById: {id}", "An error occurred while retrieving the QR code", ex);
        }
    }

    public async Task<QrCodeResponseDto?> GetQrCodeByHashAsync(string qrHash)
    {
        try
        {
            _logger.LogInformation("Retrieving QR code with hash: {QrHash}", qrHash);

            var qrCode = await _qrCodeRepository.GetQrCodeByHashAsync(qrHash).ConfigureAwait(false);
            if (qrCode == null)
            {
                _logger.LogWarning("QR code with hash {QrHash} not found", qrHash);
                return null;
            }

            var responseDto = QrCodeMapper.MapToResponseDto(qrCode);
            _logger.LogInformation("Successfully retrieved QR code with hash: {QrHash}", qrHash);
            return responseDto;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error occurred while retrieving QR code with hash: {QrHash}", qrHash);
            throw new EntityServiceException("QrCode", $"GetQrCodeByHash: {qrHash}", "An error occurred while retrieving the QR code", ex);
        }
    }

    public async Task<QrCodeResponseDto?> GetQrCodeByUuidAsync(Guid uuid)
    {
        try
        {
            _logger.LogInformation("Retrieving QR code with UUID: {QrCodeUuid}", uuid);

            var qrCode = await _qrCodeRepository.GetQrCodeByUuidAsync(uuid).ConfigureAwait(false);
            if (qrCode == null)
            {
                _logger.LogWarning("QR code with UUID {QrCodeUuid} not found", uuid);
                return null;
            }

            var responseDto = QrCodeMapper.MapToResponseDto(qrCode);
            _logger.LogInformation("Successfully retrieved QR code with UUID: {QrCodeUuid}", uuid);
            return responseDto;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error occurred while retrieving QR code with UUID: {QrCodeUuid}", uuid);
            throw new EntityServiceException("QrCode", $"GetQrCodeByUuid: {uuid}", "An error occurred while retrieving the QR code", ex);
        }
    }

    public async Task<IEnumerable<QrCodeResponseDto>> GetQrCodesByScheduleIdAsync(int scheduleId)
    {
        try
        {
            _logger.LogInformation("Retrieving QR codes for schedule ID: {ScheduleId}", scheduleId);

            var qrCodes = await _qrCodeRepository.GetQrCodesByScheduleIdAsync(scheduleId).ConfigureAwait(false);
            var responseDtos = qrCodes.Select(QrCodeMapper.MapToResponseDto).ToList();

            _logger.LogInformation("Successfully retrieved {Count} QR codes for schedule ID: {ScheduleId}", responseDtos.Count, scheduleId);
            return responseDtos;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error occurred while retrieving QR codes for schedule ID: {ScheduleId}", scheduleId);
            throw new EntityServiceException("QrCode", $"GetQrCodesByScheduleId: {scheduleId}", "An error occurred while retrieving QR codes", ex);
        }
    }

    public async Task<IEnumerable<QrCodeResponseDto>> GetQrCodesBySectionIdAsync(int sectionId)
    {
        try
        {
            _logger.LogInformation("Retrieving QR codes for section ID: {SectionId}", sectionId);

            var qrCodes = await _qrCodeRepository.GetQrCodesBySectionIdAsync(sectionId).ConfigureAwait(false);
            var responseDtos = qrCodes.Select(QrCodeMapper.MapToResponseDto).ToList();

            _logger.LogInformation("Successfully retrieved {Count} QR codes for section ID: {SectionId}", responseDtos.Count, sectionId);
            return responseDtos;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error occurred while retrieving QR codes for section ID: {SectionId}", sectionId);
            throw new EntityServiceException("QrCode", $"GetQrCodesBySectionId: {sectionId}", "An error occurred while retrieving QR codes", ex);
        }
    }

    public async Task<IEnumerable<QrCodeResponseDto>> GetQrCodesBySessionIdAsync(int sessionId)
    {
        try
        {
            _logger.LogInformation("Retrieving QR codes for session ID: {SessionId}", sessionId);

            var qrCodes = await _qrCodeRepository.GetQrCodesBySessionIdAsync(sessionId).ConfigureAwait(false);
            var responseDtos = qrCodes.Select(QrCodeMapper.MapToResponseDto).ToList();

            _logger.LogInformation("Successfully retrieved {Count} QR codes for session ID: {SessionId}", responseDtos.Count, sessionId);
            return responseDtos;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error occurred while retrieving QR codes for session ID: {SessionId}", sessionId);
            throw new EntityServiceException("QrCode", $"GetQrCodesBySessionId: {sessionId}", "An error occurred while retrieving QR codes", ex);
        }
    }

    public async Task<IEnumerable<QrCodeResponseDto>> GetQrCodesBySessionUuidAsync(Guid sessionUuid)
    {
        var sessionId = await _context.Sessions
            .AsNoTracking()
            .Where(session => session.Uuid == sessionUuid)
            .Select(session => (int?)session.Id)
            .FirstOrDefaultAsync()
            .ConfigureAwait(false);

        if (!sessionId.HasValue)
        {
            throw new EntityNotFoundException<Guid>("Session", sessionUuid);
        }

        return await GetQrCodesBySessionIdAsync(sessionId.Value).ConfigureAwait(false);
    }

    public async Task<IEnumerable<QrCodeResponseDto>> GetActiveQrCodesAsync()
    {
        try
        {
            _logger.LogInformation("Retrieving all active QR codes");

            var qrCodes = await _qrCodeRepository.GetActiveQrCodesAsync().ConfigureAwait(false);
            var responseDtos = qrCodes.Select(QrCodeMapper.MapToResponseDto).ToList();

            _logger.LogInformation("Successfully retrieved {Count} active QR codes", responseDtos.Count);
            return responseDtos;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error occurred while retrieving active QR codes");
            throw new EntityServiceException("QrCode", "GetActiveQrCodes", "An error occurred while retrieving active QR codes", ex);
        }
    }

    public async Task<QrCodeScanHistoryResponseDto> GetScanHistoryAsync(
        int qrCodeId,
        int instructorId,
        string userRole,
        int pageNumber = 1,
        int pageSize = 50)
    {
        _logger.LogInformation(
            "Retrieving scan history for QR code {QrCodeId} by instructor {InstructorId} with role {UserRole}",
            qrCodeId, instructorId, userRole);

        try
        {
            if (pageNumber < 1)
            {
                throw new ValidationException("Page number must be greater than 0");
            }

            if (pageSize < 1 || pageSize > 100)
            {
                throw new ValidationException("Page size must be between 1 and 100");
            }

            var qrCode = await _qrCodeRepository.GetQrCodeByIdAsync(qrCodeId);
            if (qrCode == null)
            {
                throw new EntityNotFoundException<int>("QrCode", qrCodeId);
            }

            // Authorization check — instructors can only see their own sections
            if (userRole == RoleConstants.Instructor)
            {
                var authSession = await _context.Sessions
                    .Include(s => s.Schedule)
                    .FirstOrDefaultAsync(s => s.Id == qrCode.SessionId);

                if (authSession == null || authSession.Schedule.InstructorId != instructorId)
                {
                    throw new EntityUnauthorizedException(
                        "QrCode",
                        qrCodeId.ToString(),
                        "You do not have permission to view scan history for this QR code");
                }
            }

            var scanHistory = await _qrCodeRepository.GetScanHistoryAsync(qrCodeId, pageNumber, pageSize);
            var statistics = await _qrCodeRepository.GetScanStatisticsAsync(qrCodeId);

            var session = await _context.Sessions
                .Include(s => s.Schedule)
                    .ThenInclude(sch => sch!.Subject)
                .FirstOrDefaultAsync(s => s.Id == qrCode.SessionId);

            var qrCodeInfo = new QrCodeInfoDto
            {
                Id = qrCode.Uuid,
                Hash = qrCode.QrHash,
                SessionId = session?.Uuid ?? Guid.Empty,
                SessionDate = session?.SessionDate ?? DateTime.MinValue,
                SessionSubject = session?.Schedule?.Subject?.Name ?? "Unknown",
                GeneratedAt = qrCode.GeneratedAt,
                ExpiresAt = qrCode.ExpiresAt,
                IsActive = qrCode.IsActive,
                TotalScans = statistics.totalScans,
                UniqueStudents = statistics.uniqueStudents
            };

            // Calculate average scan time from all attendance records
            DateTime? averageScanTime = null;
            if (statistics.totalScans > 0)
            {
                var allScanTimes = await _context.AttendanceRecords
                    .Where(ar => ar.QrCodeId == qrCodeId)
                    .Where(ar => !ar.Student.IsDeleted)
                    .Select(ar => ar.CheckInTime)
                    .ToListAsync();

                if (allScanTimes.Any())
                {
                    var totalTicks = allScanTimes.Sum(dt => dt.Ticks);
                    var avgTicks = totalTicks / allScanTimes.Count;
                    averageScanTime = new DateTime(avgTicks);
                }
            }

            var scanStatistics = new QrCodeStatisticsDto
            {
                TotalScans = statistics.totalScans,
                UniqueStudents = statistics.uniqueStudents,
                PresentCount = statistics.statusBreakdown.GetValueOrDefault("Present", 0),
                LateCount = statistics.statusBreakdown.GetValueOrDefault("Late", 0),
                ExcusedCount = statistics.statusBreakdown.GetValueOrDefault("Excused", 0),
                FirstScanAt = statistics.firstScan,
                LastScanAt = statistics.lastScan,
                AverageScanTime = averageScanTime
            };

            var scanItems = scanHistory.Items.Select(ar => new QrCodeScanHistoryItemDto
            {
                AttendanceRecordId = ar.Uuid,
                StudentId = ar.Student?.Uuid ?? Guid.Empty,
                StudentEmail = ar.Student?.User?.Email ?? "Unknown",
                StudentName = $"{ar.Student?.Firstname} {ar.Student?.Lastname}".Trim(),
                ScannedAt = ar.CheckInTime,
                Status = ar.Status.ToString(),
                SessionId = ar.Session?.Uuid ?? Guid.Empty,
                SessionDate = ar.Session?.SessionDate ?? DateTime.MinValue,
                SessionSubject = ar.Session?.Schedule?.Subject?.Name ?? "Unknown",
                SessionSection = ar.Session?.Schedule?.Section?.Name ?? "Unknown"
            }).ToList();

            var pagedScans = new PagedResult<QrCodeScanHistoryItemDto>
            {
                Items = scanItems,
                TotalCount = scanHistory.TotalCount,
                PageNumber = scanHistory.PageNumber,
                PageSize = scanHistory.PageSize
            };

            _logger.LogInformation(
                "Successfully retrieved {Count} scan records for QR code {QrCodeId}",
                scanItems.Count, qrCodeId);

            return new QrCodeScanHistoryResponseDto
            {
                QrCodeInfo = qrCodeInfo,
                ScanStatistics = scanStatistics,
                Scans = pagedScans
            };
        }
        catch (ValidationException)
        {
            throw;
        }
        catch (EntityNotFoundException<int>)
        {
            throw;
        }
        catch (EntityUnauthorizedException)
        {
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error occurred while retrieving scan history for QR code {QrCodeId}", qrCodeId);
            throw new EntityServiceException("QrCode", "GetScanHistory", "An error occurred while retrieving scan history", ex);
        }
    }

    public async Task<QrCodeScanHistoryResponseDto> GetScanHistoryByUuidAsync(
        Guid uuid,
        int instructorId,
        string userRole,
        int pageNumber = 1,
        int pageSize = 50)
    {
        var qrCode = await _qrCodeRepository.GetQrCodeByUuidAsync(uuid).ConfigureAwait(false);
        if (qrCode == null)
        {
            throw new EntityNotFoundException<Guid>("QrCode", uuid);
        }

        return await GetScanHistoryAsync(qrCode.Id, instructorId, userRole, pageNumber, pageSize).ConfigureAwait(false);
    }

    public async Task<QrCodeScanHistoryResponseDto> GetScanHistoryByHashAsync(
        string qrHash,
        int instructorId,
        string userRole,
        int pageNumber = 1,
        int pageSize = 50)
    {
        _logger.LogInformation(
            "Retrieving scan history for QR hash {QrHash} by instructor {InstructorId}",
            qrHash, instructorId);

        try
        {
            var qrCode = await _qrCodeRepository.GetQrCodeByHashAsync(qrHash);
            if (qrCode == null)
            {
                throw new EntityNotFoundException<string>("QrCode", qrHash);
            }

            return await GetScanHistoryAsync(qrCode.Id, instructorId, userRole, pageNumber, pageSize);
        }
        catch (ValidationException)
        {
            throw;
        }
        catch (EntityNotFoundException<int>)
        {
            throw;
        }
        catch (EntityNotFoundException<string>)
        {
            throw;
        }
        catch (EntityUnauthorizedException)
        {
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error occurred while retrieving scan history for QR hash {QrHash}", qrHash);
            throw new EntityServiceException("QrCode", "GetScanHistoryByHash", "An error occurred while retrieving scan history", ex);
        }
    }
}
