using System.Net;
using System.Net.Http.Json;
using System.Text.Json.Nodes;
using attendance.testproject.Integration_Testing.Support;
using attendance_monitoring.Models.DTO.Request;
using attendance_monitoring.Models.DTO.Response;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;

namespace attendance.testproject.Integration_Testing;

public sealed class AttendanceFlowIntegrationTests
{
    [Fact]
    public async Task CreateAttendance_ReturnsCreated_WithApprovedPayload_AndPersistsRecord()
    {
        await using var host = await ApiIntegrationHost.CreateAttendanceQrAsync(AttendanceQrSeedData.ValidAttendanceCreate);
        host.AuthenticateAs(userId: host.AttendanceQrScenario!.InstructorUserId, username: "integration-instructor", role: "Instructor");

        var request = new CreateAttendanceRequest
        {
            StudentId = host.AttendanceQrScenario.StudentId,
            SessionId = host.AttendanceQrScenario.SessionId,
            Status = "Present",
            Notes = "Manual integration check-in"
        };

        var response = await host.PostAsJsonAsync("/api/attendance", request);
        var payload = await response.Content.ReadFromJsonAsync<AttendanceRecordResponseDto>();

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        Assert.NotNull(payload);
        Assert.True(payload.Id > 0);
        Assert.Equal(request.StudentId, payload.StudentId);
        Assert.Equal(request.SessionId, payload.SessionId);
        Assert.Equal("Sam Student", payload.StudentName);
        Assert.Equal(request.Status, payload.Status);
        Assert.Equal(request.Notes, payload.Notes);
        Assert.True(payload.IsManualEntry);
        Assert.Equal(host.AttendanceQrScenario.InstructorUserId, payload.EnteredBy);
        Assert.Equal("Integration Testing", payload.SubjectName);
        Assert.Equal("INT-SEC-A", payload.SectionName);
        Assert.Equal("Integration Room 1", payload.RoomName);
        Assert.Equal("Ivy Instructor", payload.InstructorName);
        Assert.Equal($"/api/attendance/{payload.Id}", response.Headers.Location?.AbsolutePath);

        var persisted = await host.ExecuteDbContextAsync(async (dbContext, cancellationToken) =>
            await dbContext.AttendanceRecords
                .SingleAsync(record =>
                    record.StudentId == request.StudentId &&
                    record.SessionId == request.SessionId,
                    cancellationToken));

        Assert.Equal(payload.Id, persisted.Id);
        Assert.True(persisted.IsManualEntry);
        Assert.Equal(request.Notes, persisted.Notes);
        Assert.Equal(host.AttendanceQrScenario.InstructorUserId, persisted.EnteredBy);
    }

    [Fact]
    public async Task GetAttendance_ReturnsOk_WithApprovedReadShape()
    {
        await using var host = await ApiIntegrationHost.CreateAttendanceQrAsync(AttendanceQrSeedData.ExistingAttendanceDuplicate);
        host.AuthenticateAs(userId: host.AttendanceQrScenario!.InstructorUserId, username: "integration-instructor", role: "Instructor");

        var response = await host.Client.GetAsync($"/api/attendance/{host.AttendanceQrScenario.ExistingAttendanceRecordId}");
        var payload = await response.Content.ReadFromJsonAsync<AttendanceRecordResponseDto>();

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.NotNull(payload);
        Assert.Equal(host.AttendanceQrScenario.ExistingAttendanceRecordId, payload.Id);
        Assert.Equal(host.AttendanceQrScenario.StudentId, payload.StudentId);
        Assert.Equal(host.AttendanceQrScenario.SessionId, payload.SessionId);
        Assert.Equal("Present", payload.Status);
        Assert.True(payload.IsManualEntry);
        Assert.Equal("Sam Student", payload.StudentName);
        Assert.Equal("Integration Testing", payload.SubjectName);
        Assert.Equal("INT-SEC-A", payload.SectionName);
        Assert.Equal("Integration Room 1", payload.RoomName);
        Assert.Equal("Ivy Instructor", payload.InstructorName);
    }

    [Fact]
    public async Task CreateAttendance_ReturnsConflict_WhenAttendanceAlreadyExists()
    {
        await using var host = await ApiIntegrationHost.CreateAttendanceQrAsync(AttendanceQrSeedData.ExistingAttendanceDuplicate);
        host.AuthenticateAs(userId: host.AttendanceQrScenario!.InstructorUserId, username: "integration-instructor", role: "Instructor");

        var response = await host.PostAsJsonAsync("/api/attendance", new CreateAttendanceRequest
        {
            StudentId = host.AttendanceQrScenario.StudentId,
            SessionId = host.AttendanceQrScenario.SessionId,
            Status = "Present"
        });

        var payload = await ReadJsonObjectAsync(response);

        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
        Assert.Equal("Attendance record already exists for this student and session", payload["message"]?.GetValue<string>());
    }

    [Fact]
    public async Task CreateAttendance_ReturnsConflict_WhenStudentIsNotEnrolledInSession()
    {
        await using var host = await ApiIntegrationHost.CreateAttendanceQrAsync(AttendanceQrSeedData.ValidAttendanceCreate);
        host.AuthenticateAs(userId: host.AttendanceQrScenario!.InstructorUserId, username: "integration-instructor", role: "Instructor");

        var response = await host.PostAsJsonAsync("/api/attendance", new CreateAttendanceRequest
        {
            StudentId = host.AttendanceQrScenario.OutsiderStudentId,
            SessionId = host.AttendanceQrScenario.SessionId,
            Status = "Present"
        });

        var payload = await ReadJsonObjectAsync(response);

        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
        Assert.Equal("Student is not enrolled in this session's section or subject", payload["message"]?.GetValue<string>());
    }

    [Fact]
    public async Task CreateAttendance_ReturnsNotFound_WhenStudentOrSessionIsMissing()
    {
        await using var host = await ApiIntegrationHost.CreateAttendanceQrAsync(AttendanceQrSeedData.ValidAttendanceCreate);
        host.AuthenticateAs(userId: host.AttendanceQrScenario!.InstructorUserId, username: "integration-instructor", role: "Instructor");

        var missingStudentResponse = await host.PostAsJsonAsync("/api/attendance", new CreateAttendanceRequest
        {
            StudentId = int.MaxValue,
            SessionId = host.AttendanceQrScenario.SessionId,
            Status = "Present"
        });
        var missingStudentPayload = await ReadJsonObjectAsync(missingStudentResponse);

        Assert.Equal(HttpStatusCode.NotFound, missingStudentResponse.StatusCode);
        Assert.Equal($"Student with ID {int.MaxValue} was not found.", missingStudentPayload["message"]?.GetValue<string>());

        var missingSessionResponse = await host.PostAsJsonAsync("/api/attendance", new CreateAttendanceRequest
        {
            StudentId = host.AttendanceQrScenario.StudentId,
            SessionId = int.MaxValue,
            Status = "Present"
        });
        var missingSessionPayload = await ReadJsonObjectAsync(missingSessionResponse);

        Assert.Equal(HttpStatusCode.NotFound, missingSessionResponse.StatusCode);
        Assert.Equal($"Session with ID {int.MaxValue} was not found.", missingSessionPayload["message"]?.GetValue<string>());
    }

    private static async Task<JsonObject> ReadJsonObjectAsync(HttpResponseMessage response)
    {
        var json = await response.Content.ReadAsStringAsync();
        try
        {
            return JsonNode.Parse(json)?.AsObject()
                ?? throw new InvalidOperationException(
                    $"Expected JSON object response but received status {(int)response.StatusCode} {response.StatusCode} with body: {json}");
        }
        catch (System.Text.Json.JsonException ex)
        {
            throw new InvalidOperationException(
                $"Expected JSON object response but received status {(int)response.StatusCode} {response.StatusCode} with body: {json}",
                ex);
        }
    }
}
