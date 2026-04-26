using attendance_monitoring.Classes;
using attendance_monitoring.Constants;
using attendance_monitoring.Extensions;
using attendance_monitoring.IRepository;
using attendance_monitoring.Options;
using attendance_monitoring.Services;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Microsoft.EntityFrameworkCore;

namespace attendance.testproject.Services_Testing;

public class AutomaticSessionEndServiceTest
{
    [Fact]
    public void CalculateAutoEndBoundary_UsesSessionDateScheduleTimeOutAndConfiguredGrace()
    {
        var service = CreateService();
        var session = CreateSession(
            sessionDate: new DateTime(2026, 4, 26),
            timeIn: new TimeOnly(10, 0),
            timeOut: new TimeOnly(11, 30));

        var boundary = service.CalculateAutoEndBoundary(session);

        Assert.Equal(new DateTime(2026, 4, 26, 11, 45, 0), boundary);
    }

    [Fact]
    public async Task AutoEndIfExpiredAsync_LeavesActiveSessionWithinGraceActive()
    {
        var repository = new Mock<ISessionRepository>();
        var service = CreateService(
            repository,
            localNow: new DateTime(2026, 4, 26, 11, 44, 0));
        var session = CreateSession(
            sessionDate: new DateTime(2026, 4, 26),
            timeIn: new TimeOnly(10, 0),
            timeOut: new TimeOnly(11, 30));

        var result = await service.AutoEndIfExpiredAsync(session);

        Assert.Equal(SessionStatusConstants.Active, result.Status);
        Assert.Null(result.ActualEndTime);
        repository.Verify(repo => repo.UpdateSessionAsync(It.IsAny<Session>()), Times.Never);
        repository.Verify(repo => repo.SaveChangesAsync(), Times.Never);
    }

    [Fact]
    public async Task AutoEndIfExpiredAsync_EndsExpiredActiveSessionAtScheduledBoundary()
    {
        var repository = new Mock<ISessionRepository>();
        var service = CreateService(
            repository,
            localNow: new DateTime(2026, 4, 26, 11, 52, 0));
        var session = CreateSession(
            sessionDate: new DateTime(2026, 4, 26),
            timeIn: new TimeOnly(10, 0),
            timeOut: new TimeOnly(11, 30));

        repository
            .Setup(repo => repo.UpdateSessionAsync(It.IsAny<Session>()))
            .ReturnsAsync((Session updated) => updated);
        repository
            .Setup(repo => repo.SaveChangesAsync())
            .ReturnsAsync(1);

        var result = await service.AutoEndIfExpiredAsync(session);

        Assert.Equal(SessionStatusConstants.Ended, result.Status);
        Assert.Equal(new DateTime(2026, 4, 26, 11, 45, 0), result.ActualEndTime);
        Assert.Null(result.EndedBy);
        Assert.Contains("Auto-ended by system", result.Description);
        repository.Verify(repo => repo.UpdateSessionAsync(
            It.Is<Session>(updated =>
                updated.Status == SessionStatusConstants.Ended &&
                updated.ActualEndTime == new DateTime(2026, 4, 26, 11, 45, 0) &&
                updated.EndedBy == null)), Times.Once);
        repository.Verify(repo => repo.SaveChangesAsync(), Times.Once);
    }

    [Fact]
    public async Task AutoEndIfExpiredAsync_DoesNotUpdateAlreadyEndedSession()
    {
        var repository = new Mock<ISessionRepository>();
        var service = CreateService(
            repository,
            localNow: new DateTime(2026, 4, 26, 11, 52, 0));
        var session = CreateSession(
            sessionDate: new DateTime(2026, 4, 26),
            timeIn: new TimeOnly(10, 0),
            timeOut: new TimeOnly(11, 30),
            status: SessionStatusConstants.Ended);

        var result = await service.AutoEndIfExpiredAsync(session);

        Assert.Equal(SessionStatusConstants.Ended, result.Status);
        repository.Verify(repo => repo.UpdateSessionAsync(It.IsAny<Session>()), Times.Never);
        repository.Verify(repo => repo.SaveChangesAsync(), Times.Never);
    }

    [Fact]
    public async Task AutoEndIfExpiredAsync_ToleratesConcurrencyRace()
    {
        var repository = new Mock<ISessionRepository>();
        var service = CreateService(
            repository,
            localNow: new DateTime(2026, 4, 26, 11, 52, 0));
        var session = CreateSession(
            sessionDate: new DateTime(2026, 4, 26),
            timeIn: new TimeOnly(10, 0),
            timeOut: new TimeOnly(11, 30));

        repository
            .Setup(repo => repo.UpdateSessionAsync(It.IsAny<Session>()))
            .ThrowsAsync(new DbUpdateConcurrencyException("manual end won"));

        var result = await service.AutoEndIfExpiredAsync(session);

        Assert.Equal(SessionStatusConstants.Ended, result.Status);
        Assert.Equal(new DateTime(2026, 4, 26, 11, 45, 0), result.ActualEndTime);
        repository.Verify(repo => repo.UpdateSessionAsync(It.IsAny<Session>()), Times.Once);
        repository.Verify(repo => repo.SaveChangesAsync(), Times.Never);
    }

    private static AutomaticSessionEndService CreateService(
        Mock<ISessionRepository>? repository = null,
        DateTime? localNow = null,
        SessionAutoEndOptions? options = null)
    {
        var effectiveLocalNow = localNow ?? new DateTime(2026, 4, 26, 12, 0, 0);
        var timeZone = TimeZoneInfo.FindSystemTimeZoneById("Asia/Manila");
        var utcNow = TimeZoneInfo.ConvertTimeToUtc(effectiveLocalNow, timeZone);
        var clock = new ConfiguredTimeZoneProvider(
            new TimeZoneSettings { TimeZoneId = "Asia/Manila" },
            new FixedTimeProvider(utcNow));

        return new AutomaticSessionEndService(
            repository?.Object ?? new Mock<ISessionRepository>().Object,
            Options.Create(options ?? new SessionAutoEndOptions()),
            clock,
            NullLogger<AutomaticSessionEndService>.Instance);
    }

    private static Session CreateSession(
        DateTime sessionDate,
        TimeOnly timeIn,
        TimeOnly timeOut,
        string status = SessionStatusConstants.Active)
    {
        return new Session
        {
            Id = Guid.NewGuid(),
            ScheduleId = Guid.NewGuid(),
            Status = status,
            SessionDate = sessionDate.Date,
            ActualStartTime = sessionDate.Date.Add(timeIn.ToTimeSpan()),
            RowVersion = [1, 2, 3, 4],
            Schedule = new Schedules
            {
                Id = Guid.NewGuid(),
                DayOfWeek = sessionDate.DayOfWeek.ToString(),
                TimeIn = timeIn,
                TimeOut = timeOut,
                SubjectId = Guid.NewGuid(),
                ClassroomId = Guid.NewGuid(),
                SectionId = Guid.NewGuid(),
                InstructorId = Guid.NewGuid()
            }
        };
    }

    private sealed class FixedTimeProvider(DateTime utcNow) : TimeProvider
    {
        public override DateTimeOffset GetUtcNow() => new DateTimeOffset(utcNow, TimeSpan.Zero);
    }
}
