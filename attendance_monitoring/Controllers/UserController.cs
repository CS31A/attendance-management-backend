using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using attendance_monitoring.IServices;
using attendance_monitoring.Models.DTO.Response;
using attendance_monitoring.Models.DTO.Request;
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
    /// <param name="status">Filter by user status (Active, Archived, All). Default is Active.</param>
    /// <returns>List of all users with role-specific details</returns>
    [HttpGet]
    public async Task<ActionResult<IEnumerable<GetAllUsersDto>>> GetAllUsers([FromQuery] UserStatus status = UserStatus.Active)
    {
        try
        {
            logger.LogInformation("Getting all users with status filter: {Status}", status);
            var users = await accountService.GetAllUsersAsync(status);
            var usersList = users.ToList();
            logger.LogInformation("Successfully retrieved {Count} users with status: {Status}", usersList.Count, status);
            return Ok(usersList);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error retrieving all users with status: {Status}", status);
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

        var (success, message, errorCode) = await accountService.AdminDeleteUserAsync(adminId, userId);

        if (!success)
        {
            logger.LogWarning("User delete failed for user {TargetUserId}: {Message} (ErrorCode: {ErrorCode})", userId, message, errorCode);

            // Return appropriate status code based on error code
            return errorCode switch
            {
                "USER_NOT_FOUND" => NotFound(new DeleteUserResponseDto { Success = false, Message = message }),
                "UNAUTHORIZED" => StatusCode(StatusCodes.Status403Forbidden, new DeleteUserResponseDto { Success = false, Message = message }),
                "SELF_DELETE" => BadRequest(new DeleteUserResponseDto { Success = false, Message = message }),
                _ => BadRequest(new DeleteUserResponseDto { Success = false, Message = message })
            };
        }

        logger.LogInformation("Admin {AdminId} successfully deleted user {TargetUserId}.", adminId, userId);
        return Ok(new DeleteUserResponseDto
        {
            Success = true,
            Message = message
        });
    }

    /// <summary>
    /// Permanently delete a user and all associated data (hard delete - cannot be undone)
    /// </summary>
    /// <param name="userId">Target user ID to permanently delete</param>
    /// <returns>Deletion status</returns>
    /// <response code="200">User permanently deleted successfully</response>
    /// <response code="400">Invalid request or cannot delete self</response>
    /// <response code="401">User not authenticated</response>
    /// <response code="403">User is not an admin</response>
    /// <response code="404">Target user not found</response>
    [HttpDelete("{userId}")]
    [ProducesResponseType(typeof(DeleteUserResponseDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(DeleteUserResponseDto), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(DeleteUserResponseDto), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(DeleteUserResponseDto), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(DeleteUserResponseDto), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<DeleteUserResponseDto>> HardDeleteUser(string userId)
    {
        logger.LogInformation("User hard delete request received for user {TargetUserId}.", userId);

        if (string.IsNullOrWhiteSpace(userId))
        {
            logger.LogWarning("User hard delete failed: userId is empty");
            return BadRequest(new DeleteUserResponseDto { Success = false, Message = "User ID is required" });
        }

        var adminId = GetUserId(User);
        if (string.IsNullOrEmpty(adminId))
        {
            logger.LogWarning("User hard delete failed: Admin not found from claims.");
            return Unauthorized(new DeleteUserResponseDto { Success = false, Message = "Admin not authenticated" });
        }

        var (success, message, errorCode) = await accountService.AdminHardDeleteUserAsync(adminId, userId);

        if (!success)
        {
            logger.LogWarning("User hard delete failed for user {TargetUserId}: {Message} (ErrorCode: {ErrorCode})", userId, message, errorCode);

            // Return appropriate status code based on error code
            return errorCode switch
            {
                "USER_NOT_FOUND" => NotFound(new DeleteUserResponseDto { Success = false, Message = message }),
                "UNAUTHORIZED" => StatusCode(StatusCodes.Status403Forbidden, new DeleteUserResponseDto { Success = false, Message = message }),
                "SELF_DELETE" => BadRequest(new DeleteUserResponseDto { Success = false, Message = message }),
                _ => BadRequest(new DeleteUserResponseDto { Success = false, Message = message })
            };
        }

        logger.LogInformation("Admin {AdminId} successfully hard deleted user {TargetUserId}.", adminId, userId);
        return Ok(new DeleteUserResponseDto
        {
            Success = true,
            Message = message
        });
    }

    /// <summary>
    /// Restore a soft-deleted user (reactivates archived user)
    /// </summary>
    /// <param name="userId">Target user ID to restore</param>
    /// <returns>Restoration status</returns>
    /// <response code="200">User restored successfully</response>
    /// <response code="400">Invalid request</response>
    /// <response code="401">User not authenticated</response>
    /// <response code="403">User is not an admin</response>
    /// <response code="404">Target user not found or not deleted</response>
    [HttpPatch("{userId}/restore")]
    [ProducesResponseType(typeof(DeleteUserResponseDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(DeleteUserResponseDto), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(DeleteUserResponseDto), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(DeleteUserResponseDto), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(DeleteUserResponseDto), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<DeleteUserResponseDto>> RestoreUser(string userId)
    {
        logger.LogInformation("User restore request received for user {TargetUserId}.", userId);

        if (string.IsNullOrWhiteSpace(userId))
        {
            logger.LogWarning("User restore failed: userId is empty");
            return BadRequest(new DeleteUserResponseDto { Success = false, Message = "User ID is required" });
        }

        var adminId = GetUserId(User);
        if (string.IsNullOrEmpty(adminId))
        {
            logger.LogWarning("User restore failed: Admin not found from claims.");
            return Unauthorized(new DeleteUserResponseDto { Success = false, Message = "Admin not authenticated" });
        }

        var (success, message, errorCode) = await accountService.AdminRestoreUserAsync(adminId, userId);

        if (!success)
        {
            logger.LogWarning("User restore failed for user {TargetUserId}: {Message} (ErrorCode: {ErrorCode})", userId, message, errorCode);

            // Return appropriate status code based on error code
            return errorCode switch
            {
                "USER_NOT_FOUND" => NotFound(new DeleteUserResponseDto { Success = false, Message = message }),
                "UNAUTHORIZED" => StatusCode(StatusCodes.Status403Forbidden, new DeleteUserResponseDto { Success = false, Message = message }),
                "NOT_DELETED" => BadRequest(new DeleteUserResponseDto { Success = false, Message = message }),
                _ => BadRequest(new DeleteUserResponseDto { Success = false, Message = message })
            };
        }

        logger.LogInformation("Admin {AdminId} successfully restored user {TargetUserId}.", adminId, userId);
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