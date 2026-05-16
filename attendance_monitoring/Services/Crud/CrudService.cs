using attendance_monitoring.Exceptions;
using attendance_monitoring.Helpers;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using ValidationException = attendance_monitoring.Exceptions.ValidationException;

namespace attendance_monitoring.Services.Crud;

/// <summary>
/// Generic CRUD service that implements the common try-catch-log-rethrow pattern
/// for entity create, read, update, and delete operations. Entity-specific behavior
/// (uniqueness checks, entity mapping, FK conflict resolution) is driven by
/// <see cref="CrudServiceConfig{TEntity,TCreate,TUpdate}"/>.
/// </summary>
public class CrudService<TEntity, TCreate, TUpdate> : ICrudService<TEntity, TCreate, TUpdate>
    where TEntity : class
{
    private readonly IGenericCrudRepository<TEntity> _repository;
    private readonly CrudServiceConfig<TEntity, TCreate, TUpdate> _config;
    private readonly ILogger<CrudService<TEntity, TCreate, TUpdate>> _logger;

    public CrudService(
        IGenericCrudRepository<TEntity> repository,
        CrudServiceConfig<TEntity, TCreate, TUpdate> config,
        ILogger<CrudService<TEntity, TCreate, TUpdate>> logger)
    {
        _repository = repository;
        _config = config;
        _logger = logger;
    }

    #region GetAllAsync

    public async Task<IEnumerable<TEntity>> GetAllAsync()
    {
        _logger.LogInformation("Retrieving all {EntityName}s", _config.EntityName);
        try
        {
            var entities = await _repository.GetAllAsync().ConfigureAwait(false);
            var list = entities.ToList();
            _logger.LogInformation("Successfully retrieved {Count} {EntityName}s", list.Count, _config.EntityName);
            return list;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error occurred while retrieving all {EntityName}s", _config.EntityName);
            throw new EntityServiceException(_config.EntityName, $"GetAll{_config.EntityName}",
                $"An error occurred while retrieving {_config.EntityName.ToLowerInvariant()}s", ex);
        }
    }

    #endregion

    #region GetByIdAsync

    public async Task<TEntity> GetByIdAsync(Guid id)
    {
        _logger.LogInformation("Retrieving {EntityName} by ID: {Id}", _config.EntityName, id);
        try
        {
            var entity = await _repository.GetByIdAsync(id).ConfigureAwait(false);
            if (entity == null)
            {
                _logger.LogWarning("{EntityName} with ID {Id} not found", _config.EntityName, id);
                throw new EntityNotFoundException<Guid>(_config.EntityName, id);
            }

            _logger.LogInformation("Successfully retrieved {EntityName} with ID: {Id}", _config.EntityName, id);
            return entity;
        }
        catch (EntityNotFoundException<Guid>)
        {
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error occurred while retrieving {EntityName} with ID {Id}", _config.EntityName, id);
            throw new EntityServiceException(_config.EntityName, $"Get{_config.EntityName}ById: {id}",
                $"An error occurred while retrieving the {_config.EntityName.ToLowerInvariant()}", ex);
        }
    }

    #endregion

    #region CreateAsync

    public async Task<TEntity> CreateAsync(TCreate createDto)
    {
        _logger.LogInformation("Creating new {EntityName}", _config.EntityName);
        try
        {
            // Run uniqueness checks
            foreach (var check in _config.CreateUniquenessChecks)
            {
                var value = check.ValueSelector(createDto);
                if (string.IsNullOrWhiteSpace(value))
                {
                    _logger.LogWarning("{EntityName} creation failed: {FieldName} is required", _config.EntityName, check.FieldName);
                    throw new ValidationException($"{check.FieldName} is required");
                }

                if (await check.ExistsAsync(value).ConfigureAwait(false))
                {
                    _logger.LogWarning("{EntityName} creation failed: {FieldName} {Value} already exists",
                        _config.EntityName, check.FieldName, value);
                    throw new EntityAlreadyExistsException<string>(_config.EntityName, check.FieldName, value);
                }
            }

            // Map DTO to entity
            var entity = _config.MapToEntity(createDto);

            // Persist
            var created = await _repository.CreateAsync(entity).ConfigureAwait(false);
            await _repository.SaveChangesAsync().ConfigureAwait(false);

            _logger.LogInformation("Successfully created {EntityName} with ID: {Id}", _config.EntityName,
                GetEntityId(created));
            return created;
        }
        catch (ValidationException)
        {
            throw;
        }
        catch (EntityAlreadyExistsException<string>)
        {
            throw;
        }
        catch (DbUpdateException ex) when (ExceptionHandlingHelper.IsUniqueConstraintViolation(ex))
        {
            var resolved = _config.ResolveUniqueConstraintViolation?.Invoke(ex);
            var fieldName = resolved?.FieldName ?? "field";
            var fieldValue = resolved?.FieldValue ?? "value";
            _logger.LogWarning(ex, "{EntityName} creation failed due to unique constraint violation: {FieldName} {FieldValue} already exists",
                _config.EntityName, fieldName, fieldValue);
            throw new EntityAlreadyExistsException<string>(_config.EntityName, fieldName, fieldValue);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error occurred while creating {EntityName}", _config.EntityName);
            throw ExceptionHandlingHelper.CreateServiceException(_config.EntityName, $"Create{_config.EntityName}", ex);
        }
    }

    #endregion

    #region UpdateAsync

    public async Task<TEntity> UpdateAsync(Guid id, TUpdate updateDto)
    {
        _logger.LogInformation("Updating {EntityName} with ID: {Id}", _config.EntityName, id);
        try
        {
            // Load tracked entity
            var existing = await _repository.GetByIdTrackedAsync(id).ConfigureAwait(false);
            if (existing == null)
            {
                _logger.LogWarning("{EntityName} update failed: {EntityName} with ID {Id} not found", _config.EntityName, id);
                throw new EntityNotFoundException<Guid>(_config.EntityName, id);
            }

            // Run update uniqueness checks
            var entityId = GetEntityId(existing);
            foreach (var check in _config.UpdateUniquenessChecks)
            {
                var newValue = check.ValueSelector(updateDto);
                if (string.IsNullOrEmpty(newValue))
                    continue;

                var currentValue = check.CurrentValueSelector(existing);
                if (newValue.Equals(currentValue, StringComparison.Ordinal))
                    continue;

                var duplicate = await check.FindByUniqueFieldAsync(newValue).ConfigureAwait(false);
                if (duplicate != null && !GetEntityId(duplicate).Equals(entityId))
                {
                    _logger.LogWarning("{EntityName} update failed: {FieldName} {Value} already exists",
                        _config.EntityName, check.FieldName, newValue);
                    throw new EntityAlreadyExistsException<string>(_config.EntityName, check.FieldName, newValue);
                }
            }

            // Apply update
            _config.ApplyUpdate(updateDto, existing);

            // Persist
            _repository.Update(existing);
            var rowsAffected = await _repository.SaveChangesAsync().ConfigureAwait(false);

            if (rowsAffected == 0)
            {
                _logger.LogWarning("{EntityName} update failed: {EntityName} with ID {Id} may have been updated by another process",
                    _config.EntityName, id);
                throw new EntityServiceException(_config.EntityName, $"Update{_config.EntityName}: {id}",
                    $"{_config.EntityName} may have been updated by another process. Please try again.");
            }

            _logger.LogInformation("Successfully updated {EntityName} with ID: {Id}", _config.EntityName, id);
            return existing;
        }
        catch (EntityNotFoundException<Guid>)
        {
            throw;
        }
        catch (EntityAlreadyExistsException<string>)
        {
            throw;
        }
        catch (EntityServiceException)
        {
            throw;
        }
        catch (DbUpdateException ex) when (ExceptionHandlingHelper.IsUniqueConstraintViolation(ex))
        {
            var resolved = _config.ResolveUniqueConstraintViolation?.Invoke(ex);
            var fieldName = resolved?.FieldName ?? "field";
            var fieldValue = resolved?.FieldValue ?? "value";
            _logger.LogWarning(ex, "{EntityName} update failed due to unique constraint violation: {FieldName} {FieldValue} already exists",
                _config.EntityName, fieldName, fieldValue);
            throw new EntityAlreadyExistsException<string>(_config.EntityName, fieldName, fieldValue);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error occurred while updating {EntityName} with ID {Id}", _config.EntityName, id);
            throw ExceptionHandlingHelper.CreateServiceException(_config.EntityName, $"Update{_config.EntityName}: {id}", ex);
        }
    }

    #endregion

    #region DeleteAsync

    public async Task DeleteAsync(Guid id)
    {
        _logger.LogInformation("Deleting {EntityName} with ID: {Id}", _config.EntityName, id);
        try
        {
            var existing = await _repository.GetByIdAsync(id).ConfigureAwait(false);
            if (existing == null)
            {
                _logger.LogWarning("{EntityName} deletion failed: {EntityName} with ID {Id} not found", _config.EntityName, id);
                throw new EntityNotFoundException<Guid>(_config.EntityName, id);
            }

            _repository.Delete(existing);
            var rowsAffected = await _repository.SaveChangesAsync().ConfigureAwait(false);

            if (rowsAffected == 0)
            {
                _logger.LogWarning("{EntityName} deletion failed: {EntityType} with ID {Id} may have been deleted by another process",
                    _config.EntityName, _config.EntityName, id);
                throw new EntityServiceException(_config.EntityName, $"Delete{_config.EntityName}: {id}",
                    $"{_config.EntityName} may have been deleted by another process.");
            }

            _logger.LogInformation("Successfully deleted {EntityName} with ID: {Id}", _config.EntityName, id);
        }
        catch (EntityNotFoundException<Guid>)
        {
            throw;
        }
        catch (EntityServiceException)
        {
            throw;
        }
        catch (DbUpdateException ex) when (ExceptionHandlingHelper.IsForeignKeyViolation(ex))
        {
            var resolved = _config.ResolveForeignKeyViolation?.Invoke(ex);
            var conflictType = resolved?.ConflictType ?? "dependencies";
            var conflictMessage = resolved?.Message ?? $"Cannot delete: {_config.EntityName} has dependencies that prevent deletion.";
            _logger.LogWarning(ex, "{EntityName} deletion failed due to foreign key constraint: {Message}", _config.EntityName, conflictMessage);
            throw new EntityConflictException(_config.EntityName, conflictType, conflictMessage, ex);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error occurred while deleting {EntityName} with ID {Id}", _config.EntityName, id);
            throw ExceptionHandlingHelper.CreateServiceException(_config.EntityName, $"Delete{_config.EntityName}: {id}", ex);
        }
    }

    #endregion

    #region Helpers

    private static object GetEntityId(TEntity entity)
    {
        var prop = typeof(TEntity).GetProperty("Id");
        return prop?.GetValue(entity) ?? "(unknown)";
    }

    #endregion
}
