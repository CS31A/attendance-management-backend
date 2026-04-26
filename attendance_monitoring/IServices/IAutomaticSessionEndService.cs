using attendance_monitoring.Classes;

namespace attendance_monitoring.IServices;

public interface IAutomaticSessionEndService
{
    DateTime CalculateAutoEndBoundary(Session session);

    bool IsPastAutoEndBoundary(Session session);

    Task<Session> AutoEndIfExpiredAsync(Session session);

    Task<int> AutoEndExpiredSessionsAsync();
}
