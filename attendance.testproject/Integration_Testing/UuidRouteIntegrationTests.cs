using System.Net;
using System.Net.Http.Json;
using attendance.testproject.Integration_Testing.Support;
using attendance_monitoring.Classes;
using attendance_monitoring.Models.DTO.Response;
using Microsoft.EntityFrameworkCore;

namespace attendance.testproject.Integration_Testing;

public sealed class UuidRouteIntegrationTests
{
    [Fact]
    public async Task GetCourseByUuid_ReturnsSameCourseAsLegacyIntRoute()
    {
        await using var host = await ApiIntegrationHost.CreateAttendanceQrAsync(AttendanceQrSeedData.ValidAttendanceCreate);
        host.AuthenticateAs(userId: host.AttendanceQrScenario!.InstructorUserId, username: "integration-instructor", role: "Instructor");

        var expected = await host.ExecuteDbContextAsync(async (dbContext, cancellationToken) =>
            await dbContext.Courses
                .AsNoTracking()
                .SingleAsync(c => c.Name == "Integration Course", cancellationToken));

        var legacyResponse = await host.Client.GetAsync($"/api/course/{expected.Id}");
        var uuidResponse = await host.Client.GetAsync($"/api/course/{expected.Uuid}");
        var legacyPayload = await legacyResponse.Content.ReadFromJsonAsync<Course>();
        var uuidPayload = await uuidResponse.Content.ReadFromJsonAsync<Course>();

        Assert.Equal(HttpStatusCode.OK, legacyResponse.StatusCode);
        Assert.Equal(HttpStatusCode.OK, uuidResponse.StatusCode);
        Assert.NotNull(legacyPayload);
        Assert.NotNull(uuidPayload);
        Assert.Equal(legacyPayload.Id, uuidPayload.Id);
        Assert.Equal(expected.Id, uuidPayload.Id);
        Assert.Equal(expected.Uuid, uuidPayload.Uuid);
        Assert.Equal(expected.Name, uuidPayload.Name);
    }

    [Fact]
    public async Task GetSubjectByUuid_ReturnsSameSubjectAsLegacyIntRoute()
    {
        await using var host = await ApiIntegrationHost.CreateAttendanceQrAsync(AttendanceQrSeedData.ValidAttendanceCreate);
        host.AuthenticateAs(userId: host.AttendanceQrScenario!.InstructorUserId, username: "integration-instructor", role: "Instructor");

        var expected = await host.ExecuteDbContextAsync(async (dbContext, cancellationToken) =>
            await dbContext.Subjects
                .AsNoTracking()
                .SingleAsync(subject => subject.Code == "ITEST1", cancellationToken));

        var legacyResponse = await host.Client.GetAsync($"/api/subjects/{expected.Id}");
        var uuidResponse = await host.Client.GetAsync($"/api/subjects/{expected.Uuid}");
        var legacyPayload = await legacyResponse.Content.ReadFromJsonAsync<Subject>();
        var uuidPayload = await uuidResponse.Content.ReadFromJsonAsync<Subject>();

        Assert.Equal(HttpStatusCode.OK, legacyResponse.StatusCode);
        Assert.Equal(HttpStatusCode.OK, uuidResponse.StatusCode);
        Assert.NotNull(legacyPayload);
        Assert.NotNull(uuidPayload);
        Assert.Equal(legacyPayload.Id, uuidPayload.Id);
        Assert.Equal(expected.Id, uuidPayload.Id);
        Assert.Equal(expected.Uuid, uuidPayload.Uuid);
        Assert.Equal(expected.Code, uuidPayload.Code);
        Assert.Equal(expected.Name, uuidPayload.Name);
    }

    [Fact]
    public async Task GetSectionByUuid_ReturnsSameSectionAsLegacyIntRoute_WithCourseUuidSidecar()
    {
        await using var host = await ApiIntegrationHost.CreateAttendanceQrAsync(AttendanceQrSeedData.ValidAttendanceCreate);
        host.AuthenticateAs(userId: host.AttendanceQrScenario!.InstructorUserId, username: "integration-instructor", role: "Instructor");

        var expected = await host.ExecuteDbContextAsync(async (dbContext, cancellationToken) =>
            await dbContext.Sections
                .AsNoTracking()
                .Include(section => section.Course)
                .SingleAsync(section => section.Name == "INT-SEC-A", cancellationToken));

        var legacyResponse = await host.Client.GetAsync($"/api/sections/{expected.Id}");
        var uuidResponse = await host.Client.GetAsync($"/api/sections/{expected.Uuid}");
        var legacyPayload = await legacyResponse.Content.ReadFromJsonAsync<SectionResponseDto>();
        var uuidPayload = await uuidResponse.Content.ReadFromJsonAsync<SectionResponseDto>();

        Assert.Equal(HttpStatusCode.OK, legacyResponse.StatusCode);
        Assert.Equal(HttpStatusCode.OK, uuidResponse.StatusCode);
        Assert.NotNull(legacyPayload);
        Assert.NotNull(uuidPayload);
        Assert.Equal(legacyPayload.Id, uuidPayload.Id);
        Assert.Equal(expected.Uuid, uuidPayload.Id);
        Assert.Equal(expected.Course!.Uuid, uuidPayload.CourseId);
        Assert.Equal(expected.Name, uuidPayload.Name);
    }

    [Fact]
    public async Task GetClassroomByUuid_ReturnsSameClassroomAsLegacyIntRoute()
    {
        await using var host = await ApiIntegrationHost.CreateAttendanceQrAsync(AttendanceQrSeedData.ValidAttendanceCreate);
        host.AuthenticateAs(userId: host.AttendanceQrScenario!.InstructorUserId, username: "integration-instructor", role: "Instructor");

        var expected = await host.ExecuteDbContextAsync(async (dbContext, cancellationToken) =>
            await dbContext.Classrooms
                .AsNoTracking()
                .SingleAsync(classroom => classroom.Name == "Integration Room 1", cancellationToken));

        var legacyResponse = await host.Client.GetAsync($"/api/classrooms/{expected.Id}");
        var uuidResponse = await host.Client.GetAsync($"/api/classrooms/{expected.Uuid}");
        var legacyPayload = await legacyResponse.Content.ReadFromJsonAsync<Classroom>();
        var uuidPayload = await uuidResponse.Content.ReadFromJsonAsync<Classroom>();

        Assert.Equal(HttpStatusCode.OK, legacyResponse.StatusCode);
        Assert.Equal(HttpStatusCode.OK, uuidResponse.StatusCode);
        Assert.NotNull(legacyPayload);
        Assert.NotNull(uuidPayload);
        Assert.Equal(legacyPayload.Id, uuidPayload.Id);
        Assert.Equal(expected.Id, uuidPayload.Id);
        Assert.Equal(expected.Uuid, uuidPayload.Uuid);
        Assert.Equal(expected.Name, uuidPayload.Name);
    }

    [Fact]
    public async Task GetScheduleByUuid_ReturnsSameScheduleAsLegacyIntRoute_WithNestedUuidSidecars()
    {
        await using var host = await ApiIntegrationHost.CreateAttendanceQrAsync(AttendanceQrSeedData.ValidAttendanceCreate);
        host.AuthenticateAs(userId: host.AttendanceQrScenario!.InstructorUserId, username: "integration-instructor", role: "Instructor");

        var expected = await host.ExecuteDbContextAsync(async (dbContext, cancellationToken) =>
        {
            var session = await dbContext.Sessions
                .AsNoTracking()
                .Include(item => item.Schedule)
                    .ThenInclude(item => item.Subject)
                .Include(item => item.Schedule)
                    .ThenInclude(item => item.Classroom)
                .Include(item => item.Schedule)
                    .ThenInclude(item => item.Section)
                        .ThenInclude(item => item.Course)
                .Include(item => item.Schedule)
                    .ThenInclude(item => item.Instructor)
                .SingleAsync(item => item.Id == host.AttendanceQrScenario!.SessionId, cancellationToken);

            return session.Schedule;
        });

        var legacyResponse = await host.Client.GetAsync($"/api/schedules/{expected.Id}");
        var uuidResponse = await host.Client.GetAsync($"/api/schedules/{expected.Uuid}");
        var legacyPayload = await legacyResponse.Content.ReadFromJsonAsync<ScheduleResponseDto>();
        var uuidPayload = await uuidResponse.Content.ReadFromJsonAsync<ScheduleResponseDto>();

        Assert.Equal(HttpStatusCode.OK, legacyResponse.StatusCode);
        Assert.Equal(HttpStatusCode.OK, uuidResponse.StatusCode);
        Assert.NotNull(legacyPayload);
        Assert.NotNull(uuidPayload);

        Assert.Equal(legacyPayload.Id, uuidPayload.Id);
        Assert.Equal(expected.Uuid, uuidPayload.Id);
        Assert.Equal(expected.Subject.Uuid, uuidPayload.Subject.Id);
        Assert.Equal(expected.Classroom.Uuid, uuidPayload.Classroom.Id);
        Assert.Equal(expected.Section.Uuid, uuidPayload.Section.Id);
        Assert.Equal(expected.Section.Course!.Uuid, uuidPayload.Section.CourseId);
        Assert.Equal(expected.Instructor.Uuid, uuidPayload.Instructor.Id);
    }

    [Fact]
    public async Task GetSessionByUuid_ReturnsSameSessionAsLegacyIntRoute_WithUuidSidecars()
    {
        await using var host = await ApiIntegrationHost.CreateAttendanceQrAsync(AttendanceQrSeedData.ValidAttendanceCreate);
        host.AuthenticateAs(userId: host.AttendanceQrScenario!.InstructorUserId, username: "integration-instructor", role: "Instructor");

        var expected = await host.ExecuteDbContextAsync(async (dbContext, cancellationToken) =>
            await dbContext.Sessions
                .AsNoTracking()
                .Include(item => item.Schedule)
                .Include(item => item.ActualRoom)
                .SingleAsync(item => item.Id == host.AttendanceQrScenario!.SessionId, cancellationToken));

        var legacyResponse = await host.Client.GetAsync($"/api/sessions/{expected.Id}");
        var uuidResponse = await host.Client.GetAsync($"/api/sessions/{expected.Uuid}");
        var legacyPayload = await legacyResponse.Content.ReadFromJsonAsync<SessionResponseDto>();
        var uuidPayload = await uuidResponse.Content.ReadFromJsonAsync<SessionResponseDto>();

        Assert.Equal(HttpStatusCode.OK, legacyResponse.StatusCode);
        Assert.Equal(HttpStatusCode.OK, uuidResponse.StatusCode);
        Assert.NotNull(legacyPayload);
        Assert.NotNull(uuidPayload);
        Assert.Equal(legacyPayload.Id, uuidPayload.Id);
        Assert.Equal(expected.Uuid, uuidPayload.Id);
        Assert.Equal(expected.Schedule!.Uuid, uuidPayload.ScheduleId);
        Assert.Equal(expected.ActualRoom?.Uuid, uuidPayload.ActualRoomId);
    }

    [Fact]
    public async Task GetStudentEnrollmentsByStudentUuid_ReturnsSameEnrollmentSetAsLegacyIntRoute_WithUuidSidecars()
    {
        await using var host = await ApiIntegrationHost.CreateAttendanceQrAsync(AttendanceQrSeedData.ValidAttendanceCreate);
        host.AuthenticateAs(userId: host.AttendanceQrScenario!.InstructorUserId, username: "integration-instructor", role: "Instructor");

        var expected = await host.ExecuteDbContextAsync(async (dbContext, cancellationToken) =>
            await dbContext.StudentEnrollments
                .AsNoTracking()
                .Include(item => item.Student)
                .Include(item => item.Section)
                .Include(item => item.Subject)
                .SingleAsync(item => item.StudentId == host.AttendanceQrScenario!.StudentId, cancellationToken));

        var legacyResponse = await host.Client.GetAsync($"/api/studentenrollment/student/{expected.StudentId}");
        var uuidResponse = await host.Client.GetAsync($"/api/studentenrollment/student/{expected.Student!.Uuid}");
        var legacyPayload = await legacyResponse.Content.ReadFromJsonAsync<StudentSectionsResponseDto>();
        var uuidPayload = await uuidResponse.Content.ReadFromJsonAsync<StudentSectionsResponseDto>();

        Assert.Equal(HttpStatusCode.OK, legacyResponse.StatusCode);
        Assert.Equal(HttpStatusCode.OK, uuidResponse.StatusCode);
        Assert.NotNull(legacyPayload);
        Assert.NotNull(uuidPayload);
        Assert.Single(legacyPayload.Enrollments);
        Assert.Single(uuidPayload.Enrollments);

        var legacyEnrollment = legacyPayload.Enrollments[0];
        var uuidEnrollment = uuidPayload.Enrollments[0];

        Assert.Equal(expected.Student.Uuid, uuidPayload.StudentId);
        Assert.Equal(expected.Uuid, uuidEnrollment.EnrollmentId);
        Assert.Equal(expected.Section!.Uuid, uuidEnrollment.SectionId);
        Assert.Equal(expected.Subject!.Uuid, uuidEnrollment.SubjectId);
    }

    [Fact]
    public async Task GetAttendanceByUuid_ReturnsSameAttendanceAsLegacyIntRoute_WithUuidSidecars()
    {
        await using var host = await ApiIntegrationHost.CreateAttendanceQrAsync(AttendanceQrSeedData.ExistingAttendanceDuplicate);
        host.AuthenticateAs(userId: host.AttendanceQrScenario!.InstructorUserId, username: "integration-instructor", role: "Instructor");

        var expected = await host.ExecuteDbContextAsync(async (dbContext, cancellationToken) =>
            await dbContext.AttendanceRecords
                .AsNoTracking()
                .Include(item => item.Student)
                .Include(item => item.Session)
                    .ThenInclude(item => item.Schedule)
                .SingleAsync(item => item.Id == host.AttendanceQrScenario!.ExistingAttendanceRecordId, cancellationToken));

        var legacyResponse = await host.Client.GetAsync($"/api/attendance/{expected.Id}");
        var uuidResponse = await host.Client.GetAsync($"/api/attendance/{expected.Uuid}");
        var legacyPayload = await legacyResponse.Content.ReadFromJsonAsync<AttendanceRecordResponseDto>();
        var uuidPayload = await uuidResponse.Content.ReadFromJsonAsync<AttendanceRecordResponseDto>();

        Assert.Equal(HttpStatusCode.OK, legacyResponse.StatusCode);
        Assert.Equal(HttpStatusCode.OK, uuidResponse.StatusCode);
        Assert.NotNull(legacyPayload);
        Assert.NotNull(uuidPayload);

        Assert.Equal(legacyPayload.Id, uuidPayload.Id);
        Assert.Equal(expected.Uuid, uuidPayload.Id);
        Assert.Equal(expected.Student!.Uuid, uuidPayload.StudentId);
        Assert.Equal(expected.Session!.Uuid, uuidPayload.SessionId);
        Assert.Equal(expected.Session.Schedule.Uuid, uuidPayload.ScheduleId);
    }

    [Fact]
    public async Task GetSessionAttendanceByUuid_ReturnsSameOverviewAsLegacyIntRoute_WithSessionAndAttendanceUuids()
    {
        await using var host = await ApiIntegrationHost.CreateAttendanceQrAsync(AttendanceQrSeedData.ExistingAttendanceDuplicate);
        host.AuthenticateAs(userId: host.AttendanceQrScenario!.InstructorUserId, username: "integration-instructor", role: "Instructor");

        var expectedSession = await host.ExecuteDbContextAsync(async (dbContext, cancellationToken) =>
            await dbContext.Sessions
                .AsNoTracking()
                .Include(item => item.Schedule)
                .SingleAsync(item => item.Id == host.AttendanceQrScenario!.SessionId, cancellationToken));

        var legacyResponse = await host.Client.GetAsync($"/api/attendance/session/{expectedSession.Id}");
        var uuidResponse = await host.Client.GetAsync($"/api/attendance/session/{expectedSession.Uuid}");
        var legacyPayload = await legacyResponse.Content.ReadFromJsonAsync<SessionAttendanceDto>();
        var uuidPayload = await uuidResponse.Content.ReadFromJsonAsync<SessionAttendanceDto>();

        Assert.Equal(HttpStatusCode.OK, legacyResponse.StatusCode);
        Assert.Equal(HttpStatusCode.OK, uuidResponse.StatusCode);
        Assert.NotNull(legacyPayload);
        Assert.NotNull(uuidPayload);
        Assert.NotEmpty(legacyPayload.AttendanceRecords);
        Assert.NotEmpty(uuidPayload.AttendanceRecords);

        Assert.Equal(legacyPayload.SessionId, uuidPayload.SessionId);
        Assert.Equal(expectedSession.Uuid, uuidPayload.SessionId);
        Assert.Equal(expectedSession.Schedule.Uuid, uuidPayload.ScheduleId);
        Assert.Equal(legacyPayload.AttendanceRecords[0].AttendanceRecordId, uuidPayload.AttendanceRecords[0].AttendanceRecordId);
        Assert.NotEqual(Guid.Empty, uuidPayload.AttendanceRecords[0].AttendanceRecordId ?? Guid.Empty);
        Assert.NotEqual(Guid.Empty, uuidPayload.AttendanceRecords[0].StudentId);
    }

    [Fact]
    public async Task GetQrCodeByUuid_ReturnsSameQrCodeAsLegacyIntRoute_WithUuidSidecars()
    {
        await using var host = await ApiIntegrationHost.CreateAttendanceQrAsync(AttendanceQrSeedData.ValidQrScan);
        host.AuthenticateAs(userId: host.AttendanceQrScenario!.InstructorUserId, username: "integration-instructor", role: "Instructor");

        var expected = await host.ExecuteDbContextAsync(async (dbContext, cancellationToken) =>
            await dbContext.QrCodes
                .AsNoTracking()
                .Include(item => item.Session)
                    .ThenInclude(item => item.Schedule)
                .SingleAsync(item => item.Id == host.AttendanceQrScenario!.QrCodeId, cancellationToken));

        var legacyResponse = await host.Client.GetAsync($"/api/qrcode/{expected.Id}");
        var uuidResponse = await host.Client.GetAsync($"/api/qrcode/{expected.Uuid}");
        var legacyPayload = await legacyResponse.Content.ReadFromJsonAsync<QrCodeResponseDto>();
        var uuidPayload = await uuidResponse.Content.ReadFromJsonAsync<QrCodeResponseDto>();

        Assert.Equal(HttpStatusCode.OK, legacyResponse.StatusCode);
        Assert.Equal(HttpStatusCode.OK, uuidResponse.StatusCode);
        Assert.NotNull(legacyPayload);
        Assert.NotNull(uuidPayload);

        Assert.Equal(legacyPayload.Id, uuidPayload.Id);
        Assert.Equal(expected.Uuid, uuidPayload.Id);
        Assert.Equal(expected.Session!.Uuid, uuidPayload.SessionId);
        Assert.Equal(expected.Session.Schedule?.Uuid, uuidPayload.ScheduleId);
        Assert.Equal(expected.QrHash, uuidPayload.QrHash);
    }
}
