using System;

namespace attendance_monitoring.Exceptions
{
    /// <summary>
    /// Exception thrown when a classroom operation fails at the service level
    /// </summary>
    public class ClassroomServiceException : Exception
    {
        public string Operation { get; }

        public ClassroomServiceException(string operation, string message) : base(message)
        {
            Operation = operation;
        }

        public ClassroomServiceException(string operation, string message, Exception innerException) : base(message, innerException)
        {
            Operation = operation;
        }
    }
}