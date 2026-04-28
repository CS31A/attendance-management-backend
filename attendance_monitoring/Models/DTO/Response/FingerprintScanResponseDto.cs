namespace attendance_monitoring.Models.DTO.Response;

/// <summary>
/// Response DTO for fingerprint scan attendance operations.
/// </summary>
public class FingerprintScanResponseDto
{
    /// <summary>
    /// Indicates if the scan operation was successful.
    /// </summary>
    public bool Success { get; set; }

    /// <summary>
    /// Message describing the result of the operation.
    /// </summary>
    public string Message { get; set; } = string.Empty;

    /// <summary>
    /// Indicates if attendance was marked (false if duplicate scan or other issue).
    /// </summary>
    public bool AttendanceMarked { get; set; }

    /// <summary>
    /// The time of attendance recording.
    /// </summary>
    public DateTime? AttendanceTime { get; set; }

    /// <summary>
    /// The identified student's UUID.
    /// </summary>
    public Guid? StudentId { get; set; }

    /// <summary>
    /// The identified student's name.
    /// </summary>
    public string StudentName { get; set; } = string.Empty;

    /// <summary>
    /// The class/section name.
    /// </summary>
    public string ClassName { get; set; } = string.Empty;

    /// <summary>
    /// The subject name.
    /// </summary>
    public string SubjectName { get; set; } = string.Empty;

    /// <summary>
    /// The room name where attendance was recorded.
    /// </summary>
    public string RoomName { get; set; } = string.Empty;

    /// <summary>
    /// The instructor's name.
    /// </summary>
    public string InstructorName { get; set; } = string.Empty;

    /// <summary>
    /// The UUID of the attendance record created.
    /// </summary>
    public Guid? AttendanceRecordId { get; set; }

    /// <summary>
    /// The attendance status (Present, Late, etc.).
    /// </summary>
    public string? AttendanceStatus { get; set; }

    /// <summary>
    /// Indicates if this was a duplicate scan (student already checked in).
    /// </summary>
    public bool IsDuplicateScan { get; set; }

    /// <summary>
    /// The session UUID for which attendance was recorded.
    /// </summary>
    public Guid? SessionId { get; set; }

    /// <summary>
    /// The method used for fingerprint matching (Exact, MatchScore, etc.).
    /// </summary>
    public string? MatchMethod { get; set; }

    /// <summary>
    /// The confidence score of the fingerprint match (0-100), if applicable.
    /// </summary>
    public int? MatchScore { get; set; }
}
