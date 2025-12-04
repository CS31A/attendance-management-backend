
namespace attendance_monitoring.Exceptions
{
    /// <summary>
    /// Generic exception thrown when an operation on an entity is unauthorized
    /// </summary>
    public class EntityUnauthorizedException : Exception
    {
        public string EntityName { get; }
        public string Operation { get; }
        public string UserId { get; }

        /// <summary>
        /// Creates an unauthorized exception with a default message
        /// </summary>
        /// <param name="entityName">The name of the entity type (e.g., "Subject", "Schedule")</param>
        /// <param name="operation">The operation that was attempted (e.g., "Update", "Delete")</param>
        /// <param name="userId">The ID of the user who attempted the operation</param>
        public EntityUnauthorizedException(string entityName, string operation, string userId)
            : base($"User {userId} is not authorized to perform {operation} on {entityName}.")
        {
            EntityName = entityName;
            Operation = operation;
            UserId = userId;
        }

        /// <summary>
        /// Creates an unauthorized exception with a custom message
        /// </summary>
        /// <param name="entityName">The name of the entity type</param>
        /// <param name="operation">The operation that was attempted</param>
        /// <param name="userId">The ID of the user who attempted the operation</param>
        /// <param name="message">Custom error message</param>
        public EntityUnauthorizedException(string entityName, string operation, string userId, string message)
            : base(message)
        {
            EntityName = entityName;
            Operation = operation;
            UserId = userId;
        }

        /// <summary>
        /// Creates an unauthorized exception with a custom message and inner exception
        /// </summary>
        /// <param name="entityName">The name of the entity type</param>
        /// <param name="operation">The operation that was attempted</param>
        /// <param name="userId">The ID of the user who attempted the operation</param>
        /// <param name="message">Custom error message</param>
        /// <param name="innerException">The exception that caused this exception</param>
        public EntityUnauthorizedException(string entityName, string operation, string userId, string message, Exception innerException)
            : base(message, innerException)
        {
            EntityName = entityName;
            Operation = operation;
            UserId = userId;
        }
    }
}
