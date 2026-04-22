using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace attendance_monitoring.Classes;

[Index(nameof(ClassroomId))]
[Index(nameof(DayOfWeek))]
[Index(nameof(TimeIn))]
[Index(nameof(TimeOut))]
[Index(nameof(TimeIn), nameof(TimeOut), IsUnique = true)]
public class Schedules
{
    [Key]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public int Id { get; set; }

    public Guid Uuid { get; set; }

    [Required]
    public TimeOnly TimeIn { get; set; }

    [Required]
    public TimeOnly TimeOut { get; set; }

    [Required]
    [StringLength(20)]
    public string DayOfWeek { get; set; } = string.Empty;

    // foreign key to subject id
    [Required]
    public int SubjectId { get; set; }

    // foreign key to classroom id
    [Required]
    public int ClassroomId { get; set; }

    [Required]
    public int SectionId { get; set; }

    // foreign key to instructor id
    [Required]
    public int InstructorId { get; set; }

    // Navigation properties
    public Subject Subject { get; set; } = null!;
    public Classroom Classroom { get; set; } = null!;
    public Section Section { get; set; } = null!;
    public Instructor Instructor { get; set; } = null!;

    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
}
