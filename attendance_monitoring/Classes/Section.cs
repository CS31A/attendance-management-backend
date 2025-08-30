using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.AspNetCore.Identity;

namespace attendance_monitoring.Classes;

public class Section
{
    [Key]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public int Id { get; set; }

    public string Name { get; set; }

    // Foreign key to Instructor - should not be nullable
    public int InstructorId { get; set; }

    // Navigation property - required relationship
    [ForeignKey("InstructorId")]
    public Instructor Instructor { get; set; } = null!;

    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
}
