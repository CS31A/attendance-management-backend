using attendance_monitoring.Classes;
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

        // Act
        var result = await _repository.GetSectionSessionReportRowsAsync(1, new DateTime(2026, 4, 1), new DateTime(2026, 4, 30));

        // Assert
        Assert.Equal([11, 10], result.Select(row => row.SessionId).ToArray());
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

        // Act
        var result = await _repository.GetInstructorSessionReportRowsAsync(1, new DateTime(2026, 4, 10), new DateTime(2026, 4, 30));

        // Assert
        var row = Assert.Single(result);
        Assert.Equal(11, row.SessionId);
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
        Assert.True(schedule.SubjectId > 0);
        Assert.True(schedule.SectionId > 0);
        Assert.True(schedule.ClassroomId > 0);
        Assert.NotEqual(Guid.Empty, schedule.Uuid);
        Assert.NotEqual(Guid.Empty, schedule.Subject.Uuid);
        Assert.NotEqual(Guid.Empty, schedule.Section.Uuid);
        Assert.NotEqual(Guid.Empty, schedule.Classroom.Uuid);
        Assert.NotEqual(Guid.Empty, schedule.Section.Course.Uuid);
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
            Id = 40,
            ScheduleId = 1,
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

        // Act
        var newerSession = await _context.Sessions
            .AsNoTracking()
            .Include(session => session.AttendanceRecords)
            .Include(session => session.QrCodes)
            .SingleAsync(session => session.Id == 11);

        var reportRow = Assert.Single(await _repository.GetSectionSessionReportRowsAsync(
            1,
            new DateTime(2026, 4, 10),
            new DateTime(2026, 4, 30)));

        // Assert
        Assert.NotEqual(Guid.Empty, newerSession.Uuid);
        Assert.All(newerSession.AttendanceRecords, record => Assert.NotEqual(Guid.Empty, record.Uuid));

        var qrCode = Assert.Single(newerSession.QrCodes);
        Assert.NotEqual(Guid.Empty, qrCode.Uuid);
        Assert.Equal(newerSession.Id, reportRow.SessionId);
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

        var course = new Course { Id = 1, Name = "BSCS" };
        var section = new Section { Id = 1, Name = "BSCS 3A", CourseId = 1, Course = course };
        var subject = new Subject { Id = 1, Name = "Math", Code = "MATH1" };
        var classroom = new Classroom { Id = 1, Name = "Room 101" };
        var instructor = new Instructor
        {
            Id = 1,
            Firstname = "Ada",
            Lastname = "Lovelace",
            UserId = user.Id,
            User = user,
        };

        var schedule = new Schedules
        {
            Id = 1,
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
            Id = 10,
            ScheduleId = schedule.Id,
            Schedule = schedule,
            SessionDate = new DateTime(2026, 4, 3),
            Status = "Ended",
            RowVersion = [1],
        };

        var newerSession = new Session
        {
            Id = 11,
            ScheduleId = schedule.Id,
            Schedule = schedule,
            SessionDate = new DateTime(2026, 4, 17),
            Status = "Ended",
            RowVersion = [2],
        };

        var qrCode = new QrCode
        {
            Id = 90,
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
            new AttendanceRecord { Id = 1, StudentId = 1, SessionId = newerSession.Id, Session = newerSession, QrCodeId = qrCode.Id, QrCode = qrCode, CheckInTime = newerSession.SessionDate.AddHours(8), Status = "Present" },
            new AttendanceRecord { Id = 2, StudentId = 2, SessionId = newerSession.Id, Session = newerSession, CheckInTime = newerSession.SessionDate.AddHours(8).AddMinutes(2), Status = "Present" },
            new AttendanceRecord { Id = 3, StudentId = 3, SessionId = newerSession.Id, Session = newerSession, CheckInTime = newerSession.SessionDate.AddHours(8).AddMinutes(5), Status = "Late" },
            new AttendanceRecord { Id = 4, StudentId = 4, SessionId = newerSession.Id, Session = newerSession, CheckInTime = newerSession.SessionDate.AddHours(8).AddMinutes(7), Status = "Absent" },
            new AttendanceRecord { Id = 5, StudentId = 5, SessionId = newerSession.Id, Session = newerSession, CheckInTime = newerSession.SessionDate.AddHours(8).AddMinutes(9), Status = "Excused" });

        await _context.SaveChangesAsync();
    }

    public void Dispose()
    {
        _context.Dispose();
    }
}
