namespace attendance_monitoring.Models.DTO.Response
{
    public class ScheduleResponseDto
    {
        public int Id { get; set; }
        public TimeOnly TimeIn { get; set; }
        public TimeOnly TimeOut { get; set; }
        public string DayOfWeek { get; set; } = string.Empty;
        public SubjectResponseDto Subject { get; set; } = null!;
        public ClassroomResponseDto Classroom { get; set; } = null!;
        public SectionResponseDto Section { get; set; } = null!;
        public InstructorResponseDto Instructor { get; set; } = null!;
        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }
    }
}
