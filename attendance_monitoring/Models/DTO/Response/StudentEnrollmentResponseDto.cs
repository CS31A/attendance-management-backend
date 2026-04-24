using System.Text.Json.Serialization;

namespace attendance_monitoring.Models.DTO.Response;

public class StudentEnrollmentResponseDto
{
    [JsonIgnore]
    public int Id { get; set; }
    [JsonPropertyName("id")]
    public Guid Uuid { get; set; }
    [JsonIgnore]
    public int StudentId { get; set; }
    [JsonPropertyName("studentId")]
    public Guid? StudentUuid { get; set; }
    public string? StudentFirstname { get; set; }
    public string? StudentLastname { get; set; }
    public string? StudentEmail { get; set; }
    [JsonIgnore]
    public int SectionId { get; set; }
    [JsonPropertyName("sectionId")]
    public Guid? SectionUuid { get; set; }
    public string? SectionName { get; set; }
    [JsonIgnore]
    public int SubjectId { get; set; }
    [JsonPropertyName("subjectId")]
    public Guid? SubjectUuid { get; set; }
    public string? SubjectName { get; set; }
    public string? SubjectCode { get; set; }
    public bool IsActive { get; set; }
    public string EnrollmentType { get; set; } = string.Empty;
    public string? AcademicYear { get; set; }
    public string? Semester { get; set; }
    public DateTime EnrolledAt { get; set; }
    public DateTime? DroppedAt { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
}

public class StudentSectionsResponseDto
{
    [JsonIgnore]
    public int StudentId { get; set; }
    [JsonPropertyName("studentId")]
    public Guid? StudentUuid { get; set; }
    public string? StudentFirstname { get; set; }
    public string? StudentLastname { get; set; }
    public bool IsRegular { get; set; }
    public List<EnrollmentSummaryDto> Enrollments { get; set; } = new();
}

public class EnrollmentSummaryDto
{
    [JsonIgnore]
    public int EnrollmentId { get; set; }
    [JsonPropertyName("enrollmentId")]
    public Guid? EnrollmentUuid { get; set; }
    [JsonIgnore]
    public int SectionId { get; set; }
    [JsonPropertyName("sectionId")]
    public Guid? SectionUuid { get; set; }
    public string? SectionName { get; set; }
    [JsonIgnore]
    public int SubjectId { get; set; }
    [JsonPropertyName("subjectId")]
    public Guid? SubjectUuid { get; set; }
    public string? SubjectName { get; set; }
    public string? SubjectCode { get; set; }
    public string EnrollmentType { get; set; } = string.Empty;
    public bool IsActive { get; set; }
    public DateTime EnrolledAt { get; set; }
}
