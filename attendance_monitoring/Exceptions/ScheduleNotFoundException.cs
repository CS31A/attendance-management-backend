namespace attendance_monitoring.Exceptions
{
    public class ScheduleNotFoundException : Exception
    {
        public int ScheduleId { get; }

        public ScheduleNotFoundException()
        {
        }

        public ScheduleNotFoundException(int scheduleId) : base($"Schedule with ID {scheduleId} not found.")
        {
            ScheduleId = scheduleId;
        }

        public ScheduleNotFoundException(int scheduleId, string? message) : base(message)
        {
            ScheduleId = scheduleId;
        }

        public ScheduleNotFoundException(int scheduleId, string? message, Exception? innerException) : base(message, innerException)
        {
            ScheduleId = scheduleId;
        }
    }
}