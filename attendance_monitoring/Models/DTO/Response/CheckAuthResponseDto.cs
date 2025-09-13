namespace attendance_monitoring.Models.DTO.Response
{
    public class CheckAuthResponseDto
    {
        public bool Success { get; set; }
        public string? Message { get; set; }
        public string? User { get; set; }
    }
}
