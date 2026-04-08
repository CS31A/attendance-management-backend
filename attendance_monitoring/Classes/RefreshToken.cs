using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.AspNetCore.Identity;
using System.Text.Json.Serialization;
using Microsoft.EntityFrameworkCore;

namespace attendance_monitoring.Classes;

[Index(nameof(ExpiresAt))]
[Index(nameof(UserId), nameof(IsRevoked), nameof(ExpiresAt))]
public class RefreshToken
{
    [Key]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public int Id { get; set; }

    // Foreign key to IdentityUser
    public string UserId { get; set; } = string.Empty;

    // Navigation property
    [ForeignKey("UserId")]
    [JsonIgnore]
    public IdentityUser User { get; set; } = null!;

    // Hashed refresh token
    public string TokenHash { get; set; } = string.Empty;

    // Token expiration
    public DateTime ExpiresAt { get; set; }

    // Creation timestamp
    public DateTime CreatedAt { get; set; }

    // For token invalidation
    public bool IsRevoked { get; set; }

    // --- Enhanced Fields for Auditing & Reuse Detection ---
    // The timestamp of revocation
    public DateTime? RevokedAt { get; set; }

    // Stores the hash of the new token that replaced this one
    public string? ReplacedByTokenHash { get; set; }
}
