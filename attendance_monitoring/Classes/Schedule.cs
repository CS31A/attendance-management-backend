using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace attendance_monitoring.Classes;

public class Schedules
{
    [Key]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public int Id { get; set; }

    [Required]
    public TimeOnly TimeIn { get; set; }
    
    [Required]
    public TimeOnly TimeOut { get; set; }
    
    [Required]
    [StringLength(20)]
    public string DayOfWeek { get; set; } = string.Empty;
    
    // foreign key to subject id
    [Required]
    public int SubjectId { get; set; }
    
    // foreign key to classroom id
    [Required]
    public int ClassroomId { get; set; }
    
    [Required] 
    public int SectionId { get; set; }

    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
}
