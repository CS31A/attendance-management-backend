using attendance_monitoring.Classes;
using attendance_monitoring.Constants;
using attendance_monitoring.Data;
using attendance_monitoring.Exceptions;
using attendance_monitoring.IRepository;
using attendance_monitoring.Models.DTO;
using attendance_monitoring.Models.DTO.Request;
using attendance_monitoring.Models.DTO.Response;
using Microsoft.EntityFrameworkCore;

namespace attendance_monitoring.Services.Account;

/// <summary>
/// Focused unit responsible for admin operations on user accounts.
/// Handles user listing, admin profile updates, soft/hard deletes, and restores.
/// </summary>
internal sealed class AdminService
{
    private readonly ApplicationDbContext _context;
    private readonly IAccountRepository _accountRepository;
    private readonly ISectionRepository _sectionRepository;
    private readonly ProfileService _profileService;
    private readonly ILogger<AdminService> _logger;

    public AdminService(
        ApplicationDbContext context,
        IAccountRepository accountRepository,
        ISectionRepository sectionRepository,
        ProfileService profileService,
        ILogger<AdminService> logger)
    {
        _context = context ?? throw new ArgumentNullException(nameof(context));
        _accountRepository = accountRepository ?? throw new ArgumentNullException(nameof(accountRepository));
        _sectionRepository = sectionRepository ?? throw new ArgumentNullException(nameof(sectionRepository));
        _profileService = profileService ?? throw new ArgumentNullException(nameof(profileService));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <summary>
    /// Retrieves all users filtered by status.
    /// </summary>
    public async Task<IEnumerable<GetAllUsersDto>> GetAllUsersAsync(UserStatus status = UserStatus.Active)
    {
        return await _accountRepository.GetAllUsersAsyncSP(status).ConfigureAwait(false);
    }

    /// <summary>
    /// Admin updates another user's profile information.
    /// </summary>
    /// <param name="adminId">The ID of the admin performing the update</param>
    /// <param name="adminUpdateDto">The profile update data including target user ID</param>
    /// <returns>Updated user profile</returns>
    /// <exception cref="EntityNotFoundException{String}">Thrown when admin or target user is not found</exception>
    /// <exception cref="EntityUnauthorizedException">Thrown when user is not an admin</exception>
    /// <exception cref="ValidationException">Thrown when validation fails</exception>
    /// <exception cref="EntityAlreadyExistsException{String}">Thrown when email is already in use</exception>
    public async Task<UserProfileResponseDto> AdminUpdateUserProfileAsync(
        string adminId,
        AdminUpdateUser adminUpdateDto)
    {
        return await ExecuteInTransactionAsync(async () =>
        {
            _logger.LogInformation("Admin profile update attempt by admin {AdminId} for user {TargetUserId}", adminId, adminUpdateDto.UserId);

        // Verify admin has Admin role
        var admin = await _accountRepository.FindUserByIdAsync(adminId).ConfigureAwait(false);
        if (admin == null)
        {
            _logger.LogWarning("Admin profile update failed: Admin not found for ID {AdminId}", adminId);
            throw new EntityNotFoundException<string>("Admin", adminId, "Admin user not found");
        }

        var adminRoles = await _accountRepository.GetUserRolesAsync(admin).ConfigureAwait(false);
        if (!adminRoles.Contains("Admin", StringComparer.OrdinalIgnoreCase))
        {
            _logger.LogWarning("Admin profile update failed: User {AdminId} is not an admin", adminId);
            throw new EntityUnauthorizedException("User", "update", adminId, "Admin role required");
        }

        // Validate target user exists
        var targetUser = await _accountRepository.FindUserByIdAsync(adminUpdateDto.UserId).ConfigureAwait(false);
        if (targetUser == null)
        {
            _logger.LogWarning("Admin profile update failed: Target user not found for ID {TargetUserId}", adminUpdateDto.UserId);
            throw new EntityNotFoundException<string>("User", adminUpdateDto.UserId, "Target user not found");
        }

        // Get target user role
        var targetRoles = await _accountRepository.GetUserRolesAsync(targetUser).ConfigureAwait(false);
        var targetRole = targetRoles?.FirstOrDefault();
        if (string.IsNullOrEmpty(targetRole))
        {
            _logger.LogWarning("Target user (ID: {TargetUserId}) has no assigned roles during admin profile update.", targetUser.Id);
            targetRole = "Unknown";
        }

        // Validate email uniqueness if email is being changed
        if (!string.IsNullOrEmpty(adminUpdateDto.Email) &&
            !adminUpdateDto.Email.Equals(targetUser.Email, StringComparison.OrdinalIgnoreCase))
        {
            var emailExists = await _accountRepository.EmailExistsAsync(adminUpdateDto.Email, adminUpdateDto.UserId).ConfigureAwait(false);
            if (emailExists)
            {
                _logger.LogWarning("Admin profile update failed: Email {Email} already exists", adminUpdateDto.Email);
                throw new EntityAlreadyExistsException<string>("User", "Email", adminUpdateDto.Email);
            }

            // Update email
            targetUser.Email = adminUpdateDto.Email;
            targetUser.NormalizedEmail = adminUpdateDto.Email.ToUpperInvariant();
        }

        // Admin password reset (no current password required)
        if (!string.IsNullOrEmpty(adminUpdateDto.NewPassword))
        {
            var resetResult = await _accountRepository.AdminResetPasswordAsync(targetUser, adminUpdateDto.NewPassword).ConfigureAwait(false);

            if (!resetResult.Succeeded)
            {
                var errors = string.Join("; ", resetResult.Errors.Select(e => e.Description));
                _logger.LogWarning("Admin profile update failed: Password reset error - {Errors}", errors);
                throw new ValidationException($"Password reset failed: {errors}");
            }

            _logger.LogInformation("Admin {AdminId} reset password for user {TargetUserId}", adminId, adminUpdateDto.UserId);
        }

        // Update user in Identity
        try
        {
            var updateResult = await _accountRepository.UpdateUserAsync(targetUser).ConfigureAwait(false);
            if (!updateResult.Succeeded)
            {
                var errors = string.Join("; ", updateResult.Errors.Select(e => e.Description));
                _logger.LogWarning("Admin profile update failed: User update error - {Errors}", errors);
                throw new ValidationException($"Profile update failed: {errors}");
            }
        }
        catch (Microsoft.EntityFrameworkCore.DbUpdateException ex) when (ex.InnerException?.Message.Contains("duplicate") == true ||
                                                                       ex.InnerException?.Message.Contains("unique") == true ||
                                                                       ex.InnerException?.Message.Contains("IX_AspNetUsers_NormalizedEmail") == true)
        {
            _logger.LogWarning("Admin profile update failed: Email already exists for another user - {Email}", adminUpdateDto.Email);
            throw new EntityAlreadyExistsException<string>("User", "Email", adminUpdateDto.Email ?? "");
        }

        // Update role-specific profile
        if (targetRole.Equals("Student", StringComparison.OrdinalIgnoreCase))
        {
            var student = await _accountRepository.GetStudentByUserIdAsync(adminUpdateDto.UserId).ConfigureAwait(false);
            if (student != null)
            {
                if (!string.IsNullOrWhiteSpace(adminUpdateDto.Firstname))
                {
                    student.Firstname = adminUpdateDto.Firstname;
                }
                else if (adminUpdateDto.Firstname != null)
                {
                    _logger.LogWarning("Admin profile update failed: Firstname is required for students");
                    throw new ValidationException("Firstname is required and cannot be empty or whitespace");
                }

                if (!string.IsNullOrWhiteSpace(adminUpdateDto.Lastname))
                {
                    student.Lastname = adminUpdateDto.Lastname;
                }
                else if (adminUpdateDto.Lastname != null)
                {
                    _logger.LogWarning("Admin profile update failed: Lastname is required for students");
                    throw new ValidationException("Lastname is required and cannot be empty or whitespace");
                }
                if (adminUpdateDto.SectionId.HasValue)
                {
                    Section? section;
                    try
                    {
                        section = await _sectionRepository.GetSectionByIdAsync(adminUpdateDto.SectionId.Value).ConfigureAwait(false);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex,
                            "Admin profile update failed for target user {TargetUserId}: Section lookup failed for SectionId {SectionId}",
                            adminUpdateDto.UserId,
                            adminUpdateDto.SectionId.Value);
                        throw;
                    }

                    if (section == null)
                    {
                        _logger.LogWarning("Admin profile update failed: Section {SectionId} does not exist", adminUpdateDto.SectionId.Value);
                        throw new EntityNotFoundException<int>("Section", adminUpdateDto.SectionId.Value);
                    }
                    student.SectionId = adminUpdateDto.SectionId.Value;
                }
                if (adminUpdateDto.IsRegular.HasValue)
                {
                    student.IsRegular = adminUpdateDto.IsRegular.Value;
                }
                if (adminUpdateDto.IsDeleted.HasValue)
                {
                    student.IsDeleted = adminUpdateDto.IsDeleted.Value;
                    student.DeletedAt = adminUpdateDto.IsDeleted.Value ? DateTime.UtcNow : null;
                }

                await _accountRepository.UpdateStudentProfileAsync(student).ConfigureAwait(false);
            }
        }
        else if (targetRole.Equals(RoleConstants.Instructor, StringComparison.OrdinalIgnoreCase))
        {
            var instructor = await _accountRepository.GetInstructorByUserIdAsync(adminUpdateDto.UserId).ConfigureAwait(false);
            if (instructor != null)
            {
                if (!string.IsNullOrEmpty(adminUpdateDto.Firstname))
                {
                    instructor.Firstname = adminUpdateDto.Firstname;
                }
                if (!string.IsNullOrEmpty(adminUpdateDto.Lastname))
                {
                    instructor.Lastname = adminUpdateDto.Lastname;
                }
                if (adminUpdateDto.IsDeleted.HasValue)
                {
                    instructor.IsDeleted = adminUpdateDto.IsDeleted.Value;
                    instructor.DeletedAt = adminUpdateDto.IsDeleted.Value ? DateTime.UtcNow : null;
                }

                await _accountRepository.UpdateInstructorProfileAsync(instructor).ConfigureAwait(false);
            }
        }

        // Save all changes
        await _accountRepository.SaveChangesAsync().ConfigureAwait(false);

        _logger.LogInformation("Admin {AdminId} successfully updated profile for user {TargetUserId}", adminId, adminUpdateDto.UserId);

            // Return updated profile
            return await _profileService.GetUserProfileAsync(adminUpdateDto.UserId).ConfigureAwait(false);
        }).ConfigureAwait(false);
    }

    /// <summary>
    /// Admin deletes a user (soft delete).
    /// </summary>
    /// <param name="adminId">The ID of the admin performing the deletion</param>
    /// <param name="targetUserId">The ID of the user to delete</param>
    /// <exception cref="EntityNotFoundException{String}">Thrown when admin or target user is not found</exception>
    /// <exception cref="EntityUnauthorizedException">Thrown when user is not an admin</exception>
    /// <exception cref="ValidationException">Thrown when trying to delete self</exception>
    /// <exception cref="EntityServiceException">Thrown when deletion fails</exception>
    public async Task AdminDeleteUserAsync(string adminId, string targetUserId)
    {
        await ExecuteInTransactionAsync(async () =>
        {
            _logger.LogInformation("Admin delete user attempt by admin {AdminId} for user {TargetUserId}", adminId, targetUserId);

            var admin = await _accountRepository.FindUserByIdAsync(adminId).ConfigureAwait(false);
            if (admin == null)
            {
                _logger.LogWarning("Admin delete failed: Admin not found for ID {AdminId}", adminId);
                throw new EntityNotFoundException<string>("Admin", adminId, "Admin user not found");
            }

            var adminRoles = await _accountRepository.GetUserRolesAsync(admin).ConfigureAwait(false);
            if (!adminRoles.Contains("Admin", StringComparer.OrdinalIgnoreCase))
            {
                _logger.LogWarning("Admin delete failed: User {AdminId} is not an admin", adminId);
                throw new EntityUnauthorizedException("User", "delete", adminId, "Admin role required");
            }

            if (adminId.Equals(targetUserId, StringComparison.OrdinalIgnoreCase))
            {
                _logger.LogWarning("Admin {AdminId} attempted to delete themselves", adminId);
                throw new ValidationException("Cannot delete your own account");
            }

            var targetUser = await _accountRepository.FindUserByIdAsync(targetUserId).ConfigureAwait(false);
            if (targetUser == null)
            {
                _logger.LogWarning("Admin delete failed: Target user not found for ID {TargetUserId}", targetUserId);
                throw new EntityNotFoundException<string>("User", targetUserId, "Target user not found");
            }

            (bool success, string message) deleteResult;
            try
            {
                deleteResult = await _accountRepository.DeleteUserAsyncSP(targetUserId).ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex,
                    "Admin {AdminId} encountered an unexpected error while deleting user {TargetUserId}",
                    adminId,
                    targetUserId);
                throw new EntityServiceException("User", "delete", "Failed to delete user due to a database error", ex);
            }

            var (success, message) = deleteResult;
            if (!success)
            {
                _logger.LogWarning("Admin {AdminId} failed to delete user {TargetUserId}: {Message}", adminId, targetUserId, message);
                throw new EntityServiceException("User", "delete", message);
            }

            try
            {
                var activeRefreshTokens = await _context.RefreshTokens
                    .Where(rt => rt.UserId == targetUserId && !rt.IsRevoked && rt.ExpiresAt > DateTime.UtcNow)
                    .ToListAsync().ConfigureAwait(false);

                foreach (var token in activeRefreshTokens)
                {
                    token.IsRevoked = true;
                    token.RevokedAt = DateTime.UtcNow;
                }

                if (activeRefreshTokens.Count > 0)
                {
                    await _context.SaveChangesAsync().ConfigureAwait(false);
                    _logger.LogInformation("Revoked {TokenCount} active refresh tokens for deleted user {TargetUserId}", activeRefreshTokens.Count, targetUserId);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to revoke refresh tokens for deleted user {TargetUserId}", targetUserId);
                throw new EntityServiceException("User", "delete", "Failed to revoke refresh tokens for deleted user", ex);
            }

            _logger.LogInformation("Admin {AdminId} successfully deleted user {TargetUserId}", adminId, targetUserId);
        }).ConfigureAwait(false);
    }

    /// <summary>
    /// Admin permanently deletes a user and all associated data (hard delete).
    /// </summary>
    /// <param name="adminId">The ID of the admin performing the deletion</param>
    /// <param name="targetUserId">The ID of the user to permanently delete</param>
    /// <exception cref="EntityNotFoundException{String}">Thrown when admin or target user is not found</exception>
    /// <exception cref="EntityUnauthorizedException">Thrown when user is not an admin</exception>
    /// <exception cref="ValidationException">Thrown when trying to delete self</exception>
    /// <exception cref="EntityServiceException">Thrown when deletion fails</exception>
    public async Task AdminHardDeleteUserAsync(string adminId, string targetUserId)
    {
        _logger.LogInformation("Admin hard delete user attempt by admin {AdminId} for user {TargetUserId}", adminId, targetUserId);

        // Verify admin has Admin role
        var admin = await _accountRepository.FindUserByIdAsync(adminId).ConfigureAwait(false);
        if (admin == null)
        {
            _logger.LogWarning("Admin hard delete failed: Admin not found for ID {AdminId}", adminId);
            throw new EntityNotFoundException<string>("Admin", adminId, "Admin user not found");
        }

        var adminRoles = await _accountRepository.GetUserRolesAsync(admin).ConfigureAwait(false);
        if (!adminRoles.Contains("Admin", StringComparer.OrdinalIgnoreCase))
        {
            _logger.LogWarning("Admin hard delete failed: User {AdminId} is not an admin", adminId);
            throw new EntityUnauthorizedException("User", "hard delete", adminId, "Admin role required");
        }

        // Prevent admin from deleting themselves
        if (adminId.Equals(targetUserId, StringComparison.OrdinalIgnoreCase))
        {
            _logger.LogWarning("Admin {AdminId} attempted to hard delete themselves", adminId);
            throw new ValidationException("Cannot delete your own account");
        }

        // Validate target user exists
        var targetUser = await _accountRepository.FindUserByIdAsync(targetUserId).ConfigureAwait(false);
        if (targetUser == null)
        {
            _logger.LogWarning("Admin hard delete failed: Target user not found for ID {TargetUserId}", targetUserId);
            throw new EntityNotFoundException<string>("User", targetUserId, "Target user not found");
        }

        // Perform hard delete using stored procedure
        (bool success, string message) deleteResult;
        try
        {
            deleteResult = await _accountRepository.HardDeleteUserAsyncSP(targetUserId).ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex,
                "Admin {AdminId} encountered an unexpected error while hard deleting user {TargetUserId}",
                adminId,
                targetUserId);
            throw new EntityServiceException("User", "hard delete", "Failed to permanently delete user due to a database error", ex);
        }

        var (success, message) = deleteResult;

        if (!success)
        {
            _logger.LogWarning("Admin {AdminId} failed to hard delete user {TargetUserId}: {Message}", adminId, targetUserId, message);
            throw new EntityServiceException("User", "hard delete", message);
        }

        _logger.LogInformation("Admin {AdminId} successfully hard deleted user {TargetUserId}", adminId, targetUserId);
    }

    /// <summary>
    /// Admin restores a soft-deleted user (reactivates archived user).
    /// </summary>
    /// <param name="adminId">The ID of the admin performing the restoration</param>
    /// <param name="targetUserId">The ID of the user to restore</param>
    /// <exception cref="EntityNotFoundException{String}">Thrown when admin or target user is not found</exception>
    /// <exception cref="EntityUnauthorizedException">Thrown when user is not an admin</exception>
    /// <exception cref="ValidationException">Thrown when user is not deleted</exception>
    /// <exception cref="EntityServiceException">Thrown when restoration fails</exception>
    public async Task AdminRestoreUserAsync(string adminId, string targetUserId)
    {
        _logger.LogInformation("Admin restore user attempt by admin {AdminId} for user {TargetUserId}", adminId, targetUserId);

        // Verify admin has Admin role
        var admin = await _accountRepository.FindUserByIdAsync(adminId).ConfigureAwait(false);
        if (admin == null)
        {
            _logger.LogWarning("Admin restore failed: Admin not found for ID {AdminId}", adminId);
            throw new EntityNotFoundException<string>("Admin", adminId, "Admin user not found");
        }

        var adminRoles = await _accountRepository.GetUserRolesAsync(admin).ConfigureAwait(false);
        if (!adminRoles.Contains("Admin", StringComparer.OrdinalIgnoreCase))
        {
            _logger.LogWarning("Admin restore failed: User {AdminId} is not an admin", adminId);
            throw new EntityUnauthorizedException("User", "restore", adminId, "Admin role required");
        }

        // Validate target user exists
        var targetUser = await _accountRepository.FindUserByIdAsync(targetUserId).ConfigureAwait(false);
        if (targetUser == null)
        {
            _logger.LogWarning("Admin restore failed: Target user not found for ID {TargetUserId}", targetUserId);
            throw new EntityNotFoundException<string>("User", targetUserId, "Target user not found");
        }

        // Perform restore using stored procedure
        (bool success, string message) restoreResult;
        try
        {
            restoreResult = await _accountRepository.RestoreUserAsyncSP(targetUserId).ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex,
                "Admin {AdminId} encountered an unexpected error while restoring user {TargetUserId}",
                adminId,
                targetUserId);
            throw new EntityServiceException("User", "restore", "Failed to restore user due to a database error", ex);
        }

        var (success, message) = restoreResult;

        if (!success)
        {
            _logger.LogWarning("Admin {AdminId} failed to restore user {TargetUserId}: {Message}", adminId, targetUserId, message);
            // Use ValidationException for "not deleted" errors, EntityServiceException for other failures
            if (message.Contains("not deleted", StringComparison.OrdinalIgnoreCase) ||
                message.Contains("already active", StringComparison.OrdinalIgnoreCase))
            {
                throw new ValidationException(message);
            }
            throw new EntityServiceException("User", "restore", message);
        }

        _logger.LogInformation("Admin {AdminId} successfully restored user {TargetUserId}", adminId, targetUserId);
    }

    private async Task<T> ExecuteInTransactionAsync<T>(Func<Task<T>> operation)
    {
        if (_context.Database.IsInMemory() || _context.Database.CurrentTransaction != null)
        {
            return await operation().ConfigureAwait(false);
        }

        var strategy = _context.Database.CreateExecutionStrategy();
        return await strategy.ExecuteAsync(async () =>
        {
            await using var transaction = await _context.Database.BeginTransactionAsync().ConfigureAwait(false);
            try
            {
                var result = await operation().ConfigureAwait(false);
                await transaction.CommitAsync().ConfigureAwait(false);
                return result;
            }
            catch
            {
                await transaction.RollbackAsync().ConfigureAwait(false);
                throw;
            }
        }).ConfigureAwait(false);
    }

    private async Task ExecuteInTransactionAsync(Func<Task> operation)
    {
        if (_context.Database.IsInMemory() || _context.Database.CurrentTransaction != null)
        {
            await operation().ConfigureAwait(false);
            return;
        }

        var strategy = _context.Database.CreateExecutionStrategy();
        await strategy.ExecuteAsync(async () =>
        {
            await using var transaction = await _context.Database.BeginTransactionAsync().ConfigureAwait(false);
            try
            {
                await operation().ConfigureAwait(false);
                await transaction.CommitAsync().ConfigureAwait(false);
            }
            catch
            {
                await transaction.RollbackAsync().ConfigureAwait(false);
                throw;
            }
        }).ConfigureAwait(false);
    }
}
