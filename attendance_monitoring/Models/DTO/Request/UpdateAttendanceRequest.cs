using System.ComponentModel.DataAnnotations;

namespace attendance_monitoring.Models.DTO.Request;

/// <summary>
/// DTO for updating an existing attendance record.
/// </summary>
public class UpdateAttendanceRequest
{
    /// <summary>
    /// Updated attendance status: Present, Late, Excused, or Absent.
    /// </summary>
    [StringLength(20, ErrorMessage = "Status cannot exceed 20 characters")]
    [RegularExpression("^(Present|Late|Excused|Absent)$", ErrorMessage = "Status must be Present, Late, Excused, or Absent")]
    public string? Status { get; set; }

    /// <summary>
    /// Updated notes about this attendance record.
    /// </summary>
    [MaxLength(500, ErrorMessage = "Notes cannot exceed 500 characters")]
    public string? Notes { get; set; }
}
