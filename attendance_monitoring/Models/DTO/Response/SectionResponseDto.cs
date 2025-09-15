namespace attendance_monitoring.Models.DTO.Response
{
    public class SectionResponseDto
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public int InstructorId { get; set; }
        public int CourseId { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }
    }
}