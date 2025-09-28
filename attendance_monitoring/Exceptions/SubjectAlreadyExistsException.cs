using System;

namespace attendance_monitoring.Exceptions
{
    /// <summary>
    /// Exception thrown when a subject already exists
    /// </summary>
    public class SubjectAlreadyExistsException : Exception
    {
        public string SubjectCode { get; }

        public SubjectAlreadyExistsException(string subjectCode) : base($"Subject with code {subjectCode} already exists.")
        {
            SubjectCode = subjectCode;
        }

        public SubjectAlreadyExistsException(string subjectCode, string message) : base(message)
        {
            SubjectCode = subjectCode;
        }
    }
}