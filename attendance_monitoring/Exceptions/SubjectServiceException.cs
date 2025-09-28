using System;

namespace attendance_monitoring.Exceptions
{
    /// <summary>
    /// General exception for subject-related operations
    /// </summary>
    public class SubjectServiceException : Exception
    {
        public string Operation { get; }

        public SubjectServiceException(string operation, string message) : base(message)
        {
            Operation = operation;
        }

        public SubjectServiceException(string operation, string message, Exception innerException) : base(message, innerException)
        {
            Operation = operation;
        }
    }
}