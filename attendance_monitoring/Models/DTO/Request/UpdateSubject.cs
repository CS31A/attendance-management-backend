using System.ComponentModel.DataAnnotations;

namespace attendance_monitoring.Models.DTO.Request;

public class UpdateSubject
{
    [StringLength(100, ErrorMessage = "Subject name must be between 2 and 100 characters", MinimumLength = 2)]
    public string? Name { get; set; }
    
    [StringLength(30, ErrorMessage = "Subject code must be between 5 and 30 characters", MinimumLength = 5)]
    public string? Code { get; set; }
}