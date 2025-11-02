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
    public int? ProfileId { get; set; }
    public string? Firstname { get; set; }
    public string? Lastname { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
}