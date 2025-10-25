using System;
using System.Runtime.Serialization;

namespace attendance_monitoring.Exceptions
{
    /// <summary>
    /// Generic exception thrown when an entity is not found
    /// </summary>
    /// <typeparam name="TKey">The type of the entity's identifier (e.g., int, string, Guid)</typeparam>
    public class EntityNotFoundException<TKey> : Exception
    {
        public string EntityName { get; } = null!;
        public TKey Key { get; } = default!;

        /// <summary>
        /// Creates an exception with a default message
        /// </summary>
        /// <param name="entityName">The name of the entity type (e.g., "Classroom", "Subject")</param>
        /// <param name="entityId">The identifier of the entity that was not found</param>
        public EntityNotFoundException(string entityName, TKey entityId) 
            : base($"{entityName} with ID {entityId} was not found.")
        {
            EntityName = entityName;
            Key = entityId;
        }

        /// <summary>
        /// Creates an exception with a custom message
        /// </summary>
        /// <param name="entityName">The name of the entity type (e.g., "Classroom", "Subject")</param>
        /// <param name="entityId">The identifier of the entity that was not found</param>
        /// <param name="message">Custom error message</param>
        public EntityNotFoundException(string entityName, TKey entityId, string message) 
            : base(message)
        {
            EntityName = entityName;
            Key = entityId;
        }

        /// <summary>
        /// Creates an exception with a custom message and inner exception
        /// </summary>
        /// <param name="entityName">The name of the entity type</param>
        /// <param name="entityId">The identifier of the entity that was not found</param>
        /// <param name="message">Custom error message</param>
        /// <param name="innerException">The exception that caused this exception</param>
        public EntityNotFoundException(string entityName, TKey entityId, string message, Exception innerException) 
            : base(message, innerException)
        {
            EntityName = entityName;
            Key = entityId;
        }

        /// <summary>
        /// Serialization constructor for deserialization
        /// </summary>
        /// <param name="info">Serialization info</param>
        /// <param name="context">Streaming context</param>
        [Obsolete("This API supports obsolete formatter-based serialization. It should not be called or extended by application code.")]
        protected EntityNotFoundException(SerializationInfo info, StreamingContext context) 
            : base(info, context)
        {
            EntityName = info.GetString(nameof(EntityName))!;
            Key = (TKey)info.GetValue(nameof(Key), typeof(TKey))!;
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
            info.AddValue(nameof(Key), Key);
        }
    }
}
