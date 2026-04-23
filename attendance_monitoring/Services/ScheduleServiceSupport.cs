using attendance_monitoring.Classes;
using attendance_monitoring.Data;
using attendance_monitoring.Exceptions;
using attendance_monitoring.Helpers;
using attendance_monitoring.Models.DTO.Response;
using Microsoft.EntityFrameworkCore;

namespace attendance_monitoring.Services;

internal static class ScheduleServiceSupport
{
    public static Task<int> ResolveSubjectIdAsync(ApplicationDbContext context, int? subjectId, Guid? subjectUuid)
        => EntityIdResolutionHelper.ResolveEntityIdAsync(
            subjectId,
            subjectUuid,
            "Subject",
            async id => await context.Subjects.AsNoTracking().Where(s => s.Id == id).Select(s => (int?)s.Id).SingleOrDefaultAsync().ConfigureAwait(false),
            async uuid => await context.Subjects.AsNoTracking().Where(s => s.Uuid == uuid).Select(s => (int?)s.Id).SingleOrDefaultAsync().ConfigureAwait(false));

    public static Task<int> ResolveClassroomIdAsync(ApplicationDbContext context, int? classroomId, Guid? classroomUuid)
        => EntityIdResolutionHelper.ResolveEntityIdAsync(
            classroomId,
            classroomUuid,
            "Classroom",
            async id => await context.Classrooms.AsNoTracking().Where(c => c.Id == id).Select(c => (int?)c.Id).SingleOrDefaultAsync().ConfigureAwait(false),
            async uuid => await context.Classrooms.AsNoTracking().Where(c => c.Uuid == uuid).Select(c => (int?)c.Id).SingleOrDefaultAsync().ConfigureAwait(false));

    public static Task<int> ResolveSectionIdAsync(ApplicationDbContext context, int? sectionId, Guid? sectionUuid)
        => EntityIdResolutionHelper.ResolveEntityIdAsync(
            sectionId,
            sectionUuid,
            "Section",
            async id => await context.Sections.AsNoTracking().Where(s => s.Id == id).Select(s => (int?)s.Id).SingleOrDefaultAsync().ConfigureAwait(false),
            async uuid => await context.Sections.AsNoTracking().Where(s => s.Uuid == uuid).Select(s => (int?)s.Id).SingleOrDefaultAsync().ConfigureAwait(false));

    public static Task<int> ResolveInstructorIdAsync(ApplicationDbContext context, int? instructorId, Guid? instructorUuid)
        => EntityIdResolutionHelper.ResolveEntityIdAsync(
            instructorId,
            instructorUuid,
            "Instructor",
            async id => await context.Instructors.AsNoTracking().Where(i => i.Id == id).Select(i => (int?)i.Id).SingleOrDefaultAsync().ConfigureAwait(false),
            async uuid => await context.Instructors.AsNoTracking().Where(i => i.Uuid == uuid).Select(i => (int?)i.Id).SingleOrDefaultAsync().ConfigureAwait(false));

    public static ScheduleResponseDto MapToResponseDto(Schedules schedule)
    {
        return new ScheduleResponseDto
        {
            Id = schedule.Id,
            Uuid = schedule.Uuid,
            TimeIn = schedule.TimeIn,
            TimeOut = schedule.TimeOut,
            DayOfWeek = schedule.DayOfWeek,
            Subject = new SubjectResponseDto
            {
                Id = schedule.Subject.Id,
                Uuid = schedule.Subject.Uuid,
                Name = schedule.Subject.Name,
                Code = schedule.Subject.Code,
                CreatedAt = schedule.Subject.CreatedAt,
                UpdatedAt = schedule.Subject.UpdatedAt,
            },
            Classroom = new ClassroomResponseDto
            {
                Id = schedule.Classroom.Id,
                Uuid = schedule.Classroom.Uuid,
                Name = schedule.Classroom.Name,
                CreatedAt = schedule.Classroom.CreatedAt,
                UpdatedAt = schedule.Classroom.UpdatedAt,
            },
            Section = new SectionResponseDto
            {
                Id = schedule.Section.Id,
                Uuid = schedule.Section.Uuid,
                Name = schedule.Section.Name,
                CourseId = schedule.Section.CourseId,
                CourseUuid = schedule.Section.Course?.Uuid,
                CreatedAt = schedule.Section.CreatedAt,
                UpdatedAt = schedule.Section.UpdatedAt,
            },
            Instructor = new InstructorResponseDto
            {
                Id = schedule.Instructor.Id,
                Uuid = schedule.Instructor.Uuid,
                Firstname = schedule.Instructor.Firstname,
                Lastname = schedule.Instructor.Lastname,
                Email = schedule.Instructor.User?.Email,
            },
            CreatedAt = schedule.CreatedAt,
            UpdatedAt = schedule.UpdatedAt,
        };
    }
}
