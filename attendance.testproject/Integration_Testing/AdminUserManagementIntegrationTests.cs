using System.Net;
using System.Net.Http.Json;
using attendance.testproject.Integration_Testing.Support;
using attendance_monitoring.Classes;
using attendance_monitoring.Data;
using attendance_monitoring.Models.DTO.Request;
using attendance_monitoring.Models.DTO.Response;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace attendance.testproject.Integration_Testing;

public sealed class AdminUserManagementIntegrationTests
{
    [RequiresEnvironmentVariableFact("ATTENDANCE_TEST_SQLSERVER_CONNECTION")]
    public async Task GetApiUsers_ReturnsUnauthorized_WhenUnauthenticated()
    {
        await using var host = await ApiIntegrationHost.CreateAdminUserManagementAsync();

        var response = await host.Client.GetAsync("/api/users");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [RequiresEnvironmentVariableFact("ATTENDANCE_TEST_SQLSERVER_CONNECTION")]
    public async Task GetApiUsers_ReturnsForbidden_ForNonAdmin()
    {
        await using var host = await ApiIntegrationHost.CreateAdminUserManagementAsync();
        host.AuthenticateAs("student-claims", "student.claims@test.com", "Student");

        var response = await host.Client.GetAsync("/api/users");

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [RequiresEnvironmentVariableFact("ATTENDANCE_TEST_SQLSERVER_CONNECTION")]
    public async Task GetApiUsers_ReturnsOnlyActiveUsers_ByDefault()
    {
        await using var host = await ApiIntegrationHost.CreateAdminUserManagementAsync();
        var scenario = AuthenticateAsAdmin(host);

        var users = await GetUsersAsync(host, "/api/users");

        Assert.Contains(users, user => user.UserId == scenario.AdminUserId);
        Assert.Contains(users, user => user.UserId == scenario.ActiveStudentUserId && user.StudentProfile?.IsDeleted == false);
        Assert.Contains(users, user => user.UserId == scenario.ActiveInstructorUserId && user.InstructorProfile?.IsDeleted == false);
        Assert.DoesNotContain(users, user => user.UserId == scenario.ArchivedStudentUserId);
    }

    [RequiresEnvironmentVariableFact("ATTENDANCE_TEST_SQLSERVER_CONNECTION")]
    public async Task GetApiUsers_WithArchivedStatus_ReturnsArchivedUsers()
    {
        await using var host = await ApiIntegrationHost.CreateAdminUserManagementAsync();
        var scenario = AuthenticateAsAdmin(host);

        var users = await GetUsersAsync(host, "/api/users?status=Archived");

        var archivedUser = Assert.Single(users);
        Assert.Equal(scenario.ArchivedStudentUserId, archivedUser.UserId);
        Assert.NotNull(archivedUser.StudentProfile);
        Assert.True(archivedUser.StudentProfile.IsDeleted);
        Assert.NotNull(archivedUser.StudentProfile.DeletedAt);
    }

    [RequiresEnvironmentVariableFact("ATTENDANCE_TEST_SQLSERVER_CONNECTION")]
    public async Task GetApiUsers_WithAllStatus_ReturnsActiveAndArchivedUsers()
    {
        await using var host = await ApiIntegrationHost.CreateAdminUserManagementAsync();
        var scenario = AuthenticateAsAdmin(host);

        var users = await GetUsersAsync(host, "/api/users?status=All");

        Assert.Contains(users, user => user.UserId == scenario.ActiveStudentUserId);
        Assert.Contains(users, user => user.UserId == scenario.ArchivedStudentUserId);
        Assert.Contains(users, user => user.UserId == scenario.ActiveInstructorUserId);
        Assert.Contains(users, user => user.UserId == scenario.ConflictStudentUserId);
        Assert.Contains(users, user => user.UserId == scenario.AdminUserId);
    }

    [RequiresEnvironmentVariableFact("ATTENDANCE_TEST_SQLSERVER_CONNECTION")]
    public async Task PatchApiAccountAdminUsersTarget_UpdatesStudentProfile_AndRouteIdWins()
    {
        await using var host = await ApiIntegrationHost.CreateAdminUserManagementAsync();
        var scenario = AuthenticateAsAdmin(host);

        var response = await host.SendAsync(new HttpRequestMessage(HttpMethod.Patch, $"/api/account/admin/users/{scenario.ActiveStudentUserId}")
        {
            Content = JsonContent.Create(new AdminUpdateUser
            {
                UserId = scenario.ArchivedStudentUserId,
                Firstname = "Routed",
                Lastname = "Target",
                Email = "student.active.updated@test.com",
                SectionId = scenario.AlternateSectionId,
                IsRegular = false
            })
        });
        var payload = await response.Content.ReadFromJsonAsync<UpdateProfileResponse>();

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.NotNull(payload);
        Assert.True(payload.Success);
        Assert.NotNull(payload.UpdatedProfile);
        Assert.Equal(scenario.ActiveStudentUserId, payload.UpdatedProfile.UserId);
        Assert.Equal("student.active.updated@test.com", payload.UpdatedProfile.Email);
        Assert.Equal("Routed", payload.UpdatedProfile.StudentProfile?.Firstname);
        Assert.Equal(scenario.AlternateSectionId, payload.UpdatedProfile.StudentProfile?.SectionId);

        var dbAssertion = await host.ExecuteDbContextAsync(async (dbContext, cancellationToken) =>
        {
            var activeStudent = await dbContext.Students
                .Include(student => student.User)
                .SingleAsync(student => student.UserId == scenario.ActiveStudentUserId, cancellationToken);
            var archivedStudent = await dbContext.Students
                .SingleAsync(student => student.UserId == scenario.ArchivedStudentUserId, cancellationToken);

            return new
            {
                activeStudent.Firstname,
                activeStudent.Lastname,
                activeStudent.SectionId,
                activeStudent.IsRegular,
                Email = activeStudent.User.Email,
                ArchivedFirstname = archivedStudent.Firstname
            };
        });

        Assert.Equal("Routed", dbAssertion.Firstname);
        Assert.Equal("Target", dbAssertion.Lastname);
        Assert.Equal(scenario.AlternateSectionId, dbAssertion.SectionId);
        Assert.False(dbAssertion.IsRegular);
        Assert.Equal("student.active.updated@test.com", dbAssertion.Email);
        Assert.Equal("Archie", dbAssertion.ArchivedFirstname);
    }

    [RequiresEnvironmentVariableFact("ATTENDANCE_TEST_SQLSERVER_CONNECTION")]
    public async Task PatchApiAccountAdminUsersTarget_ResetsPasswordWithoutCurrentPassword()
    {
        await using var host = await ApiIntegrationHost.CreateAdminUserManagementAsync();
        var scenario = AuthenticateAsAdmin(host);
        const string newPassword = "Reset@5678";

        var response = await host.SendAsync(new HttpRequestMessage(HttpMethod.Patch, $"/api/account/admin/users/{scenario.ActiveStudentUserId}")
        {
            Content = JsonContent.Create(new AdminUpdateUser
            {
                NewPassword = newPassword
            })
        });

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var passwordCheck = await host.ExecuteDbContextAsync(async (_, _) =>
        {
            await using var scope = host.Services.CreateAsyncScope();
            var userManager = scope.ServiceProvider.GetRequiredService<UserManager<IdentityUser>>();
            var user = await userManager.FindByIdAsync(scenario.ActiveStudentUserId);
            Assert.NotNull(user);
            return await userManager.CheckPasswordAsync(user, newPassword);
        });

        Assert.True(passwordCheck);
    }

    [RequiresEnvironmentVariableFact("ATTENDANCE_TEST_SQLSERVER_CONNECTION")]
    public async Task PatchApiAccountAdminUsersTarget_ReturnsConflict_ForDuplicateEmail()
    {
        await using var host = await ApiIntegrationHost.CreateAdminUserManagementAsync();
        var scenario = AuthenticateAsAdmin(host);

        var response = await host.SendAsync(new HttpRequestMessage(HttpMethod.Patch, $"/api/account/admin/users/{scenario.ActiveStudentUserId}")
        {
            Content = JsonContent.Create(new AdminUpdateUser
            {
                Email = scenario.ConflictStudentEmail
            })
        });
        var payload = await response.Content.ReadFromJsonAsync<UpdateProfileResponse>();

        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
        Assert.NotNull(payload);
        Assert.False(payload.Success);
    }

    [RequiresEnvironmentVariableFact("ATTENDANCE_TEST_SQLSERVER_CONNECTION")]
    public async Task PatchApiAccountAdminUsersTarget_ReturnsNotFound_ForUnknownSection()
    {
        await using var host = await ApiIntegrationHost.CreateAdminUserManagementAsync();
        var scenario = AuthenticateAsAdmin(host);

        var response = await host.SendAsync(new HttpRequestMessage(HttpMethod.Patch, $"/api/account/admin/users/{scenario.ActiveStudentUserId}")
        {
            Content = JsonContent.Create(new AdminUpdateUser
            {
                SectionId = int.MaxValue
            })
        });
        var payload = await response.Content.ReadFromJsonAsync<UpdateProfileResponse>();

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        Assert.NotNull(payload);
        Assert.False(payload.Success);
    }

    [RequiresEnvironmentVariableFact("ATTENDANCE_TEST_SQLSERVER_CONNECTION")]
    public async Task PatchApiAccountAdminUsersTarget_ReturnsBadRequest_ForInvalidModel()
    {
        await using var host = await ApiIntegrationHost.CreateAdminUserManagementAsync();
        var scenario = AuthenticateAsAdmin(host);

        var response = await host.SendAsync(new HttpRequestMessage(HttpMethod.Patch, $"/api/account/admin/users/{scenario.ActiveStudentUserId}")
        {
            Content = JsonContent.Create(new AdminUpdateUser
            {
                Email = "not-an-email"
            })
        });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [RequiresEnvironmentVariableFact("ATTENDANCE_TEST_SQLSERVER_CONNECTION")]
    public async Task PatchApiUsersSoftDelete_ArchivesUser_AndRevokesOnlyTargetRefreshTokens()
    {
        await using var host = await ApiIntegrationHost.CreateAdminUserManagementAsync();
        var scenario = AuthenticateAsAdmin(host);

        var response = await host.SendAsync(new HttpRequestMessage(HttpMethod.Patch, $"/api/users/{scenario.ActiveStudentUserId}/soft-delete"));
        var payload = await response.Content.ReadFromJsonAsync<DeleteUserResponseDto>();

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.NotNull(payload);
        Assert.True(payload.Success);

        var activeUsers = await GetUsersAsync(host, "/api/users");
        var archivedUsers = await GetUsersAsync(host, "/api/users?status=Archived");

        Assert.DoesNotContain(activeUsers, user => user.UserId == scenario.ActiveStudentUserId);
        Assert.Contains(archivedUsers, user => user.UserId == scenario.ActiveStudentUserId && user.StudentProfile?.IsDeleted == true);

        var tokenState = await host.ExecuteDbContextAsync(async (dbContext, cancellationToken) =>
        {
            var targetToken = await dbContext.RefreshTokens
                .SingleAsync(token => token.Id == scenario.ActiveStudentRefreshTokenId, cancellationToken);
            var controlToken = await dbContext.RefreshTokens
                .SingleAsync(token => token.Id == scenario.ControlRefreshTokenId, cancellationToken);
            var student = await dbContext.Students
                .SingleAsync(row => row.UserId == scenario.ActiveStudentUserId, cancellationToken);

            return new
            {
                targetToken.IsRevoked,
                targetToken.RevokedAt,
                ControlTokenRevoked = controlToken.IsRevoked,
                student.IsDeleted,
                student.DeletedAt
            };
        });

        Assert.True(tokenState.IsRevoked);
        Assert.NotNull(tokenState.RevokedAt);
        Assert.False(tokenState.ControlTokenRevoked);
        Assert.True(tokenState.IsDeleted);
        Assert.NotNull(tokenState.DeletedAt);
    }

    [RequiresEnvironmentVariableFact("ATTENDANCE_TEST_SQLSERVER_CONNECTION")]
    public async Task PatchApiUsersSoftDelete_ReturnsBadRequest_WhenAdminDeletesSelf()
    {
        await using var host = await ApiIntegrationHost.CreateAdminUserManagementAsync();
        var scenario = AuthenticateAsAdmin(host);

        var response = await host.SendAsync(new HttpRequestMessage(HttpMethod.Patch, $"/api/users/{scenario.AdminUserId}/soft-delete"));

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [RequiresEnvironmentVariableFact("ATTENDANCE_TEST_SQLSERVER_CONNECTION")]
    public async Task PatchApiUsersRestore_ReactivatesArchivedUser()
    {
        await using var host = await ApiIntegrationHost.CreateAdminUserManagementAsync();
        var scenario = AuthenticateAsAdmin(host);

        var response = await host.SendAsync(new HttpRequestMessage(HttpMethod.Patch, $"/api/users/{scenario.ArchivedStudentUserId}/restore"));
        var payload = await response.Content.ReadFromJsonAsync<DeleteUserResponseDto>();

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.NotNull(payload);
        Assert.True(payload.Success);

        var activeUsers = await GetUsersAsync(host, "/api/users");
        Assert.Contains(activeUsers, user => user.UserId == scenario.ArchivedStudentUserId && user.StudentProfile?.IsDeleted == false);

        var restoreState = await host.ExecuteDbContextAsync(async (dbContext, cancellationToken) =>
        {
            var restoredStudent = await dbContext.Students
                .SingleAsync(student => student.UserId == scenario.ArchivedStudentUserId, cancellationToken);

            return new
            {
                restoredStudent.IsDeleted,
                restoredStudent.DeletedAt
            };
        });

        Assert.False(restoreState.IsDeleted);
        Assert.Null(restoreState.DeletedAt);
    }

    [RequiresEnvironmentVariableFact("ATTENDANCE_TEST_SQLSERVER_CONNECTION")]
    public async Task PatchApiUsersRestore_ReturnsBadRequest_WhenUserIsAlreadyActive()
    {
        await using var host = await ApiIntegrationHost.CreateAdminUserManagementAsync();
        var scenario = AuthenticateAsAdmin(host);

        var response = await host.SendAsync(new HttpRequestMessage(HttpMethod.Patch, $"/api/users/{scenario.ActiveStudentUserId}/restore"));

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [RequiresEnvironmentVariableFact("ATTENDANCE_TEST_SQLSERVER_CONNECTION")]
    public async Task DeleteApiUsers_RemovesUserAndProfile()
    {
        await using var host = await ApiIntegrationHost.CreateAdminUserManagementAsync();
        var scenario = AuthenticateAsAdmin(host);

        var response = await host.SendAsync(new HttpRequestMessage(HttpMethod.Delete, $"/api/users/{scenario.ActiveInstructorUserId}"));
        var payload = await response.Content.ReadFromJsonAsync<DeleteUserResponseDto>();

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.NotNull(payload);
        Assert.True(payload.Success);

        var users = await GetUsersAsync(host, "/api/users?status=All");
        Assert.DoesNotContain(users, user => user.UserId == scenario.ActiveInstructorUserId);

        var hardDeleteState = await host.ExecuteDbContextAsync(async (dbContext, cancellationToken) =>
        {
            var user = await dbContext.Users
                .FirstOrDefaultAsync(identity => identity.Id == scenario.ActiveInstructorUserId, cancellationToken);
            var instructor = await dbContext.Instructors
                .FirstOrDefaultAsync(row => row.UserId == scenario.ActiveInstructorUserId, cancellationToken);

            return new
            {
                UserExists = user != null,
                InstructorExists = instructor != null
            };
        });

        Assert.False(hardDeleteState.UserExists);
        Assert.False(hardDeleteState.InstructorExists);
    }

    [RequiresEnvironmentVariableFact("ATTENDANCE_TEST_SQLSERVER_CONNECTION")]
    public async Task DeleteApiUsers_ReturnsBadRequest_WhenAdminDeletesSelf()
    {
        await using var host = await ApiIntegrationHost.CreateAdminUserManagementAsync();
        var scenario = AuthenticateAsAdmin(host);

        var response = await host.SendAsync(new HttpRequestMessage(HttpMethod.Delete, $"/api/users/{scenario.AdminUserId}"));

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    private static AdminUserManagementScenarioContext AuthenticateAsAdmin(ApiIntegrationHost host)
    {
        var scenario = host.AdminUserManagementScenario ?? throw new InvalidOperationException("Admin user management scenario was not loaded.");
        host.AuthenticateAs(scenario.AdminUserId, scenario.AdminEmail, "Admin");
        return scenario;
    }

    private static async Task<IReadOnlyList<GetAllUsersDto>> GetUsersAsync(ApiIntegrationHost host, string url)
    {
        var response = await host.Client.GetAsync(url);
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var payload = await response.Content.ReadFromJsonAsync<List<GetAllUsersDto>>();
        Assert.NotNull(payload);
        return payload;
    }
}
