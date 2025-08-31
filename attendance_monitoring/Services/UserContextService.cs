using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using Microsoft.AspNetCore.Identity;

namespace attendance_monitoring.Services;

/// <summary>
/// Service to handle user context operations and extract user information from claims
/// </summary>
public class UserContextService
{
    private readonly UserManager<IdentityUser> _userManager;

    public UserContextService(UserManager<IdentityUser> userManager)
    {
        _userManager = userManager ?? throw new ArgumentNullException(nameof(userManager));
    }

    /// <summary>
    /// Extracts user ID from ClaimsPrincipal with multiple fallback strategies
    /// </summary>
    /// <param name="userPrincipal">The user's claims principal</param>
    /// <returns>User ID if found, null otherwise</returns>
    public async Task<string?> GetUserIdAsync(ClaimsPrincipal userPrincipal)
    {
        if (userPrincipal == null)
        {
            return null;
        }

        // Try NameIdentifier claim first (most reliable)
        var userId = userPrincipal.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (!string.IsNullOrEmpty(userId))
        {
            return userId;
        }

        // Try JWT Subject claim
        userId = userPrincipal.FindFirst(JwtRegisteredClaimNames.Sub)?.Value;
        if (!string.IsNullOrEmpty(userId))
        {
            return userId;
        }

        // Fallback: try to get user ID from username
        var username = userPrincipal.FindFirst(ClaimTypes.Name)?.Value;
        if (!string.IsNullOrEmpty(username))
        {
            var user = await _userManager.FindByNameAsync(username).ConfigureAwait(false);
            if (user != null)
            {
                return user.Id;
            }
        }

        return null;
    }

    /// <summary>
    /// Gets the user's role from claims
    /// </summary>
    /// <param name="userPrincipal">The user's claims principal</param>
    /// <returns>User role if found, null otherwise</returns>
    public string? GetUserRole(ClaimsPrincipal userPrincipal)
    {
        return userPrincipal?.FindFirst(ClaimTypes.Role)?.Value;
    }

    /// <summary>
    /// Checks if the user is authorized to perform an action on a resource
    /// </summary>
    /// <param name="userPrincipal">The user's claims principal</param>
    /// <param name="resourceUserId">The user ID associated with the resource</param>
    /// <param name="allowedRoles">Roles that can access the resource regardless of ownership</param>
    /// <returns>True if authorized, false otherwise</returns>
    public async Task<bool> IsAuthorizedAsync(ClaimsPrincipal userPrincipal, string resourceUserId, params string[] allowedRoles)
    {
        var currentUserId = await GetUserIdAsync(userPrincipal).ConfigureAwait(false);
        if (string.IsNullOrEmpty(currentUserId))
        {
            return false;
        }

        // User owns the resource
        if (currentUserId == resourceUserId)
        {
            return true;
        }

        // Check if user has privileged role
        var userRole = GetUserRole(userPrincipal);
        if (!string.IsNullOrEmpty(userRole) && allowedRoles.Contains(userRole, StringComparer.OrdinalIgnoreCase))
        {
            return true;
        }

        return false;
    }
}