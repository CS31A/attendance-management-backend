namespace attendance_monitoring.Models.DTO.Response;

/// <summary>
/// Response DTO for profile update operations
/// </summary>
public class UpdateProfileResponse
{
    /// <summary>
    /// Indicates whether the update was successful
    /// </summary>
    public bool Success { get; set; }

    /// <summary>
    /// Message describing the result of the update operation
    /// </summary>
    public string Message { get; set; } = string.Empty;

    /// <summary>
    /// Updated user profile information (populated on success)
    /// </summary>
    public UserProfileResponseDto? UpdatedProfile { get; set; }
}
