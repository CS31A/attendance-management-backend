namespace attendance_monitoring.Models.DTO.Response;

/// <summary>
/// Response DTO for student's subject information including schedule details
/// </summary>
public class StudentSubjectResponseDto
{
    /// <summary>
    /// Subject information
    /// </summary>
    public SubjectResponseDto Subject { get; set; } = null!;

    /// <summary>
    /// Schedule information for this subject
    /// </summary>
    public StudentSubjectScheduleDto Schedule { get; set; } = null!;

    /// <summary>
    /// Instructor information for this subject
    /// </summary>
    public InstructorResponseDto Instructor { get; set; } = null!;

    /// <summary>
    /// Classroom information for this subject
    /// </summary>
    public ClassroomResponseDto Classroom { get; set; } = null!;
}

/// <summary>
/// Simplified schedule information for student subjects (without nested objects to avoid circular references)
/// </summary>
public class StudentSubjectScheduleDto
{
    public Guid Id { get; set; }
    public TimeOnly TimeIn { get; set; }
    public TimeOnly TimeOut { get; set; }
    public string DayOfWeek { get; set; } = string.Empty;
}
