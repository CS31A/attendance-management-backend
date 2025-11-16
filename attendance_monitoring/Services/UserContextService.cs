using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using attendance_monitoring.Data;

namespace attendance_monitoring.Services;

/// <summary>
/// Service to handle user context operations and extract user information from claims
/// </summary>
public class UserContextService(UserManager<IdentityUser> userManager, ApplicationDbContext context)
{
    private readonly UserManager<IdentityUser> _userManager = userManager ?? throw new ArgumentNullException(nameof(userManager));
    private readonly ApplicationDbContext _context = context ?? throw new ArgumentNullException(nameof(context));

    private static readonly HashSet<string> ValidRoles = new(StringComparer.OrdinalIgnoreCase)
    {
        "Student", "Teacher", "Admin", "Instructor"
    };

    // Equivalent na ani, basin malimot ka
    // private static readonly HashSet<string> ValidRoles = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
    // {
    //     "Student", "Teacher", "Admin"
    // };

    #region User Information Operations
    /// <summary>
    /// Extracts user ID from ClaimsPrincipal with multiple fallback strategies
    /// </summary>
    /// <param name="userPrincipal">The user's claims principal</param>
    /// <returns>User ID if found, null otherwise</returns>
    public async Task<string?> GetUserIdAsync(ClaimsPrincipal userPrincipal)
    {
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
        if (string.IsNullOrEmpty(username)) return null;
        var user = await _userManager.FindByNameAsync(username).ConfigureAwait(false);
        return user?.Id;
    }

    /// <summary>
    /// Gets the user's role from claims
    /// </summary>
    /// <param name="userPrincipal">The user's claims principal</param>
    /// <returns>User role if found, null otherwise</returns>
    private static string? GetUserRole(ClaimsPrincipal userPrincipal)
    {
        return userPrincipal?.FindFirst(ClaimTypes.Role)?.Value;
    }

    #endregion

    #region Instructor Operations
    /// <summary>
    /// Gets the instructor ID associated with the current user
    /// </summary>
    /// <param name="userPrincipal">The user's claims principal</param>
    /// <returns>Instructor ID if user is an instructor, null otherwise</returns>
    public async Task<int?> GetInstructorIdAsync(ClaimsPrincipal userPrincipal)
    {
        var userId = await GetUserIdAsync(userPrincipal).ConfigureAwait(false);
        if (string.IsNullOrEmpty(userId))
        {
            return null;
        }

        var instructor = await _context.Instructors
            .AsNoTracking()
            .Where(i => i.UserId == userId && !i.IsDeleted)
            .Select(i => i.Id)
            .FirstOrDefaultAsync()
            .ConfigureAwait(false);

        return instructor == 0 ? null : instructor;
    }
    #endregion

    #region Authorization Operations
    /// <summary>
    /// Checks if the user is authorized to perform an action on a resource
    /// </summary>
    /// <param name="userPrincipal">The user's claims principal</param>
    /// <param name="resourceUserId">The user ID associated with the resource</param>
    /// <param name="allowedRoles">Roles that can access the resource regardless of ownership</param>
    /// <returns>True if authorized, false otherwise</returns>
    public async Task<bool> IsAuthorizedAsync(ClaimsPrincipal userPrincipal, string resourceUserId, params string[] allowedRoles)
    {
        // Validate allowedRoles parameter
        foreach (var role in allowedRoles)
        {
            if (!string.IsNullOrEmpty(role) && !ValidRoles.Contains(role))
            {
                throw new ArgumentException($"Invalid role specified: {role}. Valid roles are: {string.Join(", ", ValidRoles)}");
            }
        }

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
        return !string.IsNullOrEmpty(userRole) && allowedRoles.Contains(userRole, StringComparer.OrdinalIgnoreCase);
    }
    #endregion
}
