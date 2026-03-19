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
/// Focused unit responsible for user profile operations.
/// Handles profile retrieval and self-service profile updates.
/// </summary>
internal sealed class ProfileService
{
    private readonly ApplicationDbContext _context;
    private readonly IAccountRepository _accountRepository;
    private readonly ISectionRepository _sectionRepository;
    private readonly IInstructorRepository _instructorRepository;
    private readonly ILogger<ProfileService> _logger;

    public ProfileService(
        ApplicationDbContext context,
        IAccountRepository accountRepository,
        ISectionRepository sectionRepository,
        IInstructorRepository instructorRepository,
        ILogger<ProfileService> logger)
    {
        _context = context ?? throw new ArgumentNullException(nameof(context));
        _accountRepository = accountRepository ?? throw new ArgumentNullException(nameof(accountRepository));
        _sectionRepository = sectionRepository ?? throw new ArgumentNullException(nameof(sectionRepository));
        _instructorRepository = instructorRepository ?? throw new ArgumentNullException(nameof(instructorRepository));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <summary>
    /// Gets comprehensive user profile information including role-specific data.
    /// </summary>
    /// <param name="userId">The user ID to retrieve profile for</param>
    /// <returns>User profile DTO with role-specific information</returns>
    /// <exception cref="EntityNotFoundException{String}">Thrown when the user is not found.</exception>
    public async Task<UserProfileResponseDto> GetUserProfileAsync(string userId)
    {
        _logger.LogInformation("Fetching user profile for user ID: {UserId}", userId);

        // Get the user from Identity
        var user = await _accountRepository.FindUserByIdAsync(userId).ConfigureAwait(false);
        if (user == null)
        {
            _logger.LogWarning("User profile fetch failed: User not found for ID {UserId}", userId);
            throw new EntityNotFoundException<string>("User", userId, "User not found");
        }

        // Get user roles
        var roles = await _accountRepository.GetUserRolesAsync(user).ConfigureAwait(false);
        var role = roles?.FirstOrDefault();
        if (string.IsNullOrEmpty(role))
        {
            _logger.LogWarning("User {Username} (ID: {UserId}) has no assigned roles.", user.UserName, user.Id);
            role = "Unknown";
        }

        // Build base profile
        var profile = new UserProfileResponseDto
        {
            UserId = user.Id,
            Username = user.UserName ?? string.Empty,
            Email = user.Email ?? string.Empty,
            Role = role,
            CreatedAt = DateTime.SpecifyKind(DateTime.UtcNow, DateTimeKind.Utc),
            UpdatedAt = DateTime.SpecifyKind(DateTime.UtcNow, DateTimeKind.Utc)
        };

        // Fetch role-specific data
        if (role.Equals("Student", StringComparison.OrdinalIgnoreCase))
        {
            var student = await _context.Students
                .AsNoTracking()
                .Include(s => s.User)
                .Include(s => s.Section)
                    .ThenInclude(sec => sec.Course)
                .FirstOrDefaultAsync(s => s.UserId == userId && !s.IsDeleted)
                .ConfigureAwait(false);

            if (student != null)
            {
                profile.StudentProfile = new StudentProfileInfo
                {
                    Id = student.Id,
                    Firstname = student.Firstname,
                    Lastname = student.Lastname,
                    IsRegular = student.IsRegular,
                    SectionId = student.SectionId,
                    SectionName = student.Section?.Name ?? string.Empty,
                    CourseId = student.Section?.CourseId ?? 0,
                    CourseName = student.Section?.Course?.Name ?? string.Empty,
                    CreatedAt = student.CreatedAt,
                    UpdatedAt = student.UpdatedAt
                };
                profile.CreatedAt = student.CreatedAt;
                profile.UpdatedAt = student.UpdatedAt;
            }
        }
        else if (role.Equals(RoleConstants.Instructor, StringComparison.OrdinalIgnoreCase))
        {
            var instructor = await _instructorRepository.GetInstructorByUserIdAsync(userId).ConfigureAwait(false);
            if (instructor != null)
            {
                profile.InstructorProfile = new InstructorProfileInfo
                {
                    Id = instructor.Id,
                    Firstname = instructor.Firstname,
                    Lastname = instructor.Lastname,
                    CreatedAt = instructor.CreatedAt,
                    UpdatedAt = instructor.UpdatedAt
                };
                profile.CreatedAt = instructor.CreatedAt;
                profile.UpdatedAt = instructor.UpdatedAt;
            }
        }
        // Admin role: only base Identity information (already populated)

        _logger.LogInformation("User profile fetched successfully for user ID: {UserId}", userId);
        return profile;
    }

    /// <summary>
    /// Updates a user's own profile information.
    /// </summary>
    /// <param name="userId">The ID of the user updating their profile</param>
    /// <param name="updateProfileDto">The profile update data</param>
    /// <returns>Updated user profile</returns>
    /// <exception cref="EntityNotFoundException{String}">Thrown when the user is not found</exception>
    /// <exception cref="ValidationException">Thrown when validation fails</exception>
    /// <exception cref="EntityAlreadyExistsException{String}">Thrown when email is already in use</exception>
    public async Task<UserProfileResponseDto> UpdateUserProfileAsync(
        string userId,
        UpdateProfile updateProfileDto)
    {
        _logger.LogInformation("User profile update attempt for user ID: {UserId}", userId);

        // Validate user exists
        var user = await _accountRepository.FindUserByIdAsync(userId).ConfigureAwait(false);
        if (user == null)
        {
            _logger.LogWarning("Profile update failed: User not found for ID {UserId}", userId);
            throw new EntityNotFoundException<string>("User", userId, "User not found");
        }

        // Get user role
        var roles = await _accountRepository.GetUserRolesAsync(user).ConfigureAwait(false);
        var role = roles?.FirstOrDefault();
        if (string.IsNullOrEmpty(role))
        {
            _logger.LogWarning("User {Username} (ID: {UserId}) has no assigned roles during profile update.", user.UserName, user.Id);
            role = "Unknown";
        }

        // Validate email uniqueness if email is being changed
        if (!string.IsNullOrEmpty(updateProfileDto.Email) &&
            !updateProfileDto.Email.Equals(user.Email, StringComparison.OrdinalIgnoreCase))
        {
            var emailExists = await _accountRepository.EmailExistsAsync(updateProfileDto.Email, userId).ConfigureAwait(false);
            if (emailExists)
            {
                _logger.LogWarning("Profile update failed: Email {Email} already exists", updateProfileDto.Email);
                throw new EntityAlreadyExistsException<string>("User", "Email", updateProfileDto.Email);
            }

            // Update email
            user.Email = updateProfileDto.Email;
            user.NormalizedEmail = updateProfileDto.Email.ToUpperInvariant();
        }

        // Validate and update password if provided
        if (!string.IsNullOrEmpty(updateProfileDto.NewPassword))
        {
            // Validate current password is provided
            if (string.IsNullOrEmpty(updateProfileDto.CurrentPassword))
            {
                _logger.LogWarning("Profile update failed: Current password required for password change");
                throw new ValidationException("Current password is required to change password");
            }

            // Validate new password matches confirmation
            if (updateProfileDto.NewPassword != updateProfileDto.ConfirmNewPassword)
            {
                _logger.LogWarning("Profile update failed: New password and confirmation do not match");
                throw new ValidationException("New password and confirmation password do not match");
            }

            // Verify current password
            var passwordCheck = await _accountRepository.CheckPasswordAsync(user, updateProfileDto.CurrentPassword).ConfigureAwait(false);
            if (!passwordCheck.Succeeded)
            {
                _logger.LogWarning("Profile update failed: Invalid current password for user {UserId}", userId);
                throw new ValidationException("Current password is incorrect");
            }

            // Change password
            var passwordResult = await _accountRepository.ChangePasswordAsync(
                user,
                updateProfileDto.CurrentPassword,
                updateProfileDto.NewPassword).ConfigureAwait(false);

            if (!passwordResult.Succeeded)
            {
                var errors = string.Join("; ", passwordResult.Errors.Select(e => e.Description));
                _logger.LogWarning("Profile update failed: Password change error - {Errors}", errors);
                throw new ValidationException($"Password change failed: {errors}");
            }

            _logger.LogInformation("Password updated successfully for user {UserId}", userId);
        }

        // Update user in Identity
        try
        {
            var updateResult = await _accountRepository.UpdateUserAsync(user).ConfigureAwait(false);
            if (!updateResult.Succeeded)
            {
                var errors = string.Join("; ", updateResult.Errors.Select(e => e.Description));
                _logger.LogWarning("Profile update failed: User update error - {Errors}", errors);
                throw new ValidationException($"Profile update failed: {errors}");
            }
        }
        catch (Microsoft.EntityFrameworkCore.DbUpdateException ex) when (ex.InnerException?.Message.Contains("duplicate") == true ||
                                                                       ex.InnerException?.Message.Contains("unique") == true ||
                                                                       ex.InnerException?.Message.Contains("IX_AspNetUsers_NormalizedEmail") == true)
        {
            _logger.LogWarning("Profile update failed: Email already exists for another user - {Email}", updateProfileDto.Email);
            throw new EntityAlreadyExistsException<string>("User", "Email", updateProfileDto.Email ?? "");
        }

        // Update role-specific profile
        if (role.Equals("Student", StringComparison.OrdinalIgnoreCase))
        {
            var student = await _accountRepository.GetStudentByUserIdAsync(userId).ConfigureAwait(false);
            if (student != null)
            {
                // Update student-specific fields if provided
                if (!string.IsNullOrWhiteSpace(updateProfileDto.Firstname))
                {
                    student.Firstname = updateProfileDto.Firstname;
                }
                else if (updateProfileDto.Firstname != null)
                {
                    _logger.LogWarning("Profile update failed: Firstname is required for students");
                    throw new ValidationException("Firstname is required and cannot be empty or whitespace");
                }

                if (!string.IsNullOrWhiteSpace(updateProfileDto.Lastname))
                {
                    student.Lastname = updateProfileDto.Lastname;
                }
                else if (updateProfileDto.Lastname != null)
                {
                    _logger.LogWarning("Profile update failed: Lastname is required for students");
                    throw new ValidationException("Lastname is required and cannot be empty or whitespace");
                }
                if (updateProfileDto.SectionId.HasValue)
                {
                    var section = await _sectionRepository.GetSectionByIdAsync(updateProfileDto.SectionId.Value).ConfigureAwait(false);
                    if (section == null)
                    {
                        _logger.LogWarning("Profile update failed: Section {SectionId} does not exist", updateProfileDto.SectionId.Value);
                        throw new EntityNotFoundException<int>("Section", updateProfileDto.SectionId.Value);
                    }
                    student.SectionId = updateProfileDto.SectionId.Value;
                }
                if (updateProfileDto.IsRegular.HasValue)
                {
                    student.IsRegular = updateProfileDto.IsRegular.Value;
                }

                await _accountRepository.UpdateStudentProfileAsync(student).ConfigureAwait(false);
            }
        }
        else if (role.Equals(RoleConstants.Instructor, StringComparison.OrdinalIgnoreCase))
        {
            var instructor = await _accountRepository.GetInstructorByUserIdAsync(userId).ConfigureAwait(false);
            if (instructor != null)
            {
                if (!string.IsNullOrEmpty(updateProfileDto.Firstname))
                {
                    instructor.Firstname = updateProfileDto.Firstname;
                }
                if (!string.IsNullOrEmpty(updateProfileDto.Lastname))
                {
                    instructor.Lastname = updateProfileDto.Lastname;
                }

                await _accountRepository.UpdateInstructorProfileAsync(instructor).ConfigureAwait(false);
            }
        }

        // Save all changes
        await _accountRepository.SaveChangesAsync().ConfigureAwait(false);

        _logger.LogInformation("Profile updated successfully for user {UserId}", userId);

        // Return updated profile
        return await GetUserProfileAsync(userId).ConfigureAwait(false);
    }
}
