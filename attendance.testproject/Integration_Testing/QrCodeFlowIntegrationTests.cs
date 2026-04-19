using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Nodes;
using attendance.testproject.Integration_Testing.Support;
using attendance_monitoring.Models.DTO.Request;
using attendance_monitoring.Models.DTO.Response;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;

namespace attendance.testproject.Integration_Testing;

public sealed class QrCodeFlowIntegrationTests
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    [Fact]
    public async Task GenerateQrCode_ReturnsOk_WithApprovedPayloadShape()
    {
        await using var host = await ApiIntegrationHost.CreateAttendanceQrAsync(AttendanceQrSeedData.ValidAttendanceCreate);
        host.AuthenticateAs(userId: host.AttendanceQrScenario!.InstructorUserId, username: "integration-instructor", role: "Instructor");

        var response = await host.PostAsJsonAsync("/api/qrcode/generate", new QrCodeRequest
        {
            SessionId = host.AttendanceQrScenario.SessionId,
            ExpirationMinutes = 15,
            MaxUsage = 3,
            UniqueHash = "integration-generate"
        });

        var payload = await ReadJsonObjectAsync(response);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal(true, payload["success"]?.GetValue<bool>());
        Assert.Equal(3, payload["maxUsage"]?.GetValue<int>());
        Assert.NotNull(payload["qrHash"]?.GetValue<string>());
        Assert.NotNull(payload["qrCodeData"]?.GetValue<string>());
        Assert.NotNull(payload["qrCodeImage"]?.GetValue<string>());
        Assert.True(Convert.FromBase64String(payload["qrCodeImage"]!.GetValue<string>()).Length > 0);
        Assert.True(payload["qrCodeId"]?.GetValue<int>() > 0);
        Assert.NotNull(payload["generatedAt"]?.GetValue<DateTime>());
        Assert.NotNull(payload["expiresAt"]?.GetValue<DateTime>());
    }

    [Fact]
    public async Task ValidateQrCode_ReturnsOk_WithApprovedValidationFields()
    {
        await using var host = await ApiIntegrationHost.CreateAttendanceQrAsync(AttendanceQrSeedData.ValidQrScan);

        var response = await host.Client.GetAsync($"/api/qrcode/validate/{host.AttendanceQrScenario!.QrHash}");
        var payload = await response.Content.ReadFromJsonAsync<QrCodeValidationResponseDto>();

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.NotNull(payload);
        Assert.True(payload.IsValid);
        Assert.Equal("QR code is valid", payload.Message);
        Assert.Equal(host.AttendanceQrScenario.QrCodeId, payload.QrCodeId);
        Assert.Equal(host.AttendanceQrScenario.SessionId, payload.ScheduleId);
        Assert.Equal(1, payload.SectionId);
        Assert.Equal(1, payload.ActualRoomId);
        Assert.Equal(10, payload.RemainingUsage);
        Assert.NotNull(payload.ExpiresAt);
        Assert.NotNull(payload.ScheduleTitle);
        Assert.Equal("INT-SEC-A", payload.SectionName);
        Assert.Equal("Integration Room 1", payload.RoomName);
        Assert.Null(payload.SubjectName);
        Assert.Null(payload.InstructorName);
    }

    [Fact]
    public async Task ScanQrCode_ReturnsOk_WithAttendanceMarked_AndPersistsAttendance()
    {
        await using var host = await ApiIntegrationHost.CreateAttendanceQrAsync(AttendanceQrSeedData.ValidQrScan);
        host.AuthenticateAs(userId: host.AttendanceQrScenario!.StudentUserId, username: "integration-student", role: "Student");

        var response = await host.PostAsJsonAsync("/api/qrcode/scan", new ValidateQrCode
        {
            QrHash = host.AttendanceQrScenario.QrHash
        });

        var payload = await response.Content.ReadFromJsonAsync<QrCodeScanResponseDto>();

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.NotNull(payload);
        Assert.True(payload.Success);
        Assert.True(payload.AttendanceMarked);
        Assert.False(payload.IsDuplicateScan);
        Assert.Equal("Attendance marked successfully", payload.Message);
        Assert.Equal("Sam Student", payload.StudentName);
        Assert.Equal("INT-SEC-A", payload.ClassName);
        Assert.Equal("Integration Testing", payload.SubjectName);
        Assert.Equal("Integration Room 1", payload.RoomName);
        Assert.Equal("Ivy Instructor", payload.InstructorName);
        Assert.NotNull(payload.AttendanceTime);
        Assert.True(payload.AttendanceRecordId > 0);
        Assert.NotNull(payload.AttendanceStatus);

        var persisted = await host.ExecuteDbContextAsync(async (dbContext, cancellationToken) =>
            await dbContext.AttendanceRecords
                .SingleAsync(record =>
                    record.StudentId == host.AttendanceQrScenario.StudentId &&
                    record.SessionId == host.AttendanceQrScenario.SessionId,
                    cancellationToken));

        Assert.Equal(payload.AttendanceRecordId, persisted.Id);
        Assert.Equal(host.AttendanceQrScenario.QrCodeId, persisted.QrCodeId);
        Assert.False(persisted.IsManualEntry);
    }

    [Fact]
    public async Task ScanQrCode_ReturnsOkWithFailure_WhenStudentIdDoesNotMatchAuthenticatedStudent()
    {
        await using var host = await ApiIntegrationHost.CreateAttendanceQrAsync(AttendanceQrSeedData.ValidQrScan);
        host.AuthenticateAs(userId: host.AttendanceQrScenario!.StudentUserId, username: "integration-student", role: "Student");

        var response = await host.PostAsJsonAsync("/api/qrcode/scan", new ValidateQrCode
        {
            QrHash = host.AttendanceQrScenario.QrHash,
            StudentId = host.AttendanceQrScenario.StudentId + 1000
        });

        var payload = await response.Content.ReadFromJsonAsync<QrCodeScanResponseDto>();

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.NotNull(payload);
        Assert.False(payload.Success);
        Assert.False(payload.AttendanceMarked);
        Assert.Equal("Student ID does not match authenticated user", payload.Message);
    }

    [Fact]
    public async Task ScanQrCode_ReturnsForbidden_WhenUserIsNotAStudent()
    {
        await using var host = await ApiIntegrationHost.CreateAttendanceQrAsync(AttendanceQrSeedData.ValidQrScan);
        host.AuthenticateAs(userId: host.AttendanceQrScenario!.InstructorUserId, username: "integration-instructor", role: "Instructor");

        var response = await host.PostAsJsonAsync("/api/qrcode/scan", new ValidateQrCode
        {
            QrHash = host.AttendanceQrScenario.QrHash
        });

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task ScanQrCode_ReturnsOk_WithDuplicateFlag_WhenAttendanceAlreadyExists()
    {
        await using var host = await ApiIntegrationHost.CreateAttendanceQrAsync(AttendanceQrSeedData.DuplicateQrScan);
        host.AuthenticateAs(userId: host.AttendanceQrScenario!.StudentUserId, username: "integration-student", role: "Student");

        var response = await host.PostAsJsonAsync("/api/qrcode/scan", new ValidateQrCode
        {
            QrHash = host.AttendanceQrScenario.QrHash
        });

        var payload = await response.Content.ReadFromJsonAsync<QrCodeScanResponseDto>();

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.NotNull(payload);
        Assert.True(payload.Success);
        Assert.False(payload.AttendanceMarked);
        Assert.True(payload.IsDuplicateScan);
        Assert.Equal("You have already checked in for this session", payload.Message);
        Assert.Null(payload.AttendanceRecordId);
        Assert.Equal("Sam Student", payload.StudentName);
    }

    [Fact]
    public async Task ScanQrCode_ConcurrentWithManualAttendance_PersistsSingleAttendanceRecord()
    {
        await using var host = await ApiIntegrationHost.CreateAttendanceQrAsync(AttendanceQrSeedData.ValidQrScan);
        var scenario = host.AttendanceQrScenario!;

        var manualRequest = new HttpRequestMessage(HttpMethod.Post, "/api/attendance")
        {
            Content = JsonContent.Create(new CreateAttendanceRequest
            {
                StudentId = scenario.StudentId,
                SessionId = scenario.SessionId,
                Status = "Present",
                Notes = "Concurrent manual create"
            })
        };
        manualRequest.Headers.Authorization = TestAuthTokenFactory.CreateBearerHeader(
            scenario.InstructorUserId,
            "integration-instructor",
            "Instructor");

        var qrRequest = new HttpRequestMessage(HttpMethod.Post, "/api/qrcode/scan")
        {
            Content = JsonContent.Create(new ValidateQrCode
            {
                QrHash = scenario.QrHash
            })
        };
        qrRequest.Headers.Authorization = TestAuthTokenFactory.CreateBearerHeader(
            scenario.StudentUserId,
            "integration-student",
            "Student");

        var responses = await Task.WhenAll(
            host.SendAsync(manualRequest),
            host.SendAsync(qrRequest));

        var manualResponse = responses[0];
        var qrResponse = responses[1];
        var manualBody = await manualResponse.Content.ReadAsStringAsync();
        var qrBody = await qrResponse.Content.ReadAsStringAsync();
        var manualPayload = JsonSerializer.Deserialize<AttendanceRecordResponseDto>(manualBody, JsonOptions);
        var qrPayload = JsonSerializer.Deserialize<QrCodeScanResponseDto>(qrBody, JsonOptions);

        Assert.True(manualPayload is not null, $"Manual response was not a valid attendance payload. Status: {(int)manualResponse.StatusCode} {manualResponse.StatusCode}. Body: {manualBody}");
        Assert.True(qrPayload is not null, $"QR response was not a valid scan payload. Status: {(int)qrResponse.StatusCode} {qrResponse.StatusCode}. Body: {qrBody}");
        Assert.True(
            manualPayload.StudentId == scenario.StudentId,
            $"Manual response did not target the expected student. Status: {(int)manualResponse.StatusCode} {manualResponse.StatusCode}. Body: {manualBody}");
        Assert.True(
            manualPayload.SessionId == scenario.SessionId,
            $"Manual response did not target the expected session. Status: {(int)manualResponse.StatusCode} {manualResponse.StatusCode}. Body: {manualBody}");
        Assert.Equal(HttpStatusCode.OK, qrResponse.StatusCode);

        var persistedRecords = await host.ExecuteDbContextAsync(async (dbContext, cancellationToken) =>
            await dbContext.AttendanceRecords
                .Where(record =>
                    record.StudentId == scenario.StudentId &&
                    record.SessionId == scenario.SessionId)
                .ToListAsync(cancellationToken));

        Assert.True(
            persistedRecords.Count == 1,
            $"Expected exactly one persisted attendance record but found {persistedRecords.Count}. Manual: {(int)manualResponse.StatusCode} {manualResponse.StatusCode} {manualBody} | QR: {(int)qrResponse.StatusCode} {qrResponse.StatusCode} {qrBody}");

        var persisted = persistedRecords[0];

        Assert.Equal(persisted.Id, manualPayload.Id);

        if (persisted.IsManualEntry)
        {
            Assert.Equal(HttpStatusCode.Created, manualResponse.StatusCode);
            Assert.True(qrPayload.Success);
            Assert.False(qrPayload.AttendanceMarked);
            Assert.True(qrPayload.IsDuplicateScan);
            Assert.Equal("You have already checked in for this session", qrPayload.Message);
            Assert.Null(qrPayload.AttendanceRecordId);
        }
        else
        {
            Assert.Equal(HttpStatusCode.OK, manualResponse.StatusCode);
            Assert.True(qrPayload.Success);
            Assert.True(qrPayload.AttendanceMarked);
            Assert.False(qrPayload.IsDuplicateScan);
            Assert.Equal("Attendance marked successfully", qrPayload.Message);
            Assert.Equal(persisted.Id, qrPayload.AttendanceRecordId);
        }
    }

    [Fact]
    public async Task ValidateQrCode_ReturnsOk_WithInvalidPayload_WhenHashIsMissing()
    {
        await using var host = await ApiIntegrationHost.CreateAttendanceQrAsync(AttendanceQrSeedData.ValidQrScan);

        var response = await host.Client.GetAsync("/api/qrcode/validate/not-a-real-hash");
        var payload = await response.Content.ReadFromJsonAsync<QrCodeValidationResponseDto>();

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.NotNull(payload);
        Assert.False(payload.IsValid);
        Assert.Equal("QR code is invalid, expired, or has reached its usage limit", payload.Message);
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
