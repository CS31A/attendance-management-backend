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
        public int SubjectId { get; set; }

        public Guid? SubjectUuid { get; set; }

        [Required]
        public int ClassroomId { get; set; }

        public Guid? ClassroomUuid { get; set; }

        [Required]
        public int SectionId { get; set; }

        public Guid? SectionUuid { get; set; }

        [Required]
        public int InstructorId { get; set; }

        public Guid? InstructorUuid { get; set; }
    }
}
