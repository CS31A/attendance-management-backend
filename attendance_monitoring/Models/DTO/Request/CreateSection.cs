using System.ComponentModel.DataAnnotations;

namespace attendance_monitoring.Models.DTO.Request
{
    public class CreateSection
    {
        [Required(ErrorMessage = "Section name is required")]
        [StringLength(100, ErrorMessage = "Section name must be between 1 and 100 characters", MinimumLength = 4)]
        public string Name { get; set; } = string.Empty;

        [Required(ErrorMessage = "CourseId is required")]
        public Guid? CourseId { get; set; }
    }
}
