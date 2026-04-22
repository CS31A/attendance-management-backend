namespace attendance_monitoring.Models.DTO.Response;

/// <summary>
/// DTO for user list with role-specific information
/// </summary>
public class GetAllUsersDto
{
    public string UserId { get; set; } = string.Empty;
    public string Username { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string Role { get; set; } = string.Empty;
    public StudentProfileDto? StudentProfile { get; set; }
    public InstructorProfileDto? InstructorProfile { get; set; }
    public AdminProfileDto? AdminProfile { get; set; }
}

/// <summary>
/// Student-specific profile information
/// </summary>
public class StudentProfileDto
{
    public int Id { get; set; }
    public Guid Uuid { get; set; }
    public string Firstname { get; set; } = string.Empty;
    public string Lastname { get; set; } = string.Empty;
    public bool IsRegular { get; set; }
    public int SectionId { get; set; }
    public string? SectionName { get; set; }
    public int? CourseId { get; set; }
    public string? CourseName { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
    public bool IsDeleted { get; set; }
    public DateTime? DeletedAt { get; set; }
}

/// <summary>
/// Instructor-specific profile information
/// </summary>
public class InstructorProfileDto
{
    public int Id { get; set; }
    public Guid Uuid { get; set; }
    public string? Firstname { get; set; }
    public string? Lastname { get; set; }
    public string? Department { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
    public bool IsDeleted { get; set; }
    public DateTime? DeletedAt { get; set; }
}

/// <summary>
/// Admin-specific profile information
/// </summary>
public class AdminProfileDto
{
    public int Id { get; set; }
    public Guid Uuid { get; set; }
    public string? Firstname { get; set; }
    public string? Lastname { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
    public bool IsDeleted { get; set; }
    public DateTime? DeletedAt { get; set; }
}
