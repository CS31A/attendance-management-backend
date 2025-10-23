using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json.Serialization;

namespace attendance_monitoring.Classes
{
    public class QrCode
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int Id { get; set; }

        [Required]
        public int ScheduleId { get; set; }

        [Required]
        public int SectionId { get; set; }

        [Required]
        public int ActualRoomId { get; set; }

        [Required]
        [StringLength(255)]
        public string QrHash { get; set; } = string.Empty;

        [Required]
        public DateTime ExpiresAt { get; set; }

        public int? MaxUsage { get; set; }

        public bool IsActive { get; set; } = true;

        public int UsageCount { get; set; } = 0;

        public DateTime GeneratedAt { get; set; }

        // Revocation audit trail
        public DateTime? RevokedAt { get; set; }

        [StringLength(256)]
        public string? RevokedBy { get; set; }

        [StringLength(500)]
        public string? RevocationReason { get; set; }

        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }

        // Navigation properties
        [ForeignKey("ScheduleId")]
        [JsonIgnore]
        public Schedules Schedule { get; set; } = null!;

        [ForeignKey("SectionId")]
        [JsonIgnore]
        public Section Section { get; set; } = null!;

        [ForeignKey("ActualRoomId")]
        [JsonIgnore]
        public Classroom ActualRoom { get; set; } = null!;
    }
}