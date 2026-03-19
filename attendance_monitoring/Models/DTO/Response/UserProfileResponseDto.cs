namespace attendance_monitoring.Models.DTO.Response;

/// <summary>
/// Response DTO for user profile information
/// </summary>
public class UserProfileResponseDto
{
    /// <summary>
    /// User's Identity ID
    /// </summary>
    public string UserId { get; set; } = string.Empty;

    /// <summary>
    /// User's username
    /// </summary>
    public string Username { get; set; } = string.Empty;

    /// <summary>
    /// User's email address
    /// </summary>
    public string Email { get; set; } = string.Empty;

    /// <summary>
    /// User's role (Student, Instructor, Admin)
    /// </summary>
    public string Role { get; set; } = string.Empty;

    /// <summary>
    /// Account creation timestamp
    /// </summary>
    public DateTime CreatedAt { get; set; }

    /// <summary>
    /// Account last update timestamp
    /// </summary>
    public DateTime UpdatedAt { get; set; }

    /// <summary>
    /// Student profile information (populated only for Student role)
    /// </summary>
    public StudentProfileInfo? StudentProfile { get; set; }

    /// <summary>
    /// Instructor profile information (populated only for Instructor role)
    /// </summary>
    public InstructorProfileInfo? InstructorProfile { get; set; }
}

/// <summary>
/// Student-specific profile information
/// </summary>
public class StudentProfileInfo
{
    public int Id { get; set; }
    public string? Firstname { get; set; }
    public string? Lastname { get; set; }
    public bool IsRegular { get; set; }
    public int SectionId { get; set; }
    public string SectionName { get; set; } = string.Empty;
    public int CourseId { get; set; }
    public string CourseName { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
}

/// <summary>
/// Instructor-specific profile information
/// </summary>
public class InstructorProfileInfo
{
    public int Id { get; set; }
    public string? Firstname { get; set; }
    public string? Lastname { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
}
