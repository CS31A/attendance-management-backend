namespace attendance_monitoring.Services.Crud;

/// <summary>
/// Generic CRUD service interface. Provides standard create, read, update, and delete
/// operations for any entity type. Entity-specific behavior (uniqueness checks, FK conflict
/// messages, entity mapping) is configured via <see cref="CrudServiceConfig{TEntity,TCreate,TUpdate}"/>.
/// </summary>
/// <typeparam name="TEntity">The domain entity type.</typeparam>
/// <typeparam name="TCreate">The create request DTO type.</typeparam>
/// <typeparam name="TUpdate">The update request DTO type.</typeparam>
public interface ICrudService<TEntity, in TCreate, in TUpdate>
{
    /// <summary>
    /// Retrieves all entities.
    /// </summary>
    Task<IEnumerable<TEntity>> GetAllAsync();

    /// <summary>
    /// Retrieves an entity by its ID.
    /// </summary>
    /// <param name="id">The entity ID.</param>
    /// <returns>The entity if found.</returns>
    /// <exception cref="Exceptions.EntityNotFoundException{Guid}">Thrown when the entity is not found.</exception>
    Task<TEntity> GetByIdAsync(Guid id);

    /// <summary>
    /// Creates a new entity after validating uniqueness constraints.
    /// </summary>
    /// <param name="createDto">The create request DTO.</param>
    /// <returns>The created entity.</returns>
    /// <exception cref="Exceptions.ValidationException">Thrown when a required field is missing.</exception>
    /// <exception cref="Exceptions.EntityAlreadyExistsException{string}">Thrown when a unique field value already exists.</exception>
    /// <exception cref="Exceptions.EntityServiceException">Thrown when an unexpected error occurs.</exception>
    Task<TEntity> CreateAsync(TCreate createDto);

    /// <summary>
    /// Updates an existing entity after validating uniqueness constraints.
    /// </summary>
    /// <param name="id">The entity ID.</param>
    /// <param name="updateDto">The update request DTO.</param>
    /// <returns>The updated entity.</returns>
    /// <exception cref="Exceptions.EntityNotFoundException{Guid}">Thrown when the entity is not found.</exception>
    /// <exception cref="Exceptions.EntityAlreadyExistsException{string}">Thrown when a unique field value already exists.</exception>
    /// <exception cref="Exceptions.EntityServiceException">Thrown when an unexpected error occurs.</exception>
    Task<TEntity> UpdateAsync(Guid id, TUpdate updateDto);

    /// <summary>
    /// Deletes an entity by its ID.
    /// </summary>
    /// <param name="id">The entity ID.</param>
    /// <exception cref="Exceptions.EntityNotFoundException{Guid}">Thrown when the entity is not found.</exception>
    /// <exception cref="Exceptions.EntityConflictException">Thrown when FK constraints prevent deletion.</exception>
    /// <exception cref="Exceptions.EntityServiceException">Thrown when an unexpected error occurs.</exception>
    Task DeleteAsync(Guid id);
}
