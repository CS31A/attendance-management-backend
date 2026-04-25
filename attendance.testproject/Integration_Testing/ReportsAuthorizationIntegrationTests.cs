using System.Net;
using attendance.testproject.Integration_Testing.Support;
using attendance_monitoring.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace attendance.testproject.Integration_Testing;

/// <summary>
/// Integration tests verifying policy-based authorization for ReportsController endpoints.
/// Tests role-based access control using UserPolicy (Admin/Instructor/Student) 
/// and PrivilegedPolicy (Admin/Instructor only).
/// </summary>
public sealed class ReportsAuthorizationIntegrationTests
{
    #region UserPolicy Endpoints - Accessible to All Roles

    [Theory]
    [InlineData("Admin")]
    [InlineData("Instructor")]
    [InlineData("Student")]
    public async Task GetAttendanceSummary_ReturnsOk_ForAuthorizedRoles(string role)
    {
        await using var host = await ApiIntegrationHost.CreateReportsAsync();
        host.AuthenticateAs(userId: $"test-{role.ToLowerInvariant()}", username: $"test-{role.ToLowerInvariant()}", role: role);

        var response = await host.Client.GetAsync("/api/reports/attendance-summary");

        Assert.NotEqual(HttpStatusCode.Forbidden, response.StatusCode);
        Assert.NotEqual(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Theory]
    [InlineData("Admin")]
    [InlineData("Instructor")]
    public async Task GetStudentAttendanceReport_ReturnsOk_ForAdminAndInstructor(string role)
    {
        await using var host = await ApiIntegrationHost.CreateReportsAsync();
        var studentId = host.ReportsScenario!.StudentUuid;
        host.AuthenticateAs(userId: $"test-{role.ToLowerInvariant()}", username: $"test-{role.ToLowerInvariant()}", role: role);

        var response = await host.Client.GetAsync($"/api/reports/student-attendance/{studentId}");

        Assert.NotEqual(HttpStatusCode.Forbidden, response.StatusCode);
        Assert.NotEqual(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    #endregion

    #region PrivilegedPolicy Endpoints - Admin/Instructor Only

    [Theory]
    [InlineData("Admin")]
    [InlineData("Instructor")]
    public async Task GetSessionAttendanceReport_ReturnsOk_ForPrivilegedRoles(string role)
    {
        await using var host = await ApiIntegrationHost.CreateReportsAsync();
        var sessionId = host.ReportsScenario!.SessionUuid;
        host.AuthenticateAs(userId: $"test-{role.ToLowerInvariant()}", username: $"test-{role.ToLowerInvariant()}", role: role);

        var response = await host.Client.GetAsync($"/api/reports/session-attendance/{sessionId}");

        Assert.NotEqual(HttpStatusCode.Forbidden, response.StatusCode);
        Assert.NotEqual(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Theory]
    [InlineData("Admin")]
    [InlineData("Instructor")]
    public async Task GetClassAttendanceReport_ReturnsOk_ForPrivilegedRoles(string role)
    {
        await using var host = await ApiIntegrationHost.CreateReportsAsync();
        var sectionId = host.ReportsScenario!.SectionUuid;
        host.AuthenticateAs(userId: $"test-{role.ToLowerInvariant()}", username: $"test-{role.ToLowerInvariant()}", role: role);

        var response = await host.Client.GetAsync($"/api/reports/class-attendance/{sectionId}");

        Assert.NotEqual(HttpStatusCode.Forbidden, response.StatusCode);
        Assert.NotEqual(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Theory]
    [InlineData("Admin")]
    [InlineData("Instructor")]
    public async Task GetInstructorSessionsReport_ReturnsOk_ForPrivilegedRoles(string role)
    {
        await using var host = await ApiIntegrationHost.CreateReportsAsync();
        var instructorId = host.ReportsScenario!.InstructorUuid;
        host.AuthenticateAs(userId: $"test-{role.ToLowerInvariant()}", username: $"test-{role.ToLowerInvariant()}", role: role);

        var response = await host.Client.GetAsync($"/api/reports/instructor-sessions/{instructorId}");

        Assert.NotEqual(HttpStatusCode.Forbidden, response.StatusCode);
        Assert.NotEqual(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    #endregion

    #region Student Access Restrictions - PrivilegedPolicy Endpoints

    [Fact]
    public async Task GetSessionAttendanceReport_ReturnsForbidden_ForStudent()
    {
        await using var host = await ApiIntegrationHost.CreateReportsAsync();
        var sessionId = host.ReportsScenario!.SessionUuid;
        host.AuthenticateAs(userId: "test-student", username: "test-student", role: "Student");

        var response = await host.Client.GetAsync($"/api/reports/session-attendance/{sessionId}");

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task GetClassAttendanceReport_ReturnsForbidden_ForStudent()
    {
        await using var host = await ApiIntegrationHost.CreateReportsAsync();
        var sectionId = host.ReportsScenario!.SectionUuid;
        host.AuthenticateAs(userId: "test-student", username: "test-student", role: "Student");

        var response = await host.Client.GetAsync($"/api/reports/class-attendance/{sectionId}");

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task GetInstructorSessionsReport_ReturnsForbidden_ForStudent()
    {
        await using var host = await ApiIntegrationHost.CreateReportsAsync();
        var instructorId = host.ReportsScenario!.InstructorUuid;
        host.AuthenticateAs(userId: "test-student", username: "test-student", role: "Student");

        var response = await host.Client.GetAsync($"/api/reports/instructor-sessions/{instructorId}");

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    #endregion

    #region Unauthenticated Access

    [Theory]
    [InlineData("attendance-summary")]
    [InlineData("student-attendance")]
    [InlineData("session-attendance")]
    [InlineData("class-attendance")]
    [InlineData("instructor-sessions")]
    public async Task ReportsEndpoints_ReturnUnauthorized_ForUnauthenticatedRequests(string endpointKind)
    {
        await using var host = await ApiIntegrationHost.CreateReportsAsync();
        host.ClearAuthentication();

        var endpoint = endpointKind switch
        {
            "attendance-summary" => "/api/reports/attendance-summary",
            "student-attendance" => $"/api/reports/student-attendance/{host.ReportsScenario!.StudentUuid}",
            "session-attendance" => $"/api/reports/session-attendance/{host.ReportsScenario!.SessionUuid}",
            "class-attendance" => $"/api/reports/class-attendance/{host.ReportsScenario!.SectionUuid}",
            "instructor-sessions" => $"/api/reports/instructor-sessions/{host.ReportsScenario!.InstructorUuid}",
            _ => throw new ArgumentOutOfRangeException(nameof(endpointKind), endpointKind, null)
        };

        var response = await host.Client.GetAsync(endpoint);

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    #endregion

    #region Cross-Instructor Access (Resource-Level Authorization)

    /// <summary>
    /// Verifies instructors cannot view other instructors' session reports.
    /// Resource-level authorization is enforced in ReportsService.
    /// </summary>
    [Fact]
    public async Task GetInstructorSessionsReport_ReturnsForbidden_WhenInstructorViewsAnotherInstructorsReport()
    {
        await using var host = await ApiIntegrationHost.CreateReportsAsync();
        var targetInstructorId = host.ReportsScenario!.InstructorUuid;
        host.AuthenticateAs(userId: "other-instructor", username: "other-instructor", role: "Instructor");

        var response = await host.Client.GetAsync($"/api/reports/instructor-sessions/{targetInstructorId}");

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    /// <summary>
    /// Verifies instructors cannot view sections they don't teach.
    /// Resource-level authorization is enforced in ReportsService.
    /// </summary>
    [Fact]
    public async Task GetClassAttendanceReport_ReturnsForbidden_WhenInstructorViewsSectionTheyDontTeach()
    {
        await using var host = await ApiIntegrationHost.CreateReportsAsync();
        var sectionId = host.ReportsScenario!.SectionUuid;
        host.AuthenticateAs(userId: "other-instructor", username: "other-instructor", role: "Instructor");

        var response = await host.Client.GetAsync($"/api/reports/class-attendance/{sectionId}");

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    #endregion

    #region Student Self-Access

    [Fact]
    public async Task GetStudentAttendanceReport_ReturnsOk_WhenStudentViewsOwnReport()
    {
        await using var host = await ApiIntegrationHost.CreateReportsAsync();
        var studentId = host.ReportsScenario!.StudentUuid;
        host.AuthenticateAs(userId: "integration-student", username: "integration-student", role: "Student");

        var response = await host.Client.GetAsync($"/api/reports/student-attendance/{studentId}");

        // Should be accessible via UserPolicy and authorized via service layer for own data
        Assert.NotEqual(HttpStatusCode.Forbidden, response.StatusCode);
        Assert.NotEqual(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task GetStudentAttendanceReport_ReturnsForbidden_WhenStudentViewsAnotherStudentsReport()
    {
        await using var host = await ApiIntegrationHost.CreateReportsAsync();
        var otherStudentId = host.ReportsScenario!.OutsiderStudentUuid;
        host.AuthenticateAs(userId: "integration-student", username: "integration-student", role: "Student");

        var response = await host.Client.GetAsync($"/api/reports/student-attendance/{otherStudentId}");

        // Should be forbidden by service layer authorization
        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    #endregion

    #region Admin Full Access

    [Fact]
    public async Task GetInstructorSessionsReport_ReturnsOk_WhenAdminViewsAnyInstructorReport()
    {
        await using var host = await ApiIntegrationHost.CreateReportsAsync();
        var instructorId = host.ReportsScenario!.InstructorUuid;
        host.AuthenticateAs(userId: "test-admin", username: "test-admin", role: "Admin");

        var response = await host.Client.GetAsync($"/api/reports/instructor-sessions/{instructorId}");

        Assert.NotEqual(HttpStatusCode.Forbidden, response.StatusCode);
        Assert.NotEqual(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task GetSessionAttendanceReport_ReturnsOk_WhenAdminViewsAnySession()
    {
        await using var host = await ApiIntegrationHost.CreateReportsAsync();
        var sessionId = host.ReportsScenario!.SessionUuid;
        host.AuthenticateAs(userId: "test-admin", username: "test-admin", role: "Admin");

        var response = await host.Client.GetAsync($"/api/reports/session-attendance/{sessionId}");

        Assert.NotEqual(HttpStatusCode.Forbidden, response.StatusCode);
        Assert.NotEqual(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task GetStudentAttendanceReport_ReturnsOk_WhenAdminViewsAnyStudentReport()
    {
        await using var host = await ApiIntegrationHost.CreateReportsAsync();
        var studentId = host.ReportsScenario!.StudentUuid;
        host.AuthenticateAs(userId: "test-admin", username: "test-admin", role: "Admin");

        var response = await host.Client.GetAsync($"/api/reports/student-attendance/{studentId}");

        Assert.NotEqual(HttpStatusCode.Forbidden, response.StatusCode);
        Assert.NotEqual(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    #endregion
}
