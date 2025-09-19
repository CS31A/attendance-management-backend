using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace attendance_monitoring.Classes;

[Index(nameof(Jti), IsUnique = true)]
public class BlacklistedToken
{
    [Key]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public int Id { get; set; }
    
    // The JWT ID (jti) of the blacklisted token
    public string Jti { get; set; } = string.Empty;
    
    // When the token was blacklisted
    public DateTime BlacklistedAt { get; set; }
    
    // When the token was originally set to expire
    public DateTime ExpiresAt { get; set; }
}