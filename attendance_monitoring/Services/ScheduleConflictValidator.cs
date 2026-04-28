using attendance_monitoring.Classes;
using attendance_monitoring.Exceptions;
using attendance_monitoring.Helpers;
using attendance_monitoring.IRepository;
using Microsoft.EntityFrameworkCore;

namespace attendance_monitoring.Services;

internal static class ScheduleConflictValidator
{
    public static bool IsScheduleDuplicateConstraintViolation(DbUpdateException ex)
    {
        return ExceptionHandlingHelper.IsUniqueConstraintViolation(
            ex,
            "IX_Schedules_ClassroomId_DayOfWeek_TimeIn_TimeOut",
            "ClassroomId",
            "DayOfWeek",
            "TimeIn",
            "TimeOut");
    }

    public static EntityConflictException CreateDuplicateScheduleConflict(DbUpdateException ex)
    {
        return new EntityConflictException(
            "Schedule",
            "duplicate",
            "Schedule conflict: classroom is already booked for that day and time range.",
            ex);
    }

    public static async Task ValidateScheduleDoesNotOverlapAsync(
        IScheduleRepository scheduleRepository,
        Guid classroomId,
        Guid instructorId,
        Guid sectionId,
        string dayOfWeek,
        TimeOnly timeIn,
        TimeOnly timeOut,
        Guid? excludedScheduleId = null)
    {
        var classroomConflict = await scheduleRepository.FindClassroomOverlapAsync(
            classroomId,
            dayOfWeek,
            timeIn,
            timeOut,
            excludedScheduleId).ConfigureAwait(false);
        ThrowIfConflict("classroom", classroomConflict);

        var instructorConflict = await scheduleRepository.FindInstructorOverlapAsync(
            instructorId,
            dayOfWeek,
            timeIn,
            timeOut,
            excludedScheduleId).ConfigureAwait(false);
        ThrowIfConflict("instructor", instructorConflict);

        var sectionConflict = await scheduleRepository.FindSectionOverlapAsync(
            sectionId,
            dayOfWeek,
            timeIn,
            timeOut,
            excludedScheduleId).ConfigureAwait(false);
        ThrowIfConflict("section", sectionConflict);
    }

    private static void ThrowIfConflict(string resourceType, ScheduleConflictDetails? conflict)
    {
        if (conflict == null)
        {
            return;
        }

        throw new EntityConflictException(
            "Schedule",
            resourceType,
            BuildScheduleConflictMessage(resourceType, conflict));
    }

    private static string BuildScheduleConflictMessage(string resourceType, ScheduleConflictDetails conflict)
    {
        var resourceName = resourceType switch
        {
            "classroom" => conflict.ClassroomName,
            "instructor" => conflict.InstructorName,
            "section" => conflict.SectionName,
            _ => null,
        };
        var resourceLabel = string.IsNullOrWhiteSpace(resourceName)
            ? resourceType
            : $"{resourceType} {resourceName}";

        return $"Schedule conflict: {resourceLabel} is already booked on {conflict.DayOfWeek} from {conflict.TimeIn:HH\\:mm} to {conflict.TimeOut:HH\\:mm}.";
    }
}
