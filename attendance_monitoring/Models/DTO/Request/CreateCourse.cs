using System.ComponentModel.DataAnnotations;

namespace attendance_monitoring.Models.DTO.Request;

public class CreateCourse
{
    [Required(ErrorMessage = "Course name is required")]
    [StringLength(100, ErrorMessage = "Course name must be between 1 and 100 characters", MinimumLength = 20)]
    public string Name { get; set; } = string.Empty;
}