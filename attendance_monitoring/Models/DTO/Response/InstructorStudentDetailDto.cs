namespace attendance_monitoring.Models.DTO.Response;

/// <summary>
/// Represents a student's enrollment entry visible to an instructor.
/// </summary>
public class InstructorStudentEnrollmentDto
{
    /// <summary>
    /// Gets or sets the subject's unique identifier.
    /// </summary>
    public Guid SubjectId { get; set; }

    /// <summary>
    /// Gets or sets the subject name.
    /// </summary>
    public string SubjectName { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the subject code.
    /// </summary>
    public string SubjectCode { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the section's unique identifier.
    /// </summary>
    public Guid SectionId { get; set; }

    /// <summary>
    /// Gets or sets the section name.
    /// </summary>
    public string SectionName { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the enrollment type.
    /// </summary>
    public string EnrollmentType { get; set; } = string.Empty;
}

/// <summary>
/// Represents aggregate attendance counts for a student.
/// </summary>
public class InstructorStudentAttendanceSummaryDto
{
    /// <summary>
    /// Gets or sets the total number of sessions.
    /// </summary>
    public int TotalSessions { get; set; }

    /// <summary>
    /// Gets or sets the number of present records.
    /// </summary>
    public int PresentCount { get; set; }

    /// <summary>
    /// Gets or sets the number of absent records.
    /// </summary>
    public int AbsentCount { get; set; }

    /// <summary>
    /// Gets or sets the number of late records.
    /// </summary>
    public int LateCount { get; set; }

    /// <summary>
    /// Gets or sets the attendance rate percentage.
    /// </summary>
    public double AttendanceRate { get; set; }
}

/// <summary>
/// Represents detailed student information for instructor-facing drilldown views.
/// </summary>
public class InstructorStudentDetailDto
{
    /// <summary>
    /// Gets or sets the student's unique identifier.
    /// </summary>
    public Guid StudentId { get; set; }

    /// <summary>
    /// Gets or sets the student's first name.
    /// </summary>
    public string Firstname { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the student's last name.
    /// </summary>
    public string Lastname { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the home section identifier, if available.
    /// </summary>
    public Guid? SectionId { get; set; }

    /// <summary>
    /// Gets or sets the home section name, if available.
    /// </summary>
    public string? SectionName { get; set; }

    /// <summary>
    /// Gets or sets the course identifier, if available.
    /// </summary>
    public Guid? CourseId { get; set; }

    /// <summary>
    /// Gets or sets the course name, if available.
    /// </summary>
    public string? CourseName { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether the student is regular.
    /// </summary>
    public bool IsRegular { get; set; }

    /// <summary>
    /// Gets or sets the student's primary enrollment type.
    /// </summary>
    public string EnrollmentType { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the student's enrollments relevant to the instructor view.
    /// </summary>
    public List<InstructorStudentEnrollmentDto> Enrollments { get; set; } = new();

    /// <summary>
    /// Gets or sets the student's aggregate attendance summary.
    /// </summary>
    public InstructorStudentAttendanceSummaryDto AttendanceSummary { get; set; } = new();
}
