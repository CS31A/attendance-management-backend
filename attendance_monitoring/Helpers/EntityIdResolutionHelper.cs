using attendance_monitoring.Exceptions;

namespace attendance_monitoring.Helpers;

public static class EntityIdResolutionHelper
{
    public static async Task<int> ResolveEntityIdAsync(
        int? id,
        Guid? uuid,
        string entityName,
        Func<int, Task<int?>> getByIdAsync,
        Func<Guid, Task<int?>> getByUuidAsync)
    {
        var hasId = id.HasValue && id.Value > 0;
        var hasUuid = IsProvided(uuid);

        if (!hasId && !hasUuid)
        {
            throw new ValidationException($"{entityName} reference is required.");
        }

        int? idFromId = null;
        if (hasId)
        {
            idFromId = await getByIdAsync(id!.Value).ConfigureAwait(false);
            if (!idFromId.HasValue)
            {
                throw new EntityNotFoundException<int>(entityName, id.Value);
            }
        }

        int? idFromUuid = null;
        if (hasUuid)
        {
            idFromUuid = await getByUuidAsync(uuid!.Value).ConfigureAwait(false);
            if (!idFromUuid.HasValue)
            {
                throw new EntityNotFoundException<Guid>(entityName, uuid.Value);
            }
        }

        if (idFromId.HasValue && idFromUuid.HasValue && idFromId.Value != idFromUuid.Value)
        {
            throw new ValidationException($"Conflicting {entityName} identifiers were provided.");
        }

        return idFromId ?? idFromUuid!.Value;
    }

    private static bool IsProvided(Guid? uuid) => uuid.HasValue && uuid.Value != Guid.Empty;
}
