using attendance_monitoring.Classes;
using attendance_monitoring.Constants;
using attendance_monitoring.Data;
using attendance_monitoring.Repositories;
using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace attendance.testproject.Repositories_Testing;

public class SessionRepositoryTest : IDisposable
{
    private readonly ApplicationDbContext _context;
    private readonly SessionRepository _repository;

    public SessionRepositoryTest()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        _context = new ApplicationDbContext(options);
        _repository = new SessionRepository(_context);
    }

    [Fact]
    public async Task GetSectionSessionReportRowsAsync_ReturnsAggregatedCountsIncludingZeroAttendanceSessions()
    {
        // Arrange
        await SeedReportDataAsync();
        var sectionId = await _context.Sections.AsNoTracking().Select(s => s.Id).FirstAsync();
        var newerSessionId = await _context.Sessions.AsNoTracking()
            .OrderByDescending(s => s.SessionDate)
            .Select(s => s.Id)
            .FirstAsync();
        var olderSessionId = await _context.Sessions.AsNoTracking()
            .OrderBy(s => s.SessionDate)
            .Select(s => s.Id)
            .FirstAsync();

        // Act
        var result = await _repository.GetSectionSessionReportRowsAsync(sectionId, new DateTime(2026, 4, 1), new DateTime(2026, 4, 30));

        // Assert
        Assert.Equal([newerSessionId, olderSessionId], result.Select(row => row.SessionId).ToArray());
        Assert.Equal(2, result[0].PresentCount);
        Assert.Equal(1, result[0].LateCount);
        Assert.Equal(1, result[0].AbsentCount);
        Assert.Equal(1, result[0].ExcusedCount);
        Assert.Equal(5, result[0].TotalRecords);
        Assert.Equal(0, result[1].TotalRecords);
        Assert.Equal("Math", result[0].SubjectName);
        Assert.Equal("BSCS 3A", result[0].SectionName);
        Assert.Equal("Thursday", result[0].DayOfWeek);
    }

    [Fact]
    public async Task GetInstructorSessionReportRowsAsync_AppliesDateFilterAndIncludesSectionNames()
    {
        // Arrange
        await SeedReportDataAsync();
        var instructorId = await _context.Instructors.AsNoTracking().Select(i => i.Id).FirstAsync();
        var newerSessionId = await _context.Sessions.AsNoTracking()
            .OrderByDescending(s => s.SessionDate)
            .Select(s => s.Id)
            .FirstAsync();

        // Act
        var result = await _repository.GetInstructorSessionReportRowsAsync(instructorId, new DateTime(2026, 4, 10), new DateTime(2026, 4, 30));

        // Assert
        var row = Assert.Single(result);
        Assert.Equal(newerSessionId, row.SessionId);
        Assert.Equal("Math", row.SubjectName);
        Assert.Equal("BSCS 3A", row.SectionName);
        Assert.Equal(5, row.TotalRecords);
    }

    [Fact]
    public async Task GetSectionSessionReportRowsAsync_PreservesSliceABridgeWhenUuidColumnsExist()
    {
        // Arrange
        await SeedReportDataAsync();

        var schedule = await _context.Schedules
            .AsNoTracking()
            .Include(row => row.Subject)
            .Include(row => row.Classroom)
            .Include(row => row.Section)
                .ThenInclude(section => section.Course)
            .SingleAsync();

        // Act
        var row = Assert.Single(await _repository.GetSectionSessionReportRowsAsync(
            schedule.SectionId,
            new DateTime(2026, 4, 10),
            new DateTime(2026, 4, 30)));

        // Assert
        Assert.NotEqual(Guid.Empty, schedule.SubjectId);
        Assert.NotEqual(Guid.Empty, schedule.SectionId);
        Assert.NotEqual(Guid.Empty, schedule.ClassroomId);
        Assert.NotEqual(Guid.Empty, schedule.Id);
        Assert.NotEqual(Guid.Empty, schedule.Subject.Id);
        Assert.NotEqual(Guid.Empty, schedule.Section.Id);
        Assert.NotEqual(Guid.Empty, schedule.Classroom.Id);
        Assert.NotEqual(Guid.Empty, schedule.Section.Course.Id);
        Assert.Equal(schedule.Subject.Name, row.SubjectName);
        Assert.Equal(schedule.Section.Name, row.SectionName);
        Assert.Equal(schedule.DayOfWeek, row.DayOfWeek);
    }

    [Fact]
    public async Task UpdateSessionAsync_ThrowsValidationException_WhenRowVersionMissing()
    {
        // Arrange
        var session = new Session
        {
            Id = Guid.NewGuid(),
            ScheduleId = Guid.NewGuid(),
            SessionDate = new DateTime(2026, 4, 10),
            Status = "NotStarted",
            RowVersion = [9, 9, 9, 9]
        };

        _context.Sessions.Add(session);
        await _context.SaveChangesAsync();

        var update = new Session
        {
            Id = session.Id,
            ScheduleId = session.ScheduleId,
            SessionDate = session.SessionDate,
            Status = "Active",
            RowVersion = Array.Empty<byte>()
        };

        // Act & Assert
        await Assert.ThrowsAsync<ValidationException>(() => _repository.UpdateSessionAsync(update));
    }

    [Fact]
    public async Task GetSectionSessionReportRowsAsync_PreservesSliceBUuidSidecarsWhileLegacySessionIdsStayAuthoritative()
    {
        // Arrange
        await SeedReportDataAsync();
        var sectionId = await _context.Sections.AsNoTracking().Select(s => s.Id).FirstAsync();

        // Act
        var newerSessionId = await _context.Sessions.AsNoTracking()
            .OrderByDescending(s => s.SessionDate)
            .Select(s => s.Id)
            .FirstAsync();

        var newerSession = await _context.Sessions
            .AsNoTracking()
            .Include(session => session.Schedule)
                .ThenInclude(schedule => schedule.Subject)
            .Include(session => session.Schedule)
                .ThenInclude(schedule => schedule.Section)
            .Include(session => session.Schedule)
                .ThenInclude(schedule => schedule.Classroom)
            .Include(session => session.AttendanceRecords)
            .Include(session => session.QrCodes)
            .SingleAsync(session => session.Id == newerSessionId);

        var reportRow = Assert.Single(await _repository.GetSectionSessionReportRowsAsync(
            sectionId,
            new DateTime(2026, 4, 10),
            new DateTime(2026, 4, 30)));

        // Assert
        Assert.NotEqual(Guid.Empty, newerSession.Id);
        Assert.NotEqual(Guid.Empty, newerSession.Schedule.Id);
        Assert.NotEqual(Guid.Empty, newerSession.Schedule.Subject.Id);
        Assert.NotEqual(Guid.Empty, newerSession.Schedule.Section.Id);
        Assert.NotEqual(Guid.Empty, newerSession.Schedule.Classroom.Id);
        Assert.Equal(newerSession.Schedule.Subject.Name, reportRow.SubjectName);
        Assert.Equal(newerSession.Schedule.Section.Name, reportRow.SectionName);
        Assert.Equal(newerSession.Schedule.DayOfWeek, reportRow.DayOfWeek);
        Assert.All(newerSession.AttendanceRecords, record => Assert.NotEqual(Guid.Empty, record.Id));

        var qrCode = Assert.Single(newerSession.QrCodes);
        Assert.NotEqual(Guid.Empty, qrCode.Id);
        Assert.Equal(newerSession.Id, reportRow.SessionId);
    }

    [Fact]
    public async Task SessionRepository_GetSessionByUuidAsync_ReturnsReadOnlyAndTrackedSession()
    {
        // Arrange
        await SeedReportDataAsync();
        _context.ChangeTracker.Clear();

        var sessionUuid = await _context.Sessions
            .AsNoTracking()
            .OrderByDescending(s => s.SessionDate)
            .Select(session => session.Id)
            .FirstAsync();

        // Act
        var readOnlySession = await _repository.GetSessionByUuidAsync(sessionUuid);
        var trackedSession = await _repository.GetSessionByUuidTrackedAsync(sessionUuid);

        // Assert
        Assert.NotNull(readOnlySession);
        Assert.Equal(sessionUuid, readOnlySession.Id);
        Assert.NotNull(readOnlySession.Schedule);
        Assert.NotNull(readOnlySession.Schedule.Subject);
        Assert.NotNull(readOnlySession.Schedule.Section);
        Assert.NotNull(readOnlySession.Schedule.Classroom);
        Assert.NotNull(readOnlySession.Schedule.Instructor);
        Assert.NotNull(trackedSession);
        Assert.Equal(EntityState.Unchanged, _context.Entry(trackedSession).State);
    }

    [Fact]
    public async Task SessionRepository_GetSessionByUuidAsync_ReturnsNull_WhenNotFound()
    {
        // Arrange
        await SeedReportDataAsync();
        var missingUuid = Guid.NewGuid();

        // Act
        var readOnlySession = await _repository.GetSessionByUuidAsync(missingUuid);
        var trackedSession = await _repository.GetSessionByUuidTrackedAsync(missingUuid);

        // Assert
        Assert.Null(readOnlySession);
        Assert.Null(trackedSession);
    }

    [Fact]
    public async Task GetSessionsByStatusesAsync_ReturnsOnlyMatchingStatuses()
    {
        // Arrange
        await SeedReportDataAsync();
        var scheduleId = await _context.Schedules.AsNoTracking().Select(schedule => schedule.Id).SingleAsync();
        var activeSessionId = Guid.NewGuid();
        var endedSessionId = Guid.NewGuid();
        var cancelledSessionId = Guid.NewGuid();

        _context.Sessions.AddRange(
            new Session
            {
                Id = activeSessionId,
                ScheduleId = scheduleId,
                SessionDate = new DateTime(2026, 4, 24),
                Status = SessionStatusConstants.Active,
                RowVersion = [3],
            },
            new Session
            {
                Id = endedSessionId,
                ScheduleId = scheduleId,
                SessionDate = new DateTime(2026, 4, 23),
                Status = SessionStatusConstants.Ended,
                RowVersion = [4],
            },
            new Session
            {
                Id = cancelledSessionId,
                ScheduleId = scheduleId,
                SessionDate = new DateTime(2026, 4, 25),
                Status = SessionStatusConstants.Cancelled,
                RowVersion = [5],
            });
        await _context.SaveChangesAsync();
        _context.ChangeTracker.Clear();

        // Act
        var result = (await _repository.GetSessionsByStatusesAsync([
            SessionStatusConstants.Active,
            SessionStatusConstants.Ended
        ])).ToList();

        // Assert
        Assert.Equal(2, result.Count);
        Assert.Contains(result, session => session.Id == activeSessionId);
        Assert.Contains(result, session => session.Id == endedSessionId);
        Assert.DoesNotContain(result, session => session.Id == cancelledSessionId);
        var expectedStatuses = new[] { SessionStatusConstants.Active, SessionStatusConstants.Ended };
        Assert.All(result, session => Assert.Contains(session.Status, expectedStatuses));
    }

    private async Task SeedReportDataAsync()
    {
        var user = new IdentityUser
        {
            Id = "inst-1",
            UserName = "inst1@example.com",
            NormalizedUserName = "INST1@EXAMPLE.COM",
            Email = "inst1@example.com",
            NormalizedEmail = "INST1@EXAMPLE.COM",
        };

        var course = new Course { Id = Guid.NewGuid(), Name = "BSCS" };
        var section = new Section { Id = Guid.NewGuid(), Name = "BSCS 3A", CourseId = Guid.NewGuid(), Course = course };
        var subject = new Subject { Id = Guid.NewGuid(), Name = "Math", Code = "MATH1" };
        var classroom = new Classroom { Id = Guid.NewGuid(), Name = "Room 101" };
        var instructor = new Instructor
        {
            Id = Guid.NewGuid(),
            Firstname = "Ada",
            Lastname = "Lovelace",
            UserId = user.Id,
            User = user,
        };

        var schedule = new Schedules
        {
            Id = Guid.NewGuid(),
            TimeIn = new TimeOnly(8, 0),
            TimeOut = new TimeOnly(10, 0),
            DayOfWeek = "Thursday",
            SubjectId = subject.Id,
            Subject = subject,
            ClassroomId = classroom.Id,
            Classroom = classroom,
            SectionId = section.Id,
            Section = section,
            InstructorId = instructor.Id,
            Instructor = instructor,
        };

        var olderSession = new Session
        {
            Id = Guid.NewGuid(),
            ScheduleId = schedule.Id,
            Schedule = schedule,
            SessionDate = new DateTime(2026, 4, 3),
            Status = "Ended",
            RowVersion = [1],
        };

        var newerSession = new Session
        {
            Id = Guid.NewGuid(),
            ScheduleId = schedule.Id,
            Schedule = schedule,
            SessionDate = new DateTime(2026, 4, 17),
            Status = "Ended",
            RowVersion = [2],
        };

        var qrCode = new QrCode
        {
            Id = Guid.NewGuid(),
            SessionId = newerSession.Id,
            Session = newerSession,
            QrHash = "slice-b-report-qr",
            GeneratedAt = newerSession.SessionDate.AddHours(7).AddMinutes(55),
            ExpiresAt = newerSession.SessionDate.AddHours(8).AddMinutes(30),
            IsActive = true
        };

        _context.Users.Add(user);
        _context.Courses.Add(course);
        _context.Sections.Add(section);
        _context.Subjects.Add(subject);
        _context.Classrooms.Add(classroom);
        _context.Instructors.Add(instructor);
        _context.Schedules.Add(schedule);
        _context.Sessions.AddRange(olderSession, newerSession);
        _context.QrCodes.Add(qrCode);
        _context.AttendanceRecords.AddRange(
            new AttendanceRecord { Id = Guid.NewGuid(), StudentId = Guid.NewGuid(), SessionId = newerSession.Id, Session = newerSession, QrCodeId = qrCode.Id, QrCode = qrCode, CheckInTime = newerSession.SessionDate.AddHours(8), Status = "Present" },
            new AttendanceRecord { Id = Guid.NewGuid(), StudentId = Guid.NewGuid(), SessionId = newerSession.Id, Session = newerSession, CheckInTime = newerSession.SessionDate.AddHours(8).AddMinutes(2), Status = "Present" },
            new AttendanceRecord { Id = Guid.NewGuid(), StudentId = Guid.NewGuid(), SessionId = newerSession.Id, Session = newerSession, CheckInTime = newerSession.SessionDate.AddHours(8).AddMinutes(5), Status = "Late" },
            new AttendanceRecord { Id = Guid.NewGuid(), StudentId = Guid.NewGuid(), SessionId = newerSession.Id, Session = newerSession, CheckInTime = newerSession.SessionDate.AddHours(8).AddMinutes(7), Status = "Absent" },
            new AttendanceRecord { Id = Guid.NewGuid(), StudentId = Guid.NewGuid(), SessionId = newerSession.Id, Session = newerSession, CheckInTime = newerSession.SessionDate.AddHours(8).AddMinutes(9), Status = "Excused" });

        await _context.SaveChangesAsync();
    }

    public void Dispose()
    {
        _context.Dispose();
    }
}
