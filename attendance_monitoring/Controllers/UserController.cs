using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using attendance_monitoring.IServices;
using attendance_monitoring.Models.DTO.Response;
using System.Security.Claims;

namespace attendance_monitoring.Controllers;

[ApiController]
[Route("api/users")]
[Authorize(Policy = "AdminPolicy")]
public class UserController(IAccountService accountService, ILogger<UserController> logger) : ControllerBase
{
    /// <summary>
    /// Get all users with their role and profile information
    /// </summary>
    /// <returns>List of all users with role-specific details</returns>
    [HttpGet]
    public async Task<ActionResult<IEnumerable<GetAllUsersDto>>> GetAllUsers()
    {
        try
        {
            logger.LogInformation("Getting all users");
            var users = await accountService.GetAllUsersAsync();
            var usersList = users.ToList();
            logger.LogInformation("Successfully retrieved {Count} users", usersList.Count);
            return Ok(usersList);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error retrieving all users");
            return StatusCode(500, new { Success = false, Message = "An error occurred while retrieving users" });
        }
    }

    /// <summary>
    /// Soft delete a user (marks as deleted without removing from database)
    /// </summary>
    /// <param name="userId">Target user ID to soft delete</param>
    /// <returns>Deletion status</returns>
    /// <response code="200">User soft deleted successfully</response>
    /// <response code="400">Invalid request or cannot delete self</response>
    /// <response code="401">User not authenticated</response>
    /// <response code="403">User is not an admin</response>
    /// <response code="404">Target user not found</response>
    [HttpPatch("{userId}/soft-delete")]
    [ProducesResponseType(typeof(DeleteUserResponseDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(DeleteUserResponseDto), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(DeleteUserResponseDto), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(DeleteUserResponseDto), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(DeleteUserResponseDto), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<DeleteUserResponseDto>> SoftDeleteUser(string userId)
    {
        logger.LogInformation("User delete request received for user {TargetUserId}.", userId);

        if (string.IsNullOrWhiteSpace(userId))
        {
            logger.LogWarning("User delete failed: userId is empty");
            return BadRequest(new DeleteUserResponseDto { Success = false, Message = "User ID is required" });
        }

        var adminId = GetUserId(User);
        if (string.IsNullOrEmpty(adminId))
        {
            logger.LogWarning("User delete failed: Admin not found from claims.");
            return Unauthorized(new DeleteUserResponseDto { Success = false, Message = "Admin not authenticated" });
        }

        var (success, message) = await accountService.AdminDeleteUserAsync(adminId, userId);

        if (!success)
        {
            logger.LogWarning("User delete failed for user {TargetUserId}: {Message}", userId, message);

            // Return appropriate status code based on error message
            if (message.Contains("not found", StringComparison.OrdinalIgnoreCase))
            {
                return NotFound(new DeleteUserResponseDto { Success = false, Message = message });
            }

            if (message.Contains("Unauthorized", StringComparison.OrdinalIgnoreCase) ||
                message.Contains("Admin role required", StringComparison.OrdinalIgnoreCase))
            {
                return StatusCode(StatusCodes.Status403Forbidden, new DeleteUserResponseDto { Success = false, Message = message });
            }

            // Generic bad request for other errors (e.g., cannot delete self, already deleted)
            return BadRequest(new DeleteUserResponseDto { Success = false, Message = message });
        }

        logger.LogInformation("Admin {AdminId} successfully deleted user {TargetUserId}.", adminId, userId);
        return Ok(new DeleteUserResponseDto
        {
            Success = true,
            Message = message
        });
    }

    private string? GetUserId(ClaimsPrincipal userPrincipal)
    {
        return userPrincipal.FindFirst(ClaimTypes.NameIdentifier)?.Value;
    }
}