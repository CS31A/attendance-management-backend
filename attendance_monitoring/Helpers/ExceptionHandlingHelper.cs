using Microsoft.EntityFrameworkCore;
using attendance_monitoring.Exceptions;

namespace attendance_monitoring.Helpers;

/// <summary>
/// Utility class for handling common exception scenarios across services.
/// Provides centralized methods for detecting database constraint violations
/// and creating appropriate exceptions.
/// </summary>
public static class ExceptionHandlingHelper
{
    /// <summary>
    /// Determines if the exception is caused by a unique constraint violation.
    /// Supports both SQL Server and SQLite database providers.
    /// </summary>
    /// <param name="ex">The DbUpdateException to analyze.</param>
    /// <returns>True if the exception is due to a unique constraint violation; otherwise, false.</returns>
    public static bool IsUniqueConstraintViolation(DbUpdateException ex)
    {
        var innerException = ex.InnerException?.Message ?? ex.Message;

        // SQL Server unique constraint violation (Error 2601, 2627)
        if (innerException.Contains("UNIQUE constraint failed") ||
            innerException.Contains("duplicate key") ||
            innerException.Contains("Cannot insert duplicate key") ||
            innerException.Contains("Violation of UNIQUE KEY constraint") ||
            innerException.Contains("UNIQUE KEY constraint"))
        {
            return true;
        }

        // SQLite unique constraint violation
        if (innerException.Contains("UNIQUE constraint failed"))
        {
            return true;
        }

        return false;
    }

    /// <summary>
    /// Determines if the exception is caused by a foreign key constraint violation.
    /// Supports both SQL Server and SQLite database providers.
    /// </summary>
    /// <param name="ex">The DbUpdateException to analyze.</param>
    /// <returns>True if the exception is due to a foreign key constraint violation; otherwise, false.</returns>
    public static bool IsForeignKeyViolation(DbUpdateException ex)
    {
        var innerException = ex.InnerException?.Message ?? ex.Message;

        // SQL Server foreign key violation
        if (innerException.Contains("FOREIGN KEY constraint") ||
            innerException.Contains("REFERENCE constraint") ||
            innerException.Contains("conflicted with the FOREIGN KEY") ||
            innerException.Contains("The DELETE statement conflicted"))
        {
            return true;
        }

        // SQLite foreign key violation
        if (innerException.Contains("FOREIGN KEY constraint failed"))
        {
            return true;
        }

        return false;
    }

    /// <summary>
    /// Extracts the constraint name from a foreign key violation exception message.
    /// </summary>
    /// <param name="ex">The DbUpdateException to analyze.</param>
    /// <returns>The constraint name if found; otherwise, "unknown constraint".</returns>
    public static string GetForeignKeyViolationMessage(DbUpdateException ex)
    {
        var innerException = ex.InnerException?.Message ?? ex.Message;

        // Try to extract the constraint name from SQL Server error message
        // Pattern: "FK_TableName_RelatedTable"
        var constraintMatch = System.Text.RegularExpressions.Regex.Match(
            innerException,
            @"constraint ""?(FK_\w+)""?|""?(FK_\w+)""? constraint",
            System.Text.RegularExpressions.RegexOptions.IgnoreCase);

        if (constraintMatch.Success)
        {
            var constraintName = constraintMatch.Groups[1].Success
                ? constraintMatch.Groups[1].Value
                : constraintMatch.Groups[2].Value;
            return $"Cannot delete due to related records (constraint: {constraintName})";
        }

        // Try to extract table name from error message
        var tableMatch = System.Text.RegularExpressions.Regex.Match(
            innerException,
            @"table ""?(\w+)""?",
            System.Text.RegularExpressions.RegexOptions.IgnoreCase);

        if (tableMatch.Success)
        {
            return $"Cannot delete because it has related records in {tableMatch.Groups[1].Value}";
        }

        return "Cannot delete because it has related records in other tables";
    }

    /// <summary>
    /// Creates a standardized EntityServiceException for service operations.
    /// </summary>
    /// <param name="entityName">The name of the entity being operated on.</param>
    /// <param name="operation">The operation being performed.</param>
    /// <param name="ex">The underlying exception.</param>
    /// <returns>A new EntityServiceException with standardized messaging.</returns>
    public static EntityServiceException CreateServiceException(string entityName, string operation, Exception ex)
    {
        return new EntityServiceException(
            entityName,
            operation,
            $"An error occurred while performing {operation} on {entityName}",
            ex);
    }

    /// <summary>
    /// Creates a standardized EntityServiceException for service operations with a custom message.
    /// </summary>
    /// <param name="entityName">The name of the entity being operated on.</param>
    /// <param name="operation">The operation being performed.</param>
    /// <param name="message">Custom error message.</param>
    /// <param name="ex">The underlying exception (optional).</param>
    /// <returns>A new EntityServiceException with the specified message.</returns>
    public static EntityServiceException CreateServiceException(string entityName, string operation, string message, Exception? ex = null)
    {
        return ex != null
            ? new EntityServiceException(entityName, operation, message, ex)
            : new EntityServiceException(entityName, operation, message);
    }
}
