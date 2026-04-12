namespace attendance_monitoring.Models.DTO.Request;

/// <summary>
/// Request model for file upload operations.
/// </summary>
public class FileUploadRequest
{
    /// <summary>
    /// The file to upload.
    /// </summary>
    public IFormFile File { get; set; } = null!;
}
