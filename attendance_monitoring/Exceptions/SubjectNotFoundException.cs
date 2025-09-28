using System;

namespace attendance_monitoring.Exceptions
{
    /// <summary>
    /// Exception thrown when a subject is not found
    /// </summary>
    public class SubjectNotFoundException : Exception
    {
        public int SubjectId { get; }

        public SubjectNotFoundException(int subjectId) : base($"Subject with ID {subjectId} was not found.")
        {
            SubjectId = subjectId;
        }

        public SubjectNotFoundException(int subjectId, string message) : base(message)
        {
            SubjectId = subjectId;
        }
    }
}