namespace attendance_monitoring.Models.DTO.Response;

/// <summary>
/// Response DTO containing subject and schedule information with enrolled students.
/// </summary>
public class SubjectScheduleDto
{
    /// <summary>
    /// Gets or sets the subject's unique identifier.
    /// </summary>
    public int SubjectId { get; set; }

    /// <summary>
    /// Gets or sets the subject's UUID.
    /// </summary>
    public Guid SubjectUuid { get; set; }

    /// <summary>
    /// Gets or sets the subject name (e.g., "Data Structures").
    /// </summary>
    public string SubjectName { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the subject code (e.g., "CS301").
    /// </summary>
    public string SubjectCode { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the schedule's unique identifier.
    /// </summary>
    public int ScheduleId { get; set; }

    /// <summary>
    /// Gets or sets the schedule's UUID.
    /// </summary>
    public Guid ScheduleUuid { get; set; }

    /// <summary>
    /// Gets or sets the day of the week for this schedule (e.g., "Monday").
    /// </summary>
    public string DayOfWeek { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the scheduled start time.
    /// </summary>
    public TimeOnly TimeIn { get; set; }

    /// <summary>
    /// Gets or sets the scheduled end time.
    /// </summary>
    public TimeOnly TimeOut { get; set; }

    /// <summary>
    /// Gets or sets the classroom's unique identifier.
    /// </summary>
    public int ClassroomId { get; set; }

    /// <summary>
    /// Gets or sets the classroom's UUID.
    /// </summary>
    public Guid ClassroomUuid { get; set; }

    /// <summary>
    /// Gets or sets the classroom name where the class is held.
    /// </summary>
    public string ClassroomName { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the list of students enrolled in this subject-section combination.
    /// </summary>
    public List<StudentDto> Students { get; set; } = new();
}
