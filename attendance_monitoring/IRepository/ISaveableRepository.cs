namespace attendance_monitoring.IRepository;

/// <summary>
/// Interface for repositories that require save capabilities for persisting changes to the database.
/// </summary>
public interface ISaveableRepository : IBaseRepository
{
    /// <summary>
    /// Saves changes to the database.
    /// </summary>
    /// <returns>The number of state entries written to the database.</returns>
    Task<int> SaveChangesAsync();
}