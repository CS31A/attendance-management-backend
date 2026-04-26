using attendance_monitoring.Hubs;
using attendance_monitoring.IRepository;
using attendance_monitoring.IServices;
using attendance_monitoring.Models.DTO.Response;
using Microsoft.AspNetCore.SignalR;

namespace attendance_monitoring.Services;

public class NotificationService : INotificationService
{
    private readonly IHubContext<NotificationHub> _hubContext;
    private readonly IUserConnectionManager _connectionManager;
    private readonly INotificationPreferenceService _preferenceService;
    private readonly IQrCodeRepository _qrCodeRepository;
    private readonly ISessionRepository _sessionRepository;
    private readonly IStudentRepository _studentRepository;
    private readonly ILogger<NotificationService> _logger;

    public NotificationService(
        IHubContext<NotificationHub> hubContext,
        IUserConnectionManager connectionManager,
        INotificationPreferenceService preferenceService,
        IQrCodeRepository qrCodeRepository,
        ISessionRepository sessionRepository,
        IStudentRepository studentRepository,
        ILogger<NotificationService> logger)
    {
        _hubContext = hubContext;
        _connectionManager = connectionManager;
        _preferenceService = preferenceService;
        _qrCodeRepository = qrCodeRepository;
        _sessionRepository = sessionRepository;
        _studentRepository = studentRepository;
        _logger = logger;
    }

    public async Task SendToUserAsync(string userId, NotificationDto notification)
    {
        try
        {
            if (await _connectionManager.IsOnlineAsync(userId))
            {
                var connections = await _connectionManager.GetConnectionsAsync(userId);
                await _hubContext.Clients.Clients(connections.ToList())
                    .SendAsync("ReceiveNotification", notification);

                _logger.LogInformation("Notification sent to user {UserId}: {Title}",
                    userId, notification.Title);
            }
            else
            {
                _logger.LogInformation("User {UserId} offline, notification not delivered: {Title}",
                    userId, notification.Title);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error sending notification to user {UserId}", userId);
        }
    }

    public async Task SendToRoleAsync(string roleName, NotificationDto notification)
    {
        try
        {
            await _hubContext.Clients.Group($"role:{roleName}")
                .SendAsync("ReceiveNotification", notification);

            _logger.LogInformation("Notification sent to role {Role}: {Title}",
                roleName, notification.Title);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error sending notification to role {Role}", roleName);
        }
    }

    public async Task SendToGroupAsync(string groupName, NotificationDto notification)
    {
        try
        {
            await _hubContext.Clients.Group(groupName)
                .SendAsync("ReceiveNotification", notification);

            _logger.LogInformation("Notification sent to group {Group}: {Title}",
                groupName, notification.Title);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error sending notification to group {Group}", groupName);
        }
    }

    public async Task NotifyQrCodeGeneratedAsync(Guid qrCodeId, string instructorId)
    {
        try
        {
            var qrCode = await _qrCodeRepository.GetQrCodeByIdAsync(qrCodeId);
            if (qrCode == null) return;

            var notification = new NotificationDto
            {
                Title = "QR Code Generated",
                Message = $"QR code generated successfully. Expires at {qrCode.ExpiresAt:HH:mm}",
                Type = "Success",
                Category = "QrCode",
                Metadata = new { QrCodeId = qrCodeId, ExpiresAt = qrCode.ExpiresAt }
            };

            await SendToUserAsync(instructorId, notification);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error notifying QR code generation for {QrCodeId}", qrCodeId);
        }
    }

    public async Task NotifyStudentCheckedInAsync(string studentId, string instructorId, Guid sessionId, string status)
    {
        try
        {
            var student = await _studentRepository.GetStudentByUserIdAsync(studentId);
            if (student == null) return;

            // Notify student
            var studentNotification = new NotificationDto
            {
                Title = "Attendance Recorded",
                Message = $"You checked in successfully. Status: {status}",
                Type = status == "Present" ? "Success" : "Warning",
                Category = "Attendance",
                Metadata = new { SessionId = sessionId, Status = status }
            };
            await SendToUserAsync(studentId, studentNotification);

            // Notify instructor ONLY if opted-in
            var instructorOptedIn = await _preferenceService.GetRealtimeCheckInAsync(instructorId);
            if (instructorOptedIn)
            {
                var instructorNotification = new NotificationDto
                {
                    Title = "Student Checked In",
                    Message = $"{student.Firstname} {student.Lastname} - {status}",
                    Type = "Info",
                    Category = "Attendance",
                    Metadata = new { StudentId = studentId, SessionId = sessionId, Status = status }
                };
                await SendToUserAsync(instructorId, instructorNotification);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error notifying student check-in");
        }
    }

    public async Task NotifySessionStartedAsync(Guid sessionId, IEnumerable<string> studentIds)
    {
        try
        {
            var session = await _sessionRepository.GetSessionByIdAsync(sessionId);
            if (session == null) return;

            var notification = new NotificationDto
            {
                Title = "Session Started",
                Message = $"{session.Schedule.Subject.Name} session is now active. Scan QR to mark attendance.",
                Type = "Info",
                Category = "Session",
                Metadata = new { SessionId = sessionId }
            };

            foreach (var studentId in studentIds)
            {
                await SendToUserAsync(studentId, notification);
            }

            _logger.LogInformation("Session started notification sent to {Count} students", studentIds.Count());
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error notifying session started for {SessionId}", sessionId);
        }
    }

    public async Task NotifySessionEndedAsync(Guid sessionId, IEnumerable<string> studentIds)
    {
        try
        {
            var session = await _sessionRepository.GetSessionByIdAsync(sessionId);
            if (session == null) return;

            var notification = new NotificationDto
            {
                Title = "Session Ended",
                Message = $"{session.Schedule.Subject.Name} session has ended. Attendance is now closed.",
                Type = "Info",
                Category = "Session",
                Metadata = new { SessionId = sessionId }
            };

            foreach (var studentId in studentIds)
            {
                await SendToUserAsync(studentId, notification);
            }

            _logger.LogInformation("Session ended notification sent to {Count} students", studentIds.Count());
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error notifying session ended for {SessionId}", sessionId);
        }
    }
}
