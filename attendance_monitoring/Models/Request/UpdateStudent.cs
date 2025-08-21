using System.ComponentModel.DataAnnotations;

namespace attendance_monitoring.Models.Request;

public class UpdateStudent
{
    [StringLength(100)]
    public string? Firstname { get; set; }

    [StringLength(100)]
    public string? Lastname { get; set; }

    [EmailAddress]
    public string? Email { get; set; }
}