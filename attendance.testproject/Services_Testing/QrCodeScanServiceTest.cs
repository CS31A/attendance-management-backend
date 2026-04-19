using System.Security.Claims;
using attendance_monitoring.Classes;
using attendance_monitoring.Constants;
using attendance_monitoring.Data;
using attendance_monitoring.IRepository;
using attendance_monitoring.IServices;
using attendance_monitoring.Models.DTO.Request;
using attendance_monitoring.Services.QrCode;
using Microsoft.EntityFrameworkCore;
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

    private static QrCodeScanService CreateService(
        ApplicationDbContext dbContext,
        IQrCodeRepository qrCodeRepository,
        IStudentRepository studentRepository,
        IUserContextService userContextService)
    {
        var authorizationService = new QrCodeAuthorizationService(
            Mock.Of<ISessionRepository>(),
            Mock.Of<IStudentEnrollmentService>(),
            userContextService,
            NullLogger<QrCodeAuthorizationService>.Instance);

        return new QrCodeScanService(
            dbContext,
            qrCodeRepository,
            studentRepository,
            Mock.Of<IAttendanceService>(),
            Mock.Of<IAttendanceRepository>(),
            Mock.Of<INotificationService>(),
            authorizationService,
            NullLogger<QrCodeScanService>.Instance);
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