using System;
using System.Runtime.Serialization;

namespace attendance_monitoring.Exceptions
{
    /// <summary>
    /// Exception thrown when an entity operation fails due to a conflict with existing data,
    /// such as foreign key constraint violations during delete operations.
    /// </summary>
    public class EntityConflictException : Exception
    {
        public string EntityName { get; }
        public string ConflictType { get; }

        /// <summary>
        /// Creates a conflict exception with a message
        /// </summary>
        /// <param name="entityName">The name of the entity type (e.g., "Section", "Classroom")</param>
        /// <param name="conflictType">The type of conflict (e.g., "schedules", "students", "enrollments")</param>
        /// <param name="message">User-facing error message describing the conflict</param>
        public EntityConflictException(string entityName, string conflictType, string message)
            : base(message)
        {
            EntityName = entityName;
            ConflictType = conflictType;
        }

        /// <summary>
        /// Creates a conflict exception with a message and inner exception
        /// </summary>
        /// <param name="entityName">The name of the entity type (e.g., "Section", "Classroom")</param>
        /// <param name="conflictType">The type of conflict (e.g., "schedules", "students", "enrollments")</param>
        /// <param name="message">User-facing error message describing the conflict</param>
        /// <param name="innerException">The underlying exception that caused the conflict</param>
        public EntityConflictException(string entityName, string conflictType, string message, Exception innerException)
            : base(message, innerException)
        {
            EntityName = entityName;
            ConflictType = conflictType;
        }

        /// <summary>
        /// Serialization constructor for deserialization
        /// </summary>
        /// <param name="info">Serialization info</param>
        /// <param name="context">Streaming context</param>
        [Obsolete("This API supports obsolete formatter-based serialization. It should not be called or extended by application code.")]
        protected EntityConflictException(SerializationInfo info, StreamingContext context)
            : base(info, context)
        {
            EntityName = info.GetString(nameof(EntityName))!;
            ConflictType = info.GetString(nameof(ConflictType))!;
        }

        /// <summary>
        /// Gets the object data for serialization
        /// </summary>
        /// <param name="info">Serialization info</param>
        /// <param name="context">Streaming context</param>
        [Obsolete("This API supports obsolete formatter-based serialization. It should not be called or extended by application code.")]
        public override void GetObjectData(SerializationInfo info, StreamingContext context)
        {
            base.GetObjectData(info, context);
            info.AddValue(nameof(EntityName), EntityName);
            info.AddValue(nameof(ConflictType), ConflictType);
        }
    }
}
