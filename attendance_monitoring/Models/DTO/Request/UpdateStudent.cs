using System.ComponentModel.DataAnnotations;
using attendance_monitoring.Attributes;

namespace attendance_monitoring.Models.DTO.Request;

public class UpdateStudent
{
    [StringLength(100)]
    public string? Firstname { get; set; }

    [StringLength(100)]
    public string? Lastname { get; set; }

    public bool? IsRegular { get; set; }
}
