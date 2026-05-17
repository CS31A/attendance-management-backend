using attendance_monitoring.Classes;

namespace attendance_monitoring.Services.Crud;

/// <summary>
/// CRUD configuration for the Section entity.
/// Uses Section as both TCreate and TUpdate since the controller maps the DTO to an entity
/// before calling the service.
/// </summary>
public static class SectionConfig
{
    public static CrudServiceConfig<Section, Section, Section> Create()
    {
        return new CrudServiceConfig<Section, Section, Section>
        {
            EntityName = "Section",

            CreateUniquenessChecks = [],

            UpdateUniquenessChecks = [],

            MapToEntity = section => new Section
            {
                Id = section.Id == Guid.Empty ? Guid.NewGuid() : section.Id,
                Name = section.Name,
                CourseId = section.CourseId,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            },

            ApplyUpdate = (newData, existing) =>
            {
                if (!string.IsNullOrEmpty(newData.Name))
                    existing.Name = newData.Name;
                if (newData.CourseId != Guid.Empty)
                    existing.CourseId = newData.CourseId;
                existing.UpdatedAt = DateTime.UtcNow;
            },

            ResolveUniqueConstraintViolation = null,

            ResolveForeignKeyViolation = ex =>
            {
                var message = ex.InnerException?.Message ?? ex.Message;
                if (message.Contains("schedule", StringComparison.OrdinalIgnoreCase))
                    return ("schedules", "Cannot delete: Section has schedules assigned. Remove schedules first.");
                if (message.Contains("enrollment", StringComparison.OrdinalIgnoreCase))
                    return ("enrollments", "Cannot delete: Section has student enrollments. Remove enrollments first.");
                if (message.Contains("student", StringComparison.OrdinalIgnoreCase))
                    return ("students", "Cannot delete: Section has students assigned. Remove students first.");
                return null;
            }
        };
    }
}
