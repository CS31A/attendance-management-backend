using System.ComponentModel.DataAnnotations;

namespace attendance_monitoring.Models.DTO.Request;

public class UpdateClassroom
{
    [StringLength(100, ErrorMessage = "Classroom name must be between 2 and 100 characters", MinimumLength = 2)]
    public string? Name { get; set; }
}