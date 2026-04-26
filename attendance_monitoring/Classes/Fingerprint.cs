using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json.Serialization;
using Microsoft.AspNetCore.Identity;

namespace attendance_monitoring.Classes;

public class Fingerprint
{
    [Key]
    public Guid Id { get; set; }
    
    // Foreign key to Identity user - should not be nullable
    public string UserId { get; set; } = string.Empty;
    
    [Required]
    public string TemplateData { get; set; } = string.Empty;
    
    [Required]
    [MaxLength(100)]
    public string DeviceId { get; set; } = string.Empty;
    
    [Required]
    public int SensorFingerprintId { get; set; }

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
