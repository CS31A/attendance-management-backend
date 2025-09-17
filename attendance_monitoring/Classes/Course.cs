using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json.Serialization;
using Microsoft.EntityFrameworkCore;

namespace attendance_monitoring.Classes;

[Index(nameof(Name), IsUnique = true)]
public class Course
{
    [Key]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public int Id { get; set; }

    [Required(ErrorMessage = "Course name is required")]
    [StringLength(100, ErrorMessage = "Course name must be between 1 and 100 characters", MinimumLength = 1)]
    public string Name { get; set; } = string.Empty;

    // Navigation property for related Sections
    [JsonIgnore]
    public ICollection<Section> Sections { get; set; } = new List<Section>();

    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
}
