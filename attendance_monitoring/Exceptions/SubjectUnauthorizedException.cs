using System;

namespace attendance_monitoring.Exceptions
{
    /// <summary>
    /// Exception thrown when a subject operation is unauthorized
    /// </summary>
    public class SubjectUnauthorizedException : Exception
    {
        public string Operation { get; }
        public string UserId { get; }

        public SubjectUnauthorizedException(string operation, string userId) : base($"User {userId} is not authorized to perform {operation} on subjects.")
        {
            Operation = operation;
            UserId = userId;
        }

        public SubjectUnauthorizedException(string operation, string userId, string message) : base(message)
        {
            Operation = operation;
            UserId = userId;
        }
    }
}