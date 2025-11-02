using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using attendance_monitoring.IServices;
using attendance_monitoring.Models.DTO.Response;

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
}