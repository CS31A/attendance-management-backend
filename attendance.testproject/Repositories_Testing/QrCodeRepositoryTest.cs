using attendance_monitoring.Classes;
using attendance_monitoring.Data;
using attendance_monitoring.Repositories;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;

namespace attendance.testproject.Repositories_Testing;

public sealed class QrCodeRepositoryTest : IDisposable
{
    private readonly ApplicationDbContext _context;
    private readonly Guid _qrCodeUuid;

    public QrCodeRepositoryTest()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        _context = new ApplicationDbContext(options);

        var subject = new Subject
        {
            Id = Guid.NewGuid(),
            Name = "Software Engineering",
            Code = "SE101",
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow,
        };

        var section = new Section
        {
            Id = Guid.NewGuid(),
            Name = "BSCS-3A",
            CourseId = Guid.NewGuid(),
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow,
        };

        var instructor = new Instructor
        {
            Id = Guid.NewGuid(),
            Firstname = "Ada",
            Lastname = "Lovelace",
            UserId = "instructor-1",
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow,
        };

        var classroom = new Classroom
        {
            Id = Guid.NewGuid(),
            Name = "Room 201",
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow,
        };

        var schedule = new Schedules
        {
            Id = Guid.NewGuid(),
            DayOfWeek = "Monday",
            TimeIn = new TimeOnly(8, 0),
            TimeOut = new TimeOnly(10, 0),
            SubjectId = subject.Id,
            Subject = subject,
            SectionId = section.Id,
            Section = section,
            ClassroomId = classroom.Id,
            Classroom = classroom,
            InstructorId = instructor.Id,
            Instructor = instructor,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow,
        };

        var session = new Session
        {
            Id = Guid.NewGuid(),
            ScheduleId = schedule.Id,
            Schedule = schedule,
            SessionDate = DateTime.UtcNow.Date,
            Status = "active",
            ActualRoomId = classroom.Id,
            ActualRoom = classroom,
            RowVersion = new byte[] { 1, 2, 3, 4, 5, 6, 7, 8 },
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow,
        };

        var qrCode = new QrCode
        {
            Id = Guid.NewGuid(),
            SessionId = session.Id,
            Session = session,
            QrHash = "qr-hash-1",
            GeneratedAt = DateTime.UtcNow,
            ExpiresAt = DateTime.UtcNow.AddMinutes(15),
            IsActive = true,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow,
        };

        _context.Subjects.Add(subject);
        _context.Sections.Add(section);
        _context.Instructors.Add(instructor);
        _context.Classrooms.Add(classroom);
        _context.Schedules.Add(schedule);
        _context.Sessions.Add(session);
        _context.QrCodes.Add(qrCode);
        _context.SaveChanges();

        _qrCodeUuid = _context.QrCodes.AsNoTracking().Single(qr => qr.Id == qrCode.Id).Id;
        _context.ChangeTracker.Clear();
    }

    [Fact]
    public async Task QrCodeRepository_GetQrCodeByUuidAsync_ReturnsReadOnlyAndTrackedQrCode()
    {
        var repository = new QrCodeRepository(_context, NullLogger<QrCodeRepository>.Instance);

        var readOnlyQrCode = await repository.GetQrCodeByUuidAsync(_qrCodeUuid);
        var trackedQrCode = await repository.GetQrCodeByUuidTrackedAsync(_qrCodeUuid);

        Assert.NotNull(readOnlyQrCode);
        Assert.Equal("qr-hash-1", readOnlyQrCode.QrHash);
        Assert.NotNull(readOnlyQrCode.Session);
        Assert.NotNull(readOnlyQrCode.Session.Schedule);
        Assert.NotNull(readOnlyQrCode.Session.Schedule.Subject);
        Assert.NotNull(readOnlyQrCode.Session.Schedule.Section);
        Assert.NotNull(readOnlyQrCode.Session.Schedule.Instructor);
        Assert.NotNull(readOnlyQrCode.Session.ActualRoom);
        Assert.NotNull(trackedQrCode);
        Assert.Equal(EntityState.Unchanged, _context.Entry(trackedQrCode).State);
    }

    [Fact]
    public async Task QrCodeRepository_GetQrCodeByUuidAsync_ReturnsNull_WhenNotFound()
    {
        var repository = new QrCodeRepository(_context, NullLogger<QrCodeRepository>.Instance);
        var missingUuid = Guid.NewGuid();

        var readOnlyQrCode = await repository.GetQrCodeByUuidAsync(missingUuid);
        var trackedQrCode = await repository.GetQrCodeByUuidTrackedAsync(missingUuid);

        Assert.Null(readOnlyQrCode);
        Assert.Null(trackedQrCode);
    }

    public void Dispose()
    {
        _context.Database.EnsureDeleted();
        _context.Dispose();
    }
}
