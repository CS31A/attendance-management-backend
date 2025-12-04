using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;
using Microsoft.EntityFrameworkCore;

namespace attendance_monitoring.Classes;

[Index(nameof(Name), IsUnique = true)]
[Index(nameof(Code), IsUnique = true)]
public class Subject
{
    [Key]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public int Id { get; set; }

    [Required]
    [StringLength(100, MinimumLength = 2, ErrorMessage = "Subject name must be greater than or equal to 2 characters")]
    public string Name { get; set; } = string.Empty;

    [Required]
    [StringLength(30, MinimumLength = 5, ErrorMessage = "Subject code must be greater than 5 characters")]
    public string Code { get; set; } = string.Empty;

    // Navigation property for student enrollments
    [JsonIgnore]
    public ICollection<StudentEnrollment> StudentEnrollments { get; set; } = new List<StudentEnrollment>();

    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
}