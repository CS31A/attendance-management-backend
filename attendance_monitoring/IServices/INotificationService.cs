using attendance_monitoring.Models.DTO.Response;

namespace attendance_monitoring.IServices;

public interface INotificationService
{
    Task SendToUserAsync(string userId, NotificationDto notification);
    Task SendToRoleAsync(string roleName, NotificationDto notification);
    Task SendToGroupAsync(string groupName, NotificationDto notification);

    // Specialized methods for critical scenarios
    Task NotifyQrCodeGeneratedAsync(Guid qrCodeId, string instructorId);
    Task NotifyStudentCheckedInAsync(string studentId, string instructorId, Guid sessionId, string status);
    Task NotifySessionStartedAsync(Guid sessionId, IEnumerable<string> studentIds);
    Task NotifySessionEndedAsync(Guid sessionId, IEnumerable<string> studentIds);
}
