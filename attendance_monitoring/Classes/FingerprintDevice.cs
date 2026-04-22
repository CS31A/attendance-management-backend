using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace attendance_monitoring.Classes;

[Index(nameof(DeviceIdentifier), IsUnique = true)]
[Index(nameof(IsActive))]
public class FingerprintDevice
{
    [Key]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public int Id { get; set; }

    public Guid Uuid { get; set; }

    [Required]
    [StringLength(100)]
    public string DeviceIdentifier { get; set; } = string.Empty;

    [StringLength(150)]
    public string? Name { get; set; }

    [StringLength(150)]
    public string? Location { get; set; }

    public bool IsActive { get; set; } = true;

    public DateTime? LastSeenAt { get; set; }

    public DateTime CreatedAt { get; set; }

    public DateTime UpdatedAt { get; set; }

    public ICollection<FingerprintScanEvent> ScanEvents { get; set; } = new List<FingerprintScanEvent>();
}
