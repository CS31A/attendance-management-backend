using attendance_monitoring.Classes;
using attendance_monitoring.IRepository;
using attendance_monitoring.Models.DTO.Request;

namespace attendance_monitoring.Services.Crud;

/// <summary>
/// CRUD configuration for the Course entity.
/// </summary>
public static class CourseConfig
{
    public static CrudServiceConfig<Course, CreateCourse, UpdateCourse> Create(
        ICourseRepository courseRepository)
    {
        return new CrudServiceConfig<Course, CreateCourse, UpdateCourse>
        {
            EntityName = "Course",

            CreateUniquenessChecks =
            [
                new UniquenessCheck<CreateCourse>
                {
                    FieldName = "Name",
                    ValueSelector = dto => dto.Name,
                    ExistsAsync = async name =>
                        await courseRepository.GetCourseByNameAsync(name).ConfigureAwait(false) != null
                }
            ],

            UpdateUniquenessChecks =
            [
                new UpdateUniquenessCheck<Course, UpdateCourse>
                {
                    FieldName = "Name",
                    ValueSelector = dto => dto.Name,
                    CurrentValueSelector = entity => entity.Name,
                    FindByUniqueFieldAsync = async name =>
                        await courseRepository.GetCourseByNameAsync(name).ConfigureAwait(false)
                }
            ],

            MapToEntity = dto => new Course
            {
                Id = Guid.NewGuid(),
                Name = dto.Name,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            },

            ApplyUpdate = (dto, entity) =>
            {
                if (!string.IsNullOrEmpty(dto.Name))
                    entity.Name = dto.Name;
                entity.UpdatedAt = DateTime.UtcNow;
            },

            ResolveUniqueConstraintViolation = _ => ("Name", ""),

            ResolveForeignKeyViolation = ex =>
            {
                var message = ex.InnerException?.Message ?? ex.Message;
                if (message.Contains("FK_Sections_Courses", StringComparison.OrdinalIgnoreCase)
                    || message.Contains("Sections", StringComparison.OrdinalIgnoreCase))
                    return ("sections", "Cannot delete: Course has sections assigned. Remove sections first.");
                return null;
            }
        };
    }
}
