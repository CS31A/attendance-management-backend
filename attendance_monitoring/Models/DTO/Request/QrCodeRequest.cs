namespace attendance_monitoring.Models.DTO.Request;

public class QrCodeRequest
{
    public int SectionId { get; set; }
    public int Schedule { get; set; } 
    public int RoomId { get; set; }
    public string SubjectName { get; set; } = string.Empty;
    public string UniqueKey { get; set; } = string.Empty; // auto-generated in the tomaclient
}