using System.ComponentModel.DataAnnotations;

namespace attendance_monitoring.Models.DTO.Request;

/// <summary>
/// DTO for starting a session.
/// </summary>
public class StartSession
{
    /// <summary>
    /// The actual room ID where the session is being held (if different from scheduled room).
    /// </summary>
    [Range(1, int.MaxValue, ErrorMessage = "Room ID must be a positive integer")]
    public int? ActualRoomId { get; set; }

    /// <summary>
    /// The number of minutes after the scheduled start time before attendance is considered late.
    /// Defaults to 15 minutes if not specified.
    /// </summary>
    [Range(0, 120, ErrorMessage = "Attendance cutoff must be between 0 and 120 minutes")]
    public int? AttendanceCutoffMinutes { get; set; }

    /// <summary>
    /// Optimistic concurrency token for the current session row.
    /// Serialized as base64 over JSON.
    /// </summary>
    public byte[]? RowVersion { get; set; }
}
