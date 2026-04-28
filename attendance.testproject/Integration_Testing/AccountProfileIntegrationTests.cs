using System.Net;
using System.Net.Http.Json;
using attendance_monitoring.Exceptions;
using attendance_monitoring.IServices;
using attendance_monitoring.Models.DTO.Request;
using attendance_monitoring.Models.DTO.Response;
using attendance.testproject.Integration_Testing.Support;

namespace attendance.testproject.Integration_Testing;

/// <summary>
/// API integration tests for PATCH /api/account/profile endpoint.
/// Tests routing, auth, middleware, and response mapping for profile update including password change.
/// </summary>
public sealed class AccountProfileIntegrationTests
{
    [Fact]
    public async Task PatchApiAccountProfile_ReturnsOk_WithPasswordChangeSuccess()
    {
        await using var host = await ApiIntegrationHost.CreateAsync();
        host.AuthenticateAs(userId: "user-123", username: "testuser", role: "Student");

        var updateProfileDto = new UpdateProfile
        {
            CurrentPassword = "OldPassword123!",
            NewPassword = "NewPassword123!",
            ConfirmNewPassword = "NewPassword123!"
        };

        var updatedProfile = new UserProfileResponseDto
        {
            UserId = "user-123",
            Username = "testuser",
            Email = "test@test.com",
            Role = "Student"
        };

        host.AccountService
            .Setup(service => service.UpdateUserProfileAsync(
                It.Is<string>(userId => userId == "user-123"),
                It.Is<UpdateProfile>(dto =>
                    dto.CurrentPassword == updateProfileDto.CurrentPassword &&
                    dto.NewPassword == updateProfileDto.NewPassword &&
                    dto.ConfirmNewPassword == updateProfileDto.ConfirmNewPassword)))
            .ReturnsAsync(updatedProfile);

        // PATCH /api/account/profile
        var request = new HttpRequestMessage(HttpMethod.Patch, "/api/account/profile")
        {
            Content = JsonContent.Create(updateProfileDto)
        };
        var response = await host.SendAsync(request);
        var payload = await response.Content.ReadFromJsonAsync<UpdateProfileResponse>();

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.NotNull(payload);
        Assert.True(payload.Success);
        Assert.Equal("Profile updated successfully", payload.Message);
    }

    [Fact]
    public async Task PatchApiAccountProfile_ReturnsUnauthorized_WhenNotAuthenticated()
    {
        await using var host = await ApiIntegrationHost.CreateAsync();
        // No authentication set

        var updateProfileDto = new UpdateProfile
        {
            CurrentPassword = "OldPassword123!",
            NewPassword = "NewPassword123!",
            ConfirmNewPassword = "NewPassword123!"
        };

        // PATCH /api/account/profile
        var request = new HttpRequestMessage(HttpMethod.Patch, "/api/account/profile")
        {
            Content = JsonContent.Create(updateProfileDto)
        };
        var response = await host.SendAsync(request);

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task PatchApiAccountProfile_ReturnsBadRequest_WhenValidationExceptionThrown()
    {
        await using var host = await ApiIntegrationHost.CreateAsync();
        host.AuthenticateAs(userId: "user-123", username: "testuser", role: "Student");

        var updateProfileDto = new UpdateProfile
        {
            CurrentPassword = "WrongPassword123!",
            NewPassword = "NewPassword123!",
            ConfirmNewPassword = "NewPassword123!"
        };

        host.AccountService
            .Setup(service => service.UpdateUserProfileAsync(
                It.IsAny<string>(),
                It.IsAny<UpdateProfile>()))
            .ThrowsAsync(new ValidationException("Current password is incorrect"));

        // PATCH /api/account/profile
        var request = new HttpRequestMessage(HttpMethod.Patch, "/api/account/profile")
        {
            Content = JsonContent.Create(updateProfileDto)
        };
        var response = await host.SendAsync(request);
        var payload = await response.Content.ReadFromJsonAsync<UpdateProfileResponse>();

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.NotNull(payload);
        Assert.False(payload.Success);
        Assert.Equal("Current password is incorrect", payload.Message);
    }

    [Fact]
    public async Task PatchApiAccountProfile_ReturnsConflict_WhenEmailAlreadyExists()
    {
        await using var host = await ApiIntegrationHost.CreateAsync();
        host.AuthenticateAs(userId: "user-123", username: "testuser", role: "Student");

        var updateProfileDto = new UpdateProfile
        {
            Email = "existing@test.com"
        };

        host.AccountService
            .Setup(service => service.UpdateUserProfileAsync(
                It.IsAny<string>(),
                It.IsAny<UpdateProfile>()))
            .ThrowsAsync(new EntityAlreadyExistsException<string>("User", "Email", "existing@test.com", "Email already in use"));

        // PATCH /api/account/profile
        var request = new HttpRequestMessage(HttpMethod.Patch, "/api/account/profile")
        {
            Content = JsonContent.Create(updateProfileDto)
        };
        var response = await host.SendAsync(request);
        var payload = await response.Content.ReadFromJsonAsync<UpdateProfileResponse>();

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.NotNull(payload);
        Assert.False(payload.Success);
    }

    [Fact]
    public async Task PatchApiAccountProfile_ReturnsBadRequest_WhenPasswordTooShort()
    {
        await using var host = await ApiIntegrationHost.CreateAsync();
        host.AuthenticateAs(userId: "user-123", username: "testuser", role: "Student");

        var updateProfileDto = new UpdateProfile
        {
            CurrentPassword = "OldPassword123!",
            NewPassword = "Short1!", // Less than 8 characters
            ConfirmNewPassword = "Short1!"
        };

        // PATCH /api/account/profile
        var request = new HttpRequestMessage(HttpMethod.Patch, "/api/account/profile")
        {
            Content = JsonContent.Create(updateProfileDto)
        };
        var response = await host.SendAsync(request);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task PatchApiAccountProfile_ReturnsBadRequest_WhenPasswordMismatch()
    {
        await using var host = await ApiIntegrationHost.CreateAsync();
        host.AuthenticateAs(userId: "user-123", username: "testuser", role: "Student");

        var updateProfileDto = new UpdateProfile
        {
            CurrentPassword = "OldPassword123!",
            NewPassword = "NewPassword123!",
            ConfirmNewPassword = "DifferentPassword123!"
        };

        // PATCH /api/account/profile
        var request = new HttpRequestMessage(HttpMethod.Patch, "/api/account/profile")
        {
            Content = JsonContent.Create(updateProfileDto)
        };
        var response = await host.SendAsync(request);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task PatchApiAccountProfile_VerifiesServiceReceivesPasswordFields()
    {
        await using var host = await ApiIntegrationHost.CreateAsync();
        host.AuthenticateAs(userId: "user-123", username: "testuser", role: "Student");

        var updateProfileDto = new UpdateProfile
        {
            CurrentPassword = "OldPassword123!",
            NewPassword = "NewPassword123!",
            ConfirmNewPassword = "NewPassword123!"
        };

        var updatedProfile = new UserProfileResponseDto
        {
            UserId = "user-123",
            Username = "testuser",
            Email = "test@test.com",
            Role = "Student"
        };

        host.AccountService
            .Setup(service => service.UpdateUserProfileAsync(
                It.Is<string>(userId => userId == "user-123"),
                It.Is<UpdateProfile>(dto =>
                    dto.CurrentPassword == "OldPassword123!" &&
                    dto.NewPassword == "NewPassword123!" &&
                    dto.ConfirmNewPassword == "NewPassword123!")))
            .ReturnsAsync(updatedProfile);

        // PATCH /api/account/profile
        var request = new HttpRequestMessage(HttpMethod.Patch, "/api/account/profile")
        {
            Content = JsonContent.Create(updateProfileDto)
        };
        await host.SendAsync(request);

        // Verify the service was called with the correct password fields
        host.AccountService.Verify(
            service => service.UpdateUserProfileAsync(
                It.Is<string>(userId => userId == "user-123"),
                It.Is<UpdateProfile>(dto =>
                    dto.CurrentPassword == "OldPassword123!" &&
                    dto.NewPassword == "NewPassword123!" &&
                    dto.ConfirmNewPassword == "NewPassword123!")),
            Times.Once);
    }
}
