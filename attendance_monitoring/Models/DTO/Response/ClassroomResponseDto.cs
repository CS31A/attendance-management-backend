namespace attendance_monitoring.Models.DTO.Response
{
    public class ClassroomResponseDto
    {
        public int Id { get; set; }
        public Guid Uuid { get; set; }
        public string Name { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }
    }
}
