using System.Text.Json.Serialization;

namespace attendance_monitoring.Models.DTO.Response;

/// <summary>
/// Represents a student belonging to an instructor section's home roster.
/// </summary>
public class InstructorHomeSectionStudentDto
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
    /// Gets or sets a value indicating whether the student is regular.
    /// </summary>
    public bool IsRegular { get; set; }

    /// <summary>
    /// Gets or sets the student's enrollment type.
    /// </summary>
    public string EnrollmentType { get; set; } = string.Empty;
}
