using attendance_monitoring.Models.DTO.Response;
using QrCodeEntity = attendance_monitoring.Classes.QrCode;

namespace attendance_monitoring.Services.QrCode;

/// <summary>
/// Static mapping helper for converting QR code entities to response DTOs.
/// Kept as a static class to avoid circular dependencies between the focused units.
/// </summary>
internal static class QrCodeMapper
{
    public static QrCodeResponseDto MapToResponseDto(QrCodeEntity qrCode)
    {
        return new QrCodeResponseDto
        {
            Id = qrCode.Id,
            SessionId = qrCode.Session?.Id ?? Guid.Empty,
            QrHash = qrCode.QrHash,
            GeneratedAt = qrCode.GeneratedAt,
            ExpiresAt = qrCode.ExpiresAt,
            IsActive = qrCode.IsActive,
            UsageCount = qrCode.UsageCount,
            MaxUsage = qrCode.MaxUsage,
            CreatedAt = qrCode.CreatedAt,
            UpdatedAt = qrCode.UpdatedAt,

            // Session information
            ScheduleId = qrCode.Session?.Schedule?.Id,
            SessionDate = qrCode.Session?.SessionDate,
            SessionStatus = qrCode.Session?.Status,

            // Related entity information (from Session -> Schedule)
            ScheduleTitle = qrCode.Session?.Schedule != null
                ? $"{qrCode.Session.Schedule.DayOfWeek} {qrCode.Session.Schedule.TimeIn}-{qrCode.Session.Schedule.TimeOut}"
                : null,
            SectionName = qrCode.Session?.Schedule?.Section?.Name,
            ActualRoomName = qrCode.Session?.ActualRoom?.Name,
            SubjectName = qrCode.Session?.Schedule?.Subject?.Name,
            InstructorName = qrCode.Session?.Schedule?.Instructor != null
                ? $"{qrCode.Session.Schedule.Instructor.Firstname} {qrCode.Session.Schedule.Instructor.Lastname}"
                : null
        };
    }
}
