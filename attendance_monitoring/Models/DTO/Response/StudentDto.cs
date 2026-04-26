using System.Text.Json.Serialization;

namespace attendance_monitoring.Models.DTO.Response;

/// <summary>
/// Response DTO containing student information with enrollment status.
/// </summary>
public class StudentDto
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
    /// Gets or sets whether the student is a regular student (true) or irregular student (false).
    /// </summary>
    public bool IsRegular { get; set; }

    /// <summary>
    /// Gets or sets the enrollment type (e.g., "Regular", "Irregular", "Retake").
    /// </summary>
    public string EnrollmentType { get; set; } = "Regular";
}
