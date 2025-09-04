using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.AspNetCore.Identity;

namespace attendance_monitoring.Classes;

public class Instructor
{
    [Key]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public int Id { get; set; }

    public string? Firstname { get; set; }
    public string? Lastname { get; set; }
    public string? Email { get; set; }

    // Foreign key to Identity user - should not be nullable
    public string UserId { get; set; } = string.Empty;

    // Navigation property - required relationship
    [ForeignKey("UserId")]
    public IdentityUser User { get; set; } = null!;

    // Navigation property for related Sections
    public ICollection<Section> Sections { get; set; } = new List<Section>();

    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
}
