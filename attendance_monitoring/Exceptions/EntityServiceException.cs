using System;

namespace attendance_monitoring.Exceptions
{
    /// <summary>
    /// Generic exception thrown when an entity operation fails at the service level
    /// </summary>
    public class EntityServiceException : Exception
    {
        public string EntityName { get; }
        public string Operation { get; }

        /// <summary>
        /// Creates a service exception with a message
        /// </summary>
        /// <param name="entityName">The name of the entity type (e.g., "Classroom", "Subject")</param>
        /// <param name="operation">The operation that failed (e.g., "CreateClassroom", "UpdateSubject")</param>
        /// <param name="message">Error message describing the failure</param>
        public EntityServiceException(string entityName, string operation, string message)
            : base(message)
        {
            EntityName = entityName;
            Operation = operation;
        }

        /// <summary>
        /// Creates a service exception with a message and inner exception
        /// </summary>
        /// <param name="entityName">The name of the entity type (e.g., "Classroom", "Subject")</param>
        /// <param name="operation">The operation that failed (e.g., "CreateClassroom", "UpdateSubject")</param>
        /// <param name="message">Error message describing the failure</param>
        /// <param name="innerException">The exception that caused this service exception</param>
        public EntityServiceException(string entityName, string operation, string message, Exception innerException)
            : base(message, innerException)
        {
            EntityName = entityName;
            Operation = operation;
        }
    }
}
