using System.ComponentModel.DataAnnotations;

namespace attendance_monitoring.Models.DTO.Request
{
    /// <summary>
    /// DTO for partially updating a schedule. All fields are optional for PATCH operations.
    /// Only provided fields will be updated; omitted fields retain their existing values.
    /// </summary>
    /// <remarks>
    /// Validation rules:
    /// - TimeOut must be after TimeIn (validated against existing values if only one is provided)
    /// - DayOfWeek must be a valid day: Monday, Tuesday, Wednesday, Thursday, Friday, Saturday, Sunday
    /// </remarks>
    public class UpdateSchedule
    {
        /// <summary>
        /// The start time of the schedule. Must be before TimeOut.
        /// </summary>
        public TimeOnly? TimeIn { get; set; }
        
        /// <summary>
        /// The end time of the schedule. Must be after TimeIn.
        /// </summary>
        public TimeOnly? TimeOut { get; set; }
        
        /// <summary>
        /// The day of the week. Must be one of: Monday, Tuesday, Wednesday, Thursday, Friday, Saturday, Sunday.
        /// </summary>
        [StringLength(20)]
        public string? DayOfWeek { get; set; }
        
        /// <summary>
        /// The ID of the subject for this schedule.
        /// </summary>
        public int? SubjectId { get; set; }
        
        /// <summary>
        /// The ID of the classroom where the schedule takes place.
        /// </summary>
        public int? ClassroomId { get; set; }
        
        /// <summary>
        /// The ID of the section assigned to this schedule.
        /// </summary>
        public int? SectionId { get; set; }
        
        /// <summary>
        /// The ID of the instructor assigned to this schedule.
        /// </summary>
        public int? InstructorId { get; set; }
    }
}