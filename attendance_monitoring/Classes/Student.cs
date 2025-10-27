using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.AspNetCore.Identity;
using System.Text.Json.Serialization;
using Microsoft.EntityFrameworkCore;

namespace attendance_monitoring.Classes;

[Index(nameof(UserId), IsUnique = true)]
[Index(nameof(IsDeleted))]
public class Student
{
    [Key]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public int Id { get; set; }

    public string? Firstname { get; set; }
    public string? Lastname { get; set; }
    public string? Email { get; set; }
    public bool IsRegular { get; set; }
    // Foreign key to Identity user - should not be nullable
    public string UserId { get; set; } = string.Empty;

    // Foreign key to Section - primary/home section (kept for backward compatibility)
    public int SectionId { get; set; }

    // Navigation property - required relationship
    [ForeignKey("UserId")]
    [JsonIgnore]
    public IdentityUser User { get; set; } = null!;

    // Navigation property - required relationship (primary/home section)
    [ForeignKey("SectionId")]
    [JsonIgnore]
    public Section Section { get; set; } = null!;

    // Navigation property for additional enrollments (irregular student subjects)
    [JsonIgnore]
    public ICollection<StudentEnrollment> AdditionalEnrollments { get; set; } = new List<StudentEnrollment>();

    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }

    // Soft delete properties
    public bool IsDeleted { get; set; } = false;
    public DateTime? DeletedAt { get; set; }
}
