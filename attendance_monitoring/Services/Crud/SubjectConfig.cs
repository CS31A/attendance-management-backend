using attendance_monitoring.Classes;
using attendance_monitoring.IRepository;
using attendance_monitoring.Models.DTO.Request;

namespace attendance_monitoring.Services.Crud;

/// <summary>
/// CRUD configuration for the Subject entity.
/// </summary>
public static class SubjectConfig
{
    public static CrudServiceConfig<Subject, CreateSubject, UpdateSubject> Create(
        ISubjectRepository subjectRepository)
    {
        return new CrudServiceConfig<Subject, CreateSubject, UpdateSubject>
        {
            EntityName = "Subject",

            CreateUniquenessChecks =
            [
                new UniquenessCheck<CreateSubject>
                {
                    FieldName = "Code",
                    ValueSelector = dto => dto.Code,
                    ExistsAsync = async code =>
                        await subjectRepository.GetSubjectByCodeAsync(code).ConfigureAwait(false) != null
                }
            ],

            UpdateUniquenessChecks =
            [
                new UpdateUniquenessCheck<Subject, UpdateSubject>
                {
                    FieldName = "Code",
                    ValueSelector = dto => dto.Code,
                    CurrentValueSelector = entity => entity.Code,
                    FindByUniqueFieldAsync = async code =>
                        await subjectRepository.GetSubjectByCodeAsync(code).ConfigureAwait(false)
                }
            ],

            MapToEntity = dto => new Subject
            {
                Id = Guid.NewGuid(),
                Name = dto.Name,
                Code = dto.Code,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            },

            ApplyUpdate = (dto, entity) =>
            {
                if (!string.IsNullOrEmpty(dto.Name))
                    entity.Name = dto.Name;
                if (!string.IsNullOrEmpty(dto.Code))
                    entity.Code = dto.Code;
                entity.UpdatedAt = DateTime.UtcNow;
            },

            ResolveUniqueConstraintViolation = _ => ("Code", ""),

            ResolveForeignKeyViolation = ex =>
            {
                var message = ex.InnerException?.Message ?? ex.Message;
                if (message.Contains("FK_StudentEnrollments", StringComparison.OrdinalIgnoreCase)
                    || message.Contains("StudentEnrollments", StringComparison.OrdinalIgnoreCase))
                    return ("enrollments", "Cannot delete: Subject has student enrollments. Remove enrollments first.");
                if (message.Contains("FK_Schedules_Subjects", StringComparison.OrdinalIgnoreCase)
                    || message.Contains("Schedules", StringComparison.OrdinalIgnoreCase))
                    return ("schedules", "Cannot delete: Subject has schedules assigned. Remove schedules first.");
                return null;
            }
        };
    }
}
