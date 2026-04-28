namespace attendance_monitoring.Classes;

public sealed class ScheduleConflictDetails
{
    public Guid ScheduleId { get; init; }
    public string DayOfWeek { get; init; } = string.Empty;
    public TimeOnly TimeIn { get; init; }
    public TimeOnly TimeOut { get; init; }
    public string? SubjectName { get; init; }
    public string? ClassroomName { get; init; }
    public string? SectionName { get; init; }
    public string? InstructorName { get; init; }
}
