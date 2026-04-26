using attendance_monitoring.Classes;
using attendance_monitoring.Data;
using attendance_monitoring.Exceptions;
using attendance_monitoring.Helpers;
using attendance_monitoring.Models.DTO.Response;
using Microsoft.EntityFrameworkCore;

namespace attendance_monitoring.Services;

internal static class ScheduleServiceSupport
{
    public static async Task<Guid> ResolveSubjectIdAsync(ApplicationDbContext context, Guid subjectId)
    {
        var exists = await context.Subjects.AsNoTracking().AnyAsync(s => s.Id == subjectId).ConfigureAwait(false);
        if (!exists)
        {
            throw new EntityNotFoundException<Guid>("Subject", subjectId);
        }

        return subjectId;
    }

    public static async Task<Guid> ResolveClassroomIdAsync(ApplicationDbContext context, Guid classroomId)
    {
        var exists = await context.Classrooms.AsNoTracking().AnyAsync(c => c.Id == classroomId).ConfigureAwait(false);
        if (!exists)
        {
            throw new EntityNotFoundException<Guid>("Classroom", classroomId);
        }

        return classroomId;
    }

    public static async Task<Guid> ResolveSectionIdAsync(ApplicationDbContext context, Guid sectionId)
    {
        var exists = await context.Sections.AsNoTracking().AnyAsync(s => s.Id == sectionId).ConfigureAwait(false);
        if (!exists)
        {
            throw new EntityNotFoundException<Guid>("Section", sectionId);
        }

        return sectionId;
    }

    public static async Task<Guid> ResolveInstructorIdAsync(ApplicationDbContext context, Guid instructorId)
    {
        var exists = await context.Instructors.AsNoTracking().AnyAsync(i => i.Id == instructorId).ConfigureAwait(false);
        if (!exists)
        {
            throw new EntityNotFoundException<Guid>("Instructor", instructorId);
        }

        return instructorId;
    }

    public static ScheduleResponseDto MapToResponseDto(Schedules schedule)
    {
        return new ScheduleResponseDto
        {
            Id = schedule.Id,
            TimeIn = schedule.TimeIn,
            TimeOut = schedule.TimeOut,
            DayOfWeek = schedule.DayOfWeek,
            Subject = new SubjectResponseDto
            {
                Id = schedule.Subject.Id,
                Name = schedule.Subject.Name,
                Code = schedule.Subject.Code,
                CreatedAt = schedule.Subject.CreatedAt,
                UpdatedAt = schedule.Subject.UpdatedAt,
            },
            Classroom = new ClassroomResponseDto
            {
                Id = schedule.Classroom.Id,
                Name = schedule.Classroom.Name,
                CreatedAt = schedule.Classroom.CreatedAt,
                UpdatedAt = schedule.Classroom.UpdatedAt,
            },
            Section = new SectionResponseDto
            {
                Id = schedule.Section.Id,
                Name = schedule.Section.Name,
                CourseId = schedule.Section.Course?.Id,
                CreatedAt = schedule.Section.CreatedAt,
                UpdatedAt = schedule.Section.UpdatedAt,
            },
            Instructor = new InstructorResponseDto
            {
                Id = schedule.Instructor.Id,
                Firstname = schedule.Instructor.Firstname ?? string.Empty,
                Lastname = schedule.Instructor.Lastname ?? string.Empty,
                Email = schedule.Instructor.User?.Email ?? string.Empty,
                CreatedAt = schedule.Instructor.CreatedAt,
                UpdatedAt = schedule.Instructor.UpdatedAt,
            },
            CreatedAt = schedule.CreatedAt,
            UpdatedAt = schedule.UpdatedAt,
        };
    }
}
