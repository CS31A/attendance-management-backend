using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.AspNetCore.Identity;
using System.Text.Json.Serialization;
using Microsoft.EntityFrameworkCore;

namespace attendance_monitoring.Classes;

[Index(nameof(UserId), IsUnique = true)]
[Index(nameof(IsDeleted))]
public class Admin
{
    [Key]
    public Guid Id { get; set; }

    public string? Firstname { get; set; }
    public string? Lastname { get; set; }

    // Foreign key to Identity user - should not be nullable
    public string UserId { get; set; } = string.Empty;

    // Navigation property - required relationship
    [ForeignKey("UserId")]
    [JsonIgnore]
    public IdentityUser User { get; set; } = null!;

    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }

    // Soft delete properties
    public bool IsDeleted { get; set; } = false;
    public DateTime? DeletedAt { get; set; }
}
