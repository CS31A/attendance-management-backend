using attendance_monitoring.IRepository;
using attendance_monitoring.IServices;
using Microsoft.AspNetCore.Identity;

namespace attendance_monitoring.Classes.Factory;

public class UserFactory(IAccountRepository accountRepository) : IUserFactory
{
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
            return new UserCreationResult { Success = false, Errors = new[] { "Username is required" } };
        }
        
        if (string.IsNullOrWhiteSpace(email))
        {
            return new UserCreationResult { Success = false, Errors = new[] { "Email is required" } };
        }
        
        if (string.IsNullOrWhiteSpace(password))
        {
            return new UserCreationResult { Success = false, Errors = new[] { "Password is required" } };
        }
        
        if (string.IsNullOrWhiteSpace(role))
        {
            return new UserCreationResult { Success = false, Errors = new[] { "Role is required" } };
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
        catch (Exception ex)
        {
            // If role assignment fails, delete the user to avoid orphaned accounts
            await accountRepository.DeleteUserAsync(identityUser).ConfigureAwait(false);
            return new UserCreationResult { Success = false, Errors = new[] { $"Role assignment failed: {ex.Message}" } };
        }

        // Create the specific user profile based on role
        switch (role.ToLower())
        {
            case "student":
                if (!sectionId.HasValue || sectionId <= 0)
                {
                    // If sectionId is not provided for student, return an error
                    return new UserCreationResult { Success = false, Errors = new[] { "SectionId is required for student registration" } };
                }

                var student = new Student
                {
                    UserId = identityUser.Id,
                    Firstname = firstName,
                    Lastname = lastName,
                    Email = email,
                    SectionId = sectionId.Value,
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                };

                try
                {
                    await accountRepository.CreateStudentProfileAsync(student).ConfigureAwait(false);
                }
                catch (Exception ex)
                {
                    // If student profile creation fails, delete the user to maintain consistency
                    await accountRepository.DeleteUserAsync(identityUser).ConfigureAwait(false);
                    return new UserCreationResult { Success = false, Errors = new[] { $"Student profile creation failed: {ex.Message}" } };
                }
                break;

            case "teacher": // This is intentional, stop flaggin it monkey
            case "instructor":
                var instructor = new Instructor
                {
                    UserId = identityUser.Id,
                    Firstname = firstName,
                    Lastname = lastName,
                    Email = email,
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                };

                try
                {
                    await accountRepository.CreateInstructorProfileAsync(instructor).ConfigureAwait(false);
                }
                catch (Exception ex)
                {
                    // If instructor profile creation fails, delete the user to maintain consistency
                    await accountRepository.DeleteUserAsync(identityUser).ConfigureAwait(false);
                    return new UserCreationResult { Success = false, Errors = new[] { $"Instructor profile creation failed: {ex.Message}" } };
                }
                break;

            case "admin":
                var admin = new Admin
                {
                    UserId = identityUser.Id,
                    Firstname = firstName,
                    Lastname = lastName,
                    Email = email,
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                };

                try
                {
                    await accountRepository.CreateAdminProfileAsync(admin).ConfigureAwait(false);
                }
                catch (Exception ex)
                {
                    // If admin profile creation fails, delete the user to maintain consistency
                    await accountRepository.DeleteUserAsync(identityUser).ConfigureAwait(false);
                    return new UserCreationResult { Success = false, Errors = new[] { $"Admin profile creation failed: {ex.Message}" } };
                }
                break;

            default:
                return new UserCreationResult { Success = false, Errors = new[] { "Invalid role specified. Valid roles are: Student, Instructor, Admin" } };
        }

        return new UserCreationResult { Success = true, UserId = identityUser.Id };
    }
}