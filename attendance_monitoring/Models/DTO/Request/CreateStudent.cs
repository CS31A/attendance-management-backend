using System.ComponentModel.DataAnnotations;
using attendance_monitoring.Attributes;

namespace attendance_monitoring.Models.DTO.Request;

public class CreateStudent
{
    [Required]
    [StringLength(100)]
    public string Firstname { get; set; } = string.Empty;

    [Required]
    [StringLength(100)]
    public string Lastname { get; set; } = string.Empty;

    [Required]
    public bool IsRegular { get; set; } = true;

    [Required]
    public Guid SectionId { get; set; }
}
