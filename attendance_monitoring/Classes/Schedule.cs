using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace attendance_monitoring.Classes;

[Index(nameof(ClassroomId))]
[Index(nameof(DayOfWeek))]
[Index(nameof(TimeIn))]
[Index(nameof(TimeOut))]
[Index(nameof(ClassroomId), nameof(DayOfWeek), nameof(TimeIn), nameof(TimeOut), IsUnique = true)]
public class Schedules
{
    [Key]
    public Guid Id { get; set; }

    [Required]
    public TimeOnly TimeIn { get; set; }

    [Required]
    public TimeOnly TimeOut { get; set; }

    [Required]
    [StringLength(20)]
    public string DayOfWeek { get; set; } = string.Empty;

    // foreign key to subject id
    [Required]
    public Guid SubjectId { get; set; }

    // foreign key to classroom id
    [Required]
    public Guid ClassroomId { get; set; }

    [Required]
    public Guid SectionId { get; set; }

    // foreign key to instructor id
    [Required]
    public Guid InstructorId { get; set; }

    // Navigation properties
    public Subject Subject { get; set; } = null!;
    public Classroom Classroom { get; set; } = null!;
    public Section Section { get; set; } = null!;
    public Instructor Instructor { get; set; } = null!;

    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
}
