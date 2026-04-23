namespace attendance_monitoring.Models.DTO.Response;

/// <summary>
/// Represents a student enrolled in a handled class for instructor drilldown views.
/// </summary>
public class InstructorHandledClassStudentDto
{
    /// <summary>
    /// Gets or sets the student's unique identifier.
    /// </summary>
    public int StudentId { get; set; }

    /// <summary>
    /// Gets or sets the student's UUID.
    /// </summary>
    public Guid StudentUuid { get; set; }

    /// <summary>
    /// Gets or sets the student's first name.
    /// </summary>
    public string Firstname { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the student's last name.
    /// </summary>
    public string Lastname { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets a value indicating whether the student is regular.
    /// </summary>
    public bool IsRegular { get; set; }

    /// <summary>
    /// Gets or sets the student's enrollment type.
    /// </summary>
    public string EnrollmentType { get; set; } = string.Empty;
}

/// <summary>
/// Represents an instructor-handled class with schedule, room, and student roster details.
/// </summary>
public class InstructorHandledClassDto
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
    /// Gets or sets the subject name.
    /// </summary>
    public string SubjectName { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the subject code.
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
    /// Gets or sets the scheduled day of week.
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
    /// Gets or sets the classroom name.
    /// </summary>
    public string ClassroomName { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the number of students in the handled class.
    /// </summary>
    public int StudentCount { get; set; }

    /// <summary>
    /// Gets or sets the students associated with the handled class.
    /// </summary>
    public List<InstructorHandledClassStudentDto> Students { get; set; } = new();
}
