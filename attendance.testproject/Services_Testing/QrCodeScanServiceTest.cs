using System.Security.Claims;
using attendance_monitoring.Classes;
using attendance_monitoring.Constants;
using attendance_monitoring.Data;
using attendance_monitoring.Extensions;
using attendance_monitoring.IRepository;
using attendance_monitoring.IServices;
using attendance_monitoring.Models.DTO.Request;
using attendance_monitoring.Models.DTO.Response;
using attendance_monitoring.Options;
using attendance_monitoring.Services.QrCode;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using Microsoft.Extensions.Logging.Abstractions;

namespace attendance.testproject.Services_Testing;

public class QrCodeScanServiceTest
{
    [Fact]
    public async Task ScanQrCodeAsync_NonStudentRole_DoesNotStartTransaction()
    {
        await using var context = CreateDbContext();
        var qrCodeRepository = new Mock<IQrCodeRepository>();
        var studentRepository = new Mock<IStudentRepository>();
        var userContextService = new Mock<IUserContextService>();

        var service = CreateService(
            context,
            qrCodeRepository.Object,
            studentRepository.Object,
            userContextService.Object);

        var principal = CreatePrincipal("instructor-user", RoleConstants.Instructor);

        var result = await service.ScanQrCodeAsync(
            new ValidateQrCode { QrHash = "test-hash" },
            principal);

        Assert.False(result.Success);
        Assert.Equal("Only students can scan QR codes", result.Message);
        qrCodeRepository.Verify(repository => repository.BeginTransactionAsync(), Times.Never);
    }

    [Fact]
    public async Task ScanQrCodeAsync_MultiRoleStudent_UsesStudentClaimAndSkipsTransactionOnReadValidation()
    {
        await using var context = CreateDbContext();
        var qrCodeRepository = new Mock<IQrCodeRepository>();
        var studentRepository = new Mock<IStudentRepository>();
        var userContextService = new Mock<IUserContextService>();

        userContextService
            .Setup(service => service.GetUserIdAsync(It.IsAny<ClaimsPrincipal>()))
            .ReturnsAsync("student-user");

        studentRepository
            .Setup(repository => repository.GetStudentByUserIdAsync("student-user"))
            .ReturnsAsync(new Student
            {
                Id = 7,
                UserId = "student-user",
                Firstname = "Sam",
                Lastname = "Student",
                SectionId = 1
            });

        qrCodeRepository
            .Setup(repository => repository.GetQrCodeByHashAsync("test-hash"))
            .ReturnsAsync((QrCode?)null);

        var service = CreateService(
            context,
            qrCodeRepository.Object,
            studentRepository.Object,
            userContextService.Object);

        var principal = CreatePrincipal(
            "student-user",
            RoleConstants.Admin,
            RoleConstants.Student);

        var result = await service.ScanQrCodeAsync(
            new ValidateQrCode { QrHash = "test-hash" },
            principal);

        Assert.False(result.Success);
        Assert.Equal("QR code not found", result.Message);
        qrCodeRepository.Verify(repository => repository.GetQrCodeByHashAsync("test-hash"), Times.Once);
        qrCodeRepository.Verify(repository => repository.BeginTransactionAsync(), Times.Never);
    }

    [Fact]
    public async Task ScanQrCodeAsync_SuccessfulScan_UsesLocalTimeForAttendanceCreation()
    {
        await using var context = CreateDbContext();
        var qrCodeRepository = new Mock<IQrCodeRepository>();
        var studentRepository = new Mock<IStudentRepository>();
        var attendanceService = new Mock<IAttendanceService>();
        var attendanceRepository = new Mock<IAttendanceRepository>();
        var notificationService = new Mock<INotificationService>();
        var userContextService = new Mock<IUserContextService>();
        var transaction = new Mock<IDbContextTransaction>();

        var student = new Student
        {
            Id = 7,
            UserId = "student-user",
            Firstname = "Sam",
            Lastname = "Student",
            SectionId = 1
        };

        var session = new Session
        {
            Id = 15,
            ActualRoom = new Classroom { Name = "Integration Room 1" },
            Schedule = new Schedules
            {
                SectionId = 1,
                SubjectId = 2,
                Section = new Section { Name = "INT-SEC-A" },
                Subject = new Subject { Name = "Integration Testing" },
                Instructor = new Instructor
                {
                    Firstname = "Ivy",
                    Lastname = "Instructor",
                    UserId = string.Empty
                }
            }
        };

        var qrCode = new QrCode
        {
            Id = 31,
            QrHash = "test-hash",
            SessionId = session.Id,
            Session = session,
            IsActive = true,
            ExpiresAt = DateTime.UtcNow.AddMinutes(5),
            UsageCount = 0,
            MaxUsage = 3
        };

        DateTime capturedCheckInTime = default;

        userContextService
            .Setup(service => service.GetUserIdAsync(It.IsAny<ClaimsPrincipal>()))
            .ReturnsAsync(student.UserId);

        studentRepository
            .Setup(repository => repository.GetStudentByUserIdAsync(student.UserId))
            .ReturnsAsync(student);

        qrCodeRepository
            .Setup(repository => repository.GetQrCodeByHashAsync(qrCode.QrHash))
            .ReturnsAsync(qrCode);

        attendanceRepository
            .Setup(repository => repository.HasAttendanceRecordAsync(student.Id, session.Id))
            .ReturnsAsync(false);

        qrCodeRepository
            .Setup(repository => repository.BeginTransactionAsync())
            .ReturnsAsync(transaction.Object);

        qrCodeRepository
            .Setup(repository => repository.AtomicIncrementUsageAsync(qrCode.QrHash, It.IsAny<DateTime>()))
            .ReturnsAsync(1);

        attendanceService
            .Setup(service => service.CreateAttendanceFromQrScanAsync(student.Id, session.Id, qrCode.Id, It.IsAny<DateTime>()))
            .Callback<int, int, int, DateTime>((_, _, _, checkInTime) => capturedCheckInTime = checkInTime)
            .ReturnsAsync(new AttendanceRecordResponseDto
            {
                Id = Guid.NewGuid(),
                StudentId = student.Uuid,
                SessionId = session.Uuid,
                CheckInTime = DateTime.Now,
                Status = "Present"
            });

        var authorizationService = new QrCodeAuthorizationService(
            Mock.Of<ISessionRepository>(),
            Mock.Of<IStudentEnrollmentService>(),
            userContextService.Object,
            NullLogger<QrCodeAuthorizationService>.Instance);

        var timeZoneProvider = new ConfiguredTimeZoneProvider(
            new TimeZoneSettings { TimeZoneId = TimeZoneInfo.Local.Id });

        var service = new QrCodeScanService(
            context,
            qrCodeRepository.Object,
            studentRepository.Object,
            attendanceService.Object,
            attendanceRepository.Object,
            notificationService.Object,
            authorizationService,
            NullLogger<QrCodeScanService>.Instance,
            timeZoneProvider);

        var principal = CreatePrincipal(student.UserId, RoleConstants.Student);

        var result = await service.ScanQrCodeAsync(
            new ValidateQrCode { QrHash = qrCode.QrHash },
            principal);

        Assert.True(result.Success);
        Assert.True(result.AttendanceMarked);
        // CheckInTime should be set (not default)
        Assert.NotEqual(default, capturedCheckInTime);
    }

    [Fact]
    public async Task ScanQrCodeAsync_AmbientTransaction_DoesNotBeginNestedTransaction()
    {
        await using var innerConnection = new SqliteConnection("Data Source=:memory:");
        await innerConnection.OpenAsync();

        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseSqlite(innerConnection)
            .Options;
        await using var context = new ApplicationDbContext(options);
        await context.Database.EnsureCreatedAsync();

        await using var ambientTransaction = await context.Database.BeginTransactionAsync();

        var qrCodeRepository = new Mock<IQrCodeRepository>();
        var studentRepository = new Mock<IStudentRepository>();
        var attendanceService = new Mock<IAttendanceService>();
        var attendanceRepository = new Mock<IAttendanceRepository>();
        var notificationService = new Mock<INotificationService>();
        var userContextService = new Mock<IUserContextService>();

        var student = new Student
        {
            Id = 7,
            UserId = "student-user",
            Firstname = "Sam",
            Lastname = "Student",
            SectionId = 1
        };

        var session = new Session
        {
            Id = 15,
            ActualRoom = new Classroom { Name = "Room 1" },
            Schedule = new Schedules
            {
                SectionId = 1,
                SubjectId = 2,
                Section = new Section { Name = "SEC-A" },
                Subject = new Subject { Name = "Test Subject" },
                Instructor = new Instructor
                {
                    Firstname = "Ivy",
                    Lastname = "Instructor",
                    UserId = "inst-1"
                }
            }
        };

        var qrCode = new QrCode
        {
            Id = 31,
            QrHash = "test-hash",
            SessionId = session.Id,
            Session = session,
            IsActive = true,
            ExpiresAt = DateTime.UtcNow.AddMinutes(5),
            UsageCount = 0,
            MaxUsage = 3
        };

        userContextService
            .Setup(service => service.GetUserIdAsync(It.IsAny<ClaimsPrincipal>()))
            .ReturnsAsync(student.UserId);

        studentRepository
            .Setup(repository => repository.GetStudentByUserIdAsync(student.UserId))
            .ReturnsAsync(student);

        qrCodeRepository
            .Setup(repository => repository.GetQrCodeByHashAsync(qrCode.QrHash))
            .ReturnsAsync(qrCode);

        attendanceRepository
            .Setup(repository => repository.HasAttendanceRecordAsync(student.Id, session.Id))
            .ReturnsAsync(false);

        qrCodeRepository
            .Setup(repository => repository.AtomicIncrementUsageAsync(qrCode.QrHash, It.IsAny<DateTime>()))
            .ReturnsAsync(1);

        attendanceService
            .Setup(service => service.CreateAttendanceFromQrScanAsync(student.Id, session.Id, qrCode.Id, It.IsAny<DateTime>()))
            .ReturnsAsync(new AttendanceRecordResponseDto
            {
                Id = Guid.NewGuid(),
                StudentId = student.Uuid,
                SessionId = session.Uuid,
                CheckInTime = DateTime.Now,
                Status = "Present"
            });

        var authorizationService = new QrCodeAuthorizationService(
            Mock.Of<ISessionRepository>(),
            Mock.Of<IStudentEnrollmentService>(),
            userContextService.Object,
            NullLogger<QrCodeAuthorizationService>.Instance);

        var timeZoneProvider = new ConfiguredTimeZoneProvider(
            new TimeZoneSettings { TimeZoneId = TimeZoneInfo.Local.Id });

        var service = new QrCodeScanService(
            context,
            qrCodeRepository.Object,
            studentRepository.Object,
            attendanceService.Object,
            attendanceRepository.Object,
            notificationService.Object,
            authorizationService,
            NullLogger<QrCodeScanService>.Instance,
            timeZoneProvider);

        var principal = CreatePrincipal(student.UserId, RoleConstants.Student);

        var result = await service.ScanQrCodeAsync(
            new ValidateQrCode { QrHash = qrCode.QrHash },
            principal);

        Assert.True(result.Success);
        Assert.True(result.AttendanceMarked);
        qrCodeRepository.Verify(repository => repository.BeginTransactionAsync(), Times.Never);
    }

    private static QrCodeScanService CreateService(
        ApplicationDbContext dbContext,
        IQrCodeRepository qrCodeRepository,
        IStudentRepository studentRepository,
        IUserContextService userContextService,
        ConfiguredTimeZoneProvider? timeZoneProvider = null)
    {
        var authorizationService = new QrCodeAuthorizationService(
            Mock.Of<ISessionRepository>(),
            Mock.Of<IStudentEnrollmentService>(),
            userContextService,
            NullLogger<QrCodeAuthorizationService>.Instance);

        // Use system local timezone if not provided
        timeZoneProvider ??= new ConfiguredTimeZoneProvider(
            new TimeZoneSettings { TimeZoneId = TimeZoneInfo.Local.Id });

        return new QrCodeScanService(
            dbContext,
            qrCodeRepository,
            studentRepository,
            Mock.Of<IAttendanceService>(),
            Mock.Of<IAttendanceRepository>(),
            Mock.Of<INotificationService>(),
            authorizationService,
            NullLogger<QrCodeScanService>.Instance,
            timeZoneProvider);
    }

    private static ApplicationDbContext CreateDbContext()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        return new ApplicationDbContext(options);
    }

    private static ClaimsPrincipal CreatePrincipal(string userId, params string[] roles)
    {
        var claims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, userId),
            new(ClaimTypes.Name, userId)
        };

        claims.AddRange(roles.Select(role => new Claim(ClaimTypes.Role, role)));

        return new ClaimsPrincipal(new ClaimsIdentity(claims, "TestAuthentication"));
    }
}
