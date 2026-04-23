using System.Net;
using System.Net.Http.Json;
using attendance_monitoring.Classes;
using attendance_monitoring.Data;
using attendance_monitoring.Models.DTO.Response;
using attendance.testproject.Integration_Testing.Support;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace attendance.testproject.Integration_Testing;

/// <summary>
/// Integration tests for the instructor sections with students endpoint.
/// Tests the full stack with real database and authentication.
/// </summary>
public sealed class InstructorSectionsIntegrationTests
{
    [RequiresEnvironmentVariableFact("RUN_INTEGRATION_TESTS")]
    public async Task GetMySectionsWithStudents_AuthenticatedInstructor_Returns200WithCorrectDataStructure()
    {
        // Arrange
        await using var host = await CreateHostWithInstructorScenarioAsync();
        var scenario = host.InstructorScenario!;
        
        host.AuthenticateAs(
            userId: scenario.InstructorUserId,
            username: scenario.InstructorUsername,
            role: "Instructor");

        // Act
        var response = await host.Client.GetAsync("/api/instructors/me/sections-with-students");
        var payload = await response.Content.ReadFromJsonAsync<InstructorSectionsWithStudentsResponseDto>();

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.NotNull(payload);
        Assert.Equal(scenario.InstructorId, payload.InstructorId);
        Assert.Equal(scenario.InstructorFirstname, payload.InstructorFirstname);
        Assert.Equal(scenario.InstructorLastname, payload.InstructorLastname);
        Assert.NotNull(payload.Sections);
        Assert.NotEmpty(payload.Sections);
        
        // Verify data structure
        var firstSection = payload.Sections[0];
        Assert.True(firstSection.SectionId > 0);
        Assert.False(string.IsNullOrEmpty(firstSection.SectionName));
        Assert.False(string.IsNullOrEmpty(firstSection.CourseName));
        Assert.NotNull(firstSection.Subjects);
        Assert.NotEmpty(firstSection.Subjects);
        
        var firstSubject = firstSection.Subjects[0];
        Assert.True(firstSubject.SubjectId > 0);
        Assert.False(string.IsNullOrEmpty(firstSubject.SubjectName));
        Assert.False(string.IsNullOrEmpty(firstSubject.SubjectCode));
        Assert.True(firstSubject.ScheduleId > 0);
        Assert.NotNull(firstSubject.Students);
        Assert.NotEmpty(firstSubject.Students);
        Assert.Contains(firstSubject.Students, student => student.IsRegular);
    }

    [RequiresEnvironmentVariableFact("RUN_INTEGRATION_TESTS")]
    public async Task GetMySectionsWithStudents_UnauthenticatedRequest_Returns401Unauthorized()
    {
        // Arrange
        await using var host = await CreateHostWithInstructorScenarioAsync();
        
        // Do not authenticate - no Authorization header

        // Act
        var response = await host.Client.GetAsync("/api/instructors/me/sections-with-students");

        // Assert
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [RequiresEnvironmentVariableFact("RUN_INTEGRATION_TESTS")]
    public async Task GetMySectionsWithStudents_NonInstructorUser_Returns404NotFound()
    {
        // Arrange
        await using var host = await CreateHostWithInstructorScenarioAsync();
        
        // Authenticate as a user who is not an instructor (no instructor record)
        host.AuthenticateAs(
            userId: "non-instructor-user-id",
            username: "student-user",
            role: "Student");

        // Act
        var response = await host.Client.GetAsync("/api/instructors/me/sections-with-students");

        // Assert
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        var content = await response.Content.ReadAsStringAsync();
        Assert.Contains("No instructor record found", content);
    }

    [RequiresEnvironmentVariableFact("RUN_INTEGRATION_TESTS")]
    public async Task GetMySectionsWithStudents_InstructorWithNoSchedules_Returns200WithEmptySections()
    {
        // Arrange
        await using var host = await CreateHostWithInstructorScenarioAsync();
        var scenario = host.InstructorScenario!;
        
        host.AuthenticateAs(
            userId: scenario.InstructorWithNoSchedulesUserId,
            username: scenario.InstructorWithNoSchedulesUsername,
            role: "Instructor");

        // Act
        var response = await host.Client.GetAsync("/api/instructors/me/sections-with-students");
        var payload = await response.Content.ReadFromJsonAsync<InstructorSectionsWithStudentsResponseDto>();

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.NotNull(payload);
        Assert.Equal(scenario.InstructorWithNoSchedulesId, payload.InstructorId);
        Assert.NotNull(payload.Sections);
        Assert.Empty(payload.Sections);
    }

    private static async Task<ApiIntegrationHost> CreateHostWithInstructorScenarioAsync(
        CancellationToken cancellationToken = default)
    {
        var host = await ApiIntegrationHost.CreateInstructorSectionsAsync(cancellationToken);
        return host;
    }
}
