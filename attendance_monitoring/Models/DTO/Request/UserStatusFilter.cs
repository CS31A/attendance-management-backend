namespace attendance_monitoring.Models.DTO.Request
{
    /// <summary>
    /// Enum to filter users by their deletion status
    /// </summary>
    public enum UserStatus
    {
        /// <summary>
        /// Only active (non-deleted) users
        /// </summary>
        Active = 0,

        /// <summary>
        /// Only archived (soft-deleted) users
        /// </summary>
        Archived = 1,

        /// <summary>
        /// All users regardless of status
        /// </summary>
        All = 2
    }
}
