using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json.Serialization;

namespace attendance_monitoring.Classes;

public class Section
{
    [Key]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public int Id { get; set; }

    [Required(ErrorMessage = "Section name is required")]
    [StringLength(100, ErrorMessage = "Section name must be between 1 and 100 characters", MinimumLength = 1)]
    public string Name { get; set; } = string.Empty;

    // Foreign key to Instructor - should not be nullable
    public int InstructorId { get; set; }

    // Foreign key to Course - should not be nullable
    public int CourseId { get; set; }

    // Navigation property - required relationship
    [ForeignKey("CourseId")]
    [JsonIgnore]
    public Course Course { get; set; } = null!;

    // Navigation property - required relationship
    [ForeignKey("InstructorId")]
    [JsonIgnore]
    public Instructor Instructor { get; set; } = null!;

    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
}
