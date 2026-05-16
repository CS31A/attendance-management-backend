namespace attendance_monitoring.Services.Crud;

/// <summary>
/// Generic repository interface for CRUD operations. Abstracts EF Core DbContext access
/// behind a minimal interface that the generic CrudService depends on.
/// </summary>
/// <typeparam name="TEntity">The domain entity type.</typeparam>
public interface IGenericCrudRepository<TEntity> where TEntity : class
{
    /// <summary>
    /// Retrieves all entities (no tracking).
    /// </summary>
    Task<IEnumerable<TEntity>> GetAllAsync();

    /// <summary>
    /// Retrieves an entity by its ID (no tracking).
    /// </summary>
    Task<TEntity?> GetByIdAsync(Guid id);

    /// <summary>
    /// Retrieves an entity by its ID with change tracking enabled.
    /// </summary>
    Task<TEntity?> GetByIdTrackedAsync(Guid id);

    /// <summary>
    /// Adds a new entity to the context.
    /// </summary>
    Task<TEntity> CreateAsync(TEntity entity);

    /// <summary>
    /// Marks an entity as modified in the context.
    /// </summary>
    void Update(TEntity entity);

    /// <summary>
    /// Removes an entity from the context.
    /// </summary>
    void Delete(TEntity entity);

    /// <summary>
    /// Saves all changes to the database.
    /// </summary>
    Task<int> SaveChangesAsync();
}
