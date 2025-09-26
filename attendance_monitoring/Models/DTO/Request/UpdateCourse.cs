using System.ComponentModel.DataAnnotations;

namespace attendance_monitoring.Models.Request;

public class UpdateCourse
{
    [Required(ErrorMessage = "Course name is required")]
    [StringLength(100, ErrorMessage = "Course name must be between 1 and 100 characters", MinimumLength = 1)]
    public string Name { get; set; } = string.Empty;
}