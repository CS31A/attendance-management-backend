using System;

namespace attendance_monitoring.Exceptions
{
    /// <summary>
    /// Generic exception thrown when attempting to create an entity that already exists
    /// </summary>
    /// <typeparam name="TKey">The type of the entity's identifier (e.g., int, string, Guid)</typeparam>
    public class EntityAlreadyExistsException<TKey> : Exception
    {
        public string EntityName { get; }
        public TKey EntityIdentifier { get; }
        public string IdentifierPropertyName { get; }

        /// <summary>
        /// Creates an exception with a default message
        /// </summary>
        /// <param name="entityName">The name of the entity type (e.g., "Subject", "User")</param>
        /// <param name="identifierPropertyName">The name of the property that caused the conflict (e.g., "Code", "Email")</param>
        /// <param name="entityIdentifier">The value that already exists</param>
        public EntityAlreadyExistsException(string entityName, string identifierPropertyName, TKey entityIdentifier) 
            : base($"{entityName} with {identifierPropertyName} '{entityIdentifier}' already exists.")
        {
            EntityName = entityName;
            IdentifierPropertyName = identifierPropertyName;
            EntityIdentifier = entityIdentifier;
        }

        /// <summary>
        /// Creates an exception with a custom message
        /// </summary>
        /// <param name="entityName">The name of the entity type</param>
        /// <param name="identifierPropertyName">The name of the property that caused the conflict</param>
        /// <param name="entityIdentifier">The value that already exists</param>
        /// <param name="message">Custom error message</param>
        public EntityAlreadyExistsException(string entityName, string identifierPropertyName, TKey entityIdentifier, string message) 
            : base(message)
        {
            EntityName = entityName;
            IdentifierPropertyName = identifierPropertyName;
            EntityIdentifier = entityIdentifier;
        }

        /// <summary>
        /// Creates an exception with a custom message and inner exception
        /// </summary>
        /// <param name="entityName">The name of the entity type</param>
        /// <param name="identifierPropertyName">The name of the property that caused the conflict</param>
        /// <param name="entityIdentifier">The value that already exists</param>
        /// <param name="message">Custom error message</param>
        /// <param name="innerException">The exception that caused this exception</param>
        public EntityAlreadyExistsException(string entityName, string identifierPropertyName, TKey entityIdentifier, string message, Exception innerException) 
            : base(message, innerException)
        {
            EntityName = entityName;
            IdentifierPropertyName = identifierPropertyName;
            EntityIdentifier = entityIdentifier;
        }
    }
}
