using System.Runtime.Serialization;

namespace attendance_monitoring.Exceptions
{
    public class ScheduleServiceException : Exception
    {
        public string? ErrorCode { get; }

        public ScheduleServiceException()
        {
        }

        public ScheduleServiceException(string? message) : base(message)
        {
        }

        public ScheduleServiceException(string? message, Exception? innerException) : base(message, innerException)
        {
        }

        public ScheduleServiceException(string? errorCode, string? message) : base(message)
        {
            ErrorCode = errorCode;
        }

        public ScheduleServiceException(string? errorCode, string? message, Exception? innerException) : base(message, innerException)
        {
            ErrorCode = errorCode;
        }

        protected ScheduleServiceException(SerializationInfo info, StreamingContext context) : base(info, context)
        {
        }
    }
}