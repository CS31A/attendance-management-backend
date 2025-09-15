using System.ComponentModel.DataAnnotations;

namespace attendance_monitoring.Models.Request
{
    public class CreateSection
    {
        [Required(ErrorMessage = "Section name is required")]
        [StringLength(100, ErrorMessage = "Section name must be between 1 and 100 characters", MinimumLength = 1)]
        public string Name { get; set; } = string.Empty;

        [Required(ErrorMessage = "Instructor ID is required")]
        public int InstructorId { get; set; }

        [Required(ErrorMessage = "Course ID is required")]
        public int CourseId { get; set; }
    }
}