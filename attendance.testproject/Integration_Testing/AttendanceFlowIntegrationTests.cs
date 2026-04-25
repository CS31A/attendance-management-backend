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

        var (studentUuid, sessionUuid) = await ResolveStudentAndSessionUuidsAsync(host);

        var request = new CreateAttendanceRequest
        {
            StudentId = studentUuid,
            SessionId = sessionUuid,
            Status = "Present",
            Notes = "Manual integration check-in"
        };

        var response = await host.PostAsJsonAsync("/api/attendance", request);
        var payload = await response.Content.ReadFromJsonAsync<AttendanceRecordResponseDto>();

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        Assert.NotNull(payload);
        Assert.NotEqual(Guid.Empty, payload.Id);
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
        Assert.Contains(payload.Id.ToString(), response.Headers.Location?.ToString());

        var persisted = await host.ExecuteDbContextAsync(async (dbContext, cancellationToken) =>
            await dbContext.AttendanceRecords
                .SingleAsync(record =>
                    record.StudentId == host.AttendanceQrScenario!.StudentId &&
                    record.SessionId == host.AttendanceQrScenario.SessionId,
                    cancellationToken));

        Assert.Equal(payload.Id, persisted.Uuid);
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
        var (studentUuid, sessionUuid) = await ResolveStudentAndSessionUuidsAsync(host);
        var attendanceUuid = await host.ExecuteDbContextAsync(async (dbContext, cancellationToken) =>
            await dbContext.AttendanceRecords
                .AsNoTracking()
                .Where(record => record.Id == host.AttendanceQrScenario.ExistingAttendanceRecordId)
                .Select(record => record.Uuid)
                .SingleAsync(cancellationToken));

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.NotNull(payload);
        Assert.Equal(attendanceUuid, payload.Id);
        Assert.Equal(studentUuid, payload.StudentId);
        Assert.Equal(sessionUuid, payload.SessionId);
        Assert.Equal("Present", payload.Status);
        Assert.True(payload.IsManualEntry);
        Assert.Equal("Sam Student", payload.StudentName);
        Assert.Equal("Integration Testing", payload.SubjectName);
        Assert.Equal("INT-SEC-A", payload.SectionName);
        Assert.Equal("Integration Room 1", payload.RoomName);
        Assert.Equal("Ivy Instructor", payload.InstructorName);
    }

    [Fact]
    public async Task CreateAttendance_ReturnsOk_WhenAttendanceAlreadyExistsForEquivalentRetry()
    {
        await using var host = await ApiIntegrationHost.CreateAttendanceQrAsync(AttendanceQrSeedData.ValidAttendanceCreate);
        host.AuthenticateAs(userId: host.AttendanceQrScenario!.InstructorUserId, username: "integration-instructor", role: "Instructor");

        var (studentUuid, sessionUuid) = await ResolveStudentAndSessionUuidsAsync(host);

        var initialRequest = new CreateAttendanceRequest
        {
            StudentId = studentUuid,
            SessionId = sessionUuid,
            Status = "Present",
            Notes = "Initial manual create"
        };

        var initialResponse = await host.PostAsJsonAsync("/api/attendance", initialRequest);
        var initialPayload = await initialResponse.Content.ReadFromJsonAsync<AttendanceRecordResponseDto>();

        Assert.Equal(HttpStatusCode.Created, initialResponse.StatusCode);
        Assert.NotNull(initialPayload);

        var equivalentRetryResponse = await host.PostAsJsonAsync("/api/attendance", new CreateAttendanceRequest
        {
            StudentId = initialRequest.StudentId,
            SessionId = initialRequest.SessionId,
            Status = initialRequest.Status,
            Notes = initialRequest.Notes
        });
        var equivalentRetryPayload = await equivalentRetryResponse.Content.ReadFromJsonAsync<AttendanceRecordResponseDto>();

        Assert.Equal(HttpStatusCode.OK, equivalentRetryResponse.StatusCode);
        Assert.NotNull(equivalentRetryPayload);
        Assert.Equal(initialPayload.Id, equivalentRetryPayload.Id);
        Assert.Equal(initialPayload.StudentId, equivalentRetryPayload.StudentId);
        Assert.Equal(initialPayload.SessionId, equivalentRetryPayload.SessionId);

        var changedPayloadRetryResponse = await host.PostAsJsonAsync("/api/attendance", new CreateAttendanceRequest
        {
            StudentId = initialRequest.StudentId,
            SessionId = initialRequest.SessionId,
            Status = "Late",
            Notes = "Retry with changed payload fields",
            CheckInTime = DateTime.UtcNow.AddMinutes(5)
        });
        var changedPayloadRetry = await changedPayloadRetryResponse.Content.ReadFromJsonAsync<AttendanceRecordResponseDto>();

        Assert.Equal(HttpStatusCode.OK, changedPayloadRetryResponse.StatusCode);
        Assert.NotNull(changedPayloadRetry);
        Assert.Equal(initialPayload.Id, changedPayloadRetry.Id);
        Assert.Equal(initialPayload.StudentId, changedPayloadRetry.StudentId);
        Assert.Equal(initialPayload.SessionId, changedPayloadRetry.SessionId);

        var persistedCount = await host.ExecuteDbContextAsync(async (dbContext, cancellationToken) =>
            await dbContext.AttendanceRecords
                .CountAsync(record =>
                    record.StudentId == host.AttendanceQrScenario!.StudentId &&
                    record.SessionId == host.AttendanceQrScenario.SessionId,
                    cancellationToken));

        Assert.Equal(1, persistedCount);
    }

    [Fact]
    public async Task CreateAttendance_ReturnsConflict_WhenStudentIsNotEnrolledInSession()
    {
        await using var host = await ApiIntegrationHost.CreateAttendanceQrAsync(AttendanceQrSeedData.ValidAttendanceCreate);
        host.AuthenticateAs(userId: host.AttendanceQrScenario!.InstructorUserId, username: "integration-instructor", role: "Instructor");

        var (_, sessionUuid) = await ResolveStudentAndSessionUuidsAsync(host);
        var outsiderStudentUuid = await host.ExecuteDbContextAsync(async (dbContext, cancellationToken) =>
            await dbContext.Students
                .AsNoTracking()
                .Where(student => student.Id == host.AttendanceQrScenario.OutsiderStudentId)
                .Select(student => student.Uuid)
                .SingleAsync(cancellationToken));

        var response = await host.PostAsJsonAsync("/api/attendance", new CreateAttendanceRequest
        {
            StudentId = outsiderStudentUuid,
            SessionId = sessionUuid,
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

        var (studentUuid, sessionUuid) = await ResolveStudentAndSessionUuidsAsync(host);
        var missingStudentUuid = Guid.NewGuid();
        var missingSessionUuid = Guid.NewGuid();

        var missingStudentResponse = await host.PostAsJsonAsync("/api/attendance", new CreateAttendanceRequest
        {
            StudentId = missingStudentUuid,
            SessionId = sessionUuid,
            Status = "Present"
        });
        var missingStudentPayload = await ReadJsonObjectAsync(missingStudentResponse);

        Assert.Equal(HttpStatusCode.NotFound, missingStudentResponse.StatusCode);
        Assert.Equal($"Student with ID {missingStudentUuid} was not found.", missingStudentPayload["message"]?.GetValue<string>());

        var missingSessionResponse = await host.PostAsJsonAsync("/api/attendance", new CreateAttendanceRequest
        {
            StudentId = studentUuid,
            SessionId = missingSessionUuid,
            Status = "Present"
        });
        var missingSessionPayload = await ReadJsonObjectAsync(missingSessionResponse);

        Assert.Equal(HttpStatusCode.NotFound, missingSessionResponse.StatusCode);
        Assert.Equal($"Session with ID {missingSessionUuid} was not found.", missingSessionPayload["message"]?.GetValue<string>());
    }

    private static async Task<(Guid StudentUuid, Guid SessionUuid)> ResolveStudentAndSessionUuidsAsync(ApiIntegrationHost host)
    {
        return await host.ExecuteDbContextAsync(async (dbContext, cancellationToken) =>
        {
            var studentUuid = await dbContext.Students
                .AsNoTracking()
                .Where(student => student.Id == host.AttendanceQrScenario!.StudentId)
                .Select(student => student.Uuid)
                .SingleAsync(cancellationToken);

            var sessionUuid = await dbContext.Sessions
                .AsNoTracking()
                .Where(session => session.Id == host.AttendanceQrScenario!.SessionId)
                .Select(session => session.Uuid)
                .SingleAsync(cancellationToken);

            return (studentUuid, sessionUuid);
        });
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
