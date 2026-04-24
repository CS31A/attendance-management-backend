using System.Text.Json.Serialization;

namespace attendance_monitoring.Models.DTO.Response;

/// <summary>
/// Represents section drilldown details for an instructor, including handled classes and home section students.
/// </summary>
public class InstructorSectionDetailDto
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
    /// Gets or sets the number of handled classes for the section.
    /// </summary>
    public int HandledClassCount { get; set; }

    /// <summary>
    /// Gets or sets the number of students in the section's home roster.
    /// </summary>
    public int HomeSectionStudentCount { get; set; }

    /// <summary>
    /// Gets or sets the handled classes for the section.
    /// </summary>
    public List<InstructorHandledClassDto> HandledClasses { get; set; } = new();

    /// <summary>
    /// Gets or sets the students in the section's home roster.
    /// </summary>
    public List<InstructorHomeSectionStudentDto> HomeSectionStudents { get; set; } = new();
}
