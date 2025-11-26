using System.Collections.Concurrent;

namespace attendance_monitoring.Services;

public interface INotificationPreferenceService
{
    Task SetRealtimeCheckInAsync(string instructorId, bool enabled);
    Task<bool> GetRealtimeCheckInAsync(string instructorId);
}

public class InMemoryPreferenceService : INotificationPreferenceService
{
    private readonly ConcurrentDictionary<string, bool> _preferences = new();

    public Task SetRealtimeCheckInAsync(string instructorId, bool enabled)
    {
        _preferences[instructorId] = enabled;
        return Task.CompletedTask;
    }

    public Task<bool> GetRealtimeCheckInAsync(string instructorId)
    {
        // Default OFF to prevent spam
        return Task.FromResult(_preferences.GetValueOrDefault(instructorId, false));
    }
}
