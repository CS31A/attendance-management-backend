using attendance_monitoring.Classes;
using attendance_monitoring.Constants;
using attendance_monitoring.IServices;
using attendance_monitoring.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace attendance_monitoring.Controllers;

/// <summary>
/// Controller for managing notification preferences
/// </summary>
[ApiController]
[Route("api/[controller]")]
[Authorize(Roles = RoleConstants.Instructor)]
public class NotificationPreferenceController : ControllerBase
{
    private readonly INotificationPreferenceService _preferenceService;
    private readonly IUserContextService _userContextService;
    private readonly ILogger<NotificationPreferenceController> _logger;

    public NotificationPreferenceController(
        INotificationPreferenceService preferenceService,
        IUserContextService userContextService,
        ILogger<NotificationPreferenceController> logger)
    {
        _preferenceService = preferenceService ?? throw new ArgumentNullException(nameof(preferenceService));
        _userContextService = userContextService ?? throw new ArgumentNullException(nameof(userContextService));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <summary>
    /// Get the current instructor's real-time check-in notification preference
    /// </summary>
    /// <returns>The current notification preference setting</returns>
    /// <response code="200">Returns the notification preference</response>
    /// <response code="401">Unauthorized</response>
    /// <response code="500">Internal server error</response>
    [HttpGet("realtime-checkin")]
    [ProducesResponseType(typeof(NotificationPreferenceResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<NotificationPreferenceResponse>> GetRealtimeCheckInPreference()
    {
        try
        {
            var userId = await _userContextService.GetUserIdAsync(User).ConfigureAwait(false);
            if (string.IsNullOrEmpty(userId))
            {
                _logger.LogWarning("User ID not found in claims");
                return Unauthorized("User ID not found");
            }

            var enabled = await _preferenceService.GetRealtimeCheckInAsync(userId).ConfigureAwait(false);

            _logger.LogInformation("Retrieved realtime check-in preference for user {UserId}: {Enabled}", userId, enabled);

            return Ok(new NotificationPreferenceResponse
            {
                Enabled = enabled,
                Message = enabled
                    ? "You will receive real-time notifications when students check in"
                    : "Real-time check-in notifications are disabled"
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving notification preference");
            return StatusCode(500, "An error occurred while retrieving notification preference");
        }
    }

    /// <summary>
    /// Update the current instructor's real-time check-in notification preference
    /// </summary>
    /// <param name="request">The preference update request</param>
    /// <returns>The updated notification preference</returns>
    /// <response code="200">Preference updated successfully</response>
    /// <response code="400">Invalid request</response>
    /// <response code="401">Unauthorized</response>
    /// <response code="500">Internal server error</response>
    [HttpPut("realtime-checkin")]
    [ProducesResponseType(typeof(NotificationPreferenceResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<NotificationPreferenceResponse>> UpdateRealtimeCheckInPreference(
        [FromBody] UpdateNotificationPreferenceRequest request)
    {
        try
        {
            if (request == null)
            {
                return BadRequest("Request body is required");
            }

            var userId = await _userContextService.GetUserIdAsync(User).ConfigureAwait(false);
            if (string.IsNullOrEmpty(userId))
            {
                _logger.LogWarning("User ID not found in claims");
                return Unauthorized("User ID not found");
            }

            await _preferenceService.SetRealtimeCheckInAsync(userId, request.Enabled).ConfigureAwait(false);

            _logger.LogInformation("Updated realtime check-in preference for user {UserId}: {Enabled}", userId, request.Enabled);

            return Ok(new NotificationPreferenceResponse
            {
                Enabled = request.Enabled,
                Message = request.Enabled
                    ? "You will now receive real-time notifications when students check in"
                    : "Real-time check-in notifications have been disabled"
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating notification preference");
            return StatusCode(500, "An error occurred while updating notification preference");
        }
    }
}

/// <summary>
/// Request model for updating notification preferences
/// </summary>
public record UpdateNotificationPreferenceRequest
{
    /// <summary>
    /// Whether the notification preference is enabled
    /// </summary>
    public bool Enabled { get; init; }
}

/// <summary>
/// Response model for notification preferences
/// </summary>
public record NotificationPreferenceResponse
{
    /// <summary>
    /// Whether the notification preference is enabled
    /// </summary>
    public bool Enabled { get; init; }

    /// <summary>
    /// A descriptive message about the current preference state
    /// </summary>
    public string Message { get; init; } = string.Empty;
}
