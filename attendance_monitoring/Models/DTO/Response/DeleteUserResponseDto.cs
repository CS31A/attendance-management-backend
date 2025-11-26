namespace attendance_monitoring.Models.DTO.Response
{
    /// <summary>
    /// Response DTO for delete user operation
    /// </summary>
    public class DeleteUserResponseDto
    {
        public bool Success { get; set; }
        public string Message { get; set; } = string.Empty;
    }
}
