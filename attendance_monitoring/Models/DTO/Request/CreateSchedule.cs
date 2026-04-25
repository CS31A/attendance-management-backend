using System.ComponentModel.DataAnnotations;
using attendance_monitoring.Attributes;

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
        [NotEmptyGuid(ErrorMessage = "SubjectId is required")]
        public Guid SubjectId { get; set; }

        [Required]
        [NotEmptyGuid(ErrorMessage = "ClassroomId is required")]
        public Guid ClassroomId { get; set; }

        [Required]
        [NotEmptyGuid(ErrorMessage = "SectionId is required")]
        public Guid SectionId { get; set; }

        [Required]
        [NotEmptyGuid(ErrorMessage = "InstructorId is required")]
        public Guid InstructorId { get; set; }
    }
}
