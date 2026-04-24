using System.Text.Json.Serialization;

namespace attendance_monitoring.Models.DTO.Response;

/// <summary>
/// Response DTO containing instructor information with their assigned sections and enrolled students.
/// </summary>
public class InstructorSectionsWithStudentsResponseDto
{
    /// <summary>
    /// Gets or sets the instructor's unique identifier.
    /// </summary>
    [JsonIgnore]
    public int InstructorId { get; set; }

    /// <summary>
    /// Gets or sets the instructor's UUID.
    /// </summary>
    [JsonPropertyName("instructorId")]
    public Guid InstructorUuid { get; set; }

    /// <summary>
    /// Gets or sets the instructor's first name.
    /// </summary>
    public string InstructorFirstname { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the instructor's last name.
    /// </summary>
    public string InstructorLastname { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the list of sections assigned to the instructor.
    /// </summary>
    public List<SectionWithStudentsDto> Sections { get; set; } = new();
}
