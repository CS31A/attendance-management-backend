using System.Security.Claims;

namespace attendance_monitoring.IServices;

/// <summary>
/// Interface for user context operations and extracting user information from claims
/// </summary>
public interface IUserContextService
{
    /// <summary>
    /// Extracts user ID from ClaimsPrincipal with multiple fallback strategies
    /// </summary>
    /// <param name="userPrincipal">The user's claims principal</param>
    /// <returns>User ID if found, null otherwise</returns>
    Task<string?> GetUserIdAsync(ClaimsPrincipal userPrincipal);

    /// <summary>
    /// Gets the instructor ID associated with the current user
    /// </summary>
    /// <param name="userPrincipal">The user's claims principal</param>
    /// <returns>Instructor ID if user is an instructor, null otherwise</returns>
    Task<int?> GetInstructorIdAsync(ClaimsPrincipal userPrincipal);

    /// <summary>
    /// Checks if the user is authorized to perform an action on a resource
    /// </summary>
    /// <param name="userPrincipal">The user's claims principal</param>
    /// <param name="resourceUserId">The user ID associated with the resource</param>
    /// <param name="allowedRoles">Roles that can access the resource regardless of ownership</param>
    /// <returns>True if authorized, false otherwise</returns>
    Task<bool> IsAuthorizedAsync(ClaimsPrincipal userPrincipal, string resourceUserId, params string[] allowedRoles);
}
