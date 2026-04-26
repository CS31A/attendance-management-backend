namespace attendance_monitoring.Models.DTO.Response;

/// <summary>
/// Lightweight DTO for student list views.
/// Matches the JSON serialization of Student entity (without [JsonIgnore] navigation properties).
/// Used for optimized database projections in list endpoints.
/// </summary>
public class StudentListDto
{
    public Guid Id { get; set; }
    public string Firstname { get; set; } = string.Empty;
    public string Lastname { get; set; } = string.Empty;
    public bool IsRegular { get; set; }
    public string UserId { get; set; } = string.Empty;
    public Guid SectionId { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
    public bool IsDeleted { get; set; }
    public DateTime? DeletedAt { get; set; }
}
