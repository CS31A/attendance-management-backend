using System.ComponentModel.DataAnnotations;

namespace attendance_monitoring.Models.DTO.Request
{
    public class CreateSchedule
    {
        [Required]
        public TimeOnly TimeIn { get; set; }

        [Required]
        public TimeOnly TimeOut { get; set; }

        [Required]
        [StringLength(20)]
        public string DayOfWeek { get; set; } = string.Empty;

        [Required]
        public Guid SubjectId { get; set; }

        [Required]
        public Guid ClassroomId { get; set; }

        [Required]
        public Guid SectionId { get; set; }

        [Required]
        public Guid InstructorId { get; set; }
    }
}
