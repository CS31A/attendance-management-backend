using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;
using System.Security.Claims;
using attendance_monitoring.Constants;
using attendance_monitoring.Services;
using attendance_monitoring.IRepository;

namespace attendance_monitoring.Hubs;

[Authorize]
public class NotificationHub : Hub
{
    private readonly IUserConnectionManager _connectionManager;
    private readonly ISessionRepository _sessionRepository;
    private readonly IStudentEnrollmentRepository _enrollmentRepository;
    private readonly IInstructorRepository _instructorRepository;
    private readonly IStudentRepository _studentRepository;
    private readonly ILogger<NotificationHub> _logger;

    public NotificationHub(
        IUserConnectionManager connectionManager,
        ISessionRepository sessionRepository,
        IStudentEnrollmentRepository enrollmentRepository,
        IInstructorRepository instructorRepository,
        IStudentRepository studentRepository,
        ILogger<NotificationHub> logger)
    {
        _connectionManager = connectionManager;
        _sessionRepository = sessionRepository;
        _enrollmentRepository = enrollmentRepository;
        _instructorRepository = instructorRepository;
        _studentRepository = studentRepository;
        _logger = logger;
    }

    public override async Task OnConnectedAsync()
    {
        var userId = Context.User?.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (userId != null)
        {
            await _connectionManager.AddConnectionAsync(userId, Context.ConnectionId);

            // Auto-join role-based groups
            var roles = Context.User?.FindAll(ClaimTypes.Role).Select(r => r.Value);
            if (roles != null)
            {
                foreach (var role in roles)
                {
                    await Groups.AddToGroupAsync(Context.ConnectionId, $"role:{role}");
                }
            }

            _logger.LogInformation("User {UserId} connected with {ConnectionId}", userId, Context.ConnectionId);
        }

        await base.OnConnectedAsync();
    }

    public override async Task OnDisconnectedAsync(Exception? exception)
    {
        var userId = Context.User?.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (userId != null)
        {
            await _connectionManager.RemoveConnectionAsync(userId, Context.ConnectionId);
            _logger.LogInformation("User {UserId} disconnected", userId);
        }

        await base.OnDisconnectedAsync(exception);
    }

    public async Task JoinSessionGroup(Guid sessionId)
    {
        var userId = Context.User?.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (string.IsNullOrEmpty(userId))
        {
            throw new HubException("Unauthorized");
        }

        if (await IsUserAuthorizedForSession(userId, sessionId))
        {
            await Groups.AddToGroupAsync(Context.ConnectionId, $"session:{sessionId}");
            _logger.LogInformation("ConnectionId {ConnectionId} (User {UserId}) joined session:{SessionId}",
                Context.ConnectionId, userId, sessionId);
        }
        else
        {
            _logger.LogWarning("User {UserId} attempted to join session:{SessionId} without authorization", userId, sessionId);
            throw new HubException("You are not authorized to join this session group.");
        }
    }

    public async Task LeaveSessionGroup(Guid sessionId)
    {
        await Groups.RemoveFromGroupAsync(Context.ConnectionId, $"session:{sessionId}");
    }

    private async Task<bool> IsUserAuthorizedForSession(string userId, Guid sessionId)
    {
        try
        {
            // 1. Get Session
            var session = await _sessionRepository.GetSessionByIdAsync(sessionId);
            if (session == null || session.Schedule == null) return false;

            // 2. Check Roles
            var user = Context.User;
            if (user == null) return false;

            if (user.IsInRole(RoleConstants.Admin)) return true;

            if (user.IsInRole(RoleConstants.Instructor))
            {
                var instructor = await _instructorRepository.GetInstructorByUserIdAsync(userId);
                return instructor != null && session.Schedule.InstructorId == instructor.Id;
            }

            if (user.IsInRole("Student"))
            {
                var student = await _studentRepository.GetStudentByUserIdAsync(userId);
                if (student == null) return false;

                return await _enrollmentRepository.IsStudentEnrolledAsync(
                    student.Id,
                    session.Schedule.SectionId,
                    session.Schedule.SubjectId);
            }

            return false;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error checking authorization for session {SessionId} and user {UserId}", sessionId, userId);
            return false;
        }
    }
}
