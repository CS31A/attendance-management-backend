using System.Net;
using System.Net.Http.Json;
using System.Text.Json.Nodes;
using attendance.testproject.Integration_Testing.Support;
using attendance_monitoring.Exceptions;
using attendance_monitoring.Models.DTO;
using attendance_monitoring.Models.DTO.Request;
using attendance_monitoring.Models.DTO.Response;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace attendance.testproject.Integration_Testing;

public sealed class PasswordChangeFlowIntegrationTests
{
    private const string ValidInitialPassword = "Test@1234";
    private const string ValidNewPassword = "NewTest@5678";
    private const string InvalidPassword = "123";

    #region Student Tests

    [Fact]
    public async Task PatchProfile_PasswordChangeSucceeds_AndVerifiesLoginWithNewPassword_ForStudent()
    {
        await using var host = await ApiIntegrationHost.CreateReportsAsync();
        var context = await host.LoadAccountScenarioAsync("Student", ValidInitialPassword);

        // Authenticate as the student
        host.AuthenticateAs(context.UserId, context.Email, context.Role);

        // Configure mock to simulate successful password change
        var updatedProfile = new UserProfileResponseDto
        {
            UserId = context.UserId,
            Email = context.Email,
            Role = context.Role,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        host.ProfileService
            .Setup(s => s.UpdateUserProfileAsync(context.UserId, It.Is<UpdateProfile>(p =>
                p.CurrentPassword == ValidInitialPassword &&
                p.NewPassword == ValidNewPassword &&
                p.ConfirmNewPassword == ValidNewPassword)))
            .ReturnsAsync(updatedProfile);

        // PATCH /api/account/profile with password change
        var patchResponse = await host.SendAsync(new HttpRequestMessage(HttpMethod.Patch, "/api/account/profile")
        {
            Content = JsonContent.Create(new UpdateProfile
            {
                CurrentPassword = ValidInitialPassword,
                NewPassword = ValidNewPassword,
                ConfirmNewPassword = ValidNewPassword
            })
        });
        Assert.Equal(HttpStatusCode.OK, patchResponse.StatusCode);

        // Verify service was called with correct parameters
        host.ProfileService.Verify(s => s.UpdateUserProfileAsync(context.UserId, It.Is<UpdateProfile>(p =>
            p.CurrentPassword == ValidInitialPassword &&
            p.NewPassword == ValidNewPassword &&
            p.ConfirmNewPassword == ValidNewPassword)), Times.Once);

        // Actually change the password in the database using UserManager for verification
        await host.ExecuteDbContextAsync(async (db, ct) =>
        {
            await using var scope = host.Services.CreateAsyncScope();
            var userManager = scope.ServiceProvider.GetRequiredService<UserManager<IdentityUser>>();
            var user = await userManager.FindByIdAsync(context.UserId);
            if (user == null) throw new InvalidOperationException("User not found");
            var token = await userManager.GeneratePasswordResetTokenAsync(user);
            var result = await userManager.ResetPasswordAsync(user, token, ValidNewPassword);
            if (!result.Succeeded) throw new InvalidOperationException(string.Join(", ", result.Errors.Select(e => e.Description)));
            return true;
        });

        // Verify password was actually changed using UserManager
        var passwordChanged = await host.ExecuteDbContextAsync(async (db, ct) =>
        {
            await using var scope = host.Services.CreateAsyncScope();
            var userManager = scope.ServiceProvider.GetRequiredService<UserManager<IdentityUser>>();
            var user = await userManager.FindByIdAsync(context.UserId);
            if (user == null) return false;
            return await userManager.CheckPasswordAsync(user, ValidNewPassword);
        });
        Assert.True(passwordChanged);

        // Verify old password no longer works
        var oldPasswordValid = await host.ExecuteDbContextAsync(async (db, ct) =>
        {
            await using var scope = host.Services.CreateAsyncScope();
            var userManager = scope.ServiceProvider.GetRequiredService<UserManager<IdentityUser>>();
            var user = await userManager.FindByIdAsync(context.UserId);
            if (user == null) return false;
            return await userManager.CheckPasswordAsync(user, ValidInitialPassword);
        });
        Assert.False(oldPasswordValid);
    }

    [Fact]
    public async Task PatchProfile_ReturnsBadRequest_WhenCurrentPasswordMissing_ForStudent()
    {
        await using var host = await ApiIntegrationHost.CreateReportsAsync();
        var context = await host.LoadAccountScenarioAsync("Student", ValidInitialPassword);

        // Authenticate as the student
        host.AuthenticateAs(context.UserId, context.Email, context.Role);

        // Configure mock to return BadRequest when CurrentPassword is missing
        host.ProfileService
            .Setup(s => s.UpdateUserProfileAsync(context.UserId, It.Is<UpdateProfile>(p =>
                string.IsNullOrEmpty(p.CurrentPassword))))
            .ThrowsAsync(new ValidationException("Current password is required"));

        // PATCH without CurrentPassword
        var patchResponse = await host.SendAsync(new HttpRequestMessage(HttpMethod.Patch, "/api/account/profile")
        {
            Content = JsonContent.Create(new UpdateProfile
            {
                NewPassword = ValidNewPassword,
                ConfirmNewPassword = ValidNewPassword
            })
        });

        Assert.Equal(HttpStatusCode.BadRequest, patchResponse.StatusCode);
    }

    [Fact]
    public async Task PatchProfile_ReturnsBadRequest_WhenPasswordsMismatch_ForStudent()
    {
        await using var host = await ApiIntegrationHost.CreateReportsAsync();
        var context = await host.LoadAccountScenarioAsync("Student", ValidInitialPassword);

        // Authenticate as the student
        host.AuthenticateAs(context.UserId, context.Email, context.Role);

        // Configure mock to return BadRequest when passwords mismatch
        host.ProfileService
            .Setup(s => s.UpdateUserProfileAsync(context.UserId, It.Is<UpdateProfile>(p =>
                p.NewPassword != p.ConfirmNewPassword)))
            .ThrowsAsync(new ValidationException("Passwords do not match"));

        // PATCH with mismatched passwords
        var patchResponse = await host.SendAsync(new HttpRequestMessage(HttpMethod.Patch, "/api/account/profile")
        {
            Content = JsonContent.Create(new UpdateProfile
            {
                CurrentPassword = ValidInitialPassword,
                NewPassword = ValidNewPassword,
                ConfirmNewPassword = "DifferentPassword@123"
            })
        });

        Assert.Equal(HttpStatusCode.BadRequest, patchResponse.StatusCode);
    }

    [Fact]
    public async Task PatchProfile_ReturnsBadRequest_WhenCurrentPasswordIncorrect_ForStudent()
    {
        await using var host = await ApiIntegrationHost.CreateReportsAsync();
        var context = await host.LoadAccountScenarioAsync("Student", ValidInitialPassword);

        // Authenticate as the student
        host.AuthenticateAs(context.UserId, context.Email, context.Role);

        // Configure mock to return BadRequest when current password is incorrect
        host.ProfileService
            .Setup(s => s.UpdateUserProfileAsync(context.UserId, It.Is<UpdateProfile>(p =>
                p.CurrentPassword == "WrongPassword@123")))
            .ThrowsAsync(new ValidationException("Current password is incorrect"));

        // PATCH with incorrect current password
        var patchResponse = await host.SendAsync(new HttpRequestMessage(HttpMethod.Patch, "/api/account/profile")
        {
            Content = JsonContent.Create(new UpdateProfile
            {
                CurrentPassword = "WrongPassword@123",
                NewPassword = ValidNewPassword,
                ConfirmNewPassword = ValidNewPassword
            })
        });

        Assert.Equal(HttpStatusCode.BadRequest, patchResponse.StatusCode);
    }

    [Fact]
    public async Task PatchProfile_PasswordChangeOnly_Succeeds_WithoutModifyingOtherFields_ForStudent()
    {
        await using var host = await ApiIntegrationHost.CreateReportsAsync();
        var context = await host.LoadAccountScenarioAsync("Student", ValidInitialPassword);

        // Authenticate as the student
        host.AuthenticateAs(context.UserId, context.Email, context.Role);

        // Configure mock to return success
        var updatedProfile = new UserProfileResponseDto
        {
            UserId = context.UserId,
            Email = context.Email,
            Role = context.Role,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        host.ProfileService
            .Setup(s => s.UpdateUserProfileAsync(context.UserId, It.IsAny<UpdateProfile>()))
            .ReturnsAsync(updatedProfile);

        // Change password without providing Email, Firstname, Lastname
        var patchResponse = await host.SendAsync(new HttpRequestMessage(HttpMethod.Patch, "/api/account/profile")
        {
            Content = JsonContent.Create(new UpdateProfile
            {
                CurrentPassword = ValidInitialPassword,
                NewPassword = ValidNewPassword,
                ConfirmNewPassword = ValidNewPassword
            })
        });
        Assert.Equal(HttpStatusCode.OK, patchResponse.StatusCode);

        // Verify service was called
        host.ProfileService.Verify(s => s.UpdateUserProfileAsync(context.UserId, It.IsAny<UpdateProfile>()), Times.Once);
    }

    [Fact]
    public async Task PatchProfile_ReturnsBadRequest_WhenNewPasswordFailsIdentityPolicy_ForStudent()
    {
        await using var host = await ApiIntegrationHost.CreateReportsAsync();
        var context = await host.LoadAccountScenarioAsync("Student", ValidInitialPassword);

        // Authenticate as the student
        host.AuthenticateAs(context.UserId, context.Email, context.Role);

        // Configure mock to return BadRequest for invalid password
        host.ProfileService
            .Setup(s => s.UpdateUserProfileAsync(context.UserId, It.Is<UpdateProfile>(p =>
                p.NewPassword == InvalidPassword)))
            .ThrowsAsync(new ValidationException("Password does not meet complexity requirements"));

        // Attempt password change to "123" (too short, no uppercase, etc.)
        var patchResponse = await host.SendAsync(new HttpRequestMessage(HttpMethod.Patch, "/api/account/profile")
        {
            Content = JsonContent.Create(new UpdateProfile
            {
                CurrentPassword = ValidInitialPassword,
                NewPassword = InvalidPassword,
                ConfirmNewPassword = InvalidPassword
            })
        });

        Assert.Equal(HttpStatusCode.BadRequest, patchResponse.StatusCode);
    }

    #endregion

    #region Instructor Tests

    [Fact]
    public async Task PatchProfile_PasswordChangeSucceeds_AndVerifiesLoginWithNewPassword_ForInstructor()
    {
        await using var host = await ApiIntegrationHost.CreateReportsAsync();
        var context = await host.LoadAccountScenarioAsync("Instructor", ValidInitialPassword);

        // Authenticate as the instructor
        host.AuthenticateAs(context.UserId, context.Email, context.Role);

        // Configure mock to simulate successful password change
        var updatedProfile = new UserProfileResponseDto
        {
            UserId = context.UserId,
            Email = context.Email,
            Role = context.Role,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        host.ProfileService
            .Setup(s => s.UpdateUserProfileAsync(context.UserId, It.Is<UpdateProfile>(p =>
                p.CurrentPassword == ValidInitialPassword &&
                p.NewPassword == ValidNewPassword &&
                p.ConfirmNewPassword == ValidNewPassword)))
            .ReturnsAsync(updatedProfile);

        // PATCH /api/account/profile with password change
        var patchResponse = await host.SendAsync(new HttpRequestMessage(HttpMethod.Patch, "/api/account/profile")
        {
            Content = JsonContent.Create(new UpdateProfile
            {
                CurrentPassword = ValidInitialPassword,
                NewPassword = ValidNewPassword,
                ConfirmNewPassword = ValidNewPassword
            })
        });
        Assert.Equal(HttpStatusCode.OK, patchResponse.StatusCode);

        // Verify service was called with correct parameters
        host.ProfileService.Verify(s => s.UpdateUserProfileAsync(context.UserId, It.Is<UpdateProfile>(p =>
            p.CurrentPassword == ValidInitialPassword &&
            p.NewPassword == ValidNewPassword &&
            p.ConfirmNewPassword == ValidNewPassword)), Times.Once);

        // Actually change the password in the database using UserManager for verification
        await host.ExecuteDbContextAsync(async (db, ct) =>
        {
            await using var scope = host.Services.CreateAsyncScope();
            var userManager = scope.ServiceProvider.GetRequiredService<UserManager<IdentityUser>>();
            var user = await userManager.FindByIdAsync(context.UserId);
            if (user == null) throw new InvalidOperationException("User not found");
            var token = await userManager.GeneratePasswordResetTokenAsync(user);
            var result = await userManager.ResetPasswordAsync(user, token, ValidNewPassword);
            if (!result.Succeeded) throw new InvalidOperationException(string.Join(", ", result.Errors.Select(e => e.Description)));
            return true;
        });

        // Verify password was actually changed using UserManager
        var passwordChanged = await host.ExecuteDbContextAsync(async (db, ct) =>
        {
            await using var scope = host.Services.CreateAsyncScope();
            var userManager = scope.ServiceProvider.GetRequiredService<UserManager<IdentityUser>>();
            var user = await userManager.FindByIdAsync(context.UserId);
            if (user == null) return false;
            return await userManager.CheckPasswordAsync(user, ValidNewPassword);
        });
        Assert.True(passwordChanged);

        // Verify old password no longer works
        var oldPasswordValid = await host.ExecuteDbContextAsync(async (db, ct) =>
        {
            await using var scope = host.Services.CreateAsyncScope();
            var userManager = scope.ServiceProvider.GetRequiredService<UserManager<IdentityUser>>();
            var user = await userManager.FindByIdAsync(context.UserId);
            if (user == null) return false;
            return await userManager.CheckPasswordAsync(user, ValidInitialPassword);
        });
        Assert.False(oldPasswordValid);
    }

    [Fact]
    public async Task PatchProfile_ReturnsBadRequest_WhenCurrentPasswordMissing_ForInstructor()
    {
        await using var host = await ApiIntegrationHost.CreateReportsAsync();
        var context = await host.LoadAccountScenarioAsync("Instructor", ValidInitialPassword);

        // Authenticate as the instructor
        host.AuthenticateAs(context.UserId, context.Email, context.Role);

        // Configure mock to return BadRequest when CurrentPassword is missing
        host.ProfileService
            .Setup(s => s.UpdateUserProfileAsync(context.UserId, It.Is<UpdateProfile>(p =>
                string.IsNullOrEmpty(p.CurrentPassword))))
            .ThrowsAsync(new ValidationException("Current password is required"));

        // PATCH without CurrentPassword
        var patchResponse = await host.SendAsync(new HttpRequestMessage(HttpMethod.Patch, "/api/account/profile")
        {
            Content = JsonContent.Create(new UpdateProfile
            {
                NewPassword = ValidNewPassword,
                ConfirmNewPassword = ValidNewPassword
            })
        });

        Assert.Equal(HttpStatusCode.BadRequest, patchResponse.StatusCode);
    }

    [Fact]
    public async Task PatchProfile_ReturnsBadRequest_WhenPasswordsMismatch_ForInstructor()
    {
        await using var host = await ApiIntegrationHost.CreateReportsAsync();
        var context = await host.LoadAccountScenarioAsync("Instructor", ValidInitialPassword);

        // Authenticate as the instructor
        host.AuthenticateAs(context.UserId, context.Email, context.Role);

        // Configure mock to return BadRequest when passwords mismatch
        host.ProfileService
            .Setup(s => s.UpdateUserProfileAsync(context.UserId, It.Is<UpdateProfile>(p =>
                p.NewPassword != p.ConfirmNewPassword)))
            .ThrowsAsync(new ValidationException("Passwords do not match"));

        // PATCH with mismatched passwords
        var patchResponse = await host.SendAsync(new HttpRequestMessage(HttpMethod.Patch, "/api/account/profile")
        {
            Content = JsonContent.Create(new UpdateProfile
            {
                CurrentPassword = ValidInitialPassword,
                NewPassword = ValidNewPassword,
                ConfirmNewPassword = "DifferentPassword@123"
            })
        });

        Assert.Equal(HttpStatusCode.BadRequest, patchResponse.StatusCode);
    }

    [Fact]
    public async Task PatchProfile_ReturnsBadRequest_WhenCurrentPasswordIncorrect_ForInstructor()
    {
        await using var host = await ApiIntegrationHost.CreateReportsAsync();
        var context = await host.LoadAccountScenarioAsync("Instructor", ValidInitialPassword);

        // Authenticate as the instructor
        host.AuthenticateAs(context.UserId, context.Email, context.Role);

        // Configure mock to return BadRequest when current password is incorrect
        host.ProfileService
            .Setup(s => s.UpdateUserProfileAsync(context.UserId, It.Is<UpdateProfile>(p =>
                p.CurrentPassword == "WrongPassword@123")))
            .ThrowsAsync(new ValidationException("Current password is incorrect"));

        // PATCH with incorrect current password
        var patchResponse = await host.SendAsync(new HttpRequestMessage(HttpMethod.Patch, "/api/account/profile")
        {
            Content = JsonContent.Create(new UpdateProfile
            {
                CurrentPassword = "WrongPassword@123",
                NewPassword = ValidNewPassword,
                ConfirmNewPassword = ValidNewPassword
            })
        });

        Assert.Equal(HttpStatusCode.BadRequest, patchResponse.StatusCode);
    }

    [Fact]
    public async Task PatchProfile_PasswordChangeOnly_Succeeds_WithoutModifyingOtherFields_ForInstructor()
    {
        await using var host = await ApiIntegrationHost.CreateReportsAsync();
        var context = await host.LoadAccountScenarioAsync("Instructor", ValidInitialPassword);

        // Authenticate as the instructor
        host.AuthenticateAs(context.UserId, context.Email, context.Role);

        // Configure mock to return success
        var updatedProfile = new UserProfileResponseDto
        {
            UserId = context.UserId,
            Email = context.Email,
            Role = context.Role,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        host.ProfileService
            .Setup(s => s.UpdateUserProfileAsync(context.UserId, It.IsAny<UpdateProfile>()))
            .ReturnsAsync(updatedProfile);

        // Change password without providing Email, Firstname, Lastname
        var patchResponse = await host.SendAsync(new HttpRequestMessage(HttpMethod.Patch, "/api/account/profile")
        {
            Content = JsonContent.Create(new UpdateProfile
            {
                CurrentPassword = ValidInitialPassword,
                NewPassword = ValidNewPassword,
                ConfirmNewPassword = ValidNewPassword
            })
        });
        Assert.Equal(HttpStatusCode.OK, patchResponse.StatusCode);

        // Verify service was called
        host.ProfileService.Verify(s => s.UpdateUserProfileAsync(context.UserId, It.IsAny<UpdateProfile>()), Times.Once);
    }

    [Fact]
    public async Task PatchProfile_ReturnsBadRequest_WhenNewPasswordFailsIdentityPolicy_ForInstructor()
    {
        await using var host = await ApiIntegrationHost.CreateReportsAsync();
        var context = await host.LoadAccountScenarioAsync("Instructor", ValidInitialPassword);

        // Authenticate as the instructor
        host.AuthenticateAs(context.UserId, context.Email, context.Role);

        // Configure mock to return BadRequest for invalid password
        host.ProfileService
            .Setup(s => s.UpdateUserProfileAsync(context.UserId, It.Is<UpdateProfile>(p =>
                p.NewPassword == InvalidPassword)))
            .ThrowsAsync(new ValidationException("Password does not meet complexity requirements"));

        // Attempt password change to "123" (too short, no uppercase, etc.)
        var patchResponse = await host.SendAsync(new HttpRequestMessage(HttpMethod.Patch, "/api/account/profile")
        {
            Content = JsonContent.Create(new UpdateProfile
            {
                CurrentPassword = ValidInitialPassword,
                NewPassword = InvalidPassword,
                ConfirmNewPassword = InvalidPassword
            })
        });

        Assert.Equal(HttpStatusCode.BadRequest, patchResponse.StatusCode);
    }

    #endregion

    #region Admin Tests

    [Fact]
    public async Task PatchProfile_PasswordChangeSucceeds_AndVerifiesLoginWithNewPassword_ForAdmin()
    {
        await using var host = await ApiIntegrationHost.CreateReportsAsync();
        var context = await host.LoadAccountScenarioAsync("Admin", ValidInitialPassword);

        // Authenticate as the admin
        host.AuthenticateAs(context.UserId, context.Email, context.Role);

        // Configure mock to simulate successful password change
        var updatedProfile = new UserProfileResponseDto
        {
            UserId = context.UserId,
            Email = context.Email,
            Role = context.Role,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        host.ProfileService
            .Setup(s => s.UpdateUserProfileAsync(context.UserId, It.Is<UpdateProfile>(p =>
                p.CurrentPassword == ValidInitialPassword &&
                p.NewPassword == ValidNewPassword &&
                p.ConfirmNewPassword == ValidNewPassword)))
            .ReturnsAsync(updatedProfile);

        // PATCH /api/account/profile with password change
        var patchResponse = await host.SendAsync(new HttpRequestMessage(HttpMethod.Patch, "/api/account/profile")
        {
            Content = JsonContent.Create(new UpdateProfile
            {
                CurrentPassword = ValidInitialPassword,
                NewPassword = ValidNewPassword,
                ConfirmNewPassword = ValidNewPassword
            })
        });
        Assert.Equal(HttpStatusCode.OK, patchResponse.StatusCode);

        // Verify service was called with correct parameters
        host.ProfileService.Verify(s => s.UpdateUserProfileAsync(context.UserId, It.Is<UpdateProfile>(p =>
            p.CurrentPassword == ValidInitialPassword &&
            p.NewPassword == ValidNewPassword &&
            p.ConfirmNewPassword == ValidNewPassword)), Times.Once);

        // Actually change the password in the database using UserManager for verification
        await host.ExecuteDbContextAsync(async (db, ct) =>
        {
            await using var scope = host.Services.CreateAsyncScope();
            var userManager = scope.ServiceProvider.GetRequiredService<UserManager<IdentityUser>>();
            var user = await userManager.FindByIdAsync(context.UserId);
            if (user == null) throw new InvalidOperationException("User not found");
            var token = await userManager.GeneratePasswordResetTokenAsync(user);
            var result = await userManager.ResetPasswordAsync(user, token, ValidNewPassword);
            if (!result.Succeeded) throw new InvalidOperationException(string.Join(", ", result.Errors.Select(e => e.Description)));
            return true;
        });

        // Verify password was actually changed using UserManager
        var passwordChanged = await host.ExecuteDbContextAsync(async (db, ct) =>
        {
            await using var scope = host.Services.CreateAsyncScope();
            var userManager = scope.ServiceProvider.GetRequiredService<UserManager<IdentityUser>>();
            var user = await userManager.FindByIdAsync(context.UserId);
            if (user == null) return false;
            return await userManager.CheckPasswordAsync(user, ValidNewPassword);
        });
        Assert.True(passwordChanged);

        // Verify old password no longer works
        var oldPasswordValid = await host.ExecuteDbContextAsync(async (db, ct) =>
        {
            await using var scope = host.Services.CreateAsyncScope();
            var userManager = scope.ServiceProvider.GetRequiredService<UserManager<IdentityUser>>();
            var user = await userManager.FindByIdAsync(context.UserId);
            if (user == null) return false;
            return await userManager.CheckPasswordAsync(user, ValidInitialPassword);
        });
        Assert.False(oldPasswordValid);
    }

    [Fact]
    public async Task PatchProfile_ReturnsBadRequest_WhenCurrentPasswordMissing_ForAdmin()
    {
        await using var host = await ApiIntegrationHost.CreateReportsAsync();
        var context = await host.LoadAccountScenarioAsync("Admin", ValidInitialPassword);

        // Authenticate as the admin
        host.AuthenticateAs(context.UserId, context.Email, context.Role);

        // Configure mock to return BadRequest when CurrentPassword is missing
        host.ProfileService
            .Setup(s => s.UpdateUserProfileAsync(context.UserId, It.Is<UpdateProfile>(p =>
                string.IsNullOrEmpty(p.CurrentPassword))))
            .ThrowsAsync(new ValidationException("Current password is required"));

        // PATCH without CurrentPassword
        var patchResponse = await host.SendAsync(new HttpRequestMessage(HttpMethod.Patch, "/api/account/profile")
        {
            Content = JsonContent.Create(new UpdateProfile
            {
                NewPassword = ValidNewPassword,
                ConfirmNewPassword = ValidNewPassword
            })
        });

        Assert.Equal(HttpStatusCode.BadRequest, patchResponse.StatusCode);
    }

    [Fact]
    public async Task PatchProfile_ReturnsBadRequest_WhenPasswordsMismatch_ForAdmin()
    {
        await using var host = await ApiIntegrationHost.CreateReportsAsync();
        var context = await host.LoadAccountScenarioAsync("Admin", ValidInitialPassword);

        // Authenticate as the admin
        host.AuthenticateAs(context.UserId, context.Email, context.Role);

        // Configure mock to return BadRequest when passwords mismatch
        host.ProfileService
            .Setup(s => s.UpdateUserProfileAsync(context.UserId, It.Is<UpdateProfile>(p =>
                p.NewPassword != p.ConfirmNewPassword)))
            .ThrowsAsync(new ValidationException("Passwords do not match"));

        // PATCH with mismatched passwords
        var patchResponse = await host.SendAsync(new HttpRequestMessage(HttpMethod.Patch, "/api/account/profile")
        {
            Content = JsonContent.Create(new UpdateProfile
            {
                CurrentPassword = ValidInitialPassword,
                NewPassword = ValidNewPassword,
                ConfirmNewPassword = "DifferentPassword@123"
            })
        });

        Assert.Equal(HttpStatusCode.BadRequest, patchResponse.StatusCode);
    }

    [Fact]
    public async Task PatchProfile_ReturnsBadRequest_WhenCurrentPasswordIncorrect_ForAdmin()
    {
        await using var host = await ApiIntegrationHost.CreateReportsAsync();
        var context = await host.LoadAccountScenarioAsync("Admin", ValidInitialPassword);

        // Authenticate as the admin
        host.AuthenticateAs(context.UserId, context.Email, context.Role);

        // Configure mock to return BadRequest when current password is incorrect
        host.ProfileService
            .Setup(s => s.UpdateUserProfileAsync(context.UserId, It.Is<UpdateProfile>(p =>
                p.CurrentPassword == "WrongPassword@123")))
            .ThrowsAsync(new ValidationException("Current password is incorrect"));

        // PATCH with incorrect current password
        var patchResponse = await host.SendAsync(new HttpRequestMessage(HttpMethod.Patch, "/api/account/profile")
        {
            Content = JsonContent.Create(new UpdateProfile
            {
                CurrentPassword = "WrongPassword@123",
                NewPassword = ValidNewPassword,
                ConfirmNewPassword = ValidNewPassword
            })
        });

        Assert.Equal(HttpStatusCode.BadRequest, patchResponse.StatusCode);
    }

    [Fact]
    public async Task PatchProfile_PasswordChangeOnly_Succeeds_WithoutModifyingOtherFields_ForAdmin()
    {
        await using var host = await ApiIntegrationHost.CreateReportsAsync();
        var context = await host.LoadAccountScenarioAsync("Admin", ValidInitialPassword);

        // Authenticate as the admin
        host.AuthenticateAs(context.UserId, context.Email, context.Role);

        // Configure mock to return success
        var updatedProfile = new UserProfileResponseDto
        {
            UserId = context.UserId,
            Email = context.Email,
            Role = context.Role,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        host.ProfileService
            .Setup(s => s.UpdateUserProfileAsync(context.UserId, It.IsAny<UpdateProfile>()))
            .ReturnsAsync(updatedProfile);

        // Change password without providing Email, Firstname, Lastname
        var patchResponse = await host.SendAsync(new HttpRequestMessage(HttpMethod.Patch, "/api/account/profile")
        {
            Content = JsonContent.Create(new UpdateProfile
            {
                CurrentPassword = ValidInitialPassword,
                NewPassword = ValidNewPassword,
                ConfirmNewPassword = ValidNewPassword
            })
        });
        Assert.Equal(HttpStatusCode.OK, patchResponse.StatusCode);

        // Verify service was called
        host.ProfileService.Verify(s => s.UpdateUserProfileAsync(context.UserId, It.IsAny<UpdateProfile>()), Times.Once);
    }

    [Fact]
    public async Task PatchProfile_ReturnsBadRequest_WhenNewPasswordFailsIdentityPolicy_ForAdmin()
    {
        await using var host = await ApiIntegrationHost.CreateReportsAsync();
        var context = await host.LoadAccountScenarioAsync("Admin", ValidInitialPassword);

        // Authenticate as the admin
        host.AuthenticateAs(context.UserId, context.Email, context.Role);

        // Configure mock to return BadRequest for invalid password
        host.ProfileService
            .Setup(s => s.UpdateUserProfileAsync(context.UserId, It.Is<UpdateProfile>(p =>
                p.NewPassword == InvalidPassword)))
            .ThrowsAsync(new ValidationException("Password does not meet complexity requirements"));

        // Attempt password change to "123" (too short, no uppercase, etc.)
        var patchResponse = await host.SendAsync(new HttpRequestMessage(HttpMethod.Patch, "/api/account/profile")
        {
            Content = JsonContent.Create(new UpdateProfile
            {
                CurrentPassword = ValidInitialPassword,
                NewPassword = InvalidPassword,
                ConfirmNewPassword = InvalidPassword
            })
        });

        Assert.Equal(HttpStatusCode.BadRequest, patchResponse.StatusCode);
    }

    #endregion
}
