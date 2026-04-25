using attendance_monitoring.Exceptions;
using attendance_monitoring.Models.DTO.Response;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.EntityFrameworkCore;

namespace attendance_monitoring.Extensions.WebApplicationExtensions;

/// <summary>
/// Extension methods for configuring global exception handling.
/// </summary>
public static class ExceptionHandlingExtensions
{
    private const string CorrelationHeaderName = "X-Correlation-ID";

    /// <summary>
    /// Configures global exception handler middleware.
    /// Maps exceptions to appropriate HTTP status codes and standardized error responses.
    /// </summary>
    /// <param name="app">The web application to configure.</param>
    /// <returns>The web application for chaining.</returns>
    public static WebApplication UseGlobalExceptionHandler(this WebApplication app)
    {
        app.UseExceptionHandler(errorApp =>
        {
            errorApp.Run(async context =>
            {
                var exceptionHandlerFeature = context.Features.Get<IExceptionHandlerPathFeature>();
                var exception = exceptionHandlerFeature?.Error;
                var logger = context.RequestServices.GetRequiredService<ILogger<Program>>();
                var correlationId = context.TraceIdentifier;

                // Map exceptions to appropriate HTTP status codes and messages
                (int statusCode, string message, bool includeDetails) = exception switch
                {
                    // Custom domain exceptions
                    EntityNotFoundException<int> ex =>
                        (StatusCodes.Status404NotFound, ex.Message, false),

                    EntityNotFoundException<Guid> ex =>
                        (StatusCodes.Status404NotFound, ex.Message, false),

                    EntityNotFoundException<string> ex =>
                        (StatusCodes.Status404NotFound, ex.Message, false),

                    EntityUnauthorizedException ex =>
                        (StatusCodes.Status403Forbidden, ex.Message, false),

                    EntityAlreadyExistsException<int> ex =>
                        (StatusCodes.Status409Conflict, ex.Message, false),

                    EntityAlreadyExistsException<string> ex =>
                        (StatusCodes.Status409Conflict, ex.Message, false),

                    EntityConflictException ex =>
                        (StatusCodes.Status409Conflict, ex.Message, false),

                    // Database exceptions
                    DbUpdateConcurrencyException ex =>
                        (StatusCodes.Status409Conflict,
                         "The record was modified by another user. Please refresh and try again.",
                         true),

                    DbUpdateException ex =>
                        (StatusCodes.Status503ServiceUnavailable,
                         "Database is temporarily unavailable. Please try again later.",
                         true),

                    // Timeout exceptions
                    TimeoutException ex =>
                        (StatusCodes.Status504GatewayTimeout,
                         "The request timed out. Please try again.",
                         true),

                    // Validation exceptions
                    ValidationException ex =>
                        (StatusCodes.Status400BadRequest, ex.Message, false),

                    ArgumentNullException ex =>
                        (StatusCodes.Status400BadRequest,
                         $"Missing required parameter: {ex.ParamName}",
                         false),

                    ArgumentException ex =>
                        (StatusCodes.Status400BadRequest,
                         ex.Message,
                         false),

                    // Service layer exceptions
                    EntityServiceException ex =>
                        (StatusCodes.Status400BadRequest, ex.Message, true),

                    // Unexpected exceptions
                    _ => (StatusCodes.Status500InternalServerError,
                          "An unexpected error occurred. Please contact support if this persists.",
                          true)
                };

                // Log the exception with appropriate severity
                if (statusCode >= 500)
                {
                    logger.LogError(exception,
                        "Unhandled exception: {ExceptionType} | Status: {StatusCode} | Path: {Path} | CorrelationId: {CorrelationId} | Message: {Message}",
                        exception?.GetType().Name, statusCode, context.Request.Path, correlationId, message);
                }
                else
                {
                    logger.LogWarning(exception,
                        "Client error: {ExceptionType} | Status: {StatusCode} | Path: {Path} | CorrelationId: {CorrelationId} | Message: {Message}",
                        exception?.GetType().Name, statusCode, context.Request.Path, correlationId, message);
                }

                // Build error response
                var errorResponse = new ErrorResponseDto
                {
                    Success = false,
                    Message = message,
                    StatusCode = statusCode,
                    Path = context.Request.Path,
                    Timestamp = DateTime.UtcNow
                };

                // Include detailed error information in development
                if (app.Environment.IsDevelopment() && includeDetails && exception != null)
                {
                    errorResponse.Details = exception.ToString();
                }

                context.Response.StatusCode = statusCode;
                context.Response.ContentType = "application/json";
                context.Response.Headers[CorrelationHeaderName] = correlationId;

                await context.Response.WriteAsJsonAsync(errorResponse);
            });
        });

        return app;
    }
}
