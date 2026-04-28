using System.Collections.Concurrent;

namespace attendance_monitoring.Services;

public interface IUserConnectionManager
{
    Task AddConnectionAsync(string userId, string connectionId);
    Task RemoveConnectionAsync(string userId, string connectionId);
    Task<IEnumerable<string>> GetConnectionsAsync(string userId);
    Task<bool> IsOnlineAsync(string userId);
}

public class UserConnectionManager : IUserConnectionManager, IDisposable
{
    private readonly ConcurrentDictionary<string, HashSet<string>> _connections = new();
    private readonly SemaphoreSlim _semaphore = new(1, 1);
    private bool _disposed;

    public async Task AddConnectionAsync(string userId, string connectionId)
    {
        await _semaphore.WaitAsync();
        try
        {
            if (!_connections.ContainsKey(userId))
            {
                _connections[userId] = new HashSet<string>();
            }
            _connections[userId].Add(connectionId);
        }
        finally
        {
            _semaphore.Release();
        }
    }

    public async Task RemoveConnectionAsync(string userId, string connectionId)
    {
        await _semaphore.WaitAsync();
        try
        {
            if (_connections.ContainsKey(userId))
            {
                _connections[userId].Remove(connectionId);
                if (_connections[userId].Count == 0)
                {
                    _connections.TryRemove(userId, out _);
                }
            }
        }
        finally
        {
            _semaphore.Release();
        }
    }

    public Task<IEnumerable<string>> GetConnectionsAsync(string userId)
    {
        if (_connections.TryGetValue(userId, out var connections))
        {
            return Task.FromResult<IEnumerable<string>>(connections.ToList());
        }
        return Task.FromResult(Enumerable.Empty<string>());
    }

    public Task<bool> IsOnlineAsync(string userId)
    {
        return Task.FromResult(_connections.ContainsKey(userId) && _connections[userId].Count > 0);
    }

    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }

    protected virtual void Dispose(bool disposing)
    {
        if (!_disposed)
        {
            if (disposing)
            {
                _semaphore?.Dispose();
            }
            _disposed = true;
        }
    }
}
