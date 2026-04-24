using System.Text.Json.Serialization;

namespace attendance_monitoring.Models.DTO.Response;

/// <summary>
/// Response DTO containing section information with associated subjects and students.
/// </summary>
public class SectionWithStudentsDto
{
    /// <summary>
    /// Gets or sets the section's unique identifier.
    /// </summary>
    [JsonIgnore]
    public int SectionId { get; set; }

    /// <summary>
    /// Gets or sets the section's UUID.
    /// </summary>
    [JsonPropertyName("sectionId")]
    public Guid SectionUuid { get; set; }

    /// <summary>
    /// Gets or sets the section name (e.g., "BSCS 3A").
    /// </summary>
    public string SectionName { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the course's unique identifier.
    /// </summary>
    [JsonIgnore]
    public int CourseId { get; set; }

    /// <summary>
    /// Gets or sets the course's UUID.
    /// </summary>
    [JsonPropertyName("courseId")]
    public Guid CourseUuid { get; set; }

    /// <summary>
    /// Gets or sets the course name (e.g., "Bachelor of Science in Computer Science").
    /// </summary>
    public string CourseName { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the list of subjects taught in this section with their schedules and enrolled students.
    /// </summary>
    public List<SubjectScheduleDto> Subjects { get; set; } = new();
}
