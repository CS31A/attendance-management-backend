using System.Security.Claims;
using attendance_monitoring.Classes;
using attendance_monitoring.Constants;
using attendance_monitoring.Data;
using attendance_monitoring.Exceptions;
using attendance_monitoring.Extensions;
using attendance_monitoring.IRepository;
using attendance_monitoring.Models.DTO.Request;
using attendance_monitoring.Options;
using attendance_monitoring.Services;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace attendance.testproject.Services_Testing;

public class AttendanceServiceUuidTests
{
    private readonly Mock<IAttendanceRepository> _mockAttendanceRepository;
    private readonly Mock<IStudentRepository> _mockStudentRepository;
    private readonly Mock<IInstructorRepository> _mockInstructorRepository;
    private readonly Mock<ISessionRepository> _mockSessionRepository;
    private readonly Mock<IStudentEnrollmentRepository> _mockStudentEnrollmentRepository;
    private readonly Mock<ILogger<AttendanceService>> _mockLogger;
    private readonly Mock<UserManager<IdentityUser>> _mockUserManager;
    private readonly AttendanceService _attendanceService;

    public AttendanceServiceUuidTests()
    {
        _mockAttendanceRepository = new Mock<IAttendanceRepository>();
        _mockStudentRepository = new Mock<IStudentRepository>();
        _mockInstructorRepository = new Mock<IInstructorRepository>();
        _mockSessionRepository = new Mock<ISessionRepository>();
        _mockStudentEnrollmentRepository = new Mock<IStudentEnrollmentRepository>();
        _mockLogger = new Mock<ILogger<AttendanceService>>();

        var mockUserStore = new Mock<IUserStore<IdentityUser>>();
        _mockUserManager = new Mock<UserManager<IdentityUser>>(
            mockUserStore.Object,
            Options.Create(new IdentityOptions()),
            new Mock<IPasswordHasher<IdentityUser>>().Object,
            Array.Empty<IUserValidator<IdentityUser>>(),
            Array.Empty<IPasswordValidator<IdentityUser>>(),
            new Mock<ILookupNormalizer>().Object,
            new Mock<IdentityErrorDescriber>().Object,
            new Mock<IServiceProvider>().Object,
            new Mock<ILogger<UserManager<IdentityUser>>>().Object);

        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        var mockContext = new Mock<ApplicationDbContext>(options);
        var userContextService = new UserContextService(_mockUserManager.Object, mockContext.Object);
        var timeZoneProvider = new ConfiguredTimeZoneProvider(new TimeZoneSettings { TimeZoneId = TimeZoneInfo.Local.Id });

        _attendanceService = new AttendanceService(
            _mockAttendanceRepository.Object,
            _mockStudentRepository.Object,
            _mockInstructorRepository.Object,
            _mockSessionRepository.Object,
            _mockStudentEnrollmentRepository.Object,
            userContextService,
            _mockLogger.Object,
            timeZoneProvider);
    }

    [Fact]
    public async Task GetAttendanceByUuidAsync_ReturnsAttendance_WhenAuthorizedInstructor()
    {
        var user = CreateInstructorUser("instructor-user");
        var attendance = CreateAttendanceRecord(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid());

        _mockAttendanceRepository
            .Setup(repository => repository.GetAttendanceByUuidAsync(attendance.Id))
            .ReturnsAsync(attendance);
        _mockUserManager
            .Setup(manager => manager.FindByIdAsync("instructor-user"))
            .ReturnsAsync(new IdentityUser { Id = "instructor-user" });
        _mockInstructorRepository
            .Setup(repository => repository.GetInstructorByUserIdAsync("instructor-user"))
            .ReturnsAsync(new Instructor { Id = attendance.Session!.Schedule!.InstructorId });

        var result = await _attendanceService.GetAttendanceByUuidAsync(attendance.Id, user);

        Assert.Equal(attendance.Id, result!.Id);
        _mockAttendanceRepository.Verify(repository => repository.GetAttendanceByUuidAsync(attendance.Id), Times.Once);
    }

    [Fact]
    public async Task GetAttendanceByUuidAsync_ThrowsEntityNotFoundException_WhenMissing()
    {
        var attendanceUuid = Guid.NewGuid();

        _mockAttendanceRepository
            .Setup(repository => repository.GetAttendanceByUuidAsync(attendanceUuid))
            .ReturnsAsync((AttendanceRecord?)null);

        await Assert.ThrowsAsync<EntityNotFoundException<Guid>>(
            () => _attendanceService.GetAttendanceByUuidAsync(attendanceUuid, CreateInstructorUser("instructor-user")));
    }

    [Fact]
    public async Task CreateAttendanceAsync_WithUuidOnlyReferences_ResolvesLegacyIdsBeforePersisting()
    {
        var studentUuid = Guid.NewGuid();
        var sessionUuid = Guid.NewGuid();
        var request = new CreateAttendanceRequest
        {
            StudentId = studentUuid,
            SessionId = sessionUuid,
            Status = "Present"
        };

        AttendanceRecord? capturedRecord = null;
        var sectionId = Guid.NewGuid();
        var subjectId = Guid.NewGuid();
        var instructorId = Guid.NewGuid();

        _mockUserManager
            .Setup(manager => manager.FindByIdAsync("instructor-user"))
            .ReturnsAsync(new IdentityUser { Id = "instructor-user" });
        _mockStudentRepository
            .Setup(repository => repository.GetStudentByUuidAsync(studentUuid))
            .ReturnsAsync(new Student { Id = studentUuid, SectionId = sectionId });
        _mockSessionRepository
            .Setup(repository => repository.GetSessionByUuidAsync(sessionUuid))
            .ReturnsAsync(new Session
            {
                Id = sessionUuid,
                Schedule = new Schedules { InstructorId = instructorId, SectionId = sectionId, SubjectId = subjectId }
            });
        _mockStudentEnrollmentRepository
            .Setup(repository => repository.GetStudentEnrollmentsAsync(studentUuid))
            .ReturnsAsync([new StudentEnrollment { StudentId = studentUuid, SectionId = sectionId, SubjectId = subjectId }]);
        _mockAttendanceRepository
            .Setup(repository => repository.CreateAsync(It.IsAny<AttendanceRecord>()))
            .Callback<AttendanceRecord>(record =>
            {
                capturedRecord = record;
                record.Id = Guid.NewGuid();
            })
            .ReturnsAsync((AttendanceRecord record) => new AttendanceRecord { Id = record.Id });
        _mockAttendanceRepository
            .Setup(repository => repository.SaveChangesAsync())
            .ReturnsAsync(1);
        _mockAttendanceRepository
            .Setup(repository => repository.GetByIdAsync(It.IsAny<Guid>()))
            .ReturnsAsync(() =>
            {
                var created = CreateAttendanceRecord(capturedRecord!.Id, studentUuid, sessionUuid);
                created.Id = capturedRecord!.Id;
                created.Status = request.Status;
                created.CheckInTime = capturedRecord.CheckInTime;
                created.IsManualEntry = true;
                created.EnteredBy = "instructor-user";
                return created;
            });

        var result = await _attendanceService.CreateAttendanceAsync(request, CreateInstructorUser("instructor-user"));

        Assert.NotNull(capturedRecord);
        Assert.Equal(studentUuid, capturedRecord.StudentId);
        Assert.Equal(sessionUuid, capturedRecord.SessionId);
        Assert.Equal(capturedRecord.Id, result.Id);
        _mockStudentRepository.Verify(repository => repository.GetStudentByUuidAsync(studentUuid), Times.Once);
        _mockSessionRepository.Verify(repository => repository.GetSessionByUuidAsync(sessionUuid), Times.Once);
        _mockStudentRepository.Verify(repository => repository.GetStudentByIdAsync(It.IsAny<Guid>()), Times.Never);
        _mockSessionRepository.Verify(repository => repository.GetSessionByIdAsync(It.IsAny<Guid>()), Times.Never);
    }

    [Fact]
    public async Task CreateAttendanceAsync_WithMissingUuidReferences_ThrowsValidationException()
    {
        var request = new CreateAttendanceRequest
        {
            StudentId = Guid.Empty,
            SessionId = Guid.Empty,
            Status = "Present"
        };

        var exception = await Assert.ThrowsAsync<attendance_monitoring.Exceptions.ValidationException>(
            () => _attendanceService.CreateAttendanceAsync(request, CreateInstructorUser("instructor-user")));

        Assert.Equal("StudentId is required.", exception.Message);
    }

    [Fact]
    public async Task UpdateAttendanceByUuidAsync_UsesResolvedLegacyId()
    {
        var attendance = CreateAttendanceRecord(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid());
        var request = new UpdateAttendanceRequest { Status = "Late", Notes = "Updated via UUID" };
        var instructorId = attendance.Session!.Schedule!.InstructorId;

        _mockAttendanceRepository
            .Setup(repository => repository.GetAttendanceByUuidAsync(attendance.Id))
            .ReturnsAsync(attendance);
        _mockAttendanceRepository
            .Setup(repository => repository.GetByIdTrackedAsync(attendance.Id))
            .ReturnsAsync(attendance);
        _mockUserManager
            .Setup(manager => manager.FindByIdAsync("instructor-user"))
            .ReturnsAsync(new IdentityUser { Id = "instructor-user" });
        _mockInstructorRepository
            .Setup(repository => repository.GetInstructorByUserIdAsync("instructor-user"))
            .ReturnsAsync(new Instructor { Id = instructorId });
        _mockAttendanceRepository
            .Setup(repository => repository.UpdateAsync(It.IsAny<AttendanceRecord>()))
            .ReturnsAsync((AttendanceRecord record) => record);
        _mockAttendanceRepository
            .Setup(repository => repository.SaveChangesAsync())
            .ReturnsAsync(1);
        _mockAttendanceRepository
            .Setup(repository => repository.GetByIdAsync(attendance.Id))
            .ReturnsAsync(() =>
            {
                var updated = CreateAttendanceRecord(attendance.Id, Guid.NewGuid(), Guid.NewGuid());
                updated.Id = attendance.Id;
                updated.Status = request.Status!;
                updated.Notes = request.Notes;
                return updated;
            });

        var result = await _attendanceService.UpdateAttendanceByUuidAsync(attendance.Id, request, CreateInstructorUser("instructor-user"));

        Assert.Equal(attendance.Id, result.Id);
        Assert.Equal("Late", result.Status);
        _mockAttendanceRepository.Verify(repository => repository.GetAttendanceByUuidAsync(attendance.Id), Times.Once);
        _mockAttendanceRepository.Verify(repository => repository.GetByIdTrackedAsync(attendance.Id), Times.Once);
    }

    [Fact]
    public async Task DeleteAttendanceByUuidAsync_UsesResolvedLegacyId()
    {
        var attendance = CreateAttendanceRecord(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid());

        _mockAttendanceRepository
            .Setup(repository => repository.GetAttendanceByUuidAsync(attendance.Id))
            .ReturnsAsync(attendance);
        _mockAttendanceRepository
            .Setup(repository => repository.GetByIdAsync(attendance.Id))
            .ReturnsAsync(attendance);
        _mockAttendanceRepository
            .Setup(repository => repository.DeleteAsync(attendance.Id))
            .ReturnsAsync(true);
        _mockAttendanceRepository
            .Setup(repository => repository.SaveChangesAsync())
            .ReturnsAsync(1);

        var deleted = await _attendanceService.DeleteAttendanceByUuidAsync(attendance.Id, CreateAdminUser("admin-user"));

        Assert.True(deleted);
        _mockAttendanceRepository.Verify(repository => repository.GetAttendanceByUuidAsync(attendance.Id), Times.Once);
        _mockAttendanceRepository.Verify(repository => repository.DeleteAsync(attendance.Id), Times.Once);
    }

    private static ClaimsPrincipal CreateInstructorUser(string userId)
        => new(new ClaimsIdentity(
            [
                new Claim(ClaimTypes.NameIdentifier, userId),
                new Claim(ClaimTypes.Role, RoleConstants.Instructor)
            ],
            "TestAuth"));

    private static ClaimsPrincipal CreateAdminUser(string userId)
        => new(new ClaimsIdentity(
            [
                new Claim(ClaimTypes.NameIdentifier, userId),
                new Claim(ClaimTypes.Role, RoleConstants.Admin)
            ],
            "TestAuth"));

    private static AttendanceRecord CreateAttendanceRecord(Guid attendanceId, Guid studentId, Guid sessionId)
    {
        return new AttendanceRecord
        {
            Id = attendanceId,
            StudentId = studentId,
            SessionId = sessionId,
            CheckInTime = DateTime.UtcNow.AddMinutes(-5),
            Status = "Present",
            Notes = "Existing attendance",
            IsManualEntry = true,
            EnteredBy = "instructor-user",
            Student = new Student
            {
                Id = studentId,
                Firstname = "Sam",
                Lastname = "Student",
                SectionId = Guid.NewGuid()
            },
            Session = new Session
            {
                Id = sessionId,
                SessionDate = DateTime.UtcNow.Date,
                ScheduleId = Guid.NewGuid(),
                Schedule = new Schedules
                {
                    Id = Guid.NewGuid(),
                    InstructorId = Guid.NewGuid(),
                    SectionId = Guid.NewGuid(),
                    SubjectId = Guid.NewGuid(),
                    Subject = new Subject { Id = Guid.NewGuid(), Name = "Testing" },
                    Section = new Section { Id = Guid.NewGuid(), Name = "BSCS-3A" },
                    Classroom = new Classroom { Id = Guid.NewGuid(), Name = "Room 204" },
                    Instructor = new Instructor { Id = Guid.NewGuid(), Firstname = "Ada", Lastname = "Lovelace" }
                },
                ActualRoom = new Classroom { Id = Guid.NewGuid(), Name = "Room 204" }
            }
        };
    }
}
