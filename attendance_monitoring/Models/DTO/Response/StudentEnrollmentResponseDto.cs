namespace attendance_monitoring.Models.DTO.Response;

public class StudentEnrollmentResponseDto
{
    public int Id { get; set; }
    public int StudentId { get; set; }
    public string? StudentFirstname { get; set; }
    public string? StudentLastname { get; set; }
    public string? StudentEmail { get; set; }
    public int SectionId { get; set; }
    public string? SectionName { get; set; }
    public int SubjectId { get; set; }
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
    public int StudentId { get; set; }
    public string? StudentFirstname { get; set; }
    public string? StudentLastname { get; set; }
    public bool IsRegular { get; set; }
    public List<EnrollmentSummaryDto> Enrollments { get; set; } = new();
}

public class EnrollmentSummaryDto
{
    public int EnrollmentId { get; set; }
    public int SectionId { get; set; }
    public string? SectionName { get; set; }
    public int SubjectId { get; set; }
    public string? SubjectName { get; set; }
    public string? SubjectCode { get; set; }
    public string EnrollmentType { get; set; } = string.Empty;
    public bool IsActive { get; set; }
    public DateTime EnrolledAt { get; set; }
}