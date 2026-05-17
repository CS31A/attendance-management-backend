using attendance_monitoring.Classes;
using attendance_monitoring.Helpers;
using attendance_monitoring.IRepository;
using attendance_monitoring.Models.DTO.Request;
using Microsoft.EntityFrameworkCore;

namespace attendance_monitoring.Services.Crud;

/// <summary>
/// CRUD configuration for the Classroom entity.
/// </summary>
public static class ClassroomConfig
{
    public static CrudServiceConfig<Classroom, CreateClassroom, UpdateClassroom> Create(
        IClassroomRepository classroomRepository)
    {
        return new CrudServiceConfig<Classroom, CreateClassroom, UpdateClassroom>
        {
            EntityName = "Classroom",

            CreateUniquenessChecks =
            [
                new UniquenessCheck<CreateClassroom>
                {
                    FieldName = "Name",
                    ValueSelector = dto => dto.Name,
                    ExistsAsync = async name =>
                        await classroomRepository.GetClassroomByNameAsync(name).ConfigureAwait(false) != null
                }
            ],

            UpdateUniquenessChecks =
            [
                new UpdateUniquenessCheck<Classroom, UpdateClassroom>
                {
                    FieldName = "Name",
                    ValueSelector = dto => dto.Name,
                    CurrentValueSelector = entity => entity.Name,
                    FindByUniqueFieldAsync = async name =>
                        await classroomRepository.GetClassroomByNameAsync(name).ConfigureAwait(false)
                }
            ],

            MapToEntity = dto => new Classroom
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
                if (message.Contains("FK_Sessions_Classrooms_ActualRoomId", StringComparison.OrdinalIgnoreCase)
                    || message.Contains("ActualRoomId", StringComparison.OrdinalIgnoreCase))
                    return ("sessions", "Cannot delete: Classroom has sessions assigned. Remove sessions first.");
                if (message.Contains("FK_Schedules_Classrooms", StringComparison.OrdinalIgnoreCase)
                    || message.Contains("Schedules", StringComparison.OrdinalIgnoreCase))
                    return ("schedules", "Cannot delete: Classroom has schedules assigned. Remove schedules first.");
                return null;
            }
        };
    }
}
