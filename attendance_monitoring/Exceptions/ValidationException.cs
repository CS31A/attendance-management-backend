using System;

namespace attendance_monitoring.Exceptions;

/// <summary>
/// Exception thrown when validation fails
/// </summary>
public class ValidationException : Exception
{
    /// <summary>
    /// Creates a validation exception with a message
    /// </summary>
    /// <param name="message">The validation error message</param>
    public ValidationException(string message) : base(message)
    {
    }

    /// <summary>
    /// Creates a validation exception with a message and inner exception
    /// </summary>
    /// <param name="message">The validation error message</param>
    /// <param name="innerException">The inner exception</param>
    public ValidationException(string message, Exception innerException) : base(message, innerException)
    {
    }
}
