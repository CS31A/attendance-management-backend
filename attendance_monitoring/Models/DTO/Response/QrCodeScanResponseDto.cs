namespace attendance_monitoring.Models.DTO.Response;

public class QrCodeScanResponseDto
{
    public bool Success { get; set; }
    public string Message { get; set; } = string.Empty;
    public bool AttendanceMarked { get; set; }
    public DateTime? AttendanceTime { get; set; }
    public string StudentName { get; set; } = string.Empty;
    public string ClassName { get; set; } = string.Empty;
    public string SubjectName { get; set; } = string.Empty;
    public string RoomName { get; set; } = string.Empty;
    public string InstructorName { get; set; } = string.Empty;
    public int RemainingScans { get; set; }
}