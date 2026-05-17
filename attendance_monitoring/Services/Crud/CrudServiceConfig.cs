using Microsoft.EntityFrameworkCore;

namespace attendance_monitoring.Services.Crud;

/// <summary>
/// Defines a uniqueness check for a field during entity creation or update.
/// </summary>
/// <typeparam name="TCreate">The create request DTO type.</typeparam>
public class UniquenessCheck<TCreate>
{
    /// <summary>
    /// The field name used in error messages (e.g., "Name", "Code").
    /// </summary>
    public required string FieldName { get; init; }

    /// <summary>
    /// Extracts the field value from the create DTO.
    /// </summary>
    public required Func<TCreate, string?> ValueSelector { get; init; }

    /// <summary>
    /// Checks if an entity with the given field value already exists.
    /// Returns the existing entity if found; otherwise, null.
    /// </summary>
    public required Func<string, Task<bool>> ExistsAsync { get; init; }
}

/// <summary>
/// Configuration for a generic CRUD module. Defines entity-specific behavior
/// (uniqueness checks, entity mapping, FK conflict resolution) while the
/// generic module handles the common try-catch-log-rethrow pattern.
/// </summary>
/// <typeparam name="TEntity">The domain entity type (e.g., Classroom, Subject).</typeparam>
/// <typeparam name="TCreate">The create request DTO type.</typeparam>
/// <typeparam name="TUpdate">The update request DTO type.</typeparam>
public class CrudServiceConfig<TEntity, TCreate, TUpdate>
{
    /// <summary>
    /// Entity name used in log messages and exception messages (e.g., "Classroom").
    /// </summary>
    public required string EntityName { get; init; }

    /// <summary>
    /// Uniqueness checks to run before create. Each check verifies a field is unique.
    /// </summary>
    public required IReadOnlyList<UniquenessCheck<TCreate>> CreateUniquenessChecks { get; init; }

    /// <summary>
    /// Uniqueness checks to run before update. The TUpdate delegate receives both
    /// the update DTO and the existing entity to determine the field value.
    /// </summary>
    public IReadOnlyList<UpdateUniquenessCheck<TEntity, TUpdate>> UpdateUniquenessChecks { get; init; } = [];

    /// <summary>
    /// Maps a create DTO to a new entity. Sets all fields including CreatedAt/UpdatedAt.
    /// </summary>
    public required Func<TCreate, TEntity> MapToEntity { get; init; }

    /// <summary>
    /// Applies update DTO fields to an existing entity. Only sets non-null fields.
    /// Should also update UpdatedAt.
    /// </summary>
    public required Action<TUpdate, TEntity> ApplyUpdate { get; init; }

    /// <summary>
    /// Resolves a DbUpdateException (typically from unique constraint violation during save)
    /// into a field name and value for the EntityAlreadyExistsException message.
    /// Returns null if the exception is not a recognized constraint violation.
    /// </summary>
    public Func<DbUpdateException, (string FieldName, string FieldValue)?>? ResolveUniqueConstraintViolation { get; init; }

    /// <summary>
    /// Resolves a DbUpdateException (typically from FK constraint violation on delete)
    /// into a conflict type and user-friendly message.
    /// Returns null if the exception is not a recognized FK violation.
    /// </summary>
    public Func<DbUpdateException, (string ConflictType, string Message)?>? ResolveForeignKeyViolation { get; init; }
}

/// <summary>
/// Defines a uniqueness check for a field during entity update.
/// </summary>
public class UpdateUniquenessCheck<TEntity, TUpdate>
{
    /// <summary>
    /// The field name used in error messages (e.g., "Name", "Code").
    /// </summary>
    public required string FieldName { get; init; }

    /// <summary>
    /// Extracts the field value from the update DTO. Returns null if the field is not being updated.
    /// </summary>
    public required Func<TUpdate, string?> ValueSelector { get; init; }

    /// <summary>
    /// Extracts the current field value from the existing entity for comparison.
    /// </summary>
    public required Func<TEntity, string> CurrentValueSelector { get; init; }

    /// <summary>
    /// Finds an entity by the given field value. Returns the entity if found; otherwise, null.
    /// Used during update to check for duplicates while excluding the current entity by ID.
    /// </summary>
    public required Func<string, Task<TEntity?>> FindByUniqueFieldAsync { get; init; }
}
