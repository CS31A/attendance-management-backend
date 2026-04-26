using System.ComponentModel.DataAnnotations;

namespace attendance_monitoring.Models.DTO.Request;

/// <summary>
/// DTO for filtering attendance records with pagination.
/// </summary>
public class AttendanceFilterRequest
{
    /// <summary>
    /// Filter by student ID.
    /// </summary>
    public Guid? StudentId { get; set; }

    /// <summary>
    /// Filter by session ID.
    /// </summary>
    public Guid? SessionId { get; set; }

    /// <summary>
    /// Filter by schedule ID.
    /// </summary>
    public Guid? ScheduleId { get; set; }

    /// <summary>
    /// Filter by section ID.
    /// </summary>
    public Guid? SectionId { get; set; }

    /// <summary>
    /// Filter by subject ID.
    /// </summary>
    public Guid? SubjectId { get; set; }

    /// <summary>
    /// Filter by attendance status.
    /// </summary>
    [RegularExpression("^(Present|Late|Excused|Absent)$", ErrorMessage = "Status must be Present, Late, Excused, or Absent")]
    public string? Status { get; set; }

    /// <summary>
    /// Filter by start date (inclusive).
    /// </summary>
    public DateTime? StartDate { get; set; }

    /// <summary>
    /// Filter by end date (inclusive).
    /// </summary>
    public DateTime? EndDate { get; set; }

    /// <summary>
    /// Filter by manual entry flag.
    /// </summary>
    public bool? IsManualEntry { get; set; }

    /// <summary>
    /// Page number for pagination (1-based).
    /// </summary>
    [Range(1, int.MaxValue, ErrorMessage = "Page number must be at least 1")]
    public int PageNumber { get; set; } = 1;

    /// <summary>
    /// Number of records per page.
    /// </summary>
    [Range(1, 100, ErrorMessage = "Page size must be between 1 and 100")]
    public int PageSize { get; set; } = 50;
}
