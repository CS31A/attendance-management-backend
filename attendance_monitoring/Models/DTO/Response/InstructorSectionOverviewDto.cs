using System.Text.Json.Serialization;

namespace attendance_monitoring.Models.DTO.Response;

/// <summary>
/// Represents a high-level overview of an instructor-handled section.
/// </summary>
public class InstructorSectionOverviewDto
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
    /// Gets or sets the section name.
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
    /// Gets or sets the course name.
    /// </summary>
    public string CourseName { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the number of classes handled by the instructor for this section.
    /// </summary>
    public int HandledClassCount { get; set; }

    /// <summary>
    /// Gets or sets the number of unique students associated with the instructor's handled classes for this section.
    /// </summary>
    public int UniqueStudentCount { get; set; }
}
