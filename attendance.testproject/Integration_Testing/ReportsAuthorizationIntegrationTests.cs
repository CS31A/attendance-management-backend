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
        var studentId = host.ReportsScenario!.StudentId;
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
        var sessionId = host.ReportsScenario!.SessionId;
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
        var sectionId = host.ReportsScenario!.SectionId;
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
        var instructorId = host.ReportsScenario!.InstructorId;
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
        var sessionId = host.ReportsScenario!.SessionId;
        host.AuthenticateAs(userId: "test-student", username: "test-student", role: "Student");

        var response = await host.Client.GetAsync($"/api/reports/session-attendance/{sessionId}");

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task GetClassAttendanceReport_ReturnsForbidden_ForStudent()
    {
        await using var host = await ApiIntegrationHost.CreateReportsAsync();
        var sectionId = host.ReportsScenario!.SectionId;
        host.AuthenticateAs(userId: "test-student", username: "test-student", role: "Student");

        var response = await host.Client.GetAsync($"/api/reports/class-attendance/{sectionId}");

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task GetInstructorSessionsReport_ReturnsForbidden_ForStudent()
    {
        await using var host = await ApiIntegrationHost.CreateReportsAsync();
        var instructorId = host.ReportsScenario!.InstructorId;
        host.AuthenticateAs(userId: "test-student", username: "test-student", role: "Student");

        var response = await host.Client.GetAsync($"/api/reports/instructor-sessions/{instructorId}");

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    #endregion

    #region Unauthenticated Access

    [Theory]
    [InlineData("/api/reports/attendance-summary")]
    [InlineData("/api/reports/student-attendance/1")]
    [InlineData("/api/reports/session-attendance/1")]
    [InlineData("/api/reports/class-attendance/1")]
    [InlineData("/api/reports/instructor-sessions/1")]
    public async Task ReportsEndpoints_ReturnUnauthorized_ForUnauthenticatedRequests(string endpoint)
    {
        await using var host = await ApiIntegrationHost.CreateReportsAsync();
        host.ClearAuthentication();

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
        var targetInstructorId = host.ReportsScenario!.InstructorId;
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
        var sectionId = host.ReportsScenario!.SectionId;
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
        var studentId = host.ReportsScenario!.StudentId;
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
        var otherStudentId = host.ReportsScenario!.OutsiderStudentId;
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
        var instructorId = host.ReportsScenario!.InstructorId;
        host.AuthenticateAs(userId: "test-admin", username: "test-admin", role: "Admin");

        var response = await host.Client.GetAsync($"/api/reports/instructor-sessions/{instructorId}");

        Assert.NotEqual(HttpStatusCode.Forbidden, response.StatusCode);
        Assert.NotEqual(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task GetSessionAttendanceReport_ReturnsOk_WhenAdminViewsAnySession()
    {
        await using var host = await ApiIntegrationHost.CreateReportsAsync();
        var sessionId = host.ReportsScenario!.SessionId;
        host.AuthenticateAs(userId: "test-admin", username: "test-admin", role: "Admin");

        var response = await host.Client.GetAsync($"/api/reports/session-attendance/{sessionId}");

        Assert.NotEqual(HttpStatusCode.Forbidden, response.StatusCode);
        Assert.NotEqual(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task GetStudentAttendanceReport_ReturnsOk_WhenAdminViewsAnyStudentReport()
    {
        await using var host = await ApiIntegrationHost.CreateReportsAsync();
        var studentId = host.ReportsScenario!.StudentId;
        host.AuthenticateAs(userId: "test-admin", username: "test-admin", role: "Admin");

        var response = await host.Client.GetAsync($"/api/reports/student-attendance/{studentId}");

        Assert.NotEqual(HttpStatusCode.Forbidden, response.StatusCode);
        Assert.NotEqual(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    #endregion
}
