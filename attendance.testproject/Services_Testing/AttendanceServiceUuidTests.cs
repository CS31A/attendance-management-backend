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
        var attendance = CreateAttendanceRecord(11, 3, 7);

        _mockAttendanceRepository
            .Setup(repository => repository.GetAttendanceByUuidAsync(attendance.Uuid))
            .ReturnsAsync(attendance);
        _mockUserManager
            .Setup(manager => manager.FindByIdAsync("instructor-user"))
            .ReturnsAsync(new IdentityUser { Id = "instructor-user" });
        _mockInstructorRepository
            .Setup(repository => repository.GetInstructorByUserIdAsync("instructor-user"))
            .ReturnsAsync(new Instructor { Id = attendance.Session!.Schedule!.InstructorId });

        var result = await _attendanceService.GetAttendanceByUuidAsync(attendance.Uuid, user);

        Assert.Equal(attendance.Uuid, result!.Id);
        _mockAttendanceRepository.Verify(repository => repository.GetAttendanceByUuidAsync(attendance.Uuid), Times.Once);
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

        _mockUserManager
            .Setup(manager => manager.FindByIdAsync("instructor-user"))
            .ReturnsAsync(new IdentityUser { Id = "instructor-user" });
        _mockStudentRepository
            .Setup(repository => repository.GetStudentByUuidAsync(studentUuid))
            .ReturnsAsync(new Student { Id = 21, Uuid = studentUuid, SectionId = 9 });
        _mockStudentRepository
            .Setup(repository => repository.GetStudentByIdAsync(21))
            .ReturnsAsync(new Student { Id = 21, Uuid = studentUuid, SectionId = 9 });
        _mockSessionRepository
            .Setup(repository => repository.GetSessionByUuidAsync(sessionUuid))
            .ReturnsAsync(new Session
            {
                Id = 44,
                Uuid = sessionUuid,
                Schedule = new Schedules { InstructorId = 10, SectionId = 9, SubjectId = 13 }
            });
        _mockSessionRepository
            .Setup(repository => repository.GetSessionByIdAsync(44))
            .ReturnsAsync(new Session
            {
                Id = 44,
                Uuid = sessionUuid,
                Schedule = new Schedules { InstructorId = 10, SectionId = 9, SubjectId = 13 }
            });
        _mockStudentEnrollmentRepository
            .Setup(repository => repository.GetStudentEnrollmentsAsync(21))
            .ReturnsAsync([new StudentEnrollment { StudentId = 21, SectionId = 9 }]);
        _mockAttendanceRepository
            .Setup(repository => repository.CreateAsync(It.IsAny<AttendanceRecord>()))
            .Callback<AttendanceRecord>(record => capturedRecord = record)
            .ReturnsAsync(new AttendanceRecord { Id = 100 });
        _mockAttendanceRepository
            .Setup(repository => repository.SaveChangesAsync())
            .ReturnsAsync(1);
        _mockAttendanceRepository
            .Setup(repository => repository.GetByIdAsync(100))
            .ReturnsAsync(() =>
            {
                var created = CreateAttendanceRecord(100, 21, 44);
                created.Status = request.Status;
                created.CheckInTime = capturedRecord!.CheckInTime;
                created.IsManualEntry = true;
                created.EnteredBy = "instructor-user";
                return created;
            });

        var result = await _attendanceService.CreateAttendanceAsync(request, CreateInstructorUser("instructor-user"));

        Assert.NotNull(capturedRecord);
        Assert.Equal(21, capturedRecord.StudentId);
        Assert.Equal(44, capturedRecord.SessionId);
        Assert.NotEqual(Guid.Empty, result.Id);
        _mockStudentRepository.Verify(repository => repository.GetStudentByUuidAsync(studentUuid), Times.Once);
        _mockSessionRepository.Verify(repository => repository.GetSessionByUuidAsync(sessionUuid), Times.Once);
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
        var attendance = CreateAttendanceRecord(11, 3, 7);
        var request = new UpdateAttendanceRequest { Status = "Late", Notes = "Updated via UUID" };

        _mockAttendanceRepository
            .Setup(repository => repository.GetAttendanceByUuidAsync(attendance.Uuid))
            .ReturnsAsync(attendance);
        _mockAttendanceRepository
            .Setup(repository => repository.GetByIdTrackedAsync(attendance.Id))
            .ReturnsAsync(CreateAttendanceRecord(attendance.Id, 3, 7));
        _mockUserManager
            .Setup(manager => manager.FindByIdAsync("instructor-user"))
            .ReturnsAsync(new IdentityUser { Id = "instructor-user" });
        _mockInstructorRepository
            .Setup(repository => repository.GetInstructorByUserIdAsync("instructor-user"))
            .ReturnsAsync(new Instructor { Id = attendance.Session!.Schedule!.InstructorId });
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
                var updated = CreateAttendanceRecord(attendance.Id, 3, 7);
                updated.Status = request.Status!;
                updated.Notes = request.Notes;
                return updated;
            });

        var result = await _attendanceService.UpdateAttendanceByUuidAsync(attendance.Uuid, request, CreateInstructorUser("instructor-user"));

        Assert.NotEqual(Guid.Empty, result.Id);
        Assert.Equal("Late", result.Status);
        _mockAttendanceRepository.Verify(repository => repository.GetAttendanceByUuidAsync(attendance.Uuid), Times.Once);
        _mockAttendanceRepository.Verify(repository => repository.GetByIdTrackedAsync(attendance.Id), Times.Once);
    }

    [Fact]
    public async Task DeleteAttendanceByUuidAsync_UsesResolvedLegacyId()
    {
        var attendance = CreateAttendanceRecord(11, 3, 7);

        _mockAttendanceRepository
            .Setup(repository => repository.GetAttendanceByUuidAsync(attendance.Uuid))
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

        var deleted = await _attendanceService.DeleteAttendanceByUuidAsync(attendance.Uuid, CreateAdminUser("admin-user"));

        Assert.True(deleted);
        _mockAttendanceRepository.Verify(repository => repository.GetAttendanceByUuidAsync(attendance.Uuid), Times.Once);
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

    private static AttendanceRecord CreateAttendanceRecord(int attendanceId, int studentId, int sessionId)
    {
        return new AttendanceRecord
        {
            Id = attendanceId,
            Uuid = Guid.NewGuid(),
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
                SectionId = 9
            },
            Session = new Session
            {
                Id = sessionId,
                Uuid = Guid.NewGuid(),
                SessionDate = DateTime.UtcNow.Date,
                ScheduleId = 4,
                Schedule = new Schedules
                {
                    Id = 4,
                    InstructorId = 10,
                    SectionId = 9,
                    SubjectId = 13,
                    Subject = new Subject { Id = 13, Name = "Testing" },
                    Section = new Section { Id = 9, Name = "BSCS-3A" },
                    Classroom = new Classroom { Id = 2, Name = "Room 204" },
                    Instructor = new Instructor { Id = 10, Firstname = "Ada", Lastname = "Lovelace" }
                },
                ActualRoom = new Classroom { Id = 2, Name = "Room 204" }
            }
        };
    }
}
