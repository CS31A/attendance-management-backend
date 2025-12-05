using attendance_monitoring.IRepository;
using attendance_monitoring.IServices;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace attendance_monitoring.Classes.Factory;

/// <summary>
/// Factory for creating users with their associated profiles.
/// Implements compensating transactions to prevent orphaned users.
/// </summary>
public class UserFactory(IAccountRepository accountRepository, ILogger<UserFactory> logger) : IUserFactory
{
    /// <summary>
    /// Creates a new user with the specified role and profile.
    /// Uses compensating transactions to rollback user creation if profile creation fails.
    /// </summary>
    public async Task<UserCreationResult> CreateUserAsync(
        string username,
        string email,
        string password,
        string role,
        string? firstName = null,
        string? lastName = null,
        int? sectionId = null)
    {
        // Validate inputs at the factory level for additional security
        if (string.IsNullOrWhiteSpace(username))
        {
            return new UserCreationResult { Success = false, Errors = ["Username is required"] };
        }

        if (string.IsNullOrWhiteSpace(email))
        {
            return new UserCreationResult { Success = false, Errors = ["Email is required"] };
        }

        if (string.IsNullOrWhiteSpace(password))
        {
            return new UserCreationResult { Success = false, Errors = ["Password is required"] };
        }

        if (string.IsNullOrWhiteSpace(role))
        {
            return new UserCreationResult { Success = false, Errors = ["Role is required"] };
        }

        // Create the IdentityUser first
        var identityUser = new IdentityUser
        {
            UserName = username,
            Email = email,
            EmailConfirmed = true // For simplicity, assuming email is confirmed during registration
        };

        var result = await accountRepository.CreateUserAsync(identityUser, password).ConfigureAwait(false);
        if (!result.Succeeded)
        {
            return new UserCreationResult { Success = false, Errors = result.Errors.Select(e => e.Description).ToArray() };
        }

        try
        {
            // Add user to the appropriate role
            await accountRepository.AddUserToRoleAsync(identityUser, role).ConfigureAwait(false);
        }
        catch (DbUpdateConcurrencyException ex)
        {
            // If role assignment fails due to concurrency issues, delete the user to avoid orphaned accounts
            logger.LogError(ex, "Role assignment failed for user {Email} due to concurrency issue. Cleaning up user.", email);
            await CleanupUserSafelyAsync(identityUser, email);
            return new UserCreationResult { Success = false, Errors = [$"Role assignment failed due to a concurrency issue: {ex.Message}"] };
        }
        catch (DbUpdateException ex)
        {
            // If role assignment fails due to database issues, delete the user to avoid orphaned accounts
            logger.LogError(ex, "Role assignment failed for user {Email} due to database error. Cleaning up user.", email);
            await CleanupUserSafelyAsync(identityUser, email);
            return new UserCreationResult { Success = false, Errors = [$"Role assignment failed due to a database error: {ex.Message}"] };
        }
        catch (Exception ex)
        {
            // If role assignment fails due to any other reason, delete the user to avoid orphaned accounts
            logger.LogError(ex, "Role assignment failed for user {Email}. Cleaning up user.", email);
            await CleanupUserSafelyAsync(identityUser, email);
            return new UserCreationResult { Success = false, Errors = [$"Role assignment failed: {ex.Message}"] };
        }

        // Create the specific user profile based on role
        switch (role.ToLower())
        {
            case "student":
                return await CreateStudentProfileAsync(identityUser, firstName, lastName, email, sectionId);

            case "teacher": // This is intentional, stop flaggin it monkey
            case "instructor":
                return await CreateInstructorProfileAsync(identityUser, firstName, lastName, email);

            case "admin":
                return await CreateAdminProfileAsync(identityUser, firstName, lastName, email);

            default:
                // Invalid role - cleanup the user
                logger.LogWarning("Invalid role '{Role}' specified for user {Email}. Cleaning up user.", role, email);
                await CleanupUserSafelyAsync(identityUser, email);
                return new UserCreationResult
                {
                    Success = false,
                    Errors =
                    ["Invalid role specified. Valid roles are: Student, Instructor, Admin"]
                };
        }
    }

    /// <summary>
    /// Safely cleanup a user, logging any errors but not throwing.
    /// </summary>
    private async Task CleanupUserSafelyAsync(IdentityUser user, string email)
    {
        try
        {
            await accountRepository.DeleteUserAsync(user).ConfigureAwait(false);
            logger.LogInformation("Successfully cleaned up orphaned user {Email}", email);
        }
        catch (Exception cleanupEx)
        {
            logger.LogError(cleanupEx, "Failed to cleanup orphaned user {Email}. Manual cleanup may be required.", email);
        }
    }

    /// <summary>
    /// Handles database constraint violations with specific error messages.
    /// </summary>
    private static UserCreationResult HandleConstraintViolation(Exception ex, string profileType)
    {
        var errorMessage = ex.Message;
        
        // Check for soft delete consistency constraint
        if (errorMessage.Contains("CK_") && errorMessage.Contains("SoftDeleteConsistency"))
        {
            return new UserCreationResult
            {
                Success = false,
                Errors = [$"{profileType} profile creation failed: Soft delete state is inconsistent. Please contact support."]
            };
        }
        
        // Check for other constraint violations
        if (errorMessage.Contains("CK_") || errorMessage.Contains("constraint"))
        {
            return new UserCreationResult
            {
                Success = false,
                Errors = [$"{profileType} profile creation failed: Database constraint violation. Please ensure all required information is provided correctly."]
            };
        }
        
        // Generic database error
        return new UserCreationResult
        {
            Success = false,
            Errors = [$"{profileType} profile creation failed: {ex.Message}"]
        };
    }

    private async Task<UserCreationResult> CreateStudentProfileAsync(IdentityUser identityUser, string? firstName, string? lastName, string email, int? sectionId)
    {
        if (sectionId is null or <= 0)
        {
            // If sectionId is not provided for student, return an error
            logger.LogWarning("SectionId is required for student registration. Cleaning up user {Email}.", email);
            await CleanupUserSafelyAsync(identityUser, email);
            return new UserCreationResult
            {
                Success = false,
                Errors =
                ["SectionId is required for student registration"]
            };
        }

        // Validate that Firstname is provided for students
        if (string.IsNullOrWhiteSpace(firstName))
        {
            logger.LogWarning("Firstname is required for student registration. Cleaning up user {Email}.", email);
            await CleanupUserSafelyAsync(identityUser, email);
            return new UserCreationResult
            {
                Success = false,
                Errors = ["Firstname is required for student registration"]
            };
        }

        // Validate that Lastname is provided for students
        if (string.IsNullOrWhiteSpace(lastName))
        {
            logger.LogWarning("Lastname is required for student registration. Cleaning up user {Email}.", email);
            await CleanupUserSafelyAsync(identityUser, email);
            return new UserCreationResult
            {
                Success = false,
                Errors = ["Lastname is required for student registration"]
            };
        }

        var student = new Student
        {
            UserId = identityUser.Id,
            Firstname = firstName,
            Lastname = lastName,
            IsRegular = true,
            SectionId = sectionId.Value,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        try
        {
            await accountRepository.CreateStudentProfileAsync(student).ConfigureAwait(false);
            await accountRepository.SaveChangesAsync().ConfigureAwait(false);
            logger.LogInformation("Successfully created student profile for user {Email}", email);
        }
        catch (DbUpdateConcurrencyException ex)
        {
            // If student profile creation fails due to concurrency issues, delete the user to maintain consistency
            logger.LogError(ex, "Student profile creation failed for user {Email} due to concurrency issue. Cleaning up user.", email);
            await CleanupUserSafelyAsync(identityUser, email);
            return new UserCreationResult { Success = false, Errors = [$"Student profile creation failed due to a concurrency issue: {ex.Message}"] };
        }
        catch (DbUpdateException ex) when (ex.InnerException?.Message.Contains("CK_") == true || ex.Message.Contains("CK_"))
        {
            // Handle constraint violations specifically
            logger.LogError(ex, "Student profile creation failed for user {Email} due to constraint violation. Cleaning up user.", email);
            await CleanupUserSafelyAsync(identityUser, email);
            return HandleConstraintViolation(ex.InnerException ?? ex, "Student");
        }
        catch (DbUpdateException ex)
        {
            // If student profile creation fails due to database issues, delete the user to maintain consistency
            logger.LogError(ex, "Student profile creation failed for user {Email} due to database error. Cleaning up user.", email);
            await CleanupUserSafelyAsync(identityUser, email);
            return new UserCreationResult { Success = false, Errors = [$"Student profile creation failed due to a database error: {ex.Message}"] };
        }
        catch (Exception ex)
        {
            // If student profile creation fails due to any other reason, delete the user to maintain consistency
            logger.LogError(ex, "Student profile creation failed for user {Email}. Cleaning up user.", email);
            await CleanupUserSafelyAsync(identityUser, email);
            return new UserCreationResult { Success = false, Errors = [$"Student profile creation failed: {ex.Message}"] };
        }

        return new UserCreationResult { Success = true, UserId = identityUser.Id };
    }

    private async Task<UserCreationResult> CreateInstructorProfileAsync(IdentityUser identityUser, string? firstName, string? lastName, string email)
    {
        var instructor = new Instructor
        {
            UserId = identityUser.Id,
            Firstname = firstName,
            Lastname = lastName,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        try
        {
            await accountRepository.CreateInstructorProfileAsync(instructor).ConfigureAwait(false);
            await accountRepository.SaveChangesAsync().ConfigureAwait(false);
            logger.LogInformation("Successfully created instructor profile for user {Email}", email);
        }
        catch (DbUpdateConcurrencyException ex)
        {
            // If instructor profile creation fails due to concurrency issues, delete the user to maintain consistency
            logger.LogError(ex, "Instructor profile creation failed for user {Email} due to concurrency issue. Cleaning up user.", email);
            await CleanupUserSafelyAsync(identityUser, email);
            return new UserCreationResult { Success = false, Errors = [$"Instructor profile creation failed due to a concurrency issue: {ex.Message}"] };
        }
        catch (DbUpdateException ex) when (ex.InnerException?.Message.Contains("CK_") == true || ex.Message.Contains("CK_"))
        {
            // Handle constraint violations specifically
            logger.LogError(ex, "Instructor profile creation failed for user {Email} due to constraint violation. Cleaning up user.", email);
            await CleanupUserSafelyAsync(identityUser, email);
            return HandleConstraintViolation(ex.InnerException ?? ex, "Instructor");
        }
        catch (DbUpdateException ex)
        {
            // If instructor profile creation fails due to database issues, delete the user to maintain consistency
            logger.LogError(ex, "Instructor profile creation failed for user {Email} due to database error. Cleaning up user.", email);
            await CleanupUserSafelyAsync(identityUser, email);
            return new UserCreationResult { Success = false, Errors = [$"Instructor profile creation failed due to a database error: {ex.Message}"] };
        }
        catch (Exception ex)
        {
            // If instructor profile creation fails due to any other reason, delete the user to maintain consistency
            logger.LogError(ex, "Instructor profile creation failed for user {Email}. Cleaning up user.", email);
            await CleanupUserSafelyAsync(identityUser, email);
            return new UserCreationResult { Success = false, Errors = [$"Instructor profile creation failed: {ex.Message}"] };
        }

        return new UserCreationResult { Success = true, UserId = identityUser.Id };
    }

    private async Task<UserCreationResult> CreateAdminProfileAsync(IdentityUser identityUser, string? firstName, string? lastName, string email)
    {
        var admin = new Admin
        {
            UserId = identityUser.Id,
            Firstname = firstName,
            Lastname = lastName,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        try
        {
            await accountRepository.CreateAdminProfileAsync(admin).ConfigureAwait(false);
            await accountRepository.SaveChangesAsync().ConfigureAwait(false);
            logger.LogInformation("Successfully created admin profile for user {Email}", email);
        }
        catch (DbUpdateConcurrencyException ex)
        {
            // If admin profile creation fails due to concurrency issues, delete the user to maintain consistency
            logger.LogError(ex, "Admin profile creation failed for user {Email} due to concurrency issue. Cleaning up user.", email);
            await CleanupUserSafelyAsync(identityUser, email);
            return new UserCreationResult { Success = false, Errors = [$"Admin profile creation failed due to a concurrency issue: {ex.Message}"] };
        }
        catch (DbUpdateException ex) when (ex.InnerException?.Message.Contains("CK_") == true || ex.Message.Contains("CK_"))
        {
            // Handle constraint violations specifically
            logger.LogError(ex, "Admin profile creation failed for user {Email} due to constraint violation. Cleaning up user.", email);
            await CleanupUserSafelyAsync(identityUser, email);
            return HandleConstraintViolation(ex.InnerException ?? ex, "Admin");
        }
        catch (DbUpdateException ex)
        {
            // If admin profile creation fails due to database issues, delete the user to maintain consistency
            logger.LogError(ex, "Admin profile creation failed for user {Email} due to database error. Cleaning up user.", email);
            await CleanupUserSafelyAsync(identityUser, email);
            return new UserCreationResult { Success = false, Errors = [$"Admin profile creation failed due to a database error: {ex.Message}"] };
        }
        catch (Exception ex)
        {
            // If admin profile creation fails due to any other reason, delete the user to maintain consistency
            logger.LogError(ex, "Admin profile creation failed for user {Email}. Cleaning up user.", email);
            await CleanupUserSafelyAsync(identityUser, email);
            return new UserCreationResult { Success = false, Errors = [$"Admin profile creation failed: {ex.Message}"] };
        }

        return new UserCreationResult { Success = true, UserId = identityUser.Id };
    }
}
