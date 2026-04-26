using attendance_monitoring.Exceptions;

namespace attendance_monitoring.Helpers;

public static class EntityIdResolutionHelper
{
    public static Guid RequireGuid(Guid? id, string entityName)
    {
        if (!IsProvided(id))
        {
            throw new ValidationException($"{entityName} reference is required.");
        }

        return id!.Value;
    }

    private static bool IsProvided(Guid? id) => id.HasValue && id.Value != Guid.Empty;
}
